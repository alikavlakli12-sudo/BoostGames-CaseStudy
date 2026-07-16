# Marble Sort

A polished, data-driven mobile puzzle prototype created for the Boost Games Developer Case Study.

## Required editor

- Unity `6000.3.10f1`
- Portrait mobile presentation
- 3D physics constrained to a 2D gameplay plane

Do not open or upgrade the project with another Unity version. The required editor version is part of the evaluation criteria.

Pressing Play from any open editor scene automatically starts `Assets/MarbleSort/Scenes/Main.unity`.

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

Unity-generated folders such as `Library`, `Temp`, `Logs`, `Obj`, `Build`, and `UserSettings` are intentionally excluded from Git.

## Current milestone

Presentation and mobile-performance work are complete. The five production levels now use a
cohesive illustrated portrait layout, cached rounded geometry, a continuous stadium conveyor,
color-matched feedback, procedural audio, safe-area HUD states, and runtime performance guards.
Delivery QA, clean-clone/build verification, and the final demonstration capture are next.
