using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace QisFadingElevator
{
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
        private const int RepairAnimationTotalTicks = 132;

        private static ModEntry? Instance;

        private ModConfig Config = null!;
        private FootholdManager Manager = null!;
        private FootholdSaveData Data = new();
        private DepthGauge Gauge = null!;
        private Texture2D Sprites = null!;
        private readonly StoryToast StoryNotice = new();

        /// <summary>Deepest new record reached since entering the cavern; announced on surfacing.</summary>
        private int pendingRecordFloor;

        /// <summary>Last locally observed health value, used to identify one health-loss event per hit.</summary>
        private int lastObservedHealth = -1;

        /// <summary>Whether the previous health observation happened on a Skull Cavern floor.</summary>
        private bool lastObservedInCavern;

        /// <summary>Transient awakening sequence; repaired state is persisted before this begins.</summary>
        private int repairAnimationTicksRemaining;
        private int lastRepairCue = -1;
        private int lastRecordToastVariant = -1;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            this.Config = helper.ReadConfig<ModConfig>();
            this.Manager = new FootholdManager(this.Config);
            this.Sprites = helper.ModContent.Load<Texture2D>("assets/qfe-sprites.png");
            this.Gauge = new DepthGauge(this.Sprites);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;

            helper.ConsoleCommands.Add("qfe_foothold", "Set your Skull Cavern foothold (and record) to a floor, for testing. Usage: qfe_foothold <floor>", this.CmdSetFoothold);
            helper.ConsoleCommands.Add("qfe_status", "Show your current foothold, record, repair state, and fade clock.", this.CmdStatus);
            helper.ConsoleCommands.Add("qfe_decay", "Apply hourly fading immediately for testing. Usage: qfe_decay [hours]", this.CmdDecay);
            helper.ConsoleCommands.Add("qfe_damage", "Test a hit. Usage: qfe_damage [health lost] [monster|blast|other]", this.CmdDamage);
            helper.ConsoleCommands.Add("qfe_repair", "Set the shaft repair state for testing. Usage: qfe_repair <on|off>", this.CmdRepair);

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                AccessTools.Method(
                    typeof(Farmer),
                    nameof(Farmer.takeDamage),
                    new[] { typeof(int), typeof(bool), typeof(StardewValley.Monsters.Monster) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeFarmerTakeDamage)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterFarmerTakeDamage)));

            this.Monitor.Log("Qi's Fading Elevator loaded. The shaft remembers... for now.", LogLevel.Info);
        }

        /// <summary>Capture the pre-call state so the postfix can distinguish a real hit from a parry/prevention.</summary>
        private static void BeforeFarmerTakeDamage(Farmer __instance, int damage, bool overrideParry, Monster? damager, out DamagePatchState __state)
        {
            DamageSource source = damager is not null
                ? DamageSource.Monster
                : overrideParry ? DamageSource.Blast : DamageSource.Other;
            __state = new DamagePatchState(__instance.health, __instance.temporarilyInvincible, Math.Max(1, damage), source);
        }

        /// <summary>Charge one floor after the game's own damage method confirms a successful local hit.</summary>
        private static void AfterFarmerTakeDamage(Farmer __instance, DamagePatchState __state)
        {
            ModEntry? mod = Instance;
            if (mod is null
                || !mod.Config.Enabled
                || !mod.Data.IsRepaired
                || !Context.IsWorldReady
                || !ReferenceEquals(__instance, Game1.player)
                || !ElevatorPrototype.IsSkullCavernFloor(__instance.currentLocation))
            {
                return;
            }

            bool registeredHit = __instance.health != __state.Health
                || (!__state.WasInvincible && __instance.temporarilyInvincible);
            if (!registeredHit)
                return;

            int healthLost = Math.Max(0, __state.Health - __instance.health);
            int severity = healthLost > 0 ? healthLost : __state.IncomingDamage;
            mod.ApplyDamagePenalty(DamageFloorsFor(severity, __state.Source));
            mod.lastObservedHealth = __instance.health;
            mod.lastObservedInCavern = true;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Register the in-game config menu if GMCM is installed.</summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm is null)
                return;

            gmcm.Register(this.ModManifest, this.ResetConfig, this.SaveConfig);

            gmcm.AddBoolOption(this.ModManifest, () => this.Config.Enabled, v => this.Config.Enabled = v, () => "Wake the old shaft");
            gmcm.AddBoolOption(this.ModManifest, () => this.Config.ShowDepthGauge, v => this.Config.ShowDepthGauge = v, () => "Show the shaft's memory");
            gmcm.AddBoolOption(this.ModManifest, () => this.Config.ShowToasts, v => this.Config.ShowToasts = v, () => "Hear the old shaft", () => "Keep the rare restoration and new-record notices.");

            gmcm.AddSectionTitle(this.ModManifest, () => "The Old Shaft");
            gmcm.AddNumberOption(this.ModManifest, () => this.Config.FloorInterval, v => this.Config.FloorInterval = v, () => "Etched stopping marks", () => "The space between the floors carved into its panel. The deepest memory always remains.", 1, 25);

            gmcm.AddSectionTitle(this.ModManifest, () => "The Cavern's Hunger");
            gmcm.AddNumberOption(this.ModManifest, () => (float)this.Config.FadePercentPerHour, v => this.Config.FadePercentPerHour = v, () => "The cavern's hunger", () => "How much of the remembered path the stone reclaims each hour, including sleep.", 0f, 5f, 0.05f);

            gmcm.AddSectionTitle(this.ModManifest, () => "Mercies");
            gmcm.AddNumberOption(this.ModManifest, () => this.Config.MinFoothold, v => this.Config.MinFoothold = v, () => "Memory that cannot fade", () => "The shallowest path the stone must remember forever.", 0, 500);
            gmcm.AddNumberOption(this.ModManifest, () => (float)this.Config.LuckInfluence, v => this.Config.LuckInfluence = v, () => "Fortune's mercy", () => "How strongly fortune calms—or provokes—the cavern. At zero, fortune is silent.", 0f, 5f, 0.25f);
        }

        private void ResetConfig()
        {
            this.Config = new ModConfig();
            this.Manager = new FootholdManager(this.Config);
        }

        private void SaveConfig()
        {
            this.Manager = new FootholdManager(this.Config);
            this.Helper.WriteConfig(this.Config);
        }

        /// <summary>Load this save's foothold state.</summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            FootholdSaveData? loaded = this.Helper.Data.ReadSaveData<FootholdSaveData>(SaveKey);
            if (loaded is null)
            {
                this.Data = new FootholdSaveData { DataVersion = CurrentDataVersion };
            }
            else
            {
                this.Data = loaded;
            }

            MigrateSaveData(this.Data);
            this.ApplyPendingSleepFade();
            this.pendingRecordFloor = 0;
            this.repairAnimationTicksRemaining = 0;
            this.lastRepairCue = -1;
            this.lastRecordToastVariant = -1;
            this.StoryNotice.Clear();
            this.ResetHealthObservation();
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
            this.ResetHealthObservation();
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

            this.ObserveHealthLoss();
            this.StoryNotice.Update();
            this.UpdateRepairAnimation();
            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.Update();
            if (this.Config.Enabled)
            {
                int repairElapsed = this.repairAnimationTicksRemaining > 0
                    ? RepairAnimationTotalTicks - this.repairAnimationTicksRemaining
                    : -1;
                ElevatorPrototype.EnsureSprite(Game1.currentLocation, this.Sprites, this.Data.IsRepaired, repairElapsed);
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
            this.CaptureHealthLossBeforeWarp(e.OldLocation);
            this.ResetHealthObservation();

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
        private void ApplyHourlyFade(int hours)
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
        private FadeResult ApplyDamagePenalty(int requestedFloors)
        {
            FadeResult fade = this.Manager.ApplyDamageFade(this.Data, requestedFloors);
            if (!fade.Applied)
                return fade;

            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.PulseDecay(fade.LostFloors, fromDamage: true);
            return fade;
        }

        /// <summary>Translate actual health loss into memory loss; explosions/hazards bite harder than creatures.</summary>
        private static int DamageFloorsFor(int healthLost, DamageSource source)
        {
            int severity = Math.Max(1, healthLost);
            int floors = severity switch
            {
                <= 10 => 1,
                <= 20 => 2,
                <= 35 => 3,
                _ => 4
            };

            if (source == DamageSource.Blast && severity > 10)
                floors++;
            return Math.Min(5, floors);
        }

        /// <summary>
        /// Poll local health once per update. Stardew applies each hit atomically and grants invincibility
        /// frames, so one downward transition represents one enemy, bomb, trap, or exhaustion hit—not HP lost.
        /// </summary>
        private void ObserveHealthLoss()
        {
            int currentHealth = Game1.player.health;
            bool inCavern = ElevatorPrototype.IsSkullCavernFloor(Game1.currentLocation);
            if (this.Config.Enabled
                && this.Data.IsRepaired
                && this.lastObservedHealth >= 0
                && this.lastObservedInCavern
                && inCavern
                && currentHealth < this.lastObservedHealth)
            {
                int healthLost = this.lastObservedHealth - currentHealth;
                this.ApplyDamagePenalty(DamageFloorsFor(healthLost, DamageSource.Other));
            }

            this.lastObservedHealth = currentHealth;
            this.lastObservedInCavern = inCavern;
        }

        /// <summary>Catch a lethal/final hit if the game warped the farmer out before the next update tick.</summary>
        private void CaptureHealthLossBeforeWarp(GameLocation oldLocation)
        {
            if (this.Config.Enabled
                && this.Data.IsRepaired
                && this.lastObservedHealth >= 0
                && ElevatorPrototype.IsSkullCavernFloor(oldLocation)
                && Game1.player.health < this.lastObservedHealth)
            {
                int healthLost = this.lastObservedHealth - Game1.player.health;
                this.ApplyDamagePenalty(DamageFloorsFor(healthLost, DamageSource.Other));
            }
        }

        private void ResetHealthObservation()
        {
            if (!Context.IsWorldReady)
            {
                this.lastObservedHealth = -1;
                this.lastObservedInCavern = false;
                return;
            }

            this.lastObservedHealth = Game1.player.health;
            this.lastObservedInCavern = ElevatorPrototype.IsSkullCavernFloor(Game1.currentLocation);
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
            this.repairAnimationTicksRemaining = RepairAnimationTotalTicks;
            this.lastRepairCue = -1;
            this.StoryNotice.Clear();
        }

        /// <summary>Drive three physical repair impacts followed by the Qi seam awakening.</summary>
        private void UpdateRepairAnimation()
        {
            if (this.repairAnimationTicksRemaining <= 0)
                return;

            int elapsed = RepairAnimationTotalTicks - this.repairAnimationTicksRemaining;
            int cue = elapsed switch
            {
                < 28 => 0,
                < 56 => 1,
                < 72 => 2,
                < 92 => 3,
                _ => 4
            };
            if (cue != this.lastRepairCue)
            {
                this.lastRepairCue = cue;
                switch (cue)
                {
                    case 0:
                        Game1.playSound("clank");
                        break;
                    case 1:
                    case 2:
                        Game1.playSound("hammer");
                        break;
                    case 3:
                        Game1.playSound("crystal");
                        break;
                }
            }

            this.repairAnimationTicksRemaining--;
            if (this.repairAnimationTicksRemaining == 0)
            {
                Game1.playSound("secret1");
                this.Gauge.Pulse();
                this.Toast("toast.repair-complete");
            }
        }

        /// <summary>Console command: seed a foothold so the elevator/gauge can be tested without grinding down the cavern.</summary>
        private void CmdSetFoothold(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }
            if (args.Length < 1 || !int.TryParse(args[0], out int floor) || floor < 0)
            {
                this.Monitor.Log("Usage: qfe_foothold <floor>  (e.g. qfe_foothold 120)", LogLevel.Warn);
                return;
            }

            this.Data.Foothold = floor;
            this.Data.DeepestFloor = floor;
            this.Data.FadeMinutes = 0;
            this.Data.HourlyFadeRemainder = 0;
            this.Gauge.SetValues(this.Data.Foothold, this.Data.DeepestFloor);
            this.Gauge.Pulse();
            this.Monitor.Log($"The old shaft now remembers floor {floor}.", LogLevel.Info);
        }

        /// <summary>Console command: trigger hourly decay without waiting on the game clock.</summary>
        private void CmdDecay(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            int hours = 1;
            if (args.Length > 0 && (!int.TryParse(args[0], out hours) || hours < 1 || hours > 24))
            {
                this.Monitor.Log("Usage: qfe_decay [hours from 1 to 24]", LogLevel.Warn);
                return;
            }

            this.ApplyHourlyFade(hours);
            this.Monitor.Log($"Applied {hours} hourly fade pulse(s). Foothold is now {this.Manager.ReachableFloor(this.Data)} ({this.Data.Foothold:0.00} exact).", LogLevel.Info);
        }

        /// <summary>Console command: test the variable hit curve without taking actual damage.</summary>
        private void CmdDamage(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            int healthLost = 10;
            if (args.Length > 0 && (!int.TryParse(args[0], out healthLost) || healthLost < 1 || healthLost > 999))
            {
                this.Monitor.Log("Usage: qfe_damage [health lost from 1 to 999] [monster|blast|other]", LogLevel.Warn);
                return;
            }

            DamageSource source = DamageSource.Monster;
            if (args.Length > 1 && !Enum.TryParse(args[1], ignoreCase: true, out source))
            {
                this.Monitor.Log("Source must be monster, blast, or other.", LogLevel.Warn);
                return;
            }

            int requested = DamageFloorsFor(healthLost, source);
            FadeResult fade = this.ApplyDamagePenalty(requested);
            this.Monitor.Log($"{source} hit for {healthLost} health requested {requested} floor(s); lost {fade.LostFloors}. Foothold is now {this.Manager.ReachableFloor(this.Data)}.", LogLevel.Info);
        }

        /// <summary>Console command: toggle repaired/broken art and mechanics for testing.</summary>
        private void CmdRepair(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }
            if (args.Length < 1 || (args[0] != "on" && args[0] != "off"))
            {
                this.Monitor.Log("Usage: qfe_repair <on|off>", LogLevel.Warn);
                return;
            }

            this.Data.IsRepaired = args[0] == "on";
            this.Data.DataVersion = CurrentDataVersion;
            this.repairAnimationTicksRemaining = 0;
            this.lastRepairCue = -1;
            ElevatorPrototype.EnsureSprite(Game1.currentLocation, this.Sprites, this.Data.IsRepaired, repairAnimationElapsed: -1);
            this.Monitor.Log(this.Data.IsRepaired ? "The old shaft is repaired." : "The old shaft is broken.", LogLevel.Info);
        }

        /// <summary>Console command: print the current foothold state.</summary>
        private void CmdStatus(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            this.Monitor.Log(
                $"Repaired: {this.Data.IsRepaired} | Foothold: {this.Manager.ReachableFloor(this.Data)} ({this.Data.Foothold:0.00} exact) | Record: {this.Data.DeepestFloor} | Hour clock: {this.Data.FadeMinutes}/60 min | Fade debt: {this.Data.HourlyFadeRemainder:0.000} | HasSkullKey: {Game1.player.hasSkullKey}",
                LogLevel.Info);
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

        private enum DamageSource
        {
            Monster,
            Blast,
            Other
        }

        private readonly record struct DamagePatchState(int Health, bool WasInvincible, int IncomingDamage, DamageSource Source);
    }
}
