#!/usr/bin/env python3
"""Bake the approved premium HUD and aqua sheet textures for Unity."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageStat


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = ROOT / "Docs" / "ApprovedArtwork" / "HudAndSheet"
PROJECT_DIR = ROOT / "MarbleSort"
HUD_OUTPUT = (
    PROJECT_DIR
    / "Assets"
    / "MarbleSort"
    / "Resources"
    / "Presentation"
    / "UI"
    / "Approved"
    / "PremiumTopHudPlateAqua.png"
)
SETTINGS_GEAR_SOURCE = SOURCE_DIR / "UserSettingsGearSource.png"
SETTINGS_GEAR_PREPARED = SOURCE_DIR / "UserSettingsGearPrepared.png"
LEVEL_TITLE_SOURCE = SOURCE_DIR / "UserLevelTitleSource.png"
LEVEL_TITLE_PREPARED = SOURCE_DIR / "UserLevelTitlePrepared.png"
COIN_HUD_SOURCE = SOURCE_DIR / "UserCoinHudSource.png"
COIN_HUD_CLEAN = SOURCE_DIR / "UserCoinHudClean.png"
COIN_HUD_PREPARED = SOURCE_DIR / "UserCoinHudPrepared.png"
SURROUND_OUTPUT = (
    PROJECT_DIR
    / "Assets"
    / "MarbleSort"
    / "Resources"
    / "Presentation"
    / "Surround"
    / "Approved"
)


def prepare_settings_gear() -> Image.Image:
    """Crop the supplied gear without changing any of its painted pixels."""
    source = Image.open(SETTINGS_GEAR_SOURCE).convert("RGBA")
    alpha = source.getchannel("A")
    visible = alpha.point(lambda value: 255 if value >= 8 else 0)
    bounds = visible.getbbox()
    if bounds is None:
        raise ValueError("The supplied settings gear has no visible artwork.")

    padding = 20
    left = max(0, bounds[0] - padding)
    top = max(0, bounds[1] - padding)
    right = min(source.width, bounds[2] + padding)
    bottom = min(source.height, bounds[3] + padding)
    cropped = source.crop((left, top, right, bottom))

    # Remove only the effectively invisible full-canvas glow. Retain the real
    # antialiasing, contact shadow, blue outline, white highlights and yellow hub.
    cropped_alpha = cropped.getchannel("A").point(
        lambda value: 0 if value < 8 else value
    )
    cropped.putalpha(cropped_alpha)

    side = max(cropped.width, cropped.height)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(
        cropped,
        ((side - cropped.width) // 2, (side - cropped.height) // 2),
    )
    prepared = square.resize((256, 256), Image.Resampling.LANCZOS)
    SETTINGS_GEAR_PREPARED.parent.mkdir(parents=True, exist_ok=True)
    prepared.save(SETTINGS_GEAR_PREPARED, optimize=True)
    return prepared


def replace_settings_gear(plate: Image.Image, gear: Image.Image) -> Image.Image:
    """Replace only the gear painted inside the existing aqua button plate."""
    result = plate.copy()
    center_x = 93
    center_y = 113
    patch_radius = 41

    # Reconstruct the small area behind the original white gear from untouched
    # aqua pixels on the left and right of the same scanline. This keeps the
    # original button gradient visible through the new gear's tooth gaps.
    background = result.copy()
    background_pixels = background.load()
    source_pixels = result.load()
    for y in range(center_y - patch_radius, center_y + patch_radius + 1):
        left_color = ImageStat.Stat(
            result.crop((47, y, 55, y + 1)).convert("RGB")
        ).median
        right_color = ImageStat.Stat(
            result.crop((132, y, 140, y + 1)).convert("RGB")
        ).median
        for x in range(center_x - patch_radius, center_x + patch_radius + 1):
            amount = (x - (center_x - patch_radius)) / (patch_radius * 2)
            color = tuple(
                round(left_color[channel] +
                      ((right_color[channel] - left_color[channel]) * amount))
                for channel in range(3)
            )
            background_pixels[x, y] = (*color, source_pixels[x, y][3])

    patch_mask = Image.new("L", result.size, 0)
    ImageDraw.Draw(patch_mask).ellipse(
        (
            center_x - patch_radius,
            center_y - patch_radius,
            center_x + patch_radius,
            center_y + patch_radius,
        ),
        fill=255,
    )
    patch_mask = patch_mask.filter(ImageFilter.GaussianBlur(3.0))
    result = Image.composite(background, result, patch_mask)

    displayed_size = 80
    displayed = gear.resize(
        (displayed_size, displayed_size),
        Image.Resampling.LANCZOS,
    )
    result.alpha_composite(
        displayed,
        (center_x - (displayed_size // 2), center_y - (displayed_size // 2)),
    )
    return result


def remove_baked_level_text(source: Image.Image) -> Image.Image:
    """Restore the glossy aqua surface only where the supplied text is baked."""
    result = source.copy()
    predicted = source.copy()
    predicted_pixels = predicted.load()
    source_pixels = source.load()

    roi_left = 400
    roi_top = 390
    roi_right = 1070
    roi_bottom = 570
    for y in range(roi_top, roi_bottom):
        left_color = ImageStat.Stat(
            source.crop((350, y, 410, y + 1)).convert("RGB")
        ).median
        right_color = ImageStat.Stat(
            source.crop((1070, y, 1130, y + 1)).convert("RGB")
        ).median
        for x in range(roi_left, roi_right):
            amount = (x - roi_left) / max(1, roi_right - roi_left - 1)
            color = tuple(
                round(left_color[channel] +
                      ((right_color[channel] - left_color[channel]) * amount))
                for channel in range(3)
            )
            predicted_pixels[x, y] = (*color, source_pixels[x, y][3])

    # The supplied glyphs include a broad, low-opacity blue shadow. Replacing
    # the complete text band avoids leaving ghost rectangles around the letters.
    text_mask = Image.new("L", source.size, 0)
    ImageDraw.Draw(text_mask).rounded_rectangle(
        (roi_left, roi_top, roi_right, roi_bottom),
        radius=18,
        fill=255,
    )
    text_mask = text_mask.filter(ImageFilter.GaussianBlur(10.0))
    return Image.composite(predicted, result, text_mask)


def prepare_level_title() -> Image.Image:
    source = Image.open(LEVEL_TITLE_SOURCE).convert("RGBA")
    source = remove_baked_level_text(source)
    alpha = source.getchannel("A")
    visible = alpha.point(lambda value: 255 if value >= 8 else 0)
    bounds = visible.getbbox()
    if bounds is None:
        raise ValueError("The supplied level-title capsule has no visible artwork.")

    cropped = source.crop(bounds)
    cropped_alpha = cropped.getchannel("A").point(
        lambda value: 0 if value < 8 else value
    )
    cropped.putalpha(cropped_alpha)
    LEVEL_TITLE_PREPARED.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(LEVEL_TITLE_PREPARED, optimize=True)
    return cropped


def replace_level_title(plate: Image.Image, level_title: Image.Image) -> Image.Image:
    """Replace the complete legacy capsule in the dynamic-title slot."""
    result = plate.copy()

    # The approved HUD source still contains the previous centre capsule,
    # including a lower white rim and blue drop shadow. Alpha-compositing the
    # replacement over that artwork leaves the old rim visible beneath it.
    # Clear the isolated centre slot first so the supplied capsule is the only
    # title artwork in the final HUD texture. This rectangle stays safely
    # between the settings button and the coin capsule.
    result.paste((0, 0, 0, 0), (175, 38, 560, 205))

    displayed = level_title.resize((360, 130), Image.Resampling.LANCZOS)
    result.alpha_composite(displayed, (190, 51))
    return result


def remove_baked_coin_count(source: Image.Image) -> Image.Image:
    """Restore the aqua meter beneath the supplied baked zero."""
    clean = Image.open(COIN_HUD_CLEAN).convert("RGBA")
    if clean.size != source.size:
        raise ValueError("The cleaned coin HUD must preserve source dimensions.")

    # Retain the exact supplied alpha and every original painted pixel outside
    # the count footprint. The constrained clean source is used only to restore
    # the aqua material directly beneath the baked zero and its cast shadow.
    clean.putalpha(source.getchannel("A"))

    # Include the broad blue/gold antialiased shadow around the zero so no
    # baked-count pixels survive beneath the runtime coin balance.
    count_mask = Image.new("L", source.size, 0)
    ImageDraw.Draw(count_mask).rounded_rectangle(
        (650, 340, 1000, 620),
        radius=70,
        fill=255,
    )
    count_mask = count_mask.filter(ImageFilter.GaussianBlur(35.0))
    return Image.composite(clean, source, count_mask)


def prepare_coin_hud() -> Image.Image:
    """Prepare the supplied coin meter as text-free transparent artwork."""
    source = Image.open(COIN_HUD_SOURCE).convert("RGBA")
    source = remove_baked_coin_count(source)
    alpha = source.getchannel("A")
    visible = alpha.point(lambda value: 255 if value >= 8 else 0)
    bounds = visible.getbbox()
    if bounds is None:
        raise ValueError("The supplied coin HUD has no visible artwork.")

    cropped = source.crop(bounds)
    cropped_alpha = cropped.getchannel("A").point(
        lambda value: 0 if value < 8 else value
    )
    cropped.putalpha(cropped_alpha)
    COIN_HUD_PREPARED.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(COIN_HUD_PREPARED, optimize=True)
    return cropped


def replace_coin_hud(plate: Image.Image, coin_hud: Image.Image) -> Image.Image:
    """Replace the complete legacy coin capsule in its existing HUD slot."""
    result = plate.copy()
    result.paste((0, 0, 0, 0), (560, 38, 853, 205))
    displayed = coin_hud.resize((285, 95), Image.Resampling.LANCZOS)
    result.alpha_composite(displayed, (562, 68))
    return result


def bake_hud() -> None:
    approved = Image.open(
        SOURCE_DIR / "PremiumTopHudAlphaSource.png"
    ).convert("RGBA")
    cleaned_rgb = Image.open(
        SOURCE_DIR / "PremiumTopHudChromaSourceClean.png"
    ).convert("RGBA")
    if cleaned_rgb.size != approved.size:
        raise ValueError(
            "The cleaned HUD source must preserve the approved source dimensions."
        )

    # Preserve every original alpha edge and every settings/level pixel. Only
    # the opaque interior of the right-hand coin capsule receives the approved
    # clean color pass. Retaining a four-pixel original edge band prevents a
    # chroma fringe if the edited antialiasing differs by a fraction of a pixel.
    approved_alpha = approved.getchannel("A")
    coin_region = (1200, 0, approved.width, approved.height)
    coin_alpha = approved_alpha.crop(coin_region).filter(ImageFilter.MinFilter(9))
    clean_mask = Image.new("L", approved.size, 0)
    clean_mask.paste(coin_alpha, coin_region[:2])
    source_rgb = approved.convert("RGB")
    source_rgb.paste(cleaned_rgb.convert("RGB"), (0, 0), clean_mask)
    source = source_rgb.convert("RGBA")
    source.putalpha(approved_alpha)
    source.save(
        SOURCE_DIR / "PremiumTopHudAlphaSourceClean.png",
        optimize=True,
    )
    # The generated source uses the approved horizontal proportions. Scaling to
    # 853 px first preserves the exact left/centre/right alignment used by the
    # portrait UI reference plate; the unused transparent tail is then trimmed.
    scaled_height = 392
    scaled = source.resize((853, scaled_height), Image.Resampling.LANCZOS)
    plate = scaled.crop((0, 0, 853, 377))
    plate = replace_settings_gear(plate, prepare_settings_gear())
    plate = replace_level_title(plate, prepare_level_title())
    plate = replace_coin_hud(plate, prepare_coin_hud())
    HUD_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    plate.save(HUD_OUTPUT, optimize=True)


def bake_sheet_surface() -> None:
    preview = Image.open(
        SOURCE_DIR / "ApprovedPremiumSheetMaterialSource.png"
    ).convert("RGB")
    # The production source is the approved preview with its trays and recess
    # removed by a constrained edit. Sampling its full-height central sheet
    # region preserves the preview's real top-to-bottom glow, lower geometric
    # texture, and material falloff instead of stretching a flat middle patch.
    sample = preview.crop((220, 300, 633, 1200))
    surface = sample.resize((1254, 1254), Image.Resampling.LANCZOS)
    SURROUND_OUTPUT.mkdir(parents=True, exist_ok=True)
    surface.save(SURROUND_OUTPUT / "AquaSheetSurface.png", optimize=True)


def interpolate_color(stops: list[tuple[float, tuple[int, int, int, int]]], t: float):
    for index in range(1, len(stops)):
        left_t, left = stops[index - 1]
        right_t, right = stops[index]
        if t <= right_t:
            amount = (t - left_t) / max(0.0001, right_t - left_t)
            return tuple(
                round(left[channel] + ((right[channel] - left[channel]) * amount))
                for channel in range(4)
            )
    return stops[-1][1]


def bake_sheet_rim() -> None:
    width = 512
    height = 1024
    profile = Image.new("RGBA", (width, height))
    draw = ImageDraw.Draw(profile)
    # A narrow pearly/cyan highlight over a clean teal keyline. The texture is
    # constant along the contour direction, preventing visible joins.
    stops = [
        (0.00, (231, 255, 255, 255)),
        (0.08, (145, 226, 234, 255)),
        (0.24, (54, 131, 153, 255)),
        (0.52, (34, 82, 105, 255)),
        (0.76, (77, 170, 186, 255)),
        (0.91, (155, 232, 237, 255)),
        (1.00, (233, 255, 255, 255)),
    ]
    for y in range(height):
        color = interpolate_color(stops, y / (height - 1))
        draw.line((0, y, width, y), fill=color)
    profile.save(SURROUND_OUTPUT / "AquaSheetRimProfile.png", optimize=True)


if __name__ == "__main__":
    bake_hud()
    bake_sheet_surface()
    bake_sheet_rim()
    print(f"Baked {HUD_OUTPUT}")
    print(f"Baked {SURROUND_OUTPUT / 'AquaSheetSurface.png'}")
    print(f"Baked {SURROUND_OUTPUT / 'AquaSheetRimProfile.png'}")
