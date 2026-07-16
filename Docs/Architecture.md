# Architecture

## Runtime boundaries

- **Data** parses the single JSON level catalog into serializable data-transfer objects.
- **Validation** applies deterministic rules shared by runtime loading and editor tooling.
- **Session** owns the current level index and wraps to level one after the final level.
- **Top Grid** owns box exposure, selection, nine-marble release, and column collapse.
- **Marbles** own lifecycle state while pooling owns allocation and reuse.
- **Platform** contains physical marbles and feeds one entrance queue.
- **Conveyor** advances 24 logical slots along one analytical stadium path.
- **Receivers** expose one three-marble box per lane and advance each FIFO queue.
- **Level Flow** evaluates completion and deadlock from state-change events.
- **Presentation** observes gameplay state and supplies animation, audio, particles, and haptics.

## Performance rules

- Dynamic rigidbodies exist only while marbles are spawning, falling, or waiting on the platform.
- Conveyor marbles are kinematic and occupy explicit logical slots.
- Conveyor motion is updated by one controller rather than one component per marble.
- Gameplay checks are event-driven; scene searches and per-frame allocations are avoided.
- Reusable marbles and effects are pooled.

## Level-data invariant

For every marble color:

```text
receiver box count == top box count * 3
```

Each top box creates nine marbles. Each receiver box accepts three marbles.
