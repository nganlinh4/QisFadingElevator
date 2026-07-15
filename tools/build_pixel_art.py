"""Generate assets/qfe-sprites.png plus reference previews.

Sheet layout (native pixels, 176x96): row 0 holds the lift sprites (shell/accents/glows/broken),
row 1 the repair frames plus bloom/sparks/motes, row 2 the modular gauge. Exact origins are the
constants below, mirrored by SpriteSheet.cs.

Color model (calibrated against live screenshots, 2026-07-15): the SHELL carries the machine's
mass as a near-white warm-neutral relief multiplied at runtime by the sampled wall color x1.30,
so its median luminance lands on the wall's own median in every room. The ACCENTS sprite holds
translucent warm-mauve interior darkness plus a few opaque identity pixels. Qi light lives on
the separate glow sprites: a carved bone skull crowns the lift, its forehead crystal and eye
sockets waking when the player is near.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image

import gauge_frames
import qfe_previews
from pixel_kit import TRANSPARENT, RGBA, px, hline, vline, rect, with_alpha, crop, checker_preview
from qfe_palette import (
    N_GLEAM, N_LIGHT, N_MID, N_SOFT, N_SHADOW, N_DARK, DOOR_MID, DOOR_DARK,
    PLATE_HI, PLATE_MID, PLATE_SHADOW, BRASS, BRASS_HI,
    INTERIOR, RIVET, COPPER, COPPER_DARK, DEAD_SOCKET,
    FLASH_WHITE, GOLD_GLEAM, CAVE_DUST, SPARK_WHITE, SPARK_BLUE,
    QI_DARK, QI_PURPLE, QI_VIOLET, QI_LIGHT, QI_GLOW, QI_WHITE,
)

PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / "assets"
REFERENCES = PROJECT / "references"

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
GAUGE_RECTS = {
    "top": GAUGE_TOP, "middle": GAUGE_MIDDLE, "bottom": GAUGE_BOTTOM,
    "marker": GAUGE_MARKER, "icon": GAUGE_ICON, "fill": GAUGE_FILL, "ember": GAUGE_EMBER,
}


def draw_shell(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """The machine's wall-tracking mass: a carved bone skull crown, rock shoulders, lintel,
    jambs, slatted doors with Qi-diamond inlays, brass straps, kick plates, and a threshold
    that dissolves into the floor. Mirrored around x=8."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def m(x: int, y: int, color: RGBA) -> None:
        p(x, y, color)
        p(16 - x, y, color)

    def h(x1: int, x2: int, y: int, color: RGBA) -> None:
        hline(sheet, ox + x1, ox + x2, oy + y, color)

    # Skull crown carved from bone: a wide cranium dome, two-pixel eye sockets under a bone
    # brow, inset cheeks, nose notch, and a small tooth row resting on the lintel jaw.
    # The accents sprite darkens the sockets; the glow sprite wakes them.
    m(7, 0, with_alpha(PLATE_SHADOW, 168)); p(8, 0, PLATE_SHADOW)
    m(5, 1, with_alpha(PLATE_SHADOW, 168)); m(6, 1, PLATE_HI); m(7, 1, PLATE_HI); p(8, 1, PLATE_MID)
    m(4, 2, with_alpha(PLATE_SHADOW, 168)); m(5, 2, PLATE_HI); m(6, 2, PLATE_HI)
    m(7, 2, PLATE_HI); p(8, 2, PLATE_MID)
    m(4, 3, PLATE_SHADOW); m(5, 3, PLATE_MID); m(6, 3, PLATE_MID); m(7, 3, PLATE_HI); p(8, 3, PLATE_HI)
    m(5, 4, PLATE_SHADOW); m(6, 4, PLATE_MID); m(7, 4, PLATE_MID); p(8, 4, PLATE_MID)
    for x in range(6, 11):
        p(x, 5, PLATE_SHADOW if x % 2 == 0 else PLATE_MID)
    m(6, 5, with_alpha(PLATE_SHADOW, 168))

    # Carved rock shoulders hugging the crown.
    m(3, 3, with_alpha(N_SOFT, 168))
    m(2, 4, with_alpha(N_SOFT, 112)); m(3, 4, N_SOFT)
    m(1, 5, with_alpha(N_SOFT, 64)); m(2, 5, N_SOFT); m(3, 5, N_SHADOW)

    # Lintel beam with a lighter inset and a single carved descent chevron.
    h(3, 13, 6, N_SHADOW); h(5, 11, 6, N_MID); p(8, 6, N_SHADOW)
    h(3, 13, 7, N_DARK)

    # Jambs: lit left face, shaded right face; the outermost columns are translucent so the
    # silhouette dissolves into the patched wall instead of cutting a hard rectangle.
    vline(sheet, ox + 2, oy + 8, oy + 26, with_alpha(N_DARK, 168))
    vline(sheet, ox + 3, oy + 8, oy + 26, N_MID)
    vline(sheet, ox + 13, oy + 8, oy + 26, N_SHADOW)
    vline(sheet, ox + 14, oy + 8, oy + 26, with_alpha(N_DARK, 168))
    for y in (10, 17, 24):
        p(3, y, N_LIGHT)
    for y in (12, 20):
        p(13, y, N_SOFT)

    # Twin doors: vertical slats one step darker than the frame; the seam is accent-dark.
    for y in range(9, 26):
        p(4, y, DOOR_DARK); p(5, y, DOOR_MID); p(6, y, DOOR_DARK); p(7, y, DOOR_MID)
        p(9, y, DOOR_MID); p(10, y, DOOR_DARK); p(11, y, DOOR_MID); p(12, y, DOOR_DARK)
    for x, y in ((5, 10), (9, 11), (7, 20), (11, 10)):
        p(x, y, N_LIGHT)

    # Qi-diamond inlays recessed into each leaf; their centers light with the awakened seam.
    for cx in (5, 11):
        p(cx, 15, N_SHADOW); p(cx - 1, 16, N_SHADOW); p(cx + 1, 16, N_MID)
        p(cx, 17, N_MID); p(cx, 16, DOOR_DARK)

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
    m(2, 28, with_alpha(N_SOFT, 168))
    h(3, 13, 29, N_SHADOW)
    m(3, 29, with_alpha(N_SHADOW, 168))
    h(5, 11, 30, with_alpha(N_DARK, 168))
    m(1, 28, with_alpha(N_SOFT, 168)); m(0, 28, with_alpha(N_SOFT, 64))
    m(2, 29, with_alpha(N_SHADOW, 112))
    m(4, 30, N_SHADOW); m(3, 30, with_alpha(N_SOFT, 112)); m(2, 30, with_alpha(N_SOFT, 64))
    m(5, 31, with_alpha(N_SHADOW, 64)); m(7, 31, with_alpha(N_SOFT, 64))

    # Rock bites over the jamb edges tie the silhouette into the patched wall.
    m(0, 9, with_alpha(N_SOFT, 64)); m(1, 9, N_SOFT); m(2, 9, with_alpha(N_SHADOW, 168))
    m(1, 10, N_SHADOW)
    m(0, 14, with_alpha(N_SOFT, 112)); m(1, 14, N_LIGHT); m(2, 14, with_alpha(N_SHADOW, 112))
    m(1, 15, N_SOFT); m(0, 15, with_alpha(N_SHADOW, 64))
    m(1, 20, N_SOFT); m(0, 20, with_alpha(N_SOFT, 64)); m(1, 21, with_alpha(N_SHADOW, 168))


def draw_accents(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Translucent interior darkness (the wall shows through, so contrast scales with the room),
    the skull's sockets, and the few opaque identity pixels: rivets and the copper node."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def h(x1: int, x2: int, y: int, color: RGBA) -> None:
        hline(sheet, ox + x1, ox + x2, oy + y, color)

    soft = with_alpha(INTERIOR, 112)
    deep = with_alpha(INTERIOR, 168)
    p(8, 1, deep); p(8, 2, soft)
    p(5, 3, deep); p(6, 3, deep); p(10, 3, deep); p(11, 3, deep)
    p(8, 4, soft)
    h(4, 12, 7, soft)
    h(4, 12, 8, soft)
    vline(sheet, ox + 8, oy + 9, oy + 25, soft)
    p(8, 9, deep); p(8, 25, deep)
    h(4, 12, 26, soft)
    h(3, 13, 27, with_alpha(INTERIOR, 64))
    for y in (13, 19):
        p(6, y, with_alpha(RIVET, 168)); p(10, y, with_alpha(RIVET, 168))
    p(13, 23, COPPER); p(13, 24, COPPER_DARK)


def draw_broken_shell(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Copy the shell, then break it: the skull's right brow is gone, a plank has sheared,
    a strap dangles, and rubble chips rest at the threshold."""
    ox, oy = origin
    sx, sy = LIFT_SHELL
    sheet.paste(sheet.crop((sx, sy, sx + LIFT[0], sy + LIFT[1])), (ox, oy))

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    p(9, 1, TRANSPARENT); p(10, 1, TRANSPARENT)
    p(10, 2, TRANSPARENT); p(11, 2, TRANSPARENT)
    p(9, 2, PLATE_SHADOW); p(11, 3, PLATE_SHADOW); p(10, 3, PLATE_SHADOW)
    p(4, 15, N_LIGHT); p(5, 16, N_SOFT); p(6, 17, N_SHADOW)
    p(4, 13, N_SHADOW); p(5, 13, N_SOFT)
    p(6, 19, N_SHADOW); p(7, 19, N_SOFT)
    p(6, 20, BRASS); p(6, 21, BRASS); p(6, 22, BRASS_HI)
    p(4, 30, N_SOFT); p(12, 30, N_SHADOW)
    p(5, 31, with_alpha(N_SHADOW, 112)); p(11, 31, with_alpha(N_SOFT, 112))


def draw_broken_accents(sheet: Image.Image, origin: tuple[int, int]) -> None:
    """Broken interior darkness: dead socket, one hollow eye, crack path, sheared gap,
    sprung right leaf, dead power node."""
    ox, oy = origin
    ax, ay = LIFT_ACCENTS
    sheet.paste(sheet.crop((ax, ay, ax + LIFT[0], ay + LIFT[1])), (ox, oy))

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    deep = with_alpha(INTERIOR, 168)
    soft = with_alpha(INTERIOR, 112)
    p(10, 3, TRANSPARENT); p(11, 3, TRANSPARENT)
    p(9, 3, soft)
    p(7, 5, soft)
    p(6, 6, deep); p(6, 9, deep); p(5, 10, deep); p(6, 11, soft)
    p(4, 14, deep); p(5, 15, deep); p(6, 16, deep); p(7, 17, deep)
    for y in range(9, 26):
        p(12, y, soft)
    p(12, 10, deep); p(12, 18, deep)
    p(13, 23, DEAD_SOCKET); p(13, 24, deep)


def draw_glow(sheet: Image.Image, origin: tuple[int, int], active: bool) -> None:
    """Only untinted Qi light. Idle: a dim forehead ember and the faintest eye hint. Active:
    the crystal wakes, the eye sockets glow, the seam and door diamonds light, and a soft
    violet pool spills onto the floor."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    def e(x: int, y: int, color: RGBA) -> None:
        p(x, y, color)
        p(16 - x, y, color)

    if not active:
        p(8, 0, with_alpha(QI_DARK, 64))
        p(8, 1, with_alpha(QI_PURPLE, 168))
        p(8, 2, with_alpha(QI_DARK, 112))
        e(6, 3, with_alpha(QI_DARK, 64))
        return

    p(8, 0, with_alpha(QI_GLOW, 112))
    p(8, 1, QI_WHITE)
    p(8, 2, QI_LIGHT)
    e(7, 1, with_alpha(QI_PURPLE, 112))
    e(7, 0, with_alpha(QI_GLOW, 64))
    e(6, 3, with_alpha(QI_VIOLET, 168))
    e(5, 3, with_alpha(QI_PURPLE, 112))
    e(4, 2, with_alpha(QI_GLOW, 64))
    for y in range(9, 26):
        if y in (13, 19):
            p(8, y, QI_LIGHT)
        elif y % 3 == 0:
            p(8, y, with_alpha(QI_PURPLE, 168))
        else:
            p(8, y, with_alpha(QI_PURPLE, 112) if y % 2 else with_alpha(QI_DARK, 112))
    e(5, 16, QI_VIOLET)
    e(5, 17, with_alpha(QI_PURPLE, 112))
    e(7, 28, with_alpha(QI_PURPLE, 112)); p(8, 28, with_alpha(QI_GLOW, 168))
    e(5, 29, with_alpha(QI_DARK, 28)); e(6, 29, with_alpha(QI_PURPLE, 64))
    e(7, 29, with_alpha(QI_GLOW, 112)); p(8, 29, with_alpha(QI_GLOW, 168))
    e(6, 30, with_alpha(QI_DARK, 28)); e(7, 30, with_alpha(QI_PURPLE, 64)); p(8, 30, with_alpha(QI_PURPLE, 112))
    e(7, 31, with_alpha(QI_DARK, 16)); p(8, 31, with_alpha(QI_PURPLE, 28))


def draw_broken_glow(sheet: Image.Image, origin: tuple[int, int]) -> None:
    px(sheet, origin[0] + 8, origin[1] + 1, with_alpha(QI_DARK, 112))


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

    p(8, 1, QI_GLOW); p(8, 2, QI_WHITE); p(8, 3, QI_GLOW); p(8, 4, QI_VIOLET)
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
    """Crown ignition flares centered on the skull's forehead crystal."""
    ox, oy = origin

    def p(x: int, y: int, color: RGBA) -> None:
        px(sheet, ox + x, oy + y, color)

    if not big:
        p(8, 0, QI_GLOW)
        p(7, 0, with_alpha(QI_VIOLET, 168)); p(9, 0, with_alpha(QI_VIOLET, 168))
        p(7, 1, QI_GLOW); p(8, 1, QI_WHITE); p(9, 1, QI_GLOW)
        p(7, 2, QI_VIOLET); p(8, 2, QI_GLOW); p(9, 2, QI_VIOLET)
        p(8, 3, with_alpha(QI_GLOW, 168))
        return
    p(8, 1, QI_WHITE)
    p(8, 0, QI_WHITE); p(8, 2, QI_WHITE); p(7, 1, QI_GLOW); p(9, 1, QI_GLOW)
    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        p(8 + dx, 1 + dy, QI_GLOW)
    p(8, 4, with_alpha(QI_GLOW, 168))
    p(5, 1, with_alpha(QI_GLOW, 168)); p(11, 1, with_alpha(QI_GLOW, 168))
    p(4, 1, with_alpha(QI_GLOW, 64)); p(12, 1, with_alpha(QI_GLOW, 64))
    p(6, 3, with_alpha(QI_GLOW, 112)); p(10, 3, with_alpha(QI_GLOW, 112))


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

    # The seam must be continuous, translucent (wall shows through), and warm—never pure black.
    accents = crop(sheet, LIFT_ACCENTS)
    for y in range(9, 26):
        pixel = accents.getpixel((8, y))
        assert pixel[:3] == INTERIOR[:3] and 0 < pixel[3] < 255, f"seam wrong at y={y}: {pixel}"

    # Skull sockets: forehead + both eyes present on the accents, lit on the active glow.
    for x, y in ((8, 1), (5, 3), (6, 3), (10, 3), (11, 3)):
        assert accents.getpixel((x, y))[3] > 0, f"socket missing at {(x, y)}"
    active = crop(sheet, GLOW_ACTIVE)
    for x, y in ((8, 1), (6, 3), (10, 3), (5, 16), (11, 16)):
        assert active.getpixel((x, y))[3] > 0, f"active glow missing at {(x, y)}"
    box = active.getbbox()
    assert box is not None and box[1] == 0 and box[3] >= 31, box

    assert crop(sheet, GLOW_IDLE).getbbox() == (6, 0, 11, 4)
    assert crop(sheet, BROKEN_GLOW).getbbox() == (8, 1, 9, 2)
    assert list(crop(sheet, BROKEN_SHELL).getdata()) != list(crop(sheet, LIFT_SHELL).getdata())

    seam = crop(sheet, SEAM)
    for y in range(1, 28):
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
    gauge_frames.draw_gauge(sheet, GAUGE_RECTS)
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
    qfe_previews.compose_gauge(sheet, GAUGE_RECTS).save(REFERENCES / "qfe-gauge-preview-4x.png")
    print("Wrote assets/qfe-sprites.png (176x96) and refreshed previews.")


if __name__ == "__main__":
    main()
