---
name: Floor Matrix Puzzle
overview: Add a new floor-color matrix puzzle manager that mirrors A17 engage/score behavior while keeping A17 stable. Reuse shared flow via a lightweight helper used by the new puzzle now, with optional later migration.
todos:
  - id: add-floor-config-so
    content: Create floor matrix config ScriptableObject with matrix solution + A17-style scoring fields
    status: completed
  - id: add-shared-scoring-helper
    content: Implement reusable helper for attempt counting, score calculation, and engage flow callbacks
    status: completed
  - id: add-floor-matrix-manager
    content: Implement scene-wired matrix manager using PolaritySwitchController grid and config matrix comparison
    status: completed
  - id: validate-and-test
    content: Add shape/null validation logs and verify solve/fail/score flows in play mode
    status: completed
isProject: false
---

# Floor Color Matrix Puzzle Plan

## Scope
- Build a new puzzle flow for floor-color matrix switches using scene-driven matrix wiring.
- Reuse A17-style engage/scoring logic via a small shared helper (hybrid-safe), without refactoring existing `A17PuzzleManager` behavior now.

## Design Decisions
- Matrix shape source: scene wiring (rows/columns composed in inspector from placed switches).
- Config remains ScriptableObject-driven for solution/scoring.
- Reuse strategy: introduce helper for attempt/scoring/solve flow; use it in new manager only.

## Planned Implementation
- Add a new floor puzzle config SO based on [Assets/WhoWiredThis/Scripts/Data/A17/PuzzleConfigSO.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Data/A17/PuzzleConfigSO.cs):
  - Keep scoring fields (`startScore`, `penaltyFreeAttempts`, `penaltyPerAttempt`, `minScore`, `hintTriggerAttempt`).
  - Replace 1D `solution` with matrix-friendly serialized format (rows list where each row stores `PolarityState[]`).
- Add a shared helper (new small class in puzzle domain) for:
  - Attempts counter
  - Score calculation
  - Success/failure flow hooks
  - This mirrors logic currently in [Assets/WhoWiredThis/Scripts/Puzzles/A17/A17PuzzleManager.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/A17/A17PuzzleManager.cs) (`ComputeCurrentScore`, `TryEngage` flow).
- Add new floor matrix manager (parallel to A17 manager):
  - Serialized scene matrix wiring of `PolaritySwitchController` (row containers / row arrays).
  - `TryEngage()` compares scene matrix against config matrix (row count, column count, cell states).
  - Uses shared helper for scoring + attempt penalties + events.
  - On success: same side effects as A17 (`ScoreManager`, `GameManager`, success event).
- Keep [Assets/WhoWiredThis/Scripts/Puzzles/A17/PolaritySwitchController.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/A17/PolaritySwitchController.cs) unchanged unless a small API convenience is needed for matrix manager readability.

## Validation
- Add defensive validation logs on engage:
  - matrix shape mismatch between scene and config
  - null switch cells
- Manual test checklist:
  - Correct matrix solves puzzle and awards expected score.
  - Incorrect matrix increments attempts and applies penalty step.
  - Hint trigger attempt value is exposed correctly.
  - Existing A17 puzzle behavior remains unchanged.

## Migration Note (Optional Later)
- If stable, optionally migrate `A17PuzzleManager` to the same shared helper in a separate follow-up change.