from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / "assets"
REFERENCES = PROJECT / "references"

# Exact colors sampled from vanilla mine_desert.png and the vanilla Qi Gem sprite.
TRANSPARENT = (0, 0, 0, 0)
INK = (22, 19, 13, 255)          # #16130d
OUTLINE = (62, 42, 39, 255)      # #3e2a27
DEEP_WOOD = (55, 30, 17, 255)    # #371e11
DARK_WOOD = (98, 59, 39, 255)    # #623b27
WOOD = (122, 67, 38, 255)        # #7a4326
AMBER = (149, 87, 25, 255)       # #955719
GOLD_DARK = (168, 110, 23, 255)  # #a86e17
GOLD = (183, 129, 58, 255)       # #b7813a
GOLD_LIGHT = (203, 151, 82, 255) # #cb9752
GOLD_GLEAM = (232, 170, 76, 255) # #e8aa4c
CAVE_SHADOW = (78, 46, 43, 255)  # #4e2e2b
CAVE_BRONZE = (118, 83, 36, 255) # #765324
CAVE_LIGHT = (154, 108, 47, 255) # #9a6c2f
CAVE_DUST = (132, 99, 46, 255)   # #84632e
# Muted desert stone colors sampled from mine_desert.png. These have less chroma than the
# timber palette, which keeps the fixture from looking pasted onto pale treasure-room walls.
DESERT_SHADOW = (116, 85, 56, 255)   # #745538
DESERT_MID = (151, 110, 71, 255)     # #976e47
DESERT_LIGHT = (175, 128, 98, 255)   # #af8062
SAND_DUST = (152, 140, 100, 255)     # #988c64
SAND_LIGHT = (173, 158, 110, 255)    # #ad9e6e
SAND_PALE = (189, 164, 132, 255)     # #bda484
ABYSS = (24, 16, 32, 255)
# Neutral material values for the elevator body. Runtime multiplies these by the actual wall palette,
# while the Qi layer is drawn separately so its violet light never gets muddied by a desert tint.
ADAPT_DEEP = (105, 105, 105, 255)
ADAPT_SHADOW = (145, 145, 145, 255)
ADAPT_DARK = (178, 178, 178, 255)
ADAPT_MID = (208, 208, 208, 255)
ADAPT_LIGHT = (232, 232, 232, 255)
ADAPT_GLEAM = (255, 255, 255, 255)
QI_DARK = (64, 19, 153, 255)     # #401399
QI_PURPLE = (106, 51, 255, 255)  # #6a33ff
QI_VIOLET = (156, 41, 255, 255)  # #9c29ff
QI_LIGHT = (127, 123, 255, 255)  # #7f7bff
QI_GLOW = (220, 173, 255, 255)   # #dcadff
QI_WHITE = (255, 234, 255, 255)  # #ffeaff

ELEVATOR_IDLE = (0, 0, 17, 32)
ELEVATOR_ACTIVE = (17, 0, 17, 32)
ELEVATOR_GLOW_IDLE = (34, 0, 17, 32)
ELEVATOR_GLOW_ACTIVE = (51, 0, 17, 32)
GAUGE_TOP = (0, 32, 8, 4)
GAUGE_MIDDLE = (0, 36, 8, 2)
GAUGE_BOTTOM = (0, 38, 8, 4)
GAUGE_MARKER = (8, 32, 10, 5)
GAUGE_RECORD = (8, 37, 10, 3)
GAUGE_ICON = (18, 32, 8, 8)
GAUGE_FILL = (26, 32, 4, 2)
FOYER_ELEVATOR = (30, 32, 17, 32)
FOYER_GLOW_IDLE = (47, 32, 17, 32)
FOYER_GLOW_ACTIVE = (64, 32, 17, 32)
FOYER_BROKEN = (81, 32, 17, 32)
FOYER_BROKEN_GLOW = (85, 0, 17, 32)
REPAIR_IMPACT_A = (0, 64, 17, 32)
REPAIR_IMPACT_B = (17, 64, 17, 32)
REPAIR_AWAKEN = (34, 64, 17, 32)


def px(image: Image.Image, x: int, y: int, color: tuple[int, int, int, int]) -> None:
    image.putpixel((x, y), color)


def hline(image: Image.Image, x1: int, x2: int, y: int, color: tuple[int, int, int, int]) -> None:
    for x in range(x1, x2 + 1):
        px(image, x, y, color)


def vline(image: Image.Image, x: int, y1: int, y2: int, color: tuple[int, int, int, int]) -> None:
    for y in range(y1, y2 + 1):
        px(image, x, y, color)


def rect(image: Image.Image, x1: int, y1: int, x2: int, y2: int, color: tuple[int, int, int, int]) -> None:
    for y in range(y1, y2 + 1):
        hline(image, x1, x2, y, color)


def draw_qi_crystal(image: Image.Image, ox: int, oy: int, active: bool) -> None:
    dark = QI_PURPLE if active else QI_DARK
    mid = QI_VIOLET if active else QI_PURPLE
    light = QI_WHITE if active else QI_LIGHT
    px(image, ox + 2, oy, light)
    hline(image, ox + 1, ox + 3, oy + 1, mid)
    hline(image, ox, ox + 4, oy + 2, dark)
    hline(image, ox + 1, ox + 3, oy + 3, mid)
    px(image, ox + 2, oy + 4, dark)
    px(image, ox + 2, oy + 1, light)


def with_alpha(color: tuple[int, int, int, int], alpha: int) -> tuple[int, int, int, int]:
    return color[0], color[1], color[2], alpha


def draw_elevator(sheet: Image.Image, origin_x: int) -> None:
    """Hand-pixel a perfectly mirrored 17x32 ancient lift, centered on its one-pixel seam."""
    ox = origin_x
    edge_faint = with_alpha(ADAPT_MID, 64)
    edge_soft = with_alpha(ADAPT_MID, 112)
    edge_mid = with_alpha(ADAPT_DARK, 168)

    # Every contour pixel has a mirrored partner around x=8. The odd source width gives the door
    # a real center pixel, avoiding the half-pixel imbalance that the old 18px design could not solve.
    hline(sheet, ox + 6, ox + 10, 2, edge_faint)
    hline(sheet, ox + 4, ox + 12, 3, edge_soft)
    hline(sheet, ox + 3, ox + 13, 4, edge_mid)
    for x, y, color in [
        (2, 5, edge_faint), (14, 5, edge_faint), (1, 8, edge_soft), (15, 8, edge_soft),
        (0, 12, edge_faint), (16, 12, edge_faint), (1, 18, edge_soft), (15, 18, edge_soft),
        (0, 24, edge_faint), (16, 24, edge_faint), (2, 29, edge_soft), (14, 29, edge_soft),
        (6, 31, edge_faint), (10, 31, edge_faint),
    ]:
        px(sheet, ox + x, y, color)

    # Stepped reliquary arch: broad enough to feel important, still narrower than one game tile.
    hline(sheet, ox + 5, ox + 11, 3, ADAPT_DEEP)
    hline(sheet, ox + 3, ox + 13, 4, ADAPT_SHADOW)
    hline(sheet, ox + 2, ox + 14, 5, ADAPT_DARK)
    hline(sheet, ox + 3, ox + 13, 6, ADAPT_MID)
    hline(sheet, ox + 4, ox + 12, 7, ADAPT_LIGHT)
    px(sheet, ox + 4, 6, ADAPT_GLEAM)
    px(sheet, ox + 12, 6, ADAPT_GLEAM)

    # Four-value mirrored jambs feel carved rather than assembled from mismatched timber strips.
    rect(sheet, ox + 1, 7, ox + 4, 27, ADAPT_DARK)
    vline(sheet, ox + 1, 9, 26, ADAPT_DEEP)
    vline(sheet, ox + 2, 8, 26, ADAPT_SHADOW)
    vline(sheet, ox + 3, 8, 26, ADAPT_MID)
    vline(sheet, ox + 4, 9, 26, ADAPT_LIGHT)
    rect(sheet, ox + 12, 7, ox + 15, 27, ADAPT_DARK)
    vline(sheet, ox + 12, 9, 26, ADAPT_LIGHT)
    vline(sheet, ox + 13, 8, 26, ADAPT_MID)
    vline(sheet, ox + 14, 8, 26, ADAPT_SHADOW)
    vline(sheet, ox + 15, 9, 26, ADAPT_DEEP)

    # Central Qi keystone socket; color lives on the separate glow layer.
    hline(sheet, ox + 7, ox + 9, 2, ADAPT_DEEP)
    rect(sheet, ox + 6, 3, ox + 10, 6, ADAPT_SHADOW)
    hline(sheet, ox + 7, ox + 9, 3, ADAPT_LIGHT)
    hline(sheet, ox + 7, ox + 9, 5, ADAPT_DARK)
    px(sheet, ox + 8, 6, ADAPT_DEEP)

    # Twin three-column doors mirror exactly around the true center seam at x=8.
    hline(sheet, ox + 5, ox + 11, 7, ADAPT_DEEP)
    rect(sheet, ox + 5, 8, ox + 11, 26, ADAPT_SHADOW)
    vline(sheet, ox + 5, 9, 25, ADAPT_SHADOW)
    vline(sheet, ox + 6, 9, 25, ADAPT_DARK)
    vline(sheet, ox + 7, 9, 25, ADAPT_MID)
    vline(sheet, ox + 8, 8, 26, ADAPT_DEEP)
    vline(sheet, ox + 9, 9, 25, ADAPT_MID)
    vline(sheet, ox + 10, 9, 25, ADAPT_DARK)
    vline(sheet, ox + 11, 9, 25, ADAPT_SHADOW)
    for band_y in (15, 22):
        hline(sheet, ox + 5, ox + 7, band_y, ADAPT_SHADOW)
        hline(sheet, ox + 9, ox + 11, band_y, ADAPT_SHADOW)
        px(sheet, ox + 6, band_y, ADAPT_LIGHT)
        px(sheet, ox + 10, band_y, ADAPT_LIGHT)
    for detail_y in (11, 19):
        px(sheet, ox + 6, detail_y, ADAPT_LIGHT)
        px(sheet, ox + 10, detail_y, ADAPT_LIGHT)
    hline(sheet, ox + 5, ox + 11, 26, ADAPT_DEEP)

    # Symmetric stone studs and a shallow, centered threshold finish the shrine-like silhouette.
    for x, y in [(3, 11), (13, 11), (3, 18), (13, 18), (3, 25), (13, 25)]:
        px(sheet, ox + x, y, ADAPT_GLEAM)
    hline(sheet, ox + 4, ox + 12, 27, ADAPT_DEEP)
    hline(sheet, ox + 3, ox + 13, 28, ADAPT_DARK)
    hline(sheet, ox + 5, ox + 11, 29, ADAPT_MID)
    hline(sheet, ox + 6, ox + 10, 30, edge_mid)


def draw_elevator_glow(sheet: Image.Image, origin_x: int, active: bool) -> None:
    """Draw only the untinted Qi light, leaving the adaptive stone body transparent."""
    ox = origin_x
    if active:
        px(sheet, ox + 8, 1, with_alpha(QI_GLOW, 112))
        px(sheet, ox + 7, 2, with_alpha(QI_PURPLE, 112))
        px(sheet, ox + 9, 2, with_alpha(QI_PURPLE, 112))
        px(sheet, ox + 8, 2, QI_WHITE)
        hline(sheet, ox + 7, ox + 9, 3, QI_VIOLET)
        px(sheet, ox + 8, 3, QI_WHITE)
        hline(sheet, ox + 7, ox + 9, 4, QI_PURPLE)
        px(sheet, ox + 8, 4, QI_GLOW)
        px(sheet, ox + 8, 5, QI_DARK)
        px(sheet, ox + 8, 13, with_alpha(QI_PURPLE, 168))
        px(sheet, ox + 8, 14, QI_VIOLET)
        px(sheet, ox + 8, 15, QI_GLOW)
        px(sheet, ox + 8, 16, QI_LIGHT)
        px(sheet, ox + 8, 17, with_alpha(QI_PURPLE, 168))
        px(sheet, ox + 4, 6, with_alpha(QI_GLOW, 64))
        px(sheet, ox + 12, 6, with_alpha(QI_GLOW, 64))
    else:
        px(sheet, ox + 8, 2, QI_DARK)
        hline(sheet, ox + 7, ox + 9, 3, QI_DARK)
        px(sheet, ox + 8, 3, QI_PURPLE)
        px(sheet, ox + 8, 4, QI_DARK)


def draw_foyer_elevator(sheet: Image.Image, origin_x: int, origin_y: int) -> None:
    """Draw the entrance-only lift from the exact vanilla Skull Cavern door/wall palette."""
    ox, oy = origin_x, origin_y
    edge_faint = with_alpha(CAVE_SHADOW, 64)
    edge_soft = with_alpha(CAVE_SHADOW, 112)
    edge_stone = with_alpha(CAVE_BRONZE, 168)

    # A soft, mirrored recess lets the patched wall remain visible through the perimeter. The frame
    # is deliberately narrower than the adaptive lift and reads as something buried in the rock.
    hline(sheet, ox + 7, ox + 9, oy + 1, edge_faint)
    hline(sheet, ox + 5, ox + 11, oy + 2, edge_soft)
    hline(sheet, ox + 4, ox + 12, oy + 3, edge_stone)
    for x, y, color in [
        (3, 4, edge_soft), (13, 4, edge_soft),
        (2, 7, edge_faint), (14, 7, edge_faint),
        (2, 12, edge_soft), (14, 12, edge_soft),
        (1, 18, edge_faint), (15, 18, edge_faint),
        (2, 24, edge_soft), (14, 24, edge_soft),
        (3, 29, edge_faint), (13, 29, edge_faint),
        (6, 31, edge_faint), (10, 31, edge_faint),
    ]:
        px(sheet, ox + x, oy + y, color)

    # Low ochre cap and deep socket. There are no pale structural pixels: the tiny Qi layer below
    # supplies all supernatural light without turning the doorway cream or salmon.
    hline(sheet, ox + 7, ox + 9, oy + 2, OUTLINE)
    hline(sheet, ox + 5, ox + 11, oy + 3, CAVE_SHADOW)
    hline(sheet, ox + 4, ox + 12, oy + 4, OUTLINE)
    hline(sheet, ox + 3, ox + 13, oy + 5, DEEP_WOOD)
    hline(sheet, ox + 4, ox + 12, oy + 6, DARK_WOOD)
    hline(sheet, ox + 6, ox + 10, oy + 6, CAVE_BRONZE)
    px(sheet, ox + 4, oy + 5, CAVE_BRONZE)
    px(sheet, ox + 12, oy + 5, CAVE_BRONZE)

    # Stone-buried jambs share the vanilla door's values and keep their highlights sparse and broken.
    rect(sheet, ox + 3, oy + 7, ox + 5, oy + 27, OUTLINE)
    vline(sheet, ox + 3, oy + 8, oy + 26, CAVE_SHADOW)
    vline(sheet, ox + 4, oy + 7, oy + 27, DARK_WOOD)
    vline(sheet, ox + 5, oy + 8, oy + 26, WOOD)
    rect(sheet, ox + 11, oy + 7, ox + 13, oy + 27, OUTLINE)
    vline(sheet, ox + 11, oy + 8, oy + 26, WOOD)
    vline(sheet, ox + 12, oy + 7, oy + 27, DARK_WOOD)
    vline(sheet, ox + 13, oy + 8, oy + 26, CAVE_SHADOW)
    for y in (9, 16, 24):
        px(sheet, ox + 5, oy + y, AMBER)
        px(sheet, ox + 11, oy + y, AMBER)
    for y in (12, 21):
        px(sheet, ox + 3, oy + y, CAVE_BRONZE)
        px(sheet, ox + 13, oy + y, CAVE_BRONZE)

    # Dark twin doors: recognizable elevator seam and panels, without the old continuous light bars.
    rect(sheet, ox + 6, oy + 7, ox + 10, oy + 27, DEEP_WOOD)
    vline(sheet, ox + 6, oy + 8, oy + 26, DARK_WOOD)
    vline(sheet, ox + 7, oy + 8, oy + 26, WOOD)
    vline(sheet, ox + 8, oy + 7, oy + 27, INK)
    vline(sheet, ox + 9, oy + 8, oy + 26, WOOD)
    vline(sheet, ox + 10, oy + 8, oy + 26, DARK_WOOD)
    for band_y in (13, 20, 26):
        hline(sheet, ox + 6, ox + 7, oy + band_y, OUTLINE)
        hline(sheet, ox + 9, ox + 10, oy + band_y, OUTLINE)
    for detail_y in (10, 17, 23):
        px(sheet, ox + 7, oy + detail_y, AMBER)
        px(sheet, ox + 9, oy + detail_y, AMBER)

    # A heavy recessed threshold and translucent dust pixels visually bury the fixture in the wall.
    hline(sheet, ox + 3, ox + 13, oy + 28, OUTLINE)
    hline(sheet, ox + 4, ox + 12, oy + 29, DARK_WOOD)
    hline(sheet, ox + 5, ox + 11, oy + 30, CAVE_BRONZE)
    px(sheet, ox + 4, oy + 30, edge_stone)
    px(sheet, ox + 12, oy + 30, edge_stone)


def draw_foyer_glow(sheet: Image.Image, origin_x: int, origin_y: int, active: bool) -> None:
    """Keep the foyer's Qi presence jewel-sized so it doesn't compete with the room's lamp."""
    ox, oy = origin_x, origin_y
    if active:
        px(sheet, ox + 8, oy + 1, with_alpha(QI_GLOW, 64))
        px(sheet, ox + 7, oy + 2, with_alpha(QI_DARK, 112))
        px(sheet, ox + 9, oy + 2, with_alpha(QI_DARK, 112))
        px(sheet, ox + 8, oy + 2, QI_LIGHT)
        hline(sheet, ox + 7, ox + 9, oy + 3, QI_DARK)
        px(sheet, ox + 8, oy + 3, QI_VIOLET)
        px(sheet, ox + 8, oy + 4, QI_PURPLE)
        px(sheet, ox + 8, oy + 16, with_alpha(QI_DARK, 112))
        px(sheet, ox + 8, oy + 17, QI_PURPLE)
    else:
        px(sheet, ox + 8, oy + 2, with_alpha(QI_DARK, 168))
        px(sheet, ox + 8, oy + 3, QI_DARK)


def draw_broken_foyer_elevator(sheet: Image.Image, origin_x: int, origin_y: int) -> None:
    """Derive a dormant, visibly repairable foyer lift without changing its outer footprint."""
    ox, oy = origin_x, origin_y
    repaired = sheet.crop((FOYER_ELEVATOR[0], FOYER_ELEVATOR[1], FOYER_ELEVATOR[0] + 17, FOYER_ELEVATOR[1] + 32))
    sheet.alpha_composite(repaired, (ox, oy))

    # Split the cap and dead socket. The gaps reveal the real wall instead of painting fake darkness.
    px(sheet, ox + 8, oy + 1, TRANSPARENT)
    px(sheet, ox + 8, oy + 2, INK)
    px(sheet, ox + 7, oy + 3, TRANSPARENT)
    px(sheet, ox + 8, oy + 3, OUTLINE)
    px(sheet, ox + 9, oy + 4, TRANSPARENT)
    px(sheet, ox + 10, oy + 4, CAVE_SHADOW)
    px(sheet, ox + 9, oy + 5, INK)

    # Missing upper slats and a jagged center seam make the failure readable at game zoom.
    for y in range(9, 15):
        px(sheet, ox + 7, oy + y, DEEP_WOOD if y % 2 else INK)
    for y in range(14, 21):
        px(sheet, ox + 8, oy + y, INK)
    px(sheet, ox + 7, oy + 15, OUTLINE)
    px(sheet, ox + 9, oy + 16, OUTLINE)
    px(sheet, ox + 7, oy + 19, CAVE_SHADOW)

    # The right inner rail has buckled into a diagonal and fallen across the threshold.
    for y in range(17, 26):
        px(sheet, ox + 9, oy + y, DEEP_WOOD)
    for x, y, color in [
        (9, 18, DARK_WOOD), (10, 19, WOOD), (10, 20, AMBER),
        (11, 21, WOOD), (11, 22, AMBER), (12, 23, DARK_WOOD),
        (7, 25, DARK_WOOD), (8, 26, WOOD), (9, 27, AMBER),
        (10, 28, WOOD), (11, 29, DARK_WOOD),
    ]:
        px(sheet, ox + x, oy + y, color)

    # A handful of loose fragments anchors the damage without growing into environmental clutter.
    px(sheet, ox + 3, oy + 29, with_alpha(CAVE_SHADOW, 168))
    px(sheet, ox + 2, oy + 30, with_alpha(CAVE_BRONZE, 112))
    px(sheet, ox + 13, oy + 30, with_alpha(CAVE_SHADOW, 168))
    px(sheet, ox + 14, oy + 31, with_alpha(CAVE_BRONZE, 64))


def draw_broken_foyer_glow(sheet: Image.Image, origin_x: int, origin_y: int) -> None:
    """One nearly dead socket pixel, pulsed very faintly by code."""
    px(sheet, origin_x + 8, origin_y + 3, with_alpha(QI_DARK, 112))


def draw_repair_overlay(sheet: Image.Image, origin_x: int, origin_y: int, frame: int) -> None:
    """Native-pixel dust/energy overlays for two impacts and the final seam ignition."""
    ox, oy = origin_x, origin_y
    if frame == 0:
        for x, y, color in [
            (3, 4, with_alpha(CAVE_DUST, 168)), (2, 5, with_alpha(CAVE_BRONZE, 112)),
            (1, 7, with_alpha(CAVE_SHADOW, 64)), (5, 2, GOLD), (6, 1, GOLD_GLEAM),
            (11, 6, with_alpha(CAVE_DUST, 112)), (14, 8, with_alpha(CAVE_SHADOW, 64)),
            (4, 28, with_alpha(CAVE_BRONZE, 112)), (2, 30, with_alpha(CAVE_SHADOW, 64)),
        ]:
            px(sheet, ox + x, oy + y, color)
    elif frame == 1:
        for x, y, color in [
            (13, 13, with_alpha(CAVE_DUST, 168)), (15, 14, with_alpha(CAVE_SHADOW, 64)),
            (12, 16, GOLD), (14, 17, GOLD_GLEAM), (10, 19, with_alpha(CAVE_BRONZE, 112)),
            (3, 22, with_alpha(CAVE_DUST, 112)), (1, 24, with_alpha(CAVE_SHADOW, 64)),
            (12, 28, with_alpha(CAVE_BRONZE, 112)), (14, 30, with_alpha(CAVE_SHADOW, 64)),
        ]:
            px(sheet, ox + x, oy + y, color)
    else:
        px(sheet, ox + 8, oy + 1, with_alpha(QI_GLOW, 64))
        hline(sheet, ox + 7, ox + 9, oy + 2, with_alpha(QI_PURPLE, 112))
        px(sheet, ox + 8, oy + 2, QI_LIGHT)
        px(sheet, ox + 8, oy + 3, QI_VIOLET)
        for y in range(4, 27):
            alpha = 255 if y % 5 == 0 else 168 if y % 2 else 112
            px(sheet, ox + 8, oy + y, with_alpha(QI_PURPLE, alpha))
        px(sheet, ox + 7, oy + 16, with_alpha(QI_GLOW, 64))
        px(sheet, ox + 9, oy + 16, with_alpha(QI_GLOW, 64))


def draw_gauge(sheet: Image.Image) -> None:
    # Top cap: a carved sandstone bracket with one restrained living crystal pixel.
    ox, oy, _, _ = GAUGE_TOP
    px(sheet, ox + 3, oy, QI_DARK)
    px(sheet, ox + 4, oy, QI_PURPLE)
    hline(sheet, ox + 2, ox + 5, oy + 1, SAND_DUST)
    hline(sheet, ox + 1, ox + 6, oy + 2, DESERT_MID)
    hline(sheet, ox + 2, ox + 5, oy + 3, CAVE_SHADOW)

    # Repeatable middle: both rows are identical, so every boundary tiles perfectly.
    ox, oy, _, _ = GAUGE_MIDDLE
    middle_row = [TRANSPARENT, TRANSPARENT, CAVE_SHADOW, DESERT_SHADOW, DESERT_SHADOW, CAVE_SHADOW, TRANSPARENT, TRANSPARENT]
    for y in range(2):
        for x, color in enumerate(middle_row):
            px(sheet, ox + x, oy + y, color)

    # Bottom cap is heavier, giving the narrow rail a physical anchor.
    ox, oy, _, _ = GAUGE_BOTTOM
    hline(sheet, ox + 2, ox + 5, oy, CAVE_SHADOW)
    hline(sheet, ox + 1, ox + 6, oy + 1, DESERT_MID)
    hline(sheet, ox, ox + 7, oy + 2, DESERT_SHADOW)
    hline(sheet, ox + 2, ox + 5, oy + 3, SAND_DUST)

    # Current-depth handle: an asymmetric clamp with a small glowing grip on its right tip.
    ox, oy, _, _ = GAUGE_MARKER
    hline(sheet, ox + 3, ox + 6, oy, SAND_DUST)
    hline(sheet, ox + 2, ox + 7, oy + 1, CAVE_SHADOW)
    hline(sheet, ox, ox + 8, oy + 2, DESERT_MID)
    px(sheet, ox + 4, oy + 2, QI_DARK)
    px(sheet, ox + 5, oy + 2, QI_PURPLE)
    px(sheet, ox + 9, oy + 2, QI_GLOW)
    hline(sheet, ox + 2, ox + 8, oy + 3, CAVE_SHADOW)
    hline(sheet, ox + 4, ox + 6, oy + 4, DESERT_SHADOW)

    # All-time record: deliberately lighter and thinner than the current marker.
    ox, oy, _, _ = GAUGE_RECORD
    px(sheet, ox + 1, oy + 1, CAVE_SHADOW)
    px(sheet, ox + 8, oy + 1, CAVE_SHADOW)
    hline(sheet, ox + 2, ox + 7, oy + 1, QI_DARK)
    px(sheet, ox + 4, oy, SAND_LIGHT)
    px(sheet, ox + 5, oy, QI_PURPLE)

    # Tiny 8x8 inset lift glyph shares the full fixture's pale frame and single seam.
    ox, oy, _, _ = GAUGE_ICON
    px(sheet, ox + 3, oy, QI_DARK)
    px(sheet, ox + 4, oy, QI_PURPLE)
    hline(sheet, ox + 2, ox + 5, oy + 1, SAND_DUST)
    rect(sheet, ox + 1, oy + 2, ox + 6, oy + 7, DESERT_SHADOW)
    rect(sheet, ox + 2, oy + 3, ox + 5, oy + 6, DESERT_MID)
    vline(sheet, ox + 3, oy + 3, oy + 6, SAND_LIGHT)
    vline(sheet, ox + 4, oy + 3, oy + 6, CAVE_SHADOW)
    hline(sheet, ox + 1, ox + 6, oy + 7, DESERT_MID)

    # Continuous two-row energy weave; code still tiles this exact modular swatch.
    ox, oy, _, _ = GAUGE_FILL
    rect(sheet, ox, oy, ox + 3, oy + 1, TRANSPARENT)
    px(sheet, ox + 1, oy, QI_DARK)
    px(sheet, ox + 2, oy, QI_LIGHT)
    px(sheet, ox + 1, oy + 1, DESERT_MID)
    px(sheet, ox + 2, oy + 1, QI_PURPLE)


def checker_preview(image: Image.Image, factor: int = 8) -> Image.Image:
    background = Image.new("RGBA", image.size, (224, 224, 224, 255))
    draw = ImageDraw.Draw(background)
    for y in range(0, image.height, 4):
        for x in range(0, image.width, 4):
            if (x // 4 + y // 4) % 2:
                draw.rectangle((x, y, x + 3, y + 3), fill=(184, 184, 184, 255))
    background.alpha_composite(image)
    return background.resize((image.width * factor, image.height * factor), Image.Resampling.NEAREST)


def multiply_tint(image: Image.Image, tint: tuple[int, int, int]) -> Image.Image:
    result = Image.new("RGBA", image.size, TRANSPARENT)
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = image.getpixel((x, y))
            result.putpixel((x, y), (r * tint[0] // 255, g * tint[1] // 255, b * tint[2] // 255, a))
    return result


def compose_foyer_preview(sheet: Image.Image) -> Image.Image:
    foyer = Image.open(REFERENCES / "vanilla-skullcave-map.png").convert("RGBA")
    foyer = foyer.resize((foyer.width * 4, foyer.height * 4), Image.Resampling.NEAREST)
    body = sheet.crop((30, 32, 47, 64))
    glow = sheet.crop((64, 32, 81, 64))
    elevator = body.resize((68, 128), Image.Resampling.NEAREST)
    elevator.alpha_composite(glow.resize((68, 128), Image.Resampling.NEAREST))
    # Tile (4,2), centered in the foyer's two-tile niche at exact world-pixel precision.
    foyer.alpha_composite(elevator, (286, 128))
    return foyer


def compose_broken_foyer_preview(sheet: Image.Image) -> Image.Image:
    foyer = Image.open(REFERENCES / "vanilla-skullcave-map.png").convert("RGBA")
    foyer = foyer.resize((foyer.width * 4, foyer.height * 4), Image.Resampling.NEAREST)
    body = sheet.crop((81, 32, 98, 64))
    glow = sheet.crop((85, 0, 102, 32))
    elevator = body.resize((68, 128), Image.Resampling.NEAREST)
    elevator.alpha_composite(glow.resize((68, 128), Image.Resampling.NEAREST))
    foyer.alpha_composite(elevator, (286, 128))
    return foyer


def compose_repair_sequence(sheet: Image.Image) -> Image.Image:
    """Make a four-state native-scale contact sheet for visual regression checking."""
    states = [
        ((81, 32, 98, 64), (85, 0, 102, 32)),
        ((81, 32, 98, 64), (0, 64, 17, 96)),
        ((81, 32, 98, 64), (17, 64, 34, 96)),
        ((30, 32, 47, 64), (34, 64, 51, 96)),
    ]
    crops: list[Image.Image] = []
    for body_box, overlay_box in states:
        foyer = Image.open(REFERENCES / "vanilla-skullcave-map.png").convert("RGBA")
        foyer = foyer.resize((foyer.width * 4, foyer.height * 4), Image.Resampling.NEAREST)
        body = sheet.crop(body_box).resize((68, 128), Image.Resampling.NEAREST)
        overlay = sheet.crop(overlay_box).resize((68, 128), Image.Resampling.NEAREST)
        body.alpha_composite(overlay)
        foyer.alpha_composite(body, (286, 128))
        crops.append(foyer.crop((184, 72, 416, 304)))

    sequence = Image.new("RGBA", (crops[0].width * len(crops), crops[0].height), (12, 9, 15, 255))
    for index, crop in enumerate(crops):
        sequence.alpha_composite(crop, (index * crop.width, 0))
    return sequence


def compose_gauge_preview(sheet: Image.Image) -> Image.Image:
    native = Image.new("RGBA", (80, 72), (12, 9, 15, 255))
    top = sheet.crop((0, 32, 8, 36))
    middle = sheet.crop((0, 36, 8, 38))
    bottom = sheet.crop((0, 38, 8, 42))
    marker = sheet.crop((8, 32, 18, 37))
    record = sheet.crop((8, 37, 18, 40))
    icon = sheet.crop((18, 32, 26, 40))
    fill = sheet.crop((26, 32, 30, 34))

    x, y = 8, 12
    native.alpha_composite(icon, (x, 2))
    native.alpha_composite(top, (x, y))
    for row in range(24):
        native.alpha_composite(middle, (x, y + 4 + row * 2))
    native.alpha_composite(bottom, (x, y + 52))
    for row in range(14):
        native.alpha_composite(fill, (x + 2, y + 4 + row * 2))
    native.alpha_composite(record, (x - 1, y + 22))
    native.alpha_composite(marker, (x - 1, y + 30))
    return native.resize((native.width * 3, native.height * 3), Image.Resampling.NEAREST)


def validate(sheet: Image.Image) -> None:
    assert sheet.size == (112, 96)
    assert sheet.mode == "RGBA"
    assert sheet.getpixel((79, 63))[3] == 0

    idle = sheet.crop((0, 0, 17, 32))
    active = sheet.crop((17, 0, 34, 32))
    glow_idle = sheet.crop((34, 0, 51, 32))
    glow_active = sheet.crop((51, 0, 68, 32))
    foyer = sheet.crop((30, 32, 47, 64))
    foyer_glow_idle = sheet.crop((47, 32, 64, 64))
    foyer_glow_active = sheet.crop((64, 32, 81, 64))
    foyer_broken = sheet.crop((81, 32, 98, 64))
    foyer_broken_glow = sheet.crop((85, 0, 102, 32))
    assert idle.getbbox() == (0, 2, 17, 32), idle.getbbox()
    assert active.getbbox() == (0, 2, 17, 32), active.getbbox()
    assert glow_idle.getbbox() == (7, 2, 10, 5), glow_idle.getbbox()
    assert glow_active.getbbox() == (4, 1, 13, 18), glow_active.getbbox()
    assert list(idle.getdata()) == list(active.getdata())
    assert foyer.getbbox() == (1, 1, 16, 32), foyer.getbbox()
    assert foyer_glow_idle.getbbox() == (8, 2, 9, 4), foyer_glow_idle.getbbox()
    assert foyer_glow_active.getbbox() == (7, 1, 10, 18), foyer_glow_active.getbbox()
    assert foyer_broken.getbbox() == (1, 1, 16, 32), foyer_broken.getbbox()
    assert foyer_broken_glow.getbbox() == (8, 3, 9, 4), foyer_broken_glow.getbbox()

    # Hard regression checks: every material pixel mirrors around x=8 and the seam is exactly one pixel.
    for y in range(idle.height):
        for x in range(idle.width):
            assert idle.getpixel((x, y)) == idle.getpixel((16 - x, y)), (x, y)
    assert idle.getpixel((8, 12)) == ADAPT_DEEP
    assert idle.getpixel((7, 12)) == ADAPT_MID
    assert idle.getpixel((9, 12)) == ADAPT_MID

    # The foyer frame stays structurally even while its translucent perimeter dissolves into rock.
    for y in range(foyer.height):
        for x in range(foyer.width):
            assert foyer.getpixel((x, y)) == foyer.getpixel((16 - x, y)), (x, y)
    assert max(pixel[0] for pixel in foyer.getdata()) <= AMBER[0]
    assert sum(pixel[3] < 255 for pixel in foyer.getdata() if pixel[3] > 0) >= 20

    middle = sheet.crop((0, 36, 8, 38))
    assert list(middle.crop((0, 0, 8, 1)).getdata()) == list(middle.crop((0, 1, 8, 2)).getdata())

    alphas = {pixel[3] for pixel in sheet.getdata()}
    expected_alphas = {0, 64, 112, 168, 255}
    assert alphas == expected_alphas, f"Unexpected alpha values found: {alphas}"


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    REFERENCES.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (112, 96), TRANSPARENT)
    draw_elevator(sheet, 0)
    draw_elevator(sheet, 17)
    draw_elevator_glow(sheet, 34, active=False)
    draw_elevator_glow(sheet, 51, active=True)
    draw_gauge(sheet)
    draw_foyer_elevator(sheet, 30, 32)
    draw_foyer_glow(sheet, 47, 32, active=False)
    draw_foyer_glow(sheet, 64, 32, active=True)
    draw_broken_foyer_elevator(sheet, 81, 32)
    draw_broken_foyer_glow(sheet, 85, 0)
    draw_repair_overlay(sheet, 0, 64, frame=0)
    draw_repair_overlay(sheet, 17, 64, frame=1)
    draw_repair_overlay(sheet, 34, 64, frame=2)
    validate(sheet)

    asset_path = ASSETS / "qfe-sprites.png"
    sheet.save(asset_path, optimize=True)
    checker_preview(sheet).save(REFERENCES / "qfe-sprites-8x.png")
    compose_foyer_preview(sheet).save(REFERENCES / "qfe-elevator-in-vanilla-4x.png")
    compose_broken_foyer_preview(sheet).save(REFERENCES / "qfe-elevator-broken-in-vanilla-4x.png")
    compose_repair_sequence(sheet).save(REFERENCES / "qfe-repair-sequence-4x.png")
    compose_gauge_preview(sheet).save(REFERENCES / "qfe-gauge-preview-4x.png")
    print(f"Wrote {asset_path}")
    print("Validated: 112x96 RGBA, repaired/broken foyer lifts, three repair overlays, adaptive lift, seamless gauge")


if __name__ == "__main__":
    main()
