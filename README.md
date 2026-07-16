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
[Docs/LevelAuthoring.md](Docs/LevelAuthoring.md).

Unity-generated folders such as `Library`, `Temp`, `Logs`, `Obj`, `Build`, and `UserSettings` are intentionally excluded from Git.

## Current milestone

Five production levels and the editor workflow are complete. The difficulty curve grows from two
unstacked top boxes to eight boxes with three-deep dependencies. The **Marble Sort > Level Catalog**
window authors the JSON draft, reports actionable structural and solvability diagnostics, shows a
verified solution order, and previews any selected level directly in `Main.unity`. Presentation,
mobile feedback, and performance profiling are the next milestone.
