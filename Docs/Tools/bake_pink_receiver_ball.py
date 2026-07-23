#!/usr/bin/env python3
"""Derive the approved pink receiver/transit ball from the blue master."""

from __future__ import annotations

import colorsys
import sys
from pathlib import Path

from PIL import Image


TARGET_HUE = 0.925


def recolor(source: Path, destination: Path) -> None:
    image = Image.open(source).convert("RGBA")
    output = Image.new("RGBA", image.size)
    source_pixels = image.load()
    target_pixels = output.load()

    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = source_pixels[x, y]
            hue, saturation, value = colorsys.rgb_to_hsv(
                red / 255.0,
                green / 255.0,
                blue / 255.0,
            )
            if alpha and saturation >= 0.08 and value >= 0.055:
                red_f, green_f, blue_f = colorsys.hsv_to_rgb(
                    TARGET_HUE,
                    min(1.0, saturation * 1.08),
                    min(1.0, value * 1.06),
                )
                red = round(red_f * 255)
                green = round(green_f * 255)
                blue = round(blue_f * 255)
            target_pixels[x, y] = red, green, blue, alpha

    destination.parent.mkdir(parents=True, exist_ok=True)
    output.save(destination, optimize=True)


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit(
            "Usage: bake_pink_receiver_ball.py <blue-ball.png> <pink-ball.png>"
        )
    recolor(Path(sys.argv[1]), Path(sys.argv[2]))


if __name__ == "__main__":
    main()
