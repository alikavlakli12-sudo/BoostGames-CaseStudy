# One-Week Implementation Plan

Each stage ends with a compiling project, focused verification, and a reviewable Git commit. `main` is pushed only after its local gate passes.

## Day 1 — Foundation

- Install and lock Unity `6000.3.10f1`.
- Create portrait project settings, assembly boundaries, base scene, materials, and repository hygiene.
- Add the JSON schema, shared validator, five starter levels, session state, stadium-path math, and foundation tests.
- Gate: Unity imports cleanly and all EditMode tests pass.

Status: complete and verified as the first project milestone.

## Day 2 — Top grid and physical marbles

- Build the runtime grid from JSON.
- Implement exposed-box input rules, column collapse, and nine-marble release.
- Add marble pooling, constrained rigidbodies, collision layers, and basin containment.
- Gate: repeated selections allocate no new marble objects after pool warm-up and never allow a covered box to be selected.

## Day 3 — Conveyor admission

- Separate physical basin marbles from logical conveyor occupants.
- Implement single-file entrance admission and 24-slot occupancy.
- Animate deterministic counterclockwise movement along the stadium path.
- Gate: no slot can contain two marbles, a full conveyor blocks admission, and long runs keep stable ordering.

## Day 4 — Receivers and level flow

- Implement four FIFO lanes, active heads, color matching, three-marble capacity, and queue advance.
- Add event-driven completion, deadlock, retry, next-level flow, and five-to-one wraparound.
- Gate: automated tests cover matching, non-matching, completion, deadlock, and session progression; one full level is playable end to end.

## Day 5 — Five production levels and editor workflow

- Finalize all five configurations and difficulty progression.
- Expand the custom level window with actionable validation and rapid preview/reload controls.
- Run solvability and count checks on every level.
- Gate: all production JSON passes validation and every level can be completed from a clean launch.

## Day 6 — Presentation and performance

- Replace blockout visuals with cohesive prototype art while retaining reference composition.
- Add selection, release, collection, box-complete, level-complete, and deadlock feedback.
- Add mobile audio, particles, haptics, safe-area handling, and responsive portrait scaling.
- Profile physics, scripts, rendering, memory, and garbage collection on the target device.
- Gate: stable target frame rate, no recurring gameplay allocations, and no blocking console errors.

## Day 7 — Delivery QA

- Perform device playthroughs, edge-case checks, clean-clone verification, and final regression tests.
- Produce the required build, README/setup instructions, architecture explanation, and demonstration capture.
- Review commit history and repository contents for secrets, caches, oversized files, and accidental artifacts.
- Gate: a reviewer can clone, open with the required Unity version, run the scene, run tests, and build without undocumented steps.

## Git gates

Before every commit:

1. Run the verification appropriate to the feature.
2. Inspect `git status --short`.
3. Stage only the intended milestone files.
4. Inspect `git diff --cached --check`, `git diff --cached --stat`, and the staged diff.
5. Commit with one outcome-focused message.

Before every push:

1. Inspect `git show --stat --oneline HEAD`.
2. Confirm the branch and remote tracking target.
3. Push normally; never force-push `main`.
