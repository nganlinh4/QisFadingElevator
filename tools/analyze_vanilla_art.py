from __future__ import annotations

from collections import Counter
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image


PROJECT = Path(__file__).resolve().parents[1]
GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley")
UNPACKED = GAME / "Content (unpacked)" / "Maps"
REFERENCES = PROJECT / "references"


def nearest_preview(image: Image.Image, factor: int = 8) -> Image.Image:
    return image.resize((image.width * factor, image.height * factor), Image.Resampling.NEAREST)


def nontransparent_palette(image: Image.Image, limit: int = 24) -> list[tuple[int, tuple[int, int, int, int]]]:
    rgba = image.convert("RGBA")
    colors = Counter(pixel for pixel in rgba.getdata() if pixel[3] > 0)
    return colors.most_common(limit)


def tile(sheet: Image.Image, tile_id: int, columns: int, tile_size: int = 16) -> Image.Image:
    x = tile_id % columns * tile_size
    y = tile_id // columns * tile_size
    return sheet.crop((x, y, x + tile_size, y + tile_size))


def render_skull_cave() -> Image.Image:
    tmx_path = UNPACKED / "SkullCave.tmx"
    root = ET.parse(tmx_path).getroot()
    width = int(root.attrib["width"])
    height = int(root.attrib["height"])
    tile_size = int(root.attrib["tilewidth"])

    sheets = {
        "paths": Image.open(UNPACKED / "paths.png").convert("RGBA"),
        "mine": Image.open(UNPACKED / "Mines" / "mine_desert.png").convert("RGBA"),
    }
    canvas = Image.new("RGBA", (width * tile_size, height * tile_size), (0, 0, 0, 0))

    for layer in root.findall("layer"):
        data = layer.find("data")
        if data is None or data.attrib.get("encoding") != "csv":
            continue
        gids = [int(value.strip()) for value in (data.text or "").replace("\n", "").split(",") if value.strip()]
        for index, gid in enumerate(gids):
            if gid == 0:
                continue
            if gid < 65:
                source = tile(sheets["paths"], gid - 1, 4, tile_size)
            else:
                source = tile(sheets["mine"], gid - 65, 16, tile_size)
            x = index % width * tile_size
            y = index // width * tile_size
            canvas.alpha_composite(source, (x, y))

    return canvas


def main() -> None:
    REFERENCES.mkdir(parents=True, exist_ok=True)
    mine = Image.open(UNPACKED / "Mines" / "mine_desert.png").convert("RGBA")
    objects = Image.open(UNPACKED / "springobjects.png").convert("RGBA")
    mine_base = Image.open(UNPACKED / "Mines" / "mine.png").convert("RGBA")

    # The vanilla SkullDoor action is tile (3,3). Its two visible tiles are local IDs 270 and 286.
    vanilla_door = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    vanilla_door.alpha_composite(tile(mine, 270, 16), (0, 0))
    vanilla_door.alpha_composite(tile(mine, 286, 16), (0, 16))
    vanilla_door.save(REFERENCES / "vanilla-skull-door.png")
    nearest_preview(vanilla_door).save(REFERENCES / "vanilla-skull-door-8x.png")

    qi_gem = tile(objects, 858, 24)
    qi_gem.save(REFERENCES / "vanilla-qi-gem.png")
    nearest_preview(qi_gem).save(REFERENCES / "vanilla-qi-gem-8x.png")

    mine_elevator_region = mine_base.crop((0, 48, 64, 176))
    mine_elevator_region.save(REFERENCES / "vanilla-mine-elevator-region.png")
    nearest_preview(mine_elevator_region, 4).save(REFERENCES / "vanilla-mine-elevator-region-4x.png")

    foyer = render_skull_cave()
    foyer.save(REFERENCES / "vanilla-skullcave-map.png")
    nearest_preview(foyer, 4).save(REFERENCES / "vanilla-skullcave-map-4x.png")

    wall_region = foyer.crop((16, 16, 112, 80))
    wall_region.save(REFERENCES / "vanilla-skullcave-wall-region.png")
    nearest_preview(wall_region).save(REFERENCES / "vanilla-skullcave-wall-region-8x.png")

    print("Vanilla door palette:")
    for color, count in nontransparent_palette(vanilla_door):
        print(f"  {color[:3]} #{color[0]:02x}{color[1]:02x}{color[2]:02x}  count={count}")
    print("\nFoyer wall-region palette:")
    for color, count in nontransparent_palette(wall_region):
        print(f"  {color[:3]} #{color[0]:02x}{color[1]:02x}{color[2]:02x}  count={count}")
    print("\nQi Gem palette:")
    for color, count in nontransparent_palette(qi_gem):
        print(f"  {color[:3]} #{color[0]:02x}{color[1]:02x}{color[2]:02x}  count={count}")
    print(f"\nWrote references to {REFERENCES}")


if __name__ == "__main__":
    main()
