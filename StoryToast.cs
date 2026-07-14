using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace QisFadingElevator
{
    /// <summary>A single, non-queuing lower-left story notice with duplicate cooldown.</summary>
    internal sealed class StoryToast
    {
        private const int LifetimeTicks = 240;
        private const int FadeInTicks = 12;
        private const int FadeOutTicks = 24;
        private const int DuplicateCooldownTicks = LifetimeTicks;

        private string? message;
        private string? lastMessage;
        private int age;
        private int remaining;
        private int duplicateCooldown;

        public void Show(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            if (value == this.lastMessage && this.duplicateCooldown > 0)
                return;

            // There is deliberately no queue: a new relevant notice replaces the old one in the same slot.
            this.message = value;
            this.lastMessage = value;
            this.age = 0;
            this.remaining = LifetimeTicks;
            this.duplicateCooldown = DuplicateCooldownTicks;
        }

        public void Clear()
        {
            this.message = null;
            this.lastMessage = null;
            this.age = 0;
            this.remaining = 0;
            this.duplicateCooldown = 0;
        }

        public void Update()
        {
            if (this.duplicateCooldown > 0)
                this.duplicateCooldown--;
            if (this.remaining <= 0)
                return;

            this.age++;
            this.remaining--;
            if (this.remaining == 0)
                this.message = null;
        }

        public void Draw(SpriteBatch b)
        {
            if (this.message is null || this.remaining <= 0)
                return;

            int textWidth = Math.Min(480, Math.Max(160, Game1.uiViewport.Width - 96));
            string wrapped = Game1.parseText(this.message, Game1.smallFont, textWidth);
            Vector2 measured = Game1.smallFont.MeasureString(wrapped);
            int boxWidth = (int)Math.Ceiling(measured.X) + 48;
            int boxHeight = (int)Math.Ceiling(measured.Y) + 32;

            float fadeIn = Math.Min(1f, this.age / (float)FadeInTicks);
            float fadeOut = Math.Min(1f, this.remaining / (float)FadeOutTicks);
            float alpha = Math.Min(fadeIn, fadeOut);
            float eased = 1f - MathF.Pow(1f - fadeIn, 3f);
            int x = 24 - (int)MathF.Round((1f - eased) * 18f);
            int y = Game1.uiViewport.Height - boxHeight - 72;

            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                x,
                y,
                boxWidth,
                boxHeight,
                Color.White * alpha,
                1f,
                drawShadow: true,
                draw_layer: 0.98f);

            Vector2 textPosition = new(x + 24, y + 16);
            b.DrawString(Game1.smallFont, wrapped, textPosition + new Vector2(2f, 2f), Color.Black * (alpha * 0.38f));
            b.DrawString(Game1.smallFont, wrapped, textPosition, Game1.textColor * alpha);
        }
    }
}
