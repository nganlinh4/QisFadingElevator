using Microsoft.Xna.Framework;

namespace QisFadingElevator
{
    /// <summary>Native-pixel source regions in assets/qfe-sprites.png.</summary>
    internal static class SpriteSheet
    {
        public static readonly Rectangle ElevatorIdle = new(0, 0, 17, 32);
        public static readonly Rectangle ElevatorActive = new(17, 0, 17, 32);
        public static readonly Rectangle ElevatorGlowIdle = new(34, 0, 17, 32);
        public static readonly Rectangle ElevatorGlowActive = new(51, 0, 17, 32);
        public static readonly Rectangle GaugeTop = new(0, 32, 8, 4);
        public static readonly Rectangle GaugeMiddle = new(0, 36, 8, 2);
        public static readonly Rectangle GaugeBottom = new(0, 38, 8, 4);
        public static readonly Rectangle GaugeMarker = new(8, 32, 10, 5);
        public static readonly Rectangle GaugeRecord = new(8, 37, 10, 3);
        public static readonly Rectangle GaugeIcon = new(18, 32, 8, 8);
        public static readonly Rectangle GaugeFill = new(26, 32, 4, 2);
        public static readonly Rectangle FoyerElevator = new(30, 32, 17, 32);
        public static readonly Rectangle FoyerGlowIdle = new(47, 32, 17, 32);
        public static readonly Rectangle FoyerGlowActive = new(64, 32, 17, 32);
        public static readonly Rectangle FoyerBroken = new(81, 32, 17, 32);
        public static readonly Rectangle FoyerBrokenGlow = new(85, 0, 17, 32);
        public static readonly Rectangle RepairImpactA = new(0, 64, 17, 32);
        public static readonly Rectangle RepairImpactB = new(17, 64, 17, 32);
        public static readonly Rectangle RepairAwaken = new(34, 64, 17, 32);
    }
}
