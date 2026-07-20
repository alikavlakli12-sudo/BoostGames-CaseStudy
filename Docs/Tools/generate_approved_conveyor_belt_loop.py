#!/usr/bin/env python3
"""Build the runtime conveyor loop directly from the approved reference pixels."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Docs/Previews/ConveyorPremiumMotionApproved.png"
OUTPUT = (
    ROOT
    / "MarbleSort/Assets/MarbleSort/Resources/Presentation/Conveyor"
    / "ConveyorApprovedBeltLoop.png"
)

# Exact straight-pocket cells from the approved lower lane. Each cell includes the
# same unified belt lighting, inner rail occlusion, socket bevel, and outer rim edge.
DARK_CELL = (512, 402, 668, 537)
LIGHT_CELL = (847, 402, 1003, 537)
CELL_SIZE = (80, 128)
SLOT_COUNT = 24


def is_light_socket(index: int) -> bool:
    sequence_index = abs(index) % 12
    return sequence_index in (0, 1, 5, 6, 7, 11)


def main() -> None:
    reference = Image.open(SOURCE).convert("RGBA")
    dark_source = reference.crop(DARK_CELL)
    light_source = reference.crop(LIGHT_CELL)

    # Every cell shares one continuous background sample. Only the approved cavity
    # itself changes value, preventing tile seams or independently lit socket cards.
    light_mask = Image.new("L", dark_source.size, 0)
    ImageDraw.Draw(light_mask).rounded_rectangle(
        (38, 5, 124, 134),
        radius=39,
        fill=255,
    )
    light_mask = light_mask.filter(ImageFilter.GaussianBlur(2.0))
    light_source_on_common_belt = Image.composite(light_source, dark_source, light_mask)

    dark = dark_source.resize(CELL_SIZE, Image.Resampling.LANCZOS)
    light = light_source_on_common_belt.resize(CELL_SIZE, Image.Resampling.LANCZOS)

    belt = Image.new("RGBA", (CELL_SIZE[0] * SLOT_COUNT, CELL_SIZE[1]))
    for index in range(SLOT_COUNT):
        belt.alpha_composite(light if is_light_socket(index) else dark, (index * CELL_SIZE[0], 0))

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    belt.save(OUTPUT, optimize=True)
    print(f"Wrote {OUTPUT} ({belt.width}x{belt.height})")


if __name__ == "__main__":
    main()
