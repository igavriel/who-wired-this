---
task: Playtest flow start/gameover + total time
date: 2026-05-27
status: implemented
---

# Playtest flow start/gameover + total time

## Scope

- Add `StartScene` and `GameOverScene` bookends around the existing `Tutorial -> Puzzle Pipes -> Puzzle Signal` flow.
- Reuse existing `SceneTransitionTrigger` and only retarget final transition to `GameOverScene`.
- Track run result as the **sum of completed gameplay scene times** (Tutorial + Puzzle Pipes + Puzzle Signal).
- Persist best (minimum) summed total using `PlayerPrefs`.

## Out of scope

- Puzzle logic changes.
- Input architecture refactor.
- Rewriting existing transition trigger behavior.
- Save slots or advanced profile systems.

## Approved implementation steps

1. Add `PlaytestRunTotal` static runtime store for start/reset, per-scene completion accumulation, and formatted time output.
2. Add `StartSceneController` to initialize total-run tracking and load `Tutorial`.
3. Add `GameOverSceneController` to show completion total, best total, rank, restart, and quit.
4. Keep existing transition script and only extend it to report completed scene time before loading next scene.
5. Rewire `Puzzle Signal` trigger target to `GameOverScene`.
6. Add `StartScene` and `GameOverScene` to build settings in required order.

## Testing checklist

- Start from `StartScene` and click Start.
- Confirm Start log appears and flow loads `Tutorial`.
- Complete Tutorial and confirm transition to `Puzzle Pipes`.
- Complete Puzzle Pipes and confirm transition to `Puzzle Signal`.
- Complete Puzzle Signal and confirm transition to `GameOverScene`.
- Confirm Completion Time, Best Time, and Crew Rank are visible.
- Confirm best time saves and only updates when the new total is lower.
- Confirm Restart returns to `StartScene` and resets current run total.
- Confirm Quit exits play mode in editor and calls quit in build.

## Rollback notes

- Remove `PlaytestRunTotal`, `StartSceneController`, and `GameOverSceneController`.
- Delete `StartScene` and `GameOverScene`.
- Restore `Puzzle Signal` trigger target to previous value.
- Restore `ProjectSettings/EditorBuildSettings.asset` from git.
