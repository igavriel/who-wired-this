---
task: Puzzel Pipes Phase 5 — Randomized Solution
date: 2026-05-19
status: validated
scene: Assets/Scenes/Puzzel Pipes.unity
---

# Puzzel Pipes Phase 5 — Randomized Solution

## Scope

- Runtime randomization of `MultiDimensionPuzzelManager` `correctIndex` values only on **Puzzel Pipes**.
- Blue and Red each receive an independent 3-index solution (4 states per input).
- No changes to labels, `displayName`, TMP, history, result visualizer, diagnostic copy, or turn flow.
- **Tutorial.unity** unchanged.

## Out of scope

- Progressive hints, scoring, high scores, main menu.
- Global prefab changes.
- Result visualizer / history / diagnostic text changes.
- Persisting generated indices into scene assets.

## Approved implementation steps

1. Minimal read/apply API on `MultiDimensionPuzzelManager` + internal `SetCorrectIndex` on `MultiDimensionPuzzleElement`.
2. `PuzzleSolutionGenerator` — constraint checks, retry, deterministic fallback `{1,2,1}`.
3. `RandomPuzzleSolutionAssigner` — `[DefaultExecutionOrder(-50)]`, `Awake` in Play Mode only; Inspector toggles.
4. Wire on `TutorialStageManager` in Puzzel Pipes; menu **Wire Random Solution Assigner (Phase 5)**.
5. `PipePressurePhase5ValidationTool` — separate menu from Phase 1; restores fixed scene indices after run.
6. Phase 1 validation unchanged (fixed `2,1,2` / `3,2,3` when randomizer disabled).

## Runtime behavior

| Field | Default (Puzzel Pipes) |
|-------|------------------------|
| `enableRandomization` | `true` |
| `useSeed` | `false` |
| `seed` | `0` |
| `logToConsole` | `false` |
| `debugBlueSolution` / `debugRedSolution` | Inspector-only debug strings |

- Seeds: Blue `seed`, Red `seed + 1` when `useSeed` is true.
- Must run before first SEND and before diagnostics compare (`Awake`, order before `TutorialStageManager`).

## Constraints (v1)

- Not all three values identical.
- Not all edge values only (`0` or `max`).
- At least one middle value per solution.
- Up to 32 random attempts, then fallback.

## Testing checklist

- [x] Phase 1 validation with fixed scene values passes.
- [x] Phase 5 validation (generator, apply, solve, diagnostic read) passes.
- [x] Scene YAML still authored `2,1,2` / `3,2,3` after validation (restore step).
- [x] `Tutorial.unity` has no `RandomPuzzleSolutionAssigner`.
- [x] Unity compiles with zero errors.
- [ ] Manual: Play Mode variance with `useSeed=false` (multiple enters).
- [ ] Manual: Play Mode repeatability with `useSeed=true`.
- [ ] Manual: Solve using `debugBlueSolution` / `debugRedSolution` indices.

## Post-implementation fix (solved state)

Phase 5 editor validation called `TryCheckSolution`, which could persist `solved: 1` on both PuzzleManagers if the scene was saved afterward. That blocked `OnAttemptSubmitted` (history) and showed solved UI on load.

- Scene: both managers reset to `solved: 0`.
- `MultiDimensionPuzzelManager.ResetSessionForNewRun()` called from `RandomPuzzleSolutionAssigner` at Play Mode start.
- Phase 5 validation restores full scene state (solve reset + fixed `correctIndex`) after each run.

## Rollback

```bash
git checkout -- Assets/Scenes/Puzzel\ Pipes.unity \
  Assets/WhoWiredThis/Scripts/Puzzles/Common/RandomPuzzleSolutionAssigner.cs \
  Assets/WhoWiredThis/Scripts/Puzzles/Common/PuzzleSolutionGenerator.cs \
  Assets/WhoWiredThis/Editor/PipePressurePhase5ValidationTool.cs \
  Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs
```

Remove `RandomPuzzleSolutionAssigner` component from `TutorialStageManager` in Puzzel Pipes if reverting scene only.

## Files touched

| Path | Role |
|------|------|
| `Scripts/Puzzles/Common/PuzzleSolutionGenerator.cs` | Generator + constraints |
| `Scripts/Puzzles/Common/RandomPuzzleSolutionAssigner.cs` | Runtime assigner |
| `Scripts/Visibility/MultiDimensionPuzzelManager.cs` | Minimal API |
| `Editor/PipePressurePhase5ValidationTool.cs` | Phase 5 menu |
| `Editor/PipePressurePuzzelPipesWireTool.cs` | Wire menu |
| `Scenes/Puzzel Pipes.unity` | Component on `TutorialStageManager` |
