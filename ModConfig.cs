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

        /*********
        ** Depth bands — deeper footholds fade faster, mapped to the game's own milestones.
        *********/
        /// <summary>Floors at or below this depth fade slowest (the "approach").</summary>
        public int ShallowBandMaxFloor { get; set; } = 50;

        /// <summary>Floors at or below this depth fade at the mid rate (the "climb"). Above it is Qi's Abyss.</summary>
        public int MidBandMaxFloor { get; set; } = 100;

        /// <summary>Hourly fade for shallow footholds, as a percent of current depth.</summary>
        public double ShallowFadePercentPerHour { get; set; } = 0.125;

        /// <summary>Hourly fade for mid footholds, as a percent of current depth.</summary>
        public double MidFadePercentPerHour { get; set; } = 0.5;

        /// <summary>Hourly fade for deep footholds (Qi's Abyss), as a percent of current depth.</summary>
        public double DeepFadePercentPerHour { get; set; } = 0.9375;

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
