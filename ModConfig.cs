namespace QisFadingElevator
{
    /// <summary>The player-configurable options (also drives the in-game GMCM menu).</summary>
    public sealed class ModConfig
    {
        /// <summary>Master switch.</summary>
        public bool Enabled { get; set; } = true;

        /*********
        ** Elevator
        *********/
        /// <summary>Spacing between selectable floors in the elevator menu (the exact foothold is always offered too).</summary>
        public int FloorInterval { get; set; } = 5;

        /// <summary>
        /// Hourly fade as a percent of the current foothold. Five percent means floor 10 loses one
        /// floor in two neutral-luck hours, while floor 100 loses one in twelve minutes.
        /// </summary>
        public double FadePercentPerHour { get; set; } = 5.0;

        /*********
        ** Softeners
        *********/
        /// <summary>The foothold never fades below this floor (a permanent toehold).</summary>
        public int MinFoothold { get; set; } = 0;

        /// <summary>How strongly daily luck slows (good) or speeds (bad) each hourly fade. 0 disables the luck link.</summary>
        public double LuckInfluence { get; set; } = 1.0;

        /// <summary>Show rare story notices for restoration, records, and direct interaction feedback.</summary>
        public bool ShowToasts { get; set; } = true;

        /// <summary>Show the depth gauge on the HUD while near the Skull Cavern.</summary>
        public bool ShowDepthGauge { get; set; } = true;

    }
}
