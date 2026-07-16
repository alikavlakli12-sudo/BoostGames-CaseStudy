# Marble Sort

A polished, data-driven mobile puzzle prototype created for the Boost Games Developer Case Study.

## Required editor

- Unity `6000.3.10f1`
- Portrait mobile presentation
- 3D physics constrained to a 2D gameplay plane

Do not open or upgrade the project with another Unity version. The required editor version is part of the evaluation criteria.

Pressing Play from any open editor scene automatically starts `Assets/MarbleSort/Scenes/Main.unity`.

## Quick start

1. Clone `https://github.com/alikavlakli12-sudo/BoostGames-CaseStudy.git`.
2. In Unity Hub, add the repository's `MarbleSort` folder using Unity `6000.3.10f1`.
3. Open `Assets/MarbleSort/Scenes/Main.unity` and press Play. The Play Mode scene guard also routes
   Play from any other open scene to `Main.unity`.
4. Select an exposed colored top box. It releases nine physical marbles; receivers collect only
   matching conveyor marbles when they pass the lane's collection point.

The game is portrait-only. Mouse input in the editor and touch input on mobile use the same
collider-based selection path.

## Prototype pillars

- Top-box grid where each selected box releases exactly nine matching marbles
- Optimized rigidbody simulation inside a funnel-shaped platform
- Counterclockwise 24-slot stadium conveyor
- Four ordered receiver lanes, with three marbles per receiver
- Event-driven completion and deadlock detection
- Five levels stored in one JSON catalog
- Shared runtime/editor validation of production structure, per-color `1:3` capacity, and solvability
- Custom Unity authoring window with safe JSON saving and selected-level preview/reload
- Responsive portrait presentation with rounded cached meshes, feedback, procedural audio, and safe-area HUD
- Professional, reviewable Git history

## Repository layout

```text
MarbleSort/   Unity project
Docs/         Architecture and workflow notes
README.md     Project overview and setup
```

The implementation source of truth is documented in
[Docs/GameSpecification.md](Docs/GameSpecification.md). The staged one-week plan is in
[Docs/ImplementationPlan.md](Docs/ImplementationPlan.md), and the custom level workflow is in
[Docs/LevelAuthoring.md](Docs/LevelAuthoring.md). Presentation architecture and runtime budgets are
documented in [Docs/PresentationAndPerformance.md](Docs/PresentationAndPerformance.md), with the
generated background's creation record in [Docs/AssetProvenance.md](Docs/AssetProvenance.md).

## Level editor

Open **Marble Sort > Level Catalog** to inspect and author the single JSON catalog. The window:

- validates the strict per-color `1:3` top-box-to-receiver-box ratio;
- verifies the 24-slot conveyor and four receiver lanes;
- runs deterministic solvability analysis;
- blocks invalid JSON from being saved or previewed; and
- previews any selected level through a clean `Main.unity` runtime state.

Detailed instructions are in [Docs/LevelAuthoring.md](Docs/LevelAuthoring.md).

## Testing and delivery QA

Use **Marble Sort > QA > Validate Delivery** for the reviewer-facing readiness gate. It verifies
the exact editor version, portrait settings, build scene, JSON source, all five solutions, scene
systems/references, material shaders, documentation, and repository ignore rules.

The Unity Test Runner contains EditMode coverage for state, validation, solver, pooling,
presentation, and delivery boundaries, plus PlayMode coverage for physics, admission, receivers,
deadlock, progression, feedback reuse, and all five runtime level builds. Reproducible command-line
commands and the optional desktop smoke-build command are documented in
[Docs/DeliveryQA.md](Docs/DeliveryQA.md).

## Architecture and optimization

- Physical marbles are prewarmed and pooled; conveyor marbles become kinematic logical occupants.
- One controller advances all 24 conveyor positions along an analytical stadium path.
- Slot reservation prevents double occupancy, and receiver transfer remains event-driven.
- Presentation meshes/materials are cached, while one particle system and one audio source serve
  the session.
- The runtime performance probe tracks rolling FPS, worst-frame time, and generation-zero GC
  collections without per-frame allocation.

See [Docs/Architecture.md](Docs/Architecture.md) and
[Docs/PresentationAndPerformance.md](Docs/PresentationAndPerformance.md) for the full decisions and
runtime budgets.

## Known limitations

- Progress is session-only by design; the brief does not require persistence, and Level 5 wraps to
  Level 1.
- The prototype is intentionally portrait-only and has no landscape-specific layout.
- Haptics use Unity's platform-guarded generic vibration API, so intensity varies by device.
- Audio is procedurally synthesized for a license-safe prototype and does not use a production
  mixer or authored music.
- Menus, economy, monetization, analytics, accessibility localization, and live content are outside
  the case-study scope.

Unity-generated folders such as `Library`, `Temp`, `Logs`, `Obj`, `Build`, and `UserSettings` are intentionally excluded from Git.

## Current milestone

Delivery QA is complete. The repository now includes an automated 12-check readiness gate,
35 EditMode tests, 16 PlayMode tests, reproducible command-line verification, a successful desktop
smoke-build workflow, clean-clone coverage, reviewer setup instructions, and explicit known
limitations. The project is ready for final repository review and submission after the
`delivery-qa` branch is pushed and merged according to the Git gate.
