#!/usr/bin/env python3
"""Bake the approved light-purple sheet as one transparent sprite per formation."""

from __future__ import annotations

import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


ROOT = Path(__file__).resolve().parents[2]
SOURCE = (
    ROOT
    / "Docs"
    / "ApprovedArtwork"
    / "HudAndSheet"
    / "RegisteredSheetMaster.png"
)
LEVELS = (
    ROOT
    / "MarbleSort"
    / "Assets"
    / "MarbleSort"
    / "Resources"
    / "Levels"
    / "levels.json"
)
OUTPUT = (
    ROOT
    / "MarbleSort"
    / "Assets"
    / "MarbleSort"
    / "Resources"
    / "Presentation"
    / "Surround"
    / "Approved"
    / "Baked"
)
CONTACT_SHEET = (
    ROOT
    / "Docs"
    / "ApprovedArtwork"
    / "HudAndSheet"
    / "BakedPremiumSheetFormations.png"
)

WIDTH = 1536
WORLD_LEFT = -3.32
WORLD_RIGHT = 3.32
WORLD_TOP = 4.70
WORLD_BOTTOM = -0.4165
WORLD_WIDTH = WORLD_RIGHT - WORLD_LEFT
WORLD_HEIGHT = WORLD_TOP - WORLD_BOTTOM
HEIGHT = round(WIDTH * WORLD_HEIGHT / WORLD_WIDTH)
# The approved master turns through roughly 11.9% of its total width at each
# upper corner.  Preserve that ratio after registering the sheet to the game's
# wider white-board aperture.
OUTER_RADIUS = 0.79
OUTER_LOWER_RADIUS = 0.30
RECESS_CLEARANCE = 0.10
RECESS_LOWER_RADIUS = 0.12
LAVENDER_SHADOW = (48, 38, 92)
LAVENDER_MIDTONE = (125, 101, 178)
LAVENDER_HIGHLIGHT = (228, 220, 250)


def formation_key(grid: dict) -> str:
    cells = sorted(
        {
            (int(box["column"]), int(box["row"]))
            for box in grid.get("boxes", [])
            if box is not None
        }
    )
    spacing = round(float(grid.get("cellSpacing", 1.0)) * 1000)
    suffix = "_".join(f"{column}x{row}" for column, row in cells)
    return f"c{int(grid.get('columns', 0))}_s{spacing}_{suffix}"


def to_pixel(point: tuple[float, float]) -> tuple[float, float]:
    x, y = point
    return (
        ((x - WORLD_LEFT) / WORLD_WIDTH) * (WIDTH - 1),
        ((WORLD_TOP - y) / WORLD_HEIGHT) * (HEIGHT - 1),
    )


def add_arc(
    path: list[tuple[float, float]],
    center: tuple[float, float],
    radius: float,
    start_degrees: float,
    end_degrees: float,
    segments: int = 64,
) -> None:
    for segment in range(1, segments + 1):
        amount = segment / segments
        angle = math.radians(
            start_degrees + ((end_degrees - start_degrees) * amount)
        )
        path.append(
            (
                center[0] + (math.cos(angle) * radius),
                center[1] + (math.sin(angle) * radius),
            )
        )


def occupied_runs(columns: int, occupied: set[tuple[int, int]]) -> list[tuple[int, int]]:
    used_columns = {column for column, _ in occupied}
    runs: list[tuple[int, int]] = []
    column = 0
    while column < columns:
        while column < columns and column not in used_columns:
            column += 1
        if column >= columns:
            break
        start = column
        while column + 1 < columns and column + 1 in used_columns:
            column += 1
        runs.append((start, column))
        column += 1
    return runs


def column_height(column: int, occupied: set[tuple[int, int]]) -> int:
    return max((row + 1 for cell_column, row in occupied if cell_column == column), default=0)


def recess_path(
    run_start: int,
    run_end: int,
    grid_center: float,
    spacing: float,
    occupied: set[tuple[int, int]],
) -> list[tuple[float, float]]:
    half_cell = (spacing * 0.5) + RECESS_CLEARANCE
    left = ((run_start - grid_center) * spacing) - half_cell
    right_edge = ((run_end - grid_center) * spacing) + half_cell
    radius = min(RECESS_LOWER_RADIUS, max(0.0, (right_edge - left) * 0.25))

    # The opening meets the sheet's stopping line through two deliberate
    # quarter-circle turns.  These are baked into the same sprite, so Unity
    # never has to approximate the corners with an extra line or overlay.
    path = [(left - radius, WORLD_BOTTOM)]
    add_arc(
        path,
        (left - radius, WORLD_BOTTOM + radius),
        radius,
        -90,
        0,
        segments=18,
    )
    current_top = ((column_height(run_start, occupied) - 0.5) * spacing) + RECESS_CLEARANCE
    path.append((left, current_top))
    for column in range(run_start, run_end + 1):
        current_center = (column - grid_center) * spacing
        right = current_center + half_cell
        next_top = current_top
        if column < run_end:
            next_top = (
                (column_height(column + 1, occupied) - 0.5) * spacing
            ) + RECESS_CLEARANCE

            # On an upward step, turn at the taller column's expanded left
            # edge. The shorter column's right edge falls inside the higher
            # tray and was the source of the visible sheet contact.
            if next_top > current_top:
                next_center = ((column + 1) - grid_center) * spacing
                right = next_center - half_cell

        path.append((right, current_top))
        if column >= run_end:
            path.append((right, WORLD_BOTTOM + radius))
            add_arc(
                path,
                (right + radius, WORLD_BOTTOM + radius),
                radius,
                180,
                270,
                segments=18,
            )
            continue
        if not math.isclose(next_top, current_top, abs_tol=1e-6):
            path.append((right, next_top))
            current_top = next_top
    return path


def make_mask(grid: dict) -> Image.Image:
    columns = int(grid["columns"])
    spacing = max(0.1, float(grid.get("cellSpacing", 1.0)))
    grid_center = (columns - 1) * 0.5
    occupied = {
        (int(box["column"]), int(box["row"]))
        for box in grid.get("boxes", [])
        if box is not None
    }
    if not occupied:
        raise ValueError("A premium sheet cannot be baked for an empty formation.")

    path: list[tuple[float, float]] = [
        (WORLD_RIGHT - OUTER_RADIUS, WORLD_TOP)
    ]
    add_arc(
        path,
        (WORLD_RIGHT - OUTER_RADIUS, WORLD_TOP - OUTER_RADIUS),
        OUTER_RADIUS,
        90,
        0,
    )
    path.append((WORLD_RIGHT, WORLD_BOTTOM + OUTER_LOWER_RADIUS))
    add_arc(
        path,
        (WORLD_RIGHT - OUTER_LOWER_RADIUS, WORLD_BOTTOM + OUTER_LOWER_RADIUS),
        OUTER_LOWER_RADIUS,
        0,
        -90,
        segments=18,
    )
    for run_start, run_end in reversed(occupied_runs(columns, occupied)):
        path.extend(
            reversed(recess_path(run_start, run_end, grid_center, spacing, occupied))
        )
    path.append((WORLD_LEFT + OUTER_LOWER_RADIUS, WORLD_BOTTOM))
    add_arc(
        path,
        (WORLD_LEFT + OUTER_LOWER_RADIUS, WORLD_BOTTOM + OUTER_LOWER_RADIUS),
        OUTER_LOWER_RADIUS,
        -90,
        -180,
        segments=18,
    )
    path.append((WORLD_LEFT, WORLD_TOP - OUTER_RADIUS))
    add_arc(
        path,
        (WORLD_LEFT + OUTER_RADIUS, WORLD_TOP - OUTER_RADIUS),
        OUTER_RADIUS,
        180,
        90,
    )

    mask = Image.new("L", (WIDTH, HEIGHT), 0)
    ImageDraw.Draw(mask).polygon([to_pixel(point) for point in path], fill=255)

    # Round the formation corners without moving its straight edges or reducing
    # the deliberate tray clearance.
    mask = mask.filter(ImageFilter.GaussianBlur(9.0))
    mask = mask.point(lambda value: 255 if value >= 128 else 0)
    return mask


def recolor_light_lavender(source: Image.Image) -> Image.Image:
    """Preserve the baked lighting while replacing only the sheet hue."""
    alpha = source.getchannel("A")
    luminance = ImageOps.grayscale(source.convert("RGB"))
    recolored = ImageOps.colorize(
        luminance,
        black=LAVENDER_SHADOW,
        mid=LAVENDER_MIDTONE,
        white=LAVENDER_HIGHLIGHT,
        blackpoint=0,
        midpoint=132,
        whitepoint=255,
    ).convert("RGBA")
    recolored.putalpha(alpha)
    return recolored


def material_surface() -> Image.Image:
    source = Image.open(SOURCE).convert("RGBA")
    # This broad crop is entirely inside the isolated approved sheet. It keeps
    # the real upper glow, quiet surface variation and restrained lower pattern
    # without importing any white-board or background pixels.
    sample = source.crop((150, 300, 703, 1120)).convert("RGB")
    registered = sample.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS).convert("RGBA")
    return recolor_light_lavender(registered)


def approved_edge_profile() -> list[tuple[int, int, int, int]]:
    """Read the real pearly/teal edge pixels from the isolated master."""
    source = Image.open(SOURCE).convert("RGBA")
    bounds = source.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("The registered sheet master has no opaque artwork.")
    center_x = (bounds[0] + bounds[2]) // 2
    profile: list[tuple[int, int, int, int]] = []
    for y in range(bounds[1], min(source.height, bounds[1] + 32)):
        pixel = source.getpixel((center_x, y))
        if pixel[3] >= 250:
            swatch = recolor_light_lavender(Image.new("RGBA", (1, 1), pixel))
            recolored = swatch.getpixel((0, 0))
            profile.append((recolored[0], recolored[1], recolored[2], 255))
        if len(profile) == 7:
            break
    if len(profile) != 7:
        raise ValueError("Could not sample the seven-pixel approved edge profile.")
    return profile


def paint_band(
    destination: Image.Image,
    alpha: Image.Image,
    color: tuple[int, int, int, int],
) -> None:
    layer = Image.new("RGBA", destination.size, color)
    if color[3] < 255:
        alpha = alpha.point(lambda value: round(value * color[3] / 255))
    layer.putalpha(alpha)
    destination.alpha_composite(layer)


def erode(mask: Image.Image, radius: int) -> Image.Image:
    """Erode while treating pixels beyond the canvas as transparent."""
    expanded = ImageOps.expand(mask, border=radius, fill=0)
    eroded = expanded.filter(ImageFilter.MinFilter((radius * 2) + 1))
    return eroded.crop((radius, radius, radius + mask.width, radius + mask.height))


def bake_sprite(
    grid: dict,
    surface: Image.Image,
    edge_profile: list[tuple[int, int, int, int]],
) -> tuple[str, Image.Image]:
    key = formation_key(grid)
    mask = make_mask(grid)

    result = surface.copy()
    result.putalpha(mask)

    # Bake the real seven-color edge cross-section sampled from the approved
    # master. Unity never reconstructs or layers this contour.
    previous = mask
    inset = 0
    for color in edge_profile:
        inset += 2
        current = erode(mask, inset)
        paint_band(result, ImageChops.subtract(previous, current), color)
        previous = current

    # One-pixel soft alpha coverage; there is no cast shadow outside the sheet.
    result.putalpha(mask.filter(ImageFilter.GaussianBlur(0.65)))
    return key, result


def load_formations() -> list[dict]:
    catalog = json.loads(LEVELS.read_text(encoding="utf-8"))
    formations = [level["topGrid"] for level in catalog["levels"]]
    formations.append(
        {
            "columns": 1,
            "rows": 2,
            "cellSpacing": 1.0,
            "boxes": [
                {"column": 0, "row": 0},
                {"column": 0, "row": 1},
            ],
        }
    )
    unique: dict[str, dict] = {}
    for formation in formations:
        unique[formation_key(formation)] = formation
    return list(unique.values())


def make_contact_sheet(outputs: list[tuple[str, Image.Image]]) -> None:
    preview_width = 640
    preview_height = round(preview_width * HEIGHT / WIDTH)
    margin = 28
    label_height = 42
    canvas = Image.new(
        "RGB",
        (
            (preview_width * 2) + (margin * 3),
            ((preview_height + label_height) * math.ceil(len(outputs) / 2))
            + (margin * (math.ceil(len(outputs) / 2) + 1)),
        ),
        (36, 91, 164),
    )
    draw = ImageDraw.Draw(canvas)
    for index, (key, image) in enumerate(outputs):
        column = index % 2
        row = index // 2
        x = margin + (column * (preview_width + margin))
        y = margin + (row * (preview_height + label_height + margin))
        preview = image.resize((preview_width, preview_height), Image.Resampling.LANCZOS)
        canvas.paste(preview.convert("RGB"), (x, y), preview.getchannel("A"))
        draw.text((x, y + preview_height + 10), key, fill=(242, 250, 255))
    CONTACT_SHEET.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(CONTACT_SHEET, optimize=True)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    surface = material_surface()
    edge_profile = approved_edge_profile()
    outputs: list[tuple[str, Image.Image]] = []
    for grid in load_formations():
        key, image = bake_sprite(grid, surface, edge_profile)
        path = OUTPUT / f"PremiumSheet_{key}.png"
        image.save(path, optimize=True)
        outputs.append((key, image))
        print(f"Baked {path}")
    make_contact_sheet(outputs)
    print(f"Baked {CONTACT_SHEET}")


if __name__ == "__main__":
    main()
