"""Depth-gauge sprite drawing: a carved sandstone rail with translucent edges, bronze clamp
marker, lift glyph, energy-fill weave, and loss ember. Rect origins come from the builder."""
from __future__ import annotations

from PIL import Image

from pixel_kit import RGBA, px, hline, with_alpha
from qfe_palette import (
    ABYSS, BONE_SHADOW, CAVE_SHADOW, DESERT_LIGHT, DESERT_MID, DESERT_SHADOW,
    GOLD, GOLD_DARK, QI_DARK, QI_GLOW, QI_LIGHT, QI_PURPLE, QI_VIOLET, SAND_LIGHT, SAND_PALE,
)


def rail_row() -> list[RGBA]:
    edge = with_alpha(DESERT_LIGHT, 64)
    stone = with_alpha(DESERT_MID, 168)
    return [edge, stone, DESERT_SHADOW, ABYSS, ABYSS, ABYSS, ABYSS, DESERT_SHADOW, stone, edge]


def draw_gauge(sheet: Image.Image, rects: dict[str, tuple]) -> None:
    # Top cap: crystal finial on a bone bracket, opening into the channel.
    ox, oy = rects["top"][:2]
    px(sheet, ox + 4, oy, QI_VIOLET); px(sheet, ox + 5, oy, QI_DARK)
    px(sheet, ox + 3, oy + 1, QI_DARK); px(sheet, ox + 4, oy + 1, QI_GLOW)
    px(sheet, ox + 5, oy + 1, QI_PURPLE); px(sheet, ox + 6, oy + 1, QI_DARK)
    hline(sheet, ox + 2, ox + 7, oy + 2, SAND_PALE)
    px(sheet, ox + 2, oy + 2, with_alpha(BONE_SHADOW, 168)); px(sheet, ox + 7, oy + 2, with_alpha(BONE_SHADOW, 168))
    hline(sheet, ox + 1, ox + 8, oy + 3, DESERT_MID)
    px(sheet, ox + 1, oy + 3, with_alpha(DESERT_MID, 168)); px(sheet, ox + 8, oy + 3, with_alpha(DESERT_MID, 168))
    px(sheet, ox + 2, oy + 3, SAND_LIGHT)
    for x, color in enumerate(rail_row()):
        px(sheet, ox + x, oy + 4, color)

    # Middle: two identical rows so the body tiles seamlessly.
    ox, oy = rects["middle"][:2]
    for y in range(2):
        for x, color in enumerate(rail_row()):
            px(sheet, ox + x, oy + y, color)

    # Bottom cap: channel mouth, then a carved anchor.
    ox, oy = rects["bottom"][:2]
    for x, color in enumerate(rail_row()):
        px(sheet, ox + x, oy, color)
    hline(sheet, ox + 1, ox + 8, oy + 1, DESERT_SHADOW)
    px(sheet, ox + 1, oy + 1, with_alpha(DESERT_SHADOW, 168)); px(sheet, ox + 8, oy + 1, with_alpha(DESERT_SHADOW, 168))
    hline(sheet, ox + 2, ox + 7, oy + 2, DESERT_MID)
    px(sheet, ox + 2, oy + 2, with_alpha(DESERT_MID, 168))
    px(sheet, ox + 7, oy + 2, SAND_LIGHT)
    hline(sheet, ox + 3, ox + 6, oy + 3, CAVE_SHADOW)
    px(sheet, ox + 4, oy + 4, with_alpha(CAVE_SHADOW, 112)); px(sheet, ox + 5, oy + 4, with_alpha(CAVE_SHADOW, 112))

    # Marker: a bronze clamp gripping the rail, with a lit Qi gem over the channel.
    ox, oy = rects["marker"][:2]
    hline(sheet, ox + 3, ox + 8, oy, with_alpha(GOLD_DARK, 168))
    hline(sheet, ox + 1, ox + 10, oy + 1, GOLD_DARK)
    px(sheet, ox + 1, oy + 1, with_alpha(GOLD_DARK, 168)); px(sheet, ox + 10, oy + 1, with_alpha(GOLD_DARK, 168))
    px(sheet, ox + 2, oy + 1, GOLD)
    hline(sheet, ox, ox + 11, oy + 2, GOLD_DARK)
    px(sheet, ox, oy + 2, with_alpha(GOLD_DARK, 112)); px(sheet, ox + 11, oy + 2, with_alpha(GOLD_DARK, 112))
    px(sheet, ox + 1, oy + 2, GOLD)
    px(sheet, ox + 5, oy + 2, QI_PURPLE); px(sheet, ox + 6, oy + 2, QI_GLOW)
    hline(sheet, ox + 1, ox + 10, oy + 3, CAVE_SHADOW)
    px(sheet, ox + 1, oy + 3, with_alpha(CAVE_SHADOW, 168)); px(sheet, ox + 10, oy + 3, with_alpha(CAVE_SHADOW, 168))
    hline(sheet, ox + 3, ox + 8, oy + 4, with_alpha(CAVE_SHADOW, 168))

    # Icon: a tiny lift glyph naming the instrument, its frame translucent like the rail edges.
    ox, oy = rects["icon"][:2]
    px(sheet, ox + 3, oy, QI_PURPLE); px(sheet, ox + 4, oy, QI_DARK)
    hline(sheet, ox + 2, ox + 5, oy + 1, SAND_PALE)
    hline(sheet, ox + 1, ox + 6, oy + 2, DESERT_SHADOW)
    px(sheet, ox + 1, oy + 2, with_alpha(DESERT_SHADOW, 168)); px(sheet, ox + 6, oy + 2, with_alpha(DESERT_SHADOW, 168))
    for y in range(3, 7):
        px(sheet, ox + 1, oy + y, with_alpha(DESERT_SHADOW, 168))
        px(sheet, ox + 2, oy + y, DESERT_MID)
        px(sheet, ox + 3, oy + y, ABYSS)
        px(sheet, ox + 4, oy + y, DESERT_MID)
        px(sheet, ox + 5, oy + y, DESERT_MID)
        px(sheet, ox + 6, oy + y, with_alpha(DESERT_SHADOW, 168))
    px(sheet, ox + 2, oy + 4, GOLD_DARK); px(sheet, ox + 4, oy + 4, GOLD_DARK)
    hline(sheet, ox + 1, ox + 6, oy + 7, DESERT_SHADOW)
    px(sheet, ox + 1, oy + 7, with_alpha(DESERT_SHADOW, 168)); px(sheet, ox + 6, oy + 7, with_alpha(DESERT_SHADOW, 168))

    # Fill: a 4x2 energy weave tiled inside the channel (code adds a travelling glint).
    ox, oy = rects["fill"][:2]
    px(sheet, ox, oy, with_alpha(QI_DARK, 168)); px(sheet, ox + 1, oy, QI_VIOLET)
    px(sheet, ox + 2, oy, QI_GLOW); px(sheet, ox + 3, oy, with_alpha(QI_PURPLE, 168))
    px(sheet, ox, oy + 1, with_alpha(QI_PURPLE, 168)); px(sheet, ox + 1, oy + 1, QI_PURPLE)
    px(sheet, ox + 2, oy + 1, QI_VIOLET); px(sheet, ox + 3, oy + 1, with_alpha(QI_DARK, 168))

    # Ember: a falling chip for loss feedback.
    ox, oy = rects["ember"][:2]
    px(sheet, ox + 1, oy, QI_GLOW)
    px(sheet, ox, oy + 1, with_alpha(QI_PURPLE, 112)); px(sheet, ox + 1, oy + 1, QI_VIOLET)
    px(sheet, ox + 2, oy + 1, with_alpha(QI_PURPLE, 112))
    px(sheet, ox + 1, oy + 2, with_alpha(QI_DARK, 112))
