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
        /// Continuous fade per in-game hour as a percent of the current foothold. Five percent means
        /// floor 10 loses one floor in roughly two neutral-luck hours, while floor 100 loses five per hour.
        /// </summary>
        public double FadePercentPerHour { get; set; } = 5.0;

        /// <summary>How strongly daily luck slows (good) or speeds (bad) the continuous fade. 0 disables the luck link.</summary>
        public double LuckInfluence { get; set; } = 1.0;
    }
}
