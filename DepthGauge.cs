using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace QisFadingElevator
{
    /// <summary>
    /// Compact carved-instrument HUD: a sandstone rail with a dark channel, luminous Qi fill, and a
    /// bronze clamp marker. Assembled from one top cap, one tileable middle, one bottom cap.
    /// </summary>
    public sealed class DepthGauge
    {
        /*********
        ** Fields
        *********/
        private static readonly Color QiGlow = new(220, 173, 255);
        private static readonly Color DamageGlow = new(255, 180, 94);
        private static readonly Color SandGlow = new(230, 198, 145);
        private static readonly Color ResidueViolet = new(88, 52, 140);
        private static readonly Color EtchShadow = new(30, 18, 24);

        private const int Scale = 3;
        private const int SegmentCount = 24;
        private const int EmberCount = 3;

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
            int capWidth = SpriteSheet.GaugeTop.Width * Scale;
            int capHeight = SpriteSheet.GaugeTop.Height * Scale;
            int segmentHeight = SpriteSheet.GaugeMiddle.Height * Scale;
            int bodyHeight = segmentHeight * SegmentCount;
            int x = 32;
            // Keep the complete instrument below the vanilla mine-floor badge, but above the
            // center-left lane where item pickup notices accumulate. On short viewports the icon
            // begins at 112px; larger viewports place the gauge around the upper quarter.
            const int headerClearance = 39;
            const int safeTop = 112;
            int y = Math.Max(safeTop + headerClearance, Game1.uiViewport.Height / 4 - bodyHeight / 2);

            float breath = (MathF.Sin(this.idlePhase) + 1f) * 0.5f;
            Color railTint = Color.Lerp(Color.White, QiGlow, 0.035f + breath * 0.055f);

            // One cap, a genuinely repeated middle segment, and one bottom cap.
            b.Draw(this.sprites, new Rectangle(x, y - capHeight, capWidth, capHeight), SpriteSheet.GaugeTop, railTint);
            for (int i = 0; i < SegmentCount; i++)
                b.Draw(this.sprites, new Rectangle(x, y + i * segmentHeight, capWidth, segmentHeight), SpriteSheet.GaugeMiddle, railTint);
            b.Draw(this.sprites, new Rectangle(x, y + bodyHeight, capWidth, capHeight), SpriteSheet.GaugeBottom, railTint);

            // Calibration notches etched into the stone rather than protruding from it.
            Color etch = EtchShadow * (0.5f + breath * 0.1f);
            for (int tick = 1; tick <= 3; tick++)
            {
                int tickY = y + bodyHeight * tick / 4;
                b.Draw(Game1.staminaRect, new Rectangle(x + Scale, tickY, 2 * Scale, 2), etch);
                b.Draw(Game1.staminaRect, new Rectangle(x + capWidth - 3 * Scale, tickY, 2 * Scale, 2), etch);
            }

            int channelX = x + 4 * Scale;
            int channelWidth = 4 * Scale;

            // Tile the fill weave inside the channel; partial segments stay aligned to native pixels.
            int fillBottom = this.FloorToY(y, bodyHeight, this.displayed);
            fillBottom = y + Math.Max(0, (fillBottom - y) / Scale * Scale);
            Color effectColor = this.damageEffect ? DamageGlow : QiGlow;
            Color restingFill = Color.Lerp(Color.White, QiGlow, 0.12f + breath * 0.12f);
            Color fillTint = Color.Lerp(restingFill, effectColor, this.flash * 0.8f);
            for (int fillY = y; fillY < fillBottom; fillY += segmentHeight)
            {
                int drawHeight = Math.Min(segmentHeight, fillBottom - fillY);
                int sourceHeight = Math.Max(1, drawHeight / Scale);
                Rectangle fillSource = new(SpriteSheet.GaugeFill.X, SpriteSheet.GaugeFill.Y, SpriteSheet.GaugeFill.Width, sourceHeight);
                b.Draw(this.sprites, new Rectangle(channelX, fillY, channelWidth, drawHeight), fillSource, fillTint);
            }

            // Below the live fill, the channel keeps a faint violet residue: the memory that faded.
            if (fillBottom < y + bodyHeight)
                b.Draw(Game1.staminaRect, new Rectangle(channelX, fillBottom, channelWidth, y + bodyHeight - fillBottom), ResidueViolet * (0.16f + breath * 0.05f));

            // The clamp marker recoils sideways on damage and springs vertically as a loss lands.
            float effectProgress = 1f - this.decayEffect;
            int kickX = (int)MathF.Round(MathF.Sin(effectProgress * 24f) * this.decayEffect) * Scale;
            int springY = (int)MathF.Round(MathF.Sin(Math.Min(1f, effectProgress * 2.2f) * MathF.PI) * 1.5f * Scale * this.decayEffect);
            Rectangle markerDest = new(
                x - Scale + kickX,
                fillBottom + springY - SpriteSheet.GaugeMarker.Height * Scale / 2,
                SpriteSheet.GaugeMarker.Width * Scale,
                SpriteSheet.GaugeMarker.Height * Scale);
            b.Draw(this.sprites, markerDest, SpriteSheet.GaugeMarker, fillTint);

            // The glyph names the instrument. The far end of the rail is the personal record, so a
            // second marker there would only collide with the live handle whenever the memory is full.
            Color iconTint = Color.Lerp(Color.White, QiGlow, 0.08f + breath * 0.12f + this.flash * 0.25f);
            int iconSize = SpriteSheet.GaugeIcon.Width * Scale;
            int headerY = y - capHeight - iconSize - Scale;
            b.Draw(this.sprites, new Rectangle(x + (capWidth - iconSize) / 2, headerY, iconSize, iconSize), SpriteSheet.GaugeIcon, iconTint);

            // A shadow and short color pop make the number readable without making the compact gauge wider.
            int numberX = x + capWidth + 8;
            int numberY = fillBottom - 8;
            Color restingNumber = Color.Lerp(SandGlow, QiGlow, 0.62f + breath * 0.1f);
            Color numberColor = Color.Lerp(restingNumber, effectColor, this.decayEffect * 0.9f);
            Utility.drawTinyDigits(this.foothold, b, new Vector2(numberX + 2, numberY + 2), 2f, 0.55f, Color.Black);
            Utility.drawTinyDigits(this.foothold, b, new Vector2(numberX, numberY), 2f, 0.98f, numberColor);

            // Ember chips shake loose from the clamp and fall into the residue when floors are lost.
            if (this.displayedLoss > 0 && this.decayEffect > 0.05f)
            {
                int emberSize = SpriteSheet.GaugeEmber.Width * Scale;
                for (int i = 0; i < EmberCount; i++)
                {
                    float phase = i * 2.1f;
                    int emberX = markerDest.Center.X - emberSize / 2 + (i - 1) * 7
                        + (int)MathF.Round(MathF.Sin(phase + effectProgress * 5f) * 2f);
                    int emberY = markerDest.Center.Y + 4 + (int)(effectProgress * effectProgress * (26f + i * 7f));
                    b.Draw(
                        this.sprites,
                        new Rectangle(emberX, emberY, emberSize, emberSize),
                        SpriteSheet.GaugeEmber,
                        effectColor * (this.decayEffect * 0.9f));
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

        private int FloorToY(int y, int bodyHeight, double floor)
        {
            return y + (int)Math.Round(bodyHeight * FootholdRules.RetainedFraction(floor, this.record));
        }
    }
}
