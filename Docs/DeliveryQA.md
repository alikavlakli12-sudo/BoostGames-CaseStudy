# Delivery QA

This document maps the final repository to the Boost Games Developer Case Study delivery and
evaluation criteria. The mandatory deliverable is the GitHub-hosted Unity project; a standalone
player is an additional smoke-test artifact and is not committed.

Delivery readiness validation: **12/12 automated checks passed**.

## Requirements evidence

| Brief requirement | Implementation evidence | Verification |
| --- | --- | --- |
| Unity `6000.3.10f1` | `ProjectSettings/ProjectVersion.txt` and delivery validator | Exact-version automated check |
| Exactly 9 marbles per top box | `TopGridController` and pooled release pattern | EditMode and PlayMode tests |
| Physics-based falling marbles | Constrained rigidbodies and funnel platform | PlayMode interaction tests |
| Continuous 24-slot conveyor | Analytical stadium path and centralized slot state | EditMode and PlayMode tests |
| One-at-a-time admission | Reservation/commit admission controller | Full-conveyor and multi-marble tests |
| Four ordered receiver lanes | FIFO receiver state/controller | Matching, transfer, completion, and advance tests |
| Transfer only at receiver position | Receiver collection-point crossing check | Non-early-transfer PlayMode test |
| Completion and receiver despawn | Event-driven level flow and receiver animation | Completion/progression tests |
| Full incompatible conveyor fails | Event-driven deadlock detector | EditMode and PlayMode deadlock tests |
| One JSON source and strict 1:3 ratio | Shared catalog loader/validator | Catalog and invalid-ratio tests |
| Custom level editor | **Marble Sort > Level Catalog** | Safe-save, solver, and preview workflow |
| At least 5 levels and wraparound | Five production levels in `levels.json` | Solver and session progression tests |
| Physics/conveyor performance | 36-marble loose-board reservation cap, 72 prewarmed actors, kinematic conveyor, cached presentation resources | Dense-board benchmark, zero-growth assertion, highest-load reuse test, and runtime probe |
| Professional Git delivery | Milestone branches and outcome-focused commits | Repository history and hygiene audit |

## Reproducible QA commands

Run from the repository root with the required Unity editor installed:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity"

"$UNITY" -batchmode -nographics -projectPath "$PWD/MarbleSort" \
  -executeMethod MarbleSort.Editor.DeliveryReadinessValidator.ValidateFromCommandLine \
  -logFile /tmp/marblesort-delivery-validation.log -quit

"$UNITY" -batchmode -nographics -projectPath "$PWD/MarbleSort" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/marblesort-editmode.xml \
  -logFile /tmp/marblesort-editmode.log

"$UNITY" -batchmode -projectPath "$PWD/MarbleSort" \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/marblesort-playmode.xml \
  -logFile /tmp/marblesort-playmode.log

"$UNITY" -batchmode -nographics -projectPath "$PWD/MarbleSort" \
  -executeMethod MarbleSort.Editor.DeliveryBuildAutomation.BuildDesktopSmokePlayerFromCommandLine \
  -logFile /tmp/marblesort-build.log -quit
```

The optional smoke player is written to `MarbleSort/Builds/QA/` and remains ignored by Git. Set
`MARBLE_SORT_BUILD_PATH` to override that destination.

## Verification record

Final local verification on 2026-07-23:

- Fresh GitHub clone: checked out `feature/level-restructure` at the pushed delivery commit and
  passed the validator, both complete test suites, and the desktop smoke build without missing
  scripts, scene references, shaders, or artwork.
- EditMode: 62/62 tests passed.
- PlayMode: 31/31 tests passed with the Metal graphics device.
- Level 5 receiver queues: no adjacent repeated colors, exact 1:3 color totals, and a verified
  10-selection solution with 16/24 peak conveyor occupancy.
- Dense-board benchmark: 120 frames at 59.9 average FPS, 23.68 ms worst frame, 31 peak loose
  rigidbodies, full 36-marble projected budget, and zero pool expansions in the complete suite.
- Desktop smoke build: succeeded as a portrait-windowed 64-bit universal macOS application
  (Apple Silicon and Intel).
- Player boot: initialized Unity `6000.3.10f1`, PhysX, the Main scene, and all 5 validated levels
  without a runtime exception.

## Final manual gate

1. Open the repository with Unity `6000.3.10f1` and confirm a clean Console.
2. Run **Marble Sort > QA > Validate Delivery**.
3. Open **Marble Sort > Level Catalog**, validate, and preview all five levels.
4. Play Level 1 through completion; verify progression, retry, and feedback.
5. Preview Level 5 and verify the maximum-load portrait layout and stable pooling.
6. Confirm the deadlock overlay and retry reset using the automated scenario or a full incompatible
   conveyor.
7. Run both test suites and the optional desktop smoke build.
8. Repeat the validation and tests from a clean clone before final submission.
