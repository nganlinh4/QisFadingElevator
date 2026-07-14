using System;

namespace QisFadingElevator
{
    /// <summary>The outcome of applying one source of fade.</summary>
    internal struct FadeResult
    {
        public int Before;
        public int After;
        public double ExactBefore;
        public double ExactAfter;
        public bool Applied;

        public bool Changed => this.Before != this.After;

        public int LostFloors => Math.Max(0, this.Before - this.After);
    }

    /// <summary>Owns the fade math and dive recording. Pure logic — no game calls, so it's testable.</summary>
    internal sealed class FootholdManager
    {
        private readonly ModConfig config;

        public FootholdManager(ModConfig config)
        {
            this.config = config;
        }

        /// <summary>The floor the elevator can currently reach.</summary>
        public int ReachableFloor(FootholdSaveData data)
        {
            return Math.Max(this.config.MinFoothold, (int)Math.Floor(data.Foothold));
        }

        /// <summary>Whether the current depth band has an hourly fade to apply.</summary>
        public bool WouldHourlyFade(FootholdSaveData data)
        {
            return data.Foothold > this.config.MinFoothold && this.FadeRateFor(data.Foothold) > 0;
        }

        /// <summary>Record that the player reached a Skull Cavern floor today.</summary>
        /// <returns>True if this was a new personal record.</returns>
        public bool RecordDive(FootholdSaveData data, int floor)
        {
            bool isRecord = floor > data.DeepestFloor;
            if (isRecord)
                data.DeepestFloor = floor;

            // Reaching a floor renews the foothold up to that depth.
            if (floor > data.Foothold)
                data.Foothold = floor;

            return isRecord;
        }

        /// <summary>Apply one playable in-game hour of fade, even while the player is actively caving.</summary>
        public FadeResult ApplyHourlyFade(FootholdSaveData data, double dailyLuck)
        {
            var result = this.Snapshot(data);
            if (data.Foothold <= this.config.MinFoothold)
                return result;

            double rate = this.FadeRateFor(data.Foothold) / 100.0;
            double luckFactor = Math.Clamp(1.0 - dailyLuck * this.config.LuckInfluence * 2.0, 0.5, 1.5);
            double faded = data.Foothold * rate * luckFactor;

            if (faded <= 0)
                return result;

            data.HourlyFadeRemainder += faded;
            int wholeFloors = (int)Math.Floor(data.HourlyFadeRemainder + 0.0000001);
            if (wholeFloors > 0)
            {
                data.Foothold = Math.Max(this.config.MinFoothold, data.Foothold - wholeFloors);
                data.HourlyFadeRemainder -= wholeFloors;
                if (data.Foothold <= this.config.MinFoothold)
                    data.HourlyFadeRemainder = 0;
            }

            result.After = this.ReachableFloor(data);
            result.ExactAfter = data.Foothold;
            result.Applied = true;
            return result;
        }

        /// <summary>Immediately remove floors based on a Skull Cavern hit's source and severity.</summary>
        public FadeResult ApplyDamageFade(FootholdSaveData data, int floors)
        {
            var result = this.Snapshot(data);
            if (data.Foothold <= this.config.MinFoothold || floors <= 0)
                return result;

            data.Foothold = Math.Max(this.config.MinFoothold, data.Foothold - floors);
            result.After = this.ReachableFloor(data);
            result.ExactAfter = data.Foothold;
            result.Applied = result.ExactAfter < result.ExactBefore;
            return result;
        }

        private FadeResult Snapshot(FootholdSaveData data)
        {
            int reachable = this.ReachableFloor(data);
            return new FadeResult
            {
                Before = reachable,
                After = reachable,
                ExactBefore = data.Foothold,
                ExactAfter = data.Foothold
            };
        }

        /// <summary>The hourly fade percent for a foothold at the given depth.</summary>
        private double FadeRateFor(double floor)
        {
            if (floor <= this.config.ShallowBandMaxFloor)
                return this.config.ShallowFadePercentPerHour;
            if (floor <= this.config.MidBandMaxFloor)
                return this.config.MidFadePercentPerHour;
            return this.config.DeepFadePercentPerHour;
        }
    }
}
