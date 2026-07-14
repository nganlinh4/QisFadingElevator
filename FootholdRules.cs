using System;

namespace QisFadingElevator
{
    /// <summary>Small shared rules for comparing the fading foothold with its personal record.</summary>
    internal static class FootholdRules
    {
        /// <summary>Whether a newly reached record survived intact until the player surfaced.</summary>
        public static bool ShouldCelebrateRecord(double foothold, int surfacedRecord)
        {
            return surfacedRecord > 0 && foothold >= surfacedRecord;
        }

        /// <summary>The share of the personal record still retained by the shaft.</summary>
        public static double RetainedFraction(double foothold, int record)
        {
            if (record <= 0)
                return 0;

            return Math.Clamp(foothold / record, 0, 1);
        }
    }
}
