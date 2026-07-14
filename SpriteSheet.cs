using Microsoft.Xna.Framework;

namespace QisFadingElevator
{
    /// <summary>Native-pixel source regions in assets/qfe-sprites.png (see tools/build_pixel_art.py).</summary>
    internal static class SpriteSheet
    {
        // Row 0: lift sprites. The mine machine keeps true material colors; its separate grey
        // shroud collar is multiplied by the sampled wall tint at runtime.
        public static readonly Rectangle MineBody = new(0, 0, 17, 32);
        public static readonly Rectangle MineShroud = new(17, 0, 17, 32);
        public static readonly Rectangle MineGlowIdle = new(34, 0, 17, 32);
        public static readonly Rectangle MineGlowActive = new(51, 0, 17, 32);
        public static readonly Rectangle FoyerBody = new(68, 0, 17, 32);
        public static readonly Rectangle FoyerGlowIdle = new(85, 0, 17, 32);
        public static readonly Rectangle FoyerGlowActive = new(102, 0, 17, 32);
        public static readonly Rectangle FoyerBroken = new(119, 0, 17, 32);
        public static readonly Rectangle FoyerBrokenGlow = new(136, 0, 17, 32);

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
        public static readonly Rectangle GaugeTop = new(0, 64, 12, 6);
        public static readonly Rectangle GaugeMiddle = new(0, 70, 12, 2);
        public static readonly Rectangle GaugeBottom = new(0, 72, 12, 6);
        public static readonly Rectangle GaugeMarker = new(16, 64, 14, 5);
        public static readonly Rectangle GaugeIcon = new(16, 70, 10, 10);
        public static readonly Rectangle GaugeFill = new(32, 64, 4, 2);
        public static readonly Rectangle GaugeEmber = new(40, 64, 3, 3);
    }
}
