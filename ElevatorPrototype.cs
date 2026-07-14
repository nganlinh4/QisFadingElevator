using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using xTile.Tiles;

namespace QisFadingElevator
{
    /// <summary>
    /// Places the elevator inside the world's normal sprite pipeline. That lets the game's lighting and
    /// front-to-back sorting affect it exactly like vanilla objects and characters.
    /// </summary>
    internal static class ElevatorPrototype
    {
        public const string FoyerLocationName = "SkullCave";

        private const int FixtureSpriteId = 813057;
        private const int GlowSpriteId = 813058;
        private const int Scale = 4;
        private const int NativeWidth = 17;
        private const int NativeHeight = 32;
        private const int MineAllocationWidthInTiles = 1;
        private const int AllocationHeightInTiles = 2;
        private const int SkullCavernAreaId = 121;
        private static readonly Color FallbackMineTint = new(245, 168, 78);
        private static readonly Dictionary<string, Color> TilePaletteCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Whether this is a generated Skull Cavern floor (not the entrance foyer).</summary>
        public static bool IsSkullCavernFloor(GameLocation? location)
        {
            return location is MineShaft shaft && shaft.getMineArea() == SkullCavernAreaId;
        }

        /// <summary>Add or update the depth-sorted fixture in the given location.</summary>
        public static void EnsureSprite(GameLocation location, Texture2D sprites, bool isRepaired, int repairAnimationElapsed)
        {
            if (!TryGetPlacement(location, out ElevatorPlacement placement))
            {
                RemoveSprite(location);
                return;
            }

            bool isFoyer = string.Equals(location.NameOrUniqueName, FoyerLocationName, StringComparison.OrdinalIgnoreCase);
            if (!isRepaired && !isFoyer)
            {
                // Until the entrance mechanism is restored, there are no answering doors below.
                RemoveSprite(location);
                return;
            }

            TemporaryAnimatedSprite fixture = GetOrCreateSprite(location, sprites, FixtureSpriteId);
            TemporaryAnimatedSprite glow = GetOrCreateSprite(location, sprites, GlowSpriteId);
            bool isActive = location == Game1.currentLocation && IsPlayerInRange(placement);
            bool isRepairing = isFoyer && repairAnimationElapsed >= 0;
            bool showBroken = isFoyer && (!isRepaired || (isRepairing && repairAnimationElapsed < 72));
            Rectangle source = isFoyer
                ? showBroken ? SpriteSheet.FoyerBroken : SpriteSheet.FoyerElevator
                : isActive ? SpriteSheet.ElevatorActive : SpriteSheet.ElevatorIdle;
            Rectangle glowSource;
            if (isFoyer && isRepairing)
            {
                glowSource = repairAnimationElapsed < 72
                    ? (repairAnimationElapsed / 28) % 2 == 0 ? SpriteSheet.RepairImpactA : SpriteSheet.RepairImpactB
                    : SpriteSheet.RepairAwaken;
            }
            else if (isFoyer && !isRepaired)
            {
                glowSource = SpriteSheet.FoyerBrokenGlow;
            }
            else
            {
                glowSource = isFoyer
                    ? isActive ? SpriteSheet.FoyerGlowActive : SpriteSheet.FoyerGlowIdle
                    : isActive ? SpriteSheet.ElevatorGlowActive : SpriteSheet.ElevatorGlowIdle;
            }

            Vector2 drawPosition = placement.WorldPosition;
            if (isRepairing && repairAnimationElapsed < 72 && repairAnimationElapsed % 28 < 5)
            {
                int direction = repairAnimationElapsed % 2 == 0 ? -1 : 1;
                drawPosition.X += direction * Scale;
            }

            fixture.texture = sprites;
            fixture.position = drawPosition;
            fixture.sourceRect = source;
            fixture.sourceRectStartingPos = new Vector2(source.X, source.Y);
            fixture.scale = Scale;
            fixture.color = placement.Tint;

            glow.texture = sprites;
            glow.position = drawPosition;
            glow.sourceRect = glowSource;
            glow.sourceRectStartingPos = new Vector2(glowSource.X, glowSource.Y);
            glow.scale = Scale;
            double elapsed = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float breath = (float)((Math.Sin(elapsed / 230.0) + 1.0) * 0.5);
            float glowOpacity;
            if (isRepairing)
            {
                if (repairAnimationElapsed < 72)
                {
                    int impactTick = repairAnimationElapsed % 28;
                    float impact = Math.Max(0f, 1f - impactTick / 12f);
                    glowOpacity = 0.12f + impact * 0.82f;
                }
                else
                {
                    glowOpacity = 0.78f + breath * 0.2f;
                }
            }
            else if (isFoyer && !isRepaired)
            {
                glowOpacity = 0.14f + breath * 0.08f;
            }
            else
            {
                glowOpacity = isFoyer
                    ? isActive ? 0.68f + breath * 0.16f : 0.42f + breath * 0.08f
                    : isActive ? 0.72f + breath * 0.28f : 0.48f + breath * 0.12f;
            }
            glow.color = Color.White * glowOpacity;

            // A wall fixture must sort behind a farmer standing on the floor in front of it.
            fixture.layerDepth = (placement.WorldPosition.Y + 8f) / 10000f;
            glow.layerDepth = fixture.layerDepth + 0.000001f;
        }

        private static TemporaryAnimatedSprite GetOrCreateSprite(GameLocation location, Texture2D sprites, int id)
        {
            TemporaryAnimatedSprite? sprite = location.TemporarySprites.FirstOrDefault(candidate => candidate.id == id);
            if (sprite is not null)
                return sprite;

            sprite = new TemporaryAnimatedSprite
            {
                id = id,
                texture = sprites,
                scale = Scale,
                animationLength = 1,
                interval = 999999f,
                totalNumberOfLoops = 1,
                holdLastFrame = true,
                dontClearOnAreaEntry = true,
                drawAboveAlwaysFront = false,
                color = Color.White
            };
            location.TemporarySprites.Add(sprite);
            return sprite;
        }

        /// <summary>Remove our fixture sprite without touching any temporary sprites owned by the game or other mods.</summary>
        public static void RemoveSprite(GameLocation location)
        {
            TemporaryAnimatedSprite? fixture = location.TemporarySprites.FirstOrDefault(sprite => sprite.id == FixtureSpriteId);
            TemporaryAnimatedSprite? glow = location.TemporarySprites.FirstOrDefault(sprite => sprite.id == GlowSpriteId);
            if (fixture is not null)
                location.TemporarySprites.Remove(fixture);
            if (glow is not null)
                location.TemporarySprites.Remove(glow);
        }

        /// <summary>Whether the local player is next to the current fixture.</summary>
        public static bool IsPlayerInRange()
        {
            if (!TryGetPlacement(Game1.currentLocation, out ElevatorPlacement placement))
                return false;

            return IsPlayerInRange(placement);
        }

        private static bool IsPlayerInRange(ElevatorPlacement placement)
        {
            Point player = Game1.player.TilePoint;
            Rectangle area = placement.TileArea;
            int nearestX = Math.Clamp(player.X, area.Left, area.Right - 1);
            int nearestY = Math.Clamp(player.Y, area.Top, area.Bottom - 1);
            int dx = Math.Abs(player.X - nearestX);
            int dy = Math.Abs(player.Y - nearestY);
            return Math.Max(dx, dy) <= 1;
        }

        /// <summary>Whether an absolute-world cursor position is directly over the rendered fixture.</summary>
        public static bool IsCursorOver(Vector2 absolutePixels)
        {
            if (!TryGetPlacement(Game1.currentLocation, out ElevatorPlacement placement))
                return false;

            Rectangle bounds = new(
                (int)placement.WorldPosition.X,
                (int)placement.WorldPosition.Y,
                NativeWidth * Scale,
                NativeHeight * Scale);
            return bounds.Contains((int)absolutePixels.X, (int)absolutePixels.Y);
        }

        /// <summary>Resolve the fixed foyer anchor or a generated-floor anchor beside the entrance ladder.</summary>
        private static bool TryGetPlacement(GameLocation? location, out ElevatorPlacement placement)
        {
            placement = default;
            if (location is null)
                return false;

            if (string.Equals(location.NameOrUniqueName, FoyerLocationName, StringComparison.OrdinalIgnoreCase))
            {
                // Keep the fixture centered in the same two-tile foyer niche even though its art is slimmer.
                // This branch has its own multi-color foyer artwork; don't flatten it through a tint.
                placement = CreatePlacement(4, 2, allocationWidthInTiles: 2, tint: Color.White);
                return true;
            }

            if (location is not MineShaft shaft || shaft.getMineArea() != SkullCavernAreaId || shaft.tileBeneathLadder == Vector2.Zero)
                return false;

            int topY = (int)shaft.tileBeneathLadder.Y - 2;
            int rightX = (int)shaft.tileBeneathLadder.X + 1;
            int leftX = (int)shaft.tileBeneathLadder.X - 1;
            int rightScore = ScoreWallBacking(location, rightX, topY, outwardDirection: 1);
            int leftScore = ScoreWallBacking(location, leftX, topY, outwardDirection: -1);

            if (rightScore == int.MinValue && leftScore == int.MinValue)
                return false;

            // Prefer the right side on ties, matching the foyer and keeping placement visually predictable.
            int tileX = rightScore >= leftScore ? rightX : leftX;
            Color tint = ResolveWallTint(location, tileX, topY);
            placement = CreatePlacement(tileX, topY, MineAllocationWidthInTiles, tint);
            return true;
        }

        /// <summary>
        /// Score one side of the ladder using three independent checks: a complete two-row wall backing,
        /// support under the fixture's four-pixel outer overhang, and an open backed access tile below it.
        /// Generated-floor objects aren't included, so breaking a rock can't make the elevator jump sides.
        /// </summary>
        private static int ScoreWallBacking(GameLocation location, int tileX, int tileY, int outwardDirection)
        {
            int mapWidth = location.Map.Layers[0].LayerWidth;
            int mapHeight = location.Map.Layers[0].LayerHeight;
            int outwardX = tileX + outwardDirection;
            int accessY = tileY + AllocationHeightInTiles;
            if (tileX < 0 || tileX >= mapWidth
                || outwardX < 0 || outwardX >= mapWidth
                || tileY < 0 || accessY >= mapHeight)
            {
                return int.MinValue;
            }

            var back = location.Map.GetLayer("Back");
            var buildings = location.Map.GetLayer("Buildings");
            var front = location.Map.GetLayer("Front");
            var alwaysFront = location.Map.GetLayer("AlwaysFront");
            int score = 0;
            for (int y = tileY; y < tileY + AllocationHeightInTiles; y++)
            {
                bool hasBack = back?.Tiles[tileX, y] is not null;
                bool hasBuildings = buildings?.Tiles[tileX, y] is not null;
                bool hasFront = front?.Tiles[tileX, y] is not null || alwaysFront?.Tiles[tileX, y] is not null;
                if (!hasBack && !hasBuildings && !hasFront)
                    return int.MinValue;

                score += hasBack ? 2 : 0;
                score += hasBuildings ? 7 : 0;
                score += hasFront ? 3 : 0;

                // The 17px art is 68 world pixels wide, so only two pixels cross either tile edge.
                bool outerSupported = back?.Tiles[outwardX, y] is not null
                    || buildings?.Tiles[outwardX, y] is not null
                    || front?.Tiles[outwardX, y] is not null
                    || alwaysFront?.Tiles[outwardX, y] is not null;
                if (!outerSupported)
                    return int.MinValue;
                score += 2;
            }

            // A fixture is useful only if its adjacent floor tile exists and isn't sealed by a wall layer.
            if (back?.Tiles[tileX, accessY] is null)
                return int.MinValue;
            score += buildings?.Tiles[tileX, accessY] is null ? 8 : -12;
            score += front?.Tiles[tileX, accessY] is null && alwaysFront?.Tiles[tileX, accessY] is null ? 3 : -3;
            return score;
        }

        /// <summary>Sample the actual patched wall tiles behind the fixture and turn them into a material tint.</summary>
        private static Color ResolveWallTint(GameLocation location, int tileX, int tileY)
        {
            var buildings = location.Map.GetLayer("Buildings");
            var front = location.Map.GetLayer("Front");
            var back = location.Map.GetLayer("Back");
            double red = 0;
            double green = 0;
            double blue = 0;
            int count = 0;

            for (int y = tileY; y < tileY + AllocationHeightInTiles; y++)
            {
                Tile? tile = buildings?.Tiles[tileX, y]
                    ?? front?.Tiles[tileX, y]
                    ?? back?.Tiles[tileX, y];
                Color? sampled = SampleTilePalette(tile);
                if (!sampled.HasValue)
                    continue;

                red += sampled.Value.R;
                green += sampled.Value.G;
                blue += sampled.Value.B;
                count++;
            }

            if (count == 0)
                return FallbackMineTint;

            Color wall = new((byte)(red / count), (byte)(green / count), (byte)(blue / count));
            double luma = wall.R * 0.2126 + wall.G * 0.7152 + wall.B * 0.0722;
            const double saturationRetention = 0.92;
            const double neutralMaterialCompensation = 1.55;
            return new Color(
                ToByte((luma + (wall.R - luma) * saturationRetention) * neutralMaterialCompensation),
                ToByte((luma + (wall.G - luma) * saturationRetention) * neutralMaterialCompensation),
                ToByte((luma + (wall.B - luma) * saturationRetention) * neutralMaterialCompensation));
        }

        private static Color? SampleTilePalette(Tile? tile)
        {
            if (tile is null || string.IsNullOrWhiteSpace(tile.TileSheet.ImageSource))
                return null;

            try
            {
                Texture2D texture = Game1.content.Load<Texture2D>(tile.TileSheet.ImageSource);
                string cacheKey = $"{tile.TileSheet.ImageSource}|{texture.GetHashCode()}|{tile.TileIndex}";
                if (TilePaletteCache.TryGetValue(cacheKey, out Color cached))
                    return cached;

                int tileWidth = Math.Max(1, tile.TileSheet.TileWidth);
                int tileHeight = Math.Max(1, tile.TileSheet.TileHeight);
                int columns = Math.Max(1, tile.TileSheet.SheetWidth);
                int sourceX = tile.TileSheet.MarginWidth
                    + (tile.TileIndex % columns) * (tileWidth + tile.TileSheet.SpacingWidth);
                int sourceY = tile.TileSheet.MarginHeight
                    + (tile.TileIndex / columns) * (tileHeight + tile.TileSheet.SpacingHeight);
                Rectangle source = new(sourceX, sourceY, tileWidth, tileHeight);
                if (source.Left < 0 || source.Top < 0 || source.Right > texture.Width || source.Bottom > texture.Height)
                    return null;

                Color[] pixels = new Color[tileWidth * tileHeight];
                texture.GetData(0, source, pixels, 0, pixels.Length);
                double red = 0;
                double green = 0;
                double blue = 0;
                double totalWeight = 0;
                foreach (Color pixel in pixels)
                {
                    if (pixel.A < 80)
                        continue;

                    double luma = pixel.R * 0.2126 + pixel.G * 0.7152 + pixel.B * 0.0722;
                    if (luma < 18)
                        continue;

                    // Pale structural pixels carry slightly more weight than tiny outline flecks.
                    double weight = pixel.A / 255.0 * (0.7 + luma / 850.0);
                    red += pixel.R * weight;
                    green += pixel.G * weight;
                    blue += pixel.B * weight;
                    totalWeight += weight;
                }

                if (totalWeight <= 0)
                    return null;

                Color result = new(
                    (byte)(red / totalWeight),
                    (byte)(green / totalWeight),
                    (byte)(blue / totalWeight));
                TilePaletteCache[cacheKey] = result;
                return result;
            }
            catch (Exception)
            {
                // Palette matching is cosmetic; retain a safe desert tint if an unusual tilesheet can't be read.
                return null;
            }
        }

        private static byte ToByte(double value)
        {
            return (byte)Math.Clamp((int)Math.Round(value), 48, 255);
        }

        private static ElevatorPlacement CreatePlacement(int tileX, int tileY, int allocationWidthInTiles, Color tint)
        {
            int allocationWidth = allocationWidthInTiles * Game1.tileSize;
            int spriteWidth = NativeWidth * Scale;
            Vector2 world = new(
                tileX * Game1.tileSize + (allocationWidth - spriteWidth) / 2,
                tileY * Game1.tileSize);
            return new ElevatorPlacement(world, new Rectangle(tileX, tileY, allocationWidthInTiles, AllocationHeightInTiles), tint);
        }

        private readonly record struct ElevatorPlacement(Vector2 WorldPosition, Rectangle TileArea, Color Tint);
    }
}
