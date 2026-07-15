"""Small drawing/preview helpers shared by the QFE sprite builder."""
from __future__ import annotations

from PIL import Image, ImageDraw

RGBA = tuple[int, int, int, int]
TRANSPARENT: RGBA = (0, 0, 0, 0)


def px(image: Image.Image, x: int, y: int, color: RGBA) -> None:
    image.putpixel((x, y), color)


def hline(image: Image.Image, x1: int, x2: int, y: int, color: RGBA) -> None:
    for x in range(x1, x2 + 1):
        px(image, x, y, color)


def vline(image: Image.Image, x: int, y1: int, y2: int, color: RGBA) -> None:
    for y in range(y1, y2 + 1):
        px(image, x, y, color)


def rect(image: Image.Image, x1: int, y1: int, x2: int, y2: int, color: RGBA) -> None:
    for y in range(y1, y2 + 1):
        hline(image, x1, x2, y, color)


def with_alpha(color: RGBA, alpha: int) -> RGBA:
    return color[0], color[1], color[2], alpha


def multiply_tint(image: Image.Image, tint: tuple[int, int, int]) -> Image.Image:
    result = Image.new("RGBA", image.size, TRANSPARENT)
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = image.getpixel((x, y))
            result.putpixel((x, y), (r * tint[0] // 255, g * tint[1] // 255, b * tint[2] // 255, a))
    return result


def crop(sheet: Image.Image, origin: tuple, size: tuple[int, int] = (17, 32)) -> Image.Image:
    """Crop a sprite by (x, y[, w, h]) origin tuple; 17x32 lift frames by default."""
    width, height = (origin[2], origin[3]) if len(origin) == 4 else size
    return sheet.crop((origin[0], origin[1], origin[0] + width, origin[1] + height))


def checker_preview(image: Image.Image, factor: int = 8) -> Image.Image:
    background = Image.new("RGBA", image.size, (224, 224, 224, 255))
    draw = ImageDraw.Draw(background)
    for y in range(0, image.height, 4):
        for x in range(0, image.width, 4):
            if (x // 4 + y // 4) % 2:
                draw.rectangle((x, y, x + 3, y + 3), fill=(184, 184, 184, 255))
    background.alpha_composite(image)
    return background.resize((image.width * factor, image.height * factor), Image.Resampling.NEAREST)


def scale_nearest(image: Image.Image, factor: int) -> Image.Image:
    return image.resize((image.width * factor, image.height * factor), Image.Resampling.NEAREST)
