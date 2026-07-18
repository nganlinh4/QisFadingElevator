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
        private const double MinimumFoothold = 0;

        private readonly ModConfig config;

        public FootholdManager(ModConfig config)
        {
            this.config = config;
        }

        /// <summary>The floor the elevator can currently reach.</summary>
        public int ReachableFloor(FootholdSaveData data)
        {
            return data.Foothold <= MinimumFoothold
                ? 0
                : Math.Max(0, (int)Math.Ceiling(data.Foothold - 0.0000001));
        }

        /// <summary>Whether the current foothold can fade as game time passes.</summary>
        public bool WouldHourlyFade(FootholdSaveData data)
        {
            return data.Foothold > MinimumFoothold && this.FadeRateFor(data.Foothold) > 0;
        }

        /// <summary>
        /// Where the memory truly stands right now: the stored foothold minus the fractional fade
        /// debt already charged, minus this hour's bite accrued minute by minute. Continuous across
        /// hourly pulses, so the gauge can bleed in realtime while whole floors keep their own event.
        /// </summary>
        public double PreviewExactFoothold(FootholdSaveData data, double dailyLuck)
        {
            return Math.Max(MinimumFoothold, data.Foothold);
        }

        /// <summary>Record that the player reached a Skull Cavern floor today.</summary>
        /// <returns>True if this was a new personal record.</returns>
        public bool RecordDive(FootholdSaveData data, int floor)
        {
            bool isRecord = floor > data.DeepestFloor;
            if (isRecord)
                data.DeepestFloor = floor;

            // Reaching a floor renews the foothold up to that depth. A renewed memory starts
            // whole: physically re-reaching depth forgives the sub-floor debt already chewed
            // from the old one. The live fade clock itself never resets.
            if (floor > data.Foothold)
                data.Foothold = floor;

            return isRecord;
        }

        /// <summary>
        /// Continuously erode the exact foothold for elapsed playable game time. Exponential
        /// retention makes the configured percentage frame-rate independent and still means that
        /// exactly five percent of the then-current memory is gone after one neutral-luck hour.
        /// Below floor one, continue at floor one's absolute rate so the last foothold can truly
        /// fade to zero instead of approaching it forever.
        /// </summary>
        public FadeResult ApplyTimeFade(FootholdSaveData data, double dailyLuck, double elapsedMinutes)
        {
            var result = this.Snapshot(data);
            if (data.Foothold <= MinimumFoothold || elapsedMinutes <= 0)
                return result;

            double rate = this.FadeRateFor(data.Foothold) / 100.0;
            double luckFactor = Math.Clamp(1.0 - dailyLuck * this.config.LuckInfluence * 2.0, 0.5, 1.5);
            double adjustedRate = Math.Clamp(rate * luckFactor, 0.0, 0.999999);
            if (adjustedRate <= 0)
                return result;

            double elapsedHours = elapsedMinutes / 60.0;
            if (data.Foothold <= 1.0)
            {
                data.Foothold = Math.Max(MinimumFoothold, data.Foothold - adjustedRate * elapsedHours);
            }
            else
            {
                double retained = Math.Pow(1.0 - adjustedRate, elapsedHours);
                double exponentiallyFaded = data.Foothold * retained;
                if (exponentiallyFaded >= 1.0)
                {
                    data.Foothold = exponentiallyFaded;
                }
                else
                {
                    double hoursToFloorOne = Math.Log(1.0 / data.Foothold) / Math.Log(1.0 - adjustedRate);
                    double hoursBelowOne = Math.Max(0, elapsedHours - hoursToFloorOne);
                    data.Foothold = Math.Max(MinimumFoothold, 1.0 - adjustedRate * hoursBelowOne);
                }
            }

            result.After = this.ReachableFloor(data);
            result.ExactAfter = data.Foothold;
            result.Applied = result.ExactAfter < result.ExactBefore;
            return result;
        }

        /// <summary>Immediately remove floors based on a Skull Cavern hit's source and severity.</summary>
        public FadeResult ApplyDamageFade(FootholdSaveData data, int floors)
        {
            var result = this.Snapshot(data);
            if (data.Foothold <= MinimumFoothold || floors <= 0)
                return result;

            data.Foothold = Math.Max(MinimumFoothold, data.Foothold - floors);
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

        /// <summary>The continuous hourly fade percent at every depth.</summary>
        private double FadeRateFor(double floor)
        {
            return this.config.FadePercentPerHour;
        }
    }
}
