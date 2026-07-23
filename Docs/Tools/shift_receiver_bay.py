#!/usr/bin/env python3
"""Lower only the baked receiver bay in the approved portrait environment."""

from pathlib import Path

import numpy as np
from PIL import Image


SOURCE = Path(
    "MarbleSort/Assets/MarbleSort/Art/Textures/PortraitEnvironmentSingleBoard.png"
)
OUTPUT = Path(
    "MarbleSort/Assets/MarbleSort/Art/Textures/PortraitEnvironmentReceiverBayLowered.png"
)

# The receiver bay's soft cast shadow begins at row 1250. Moving from this row
# keeps the entire bay—shadow, outer rim, and panel—together while leaving the
# upper board and chute artwork untouched.
BAY_START_Y = 1250
BAY_SHIFT_Y = 70


def reconstruct_columns(image: Image.Image, left: int, right: int) -> None:
    """Replace a contaminated vertical strip between clean neighbouring columns."""
    height = image.height
    left_column = image.crop((left - 1, 0, left, height))
    right_column = image.crop((right, 0, right + 1, height))
    span = right - left
    for column_index in range(span):
        t = (column_index + 1) / (span + 1)
        image.paste(
            Image.blend(left_column, right_column, t),
            (left + column_index, 0),
        )


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    width, height = source.size
    if (width, height) != (853, 1844):
        raise ValueError(f"Unexpected environment size: {(width, height)}")

    output = source.copy()

    # Reuse the real local blue texture, normalize each row to the final source
    # row, then fade that texture in and back out with a smooth periodic window.
    # Both joins therefore use the exact source pixels while the centre retains
    # natural texture instead of becoming a flat synthetic strip.
    local_texture = source.crop(
        (0, BAY_START_Y - BAY_SHIFT_Y, width, BAY_START_Y)
    )
    reconstruct_columns(local_texture, 280, 570)

    raw_base = np.asarray(source)[BAY_START_Y - 1].astype(np.float32)
    clean_base_image = source.crop(
        (0, BAY_START_Y - 1, width, BAY_START_Y)
    )
    reconstruct_columns(clean_base_image, 280, 570)
    clean_base = np.asarray(clean_base_image)[0].astype(np.float32)

    texture = np.asarray(local_texture).astype(np.float32)
    target_mean = clean_base[:, :3].mean(axis=0)
    row_means = texture[:, :, :3].mean(axis=1, keepdims=True)
    texture[:, :, :3] += target_mean.reshape(1, 1, 3) - row_means
    t = np.linspace(0.0, 1.0, BAY_SHIFT_Y, dtype=np.float32).reshape(
        BAY_SHIFT_Y, 1, 1
    )
    smooth_t = t * t * (3.0 - 2.0 * t)
    boundary = raw_base.reshape(1, width, 4) + smooth_t * (
        clean_base.reshape(1, width, 4) - raw_base.reshape(1, width, 4)
    )
    texture_weight = (np.sin(t * np.pi) ** 2) * 0.62
    continuation_pixels = boundary + texture_weight * (
        texture - clean_base.reshape(1, width, 4)
    )
    continuation_pixels[:, :, 3] = 255.0
    continuation = Image.fromarray(
        np.clip(continuation_pixels, 0.0, 255.0).astype(np.uint8),
        mode="RGBA",
    )
    output.paste(continuation, (0, BAY_START_Y))

    # Translate the receiver bay as one indivisible raster layer. The lower
    # 70 px are intentionally clipped by the canvas, removing the bottom rim.
    bay = source.crop((0, BAY_START_Y, width, height - BAY_SHIFT_Y))
    bay_head_height = min(36, bay.height)
    bay_head = bay.crop((0, 0, width, bay_head_height))
    reconstruct_columns(bay_head, 280, 570)
    bay.paste(bay_head, (0, 0))
    output.paste(bay, (0, BAY_START_Y + BAY_SHIFT_Y))

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    output.save(OUTPUT, optimize=True)
    print(f"Wrote {OUTPUT} ({width}x{height}, bay shift {BAY_SHIFT_Y}px).")


if __name__ == "__main__":
    main()
