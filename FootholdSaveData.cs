namespace QisFadingElevator
{
    /// <summary>Per-save persisted state for the fading foothold.</summary>
    public sealed class FootholdSaveData
    {
        /// <summary>Save schema version. Zero identifies data written before the repair system existed.</summary>
        public int DataVersion { get; set; }

        /// <summary>Whether the entrance mechanism has been restored.</summary>
        public bool IsRepaired { get; set; }

        /// <summary>The all-time deepest Skull Cavern floor reached (the "ghost line" to chase).</summary>
        public int DeepestFloor { get; set; }

        /// <summary>The current foothold the elevator can reach. Kept fractional internally for smooth fade.</summary>
        public double Foothold { get; set; }

        /// <summary>
        /// Playable in-game minutes carried toward the next hourly fade. This persists so saving or
        /// returning to the title can't reset the clock.
        /// </summary>
        public int FadeMinutes { get; set; }

        /// <summary>
        /// Fractional hourly loss carried until it becomes a whole floor. Keeping this separate prevents
        /// an infinitesimal percentage from making an integer foothold appear to lose a full floor early.
        /// </summary>
        public double HourlyFadeRemainder { get; set; }
    }
}
