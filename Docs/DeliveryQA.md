# Delivery QA

This document maps the final repository to the Boost Games Developer Case Study delivery and
evaluation criteria. The mandatory deliverable is the GitHub-hosted Unity project; a standalone
player is an additional smoke-test artifact and is not committed.

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
| Physics/conveyor performance | Pooling, kinematic conveyor, cached presentation resources | Highest-load reuse test and runtime probe |
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

"$UNITY" -batchmode -nographics -projectPath "$PWD/MarbleSort" \
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

Final local verification on 2026-07-16:

- Delivery readiness: 12/12 checks passed.
- EditMode: 35/35 tests passed.
- PlayMode: 16/16 tests passed.
- Desktop smoke build: succeeded as a portrait-windowed 64-bit universal macOS application
  (Apple Silicon and Intel).
- Player boot: initialized Unity `6000.3.10f1`, PhysX, the Main scene, and all 5 validated levels
  without a runtime exception.
- Repository history: no reachable oversized or accidental binary objects; largest tracked asset
  is the 1.6 MB project background.

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
