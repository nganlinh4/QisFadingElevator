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

        /// <summary>Legacy pre-v2 hourly clock. Kept only so old saves migrate cleanly.</summary>
        public int FadeMinutes { get; set; }

        /// <summary>Legacy pre-v2 fractional debt. Folded into Foothold during migration.</summary>
        public double HourlyFadeRemainder { get; set; }

        /// <summary>Minutes from the last bedtime until 6:00 AM, waiting to be faded next morning.</summary>
        public int PendingSleepMinutes { get; set; }
    }
}
