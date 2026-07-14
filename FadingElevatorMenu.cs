using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace QisFadingElevator
{
    /// <summary>
    /// A Skull Cavern elevator menu. Lists floors up to the player's current foothold and warps there,
    /// reusing the vanilla elevator's layout + warp mechanism (learned from StardewValley.Menus.MineElevatorMenu).
    /// Identity is diegetic: a Qi Gem header and a purple-lit foothold floor, no meta text.
    /// Closes via Esc, the menu button, clicking outside, or picking a floor.
    /// </summary>
    public sealed class FadingElevatorMenu : IClickableMenu
    {
        /*********
        ** Fields
        *********/
        /// <summary>Skull Cavern floor N is MineShaft level (120 + N).</summary>
        private const int SkullCavernFloorOffset = 120;

        /// <summary>Pixel pitch of a floor button cell (40px button + 4px gap).</summary>
        private const int Cell = 44;

        /// <summary>Max buttons per row.</summary>
        private const int MaxColumns = 10;

        /// <summary>Mr. Qi's signature color (his Walnut Room door / Qi Gems are this purple).</summary>
        private static readonly Color QiPurple = new(178, 75, 243);

        private readonly List<ClickableComponent> buttons = new();
        private readonly int currentFloor;
        private readonly int footholdFloor;

        /*********
        ** Constructor
        *********/
        /// <param name="reachableFloor">The deepest Skull Cavern floor the elevator can reach (the foothold).</param>
        /// <param name="interval">Spacing between offered floors.</param>
        /// <param name="currentFloor">The floor the player is on now (grayed out), or -1 if outside the cavern.</param>
        public FadingElevatorMenu(int reachableFloor, int interval, int currentFloor)
            : base(0, 0, 0, 0, showUpperRightCloseButton: false)
        {
            this.currentFloor = currentFloor;
            this.footholdFloor = reachableFloor;

            List<int> floors = BuildFloorList(reachableFloor, Math.Max(1, interval));

            int cols = Math.Min(MaxColumns, Math.Max(1, floors.Count));
            int rows = (int)Math.Ceiling(floors.Count / (double)cols);
            int gridWidth = cols * Cell;
            int gridHeight = rows * Cell;

            this.width = gridWidth + IClickableMenu.borderWidth * 2;
            this.height = gridHeight + IClickableMenu.borderWidth * 2;
            this.xPositionOnScreen = Game1.uiViewport.Width / 2 - this.width / 2;
            this.yPositionOnScreen = Game1.uiViewport.Height / 2 - this.height / 2;

            // Vanilla elevator button offsets (proven to sit correctly inside the dialogue frame).
            int startX = this.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder * 3 / 4;
            int startY = this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.borderWidth / 3;
            for (int i = 0; i < floors.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Rectangle bounds = new(startX + col * Cell, startY + row * Cell, 40, 40);
                this.buttons.Add(new ClickableComponent(bounds, floors[i].ToString())
                {
                    myID = i,
                    rightNeighborID = (col == cols - 1) ? -1 : i + 1,
                    leftNeighborID = (col == 0) ? -1 : i - 1,
                    downNeighborID = i + cols,
                    upNeighborID = i - cols
                });
            }

            Game1.playSound("crystal");
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                this.populateClickableComponentList();
                this.snapToDefaultClickableComponent();
            }
        }

        /*********
        ** Public methods
        *********/
        public override void snapToDefaultClickableComponent()
        {
            this.currentlySnappedComponent = this.getComponentWithID(0);
            this.snapCursorToCurrentSnappedComponent();
        }

        /// <inheritdoc />
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (ClickableComponent button in this.buttons)
            {
                if (!button.containsPoint(x, y))
                    continue;

                int floor = Convert.ToInt32(button.name);
                if (floor == this.currentFloor)
                    return;

                Game1.playSound("smallSelect");
                Game1.player.ridingMineElevator = true;
                Game1.enterMine(SkullCavernFloorOffset + floor);
                Game1.exitActiveMenu();
                return;
            }

            if (!this.isWithinBounds(x, y))
                Game1.exitActiveMenu();
        }

        /// <inheritdoc />
        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            foreach (ClickableComponent button in this.buttons)
                button.scale = button.containsPoint(x, y) ? 2f : 1f;
        }

        /// <inheritdoc />
        public override void draw(SpriteBatch b)
        {
            if (!Game1.options.showClearBackgrounds)
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // Draw the frame with the vanilla elevator's offsets so buttons sit inside it.
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen - 64 + 8, this.width + 21, this.height + 64, speaker: false, drawOnlyBox: true);

            foreach (ClickableComponent button in this.buttons)
            {
                int floor = Convert.ToInt32(button.name);
                bool isCurrent = floor == this.currentFloor;
                bool isFoothold = floor == this.footholdFloor && !isCurrent;
                int sprite = (button.scale > 1f) ? 267 : 256;

                Color buttonColor = isFoothold ? QiPurple : Color.White;
                Color numberColor = isCurrent ? (Color.Gray * 0.75f) : Color.Gold;

                b.Draw(Game1.mouseCursors, new Vector2(button.bounds.X - 4, button.bounds.Y + 4), new Rectangle(sprite, 256, 10, 10), Color.Black * 0.5f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
                b.Draw(Game1.mouseCursors, new Vector2(button.bounds.X, button.bounds.Y), new Rectangle(sprite, 256, 10, 10), buttonColor, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.868f);
                NumberSprite.draw(
                    number: floor,
                    b: b,
                    position: new Vector2(button.bounds.X + 16 + NumberSprite.numberOfDigits(floor) * 6, button.bounds.Y + 24 - NumberSprite.getHeight() / 4),
                    c: numberColor,
                    scale: 0.5f,
                    layerDepth: 0.86f,
                    alpha: 1f,
                    secondDigitOffset: 0);
            }

            this.drawMouse(b);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Build the offered floors: the entrance, each interval, and always the exact foothold.</summary>
        private static List<int> BuildFloorList(int reachableFloor, int interval)
        {
            List<int> floors = new() { 1 };
            for (int f = interval; f < reachableFloor; f += interval)
            {
                if (f > 1)
                    floors.Add(f);
            }
            if (reachableFloor > 1 && floors[floors.Count - 1] != reachableFloor)
                floors.Add(reachableFloor);
            return floors;
        }
    }
}
