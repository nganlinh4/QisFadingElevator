"""Generate assets/qfe-sprites.png plus reference previews.

Sheet layout (native pixels, 176x96): row 0 holds the lift sprites (shell/accents/glows/broken),
row 1 the repair frames plus bloom/sparks/motes, row 2 the modular gauge. Exact origins are the
constants below, mirrored by SpriteSheet.cs.

Color model (calibrated against live screenshots, 2026-07-15): the SHELL carries the machine's
mass as a near-white warm-neutral relief (midtone ~212) and is multiplied at runtime by the
sampled wall color x1.20, so its median luminance lands on the wall's own median in every room.
The ACCENTS sprite holds only absolute darkness and identity pixels (interior openings, rivets,
copper node) and is drawn untinted. Qi light lives on the separate glow sprites.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image

import qfe_previews
from pixel_kit import (
    TRANSPARENT, RGBA, px, hline, vline, rect, with_alpha, crop, checker_preview,
)

PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / "assets"
REFERENCES = PROJECT / "references"


def grey(value: int) -> RGBA:
    return (value, value, value, 255)


def warm(value: int, ratio: tuple[float, float, float]) -> RGBA:
    return (round(value * ratio[0]), round(value * ratio[1]), round(value * ratio[2]), 255)


# Shell relief values (multiplied by wall tint x1.20 at runtime; 212 x 1.20 = wall parity).
N_GLEAM = grey(244)
N_LIGHT = grey(228)
N_MID = grey(212)
N_SOFT = grey(192)
N_SHADOW = grey(170)
N_DARK = grey(142)
DOOR_MID = grey(202)
DOOR_DARK = grey(182)
# Bone dial plate: the palest fixture element in every room, like vanilla's pale elevator dial.
BONE_RATIO = (1.0, 0.95, 0.86)
PLATE_HI = warm(246, BONE_RATIO)
PLATE_MID = warm(224, BONE_RATIO)
PLATE_SHADOW = warm(194, BONE_RATIO)
# Brass straps: a whisper warmer than the wall, never a saturated orange pop.
BRASS_RATIO = (1.0, 0.92, 0.78)
BRASS = warm(176, BRASS_RATIO)
BRASS_HI = warm(236, BRASS_RATIO)
# Accent absolutes.
INTERIOR = (28, 22, 16, 255)
RIVET = (46, 36, 26, 255)
COPPER = (196, 106, 56, 255)
COPPER_DARK = (135, 66, 38, 255)
DEAD_SOCKET = (52, 38, 28, 255)
FLASH_WHITE = (255, 250, 226, 255)
GOLD_GLEAM = (232, 170, 76, 255)
CAVE_DUST = (132, 99, 46, 255)
SPARK_WHITE = (240, 252, 255, 255)
SPARK_BLUE = (138, 214, 255, 255)
# Gauge sandstone + Qi set (HUD, fixed colors).
DESERT_SHADOW = (116, 85, 56, 255)
DESERT_MID = (151, 110, 71, 255)
DESERT_LIGHT = (175, 128, 98, 255)
SAND_LIGHT = (173, 158, 110, 255)
SAND_PALE = (189, 164, 132, 255)
BONE_SHADOW = (129, 104, 74, 255)
CAVE_SHADOW = (78, 46, 43, 255)
ABYSS = (24, 16, 32, 255)
GOLD_DARK = (168, 110, 23, 255)
GOLD = (183, 129, 58, 255)
QI_DARK = (64, 19, 153, 255)
QI_PURPLE = (106, 51, 255, 255)
QI_VIOLET = (156, 41, 255, 255)
QI_LIGHT = (127, 123, 255, 255)
QI_GLOW = (220, 173, 255, 255)
QI_WHITE = (255, 234, 255, 255)

SHEET_SIZE = (176, 96)
LIFT = (17, 32)

LIFT_SHELL = (0, 0)
LIFT_ACCENTS = (17, 0)
GLOW_IDLE = (34, 0)
GLOW_ACTIVE = (51, 0)
BROKEN_SHELL = (68, 0)
BROKEN_ACCENTS = (85, 0)
BROKEN_GLOW = (102, 0)
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
GAUGE_TOP = (0, 64, 10, 5)
GAUGE_MIDDLE = (0, 69, 10, 2)
GAUGE_BOTTOM = (0, 71, 10, 5)
GAUGE_MARKER = (12, 64, 12, 5)
GAUGE_ICON = (12, 70, 8, 8)
GAUGE_FILL = (26, 64, 4, 2)
GAUGE_EMBER = (32, 64, 3, 3)


def draw_shell(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """The machine's wall-tracking mass: dial crown, shoulders, lintel, jambs, slatted doors,
    straps, kick plates, threshold, and the dissolving rock collar. Mirrored around x=8."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def m(x: int, y: int, color: RGBA) -> None:
        p(x, y, color)
        p(16 - x, y, color)

    def h(x1: int, x2: int, y: int, color: RGBA) -> None:
        hline(sheet, ox + x1, ox + x2, oy + y, color)

    # Bone dial crown; the socket pixels stay plate-colored here and the accents darken them.
    h(6, 10, 1, PLATE_SHADOW)
    m(5, 2, PLATE_SHADOW); h(6, 10, 2, PLATE_HI)
    m(4, 3, PLATE_SHADOW); p(5, 3, PLATE_HI); p(6, 3, PLATE_HI)
    h(7, 9, 3, PLATE_MID); p(10, 3, PLATE_MID); p(11, 3, PLATE_MID)
    m(4, 4, PLATE_SHADOW); h(5, 11, 4, PLATE_MID)
    h(4, 12, 5, PLATE_SHADOW)

    # Carved rock shoulders hugging the crown.
    m(3, 3, with_alpha(N_SOFT, 168))
    m(2, 4, with_alpha(N_SOFT, 112)); m(3, 4, N_SOFT)
    m(1, 5, with_alpha(N_SOFT, 64)); m(2, 5, N_SOFT); m(3, 5, N_SHADOW)

    # Lintel beam with a lighter inset; the dark underside lives on the accents sprite.
    h(3, 13, 6, N_SHADOW); h(5, 11, 6, N_MID)
    h(3, 13, 7, N_DARK)

    # Jambs: lit left face, shaded right face, room-value edges.
    vline(sheet, ox + 2, oy + 8, oy + 26, N_DARK)
    vline(sheet, ox + 3, oy + 8, oy + 26, N_MID)
    vline(sheet, ox + 13, oy + 8, oy + 26, N_SHADOW)
    vline(sheet, ox + 14, oy + 8, oy + 26, N_DARK)
    for y in (10, 17, 24):
        p(3, y, N_LIGHT)
    for y in (12, 20):
        p(13, y, N_SOFT)

    # Twin doors: vertical slats one step darker than the frame; the seam is accent-dark.
    for y in range(9, 26):
        p(4, y, DOOR_DARK); p(5, y, DOOR_MID); p(6, y, DOOR_DARK); p(7, y, DOOR_MID)
        p(9, y, DOOR_MID); p(10, y, DOOR_DARK); p(11, y, DOOR_MID); p(12, y, DOOR_DARK)
    for x, y in ((5, 10), (11, 15), (7, 17), (9, 11)):
        p(x, y, N_LIGHT)
    for y in (13, 19):
        h(4, 7, y, BRASS); h(9, 12, y, BRASS)
        p(4, y, BRASS_HI); p(9, y, BRASS_HI)
    for x1, x2 in ((4, 7), (9, 12)):
        rect(sheet, ox + x1, oy + 21, ox + x2, oy + 25, N_SHADOW)
        rect(sheet, ox + x1 + 1, oy + 22, ox + x2 - 1, oy + 24, DOOR_MID)
    m(5, 22, N_LIGHT)

    # Threshold steps and the dissolving base skirt.
    h(3, 13, 27, N_DARK)
    h(2, 14, 28, N_SOFT)
    h(3, 13, 29, N_SHADOW)
    h(5, 11, 30, N_DARK)
    m(1, 28, with_alpha(N_SOFT, 168)); m(0, 28, with_alpha(N_SOFT, 64))
    m(2, 29, with_alpha(N_SHADOW, 112))
    m(4, 30, N_SHADOW); m(3, 30, with_alpha(N_SOFT, 112))
    m(5, 31, with_alpha(N_SHADOW, 64)); m(7, 31, with_alpha(N_SOFT, 64))

    # Rock bites over the jamb edges tie the silhouette into the patched wall.
    m(0, 9, with_alpha(N_SOFT, 64)); m(1, 9, N_SOFT); m(2, 9, with_alpha(N_SHADOW, 168))
    m(1, 10, N_SHADOW)
    m(0, 14, with_alpha(N_SOFT, 112)); m(1, 14, N_LIGHT); m(2, 14, with_alpha(N_SHADOW, 112))
    m(1, 15, N_SOFT); m(0, 15, with_alpha(N_SHADOW, 64))
    m(1, 20, N_SOFT); m(0, 20, with_alpha(N_SOFT, 64)); m(1, 21, with_alpha(N_SHADOW, 168))


def draw_accents(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Absolute pixels only: interior openings that stay dark in any room, rivets, copper node."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def h(x1: int, x2: int, y: int, color: RGBA) -> None:
        hline(sheet, ox + x1, ox + x2, oy + y, color)

    p(8, 2, INTERIOR)
    h(7, 9, 3, INTERIOR)
    p(8, 4, INTERIOR)
    h(4, 12, 7, INTERIOR)
    h(4, 12, 8, INTERIOR)
    vline(sheet, ox + 8, oy + 9, oy + 25, INTERIOR)
    h(4, 12, 26, INTERIOR)
    h(3, 13, 27, with_alpha(INTERIOR, 112))
    for y in (13, 19):
        p(6, y, RIVET); p(10, y, RIVET)
    p(13, 23, COPPER); p(13, 24, COPPER_DARK)


def draw_broken_shell(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Copy the shell, then break it: a missing dial chunk, a sheared plank catching light,
    and rubble chips at the threshold."""
    ox, oy = origin
    sx, sy = LIFT_SHELL
    sheet.paste(sheet.crop((sx, sy, sx + LIFT[0], sy + LIFT[1])), (ox, oy))

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    p(9, 1, TRANSPARENT); p(10, 1, TRANSPARENT)
    p(10, 2, TRANSPARENT); p(11, 2, TRANSPARENT)
    p(9, 2, PLATE_SHADOW); p(11, 3, PLATE_SHADOW); p(12, 3, PLATE_SHADOW)
    p(4, 15, N_LIGHT); p(5, 16, N_SOFT); p(6, 17, N_SHADOW)
    p(4, 13, N_SHADOW); p(5, 13, N_SOFT)
    p(6, 19, N_SHADOW); p(7, 19, N_SOFT)
    p(6, 20, BRASS); p(6, 21, BRASS); p(6, 22, BRASS_HI)
    p(4, 30, N_SOFT); p(12, 30, N_SHADOW)
    p(5, 31, with_alpha(N_SHADOW, 112)); p(11, 31, with_alpha(N_SOFT, 112))


def draw_broken_accents(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Broken interior darkness: crack path, sheared gap, sprung right leaf, dead power node."""
    ox, oy = origin
    ax, ay = LIFT_ACCENTS
    sheet.paste(sheet.crop((ax, ay, ax + LIFT[0], ay + LIFT[1])), (ox, oy))

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    p(8, 2, INTERIOR); p(7, 5, with_alpha(INTERIOR, 168))
    p(6, 6, INTERIOR); p(6, 9, INTERIOR); p(5, 10, INTERIOR); p(6, 11, with_alpha(INTERIOR, 168))
    p(4, 14, INTERIOR); p(5, 15, INTERIOR); p(6, 16, INTERIOR); p(7, 17, INTERIOR)
    for y in range(9, 26):
        p(12, y, INTERIOR)
    p(13, 23, DEAD_SOCKET); p(13, 24, INTERIOR)


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

    p(8, 2, QI_WHITE); p(8, 3, QI_GLOW); p(8, 4, QI_VIOLET)
    p(8, 5, QI_LIGHT); p(8, 6, QI_VIOLET); p(8, 7, with_alpha(QI_PURPLE, 168)); p(8, 8, QI_VIOLET)
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
    return [edge, DESERT_MID, DESERT_SHADOW, ABYSS, ABYSS, ABYSS, ABYSS, DESERT_SHADOW, DESERT_MID, edge]


def draw_gauge(sheet: Image.Image) -> None:
    # Top cap: crystal finial on a bone bracket, opening into the channel.
    ox, oy = GAUGE_TOP[:2]
    px(sheet, ox + 4, oy, QI_VIOLET); px(sheet, ox + 5, oy, QI_DARK)
    px(sheet, ox + 3, oy + 1, QI_DARK); px(sheet, ox + 4, oy + 1, QI_GLOW)
    px(sheet, ox + 5, oy + 1, QI_PURPLE); px(sheet, ox + 6, oy + 1, QI_DARK)
    hline(sheet, ox + 2, ox + 7, oy + 2, SAND_PALE)
    px(sheet, ox + 2, oy + 2, BONE_SHADOW); px(sheet, ox + 7, oy + 2, BONE_SHADOW)
    hline(sheet, ox + 1, ox + 8, oy + 3, DESERT_MID)
    px(sheet, ox + 2, oy + 3, SAND_LIGHT)
    for x, color in enumerate(gauge_rail_row()):
        px(sheet, ox + x, oy + 4, color)

    # Middle: two identical rows so the body tiles seamlessly.
    ox, oy = GAUGE_MIDDLE[:2]
    for y in range(2):
        for x, color in enumerate(gauge_rail_row()):
            px(sheet, ox + x, oy + y, color)

    # Bottom cap: channel mouth, then a carved anchor.
    ox, oy = GAUGE_BOTTOM[:2]
    for x, color in enumerate(gauge_rail_row()):
        px(sheet, ox + x, oy, color)
    hline(sheet, ox + 1, ox + 8, oy + 1, DESERT_SHADOW)
    hline(sheet, ox + 2, ox + 7, oy + 2, DESERT_MID)
    px(sheet, ox + 7, oy + 2, SAND_LIGHT)
    hline(sheet, ox + 3, ox + 6, oy + 3, CAVE_SHADOW)
    px(sheet, ox + 4, oy + 4, with_alpha(CAVE_SHADOW, 112)); px(sheet, ox + 5, oy + 4, with_alpha(CAVE_SHADOW, 112))

    # Marker: a bronze clamp gripping the rail, with a lit Qi gem over the channel.
    ox, oy = GAUGE_MARKER[:2]
    hline(sheet, ox + 3, ox + 8, oy, with_alpha(GOLD_DARK, 168))
    hline(sheet, ox + 1, ox + 10, oy + 1, GOLD_DARK)
    px(sheet, ox + 2, oy + 1, GOLD)
    hline(sheet, ox, ox + 11, oy + 2, GOLD_DARK)
    px(sheet, ox + 1, oy + 2, GOLD)
    px(sheet, ox + 5, oy + 2, QI_PURPLE); px(sheet, ox + 6, oy + 2, QI_GLOW)
    hline(sheet, ox + 1, ox + 10, oy + 3, CAVE_SHADOW)
    hline(sheet, ox + 3, ox + 8, oy + 4, with_alpha(CAVE_SHADOW, 168))

    # Icon: a tiny lift glyph naming the instrument.
    ox, oy = GAUGE_ICON[:2]
    px(sheet, ox + 3, oy, QI_PURPLE); px(sheet, ox + 4, oy, QI_DARK)
    hline(sheet, ox + 2, ox + 5, oy + 1, SAND_PALE)
    hline(sheet, ox + 1, ox + 6, oy + 2, DESERT_SHADOW)
    for y in range(3, 7):
        px(sheet, ox + 1, oy + y, DESERT_SHADOW)
        px(sheet, ox + 2, oy + y, DESERT_MID)
        px(sheet, ox + 3, oy + y, ABYSS)
        px(sheet, ox + 4, oy + y, DESERT_MID)
        px(sheet, ox + 5, oy + y, DESERT_MID)
        px(sheet, ox + 6, oy + y, DESERT_SHADOW)
    px(sheet, ox + 2, oy + 4, GOLD_DARK); px(sheet, ox + 4, oy + 4, GOLD_DARK)
    hline(sheet, ox + 1, ox + 6, oy + 7, DESERT_SHADOW)

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
    # The copper power node is the one sanctioned asymmetric identity detail.
    node_pixels = {(13, 23), (3, 23), (13, 24), (3, 24)}
    for origin in (LIFT_SHELL, LIFT_ACCENTS):
        sprite = crop(sheet, origin)
        for y in range(32):
            for x in range(17):
                if origin == LIFT_ACCENTS and (x, y) in node_pixels:
                    continue
                assert sprite.getpixel((x, y))[3] == sprite.getpixel((16 - x, y))[3], (origin, x, y)

    # Shell calibration: opaque interior pixels stay in the wall-parity band (see module docstring).
    shell = crop(sheet, LIFT_SHELL)
    interior = [p for y in range(1, 30) for p in [shell.getpixel((x, y)) for x in range(3, 14)] if p[3] == 255]
    lumas = sorted(0.2126 * p[0] + 0.7152 * p[1] + 0.0722 * p[2] for p in interior)
    median = lumas[len(lumas) // 2]
    assert 178 <= median <= 218, f"shell median {median:.0f} off wall-parity target"
    assert min(lumas) >= 130, f"shell has too-dark pixel ({min(lumas):.0f}); darkness belongs to accents"

    accents = crop(sheet, LIFT_ACCENTS)
    for y in range(9, 26):
        assert accents.getpixel((8, y))[:3] == (28, 22, 16), f"seam broken at y={y}"

    assert crop(sheet, GLOW_IDLE).getbbox() == (7, 2, 10, 5)
    box = crop(sheet, GLOW_ACTIVE).getbbox()
    assert box is not None and box[1] <= 2 and box[3] >= 29, box
    assert crop(sheet, BROKEN_GLOW).getbbox() == (8, 3, 9, 4)
    assert list(crop(sheet, BROKEN_SHELL).getdata()) != list(crop(sheet, LIFT_SHELL).getdata())

    seam = crop(sheet, SEAM)
    for y in range(2, 28):
        assert seam.getpixel((8, y))[3] > 0, f"seam column gap at y={y}"

    middle = crop(sheet, GAUGE_MIDDLE)
    assert list(middle.crop((0, 0, 10, 1)).getdata()) == list(middle.crop((0, 1, 10, 2)).getdata())
    fill = crop(sheet, GAUGE_FILL)
    assert list(fill.crop((0, 0, 4, 1)).getdata()) != list(fill.crop((0, 1, 4, 2)).getdata())


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    REFERENCES.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", SHEET_SIZE, TRANSPARENT)

    draw_shell(sheet, LIFT_SHELL)
    draw_accents(sheet, LIFT_ACCENTS)
    draw_glow(sheet, GLOW_IDLE, active=False)
    draw_glow(sheet, GLOW_ACTIVE, active=True)
    draw_broken_shell(sheet, BROKEN_SHELL)
    draw_broken_accents(sheet, BROKEN_ACCENTS)
    draw_broken_glow(sheet, BROKEN_GLOW)
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
    lift = {"shell": LIFT_SHELL, "accents": LIFT_ACCENTS, "broken_shell": BROKEN_SHELL,
            "broken_accents": BROKEN_ACCENTS, "glow_idle": GLOW_IDLE, "glow_active": GLOW_ACTIVE,
            "broken_glow": BROKEN_GLOW}
    qfe_previews.compose_live_walls(sheet, REFERENCES, lift).save(REFERENCES / "qfe-live-wall-check-3x.png")
    repair_states = [
        (BROKEN_SHELL, BROKEN_ACCENTS, BROKEN_GLOW), (BROKEN_SHELL, BROKEN_ACCENTS, FLASH_A),
        (BROKEN_SHELL, BROKEN_ACCENTS, FLASH_C), (LIFT_SHELL, LIFT_ACCENTS, SEAM),
        (LIFT_SHELL, LIFT_ACCENTS, FLARE_B), (LIFT_SHELL, LIFT_ACCENTS, GLOW_ACTIVE),
    ]
    qfe_previews.compose_repair_sequence(sheet, REFERENCES, repair_states).save(REFERENCES / "qfe-repair-sequence-4x.png")
    gauge_rects = {
        "top": GAUGE_TOP, "middle": GAUGE_MIDDLE, "bottom": GAUGE_BOTTOM,
        "marker": GAUGE_MARKER, "icon": GAUGE_ICON, "fill": GAUGE_FILL,
    }
    qfe_previews.compose_gauge(sheet, gauge_rects).save(REFERENCES / "qfe-gauge-preview-4x.png")
    print("Wrote assets/qfe-sprites.png (176x96) and refreshed previews.")


if __name__ == "__main__":
    main()
