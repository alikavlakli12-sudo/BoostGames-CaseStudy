# Git Workflow

## Branches

- `main` must always represent a compiling, reviewable milestone.
- Feature work uses short-lived, descriptive branches such as `presentation-performance` and
  `delivery-qa`.
- Do not commit Unity-generated cache folders or local builds.

## Commit boundaries

1. Exact-version project foundation, JSON contract, base scene, and repository hygiene
2. Top grid, pooling, and physical marble spawning
3. Stadium conveyor and one-at-a-time entrance
4. Receiver queues, completion, and deadlock logic
5. Five production levels and custom level-editor workflow
6. Presentation polish and mobile feedback
7. Profiling evidence, delivery documentation, and final QA

Each commit should compile and should include the tests or documentation relevant to its feature.

## Push policy

- Push only after the matching local verification step passes.
- Review `git status` and `git diff --cached` before every commit.
- Review the new commit with `git show --stat --oneline HEAD` before every push.
- Never force-push `main`.
