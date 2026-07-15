using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace QisFadingElevator
{
    /// <summary>The source of a foothold-damaging hit.</summary>
    internal enum DamageSource
    {
        Monster,
        Blast,
        Other
    }

    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>Skull Cavern floor N is MineShaft level (120 + N).</summary>
        private const int SkullCavernFloorOffset = 120;

        /// <summary>The MineShaft area id for the Skull Cavern (distinct from the mines and the quarry, which is level 77377).</summary>
        private const int SkullCavernAreaId = 121;

        /// <summary>Save-data key.</summary>
        private const string SaveKey = "foothold";

        private const int CurrentDataVersion = 1;
        private const string IridiumBarItemId = "(O)337";
        private const string BatteryPackItemId = "(O)787";
        private const int RepairIridiumBars = 5;
        private const int RepairBatteryPacks = 1;

        private FootholdSaveData data = new();
        private DamageWatcher damageWatcher = null!;
        private Texture2D Sprites = null!;
        private readonly StoryToast StoryNotice = new();

        /// <summary>Deepest new record reached since entering the cavern; announced on surfacing.</summary>
        private int pendingRecordFloor;

        /// <summary>Transient awakening sequence; repaired state is persisted before this begins.</summary>
        private int repairAnimationTicksRemaining;
        private int lastRecordToastVariant = -1;

        /*********
        ** Internal state shared with ConfigMenu/ConsoleCommands/DamageWatcher
        *********/
        internal ModConfig Config { get; private set; } = null!;
        internal FootholdManager Manager { get; private set; } = null!;
        internal DepthGauge Gauge { get; private set; } = null!;
        internal FootholdSaveData Data => this.data;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            this.Manager = new FootholdManager(this.Config);
            this.Sprites = helper.ModContent.Load<Texture2D>("assets/qfe-sprites.png");
            this.Gauge = new DepthGauge(this.Sprites);
            this.damageWatcher = new DamageWatcher(this);

            helper.Events.GameLoop.GameLaunched += (_, _) => ConfigMenu.Register(this);
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;

            new ConsoleCommands(this).Register(helper);
            this.damageWatcher.RegisterPatches(new Harmony(this.ModManifest.UniqueID));

            this.Monitor.Log("Qi's Fading Elevator loaded. The shaft remembers... for now.", LogLevel.Info);
        }

        /*********
        ** Config plumbing (called by ConfigMenu)
        *********/
        internal void ResetConfig()
        {
            this.Config = new ModConfig();
            this.Manager = new FootholdManager(this.Config);
        }

        internal void SaveConfig()
        {
            this.Manager = new FootholdManager(this.Config);
            this.Helper.WriteConfig(this.Config);
        }

        /// <summary>Console-command hook: force the repair state and repaint the fixture.</summary>
        internal void SetRepairedForTesting(bool repaired)
        {
            this.Data.IsRepaired = repaired;
            this.Data.DataVersion = CurrentDataVersion;
            this.repairAnimationTicksRemaining = 0;
            ElevatorPrototype.EnsureSprite(Game1.currentLocation, this.Sprites, this.Data.IsRepaired, repairAnimationElapsed: -1);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Load this save's foothold state.</summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.data = this.Helper.Data.ReadSaveData<FootholdSaveData>(SaveKey)
                ?? new FootholdSaveData { DataVersion = CurrentDataVersion };

            MigrateSaveData(this.Data);
            this.ApplyPendingSleepFade();
            this.pendingRecordFloor = 0;
            this.repairAnimationTicksRemaining = 0;
            this.lastRecordToastVariant = -1;
            this.StoryNotice.Clear();
            this.damageWatcher.Reset();
            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
        }

        private static void MigrateSaveData(FootholdSaveData data)
        {
            if (data.DataVersion < CurrentDataVersion)
            {
                // Players who already built a foothold before this system existed keep their working lift.
                data.IsRepaired = data.DeepestFloor > 0 || data.Foothold > 0;
                data.DataVersion = CurrentDataVersion;
            }

            data.FadeMinutes = Math.Clamp(data.FadeMinutes, 0, 59);
            data.HourlyFadeRemainder = Math.Max(0, data.HourlyFadeRemainder);
            data.PendingSleepMinutes = Math.Clamp(data.PendingSleepMinutes, 0, 1440);
        }

        /// <summary>Persist the foothold state into the save.</summary>
        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Data.DataVersion = CurrentDataVersion;
            this.Helper.Data.WriteSaveData(SaveKey, this.Data);
        }

        /// <summary>Capture the actual time between bedtime and 6:00 AM for overnight fading.</summary>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            this.Data.PendingSleepMinutes = this.Config.Enabled && this.Data.IsRepaired && Context.IsMainPlayer
                ? SleepMinutesUntilSix(Game1.timeOfDay)
                : 0;
        }

        /// <summary>Apply sleeping time, then reset transient health observation.</summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            this.ApplyPendingSleepFade();
            this.damageWatcher.Reset();
        }

        /// <summary>Accumulate active world-clock minutes and fade once for every complete in-game hour.</summary>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!this.Config.Enabled || !this.Data.IsRepaired || !Context.IsWorldReady || !Context.IsMainPlayer)
                return;

            int elapsed = ElapsedClockMinutes(e.OldTime, e.NewTime);
            if (elapsed <= 0)
                return;

            this.AccumulateFadeMinutes(elapsed);
        }

        /// <summary>Animate the gauge and keep it in sync with the current foothold.</summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            this.damageWatcher.Observe();
            this.StoryNotice.Update();
            this.UpdateRepairAnimation();
            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.Update();
            if (this.Config.Enabled)
            {
                int repairElapsed = this.repairAnimationTicksRemaining > 0
                    ? RepairSequence.TotalTicks - this.repairAnimationTicksRemaining
                    : -1;
                ElevatorPrototype.EnsureSprite(Game1.currentLocation, this.Sprites, this.Data.IsRepaired, repairElapsed);

                // The awakened machine sheds a rare mote while the player stands close.
                if (this.Data.IsRepaired
                    && this.repairAnimationTicksRemaining == 0
                    && !Game1.eventUp
                    && ElevatorPrototype.IsPlayerInRange()
                    && Game1.random.NextDouble() < 0.0045)
                {
                    RepairEffects.SpawnAmbientMote(Game1.currentLocation, this.Sprites);
                }
            }
            else
                ElevatorPrototype.RemoveSprite(Game1.currentLocation);
        }

        /// <summary>Draw the depth gauge while the player is near the Skull Cavern.</summary>
        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!this.Config.Enabled || !Context.IsWorldReady || Game1.eventUp)
                return;

            if (this.Config.ShowToasts)
                this.StoryNotice.Draw(e.SpriteBatch);

            if (this.Config.ShowDepthGauge
                && this.Data.IsRepaired
                && ElevatorPrototype.IsSkullCavernFloor(Game1.currentLocation))
                this.Gauge.Draw(e.SpriteBatch);
        }

        /// <summary>Detect Skull Cavern descents (renew the foothold), and announce records on surfacing.</summary>
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            this.damageWatcher.CaptureBeforeWarp(e.OldLocation);
            this.damageWatcher.Reset();

            if (!this.Config.Enabled)
            {
                ElevatorPrototype.RemoveSprite(e.NewLocation);
                return;
            }

            ElevatorPrototype.EnsureSprite(e.NewLocation, this.Sprites, this.Data.IsRepaired, repairAnimationElapsed: -1);

            if (e.NewLocation is MineShaft shaft && shaft.getMineArea() == SkullCavernAreaId)
            {
                int floor = shaft.mineLevel - SkullCavernFloorOffset;
                if (this.Manager.RecordDive(this.Data, floor))
                    this.pendingRecordFloor = Math.Max(this.pendingRecordFloor, floor);
            }
            else if (this.pendingRecordFloor > 0)
            {
                int surfacedRecord = this.pendingRecordFloor;
                this.pendingRecordFloor = 0;

                // A depth only feels like a victory if the cavern did not claw any of it back
                // before the player surfaced. Fractional fade debt is not a lost displayed floor.
                if (FootholdRules.ShouldCelebrateRecord(this.Data.Foothold, surfacedRecord))
                {
                    this.Gauge.Pulse();
                    Game1.playSound("secret1");
                    this.ToastRecord(surfacedRecord);
                }
            }
        }

        /// <summary>Awaken the physical elevator through its doors.</summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!this.Config.Enabled || !Context.IsPlayerFree || e.Button != SButton.MouseRight)
                return;
            if (!ElevatorPrototype.IsCursorOver(e.Cursor.AbsolutePixels))
                return;
            if (!ElevatorPrototype.IsPlayerInRange())
                return;

            this.Helper.Input.Suppress(e.Button);
            this.TrySummonElevator();
        }

        /// <summary>Apply the requested number of hourly fade pulses and aggregate their HUD feedback.</summary>
        internal void ApplyHourlyFade(int hours)
        {
            int lostFloors = 0;
            bool applied = false;
            for (int i = 0; i < hours; i++)
            {
                if (!this.Manager.WouldHourlyFade(this.Data))
                    break;

                FadeResult fade = this.Manager.ApplyHourlyFade(this.Data, Game1.player.DailyLuck);
                applied |= fade.Applied;
                lostFloors += fade.LostFloors;
            }

            if (!applied)
                return;

            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.PulseDecay(lostFloors, fromDamage: false);
        }

        /// <summary>Apply a source/severity-scaled foothold loss for one observed Skull Cavern hit.</summary>
        internal FadeResult ApplyDamagePenalty(int requestedFloors)
        {
            FadeResult fade = this.Manager.ApplyDamageFade(this.Data, requestedFloors);
            if (!fade.Applied)
                return fade;

            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.PulseDecay(fade.LostFloors, fromDamage: true);
            return fade;
        }

        /// <summary>Convert Stardew's HHMM clock values into elapsed playable minutes, ignoring day resets.</summary>
        private static int ElapsedClockMinutes(int oldTime, int newTime)
        {
            if (newTime <= oldTime)
                return 0;

            int oldMinutes = oldTime / 100 * 60 + oldTime % 100;
            int newMinutes = newTime / 100 * 60 + newTime % 100;
            return Math.Max(0, newMinutes - oldMinutes);
        }

        /// <summary>Get in-game minutes from the actual bedtime until the next 6:00 AM.</summary>
        private static int SleepMinutesUntilSix(int bedtime)
        {
            int hour = bedtime / 100;
            int minute = bedtime % 100;
            if (hour < 6)
                hour += 24;

            int elapsedSinceSix = (hour - 6) * 60 + minute;
            return Math.Clamp(1440 - elapsedSinceSix, 0, 1440);
        }

        /// <summary>Add waking or sleeping minutes to the same persistent hourly fade clock.</summary>
        private void AccumulateFadeMinutes(int minutes)
        {
            if (minutes <= 0)
                return;

            this.Data.FadeMinutes += minutes;
            int elapsedHours = this.Data.FadeMinutes / 60;
            this.Data.FadeMinutes %= 60;
            if (elapsedHours > 0)
                this.ApplyHourlyFade(elapsedHours);
        }

        /// <summary>Consume sleeping time exactly once after a day transition.</summary>
        private void ApplyPendingSleepFade()
        {
            int minutes = this.Data.PendingSleepMinutes;
            this.Data.PendingSleepMinutes = 0;
            if (this.Config.Enabled && this.Data.IsRepaired && Context.IsMainPlayer)
                this.AccumulateFadeMinutes(minutes);
        }

        /// <summary>Validate the player's situation and open the elevator menu.</summary>
        private void TrySummonElevator()
        {
            if (this.repairAnimationTicksRemaining > 0)
                return;

            if (!Game1.player.hasSkullKey)
            {
                this.Toast("toast.no-skull-key");
                return;
            }

            if (!ElevatorPrototype.IsPlayerInRange())
                return;

            if (!this.Data.IsRepaired)
            {
                this.TryOfferRepair();
                return;
            }

            if (this.Data.DeepestFloor < 1)
            {
                this.Toast("toast.no-foothold");
                return;
            }

            int reach = this.Manager.ReachableFloor(this.Data);
            if (reach < 1)
            {
                this.Toast("toast.memory-spent");
                return;
            }

            int currentFloor = Game1.currentLocation is MineShaft shaft && shaft.getMineArea() == SkullCavernAreaId
                ? Game1.CurrentMineLevel - SkullCavernFloorOffset
                : -1;
            Game1.activeClickableMenu = new FadingElevatorMenu(reach, this.Config.FloorInterval, currentFloor);
        }

        /// <summary>Offer the one-time restoration when the player examines the broken foyer mechanism.</summary>
        private void TryOfferRepair()
        {
            if (!string.Equals(Game1.currentLocation.NameOrUniqueName, ElevatorPrototype.FoyerLocationName, StringComparison.OrdinalIgnoreCase)
                || this.repairAnimationTicksRemaining > 0)
            {
                return;
            }

            if (!this.HasRepairMaterials(Game1.player))
            {
                Game1.drawObjectDialogue(this.T("repair.inspect"));
                return;
            }

            Response[] responses =
            {
                new("Restore", this.T("repair.restore")),
                new("Leave", this.T("repair.leave"))
            };
            Game1.currentLocation.createQuestionDialogue(this.T("repair.question"), responses, this.OnRepairAnswer);
        }

        private bool HasRepairMaterials(Farmer who)
        {
            return who.Items.CountId(IridiumBarItemId) >= RepairIridiumBars
                && who.Items.CountId(BatteryPackItemId) >= RepairBatteryPacks;
        }

        private void OnRepairAnswer(Farmer who, string answer)
        {
            if (!string.Equals(answer, "Restore", StringComparison.Ordinal)
                || this.Data.IsRepaired
                || !this.HasRepairMaterials(who)
                || !string.Equals(who.currentLocation.NameOrUniqueName, ElevatorPrototype.FoyerLocationName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            who.Items.ReduceId(IridiumBarItemId, RepairIridiumBars);
            who.Items.ReduceId(BatteryPackItemId, RepairBatteryPacks);
            this.Data.IsRepaired = true;
            this.Data.FadeMinutes = 0;
            this.Data.HourlyFadeRemainder = 0;
            this.repairAnimationTicksRemaining = RepairSequence.TotalTicks;
            this.StoryNotice.Clear();

            // Hold the farmer facing the machine for the scene, vanilla-cutscene style.
            who.completelyStopAnimatingOrDoingAction();
            who.faceDirection(0);
            who.freezePause = RepairSequence.TotalTicks * 1000 / 60 + 250;
        }

        /// <summary>Advance the repair scene one tick: impacts, settle, power, seam climb, ignition.</summary>
        private void UpdateRepairAnimation()
        {
            if (this.repairAnimationTicksRemaining <= 0)
                return;

            int elapsed = RepairSequence.TotalTicks - this.repairAnimationTicksRemaining;
            RepairEffects.FireBeats(Game1.currentLocation, elapsed, this.Sprites);

            this.repairAnimationTicksRemaining--;
            if (this.repairAnimationTicksRemaining == 0)
            {
                this.Gauge.Pulse();
                this.Toast("toast.repair-complete");
            }
        }

        /// <summary>Show one cooldown-controlled lower-left notice; never enqueue or stack.</summary>
        private void Toast(string key, object? tokens = null)
        {
            if (this.Config.ShowToasts)
                this.StoryNotice.Show(key, this.T(key, tokens));
        }

        /// <summary>Choose a non-repeating environmental reaction while sharing one record cooldown.</summary>
        private void ToastRecord(int floor)
        {
            const int variantCount = 3;
            int variant = this.lastRecordToastVariant < 0
                ? Game1.random.Next(variantCount)
                : Game1.random.Next(variantCount - 1);
            if (this.lastRecordToastVariant >= 0 && variant >= this.lastRecordToastVariant)
                variant++;
            this.lastRecordToastVariant = variant;

            if (this.Config.ShowToasts)
                this.StoryNotice.Show("toast.new-record", this.T($"toast.new-record.{variant + 1}", new { floor }));
        }

        /// <summary>Look up a translation.</summary>
        private string T(string key, object? tokens = null)
        {
            return tokens is null
                ? this.Helper.Translation.Get(key)
                : this.Helper.Translation.Get(key, tokens);
        }
    }
}
