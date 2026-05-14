---
name: Tutorial metrics tracker
overview: "Split Tutorial: data-only `TutorialMetricsTracker` subscribed to `TutorialStageManager` lifecycle + both `MultiDimensionPuzzelManager.OnAttemptSubmitted`; `Time.realtimeSinceStartup`; debug Inspector fields + `GetSnapshot()`; no scoring, high scores, summary UI, or Body_TMP writes."
---

# Tutorial metrics tracking (Split Tutorial)

## Goal

Track tutorial session data only: elapsed times, per-player attempts, solve flags, completion — for future summary, scoring, and local best persistence. Do not change puzzle, diagnostic, history, input, or action-lock behavior.

## Architecture

### `TutorialStageManager` ([`Assets/WhoWiredThis/Scripts/Tutorial/TutorialStageManager.cs`](Assets/WhoWiredThis/Scripts/Tutorial/TutorialStageManager.cs))

- **`TutorialSessionStage`**: `PlayerAOperator`, `PlayerBOperator`, `Complete` (public enum in the same file as the manager for reliable Unity compilation).
- **`CurrentStage`** (public getter).
- **`OnTutorialStarted`**: once after initial `ApplyStageVisualAndLocks()` in `Start()` (tutorial ready for input; does not wait for intro diagnostic coroutine).
- **`OnStageChanged(TutorialSessionStage)`**: after first apply, on A→B, on B→Complete (before `OnTutorialCompleted`).
- **`OnTutorialCompleted`**: unchanged; still paired with optional `UnityEvent`.

### `TutorialMetricsTracker` ([`Assets/WhoWiredThis/Scripts/Tutorial/TutorialMetricsTracker.cs`](Assets/WhoWiredThis/Scripts/Tutorial/TutorialMetricsTracker.cs))

- Serialized refs: `TutorialStageManager`, `playerAPuzzleManager`, `playerBPuzzleManager`.
- Subscribes: `OnTutorialStarted`, `OnStageChanged`, `OnTutorialCompleted`, both `OnAttemptSubmitted`.
- Timing: **`Time.realtimeSinceStartup`** only.
- Attempts: count only real `OnAttemptSubmitted` invocations (locked/no-op paths do not emit).
- Debug-only `[SerializeField]` fields for Play Mode: `totalAttempts`, `playerAAttempts`, `playerBAttempts`, `totalElapsedSeconds`, `playerAElapsedSeconds`, `playerBElapsedSeconds`, `playerASolved`, `playerBSolved`, `tutorialComplete`.
- **`GetSnapshot()`** → [`TutorialMetricsSnapshot`](Assets/WhoWiredThis/Scripts/Tutorial/TutorialMetricsSnapshot.cs) for future consumers.

### Metric definitions

| Metric | Rule |
|--------|------|
| Tutorial / total elapsed | Start at `OnTutorialStarted`; end at `OnTutorialCompleted`. |
| Player A segment | Start with tutorial; end at first A `OnAttemptSubmitted` with `IsSolved`. |
| Player B segment | Start at `OnStageChanged(PlayerBOperator)`; end at first B solved `OnAttemptSubmitted`. |
| Attempts | Every `OnAttemptSubmitted` on that side’s manager (includes failed and solved). |

## Scene wiring

- [`Assets/Scenes/Split Tutorial.unity`](Assets/Scenes/Split Tutorial.unity): `TutorialMetricsTracker` on the **`TutorialStageManager`** GameObject; references match the existing `TutorialStageManager` puzzle manager assignments.

## Explicit non-goals

- No score calculation, high scores, summary UI, or `Body_TMP` writes from this layer.
- No changes to combination checking, diagnostic adapters, shared history, input modules, or `PanelActionLock` / lock bundles.

## Implementation status

Implemented in-repo per the approved spec. Optional: re-split `TutorialSessionStage` into its own `.cs` file later if desired (enum currently lives in `TutorialStageManager.cs` after a transient compile issue with a standalone file).

## Manual verification (Play Mode)

1. `totalAttempts` ≈ Shared History row count (A rows + B rows).
2. `playerAElapsedSeconds` freezes when A solves.
3. `playerBElapsedSeconds` begins when B becomes operator (stage change).
4. `totalElapsedSeconds` freezes on tutorial complete.
