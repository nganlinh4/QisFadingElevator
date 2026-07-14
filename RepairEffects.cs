using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace QisFadingElevator
{
    /// <summary>
    /// Sounds and one-shot particles for the repair scene. ElevatorPrototype owns the persistent
    /// fixture frames; everything here spawns, plays, and expires on its own.
    /// </summary>
    internal static class RepairEffects
    {
        private static readonly Color DustTint = new(214, 176, 128);

        /// <summary>Fire the audio/particle beats for one elapsed tick of the repair scene.</summary>
        public static void FireBeats(GameLocation location, int elapsed, Texture2D sprites)
        {
            switch (elapsed)
            {
                case RepairSequence.Impact1:
                    Game1.playSound("clank");
                    SpawnImpactDust(location, new Vector2(-26f, -6f));
                    SpawnDebris(location, wood: true, chunks: 2);
                    break;

                case RepairSequence.Impact2:
                    Game1.playSound("hammer");
                    SpawnImpactDust(location, new Vector2(22f, -34f));
                    SpawnDebris(location, wood: true, chunks: 2);
                    break;

                case RepairSequence.Impact3:
                    Game1.playSound("hammer");
                    SpawnImpactDust(location, new Vector2(-24f, -2f));
                    SpawnImpactDust(location, new Vector2(20f, -2f));
                    SpawnDebris(location, wood: false, chunks: 3);
                    break;

                case RepairSequence.Impact3 + 4:
                    Game1.playSound("clank");
                    break;

                case RepairSequence.SettleCreak:
                    Game1.playSound("openBox");
                    break;

                case RepairSequence.BatteryAt:
                    Game1.playSound("crystal");
                    SpawnBatterySpark(location, sprites);
                    break;

                case RepairSequence.SeamStart:
                    Game1.playSound("wand");
                    break;

                case RepairSequence.FlareSmallAt:
                    Game1.playSound("secret1");
                    break;

                case RepairSequence.FlareBigAt:
                    Game1.screenGlowOnce(new Color(110, 60, 200), false, 0.012f, 0.18f);
                    SpawnBloom(location, sprites);
                    SpawnIgnitionMotes(location, sprites);
                    break;
            }
        }

        /// <summary>A warm vanilla smoke poof rising from the struck point.</summary>
        private static void SpawnImpactDust(GameLocation location, Vector2 offsetFromThreshold)
        {
            if (!ElevatorPrototype.TryGetThresholdPoint(location, out Vector2 threshold))
                return;

            location.TemporarySprites.Add(new TemporaryAnimatedSprite(5, threshold + offsetFromThreshold - new Vector2(32f, 40f), DustTint)
            {
                motion = new Vector2(offsetFromThreshold.X < 0 ? -0.12f : 0.12f, -0.18f),
                interval = 90f,
                scale = 0.75f,
                layerDepth = 1f
            });
        }

        /// <summary>Vanilla chopping/mining chips bouncing off the fixture.</summary>
        private static void SpawnDebris(GameLocation location, bool wood, int chunks)
        {
            if (!ElevatorPrototype.TryGetThresholdPoint(location, out Vector2 threshold))
                return;

            Game1.createRadialDebris(location, wood ? 12 : 14, (int)(threshold.X / 64f), (int)(threshold.Y / 64f), chunks, resource: false);
        }

        /// <summary>A brief blue-white electric flicker where the battery seats into the copper node.</summary>
        private static void SpawnBatterySpark(GameLocation location, Texture2D sprites)
        {
            if (!ElevatorPrototype.TryGetSocketPoint(location, out Vector2 socket))
                return;

            location.TemporarySprites.Add(new TemporaryAnimatedSprite
            {
                texture = sprites,
                sourceRect = SpriteSheet.SparkA,
                sourceRectStartingPos = new Vector2(SpriteSheet.SparkA.X, SpriteSheet.SparkA.Y),
                position = socket - new Vector2(10f, 10f),
                scale = 4f,
                animationLength = 2,
                interval = 45f,
                totalNumberOfLoops = 3,
                alpha = 1f,
                alphaFade = 0.028f,
                drawAboveAlwaysFront = true
            });
        }

        /// <summary>One restrained radial bloom over the crown dial as the crystal ignites.</summary>
        private static void SpawnBloom(GameLocation location, Texture2D sprites)
        {
            if (!ElevatorPrototype.TryGetCrownCenter(location, out Vector2 crown))
                return;

            location.TemporarySprites.Add(new TemporaryAnimatedSprite
            {
                texture = sprites,
                sourceRect = SpriteSheet.RepairBloom,
                sourceRectStartingPos = new Vector2(SpriteSheet.RepairBloom.X, SpriteSheet.RepairBloom.Y),
                position = crown - new Vector2(16.5f * 4f, 7.5f * 4f),
                scale = 4f,
                animationLength = 1,
                interval = 999999f,
                alpha = 0.55f,
                alphaFade = 0.011f,
                drawAboveAlwaysFront = true
            });
        }

        /// <summary>A handful of violet motes drifting up the awakened seam, Junimo-hut style.</summary>
        private static void SpawnIgnitionMotes(GameLocation location, Texture2D sprites)
        {
            if (!ElevatorPrototype.TryGetCrownCenter(location, out Vector2 crown))
                return;

            for (int i = 0; i < 5; i++)
            {
                Rectangle source = i % 2 == 0 ? SpriteSheet.MoteA : SpriteSheet.MoteB;
                location.TemporarySprites.Add(new TemporaryAnimatedSprite
                {
                    texture = sprites,
                    sourceRect = source,
                    sourceRectStartingPos = new Vector2(source.X, source.Y),
                    position = crown + new Vector2((i - 2) * 9f - 6f, 18f + i % 3 * 26f),
                    scale = 4f,
                    animationLength = 1,
                    interval = 999999f,
                    motion = new Vector2(0f, -0.42f - i * 0.05f),
                    acceleration = new Vector2(0f, -0.004f),
                    xPeriodic = true,
                    xPeriodicLoopTime = 1300f + i * 260f,
                    xPeriodicRange = 3f + i,
                    alpha = 0.85f,
                    alphaFade = 0.0075f,
                    delayBeforeAnimationStart = i * 130,
                    drawAboveAlwaysFront = true
                });
            }
        }
    }
}
