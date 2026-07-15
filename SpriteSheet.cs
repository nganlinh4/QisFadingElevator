using Microsoft.Xna.Framework;

namespace QisFadingElevator
{
    /// <summary>Native-pixel source regions in assets/qfe-sprites.png (see tools/build_pixel_art.py).</summary>
    internal static class SpriteSheet
    {
        // Row 0: lift sprites. The shell is a near-white relief multiplied by the sampled wall
        // color at runtime; the accents sprite holds absolute interior darkness and identity
        // details and draws untinted above it.
        public static readonly Rectangle LiftShell = new(0, 0, 17, 32);
        public static readonly Rectangle LiftAccents = new(17, 0, 17, 32);
        public static readonly Rectangle GlowIdle = new(34, 0, 17, 32);
        public static readonly Rectangle GlowActive = new(51, 0, 17, 32);
        public static readonly Rectangle BrokenShell = new(68, 0, 17, 32);
        public static readonly Rectangle BrokenAccents = new(85, 0, 17, 32);
        public static readonly Rectangle BrokenGlow = new(102, 0, 17, 32);

        // Row 1: repair-sequence frames.
        public static readonly Rectangle RepairFlashA = new(0, 32, 17, 32);
        public static readonly Rectangle RepairFlashB = new(17, 32, 17, 32);
        public static readonly Rectangle RepairFlashC = new(34, 32, 17, 32);
        public static readonly Rectangle RepairSeam = new(51, 32, 17, 32);
        public static readonly Rectangle RepairFlareA = new(68, 32, 17, 32);
        public static readonly Rectangle RepairFlareB = new(85, 32, 17, 32);
        public static readonly Rectangle RepairBloom = new(102, 32, 33, 32);
        public static readonly Rectangle SparkA = new(136, 32, 5, 5);
        public static readonly Rectangle SparkB = new(141, 32, 5, 5);
        public static readonly Rectangle MoteA = new(148, 32, 3, 3);
        public static readonly Rectangle MoteB = new(151, 32, 3, 3);

        // Row 2: depth gauge.
        public static readonly Rectangle GaugeTop = new(0, 64, 10, 5);
        public static readonly Rectangle GaugeMiddle = new(0, 69, 10, 2);
        public static readonly Rectangle GaugeBottom = new(0, 71, 10, 5);
        public static readonly Rectangle GaugeMarker = new(12, 64, 12, 5);
        public static readonly Rectangle GaugeIcon = new(12, 70, 8, 8);
        public static readonly Rectangle GaugeFill = new(26, 64, 4, 2);
        public static readonly Rectangle GaugeEmber = new(32, 64, 3, 3);
    }
}
