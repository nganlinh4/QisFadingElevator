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
        private const int DecayStepTicks = 6;

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
        private bool steppingDecay;
        private int decayStepTimer;
        private int pendingFoothold;
        private double pendingExact;

        public DepthGauge(Texture2D sprites)
        {
            this.sprites = sprites;
        }

        /*********
        ** Public methods
        *********/
        /// <summary>
        /// Set what the gauge reflects. The fill and marker track the exact live foothold (including
        /// fractional fade accruing in realtime); the number shows the whole floors the mechanics
        /// still honor, so it only moves when the rounded floor is truly collected.
        /// </summary>
        public void SetValues(double exactFoothold, int reachableFoothold, int record)
        {
            double nextExact = Math.Max(0, exactFoothold);
            int nextFoothold = Math.Max(0, reachableFoothold);
            this.record = Math.Max(0, record);
            if (!this.initialized)
            {
                this.target = nextExact;
                this.foothold = nextFoothold;
                this.pendingExact = nextExact;
                this.pendingFoothold = nextFoothold;
                this.displayed = (float)nextExact;
                this.initialized = true;
                return;
            }

            // Mechanics settle immediately, but a decay already announced by PulseDecay is allowed
            // to count through every lost floor. Reaching deeper ground cancels that old countdown:
            // renewal should feel instant and triumphant rather than look like reverse decay.
            if (this.steppingDecay && nextFoothold <= this.foothold)
            {
                this.pendingExact = nextExact;
                this.pendingFoothold = nextFoothold;
                return;
            }

            this.steppingDecay = false;
            this.target = nextExact;
            this.foothold = nextFoothold;
            this.pendingExact = nextExact;
            this.pendingFoothold = nextFoothold;
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
            this.displayedLoss = 0;
            this.damageEffect = fromDamage;
            if (floorsLost > 0 && this.initialized)
            {
                this.steppingDecay = true;
                this.decayStepTimer = 0;
            }
        }

        /// <summary>Advance the animation one tick.</summary>
        public void Update()
        {
            if (this.steppingDecay)
            {
                if (this.foothold <= this.pendingFoothold)
                {
                    this.steppingDecay = false;
                    this.foothold = this.pendingFoothold;
                    this.target = this.pendingExact;
                }
                else if (this.decayStepTimer-- <= 0)
                {
                    this.foothold--;
                    this.target = this.foothold > this.pendingFoothold
                        ? this.foothold
                        : this.pendingExact;
                    this.decayStepTimer = DecayStepTicks;

                    // Each floor gets its own visible beat: number, clamp recoil, color flash, and
                    // a single ember loss instead of one aggregate jump.
                    this.flash = 1f;
                    this.decayEffect = 1f;
                    this.displayedLoss = 1;
                }
            }

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
            // Center the rail under the vanilla mine-floor badge (box spans x=4..47 on screen).
            const int badgeCenterX = 25;
            int x = badgeCenterX - capWidth / 2;
            // Keep the complete instrument below the vanilla mine-floor badge, but above the
            // center-left lane where item pickup notices accumulate. On short viewports the icon
            // begins at 112px; larger viewports place the gauge around the upper quarter.
            const int headerClearance = 39;
            const int safeTop = 112;
            int y = Math.Max(safeTop + headerClearance, Game1.uiViewport.Height / 4 - bodyHeight / 2);

            float breath = (MathF.Sin(this.idlePhase) + 1f) * 0.5f;
            Color railTint = Color.Lerp(Color.White, QiGlow, 0.035f + breath * 0.055f);

            // One cap, a genuinely repeated middle segment, and one bottom cap. When the mechanics
            // still honor the full remembered depth, the anchor warms gold in quiet reward.
            bool atRecord = this.record > 0 && this.foothold >= this.record;
            Color anchorTint = atRecord
                ? Color.Lerp(railTint, new Color(255, 208, 120), 0.3f + breath * 0.25f)
                : railTint;
            b.Draw(this.sprites, new Rectangle(x, y - capHeight, capWidth, capHeight), SpriteSheet.GaugeTop, railTint);
            for (int i = 0; i < SegmentCount; i++)
                b.Draw(this.sprites, new Rectangle(x, y + i * segmentHeight, capWidth, segmentHeight), SpriteSheet.GaugeMiddle, railTint);
            b.Draw(this.sprites, new Rectangle(x, y + bodyHeight, capWidth, capHeight), SpriteSheet.GaugeBottom, anchorTint);

            // Calibration notches etched into the stone rather than protruding from it.
            Color etch = EtchShadow * (0.5f + breath * 0.1f);
            for (int tick = 1; tick <= 3; tick++)
            {
                int tickY = y + bodyHeight * tick / 4;
                b.Draw(Game1.staminaRect, new Rectangle(x + Scale, tickY, 2 * Scale, 2), etch);
                b.Draw(Game1.staminaRect, new Rectangle(x + capWidth - 3 * Scale, tickY, 2 * Scale, 2), etch);
            }

            int channelX = x + 3 * Scale;
            int channelWidth = 4 * Scale;

            // Tile the fill weave inside the channel. The edge is deliberately unsnapped from the
            // native grid: the fading memory must visibly bleed away screen-pixel by screen-pixel.
            int fillBottom = this.FloorToY(y, bodyHeight, this.displayed);
            Color effectColor = this.damageEffect ? DamageGlow : QiGlow;
            Color restingFill = Color.Lerp(Color.White, QiGlow, 0.12f + breath * 0.12f);
            Color fillTint = Color.Lerp(restingFill, effectColor, this.flash * 0.8f);
            int segmentIndex = 0;
            for (int fillY = y; fillY < fillBottom; fillY += segmentHeight)
            {
                int drawHeight = Math.Min(segmentHeight, fillBottom - fillY);
                int sourceHeight = Math.Max(1, drawHeight / Scale);
                Rectangle fillSource = new(SpriteSheet.GaugeFill.X, SpriteSheet.GaugeFill.Y, SpriteSheet.GaugeFill.Width, sourceHeight);

                // A slow luminous knot travels down the weave, so the retained memory reads alive.
                float knot = MathF.Max(0f, MathF.Sin(this.idlePhase * 1.6f - segmentIndex * 0.55f));
                Color segmentTint = Color.Lerp(fillTint, Color.White, knot * knot * 0.22f);
                b.Draw(this.sprites, new Rectangle(channelX, fillY, channelWidth, drawHeight), fillSource, segmentTint);
                segmentIndex++;
            }

            // Below the live fill, the channel keeps a faint violet residue: the memory that faded.
            if (fillBottom < y + bodyHeight)
                b.Draw(Game1.staminaRect, new Rectangle(channelX, fillBottom, channelWidth, y + bodyHeight - fillBottom), ResidueViolet * (0.22f + breath * 0.05f));

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
