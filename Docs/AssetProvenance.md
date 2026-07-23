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

## Superseded adaptive molded tray-formation sheet

- Preserved design sources:
  - `Docs/ApprovedArtwork/HudAndSheet/SupersededSheetIterations/MoldedSheetSurface.png`
  - `Docs/ApprovedArtwork/HudAndSheet/SupersededSheetIterations/MoldedSheetSurfaceV2.png`
  - `Docs/ApprovedArtwork/HudAndSheet/SupersededSheetIterations/MoldedSheetRimProfile.png`
- Created: 2026-07-18
- Creation mode: built-in image generation based on the user-approved molded-sheet preview
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  artwork, logos, characters, or interface elements were used.
- Historical prototype treatment: Unity mapped one continuous steel-blue material across
  procedural surface spans, placed a darker offset sidewall underneath, then wrapped one seamless
  generated highlight-to-shadow profile around a continuous closed contour. Each level built an
  immutable recess from its original tray formation with a fixed narrow clearance, leaving every
  tray cell as a true opening; removing a tray during play never changed or refilled the sheet.
- Final delivery treatment: these iterations are retained outside Unity `Resources` for provenance
  only. The approved Aqua sheet documented below is the sole runtime sheet material.

Prompt brief:

> Create a seamless premium molded steel-blue plastic surface for a polished casual mobile puzzle
> game. Use a smooth satin finish, a subtle cool vertical lighting gradient, a restrained soft
> center glow, and extremely fine material grain. Keep the surface free of borders, seams, holes,
> objects, text, logos, cast shadows, strong hotspots, or directional shapes so it can be mapped
> continuously across an adaptive Unity mesh and combined with separate layered bevel geometry.

> Create a perfectly straight, seamless horizontal close-up of the approved sheet's thick molded
> steel-blue plastic edge. Across the vertical direction, render a thin cool-white highlight, pale
> blue rounded bevel, broad satin blue-gray face, darker lower sidewall, and deep-blue ambient
> occlusion band. Keep it orthographic, edge-to-edge, free of perspective, corners, end caps, text,
> objects, and hard black outlines. The generated profile is horizontally normalized after export
> so its closed Unity contour has no texture seam.

## Superseded single-board environment and periwinkle sheet

- Preserved design sources:
  - `MarbleSort/Assets/MarbleSort/Art/Textures/PortraitEnvironmentSingleBoard.png`
  - `Docs/ApprovedArtwork/HudAndSheet/SupersededSheetIterations/PeriwinkleSheetSurface.png`
  - `Docs/ApprovedArtwork/HudAndSheet/SupersededSheetIterations/PeriwinkleSheetRimProfile.png`
- Created: 2026-07-18
- Creation mode: built-in image editing and generation from the user-approved single-board layout
  preview, followed by deterministic horizontal normalization of the rim profile
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logos, characters, or interface elements were used.
- Historical prototype treatment: the environment plate removed the deeper duplicate upper-board
  chassis while preserving the original 853×1844 camera mapping and lower receiver bay. Unity
  drew the periwinkle sheet as one UV-continuous inset surface that reached close to the board's
  upper inner rim, then opened completely around the level's original tray formation. The entire
  area beneath the trays—including the lower slopes and chute—remained the original icy-blue
  backboard. The recess preserved a fixed visible air gap around every tray. The sheet had no
  separate cast-shadow layer; its molded rim alone finished exactly on the tray-bottom line, so
  it read behind the formation rather than projecting in front of it. The board and sheet
  contours shared a centered symmetry axis; tray removal never rebuilt or refilled the immutable
  opening.
- Final delivery treatment: the periwinkle iteration is retained outside Unity `Resources` for
  provenance only. The lowered receiver-bay environment and approved Aqua sheet documented below
  are the runtime assets.

## Artwork-matched invisible backboard collision contour

- Runtime project code:
  - `MarbleSort/Assets/MarbleSort/Runtime/Gameplay/Conveyor/ChuteBoundaryRig.cs`
- Created: 2026-07-18
- Creation mode: deterministic invisible Unity collision; no external artwork or added visible
  geometry
- Runtime treatment: the approved illustrated backboard remains the only visible pinball border.
  Its inner left contact edge was measured directly on the 853×1844 source plate, mirrored about
  the board center, and normalized to the game's 9.3-unit portrait board width. Twenty-eight
  thin invisible solid segments now follow the exact vertical walls, rounded lower turns, funnel
  slopes, curved chute transitions, and both visible chute walls. Each collider's inward face—not its center—is aligned to the
  measured image line, so a ball's visible edge meets the border instead of stopping in empty
  space or overlapping the artwork. The former approximate entrance guides are repurposed as the
  final two measured chute-wall segments; only one hidden admission gate remains as a high-speed
  fallback for one-ball flow. No runtime renderer is created for this collision contour.

Prompt brief:

> Edit the clean portrait environment into one enlarged icy-blue molded board with narrow equal
> screen margins, rounded upper corners, mirror-symmetric lower slopes, and one centered chute.
> Remove the deepest navy backing board completely while preserving the blue background, empty
> basin, lower receiver bay, exact 853×1844 composition, and all existing gameplay coordinates.
> Include no sheet, gameplay pieces, conveyor, UI, text, logo, or watermark in the environment
> plate.

> Create a seamless satin periwinkle/slate-lavender sheet surface matching the approved preview,
> with a soft center glow and subtle plastic grain. Create its orthographic edge profile with an
> icy highlight, pale lavender bevel, periwinkle face, darker blue-violet sidewall, and soft navy
> occlusion band. Include no shapes, holes, objects, text, hard outlines, or external shadows.

## Approved tightly packed 3x3 trays

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/TopTray_*_00.png`
    through `TopTray_*_09.png`
- Approved production sources:
  - `Docs/ApprovedArtwork/TopTray/ApprovedTopTrayFilledGreen.png`
  - `Docs/ApprovedArtwork/TopTray/ApprovedTopTrayEmptyGreen.png`
- Deterministic production tool:
  - `Docs/Tools/bake_approved_top_trays.swift`
- Created: 2026-07-20
- Creation mode: built-in image generation produced the approved filled green tray and its matching
  empty interior. The deterministic Swift baker removes the studio background, preserves the molded
  rim, deep front wall, highlights, and contact depth, then applies a uniform anti-aliased premium
  navy silhouette outline before deriving matching green, blue, orange, and yellow color sets. It
  bakes ten transparent occupancy states per color. Trilinear mip filtering keeps that outline clean
  at the smaller gameplay presentation size.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, or interface asset was added.
- Runtime treatment: each exposed tray uses exactly one SpriteRenderer. The full nine-ball frame is
  the user-approved preview with no visible interior between the tightly packed balls. As the existing
  mechanics release the bottom, middle, and top rows, the renderer swaps to the corresponding baked
  occupancy frame. The nine existing marker transforms remain the sole release anchors but their old
  ball renderers are disabled, preventing duplicate artwork while preserving selection, release,
  collision, pooling, fixed-grid reveal, and disappearance behavior.

## Approved standalone hidden top trays

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/Hidden/HiddenTopTray_Green.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/Hidden/HiddenTopTray_Blue.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/Hidden/HiddenTopTray_Orange.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/Hidden/HiddenTopTray_Yellow.png`
- Approved production source:
  - `Docs/ApprovedArtwork/HiddenTopTrays/ApprovedHiddenTopTrayReference.png`
- Deterministic production tool:
  - `Docs/Tools/bake_approved_hidden_top_trays.swift`
- Created: 2026-07-21
- Creation mode: the user-approved standalone blue hidden-tray render was preserved as the single
  production master. The deterministic Swift baker isolates that complete tray once, retains its
  exact gloss, bevel, outline, deep front wall, perspective, and contact depth, then derives the
  green, blue, orange, and yellow treatments from the same silhouette. No runtime color variant is
  cropped from a larger composition, and all four variants have pixel-identical geometry.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, interface asset, or typeface was added.
- Runtime treatment: a hidden tray is one baked SpriteRenderer and is never reconstructed from
  Unity mesh layers. Front-row trays start exposed. A hidden tray reveals only after a real tray
  directly in front of it or directly beside it has been cleared; diagonal cells and pre-existing
  gaps do not trigger reveals. Revealing swaps to the existing nine-ball tray in the exact same
  authored cell. No tray ever falls, compacts, or changes its grid coordinate.

## Approved premium layered conveyor

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Conveyor/ConveyorAnimationAtlas.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Conveyor/ConveyorAnimationAtlasShader.shader`
- Approved visual reference:
  - `Docs/Previews/ConveyorPremiumMotionApproved.png`
- Runtime atlas smoke evidence:
  - `Docs/Previews/ConveyorAtlasRuntimeSmoke.png`
- Preserved production sources (excluded from the Unity player):
  - `Docs/ApprovedArtwork/Conveyor/ConveyorApprovedReference.png`
  - `Docs/ApprovedArtwork/Conveyor/ConveyorApprovedCleanPlate.png`
  - `Docs/ApprovedArtwork/Conveyor/ConveyorApprovedBeltLoop.png`
  - `Docs/ApprovedArtwork/Conveyor/AnimationFrames/ConveyorFrame_*.png`
- Production socket sources:
  - `MarbleSort/Assets/MarbleSort/Art/Source/ConveyorSockets/ConveyorSocket_Dark.png`
  - `MarbleSort/Assets/MarbleSort/Art/Source/ConveyorSockets/ConveyorSocket_Light.png`
  - `Docs/Previews/ConveyorSocketPairChroma.png`
  - `Docs/Previews/ConveyorSocketPairAlpha.png`
- Deterministic production tool:
  - `Docs/Tools/generate_approved_conveyor_belt_loop.py`
  - `Docs/Tools/bake_clean_conveyor_frames.swift`
  - `MarbleSort/Assets/MarbleSort/Editor/ConveyorSceneVisualCleanup.cs`
- Created: 2026-07-19; layered cavity rebuild completed 2026-07-20
- Creation mode: the 2172×724 user-approved render is preserved as a production source outside
  Unity `Resources`. A built-in image edit removes only the 24 static cavities to produce a
  socket-free clean plate.
  A built-in image-generation pass derives one dark and one pale cavity-only source on chroma green
  from the approved material reference. The standard imagegen chroma-key helper converts the pair
  to alpha, after which the deterministic Swift baker draws a clean lane, the 24 moving transparent
  cavities, and finally the approved chassis/center-rail foreground. No cavity contains surrounding
  belt pixels, an external shadow, a rectangular backing card, or a duplicated conveyor layer.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logos, characters, or interface elements were added.
- Runtime treatment: the production tool bakes 192 complete 797×207 transparent conveyor frames
  over one full loop. Frame zero uses all 24 measured approved socket centers and the exact
  twelve-dark/twelve-pale sequence, grouped continuously as three dark then three pale. The two
  formerly isolated dark cavities at logical indices 11 and 23 are now pale. Straight cavities
  retain the approved compact proportions;
  diagonal turn poses receive restrained perspective compression and remain beneath the rim.
  Every frame contains the full approved chassis, connected upper/lower outline and depth, raised
  center rail, and only the correctly positioned moving cavity sprites. The baker packs them into
  one 8×24, 6440×5160 atlas with a four-pixel duplicated-edge gutter around every frame. Runtime
  uses one SpriteRenderer, one shared material, one texture, and one reused MaterialPropertyBlock;
  the shader selects a cell without swapping sprites or allocating. Frame selection is driven by
  the existing physical conveyor phase, producing approximately 53 visual frames per second at the
  configured gameplay speed. The atlas is non-readable, has no mipmaps, uses ASTC 5×5 on iOS, and
  lets Android Build Settings select ASTC or the ETC2 compatibility fallback. The 192 source PNGs,
  approved reference, clean plate, and old belt loop are outside
  `Resources` and therefore excluded from the player. The saved scene contains zero conveyor
  MeshRenderers and zero MeshFilters beneath the visual; only 24 empty Transform anchors remain for mechanics.
  This makes the pockets bend and rotate continuously at both turns without ghost cavity rims,
  outline overlap, or a second stationary belt. Twelve dark and twelve light pockets preserve the
  corrected continuous three-dark/three-light shade sequence while retaining the
  existing counterclockwise occupancy loop, speed, admission, receiver matching, ball placement,
  and one-ball chute principles.

Prompt brief:

> Remove only the 24 rounded socket cavities from the approved conveyor. Fill them with a
> continuous lavender-gray track surface matching the surrounding material and lighting. Keep the
> outer white/lavender chassis, every outline and bevel, rounded silhouette, raised white center
> capsule, canvas, placement, colors, shadows, and camera angle unchanged. Add no sockets, ghost
> rims, duplicated layers, corrupted borders, text, or watermark.

> Preserve the exact approved low-profile lavender-gray conveyor chassis, bevels, highlights,
> camera angle, proportions, recessed moving belt, raised pearlescent white center bridge, and
> exact 24-socket count. Keep the outer chassis, belt, center rail, and pocket loop visually
> separate, with every turning socket safely contained inside the outline.

> Create matching dark and light compact vertical rounded-rectangle sockets that unmistakably
> read as cavities cut into lavender-gray plastic. Use dark ambient occlusion only inside the top
> and left edge, a restrained highlight only on the inner bottom and right bevel, and no external
> cast shadow, contact shadow, pedestal, button, raised block, surrounding panel, or perspective.

Final built-in socket-source prompt:

> Create exactly two isolated vertical rounded-rectangle recessed cavities matching the approved
> conveyor sockets: one dark charcoal-lavender cavity and one pale lavender cavity. Place them on
> a perfectly flat solid #00FF00 chroma-key background with generous separation. Match the approved
> compact proportions, inner ambient occlusion, fine inner bevel, and restrained lower-right rim
> highlight. Each socket must be a recessed hole, never a raised button, block, tile, tray, or
> panel. Include no surrounding conveyor surface, rectangular backing card, chassis, center rail,
> external shadow, text, logo, or watermark.

## Approved tightly packed receiver boxes

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/ReceiversV3/Receiver_*_Open_00.png`
    through `Receiver_*_Open_03.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/ReceiversV3/Receiver_*_Closed.png`
- Approved production sources:
  - `Docs/ApprovedArtwork/Receivers/ApprovedReceiverBlueOpen00.png`
  - `Docs/ApprovedArtwork/Receivers/ApprovedReceiverBlueOpen01.png`
  - `Docs/ApprovedArtwork/Receivers/ApprovedReceiverBlueOpen02.png`
  - `Docs/ApprovedArtwork/Receivers/ApprovedReceiverBlueOpen03.png`
  - `Docs/ApprovedArtwork/Receivers/ApprovedReceiverBlueClosed.png`
- Deterministic production tool:
  - `Docs/Tools/bake_approved_receivers.swift`
- Created: 2026-07-20
- Creation mode: built-in image generation produced the approved thick blue open and closed box
  states. The deterministic Swift baker isolates the receiver from its studio background, removes
  detached pixels and external backing, normalizes every state to one transparent canvas, applies
  one uniform anti-aliased premium navy silhouette outline, and derives matching green, blue,
  orange, and yellow sets while preserving the deep front wall, molded rim, packed balls, empty
  wells, highlights, and material depth. Trilinear mip filtering keeps the outline stable at the
  smaller gameplay presentation size.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, or interface asset was added.
- Runtime treatment: an open receiver uses exactly one SpriteRenderer and swaps complete baked
  occupancy frames after each accepted marble. A waiting or completed receiver uses one complete
  baked closed-box sprite over the open state; the existing lid timing fades that whole approved
  state in or out without a hinge or connector. Three hidden marker transforms remain only as
  transfer destinations, so queue progression, color matching, one-at-a-time collection, lid
  timing, completion, exit, and lane advancement mechanics are unchanged. The legacy separate
  body, ball, and cap artwork is not rendered.

## Approved receiver completion star

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Effects/ReceiverCompletionStar.png`
- Created: 2026-07-20
- Creation mode: built-in image generation produced the user-approved pearlescent white, softly
  molded five-point star. A background-only image edit replaced the preview backdrop with a flat
  chroma key, and the standard imagegen chroma-key helper removed that key without rebuilding or
  recoloring the star.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, or interface asset was added.
- Runtime treatment: the exact transparent star sprite is reused four times at four explicitly
  different sizes. The receiver starts the short outward pop on the first frame after its lid is
  fully closed, while its existing exit and queue-advance mechanics continue unchanged. The old
  generic receiver-completion sphere burst is suppressed so the visible completion cue is exactly
  the approved four-star effect.

## Approved aqua HUD and adaptive sheet

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Approved/PremiumTopHudPlateAqua.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Surround/Approved/AquaSheetSurface.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Surround/Approved/AquaSheetRimProfile.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Surround/Approved/Baked/PremiumSheet_*.png`
- Approved production sources:
  - `Docs/ApprovedArtwork/HudAndSheet/ApprovedPremiumHudSheetPreview.png`
  - `Docs/ApprovedArtwork/HudAndSheet/ApprovedPremiumSheetMaterialSource.png`
  - `Docs/ApprovedArtwork/HudAndSheet/RegisteredSheetMasterChroma.png`
  - `Docs/ApprovedArtwork/HudAndSheet/RegisteredSheetMaster.png`
  - `Docs/ApprovedArtwork/HudAndSheet/PremiumTopHudChromaSource.png`
  - `Docs/ApprovedArtwork/HudAndSheet/PremiumTopHudAlphaSource.png`
  - `Docs/ApprovedArtwork/HudAndSheet/PremiumTopHudChromaSourceClean.png`
  - `Docs/ApprovedArtwork/HudAndSheet/PremiumTopHudAlphaSourceClean.png`
- Deterministic production tool:
  - `Docs/Tools/bake_approved_hud_sheet.py`
  - `Docs/Tools/bake_premium_sheet_sprites.py`
- Created: 2026-07-21
- Creation mode: built-in image editing produced the final approved aqua, white, and gold HUD and
  matching sea-glass sheet preview. A second built-in pass isolated the three HUD shells on a flat
  magenta chroma key with the dynamic level and coin text areas intentionally blank. The standard
  imagegen chroma-key helper removed that key, and the deterministic baker normalized the plate to
  the game's 853×377 HUD coordinate system. The same baker samples the uninterrupted center of the
  approved sheet preview and creates its seamless aqua surface and matching molded rim profile.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, interface asset, or typeface was added.
- Runtime treatment: the settings gear and coin-plus shell remain baked in the approved plate,
  while Unity draws the current level name and coin balance dynamically. The level capsule is a
  non-interactive label; only the settings and plus hit areas are buttons. Coins initialize at
  zero and expose explicit setter/addition methods for the future reward milestone. The sheet
  keeps the existing immutable, per-level formation contour: it is built once from the original
  tray layout, never refills when a tray is removed, leaves the board exposed beneath every tray,
  and uses a fixed visible clearance with no colliders or input interception.
- Sheet material correction: a constrained built-in image edit isolated the complete approved
  sea-glass sheet on a flat magenta key while preserving its lighting, lower pattern, pearly edge,
  and large rounded silhouette. The standard chroma-key helper produced the transparent registered
  master. The final production baker samples that master, uses white-board aperture measurements
  for the sheet width and corner radius, cuts only the exact initial formation opening, and carries
  the master sheet's real seven-pixel edge profile into one 1536×1184 sprite for every production
  layout. Runtime places exactly one
  finished sprite and never reconstructs, tiles, stretches, lights, or redraws the sheet. This also
  removes the former multi-mesh seams and three LineRenderer contours while retaining the immutable
  per-level opening, fixed tray clearance, exposed lower backboard, and collider-free input behavior.
- Coin-HUD cleanup: a constrained built-in image edit removed the warm cloudy contamination from
  only the coin capsule. The baker preserves the original alpha silhouette and every original
  settings/level pixel, replacing only the right capsule region before scaling. Runtime places the
  complete HUD group at 88% of its former size and keeps its first visible pixel 14 px below the
  device safe-area top, so notched phones and the Dynamic Island cannot overlap it.

Prompt brief:

> Reproduce only the approved three premium top HUD controls: a polished aqua settings button,
> blank aqua level capsule with pearlescent white bevel and thin gold accent, and an aqua coin
> capsule with a dimensional gold coin and gold plus button. Use a perfectly flat magenta chroma
> background, leave the level and coin-number areas blank for runtime text, include no locked-level
> cards, purple materials, dirt, scratches, extra text, extra objects, or watermark, and preserve
> the approved clean molded casual-game finish and spacing.

## Approved cleared-tray footprint

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopGrid/ClearedTraySpot.png`
- Approved production sources:
  - `Docs/ApprovedArtwork/TopGrid/ClearedTraySpotChroma.png`
  - `Docs/ApprovedArtwork/TopGrid/ClearedTraySpotChromaGreen.png`
- Created: 2026-07-21
- Creation mode: built-in image generation created the user-approved comparison preview with one
  active 3×3 tray and one smaller pale footprint. A constrained built-in background-extraction
  edit isolated only that footprint on a flat chroma-green background. The standard imagegen
  chroma-key helper removed the key with a contracted, despilled alpha edge so the production
  sprite has no colored halo.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, interface asset, or typeface was added.
- Runtime treatment: the level creates one immutable spot for every unique cell in the original
  tray formation. Spots are approximately 82% of an exposed tray's presented size, contain no
  colliders, stay behind trays, never move during fixed-grid reveals, stay hidden while their tray
  occupies the cell, and become permanently visible after that tray disappears. Rebuilding or
  advancing the level replaces the complete spot layer from the
  new level's original formation without changing tray release, sheet, chute, conveyor, receiver,
  or input mechanics.

Prompt brief:

> Isolate only the approved pale icy-blue cleared-tray footprint. Preserve its smaller rounded
> silhouette, satin molded finish, shallow recessed center, delicate inner bevel, and soft
> upper-left lighting. Place it on one flat chroma-green background with no occupied tray, balls,
> holes, icons, text, cast shadow, extra objects, color spill, or watermark.

## Approved flat receiver queue lane

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/ReceiverLanes/ReceiverQueueLane.png`
- Approved production source:
  - `Docs/ApprovedArtwork/Receivers/ReceiverQueueLaneChroma.png`
- Created: 2026-07-21
- Creation mode: built-in image generation created the user-approved flat icy-blue lane preview.
  A constrained built-in background-extraction edit preserved the complete flat lane and its three
  arrows while replacing only the surrounding preview background with a chroma-green key. The
  standard imagegen helper removed that key with a contracted, despilled alpha edge.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, interface asset, or typeface was added.
- Runtime treatment: every receiver queue owns one persistent, collider-free lane sprite behind
  its boxes. The lane is rendered from one approved image, never reconstructed from Unity shapes,
  and is scaled uniformly to the receiver's visible artwork width—never wider. Receiver boxes,
  lids, balls, completion stars, and queue advancement remain in front of it. Rebuilding a queue
  preserves its lane; changing levels replaces the four lanes with the new level's queue roots.

Prompt brief:

> Preserve the approved flat, borderless pale icy-blue receiver lane and exactly three evenly
> spaced upward blue-lavender chevrons. Replace only the surrounding background with one flat
> chroma-green key. Keep the straight-on proportions, gentle rounded ends, soft premium shading,
> and absence of thickness. Add no rails, rim, outline, bevel, shadow, boxes, balls, text, extra
> objects, green spill, or watermark.

## Lowered receiver-bay composition

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Art/Textures/PortraitEnvironmentReceiverBayLowered.png`
- Deterministic production tool:
  - `Docs/Tools/shift_receiver_bay.py`
- Created: 2026-07-21
- Creation mode: deterministic raster translation from the approved single-board environment;
  no artwork was regenerated or redrawn.
- Runtime treatment: the baked lower receiver bay—including its shadow, outer rim, silver panel,
  and blue-violet depth—is translated downward by exactly 70 source pixels. The newly exposed gap
  continues the existing blue environment texture without duplicating the chute posts. Unity
  applies the matching `-0.765` local-Y offset to the complete receiver controller, so receiver
  boxes, lids, flat arrow lanes, transfer destinations, queue motion, and completion stars remain
  registered to the moved bay. The lower rim is intentionally clipped below the portrait viewport;
  the upper board, chute, conveyor, receiver scale, spacing, and gameplay mechanics are unchanged.

## Marble Star level-completion UI

- Runtime project assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MarbleStarCompletion20.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MarbleStarCompletion40.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MarbleStarCompletion60.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MarbleStarCompletion80.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MarbleStarCompletion100.png`
- Approved production sources:
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion20_Source.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion40_Source.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion60_Source.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion80_Source.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion100_Source.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion40_CleanSource.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion60_CleanSource.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion80_CleanSource.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion100_CleanSource.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion40_CleanSourceV2.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion60_CleanSourceV2.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion80_CleanSourceV2.png`
  - `Docs/ApprovedArtwork/Completion/MarbleStarCompletion100_CleanSourceV2.png`
- Deterministic production tool:
  - `Docs/Tools/bake_marble_star_completion_cards.py`
- Created: 2026-07-22
- Creation mode: the 40% and 60% V2 production masters are the exact approved files supplied by
  the user; the approved 80% and 100% previews are copied unchanged into their V2 production-master
  slots. No completion-card artwork is regenerated by Unity. The deterministic baker identifies
  the authored closed pearlescent outer rim on either navy or warm-grey inspection canvases,
  removes only that exterior canvas, preserves every authored pixel inside the card, and outputs
  transparent RGBA textures for live-game compositing. Celebration rays and sparkles remain
  exclusive to the approved 100% source.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  game artwork, logo, character, interface asset, or typeface was added.
- Runtime treatment: the first completed level starts the visible Marble Star at 40%, then advances
  to 60%, 80%, and 100% by the end of level 4; every completion awards 25 coins. The progress bar
  uses one stage color across its complete filled
  portion: red, blue, green, yellow, then violet. The 20–80% states deliberately contain no rays,
  halo, or sparkle stars; those celebration effects appear only at 100%. The card is composited
  over a 56%-strength navy dimmer drawn in physical screen space so the entire game background is
  covered on every aspect ratio. There is no timed transition: only the baked Continue button
  advances to the next level, and the completion after 100% restarts the visible cycle at 40%.
  The visible progress channel uses one live runtime layer for both the 1.15-second count-up and
  the completed endpoint. It is clipped to the exact inner navy track and mathematically clamped
  from 0 to 1, so it cannot overlap or exceed the pearlescent bar frame. The percentage counts in
  sync while the fill blends red, blue, green, yellow, and violet as each stage is crossed. Keeping
  the same layer active after it reaches its target prevents any color or geometry jump to the bar
  contained in the source artwork; the approved card frame and Marble Star remain unchanged.

Prompt brief:

> Preserve the exact approved premium blue completion card and five-point Marble Star. Produce
> 20, 40, 60, 80, and 100 percent states with clockwise red, blue, green, yellow, and violet point
> activation; use the newest point's color across the complete filled bar; keep LEVEL COMPLETED!,
> +25, and CONTINUE exact. Use ordinary material highlights only for partial states, and reserve
> the soft aura, restrained rays, and small golden sparkle stars exclusively for 100%.

## Level 4 Mystery Box completion card

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/MysteryBoxCompletion.png`
- Exact approved source:
  - `Docs/ApprovedArtwork/Completion/MysteryBoxCompletion_Source.png`
  - `Docs/ApprovedArtwork/Completion/MysteryTrayPreview_Source.png`
  - `Docs/ApprovedArtwork/Completion/MysteryBoxCompletion_WithTray_Source.png`
- Deterministic production tool:
  - `Docs/Tools/bake_marble_star_completion_cards.py`
  - `Docs/Tools/composite_mystery_tray_completion.py`
- Created: 2026-07-22
- Creation mode: the user-approved card and grey mystery-tray previews are preserved as their
  production sources. The deterministic compositor removes only the tray preview's pale-blue
  inspection canvas, scales its continuous 3D silhouette, and centers it in the card's recessed
  showcase above the gold ribbon. The card baker then removes only the external dark studio canvas
  and crops the transparent exterior; Unity does not reconstruct the mystery tray, card, ribbon,
  reward capsule, typography, or Continue button.
- Runtime treatment: completing level 4 first shows the normal 100% Marble Star card. Pressing its
  Continue button keeps level 4 loaded and swaps to this baked Mystery Box card over the same
  full-screen dimmer. The card awards the displayed 25-coin milestone bonus exactly once. Its baked
  Continue button then advances to level 5. Only an invisible hit region and the shared press/bounce
  treatment are layered over the authored button pixels.

## Level 5 mystery trays and Pink production set

- Runtime mystery asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/Mystery/MysteryTopTray.png`
- Runtime Pink assets:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/TopTrays/TopTray_Pink_00.png` through
    `TopTray_Pink_09.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/ReceiversV3/Receiver_Pink_Open_00.png`
    through `Receiver_Pink_Open_03.png`, plus `Receiver_Pink_Closed.png`
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/Receivers/ReceiverBall_Pink.png`
- Deterministic production tools:
  - `Docs/Tools/bake_approved_top_trays.swift`
  - `Docs/Tools/bake_approved_receivers.swift`
  - `Docs/Tools/bake_pink_receiver_ball.py`
- Created: 2026-07-22
- Creation mode: built-in image generation was used only to preserve the approved standalone
  grey mystery tray while replacing its preview canvas with a removable chroma key. The standard
  local image helper converted that key to contracted, despilled alpha. Pink tray, receiver, and
  ball states are deterministic hue variants of the exact approved production masters used by
  the existing colors; Unity never rebuilds their appearance.
- Runtime treatment: Level 5 contains four ordinary exposed first-row trays, four mystery trays
  in the second row, and two centered mystery trays in the third row. Mystery is explicit catalog
  data, independent of the concealed color. A mystery tray remains fixed in its authored cell and
  uses the same directly-in-front/directly-beside reveal rule as existing hidden trays; reveal
  swaps the one baked mystery sprite for the correct complete nine-ball tray. Pink occurs exactly
  once, in a third-row mystery tray, with three matching receiver boxes.

Prompt brief:

> Preserve the approved compact grey-blue 3D mystery tray, large centered white question mark,
> rounded premium shell, front-wall depth, thin dark outline, and clean highlights. Replace only
> the exterior preview background with a flat chroma key for alpha extraction. Add no balls,
> text, extra icons, cast background, watermark, or geometry changes.

## Conveyor-full loss card

- Runtime project asset:
  - `MarbleSort/Assets/MarbleSort/Resources/Presentation/UI/Completion/ConveyorFullLossCard.png`
- Approved production source:
  - `Docs/ApprovedArtwork/Completion/ConveyorFullLossCard_Source.png`
- Deterministic production tool:
  - `Docs/Tools/bake_conveyor_full_loss_card.py`
- Created: 2026-07-23
- Creation mode: built-in image generation edited the approved loss-card direction into a clean
  static frame containing LEVEL FAILED!, an empty navy showcase, CONVEYOR FULL, and TRY AGAIN.
  The deterministic baker removes only the exterior warm-grey inspection canvas using the same
  closed pearlescent-rim silhouette process as the approved completion cards.
  The earlier generated conveyor illustration was deliberately excluded from the production
  asset; there is no baked conveyor icon, ball arrangement, warning symbol, or duplicate conveyor.
- Ownership/licensing note: generated specifically for this case-study prototype; no third-party
  artwork, logo, character, or interface asset was added.
- Runtime treatment: when the deadlock detector confirms that the conveyor is full, gameplay input
  and conveyor motion are frozen first. A fresh camera render is cropped to the real conveyor's
  renderer bounds and composited into the card's empty showcase, so the loss UI displays the exact
  final conveyor and ball state that caused the failure. The full physical screen is dimmed behind
  the card. The baked Try Again button keeps an invisible hit target plus the shared press/bounce
  feedback; activating it reloads the current level, destroys the transient snapshot, and resumes
  the same conveyor mechanics.

Prompt brief:

> Preserve the premium glossy blue, white, and gold loss-card frame and its typography. Remove the
> baked conveyor, balls, warning icon, progress, and coin reward completely. Leave one large clean
> dark-navy recessed viewport for the game's exact runtime conveyor capture. Keep CONVEYOR FULL and
> the turquoise TRY AGAIN button, with no extra objects, decorative particles, or corrupted marks.
