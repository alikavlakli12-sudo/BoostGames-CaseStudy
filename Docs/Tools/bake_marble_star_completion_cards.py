#!/usr/bin/env python3
"""Extract the approved completion cards from their dark preview canvases.

The generated source images intentionally keep a dark inspection background.
Runtime cards need a transparent exterior so the live game can remain visible
under the completion dimmer.  The bright closed outer rim is used as the
silhouette boundary; no colors inside the authored card are altered.
"""

from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = ROOT / "Docs" / "ApprovedArtwork" / "Completion"
OUTPUT_DIR = (
    ROOT
    / "MarbleSort"
    / "Assets"
    / "MarbleSort"
    / "Resources"
    / "Presentation"
    / "UI"
    / "Completion"
)
STAGES = (20, 40, 60, 80, 100)
MYSTERY_SOURCE = SOURCE_DIR / "MysteryBoxCompletion_Source.png"
MYSTERY_WITH_TRAY_SOURCE = SOURCE_DIR / "MysteryBoxCompletion_WithTray_Source.png"
MYSTERY_TARGET = OUTPUT_DIR / "MysteryBoxCompletion.png"


def is_outer_rim(pixel: tuple[int, int, int]) -> bool:
    red, green, blue = pixel
    # Approved cards can arrive on either the original navy inspection canvas
    # or the newer warm-grey one.  Only the pearlescent rim reaches this
    # balanced near-white range on both canvases.  The stricter threshold keeps
    # the grey studio background out of the alpha silhouette while continuing
    # to ignore blue card surfaces and gold celebration particles.
    return red >= 220 and green >= 210 and blue >= 190


def silhouette_mask(image: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    raw = Image.new("L", rgb.size, 0)
    mask_pixels = raw.load()

    rows: list[tuple[int, int] | None] = []
    center = width // 2
    for y in range(height):
        left_candidates = [x for x in range(center + 1) if is_outer_rim(pixels[x, y])]
        right_candidates = [
            x for x in range(center, width) if is_outer_rim(pixels[x, y])
        ]
        if not left_candidates or not right_candidates:
            rows.append(None)
            continue

        left = min(left_candidates)
        right = max(right_candidates)
        if right - left < 36:
            rows.append(None)
            continue
        rows.append((max(0, left - 2), min(width - 1, right + 2)))

    # Ignore isolated highlights outside the card by retaining only the single
    # long run of rows surrounding the canvas center.
    middle = height // 2
    top = middle
    while top > 0 and rows[top - 1] is not None:
        top -= 1
    bottom = middle
    while bottom + 1 < height and rows[bottom + 1] is not None:
        bottom += 1

    # Short anti-aliased rows at the crown/bottom can be separated by one dark
    # pixel. Extend to nearby valid rows without allowing background artifacts.
    for _ in range(8):
        if top > 0 and rows[top - 1] is not None:
            top -= 1
        if bottom + 1 < height and rows[bottom + 1] is not None:
            bottom += 1

    for y in range(top, bottom + 1):
        bounds = rows[y]
        if bounds is None:
            # Interpolate rare one-pixel misses from adjacent closed-rim rows.
            before = next((rows[i] for i in range(y - 1, top - 1, -1) if rows[i]), None)
            after = next((rows[i] for i in range(y + 1, bottom + 1) if rows[i]), None)
            if before is None or after is None:
                continue
            bounds = (
                int(round((before[0] + after[0]) * 0.5)),
                int(round((before[1] + after[1]) * 0.5)),
            )
        left, right = bounds
        for x in range(left, right + 1):
            mask_pixels[x, y] = 255

    return raw.filter(ImageFilter.GaussianBlur(radius=0.75))


def bake(stage: int) -> None:
    clean_source_v2 = SOURCE_DIR / f"MarbleStarCompletion{stage}_CleanSourceV2.png"
    clean_source = SOURCE_DIR / f"MarbleStarCompletion{stage}_CleanSource.png"
    source = (
        clean_source_v2
        if clean_source_v2.exists()
        else clean_source
        if clean_source.exists()
        else SOURCE_DIR / f"MarbleStarCompletion{stage}_Source.png"
    )
    target = OUTPUT_DIR / f"MarbleStarCompletion{stage}.png"
    image = Image.open(source).convert("RGBA")
    alpha = silhouette_mask(image)
    image.putalpha(alpha)

    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No card silhouette found in {source}")

    padding = 4
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    image.crop((left, top, right, bottom)).save(target, optimize=True)
    print(f"{stage}% -> {target.relative_to(ROOT)} ({right-left}x{bottom-top})")


def bake_mystery_box() -> None:
    """Preserve the approved Mystery Box card pixels and remove only its studio canvas."""
    source = (
        MYSTERY_WITH_TRAY_SOURCE
        if MYSTERY_WITH_TRAY_SOURCE.exists()
        else MYSTERY_SOURCE
    )
    image = Image.open(source).convert("RGBA")
    alpha = silhouette_mask(image)
    image.putalpha(alpha)

    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No card silhouette found in {source}")

    padding = 4
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    image.crop((left, top, right, bottom)).save(MYSTERY_TARGET, optimize=True)
    print(
        "Mystery Box -> "
        f"{MYSTERY_TARGET.relative_to(ROOT)} ({right-left}x{bottom-top})"
    )


def main() -> None:
    for stage in STAGES:
        bake(stage)
    if MYSTERY_SOURCE.exists():
        bake_mystery_box()


if __name__ == "__main__":
    main()
