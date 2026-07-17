namespace QisFadingElevator
{
    /// <summary>
    /// The shared repair-scene timeline, in 60ths of a second. ModEntry fires sounds and one-shot
    /// effects on these beats while ElevatorPrototype maps the same beats to body/overlay frames,
    /// so audio and pixels can never drift apart.
    /// </summary>
    internal static class RepairSequence
    {
        /// <summary>Three physical impacts seat the frame and doors.</summary>
        public const int Impact1 = 18;
        public const int Impact2 = 66;
        public const int Impact3 = 114;

        /// <summary>The farmer starts each pickaxe swing just before its matching impact.</summary>
        public const int SwingLeadTicks = 9;

        /// <summary>How long each impact's contact flash stays visible.</summary>
        public const int FlashTicks = 9;

        /// <summary>How long each impact physically jolts the fixture.</summary>
        public const int JoltTicks = 6;

        /// <summary>The third impact swaps the broken body for the seated, repaired one.</summary>
        public const int BodyRepairedAt = Impact3;

        /// <summary>The doors settle plumb in the quiet beat after the impacts.</summary>
        public const int SettleCreak = 138;

        /// <summary>The battery seats in the copper node and the socket first flickers.</summary>
        public const int BatteryAt = 150;

        /// <summary>Violet light climbs the door seam from the threshold to the crown.</summary>
        public const int SeamStart = 162;
        public const int SeamEnd = 234;

        /// <summary>The crown crystal ignites, blooms, and sheds rising motes.</summary>
        public const int FlareSmallAt = SeamEnd;
        public const int FlareBigAt = SeamEnd + 6;
        public const int FlareEnd = SeamEnd + 16;

        public const int TotalTicks = 300;

        public static bool IsImpactBeat(int elapsed, out int impactStart)
        {
            impactStart = elapsed >= Impact3 ? Impact3 : elapsed >= Impact2 ? Impact2 : Impact1;
            return elapsed >= Impact1 && elapsed < BatteryAt;
        }

        /// <summary>Get whether a physical swing begins on this repair tick.</summary>
        public static bool IsSwingStart(int elapsed)
        {
            return elapsed == Impact1 - SwingLeadTicks
                || elapsed == Impact2 - SwingLeadTicks
                || elapsed == Impact3 - SwingLeadTicks;
        }
    }
}
