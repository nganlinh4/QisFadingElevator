"""Generate assets/qfe-sprites.png plus reference previews.

Sheet layout (native pixels, 176x96): row 0 holds the nine 17x32 lift sprites, row 1 the repair
frames plus bloom/sparks/motes, row 2 the modular gauge. Exact origins are the constants below,
mirrored by SpriteSheet.cs.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image

import qfe_previews
from pixel_kit import (
    TRANSPARENT, RGBA, px, hline, vline, rect, with_alpha, mute, crop, checker_preview,
)

PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / "assets"
REFERENCES = PROJECT / "references"

# Sampled from vanilla mine_desert.png / Skull Cavern door / Qi gem.
INK = (22, 19, 13, 255)
OUTLINE = (62, 42, 39, 255)
DEEP_WOOD = (55, 30, 17, 255)
DARK_WOOD = (98, 59, 39, 255)
WOOD = (122, 67, 38, 255)
AMBER = (149, 87, 25, 255)
GOLD_DARK = (168, 110, 23, 255)
GOLD = (183, 129, 58, 255)
GOLD_LIGHT = (203, 151, 82, 255)
GOLD_GLEAM = (232, 170, 76, 255)
CAVE_SHADOW = (78, 46, 43, 255)
CAVE_BRONZE = (118, 83, 36, 255)
CAVE_DUST = (132, 99, 46, 255)
DESERT_SHADOW = (116, 85, 56, 255)
DESERT_MID = (151, 110, 71, 255)
DESERT_LIGHT = (175, 128, 98, 255)
SAND_DUST = (152, 140, 100, 255)
SAND_LIGHT = (173, 158, 110, 255)
SAND_PALE = (189, 164, 132, 255)
ABYSS = (24, 16, 32, 255)
# Bone plate for the crown dial; pale enough to read on every wall without being HUD-white.
BONE_PALE = (214, 192, 156, 255)
BONE = (186, 160, 122, 255)
BONE_SHADOW = (129, 104, 74, 255)
COPPER = (196, 106, 56, 255)
COPPER_DARK = (135, 66, 38, 255)
FLASH_WHITE = (255, 250, 226, 255)
SPARK_WHITE = (240, 252, 255, 255)
SPARK_BLUE = (138, 214, 255, 255)
QI_DARK = (64, 19, 153, 255)
QI_PURPLE = (106, 51, 255, 255)
QI_VIOLET = (156, 41, 255, 255)
QI_LIGHT = (127, 123, 255, 255)
QI_GLOW = (220, 173, 255, 255)
QI_WHITE = (255, 234, 255, 255)
# Neutral rock greys for the mine shroud; runtime multiplies these by the sampled wall tint.
G_SHADOW = (126, 126, 126, 255)
G_MID = (168, 168, 168, 255)
G_LIGHT = (204, 204, 204, 255)
G_GLEAM = (230, 230, 230, 255)

SHEET_SIZE = (176, 96)
LIFT = (17, 32)

MINE_BODY = (0, 0)
MINE_SHROUD = (17, 0)
MINE_GLOW_IDLE = (34, 0)
MINE_GLOW_ACTIVE = (51, 0)
FOYER_BODY = (68, 0)
FOYER_GLOW_IDLE = (85, 0)
FOYER_GLOW_ACTIVE = (102, 0)
FOYER_BROKEN = (119, 0)
FOYER_BROKEN_GLOW = (136, 0)
FLASH_A = (0, 32)
FLASH_B = (17, 32)
FLASH_C = (34, 32)
SEAM = (51, 32)
FLARE_A = (68, 32)
FLARE_B = (85, 32)
BLOOM = (102, 32, 33, 32)
SPARK_A = (136, 32, 5, 5)
SPARK_B = (141, 32, 5, 5)
MOTE_A = (148, 32, 3, 3)
MOTE_B = (151, 32, 3, 3)
GAUGE_TOP = (0, 64, 12, 6)
GAUGE_MIDDLE = (0, 70, 12, 2)
GAUGE_BOTTOM = (0, 72, 12, 6)
GAUGE_MARKER = (16, 64, 14, 5)
GAUGE_ICON = (16, 70, 10, 10)
GAUGE_FILL = (32, 64, 4, 2)
GAUGE_EMBER = (40, 64, 3, 3)


def lift_palette(adaptive: bool) -> dict[str, RGBA]:
    """Foyer uses the raw sampled palette; the mine machine is gently muted so the runtime
    lerp toward the sampled wall tint can add room color without turning it neon."""
    base = {
        "outline": OUTLINE, "ink": INK,
        "frame_mid": WOOD, "frame_dark": DARK_WOOD,
        "slat_dark": DEEP_WOOD, "slat_mid": DARK_WOOD, "slat_light": WOOD, "grain": AMBER,
        "strap": GOLD_DARK, "strap_gleam": GOLD,
        "kick": GOLD_DARK, "kick_frame": AMBER,
        "plate_hi": BONE_PALE, "plate_mid": BONE, "plate_shadow": BONE_SHADOW,
        "thresh_mid": DARK_WOOD, "thresh_dark": DEEP_WOOD, "thresh_low": CAVE_BRONZE,
        "glint": AMBER,
    }
    if adaptive:
        return {key: mute(color, 0.30) for key, color in base.items()}
    return base


def draw_lift_body(sheet: Image.Image, origin: tuple[int, int], pal: dict[str, RGBA]) -> None:
    """The machine itself: crown dial, lintel, twin slatted doors, straps, kick plates, threshold.
    Mirrored around x=8; asymmetric touches only recolor pixels so the alpha mask stays balanced."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def h(x1: int, x2: int, y: int, color: RGBA) -> None:
        hline(sheet, ox + x1, ox + x2, oy + y, color)

    # Crown: a bone half-disc dial (vanilla elevators carry a dial here) with a dark Qi socket.
    h(6, 10, 1, pal["plate_shadow"])
    p(5, 2, pal["plate_shadow"]); h(6, 10, 2, pal["plate_hi"]); p(11, 2, pal["plate_shadow"])
    p(8, 2, pal["ink"])
    p(4, 3, pal["plate_shadow"]); p(5, 3, pal["plate_hi"]); p(6, 3, pal["plate_hi"])
    h(7, 9, 3, pal["ink"])
    p(10, 3, pal["plate_mid"]); p(11, 3, pal["plate_mid"]); p(12, 3, pal["plate_shadow"])
    p(4, 4, pal["plate_shadow"]); h(5, 7, 4, pal["plate_mid"]); p(8, 4, pal["ink"])
    h(9, 11, 4, pal["plate_mid"]); p(12, 4, pal["plate_shadow"])
    h(4, 12, 5, pal["plate_shadow"])

    # Lintel beam with a lighter inset strip, then its shadow.
    h(3, 13, 6, pal["frame_dark"]); h(5, 11, 6, pal["frame_mid"])
    h(3, 13, 7, pal["outline"])

    # Jambs: outline edge + lit inner face (left) / shaded inner face (right).
    vline(sheet, ox + 2, oy + 8, oy + 26, pal["outline"])
    vline(sheet, ox + 3, oy + 8, oy + 26, pal["frame_mid"])
    vline(sheet, ox + 13, oy + 8, oy + 26, pal["frame_dark"])
    vline(sheet, ox + 14, oy + 8, oy + 26, pal["outline"])
    for y in (10, 17, 24):
        p(3, y, pal["glint"])
    for y in (12, 20):
        p(13, y, pal["glint"])

    # Twin doors: vertical slat grain, a true one-pixel seam, two straps, kick plates.
    h(4, 12, 8, pal["ink"])
    for y in range(9, 26):
        p(4, y, pal["slat_dark"]); p(5, y, pal["slat_mid"]); p(6, y, pal["slat_dark"]); p(7, y, pal["slat_mid"])
        p(8, y, pal["ink"])
        p(9, y, pal["slat_mid"]); p(10, y, pal["slat_dark"]); p(11, y, pal["slat_mid"]); p(12, y, pal["slat_dark"])
    for x, y in ((5, 10), (11, 15), (7, 17), (9, 11)):
        p(x, y, pal["slat_light"])
    for y in (13, 19):
        h(4, 7, y, pal["strap"]); h(9, 12, y, pal["strap"])
        p(4, y, pal["strap_gleam"]); p(9, y, pal["strap_gleam"])
    for x1, x2 in ((4, 7), (9, 12)):
        rect(sheet, ox + x1, oy + 21, ox + x2, oy + 25, pal["kick_frame"])
        rect(sheet, ox + x1 + 1, oy + 22, ox + x2 - 1, oy + 24, pal["kick"])
    p(5, 22, pal["strap_gleam"]); p(10, 22, pal["strap_gleam"])
    h(4, 12, 26, pal["ink"])

    # Copper power node on the right jamb: the battery seat (recolors jamb pixels only).
    p(13, 23, COPPER); p(13, 24, COPPER_DARK)

    # Threshold steps sink the machine into the floor line.
    h(3, 13, 27, pal["outline"])
    h(2, 14, 28, pal["thresh_mid"])
    h(3, 13, 29, pal["thresh_dark"])
    h(5, 11, 30, pal["thresh_low"])


def draw_foyer_dressing(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Carved rock shoulders and translucent buried edges, hand-colored for the fixed foyer wall."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)
        px(sheet, ox + (16 - x), oy + y, color)

    p(5, 1, with_alpha(CAVE_SHADOW, 112))
    p(3, 3, with_alpha(CAVE_BRONZE, 168))
    p(2, 4, with_alpha(CAVE_SHADOW, 112)); p(3, 4, CAVE_BRONZE)
    p(2, 5, CAVE_BRONZE); p(1, 5, with_alpha(CAVE_SHADOW, 64))
    p(2, 6, with_alpha(CAVE_BRONZE, 168)); p(2, 7, with_alpha(CAVE_SHADOW, 112))
    p(1, 9, with_alpha(CAVE_SHADOW, 112))
    p(0, 13, with_alpha(CAVE_SHADOW, 64))
    p(1, 14, CAVE_BRONZE); p(0, 14, with_alpha(CAVE_BRONZE, 112))
    p(1, 15, with_alpha(CAVE_SHADOW, 168))
    p(1, 19, with_alpha(CAVE_SHADOW, 64))
    p(1, 23, with_alpha(CAVE_BRONZE, 112))
    p(1, 26, with_alpha(CAVE_SHADOW, 112))
    p(1, 28, with_alpha(CAVE_BRONZE, 168)); p(0, 28, with_alpha(CAVE_SHADOW, 64))
    p(2, 29, CAVE_BRONZE); p(1, 29, with_alpha(CAVE_SHADOW, 112))
    p(3, 30, with_alpha(CAVE_BRONZE, 168)); p(4, 30, CAVE_DUST)
    p(4, 31, with_alpha(CAVE_SHADOW, 112)); p(6, 31, with_alpha(CAVE_DUST, 64))
    px(sheet, ox + 8, oy + 31, with_alpha(CAVE_SHADOW, 64))


def draw_mine_shroud(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Neutral-grey rock collar drawn over the machine's silhouette; runtime multiplies it by the
    sampled wall tint so generated rooms visibly grow around the fixture."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)
        px(sheet, ox + (16 - x), oy + y, color)

    p(5, 1, with_alpha(G_MID, 112))
    p(3, 3, with_alpha(G_MID, 168))
    p(2, 4, with_alpha(G_SHADOW, 112)); p(3, 4, G_MID)
    p(2, 5, G_MID); p(1, 5, with_alpha(G_SHADOW, 64)); p(3, 5, G_LIGHT)
    p(2, 6, with_alpha(G_MID, 168)); p(2, 7, with_alpha(G_SHADOW, 112))
    p(0, 9, with_alpha(G_MID, 64)); p(1, 9, G_MID); p(2, 9, with_alpha(G_SHADOW, 168))
    p(1, 10, G_SHADOW)
    p(0, 14, with_alpha(G_MID, 112)); p(1, 14, G_LIGHT); p(2, 14, with_alpha(G_SHADOW, 112))
    p(1, 15, G_MID); p(0, 15, with_alpha(G_SHADOW, 64))
    p(1, 20, G_MID); p(0, 20, with_alpha(G_MID, 64)); p(1, 21, with_alpha(G_SHADOW, 168))
    p(2, 27, with_alpha(G_SHADOW, 112))
    p(1, 28, G_MID); p(0, 28, with_alpha(G_MID, 64))
    p(2, 29, G_SHADOW); p(1, 29, with_alpha(G_SHADOW, 112))
    p(4, 30, G_MID); p(3, 30, with_alpha(G_MID, 112))
    p(5, 31, with_alpha(G_SHADOW, 112)); p(7, 31, with_alpha(G_MID, 64))
    p(2, 10, with_alpha(G_GLEAM, 64))


def draw_glow(sheet: Image.Image, origin: tuple[int, int], active: bool) -> None:
    """Only untinted Qi light: the crown crystal, and when active a living seam plus floor spill."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    if not active:
        p(8, 2, with_alpha(QI_DARK, 168))
        p(7, 3, with_alpha(QI_DARK, 112)); p(8, 3, QI_PURPLE); p(9, 3, with_alpha(QI_DARK, 112))
        p(8, 4, with_alpha(QI_DARK, 168))
        return

    p(8, 1, with_alpha(QI_GLOW, 112))
    p(7, 2, with_alpha(QI_PURPLE, 112)); p(8, 2, QI_LIGHT); p(9, 2, with_alpha(QI_PURPLE, 112))
    p(7, 3, QI_PURPLE); p(8, 3, QI_WHITE); p(9, 3, QI_VIOLET)
    p(7, 4, with_alpha(QI_PURPLE, 112)); p(8, 4, QI_VIOLET); p(9, 4, with_alpha(QI_PURPLE, 112))
    p(8, 5, with_alpha(QI_GLOW, 112))
    p(5, 3, with_alpha(QI_GLOW, 64)); p(11, 3, with_alpha(QI_GLOW, 64))
    for y in range(9, 26):
        if y in (13, 19):
            p(8, y, QI_LIGHT)
        elif y % 3 == 0:
            p(8, y, with_alpha(QI_PURPLE, 168))
        else:
            p(8, y, with_alpha(QI_PURPLE, 112) if y % 2 else with_alpha(QI_DARK, 112))
    p(7, 29, with_alpha(QI_DARK, 64)); p(8, 29, with_alpha(QI_PURPLE, 112)); p(9, 29, with_alpha(QI_DARK, 64))


def draw_broken_foyer(sheet: Image.Image, origin: tuple[int, int], pal: dict[str, RGBA]) -> None:
    """Copy the repaired foyer lift, then break it: cracked dial, buckled left leaf, sprung right
    leaf, a dangling strap, and rubble. The Qi socket goes dark; its glow lives in a separate rect."""
    ox, oy = origin
    fx, fy = FOYER_BODY
    repaired = sheet.crop((fx, fy, fx + LIFT[0], fy + LIFT[1]))
    sheet.paste(repaired, (ox, oy))

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    # Dial: a chunk of the rim is gone; a hairline crack runs from the socket into the left leaf.
    p(9, 1, TRANSPARENT); p(10, 1, TRANSPARENT)
    p(10, 2, TRANSPARENT); p(11, 2, TRANSPARENT)
    p(9, 2, pal["plate_shadow"]); p(11, 3, pal["plate_shadow"]); p(12, 3, pal["plate_shadow"])
    p(8, 2, pal["ink"]); p(7, 5, pal["outline"])
    p(6, 6, pal["ink"]); p(6, 9, pal["ink"]); p(5, 10, pal["ink"]); p(6, 11, pal["outline"])

    # Left leaf buckled along a diagonal shear; the lifted plank edge still catches light.
    p(4, 14, pal["ink"]); p(5, 15, pal["ink"]); p(6, 16, pal["ink"]); p(7, 17, pal["ink"])
    p(4, 15, pal["slat_light"]); p(5, 16, pal["slat_mid"]); p(6, 17, pal["slat_dark"])
    p(4, 13, pal["slat_dark"]); p(5, 13, pal["slat_mid"])

    # Right leaf sprung open: a dark gap along the jamb, kick plate edge included.
    for y in range(9, 26):
        p(12, y, pal["ink"])

    # The lower-left strap has torn free and dangles over the kick plate.
    p(6, 19, pal["slat_dark"]); p(7, 19, pal["slat_mid"])
    p(6, 20, pal["strap"]); p(6, 21, pal["strap"]); p(6, 22, pal["strap_gleam"])

    # Dead socket and rubble at the threshold.
    p(8, 3, pal["ink"])
    p(4, 30, CAVE_DUST); p(12, 30, CAVE_BRONZE)
    p(5, 31, with_alpha(CAVE_SHADOW, 112)); p(11, 31, with_alpha(CAVE_BRONZE, 112))
    p(13, 23, COPPER_DARK); p(13, 24, pal["outline"])


def draw_broken_glow(sheet: Image.Image, origin: tuple[int, int]) -> None:
    px(sheet, origin[0] + 8, origin[1] + 3, with_alpha(QI_DARK, 112))


def draw_flash(sheet: Image.Image, origin: tuple[int, int], variant: int) -> None:
    """One-frame golden contact flashes: variant 0 hits the left jamb, 1 the right, 2 the threshold."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def star(cx: int, cy: int) -> None:
        p(cx, cy, FLASH_WHITE)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            p(cx + dx, cy + dy, GOLD_GLEAM)
        for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            p(cx + dx, cy + dy, with_alpha(GOLD_GLEAM, 112))
        p(cx, cy - 2, with_alpha(GOLD_GLEAM, 64)); p(cx, cy + 2, with_alpha(GOLD_GLEAM, 64))

    if variant == 0:
        star(3, 15)
        p(2, 19, with_alpha(CAVE_DUST, 112)); p(1, 21, with_alpha(CAVE_DUST, 64))
    elif variant == 1:
        star(13, 12)
        p(14, 16, with_alpha(CAVE_DUST, 112)); p(15, 18, with_alpha(CAVE_DUST, 64))
    else:
        hline(sheet, ox + 5, ox + 11, oy + 27, with_alpha(GOLD_GLEAM, 168))
        p(8, 27, FLASH_WHITE)
        star(3, 24); star(13, 24)
        p(4, 26, with_alpha(CAVE_DUST, 112)); p(12, 26, with_alpha(CAVE_DUST, 112))
        p(6, 23, with_alpha(CAVE_DUST, 64)); p(10, 23, with_alpha(CAVE_DUST, 64))


def draw_seam(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """The full awakening column. Runtime reveals it bottom-up with a shrinking source rect."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    p(8, 2, QI_WHITE)
    p(8, 3, QI_GLOW)
    p(8, 4, QI_VIOLET)
    p(8, 5, QI_LIGHT)
    p(8, 6, QI_VIOLET)
    p(8, 7, with_alpha(QI_PURPLE, 168))
    p(8, 8, QI_VIOLET)
    for y in range(9, 27):
        if y in (13, 19):
            p(8, y, QI_LIGHT)
        else:
            p(8, y, QI_VIOLET if y % 4 else with_alpha(QI_PURPLE, 168))
    for y in (13, 19):
        p(7, y, with_alpha(QI_VIOLET, 168)); p(9, y, with_alpha(QI_VIOLET, 168))
        p(6, y, with_alpha(QI_PURPLE, 112)); p(10, y, with_alpha(QI_PURPLE, 112))
    p(8, 27, QI_GLOW)
    p(7, 28, with_alpha(QI_PURPLE, 112)); p(8, 28, QI_GLOW); p(9, 28, with_alpha(QI_PURPLE, 112))
    p(6, 29, with_alpha(QI_DARK, 64)); p(7, 29, with_alpha(QI_PURPLE, 112))
    p(8, 29, with_alpha(QI_GLOW, 168)); p(9, 29, with_alpha(QI_PURPLE, 112)); p(10, 29, with_alpha(QI_DARK, 64))


def draw_flare(sheet: Image.Image, origin: tuple[int, int], big: bool) -> None:
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    if not big:
        p(8, 1, with_alpha(QI_GLOW, 168))
        p(7, 2, QI_VIOLET); p(8, 2, QI_GLOW); p(9, 2, QI_VIOLET)
        p(7, 3, QI_GLOW); p(8, 3, QI_WHITE); p(9, 3, QI_GLOW)
        p(7, 4, QI_VIOLET); p(8, 4, QI_GLOW); p(9, 4, QI_VIOLET)
        p(8, 5, with_alpha(QI_GLOW, 168))
        return
    p(8, 3, QI_WHITE)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        p(8 + dx, 3 + dy, QI_WHITE if dy else QI_GLOW)
    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        p(8 + dx, 3 + dy, QI_GLOW)
    p(8, 0, with_alpha(QI_GLOW, 168)); p(8, 6, with_alpha(QI_GLOW, 168))
    p(5, 3, with_alpha(QI_GLOW, 168)); p(11, 3, with_alpha(QI_GLOW, 168))
    p(4, 3, with_alpha(QI_GLOW, 64)); p(12, 3, with_alpha(QI_GLOW, 64))
    p(6, 1, with_alpha(QI_GLOW, 112)); p(10, 1, with_alpha(QI_GLOW, 112))
    p(6, 5, with_alpha(QI_GLOW, 112)); p(10, 5, with_alpha(QI_GLOW, 112))


def draw_bloom(sheet: Image.Image) -> None:
    """A restrained radial bloom centered on the crown; drawn once, faded out by the runtime."""
    ox, oy, width, height = BLOOM
    cx, cy, rx, ry = 16.0, 7.0, 15.0, 10.0
    for y in range(height):
        for x in range(width):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d <= 0.2:
                alpha = 40
            elif d <= 0.55:
                alpha = 28
            elif d <= 1.0:
                alpha = 16
            else:
                continue
            px(sheet, ox + x, oy + y, with_alpha(QI_GLOW, alpha))


def draw_sparks_and_motes(sheet: Image.Image) -> None:
    ox, oy = SPARK_A[:2]
    px(sheet, ox + 2, oy + 2, SPARK_WHITE)
    for dx, dy in ((0, -2), (0, 2), (-2, 0), (2, 0)):
        px(sheet, ox + 2 + dx, oy + 2 + dy, with_alpha(SPARK_BLUE, 168))
    for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        px(sheet, ox + 2 + dx, oy + 2 + dy, SPARK_BLUE)
    ox, oy = SPARK_B[:2]
    px(sheet, ox + 2, oy + 2, SPARK_WHITE)
    for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        px(sheet, ox + 2 + dx, oy + 2 + dy, with_alpha(SPARK_BLUE, 168))
    for dx, dy in ((-2, -2), (2, -2), (-2, 2), (2, 2)):
        px(sheet, ox + 2 + dx, oy + 2 + dy, with_alpha(SPARK_BLUE, 64))

    ox, oy = MOTE_A[:2]
    px(sheet, ox + 1, oy + 1, QI_GLOW)
    for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        px(sheet, ox + 1 + dx, oy + 1 + dy, with_alpha(QI_PURPLE, 112))
    ox, oy = MOTE_B[:2]
    px(sheet, ox + 1, oy + 1, QI_LIGHT)
    for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        px(sheet, ox + 1 + dx, oy + 1 + dy, with_alpha(QI_PURPLE, 64))


def gauge_rail_row() -> list[RGBA]:
    edge = with_alpha(DESERT_LIGHT, 64)
    return [edge, DESERT_MID, DESERT_SHADOW, CAVE_SHADOW, ABYSS, ABYSS, ABYSS, ABYSS,
            CAVE_SHADOW, DESERT_SHADOW, DESERT_MID, edge]


def draw_gauge(sheet: Image.Image) -> None:
    # Top cap: crystal finial on a bone bracket, opening into the channel.
    ox, oy = GAUGE_TOP[:2]
    px(sheet, ox + 5, oy, QI_VIOLET); px(sheet, ox + 6, oy, QI_DARK)
    px(sheet, ox + 4, oy + 1, QI_DARK); px(sheet, ox + 5, oy + 1, QI_GLOW)
    px(sheet, ox + 6, oy + 1, QI_PURPLE); px(sheet, ox + 7, oy + 1, QI_DARK)
    hline(sheet, ox + 3, ox + 8, oy + 2, SAND_PALE)
    px(sheet, ox + 3, oy + 2, BONE_SHADOW); px(sheet, ox + 8, oy + 2, BONE_SHADOW)
    hline(sheet, ox + 2, ox + 9, oy + 3, DESERT_MID)
    hline(sheet, ox + 1, ox + 10, oy + 4, DESERT_SHADOW)
    px(sheet, ox + 2, oy + 4, SAND_LIGHT)
    for x, color in enumerate(gauge_rail_row()):
        px(sheet, ox + x, oy + 5, color)

    # Middle: two identical rows so the body tiles seamlessly.
    ox, oy = GAUGE_MIDDLE[:2]
    for y in range(2):
        for x, color in enumerate(gauge_rail_row()):
            px(sheet, ox + x, oy + y, color)

    # Bottom cap: channel mouth, then a heavier carved anchor.
    ox, oy = GAUGE_BOTTOM[:2]
    for x, color in enumerate(gauge_rail_row()):
        px(sheet, ox + x, oy, color)
    hline(sheet, ox + 1, ox + 10, oy + 1, DESERT_SHADOW)
    hline(sheet, ox + 2, ox + 9, oy + 2, DESERT_MID)
    px(sheet, ox + 9, oy + 2, SAND_LIGHT)
    hline(sheet, ox + 3, ox + 8, oy + 3, DESERT_SHADOW)
    hline(sheet, ox + 4, ox + 7, oy + 4, CAVE_SHADOW)
    px(sheet, ox + 5, oy + 5, with_alpha(CAVE_SHADOW, 112)); px(sheet, ox + 6, oy + 5, with_alpha(CAVE_SHADOW, 112))

    # Marker: a bronze clamp gripping the rail, with a lit Qi gem over the channel.
    ox, oy = GAUGE_MARKER[:2]
    hline(sheet, ox + 4, ox + 9, oy, with_alpha(GOLD_DARK, 168))
    hline(sheet, ox + 2, ox + 11, oy + 1, GOLD_DARK)
    px(sheet, ox + 3, oy + 1, GOLD)
    hline(sheet, ox + 1, ox + 12, oy + 2, GOLD_DARK)
    px(sheet, ox + 2, oy + 2, GOLD_GLEAM)
    px(sheet, ox + 6, oy + 2, QI_PURPLE); px(sheet, ox + 7, oy + 2, QI_GLOW)
    px(sheet, ox + 13, oy + 2, with_alpha(GOLD_DARK, 168))
    hline(sheet, ox + 2, ox + 11, oy + 3, CAVE_SHADOW)
    hline(sheet, ox + 4, ox + 9, oy + 4, with_alpha(CAVE_SHADOW, 168))

    # Icon: a tiny lift glyph naming the instrument.
    ox, oy = GAUGE_ICON[:2]
    px(sheet, ox + 4, oy, QI_PURPLE); px(sheet, ox + 5, oy, QI_DARK)
    hline(sheet, ox + 3, ox + 6, oy + 1, SAND_PALE)
    hline(sheet, ox + 1, ox + 8, oy + 2, DESERT_SHADOW)
    for y in range(3, 9):
        px(sheet, ox + 1, oy + y, DESERT_SHADOW)
        px(sheet, ox + 2, oy + y, DESERT_MID)
        px(sheet, ox + 3, oy + y, CAVE_SHADOW)
        px(sheet, ox + 4, oy + y, ABYSS)
        px(sheet, ox + 5, oy + y, CAVE_SHADOW)
        px(sheet, ox + 6, oy + y, DESERT_MID)
        px(sheet, ox + 7, oy + y, DESERT_SHADOW)
        px(sheet, ox + 8, oy + y, with_alpha(DESERT_SHADOW, 168))
    px(sheet, ox + 2, oy + 5, GOLD_DARK); px(sheet, ox + 6, oy + 5, GOLD_DARK)
    hline(sheet, ox + 2, ox + 7, oy + 9, DESERT_SHADOW)

    # Fill: a 4x2 energy weave tiled inside the channel.
    ox, oy = GAUGE_FILL[:2]
    px(sheet, ox, oy, with_alpha(QI_DARK, 168)); px(sheet, ox + 1, oy, QI_VIOLET)
    px(sheet, ox + 2, oy, QI_GLOW); px(sheet, ox + 3, oy, with_alpha(QI_PURPLE, 168))
    px(sheet, ox, oy + 1, with_alpha(QI_PURPLE, 168)); px(sheet, ox + 1, oy + 1, QI_PURPLE)
    px(sheet, ox + 2, oy + 1, QI_VIOLET); px(sheet, ox + 3, oy + 1, with_alpha(QI_DARK, 168))

    # Ember: a falling chip for loss feedback.
    ox, oy = GAUGE_EMBER[:2]
    px(sheet, ox + 1, oy, QI_GLOW)
    px(sheet, ox, oy + 1, with_alpha(QI_PURPLE, 112)); px(sheet, ox + 1, oy + 1, QI_VIOLET)
    px(sheet, ox + 2, oy + 1, with_alpha(QI_PURPLE, 112))
    px(sheet, ox + 1, oy + 2, with_alpha(QI_DARK, 112))


def validate(sheet: Image.Image) -> None:
    assert sheet.size == SHEET_SIZE and sheet.mode == "RGBA"

    alphas = {pixel[3] for pixel in sheet.getdata()}
    assert alphas == {0, 16, 28, 40, 64, 112, 168, 255}, f"unexpected alphas: {sorted(alphas)}"

    # The machine silhouette must stay balanced: alpha masks mirror around x=8.
    for origin in (MINE_BODY, FOYER_BODY, MINE_SHROUD):
        sprite = crop(sheet, origin)
        for y in range(32):
            for x in range(17):
                left = sprite.getpixel((x, y))[3]
                right = sprite.getpixel((16 - x, y))[3]
                assert left == right, f"{origin} alpha asymmetry at {(x, y)}"

    # The shroud may bite edges and shoulders but must never cover the doors or dial face.
    shroud = crop(sheet, MINE_SHROUD)
    for y in range(2, 30):
        for x in range(4, 13):
            assert shroud.getpixel((x, y))[3] == 0, f"shroud invades machine at {(x, y)}"

    body = crop(sheet, MINE_BODY)
    for y in range(9, 26):
        assert body.getpixel((8, y))[:3] == mute(INK, 0.30)[:3], f"seam broken at y={y}"

    for origin, expect in ((MINE_GLOW_IDLE, (7, 2, 10, 5)), (FOYER_GLOW_IDLE, (7, 2, 10, 5))):
        assert crop(sheet, origin).getbbox() == expect, crop(sheet, origin).getbbox()
    for origin in (MINE_GLOW_ACTIVE, FOYER_GLOW_ACTIVE):
        box = crop(sheet, origin).getbbox()
        assert box is not None and box[1] <= 2 and box[3] >= 29, box

    seam = crop(sheet, SEAM)
    for y in range(2, 28):
        assert seam.getpixel((8, y))[3] > 0, f"seam column gap at y={y}"

    middle = crop(sheet, GAUGE_MIDDLE)
    assert list(middle.crop((0, 0, 12, 1)).getdata()) == list(middle.crop((0, 1, 12, 2)).getdata())
    fill = crop(sheet, GAUGE_FILL)
    assert list(fill.crop((0, 0, 4, 1)).getdata()) != list(fill.crop((0, 1, 4, 2)).getdata())

    broken = crop(sheet, FOYER_BROKEN)
    repaired = crop(sheet, FOYER_BODY)
    assert list(broken.getdata()) != list(repaired.getdata())
    assert crop(sheet, FOYER_BROKEN_GLOW).getbbox() == (8, 3, 9, 4)


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    REFERENCES.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", SHEET_SIZE, TRANSPARENT)

    draw_lift_body(sheet, MINE_BODY, lift_palette(adaptive=True))
    draw_mine_shroud(sheet, MINE_SHROUD)
    draw_glow(sheet, MINE_GLOW_IDLE, active=False)
    draw_glow(sheet, MINE_GLOW_ACTIVE, active=True)
    foyer_pal = lift_palette(adaptive=False)
    draw_lift_body(sheet, FOYER_BODY, foyer_pal)
    draw_foyer_dressing(sheet, FOYER_BODY)
    draw_glow(sheet, FOYER_GLOW_IDLE, active=False)
    draw_glow(sheet, FOYER_GLOW_ACTIVE, active=True)
    draw_broken_foyer(sheet, FOYER_BROKEN, foyer_pal)
    draw_broken_glow(sheet, FOYER_BROKEN_GLOW)
    for index, origin in enumerate((FLASH_A, FLASH_B, FLASH_C)):
        draw_flash(sheet, origin, index)
    draw_seam(sheet, SEAM)
    draw_flare(sheet, FLARE_A, big=False)
    draw_flare(sheet, FLARE_B, big=True)
    draw_bloom(sheet)
    draw_sparks_and_motes(sheet)
    draw_gauge(sheet)
    validate(sheet)

    sheet.save(ASSETS / "qfe-sprites.png", optimize=True)
    checker_preview(sheet).save(REFERENCES / "qfe-sprites-8x.png")
    qfe_previews.compose_foyer(sheet, REFERENCES, FOYER_BODY, FOYER_GLOW_IDLE).save(REFERENCES / "qfe-elevator-in-vanilla-4x.png")
    qfe_previews.compose_foyer(sheet, REFERENCES, FOYER_BROKEN, FOYER_BROKEN_GLOW).save(REFERENCES / "qfe-elevator-broken-in-vanilla-4x.png")
    repair_states = [
        (FOYER_BROKEN, FOYER_BROKEN_GLOW), (FOYER_BROKEN, FLASH_A), (FOYER_BROKEN, FLASH_C),
        (FOYER_BODY, SEAM), (FOYER_BODY, FLARE_B), (FOYER_BODY, FOYER_GLOW_ACTIVE),
    ]
    qfe_previews.compose_repair_sequence(sheet, REFERENCES, repair_states).save(REFERENCES / "qfe-repair-sequence-4x.png")
    qfe_previews.compose_mine(sheet, MINE_BODY, MINE_SHROUD, MINE_GLOW_ACTIVE).save(REFERENCES / "qfe-mine-adaptive-4x.png")
    gauge_rects = {
        "top": GAUGE_TOP, "middle": GAUGE_MIDDLE, "bottom": GAUGE_BOTTOM,
        "marker": GAUGE_MARKER, "icon": GAUGE_ICON, "fill": GAUGE_FILL,
    }
    qfe_previews.compose_gauge(sheet, gauge_rects).save(REFERENCES / "qfe-gauge-preview-4x.png")
    print("Wrote assets/qfe-sprites.png (176x96) and refreshed previews.")


if __name__ == "__main__":
    main()
