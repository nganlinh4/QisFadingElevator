from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np
from PIL import Image


GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley")
PROJECT = Path(__file__).resolve().parents[1]


def mean_luma(rgb: np.ndarray) -> float:
    if rgb.size == 0:
        return 0.0
    return float(np.mean(rgb[..., 0] * 0.2126 + rgb[..., 1] * 0.7152 + rgb[..., 2] * 0.0722))


def mean_saturation(rgb: np.ndarray) -> float:
    if rgb.size == 0:
        return 0.0
    hsv = cv2.cvtColor(rgb.reshape((-1, 1, 3)).astype(np.uint8), cv2.COLOR_RGB2HSV)
    return float(np.mean(hsv[..., 1]))


def main() -> None:
    latest = sorted(GAME.glob("StardewModdingAPI *.png"), key=lambda p: p.stat().st_mtime, reverse=True)[:2]
    if len(latest) < 2:
        raise SystemExit("Need two Stardew screenshots")

    atlas = Image.open(PROJECT / "assets" / "qfe-sprites.png").convert("RGBA")
    active = atlas.crop((18, 0, 36, 32)).resize((72, 128), Image.Resampling.NEAREST)
    template_rgba = np.array(active)
    template_bgr = cv2.cvtColor(template_rgba[..., :3], cv2.COLOR_RGB2BGR)
    mask = np.where(template_rgba[..., 3] > 0, 255, 0).astype(np.uint8)
    sprite_height, sprite_width = mask.shape

    print("Screenshots:")
    for path in latest:
        print(f"  {path.name}: {Image.open(path).size}")

    template_gray = cv2.cvtColor(template_bgr, cv2.COLOR_BGR2GRAY)
    template_edges = cv2.Canny(template_gray, 30, 90)
    for path in latest:
        screen_bgr = cv2.imread(str(path), cv2.IMREAD_COLOR)
        screen_height, screen_width = screen_bgr.shape[:2]
        search_left = int(screen_width * 0.20)
        search_right = int(screen_width * 0.80)
        search_bottom = int(screen_height * 0.78)
        search = screen_bgr[:search_bottom, search_left:search_right]
        search_edges = cv2.Canny(cv2.cvtColor(search, cv2.COLOR_BGR2GRAY), 30, 90)
        result = cv2.matchTemplate(search_edges, template_edges, cv2.TM_CCORR_NORMED)
        _, score, _, max_location = cv2.minMaxLoc(result)
        x, y = max_location[0] + search_left, max_location[1]

        screen_rgb = cv2.cvtColor(screen_bgr, cv2.COLOR_BGR2RGB)
        sprite_crop = screen_rgb[y : y + sprite_height, x : x + sprite_width]
        sprite_pixels = sprite_crop[mask > 0]

        pad = 16
        x1, y1 = max(0, x - pad), max(0, y - pad)
        x2, y2 = min(screen_rgb.shape[1], x + sprite_width + pad), min(screen_rgb.shape[0], y + sprite_height + pad)
        ring = screen_rgb[y1:y2, x1:x2]
        ring_mask = np.ones(ring.shape[:2], dtype=bool)
        inner_x, inner_y = x - x1, y - y1
        ring_mask[inner_y : inner_y + sprite_height, inner_x : inner_x + sprite_width] = False
        ring_pixels = ring[ring_mask]

        print(f"\n{path.name}: match={score:.3f}, x={x}, y={y}, size={sprite_width}x{sprite_height}")
        print(f"Sprite luma={mean_luma(sprite_pixels):.1f}, nearby wall luma={mean_luma(ring_pixels):.1f}")
        print(f"Sprite saturation={mean_saturation(sprite_pixels):.1f}, nearby wall saturation={mean_saturation(ring_pixels):.1f}")
    print("A large mismatch supports moving the sprite into the world lighting pass and reducing saturated fill area.")


if __name__ == "__main__":
    main()
