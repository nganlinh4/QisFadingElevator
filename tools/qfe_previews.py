"""Compose in-context reference previews for the QFE sprite sheet."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

from pixel_kit import crop, lerp_toward, multiply_tint, scale_nearest


def compose_foyer(sheet: Image.Image, references: Path, body: tuple, glow: tuple) -> Image.Image:
    """Place a lift body+glow pair onto the vanilla Skull Cavern foyer at tile (4,2)."""
    foyer = Image.open(references / "vanilla-skullcave-map.png").convert("RGBA")
    foyer = scale_nearest(foyer, 4)
    sprite = scale_nearest(crop(sheet, body), 4)
    sprite.alpha_composite(scale_nearest(crop(sheet, glow), 4))
    foyer.alpha_composite(sprite, (286, 128))
    return foyer


def compose_repair_sequence(sheet: Image.Image, references: Path, states: list[tuple]) -> Image.Image:
    """A contact strip of (body, overlay) beats for visual regression checking."""
    crops = []
    for body, overlay in states:
        scene = compose_foyer(sheet, references, body, overlay)
        crops.append(scene.crop((184, 72, 416, 304)))
    strip = Image.new("RGBA", (crops[0].width * len(crops), crops[0].height), (12, 9, 15, 255))
    for index, tile in enumerate(crops):
        strip.alpha_composite(tile, (index * tile.width, 0))
    return strip


def compose_mine(sheet: Image.Image, body: tuple, shroud: tuple, glow: tuple) -> Image.Image:
    """Simulate the runtime layering (body lerp 0.22 toward tint, shroud multiplied) on three walls."""
    tints = [(232, 155, 77), (238, 222, 189), (150, 92, 60)]
    swatches = [(150, 100, 52), (204, 184, 148), (94, 56, 38)]
    strip = Image.new("RGBA", (100 * len(tints), 160), (12, 9, 15, 255))
    for index, (tint, swatch) in enumerate(zip(tints, swatches)):
        cell = Image.new("RGBA", (25, 40), swatch + (255,))
        composed = lerp_toward(crop(sheet, body), tint, 0.22)
        composed.alpha_composite(multiply_tint(crop(sheet, shroud), tint))
        composed.alpha_composite(crop(sheet, glow))
        cell.alpha_composite(composed, (4, 4))
        strip.alpha_composite(scale_nearest(cell, 4), (index * 100, 0))
    return strip


def compose_gauge(sheet: Image.Image, rects: dict[str, tuple]) -> Image.Image:
    """Assemble the modular gauge the same way DepthGauge.Draw does."""
    native = Image.new("RGBA", (72, 84), (12, 9, 15, 255))
    x, y = 12, 18
    native.alpha_composite(crop(sheet, rects["icon"]), (x + 1, 2))
    native.alpha_composite(crop(sheet, rects["top"]), (x, y - 6))
    for row in range(24):
        native.alpha_composite(crop(sheet, rects["middle"]), (x, y + row * 2))
    native.alpha_composite(crop(sheet, rects["bottom"]), (x, y + 48))
    for row in range(13):
        native.alpha_composite(crop(sheet, rects["fill"]), (x + 4, y + row * 2))
    native.alpha_composite(crop(sheet, rects["marker"]), (x - 1, y + 24))
    return scale_nearest(native, 3)
