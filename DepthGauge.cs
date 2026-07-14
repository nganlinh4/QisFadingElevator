using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace QisFadingElevator
{
    /// <summary>
    /// Compact, modular HUD prototype. Its body is assembled from repeated equal-height segments so the final
    /// sprite sheet can provide one top cap, one tileable middle, one bottom cap, and one marker.
    /// </summary>
    public sealed class DepthGauge
    {
        /*********
        ** Fields
        *********/
        private static readonly Color QiGlow = new(220, 173, 255);
        private static readonly Color DamageGlow = new(255, 180, 94);
        private static readonly Color SandGlow = new(230, 198, 145);
        private static readonly Point[] SparkDirections =
        {
            new(-1, -1),
            new(1, -1),
            new(-1, 1),
            new(1, 1)
        };

        private readonly Texture2D sprites;
        private float displayed;
        private double target;
        private int foothold;
        private int record;
        private float flash;
        private float decayEffect;
        private float idlePhase;
        private int displayedLoss;
        private bool damageEffect;
        private bool initialized;

        public DepthGauge(Texture2D sprites)
        {
            this.sprites = sprites;
        }

        /*********
        ** Public methods
        *********/
        /// <summary>Set the current foothold and all-time record the gauge should reflect.</summary>
        public void SetValues(double foothold, int record)
        {
            this.target = Math.Max(0, foothold);
            this.foothold = Math.Max(0, (int)Math.Floor(this.target));
            this.record = Math.Max(0, record);
            if (!this.initialized)
            {
                this.displayed = (float)this.target;
                this.initialized = true;
            }
        }

        /// <summary>Flash the gauge (called on a fade or a new record).</summary>
        public void Pulse()
        {
            this.flash = 1f;
            this.decayEffect = Math.Max(this.decayEffect, 0.55f);
            this.displayedLoss = 0;
            this.damageEffect = false;
        }

        /// <summary>Animate a live fade. Damage uses a warmer recoil; clock decay stays Qi-purple.</summary>
        public void PulseDecay(int floorsLost, bool fromDamage)
        {
            this.flash = 1f;
            this.decayEffect = 1f;
            this.displayedLoss = Math.Max(0, floorsLost);
            this.damageEffect = fromDamage;
        }

        /// <summary>Advance the animation one tick.</summary>
        public void Update()
        {
            this.displayed += ((float)this.target - this.displayed) * 0.14f;
            if (Math.Abs(this.displayed - this.target) < 0.025f)
                this.displayed = (float)this.target;
            if (this.flash > 0f)
                this.flash = Math.Max(0f, this.flash - 0.025f);
            if (this.decayEffect > 0f)
                this.decayEffect = Math.Max(0f, this.decayEffect - 0.02f);
            this.idlePhase = (this.idlePhase + 0.035f) % MathHelper.TwoPi;
        }

        /// <summary>Draw the gauge to the HUD.</summary>
        public void Draw(SpriteBatch b)
        {
            const int scale = 3;
            const int capWidth = 8 * scale;
            const int segmentHeight = 2 * scale;
            const int segmentCount = 24;
            const int bodyHeight = segmentHeight * segmentCount;
            const int capHeight = 4 * scale;
            int x = 32;
            // Keep the complete instrument below the vanilla mine-floor badge, but above the
            // center-left lane where item pickup notices accumulate. On short viewports the icon
            // begins at 112px; larger viewports place the gauge around the upper quarter.
            const int headerClearance = 39;
            const int safeTop = 112;
            int y = Math.Max(safeTop + headerClearance, Game1.uiViewport.Height / 4 - bodyHeight / 2);

            int FloorToY(double floor)
            {
                return y + (int)Math.Round(bodyHeight * FootholdRules.RetainedFraction(floor, this.record));
            }

            float breath = (MathF.Sin(this.idlePhase) + 1f) * 0.5f;
            Color railTint = Color.Lerp(Color.White, QiGlow, 0.035f + breath * 0.055f);

            // One cap, a genuinely repeated middle segment, and one bottom cap.
            b.Draw(this.sprites, new Rectangle(x, y - capHeight, capWidth, capHeight), SpriteSheet.GaugeTop, railTint);
            for (int i = 0; i < segmentCount; i++)
                b.Draw(this.sprites, new Rectangle(x, y + i * segmentHeight, capWidth, segmentHeight), SpriteSheet.GaugeMiddle, railTint);
            b.Draw(this.sprites, new Rectangle(x, y + bodyHeight, capWidth, capHeight), SpriteSheet.GaugeBottom, railTint);

            // Three warm calibration cuts keep the modular rail legible without adding a bulky frame.
            Color tickTint = Color.Lerp(SandGlow, QiGlow, 0.12f + breath * 0.08f) * 0.72f;
            for (int tick = 1; tick <= 3; tick++)
            {
                int tickY = y + bodyHeight * tick / 4;
                int tickWidth = tick == 2 ? 5 : 3;
                b.Draw(Game1.staminaRect, new Rectangle(x - tickWidth, tickY, tickWidth + 1, 2), tickTint);
                b.Draw(Game1.staminaRect, new Rectangle(x + capWidth - 1, tickY, tickWidth + 1, 2), tickTint);
            }

            // Tile the fill swatch inside the shaft; partial segments remain aligned to native pixels.
            int fillBottom = FloorToY(this.displayed);
            fillBottom = y + Math.Max(0, (fillBottom - y) / scale * scale);
            Color effectColor = this.damageEffect ? DamageGlow : QiGlow;
            Color restingFill = Color.Lerp(Color.White, QiGlow, 0.12f + breath * 0.12f);
            Color fillTint = Color.Lerp(restingFill, effectColor, this.flash * 0.8f);
            for (int fillY = y; fillY < fillBottom; fillY += segmentHeight)
            {
                int drawHeight = Math.Min(segmentHeight, fillBottom - fillY);
                int sourceHeight = Math.Max(1, drawHeight / scale);
                Rectangle fillSource = new(SpriteSheet.GaugeFill.X, SpriteSheet.GaugeFill.Y, SpriteSheet.GaugeFill.Width, sourceHeight);
                b.Draw(this.sprites, new Rectangle(x + 2 * scale, fillY, 4 * scale, drawHeight), fillSource, fillTint);
            }

            float effectProgress = 1f - this.decayEffect;
            int kickX = (int)MathF.Round(MathF.Sin(effectProgress * 24f) * this.decayEffect) * scale;
            Rectangle markerDest = new(x - scale + kickX, fillBottom - SpriteSheet.GaugeMarker.Height * scale / 2, SpriteSheet.GaugeMarker.Width * scale, SpriteSheet.GaugeMarker.Height * scale);
            b.Draw(this.sprites, markerDest, SpriteSheet.GaugeMarker, fillTint);

            // The glyph names the instrument. The far end of the rail is the personal record now, so a
            // second marker there would only collide with the live handle whenever the memory is full.
            Color iconTint = Color.Lerp(Color.White, QiGlow, 0.08f + breath * 0.12f + this.flash * 0.25f);
            int headerY = y - capHeight - 9 * scale;
            b.Draw(this.sprites, new Rectangle(x, headerY, 8 * scale, 8 * scale), SpriteSheet.GaugeIcon, iconTint);

            // A shadow and short color pop make the number readable without making the compact gauge wider.
            int numberX = x + capWidth + 8;
            int numberY = fillBottom - 8;
            Color restingNumber = Color.Lerp(SandGlow, QiGlow, 0.62f + breath * 0.1f);
            Color numberColor = Color.Lerp(restingNumber, effectColor, this.decayEffect * 0.9f);
            Utility.drawTinyDigits(this.foothold, b, new Vector2(numberX + 2, numberY + 2), 2f, 0.55f, Color.Black);
            Utility.drawTinyDigits(this.foothold, b, new Vector2(numberX, numberY), 2f, 0.98f, numberColor);

            // Four deterministic native-pixel motes expand from the handle, then disappear quickly.
            if (this.decayEffect > 0.02f)
            {
                int travel = 3 + (int)(effectProgress * 11f);
                int centerX = markerDest.Center.X;
                int centerY = markerDest.Center.Y;
                foreach (Point direction in SparkDirections)
                {
                    int sparkX = centerX + direction.X * travel;
                    int sparkY = centerY + direction.Y * (2 + travel / 2);
                    b.Draw(Game1.staminaRect, new Rectangle(sparkX, sparkY, 2, 2), effectColor * (this.decayEffect * 0.75f));
                }
            }

            // The discrete floor loss rises beside the current value; fractional clock pulses omit a fake number.
            if (this.displayedLoss > 0 && this.decayEffect > 0.02f)
            {
                float lossAlpha = Math.Min(1f, this.decayEffect * 1.6f);
                int rise = (int)(effectProgress * 10f);
                int lossX = numberX + 38;
                int lossY = numberY - rise;
                b.Draw(Game1.staminaRect, new Rectangle(lossX + 1, lossY + 5, 5, 2), Color.Black * (lossAlpha * 0.5f));
                b.Draw(Game1.staminaRect, new Rectangle(lossX, lossY + 4, 5, 2), effectColor * lossAlpha);
                Utility.drawTinyDigits(this.displayedLoss, b, new Vector2(lossX + 8, lossY), 1f, lossAlpha, effectColor);
            }
        }

    }
}
