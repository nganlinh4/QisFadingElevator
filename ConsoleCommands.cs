using System;
using StardewModdingAPI;
using StardewValley;

namespace QisFadingElevator
{
    /// <summary>SMAPI console commands for exercising foothold, fade, damage, and repair states.</summary>
    internal sealed class ConsoleCommands
    {
        private readonly ModEntry mod;

        public ConsoleCommands(ModEntry mod)
        {
            this.mod = mod;
        }

        public void Register(IModHelper helper)
        {
            helper.ConsoleCommands.Add("qfe_foothold", "Set your Skull Cavern foothold (and record) to a floor, for testing. Usage: qfe_foothold <floor>", this.CmdSetFoothold);
            helper.ConsoleCommands.Add("qfe_status", "Show your current foothold, record, repair state, and fade clock.", this.CmdStatus);
            helper.ConsoleCommands.Add("qfe_decay", "Apply hourly fading immediately for testing. Usage: qfe_decay [hours]", this.CmdDecay);
            helper.ConsoleCommands.Add("qfe_damage", "Test a hit. Usage: qfe_damage [health lost] [monster|blast|other]", this.CmdDamage);
            helper.ConsoleCommands.Add("qfe_repair", "Set the shaft repair state for testing. Usage: qfe_repair <on|off>", this.CmdRepair);
        }

        /// <summary>Seed a foothold so the elevator/gauge can be tested without grinding down the cavern.</summary>
        private void CmdSetFoothold(string command, string[] args)
        {
            if (!this.RequireWorld())
                return;
            if (args.Length < 1 || !int.TryParse(args[0], out int floor) || floor < 0)
            {
                this.mod.Monitor.Log("Usage: qfe_foothold <floor>  (e.g. qfe_foothold 120)", LogLevel.Warn);
                return;
            }

            this.mod.Data.Foothold = floor;
            this.mod.Data.DeepestFloor = floor;
            this.mod.Data.FadeMinutes = 0;
            this.mod.Data.HourlyFadeRemainder = 0;
            this.mod.Gauge.SetValues(this.mod.Data.Foothold, this.mod.Data.DeepestFloor);
            this.mod.Gauge.Pulse();
            this.mod.Monitor.Log($"The old shaft now remembers floor {floor}.", LogLevel.Info);
        }

        /// <summary>Trigger hourly decay without waiting on the game clock.</summary>
        private void CmdDecay(string command, string[] args)
        {
            if (!this.RequireWorld())
                return;

            int hours = 1;
            if (args.Length > 0 && (!int.TryParse(args[0], out hours) || hours < 1 || hours > 24))
            {
                this.mod.Monitor.Log("Usage: qfe_decay [hours from 1 to 24]", LogLevel.Warn);
                return;
            }

            this.mod.ApplyHourlyFade(hours);
            this.mod.Monitor.Log($"Applied {hours} hourly fade pulse(s). Foothold is now {this.mod.Manager.ReachableFloor(this.mod.Data)} ({this.mod.Data.Foothold:0.00} exact).", LogLevel.Info);
        }

        /// <summary>Test the variable hit curve without taking actual damage.</summary>
        private void CmdDamage(string command, string[] args)
        {
            if (!this.RequireWorld())
                return;

            int healthLost = 10;
            if (args.Length > 0 && (!int.TryParse(args[0], out healthLost) || healthLost < 1 || healthLost > 999))
            {
                this.mod.Monitor.Log("Usage: qfe_damage [health lost from 1 to 999] [monster|blast|other]", LogLevel.Warn);
                return;
            }

            DamageSource source = DamageSource.Monster;
            if (args.Length > 1 && !Enum.TryParse(args[1], ignoreCase: true, out source))
            {
                this.mod.Monitor.Log("Source must be monster, blast, or other.", LogLevel.Warn);
                return;
            }

            int requested = DamageWatcher.DamageFloorsFor(healthLost, source);
            FadeResult fade = this.mod.ApplyDamagePenalty(requested);
            this.mod.Monitor.Log($"{source} hit for {healthLost} health requested {requested} floor(s); lost {fade.LostFloors}. Foothold is now {this.mod.Manager.ReachableFloor(this.mod.Data)}.", LogLevel.Info);
        }

        /// <summary>Toggle repaired/broken art and mechanics for testing.</summary>
        private void CmdRepair(string command, string[] args)
        {
            if (!this.RequireWorld())
                return;
            if (args.Length < 1 || (args[0] != "on" && args[0] != "off"))
            {
                this.mod.Monitor.Log("Usage: qfe_repair <on|off>", LogLevel.Warn);
                return;
            }

            this.mod.SetRepairedForTesting(args[0] == "on");
            this.mod.Monitor.Log(this.mod.Data.IsRepaired ? "The old shaft is repaired." : "The old shaft is broken.", LogLevel.Info);
        }

        /// <summary>Print the current foothold state.</summary>
        private void CmdStatus(string command, string[] args)
        {
            if (!this.RequireWorld())
                return;

            this.mod.Monitor.Log(
                $"Repaired: {this.mod.Data.IsRepaired} | Foothold: {this.mod.Manager.ReachableFloor(this.mod.Data)} ({this.mod.Data.Foothold:0.00} exact) | Record: {this.mod.Data.DeepestFloor} | Hour clock: {this.mod.Data.FadeMinutes}/60 min | Fade debt: {this.mod.Data.HourlyFadeRemainder:0.000} | HasSkullKey: {Game1.player.hasSkullKey}",
                LogLevel.Info);
        }

        private bool RequireWorld()
        {
            if (Context.IsWorldReady)
                return true;

            this.mod.Monitor.Log("Load a save first.", LogLevel.Warn);
            return false;
        }
    }
}
