#!/usr/bin/env python3
"""Remove only the preview canvas around the approved conveyor-full loss card."""

from PIL import Image

from bake_marble_star_completion_cards import OUTPUT_DIR, ROOT, silhouette_mask


SOURCE = ROOT / "Docs" / "ApprovedArtwork" / "Completion" / "ConveyorFullLossCard_Source.png"
TARGET = OUTPUT_DIR / "ConveyorFullLossCard.png"


def main() -> None:
    image = Image.open(SOURCE).convert("RGBA")
    alpha = silhouette_mask(image)
    image.putalpha(alpha)
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No card silhouette found in {SOURCE}")

    padding = 4
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    image.crop((left, top, right, bottom)).save(TARGET, optimize=True)
    print(f"Loss card -> {TARGET.relative_to(ROOT)} ({right-left}x{bottom-top})")


if __name__ == "__main__":
    main()
