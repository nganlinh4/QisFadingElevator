"""Shared QFE palette. Shell values are near-white relief multiplied by wall tint at runtime;
accent/Qi/gauge colors are absolute. Alphas must stay within {0,16,28,40,64,112,168,255}."""
from __future__ import annotations

RGBA = tuple[int, int, int, int]


def grey(value: int) -> RGBA:
    return (value, value, value, 255)


def warm(value: int, ratio: tuple[float, float, float]) -> RGBA:
    return (round(value * ratio[0]), round(value * ratio[1]), round(value * ratio[2]), 255)


# Shell relief values (multiplied by wall tint x1.30 at runtime; 212 x 1.30 = wall parity).
N_GLEAM = grey(244)
N_LIGHT = grey(228)
N_MID = grey(212)
N_SOFT = grey(192)
N_SHADOW = grey(170)
N_DARK = grey(142)
DOOR_MID = grey(202)
DOOR_DARK = grey(182)
# Bone plate: the palest fixture element in every room, echoing the cavern's skull motifs.
BONE_RATIO = (1.0, 0.95, 0.86)
PLATE_HI = warm(246, BONE_RATIO)
PLATE_MID = warm(224, BONE_RATIO)
PLATE_SHADOW = warm(194, BONE_RATIO)
# Brass straps: a whisper warmer than the wall, never a saturated orange pop.
BRASS_RATIO = (1.0, 0.92, 0.78)
BRASS = warm(176, BRASS_RATIO)
BRASS_HI = warm(236, BRASS_RATIO)
# Accent darkness: warm mauve like vanilla's own shadow lines (skull door darks eyedrop to
# ~(72,48,52)), applied translucently so gaps stay darker siblings of each room's wall.
INTERIOR = (58, 40, 42, 255)
RIVET = (52, 38, 34, 255)
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
