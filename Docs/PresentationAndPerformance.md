# Presentation and Performance

Milestone 5 keeps the verified gameplay state model unchanged and layers presentation onto the
existing runtime events. The visual scene can still be rebuilt deterministically with
**Marble Sort > Setup > Rebuild Base Scene**.

## Visual system

- `PortraitBackground.png` is an original project-specific illustrated backdrop. It is imported at
  1024×1536, compressed, clamped, and without mipmaps for the fixed portrait camera.
- Its creation method, final prompt, and licensing note are recorded in
  [AssetProvenance.md](AssetProvenance.md).
- Closed top boxes, basin panels, and the conveyor use cached procedural meshes.
  Generated mesh objects are marked `DontSave`; serialized mesh specifications recreate them on
  load without duplicating mesh data inside the scene.
- Exposed upper boxes replace the closed-box renderers with one of four transparent hyper-realistic
  3×3 tray renders: visible lower molded side, highlighted rounded rim, clean face, and nine deep
  color-matched wells. Nine independently controlled glossy-ball sprites sit over those wells and
  disappear one-by-one during release. Covered boxes keep a clean closed face until they become
  selectable, matching the reference game's interaction language.
- Receiver boxes use four color-specific transparent renders of the approved molded 1×3 artwork.
  Each render contains the rounded highlight rim, deep color-matched wells, and visible lower side.
  Three independent glossy-ball sprite layers preserve the receiver's 0–3 fill states and transfer
  targets instead of flattening gameplay into a single filled image. The importer keeps alpha,
  disables mipmaps, clamps edges, and caps source textures at 1024 px for the mobile prototype.
- Pooled physics balls and conveyor occupants keep using four cached color-specific glossy
  materials. Top trays reuse the same four rendered glossy-ball sprites as receivers; all tray and
  ball textures are cached once and shared across levels.
- One continuous stadium-ribbon mesh replaces the three-piece conveyor blockout.
- Shared materials, instancing, and disabled real-time shadows keep the soft outlined style
  inexpensive. Highlights and shadows are explicit lightweight geometry.
- The responsive camera preserves the full gameplay width on narrow portrait devices and scales
  the illustrated backdrop to cover the visible area.
- The immediate-mode HUD uses generated nine-slice textures, honors `Screen.safeArea`, and exposes
  retry, completion, deadlock, live tray progress, and one-time Level 1 guidance without adding a
  canvas hierarchy.
- Exposed top boxes use nine visible marble markers plus a subtle allocation-free pulse, making the
  valid interaction state readable without adding permanent arrows or hand overlays.
- If a nine-marble release would exceed the 36-marble loose-board budget, that tray remains intact
  and briefly displays `Board Full` directly over its artwork. The feedback object is prebuilt with
  the tray and reuses its renderer on repeated taps.

## Feedback and audio

- Top-box selection, conveyor admission, marble collection, receiver completion, level completion,
  and deadlock state feed one `GameFeedbackController`.
- A single reusable particle system emits all color-matched bursts. Level reloads do not create
  additional particle systems.
- Six short audio clips are synthesized and prewarmed once per scene, so the prototype has no
  third-party audio licensing dependency. Admission and collection sounds are rate-limited to
  avoid noisy overlaps.
- Major completion and deadlock events use platform-guarded haptics on iOS and Android and remain
  no-ops in the editor and unsupported builds.
- Receiver feedback keeps a lane locked until its transfer and pulse animation finish, preserving
  the one-marble-at-a-time state invariant.

## Runtime budgets and guards

| Resource | Budget/behavior |
| --- | --- |
| Marble actors | 72 prewarmed: 36 loose/reserved board marbles + 24 conveyor occupants + 4 receiver transfers + 8 hand-off reserves; stress test permits zero runtime expansion |
| Conveyor positions | 24 deterministic logical slots |
| Feedback particles | One system, maximum 160 live particles |
| Feedback audio | One `AudioSource`, six prewarmed procedural clips |
| Presentation meshes | Shared cache keyed by dimensions; repeated level loads do not grow it |
| Tray artwork | Twelve cached alpha textures (four receivers, four 3×3 trays, four shared balls); no mipmaps; 1024 px maximum import size |
| Background texture | 1024×1536, compressed, no mipmaps |
| Target frame rate | 60 FPS |
| Runtime diagnostics | Allocation-free rolling frame probe with FPS, worst-frame, and GC counters |

The automated highest-load checks build Level 4 twice and verify that marble count, mesh-cache
count, particle system, and audio source remain unchanged. A separate dense-board test reserves
the full 36-marble budget, verifies tray-local rejection and recovery, samples 120 steady-state
frames with at least 30 simultaneously settled rigidbodies, checks the no-overlap invariant, and
requires zero pool expansion. Rendering and interaction objects use shared materials and do not
request real-time shadows.

## QA checklist

1. Play Level 1 and confirm the retry control and level title remain below the device safe area.
2. Clear all six Level 1 trays and confirm hidden-tray reveals, selection, admission, collection, receiver completion,
   level completion, and automatic progression feedback.
3. Preview Level 4 through **Marble Sort > Level Catalog** and verify three-deep stacks, all four
   nine-box receiver lanes, and the full lower receiver bay remain inside the portrait frame.
4. Trigger a full incompatible conveyor and confirm the deadlock overlay and retry reset.
5. Run all EditMode and PlayMode tests after any presentation or scene-bootstrapper change.

The PlayMode layout guard also loads every production level and verifies that all top-box and
receiver renderers remain inside the portrait camera, including Level 4's nine-deep receiver lanes.
