# Game Specification

This document converts the supplied case-study brief, screenshot, and gameplay recording into implementation rules. It is the working source of truth for the prototype.

## Playfield

- The game is presented in portrait with an orthographic 2.5D view.
- The upper section contains a gravity-driven basin, top-box grid, sloped funnel, and a narrow conveyor entrance.
- The middle section is a counterclockwise stadium conveyor with exactly 24 logical slots.
- The lower section contains four ordered receiver lanes.
- Physics is used for loose marbles in the upper basin. Conveyor and receiver movement are deterministic presentation systems.

## Top-box grid

- A top box has a stable ID, marble color, column, and row loaded from JSON.
- Every top box contains exactly nine same-color marbles.
- A box is exposed only when no box occupies a lower row in its column.
- Only exposed boxes accept input. Input is locked while that selection is being released.
- Selecting a box releases nine pooled rigidbody marbles into the basin.
- When the release finishes, the empty box is removed but every remaining tray stays at its authored grid coordinate.
- A hidden tray becomes exposed and selectable after a tray directly in front of it or directly beside it is cleared. Diagonal and pre-existing empty cells do not trigger a reveal.

## Loose marbles and entrance

- Spawned marbles use dynamic 3D rigidbodies constrained to the gameplay plane.
- Basin walls and sloped funnel surfaces keep loose marbles contained and guide them to the top-center conveyor entrance.
- The entrance admits at most one marble into one free logical conveyor slot at a time.
- A marble stays in the basin if the required entrance slot is unavailable.
- The same pooled actor switches from dynamic physics to a kinematic conveyor mode during transfer and returns to the pool after collection.

## Conveyor

- The conveyor follows one analytical stadium path and travels counterclockwise.
- It contains exactly 24 equally spaced logical slots.
- One controller advances the slot phase; individual slot occupants do not run independent update loops.
- Each slot is either empty or owns one marble color.
- Occupants retain their slot until collected by a compatible receiver.

## Receiver queues

- There are four FIFO receiver lanes.
- Only the head box of each lane is active.
- An active receiver accepts only marbles matching its color.
- Each receiver has a capacity of exactly three marbles.
- A matching conveyor occupant transfers into the receiver when it reaches that lane's collection point and capacity remains.
- On the third marble, the receiver plays completion feedback, despawns, and advances the next queued box into the active position.

## Completion and deadlock

- A level completes after every configured receiver box has been filled and no unresolved transfer remains.
- After completion feedback, the session advances to the next JSON level.
- Completing level five wraps the session back to level one.
- A deadlock occurs only when all 24 conveyor slots are occupied and none of their marble colors can enter any currently active receiver head with remaining capacity.
- Deadlock evaluation is event-driven after relevant slot or receiver state changes.

## Level-data invariant

For each color in a level:

```text
receiver box count == top box count * 3
```

One top box produces nine marbles and each receiver consumes three, so each top box requires exactly three receiver boxes of the same color. Invalid levels must be rejected by both the editor tool and runtime bootstrap.

## Prototype scope

Required for the case study:

- Complete core loop and five playable levels
- JSON-driven level configuration and custom editor support
- Pooling and mobile-conscious physics
- Level completion, deadlock, restart, and five-to-one wraparound
- Clear interaction, transfer, completion, failure, audio, particle, and haptic feedback
- Profiling evidence and an installable mobile build

Not part of the first gameplay milestone:

- Coins, purchases, ads, level locks, settings, analytics, or live-operations systems shown in the commercial reference
- Exact duplication of proprietary production art

Those reference elements inform composition and polish only; the evaluated prototype remains focused on the requested sorting loop.
