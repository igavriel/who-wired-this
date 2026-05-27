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

## Player build crash (Mac + Windows, 2026-05-28)

### Symptom

- Build crashes ~7s after launch when Start loads Tutorial.
- macOS crash: `EXC_BREAKPOINT` in `UnityPlayer.dylib` on a loader thread (not a managed null ref).
- `Player.log` (before native crash): `The file '.../Data/level1' is corrupted!` then `Position out of bounds!`.
- Build index map: `level0` = StartScene, **`level1` = Tutorial**.

### Root cause (confirmed 2026-05-28)

- **Broken prefab-instance serialization** in `Tutorial.unity` (and puzzle scenes): thousands of null entries in `m_Component` arrays, especially under `Room5x5` / `NextLevelCollider`.
- Player build then fails loading `level1` with `CachedReader::OutOfBoundsError` while deserializing `MonoBehaviour` data — not a C# load-guard issue.
- Fix: run **Who Wired This → Playtest → Repair Build Scenes For Player** (reverts `Room5x5` prefab instances + strips null component slots), save scenes, **rebuild** player.

### Earlier hypothesis (partial)

- Stale build artifacts can still cause problems; always delete old `.app` output before rebuilding after scene repair.

### Fix (do this first)

1. Delete the old player output folder/app entirely (e.g. `who-wired-this-2.app`).
2. In Unity: **File → Build Settings** — confirm order: StartScene, Tutorial, Puzzle Pipes, Puzzle Signal, GameOverScene (all enabled).
3. **Build** again (prefer **Development Build** for logs).
4. Re-test and read log:
   - macOS: `~/Library/Logs/Ilan Gavriel/who-wired-this/Player.log`
   - Windows: `%USERPROFILE%\AppData\LocalLow\Ilan Gavriel\who-wired-this\Player.log`
5. Menu: **Who Wired This → Playtest → Validate Build Scenes For Player** (editor) before building.

### Isolation test

- Temporarily set **only Tutorial** in Build Settings (index 0) and build. If it still crashes, the Tutorial scene content or project build pipeline is the problem; if it works, suspect stale multi-scene build artifacts.

### Code hardening added

- `PlaytestSceneLoadUtility` — build-index + `CanStreamedLevelBeLoaded` checks before load.
- `StartSceneController` / `GameOverSceneController` — use utility; `BeginRun`/`ResetRun` **before** `LoadScene` (scene unload would skip code after load).
- `PlaytestBuildSceneValidator` editor menu item.
