# Architecture

## Runtime boundaries

- **Data** parses the single JSON level catalog into serializable data-transfer objects.
- **Validation** applies deterministic rules shared by runtime loading and editor tooling.
- **Session** owns the current level index and wraps to level one after the final level.
- **Top Grid** owns fixed-grid exposure, selection, and nine-marble release. Hidden trays reveal when a tray directly in front or beside them is cleared; authored grid positions never compact or move.
- **Marbles** own lifecycle state while pooling owns allocation and reuse.
- **Platform** contains physical marbles and feeds one entrance queue.
- **Conveyor** advances 24 logical slots along one analytical stadium path.
- **Receivers** expose one three-marble box per lane and advance each FIFO queue.
- **Level Flow** evaluates completion and deadlock from state-change events.
- **Presentation** observes gameplay state and supplies animation, audio, particles, and haptics.

## Performance rules

- Dynamic rigidbodies exist only while marbles are spawning, falling, or waiting on the platform.
  Tray selection reserves all nine release positions atomically, and the loose/reserved board
  total cannot exceed 36. Conveyor and receiver actors are excluded from this board budget.
- The marble pool prewarms 72 actors: 36 board marbles, 24 conveyor occupants, four possible
  receiver transfers, and eight hand-off reserves. Runtime expansion and peak loose/active counts
  remain observable for QA, and the bounded overlap solver performs no collection allocation.
- Conveyor marbles are kinematic and occupy explicit logical slots.
- Conveyor motion is updated by one controller rather than one component per marble.
- The 192-frame conveyor render is stored in one padded 8×24 atlas. One SpriteRenderer and one
  shared material select the phase-locked frame through a reused MaterialPropertyBlock; no runtime
  sprite array, `Resources.LoadAll`, per-frame allocation, or independent visual timer exists.
- Gameplay checks are event-driven; scene searches and per-frame allocations are avoided.
- Reusable marbles and effects are pooled. A rejected full-board selection does not mutate tray
  state and uses a prebuilt, self-hiding text renderer on the attempted tray.
- Rounded box and stadium meshes are cached by dimensions and regenerated from serialized
  specifications instead of being duplicated in the scene.
- One feedback controller, particle system, audio source, and rolling performance probe serve the
  entire session and survive clean level rebuilds.

## Level-data invariant

For every marble color:

```text
receiver box count == top box count * 3
```

Each top box creates nine marbles. Each receiver box accepts three marbles.

## Production authoring boundary

`levels.json` is the single source of truth. The custom editor works on an in-memory serialized
draft, runs the same structural validator used at runtime, and invokes a deterministic search over
exposed top-box choices and receiver heads. Only a structurally valid, solvable draft can be saved
or previewed. Preview always starts `Main.unity` and asks the runtime level-flow controller to
rebuild the selected level, so authoring does not require scene edits or level-specific prefabs.

## Presentation event flow

Top-grid selection, conveyor admission, receiver acceptance, receiver completion, and level-flow
status changes publish events from their owning gameplay systems. The presentation controller
subscribes to those events and emits particles, audio, and platform-safe haptics without mutating
gameplay state. The responsive camera and safe-area HUD are independent observers of device layout.
