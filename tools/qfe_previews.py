"""Compose in-context reference previews for the QFE sprite sheet."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

from pixel_kit import crop, multiply_tint, scale_nearest

GAME = Path(r"C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley")

# Wall-only patches sampled from live screenshots (2026-07-15), the calibration ground truth.
LIVE_WALLS = [
    ("pale", "StardewModdingAPI 2026-07-15 10-44-26_536.png", (1240, 340, 1360, 500)),
    ("tan", "StardewModdingAPI 2026-07-15 10-44-43_071.png", (1330, 170, 1450, 330)),
    ("foyer", "StardewModdingAPI 2026-07-15 10-45-51_819.png", (1128, 320, 1248, 480)),
]

TINT_COMPENSATION = 1.30


def median_color(image: Image.Image) -> tuple[int, int, int]:
    pixels = list(image.convert("RGB").getdata())
    return tuple(sorted(p[c] for p in pixels)[len(pixels) // 2] for c in range(3))


def wall_tint(wall: tuple[int, int, int]) -> tuple[int, int, int]:
    return tuple(min(255, round(c * TINT_COMPENSATION)) for c in wall)


def compose_lift(sheet: Image.Image, shell: tuple, accents: tuple, overlay: tuple,
                 tint: tuple[int, int, int]) -> Image.Image:
    """Simulate the runtime three-sprite composite at native scale."""
    composed = multiply_tint(crop(sheet, shell), tint)
    composed.alpha_composite(crop(sheet, accents))
    composed.alpha_composite(crop(sheet, overlay))
    return composed


def compose_live_walls(sheet: Image.Image, references: Path, lift: dict[str, tuple]) -> Image.Image:
    """Paste the simulated composite onto real screenshot wall patches and verify luma parity."""
    cells = []
    for name, filename, box in LIVE_WALLS:
        patch = Image.open(GAME / filename).convert("RGBA").crop(box)
        wall = median_color(patch)
        pair = {
            "repaired": compose_lift(sheet, lift["shell"], lift["accents"], lift["glow_active"], wall_tint(wall)),
            "broken": compose_lift(sheet, lift["broken_shell"], lift["broken_accents"], lift["broken_glow"], wall_tint(wall)),
        }
        for label, composite in pair.items():
            cell = patch.copy()
            cell.alpha_composite(scale_nearest(composite, 4), ((cell.width - 68) // 2, cell.height - 138))
            cells.append(cell)

            opaque = [p for p in composite.getdata() if p[3] == 255]
            fix_lumas = sorted(0.2126 * p[0] + 0.7152 * p[1] + 0.0722 * p[2] for p in opaque)
            fix_med = fix_lumas[len(fix_lumas) // 2]
            wall_med = 0.2126 * wall[0] + 0.7152 * wall[1] + 0.0722 * wall[2]
            ratio = fix_med / max(1.0, wall_med)
            print(f"live-wall {name:6} {label:9} fixture/wall luma ratio: {ratio:.2f}")
            if name != "foyer" and label == "repaired":
                assert 0.80 <= ratio <= 1.20, f"{name} fixture drifts from wall parity ({ratio:.2f})"

    strip = Image.new("RGBA", (sum(c.width for c in cells), max(c.height for c in cells)), (12, 9, 15, 255))
    x = 0
    for cell in cells:
        strip.alpha_composite(cell, (x, 0))
        x += cell.width
    return strip


def compose_repair_sequence(sheet: Image.Image, references: Path, states: list[tuple]) -> Image.Image:
    """A contact strip of (shell, accents, overlay) beats on the vanilla foyer map."""
    foyer = Image.open(references / "vanilla-skullcave-map.png").convert("RGBA")
    tint = wall_tint(median_color(foyer.crop((66, 30, 94, 62))))
    scaled_map = scale_nearest(foyer, 4)
    crops = []
    for shell, accents, overlay in states:
        scene = scaled_map.copy()
        scene.alpha_composite(scale_nearest(compose_lift(sheet, shell, accents, overlay, tint), 4), (286, 128))
        crops.append(scene.crop((184, 72, 416, 304)))
    strip = Image.new("RGBA", (crops[0].width * len(crops), crops[0].height), (12, 9, 15, 255))
    for index, tile in enumerate(crops):
        strip.alpha_composite(tile, (index * tile.width, 0))
    return strip


def compose_gauge(sheet: Image.Image, rects: dict[str, tuple]) -> Image.Image:
    """Assemble the modular gauge the same way DepthGauge.Draw does."""
    native = Image.new("RGBA", (64, 84), (12, 9, 15, 255))
    x, y = 10, 16
    native.alpha_composite(crop(sheet, rects["icon"]), (x + 1, 2))
    native.alpha_composite(crop(sheet, rects["top"]), (x, y - 5))
    for row in range(24):
        native.alpha_composite(crop(sheet, rects["middle"]), (x, y + row * 2))
    native.alpha_composite(crop(sheet, rects["bottom"]), (x, y + 48))
    for row in range(13):
        native.alpha_composite(crop(sheet, rects["fill"]), (x + 3, y + row * 2))
    native.alpha_composite(crop(sheet, rects["marker"]), (x - 1, y + 24))
    return scale_nearest(native, 3)
