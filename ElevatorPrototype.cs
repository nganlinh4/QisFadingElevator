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

        private const int ShellSpriteId = 813057;
        private const int GlowSpriteId = 813058;
        private const int AccentsSpriteId = 813059;
        private const int Scale = 4;
        private const int NativeWidth = 17;
        private const int NativeHeight = 32;
        private const int MineAllocationWidthInTiles = 1;
        private const int AllocationHeightInTiles = 2;
        private const int SkullCavernAreaId = 121;

        private static readonly Color FallbackWallColor = new(150, 100, 52);
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

            bool isRepairing = isFoyer && repairAnimationElapsed >= 0;
            Vector2 drawPosition = placement.WorldPosition + GetRepairJolt(isRepairing, repairAnimationElapsed);
            bool isActive = location == Game1.currentLocation && IsPlayerInRange(placement);
            double elapsedMs = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float breath = (float)((Math.Sin(elapsedMs / 230.0) + 1.0) * 0.5);

            // The shell relief takes the full sampled wall color so its values land inside the
            // room's own range; the accents sprite keeps interior darkness and identity untinted.
            bool showBroken = isFoyer
                && (!isRepaired || (isRepairing && repairAnimationElapsed < RepairSequence.BodyRepairedAt));
            TemporaryAnimatedSprite shell = GetOrCreateSprite(location, sprites, ShellSpriteId);
            ApplySprite(shell, sprites, showBroken ? SpriteSheet.BrokenShell : SpriteSheet.LiftShell, drawPosition);
            shell.color = CompensateForMultiply(placement.WallColor);

            TemporaryAnimatedSprite accents = GetOrCreateSprite(location, sprites, AccentsSpriteId);
            ApplySprite(accents, sprites, showBroken ? SpriteSheet.BrokenAccents : SpriteSheet.LiftAccents, drawPosition);
            accents.color = Color.White;

            TemporaryAnimatedSprite glow = GetOrCreateSprite(location, sprites, GlowSpriteId);
            (Rectangle glowSource, float glowOpacity, Vector2 glowOffset) = isRepairing
                ? GetRepairGlow(repairAnimationElapsed, breath)
                : GetRestingGlow(isRepaired, isActive, breath);
            ApplySprite(glow, sprites, glowSource, drawPosition + glowOffset);
            glow.color = Color.White * glowOpacity;

            // A wall fixture must sort behind a farmer standing on the floor in front of it.
            shell.layerDepth = (placement.WorldPosition.Y + 8f) / 10000f;
            accents.layerDepth = shell.layerDepth + 0.000001f;
            glow.layerDepth = shell.layerDepth + 0.000002f;
        }

        /// <summary>Resting-state glow: a dim socket ember when broken, a breathing crystal otherwise.</summary>
        private static (Rectangle, float, Vector2) GetRestingGlow(bool isRepaired, bool isActive, float breath)
        {
            if (!isRepaired)
                return (SpriteSheet.BrokenGlow, 0.6f + breath * 0.25f, Vector2.Zero);

            Rectangle source = isActive ? SpriteSheet.GlowActive : SpriteSheet.GlowIdle;
            float opacity = isActive ? 0.78f + breath * 0.22f : 0.5f + breath * 0.14f;
            return (source, opacity, Vector2.Zero);
        }

        /// <summary>Map a repair-scene tick to overlay frame, opacity, and offset (for the seam slice).</summary>
        private static (Rectangle, float, Vector2) GetRepairGlow(int elapsed, float breath)
        {
            if (elapsed >= RepairSequence.Impact1 && elapsed < RepairSequence.Impact1 + RepairSequence.FlashTicks)
                return (SpriteSheet.RepairFlashA, FlashFade(elapsed - RepairSequence.Impact1), Vector2.Zero);
            if (elapsed >= RepairSequence.Impact2 && elapsed < RepairSequence.Impact2 + RepairSequence.FlashTicks)
                return (SpriteSheet.RepairFlashB, FlashFade(elapsed - RepairSequence.Impact2), Vector2.Zero);
            if (elapsed >= RepairSequence.Impact3 && elapsed < RepairSequence.Impact3 + RepairSequence.FlashTicks + 3)
                return (SpriteSheet.RepairFlashC, FlashFade(elapsed - RepairSequence.Impact3), Vector2.Zero);

            if (elapsed >= RepairSequence.BatteryAt && elapsed < RepairSequence.SeamStart)
            {
                // The dead socket flickers twice before holding: off-on-off-on.
                int flickerTick = (elapsed - RepairSequence.BatteryAt) / 3;
                float opacity = flickerTick switch
                {
                    0 or 2 => 0.15f,
                    1 => 0.75f,
                    _ => 0.55f + breath * 0.2f
                };
                return (SpriteSheet.GlowIdle, opacity, Vector2.Zero);
            }

            if (elapsed >= RepairSequence.SeamStart && elapsed < RepairSequence.SeamEnd)
            {
                // Reveal the seam bottom-up: light climbing from the threshold toward the crown.
                float progress = (elapsed - RepairSequence.SeamStart) / (float)(RepairSequence.SeamEnd - RepairSequence.SeamStart);
                int topRow = 29 - (int)Math.Round(progress * 27);
                Rectangle slice = new(
                    SpriteSheet.RepairSeam.X,
                    SpriteSheet.RepairSeam.Y + topRow,
                    SpriteSheet.RepairSeam.Width,
                    SpriteSheet.RepairSeam.Height - topRow);
                return (slice, 0.85f + progress * 0.15f, new Vector2(0, topRow * Scale));
            }

            if (elapsed >= RepairSequence.FlareSmallAt && elapsed < RepairSequence.FlareBigAt)
                return (SpriteSheet.RepairFlareA, 1f, Vector2.Zero);
            if (elapsed >= RepairSequence.FlareBigAt && elapsed < RepairSequence.FlareEnd)
                return (SpriteSheet.RepairFlareB, 1f, Vector2.Zero);
            if (elapsed >= RepairSequence.FlareEnd)
            {
                float settle = Math.Min(1f, (elapsed - RepairSequence.FlareEnd) / 20f);
                return (SpriteSheet.GlowActive, 0.5f + settle * 0.28f + breath * 0.22f * settle, Vector2.Zero);
            }

            // Between beats the broken socket ember barely holds on.
            return (SpriteSheet.BrokenGlow, 0.5f + breath * 0.2f, Vector2.Zero);
        }

        private static float FlashFade(int ticksIn)
        {
            return Math.Max(0.25f, 1f - ticksIn * 0.09f);
        }

        /// <summary>Physical recoil for each seating impact; the third blow lands hardest.</summary>
        private static Vector2 GetRepairJolt(bool isRepairing, int elapsed)
        {
            if (!isRepairing || !RepairSequence.IsImpactBeat(elapsed, out int impactStart))
                return Vector2.Zero;

            int ticksIn = elapsed - impactStart;
            if (ticksIn is < 0 or >= RepairSequence.JoltTicks)
                return Vector2.Zero;

            int sway = ticksIn % 2 == 0 ? 1 : -1;
            return impactStart switch
            {
                RepairSequence.Impact1 => new Vector2(sway * Scale, 0),
                RepairSequence.Impact2 => new Vector2(-sway * Scale, 0),
                _ => new Vector2(sway * Scale, ticksIn < 3 ? Scale : 0)
            };
        }

        private static void ApplySprite(TemporaryAnimatedSprite sprite, Texture2D sprites, Rectangle source, Vector2 position)
        {
            sprite.texture = sprites;
            sprite.position = position;
            sprite.sourceRect = source;
            sprite.sourceRectStartingPos = new Vector2(source.X, source.Y);
            sprite.scale = Scale;
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

        /// <summary>Remove our fixture sprites without touching any temporary sprites owned by the game or other mods.</summary>
        public static void RemoveSprite(GameLocation location)
        {
            RemoveSpriteById(location, ShellSpriteId);
            RemoveSpriteById(location, AccentsSpriteId);
            RemoveSpriteById(location, GlowSpriteId);
        }

        private static void RemoveSpriteById(GameLocation location, int id)
        {
            TemporaryAnimatedSprite? sprite = location.TemporarySprites.FirstOrDefault(candidate => candidate.id == id);
            if (sprite is not null)
                location.TemporarySprites.Remove(sprite);
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

        /// <summary>World-pixel center of the skull's forehead crystal, where ignition effects bloom.</summary>
        public static bool TryGetCrownCenter(GameLocation? location, out Vector2 center)
        {
            center = Vector2.Zero;
            if (!TryGetPlacement(location, out ElevatorPlacement placement))
                return false;

            center = placement.WorldPosition + new Vector2(8.5f * Scale, 2f * Scale);
            return true;
        }

        /// <summary>World-pixel point on the right jamb where the battery seats.</summary>
        public static bool TryGetSocketPoint(GameLocation? location, out Vector2 point)
        {
            point = Vector2.Zero;
            if (!TryGetPlacement(location, out ElevatorPlacement placement))
                return false;

            point = placement.WorldPosition + new Vector2(13.5f * Scale, 23.5f * Scale);
            return true;
        }

        /// <summary>World-pixel rectangle of the fixture's threshold, where impact dust rises.</summary>
        public static bool TryGetThresholdPoint(GameLocation? location, out Vector2 point)
        {
            point = Vector2.Zero;
            if (!TryGetPlacement(location, out ElevatorPlacement placement))
                return false;

            point = placement.WorldPosition + new Vector2(8.5f * Scale, 28f * Scale);
            return true;
        }

        /// <summary>Resolve the fixed foyer anchor or a generated-floor anchor beside the entrance ladder.</summary>
        private static bool TryGetPlacement(GameLocation? location, out ElevatorPlacement placement)
        {
            placement = default;
            if (location is null)
                return false;

            if (string.Equals(location.NameOrUniqueName, FoyerLocationName, StringComparison.OrdinalIgnoreCase))
            {
                // Keep the fixture centered in the same two-tile foyer niche even though its art is
                // slimmer. The foyer wall is sampled like any generated room, so recolor mods and
                // the shell's wall-parity calibration apply here too.
                Color foyerWall = ResolveWallColor(location, 4, 2, allocationWidthInTiles: 2);
                placement = CreatePlacement(4, 2, allocationWidthInTiles: 2, wallColor: foyerWall);
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
            Color wallColor = ResolveWallColor(location, tileX, topY, MineAllocationWidthInTiles);
            placement = CreatePlacement(tileX, topY, MineAllocationWidthInTiles, wallColor);
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

        /// <summary>Sample the actual patched wall tiles behind the fixture into one raw wall color.</summary>
        private static Color ResolveWallColor(GameLocation location, int tileX, int tileY, int allocationWidthInTiles)
        {
            var buildings = location.Map.GetLayer("Buildings");
            var front = location.Map.GetLayer("Front");
            var back = location.Map.GetLayer("Back");
            double red = 0;
            double green = 0;
            double blue = 0;
            int count = 0;

            for (int x = tileX; x < tileX + allocationWidthInTiles; x++)
            {
                for (int y = tileY; y < tileY + AllocationHeightInTiles; y++)
                {
                    Tile? tile = buildings?.Tiles[x, y]
                        ?? front?.Tiles[x, y]
                        ?? back?.Tiles[x, y];
                    Color? sampled = SampleTilePalette(tile);
                    if (!sampled.HasValue)
                        continue;

                    red += sampled.Value.R;
                    green += sampled.Value.G;
                    blue += sampled.Value.B;
                    count++;
                }
            }

            return count == 0
                ? FallbackWallColor
                : new Color((byte)(red / count), (byte)(green / count), (byte)(blue / count));
        }

        /// <summary>
        /// Brighten the sampled wall color so the shell's near-white relief (midtone ~212) multiplied
        /// by it lands on the wall's own median luminance. Calibrated against live screenshots:
        /// 212/255 x 1.30 keeps the fixture at 0.8-0.95 of the wall's brightness in every room.
        /// </summary>
        private static Color CompensateForMultiply(Color wall)
        {
            double luma = wall.R * 0.2126 + wall.G * 0.7152 + wall.B * 0.0722;
            const double saturationRetention = 0.92;
            const double neutralMaterialCompensation = 1.30;
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

        private static ElevatorPlacement CreatePlacement(int tileX, int tileY, int allocationWidthInTiles, Color wallColor)
        {
            int allocationWidth = allocationWidthInTiles * Game1.tileSize;
            int spriteWidth = NativeWidth * Scale;
            Vector2 world = new(
                tileX * Game1.tileSize + (allocationWidth - spriteWidth) / 2,
                tileY * Game1.tileSize);
            return new ElevatorPlacement(world, new Rectangle(tileX, tileY, allocationWidthInTiles, AllocationHeightInTiles), wallColor);
        }

        private readonly record struct ElevatorPlacement(Vector2 WorldPosition, Rectangle TileArea, Color WallColor);
    }
}
