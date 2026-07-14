using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace QisFadingElevator
{
    /// <summary>
    /// Detects real Skull Cavern hits and converts them into foothold loss. A Harmony patch around
    /// Farmer.takeDamage catches ordinary combat; a per-tick health poll catches everything else
    /// (bombs, hazards, exhaustion), and a pre-warp capture catches lethal final hits.
    /// </summary>
    internal sealed class DamageWatcher
    {
        private static DamageWatcher? Instance;

        private readonly ModEntry mod;

        /// <summary>Last locally observed health value, used to identify one health-loss event per hit.</summary>
        private int lastObservedHealth = -1;

        /// <summary>Whether the previous health observation happened on a Skull Cavern floor.</summary>
        private bool lastObservedInCavern;

        public DamageWatcher(ModEntry mod)
        {
            this.mod = mod;
            Instance = this;
        }

        public void RegisterPatches(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(
                    typeof(Farmer),
                    nameof(Farmer.takeDamage),
                    new[] { typeof(int), typeof(bool), typeof(Monster) }),
                prefix: new HarmonyMethod(typeof(DamageWatcher), nameof(BeforeFarmerTakeDamage)),
                postfix: new HarmonyMethod(typeof(DamageWatcher), nameof(AfterFarmerTakeDamage)));
        }

        /// <summary>Translate actual health loss into memory loss; explosions/hazards bite harder than creatures.</summary>
        public static int DamageFloorsFor(int healthLost, DamageSource source)
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
        public void Observe()
        {
            int currentHealth = Game1.player.health;
            bool inCavern = ElevatorPrototype.IsSkullCavernFloor(Game1.currentLocation);
            if (this.mod.Config.Enabled
                && this.mod.Data.IsRepaired
                && this.lastObservedHealth >= 0
                && this.lastObservedInCavern
                && inCavern
                && currentHealth < this.lastObservedHealth)
            {
                int healthLost = this.lastObservedHealth - currentHealth;
                this.mod.ApplyDamagePenalty(DamageFloorsFor(healthLost, DamageSource.Other));
            }

            this.lastObservedHealth = currentHealth;
            this.lastObservedInCavern = inCavern;
        }

        /// <summary>Catch a lethal/final hit if the game warped the farmer out before the next update tick.</summary>
        public void CaptureBeforeWarp(GameLocation oldLocation)
        {
            if (this.mod.Config.Enabled
                && this.mod.Data.IsRepaired
                && this.lastObservedHealth >= 0
                && ElevatorPrototype.IsSkullCavernFloor(oldLocation)
                && Game1.player.health < this.lastObservedHealth)
            {
                int healthLost = this.lastObservedHealth - Game1.player.health;
                this.mod.ApplyDamagePenalty(DamageFloorsFor(healthLost, DamageSource.Other));
            }
        }

        public void Reset()
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
            DamageWatcher? watcher = Instance;
            if (watcher is null
                || !watcher.mod.Config.Enabled
                || !watcher.mod.Data.IsRepaired
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
            watcher.mod.ApplyDamagePenalty(DamageFloorsFor(severity, __state.Source));
            watcher.lastObservedHealth = __instance.health;
            watcher.lastObservedInCavern = true;
        }

        private readonly record struct DamagePatchState(int Health, bool WasInvincible, int IncomingDamage, DamageSource Source);
    }
}
