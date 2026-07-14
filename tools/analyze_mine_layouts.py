from __future__ import annotations

from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw


GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley")
MAPS = GAME / "Content (unpacked)" / "Maps" / "Mines"
PROJECT = Path(__file__).resolve().parents[1]
REFERENCES = PROJECT / "references"
TILE_SIZE = 16
FLIP_H = 0x80000000
FLIP_V = 0x40000000
FLIP_D = 0x20000000
GID_MASK = 0x1FFFFFFF


def parse_layout(path: Path) -> tuple[int, int, list[tuple[str, list[int]]]]:
    root = ET.parse(path).getroot()
    width = int(root.attrib["width"])
    height = int(root.attrib["height"])
    layers: list[tuple[str, list[int]]] = []
    for layer in root.findall("layer"):
        data = layer.find("data")
        if data is None or data.attrib.get("encoding") != "csv":
            continue
        gids = [int(value.strip()) for value in (data.text or "").replace("\n", "").split(",") if value.strip()]
        layers.append((layer.attrib["name"], gids))
    return width, height, layers


def get_tile(sheet: Image.Image, encoded_gid: int) -> Image.Image | None:
    gid = encoded_gid & GID_MASK
    if gid == 0:
        return None
    tile_id = gid - 1
    x = tile_id % 16 * TILE_SIZE
    y = tile_id // 16 * TILE_SIZE
    tile = sheet.crop((x, y, x + TILE_SIZE, y + TILE_SIZE))
    if encoded_gid & FLIP_D:
        tile = tile.transpose(Image.Transpose.TRANSPOSE)
    if encoded_gid & FLIP_H:
        tile = tile.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    if encoded_gid & FLIP_V:
        tile = tile.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    return tile


def render_layout(width: int, height: int, layers: list[tuple[str, list[int]]], sheet: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", (width * TILE_SIZE, height * TILE_SIZE), (0, 0, 0, 0))
    for _, gids in layers:
        for index, encoded_gid in enumerate(gids):
            tile = get_tile(sheet, encoded_gid)
            if tile is None:
                continue
            canvas.alpha_composite(tile, (index % width * TILE_SIZE, index // width * TILE_SIZE))
    return canvas


def find_ladder(width: int, layers: list[tuple[str, list[int]]]) -> tuple[int, int]:
    buildings = next(gids for name, gids in layers if name == "Buildings")
    for index, encoded_gid in enumerate(buildings):
        if (encoded_gid & GID_MASK) - 1 == 115:
            return index % width, index // width
    raise ValueError("No exit ladder")


def mean_luma(image: Image.Image) -> float:
    pixels = [pixel for pixel in image.getdata() if pixel[3] > 0]
    if not pixels:
        return 0.0
    return sum(0.2126 * r + 0.7152 * g + 0.0722 * b for r, g, b, _ in pixels) / len(pixels)


def layer_ids_at(
    width: int,
    layers: list[tuple[str, list[int]]],
    tile_x: int,
    tile_y: int,
) -> str:
    result = []
    for layer_name, gids in layers:
        tile_ids = []
        for y in (tile_y, tile_y + 1):
            gid = gids[y * width + tile_x] & GID_MASK
            tile_ids.append("-" if gid == 0 else str(gid - 1))
        result.append(f"{layer_name[0]}:{'/'.join(tile_ids)}")
    return " ".join(result)


def main() -> None:
    REFERENCES.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(MAPS / "mine_desert.png").convert("RGBA")
    cells: list[tuple[int, Image.Image]] = []
    for layout_number in range(1, 40):
        path = MAPS / f"{layout_number}.tmx"
        if not path.exists():
            continue
        width, height, layers = parse_layout(path)
        ladder_x, ladder_y = find_ladder(width, layers)
        rendered = render_layout(width, height, layers, sheet)
        top_y = ladder_y - 1
        side_samples = []
        for side_name, tile_x in (("L", ladder_x - 1), ("R", ladder_x + 1)):
            region = rendered.crop((
                tile_x * TILE_SIZE,
                top_y * TILE_SIZE,
                (tile_x + 1) * TILE_SIZE,
                (top_y + 2) * TILE_SIZE,
            ))
            side_samples.append(
                f"{side_name} luma={mean_luma(region):5.1f} {layer_ids_at(width, layers, tile_x, top_y)}"
            )
        print(f"layout {layout_number:2}: " + " | ".join(side_samples))
        left = max(0, ladder_x - 5) * TILE_SIZE
        top = max(0, ladder_y - 4) * TILE_SIZE
        right = min(width, ladder_x + 6) * TILE_SIZE
        bottom = min(height, ladder_y + 4) * TILE_SIZE
        crop = rendered.crop((left, top, right, bottom))
        frame = Image.new("RGBA", (184, 148), (12, 9, 15, 255))
        frame.alpha_composite(crop, ((frame.width - crop.width) // 2, 18))
        ImageDraw.Draw(frame).text((4, 3), f"layout {layout_number}", fill=(255, 235, 190, 255))
        cells.append((layout_number, frame))

    columns = 5
    rows = (len(cells) + columns - 1) // columns
    contact = Image.new("RGBA", (columns * 184, rows * 148), (5, 3, 7, 255))
    for index, (_, frame) in enumerate(cells):
        contact.alpha_composite(frame, ((index % columns) * 184, (index // columns) * 148))

    output = REFERENCES / "skull-layout-entrances.png"
    contact.save(output)
    tile_atlas = Image.new("RGBA", (8 * 48, 11 * 56), (12, 9, 15, 255))
    tile_draw = ImageDraw.Draw(tile_atlas)
    for index, tile_id in enumerate(range(80, 168)):
        tile_image = get_tile(sheet, tile_id + 1)
        if tile_image is None:
            continue
        x = index % 8 * 48
        y = index // 8 * 56
        tile_atlas.alpha_composite(tile_image.resize((48, 48), Image.Resampling.NEAREST), (x, y + 8))
        tile_draw.text((x + 2, y), str(tile_id), fill=(255, 235, 190, 255))
    tile_output = REFERENCES / "skull-wall-tiles-labeled.png"
    tile_atlas.save(tile_output)
    print(f"Wrote {output}")
    print(f"Wrote {tile_output}")


if __name__ == "__main__":
    main()
