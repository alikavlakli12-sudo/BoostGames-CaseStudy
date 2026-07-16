# Asset Provenance

## Portrait gameplay background

- Project asset: `MarbleSort/Assets/MarbleSort/Art/Textures/PortraitBackground.png`
- Created: 2026-07-16
- Creation mode: built-in image generation, `stylized-concept`
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logos, characters, or interface elements were used.

Final prompt:

> Use case: stylized-concept. Asset type: portrait mobile puzzle-game background texture for
> Unity. Primary request: Create an original polished casual mobile-game backdrop for a colorful
> marble sorting puzzle. Scene/backdrop: a clean vertical periwinkle-blue field with a very subtle
> lighter center glow, soft broad layered gradients, faint oversized rounded abstract shapes near
> the outer edges, and a gentle vignette that keeps the center readable. Style/medium: premium 2D
> casual mobile game illustration, smooth and minimal, similar in finish to a modern polished
> puzzle game but not copying any branded UI. Composition/framing: portrait-oriented visual logic
> with a completely unobstructed central gameplay area; edge decoration only; must also crop
> cleanly from a square source. Lighting/mood: bright, friendly, calm, soft studio-like depth.
> Color palette: periwinkle, cornflower blue, pale lavender-blue, tiny hints of cool white.
> Constraints: background only; no text, no logo, no icons, no characters, no marbles, no boxes,
> no UI panels, no watermark; no sharp details; low visual noise; suitable behind high-contrast
> game pieces.

## Receiver trays and balls

- Project assets: `MarbleSort/Assets/MarbleSort/Resources/Presentation/Receivers/Receiver*.png`
- Created: 2026-07-16
- Creation mode: built-in image generation followed by deterministic chroma-key removal
- Ownership/licensing note: generated specifically for this case-study prototype from the approved
  receiver preview; no third-party game artwork, logos, characters, or interface elements were
  used.
- Runtime treatment: four transparent empty 1×3 molded receiver bases and four transparent glossy
  ball layers. Unity crops their transparent generation canvas through normalized sprite metadata,
  so the approved lighting and molded depth are preserved while the three fill slots remain
  independent.

Prompt brief:

> Create a single premium casual-mobile-game receiver asset on a solid magenta chroma-key
> background. The receiver is a horizontal 1×3 molded plastic tray facing the camera with a slight
> top-down near-front view, broad clean face, rounded glossy highlight rim, three deep circular
> color-matched wells, visible darker lower side, soft studio lighting from the upper left, no
> text, no logo, no extra objects, and no external cast shadow. Produce matching green, blue,
> orange, and yellow variants. Create each matching glossy ball separately with the same upper-left
> oval highlight, rounded shading, centered framing, and no cast shadow.

## Exposed 3×3 trays

- Project assets: `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/TopTray_*.png`
- Created: 2026-07-17
- Creation mode: built-in image editing from the approved 3×3 preview followed by deterministic
  chroma-key removal
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  artwork was added.
- Runtime treatment: four transparent empty molded tray bases. The nine ball positions reuse the
  receiver ball sprites as independent layers, preserving the original selection, one-by-one
  release, and physics behavior.

Prompt brief:

> Preserve the approved square 3×3 tray's exact geometry, camera angle, rounded molded rim,
> visible darker lower side, studio lighting, and premium hyper-realistic casual-game finish.
> Remove all nine balls to reveal nine deep color-matched circular wells. Center one tray on a
> perfectly flat #FF00FF chroma-key background with even padding and no external cast shadow,
> floor, text, logo, watermark, or extra objects. Produce matching green, blue, orange, and yellow
> versions while changing only the tray color.
