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
- Shared runtime/editor validation of the per-color `1:3` box ratio
- Professional, reviewable Git history

## Repository layout

```text
MarbleSort/   Unity project
Docs/         Architecture and workflow notes
README.md     Project overview and setup
```

The implementation source of truth is documented in
[Docs/GameSpecification.md](Docs/GameSpecification.md). The staged one-week plan is in
[Docs/ImplementationPlan.md](Docs/ImplementationPlan.md).

Unity-generated folders such as `Library`, `Temp`, `Logs`, `Obj`, `Build`, and `UserSettings` are intentionally excluded from Git.

## Current milestone

Top-grid and conveyor gameplay slice. JSON levels build the runtime grid, selections release nine
pooled rigidbody marbles, a single-file entrance admits them into distinct logical slots, and one
controller moves all 24 slots counterclockwise. Receiver collection and end-to-end level flow are
the next feature milestone.
