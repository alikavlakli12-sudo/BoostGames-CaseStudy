#!/usr/bin/env python3
"""Place the approved mystery-tray render into the approved milestone card.

Both approved sources remain untouched. The tray's pale inspection canvas is
removed from its closed silhouette, then the resulting cutout is scaled and
centered in the authored showcase recess above the Mystery Box ribbon.
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = ROOT / "Docs" / "ApprovedArtwork" / "Completion"
CARD_SOURCE = SOURCE_DIR / "MysteryBoxCompletion_Source.png"
TRAY_SOURCE = SOURCE_DIR / "MysteryTrayPreview_Source.png"
OUTPUT_SOURCE = SOURCE_DIR / "MysteryBoxCompletion_WithTray_Source.png"

TARGET_WIDTH = 440
TARGET_TOP = 390


def extract_tray(image: Image.Image) -> Image.Image:
    rgb = np.asarray(image.convert("RGB"))
    maximum = rgb.max(axis=2)
    minimum = rgb.min(axis=2)

    # The approved object is neutral grey (plus a dark/white question mark),
    # while its inspection canvas is a pale blue gradient. Row filling keeps
    # the exact continuous molded-box silhouette, including its own shadow.
    candidates = ((maximum - minimum) < 20) | (maximum < 125)
    candidates[:120, :] = False
    candidates[1120:, :] = False
    candidates[:, :80] = False
    candidates[:, 1180:] = False

    mask = Image.new("L", image.size, 0)
    mask_pixels = mask.load()
    valid_rows: list[tuple[int, int, int]] = []
    for y in range(image.height):
        xs = np.flatnonzero(candidates[y])
        if xs.size < 20:
            continue
        left = max(0, int(xs.min()) - 2)
        right = min(image.width - 1, int(xs.max()) + 2)
        valid_rows.append((y, left, right))
        for x in range(left, right + 1):
            mask_pixels[x, y] = 255

    if not valid_rows:
        raise RuntimeError(f"No mystery-tray silhouette found in {TRAY_SOURCE}")

    mask = mask.filter(ImageFilter.GaussianBlur(radius=0.85))
    rgba = image.convert("RGBA")
    rgba.putalpha(mask)

    bbox = mask.getbbox()
    if bbox is None:
        raise RuntimeError(f"No mystery-tray alpha bounds found in {TRAY_SOURCE}")

    padding = 5
    crop = (
        max(0, bbox[0] - padding),
        max(0, bbox[1] - padding),
        min(image.width, bbox[2] + padding),
        min(image.height, bbox[3] + padding),
    )
    return rgba.crop(crop)


def main() -> None:
    card = Image.open(CARD_SOURCE).convert("RGBA")
    tray = extract_tray(Image.open(TRAY_SOURCE))

    target_height = round(tray.height * (TARGET_WIDTH / tray.width))
    tray = tray.resize((TARGET_WIDTH, target_height), Image.Resampling.LANCZOS)
    target_left = round((card.width - tray.width) * 0.5)
    card.alpha_composite(tray, (target_left, TARGET_TOP))
    card.convert("RGB").save(OUTPUT_SOURCE, optimize=True)
    print(
        f"Mystery tray {tray.width}x{tray.height} at "
        f"({target_left}, {TARGET_TOP}) -> {OUTPUT_SOURCE.relative_to(ROOT)}"
    )


if __name__ == "__main__":
    main()
