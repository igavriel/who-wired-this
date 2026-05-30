---
task: Phase 1 history clear + escape menu
date: 2026-05-30
status: implemented
overview: Clear shared history on scene transitions, cap in-puzzle entries at 20 without renumbering, fix display clipping, and add Escape-to-StartScene via PlaytestFlowUtility.
---

# Phase 1 Tasks 3, 4, 6 — History Reset + Escape Menu

## Scope

- Clear `SharedHistorySO` (including attempt counter reset) on every playtest scene transition, run restart, and Escape abort.
- Trim to latest 20 entries within a puzzle without renumbering attempt numbers.
- Bottom-align history body text so newest rows stay visible.
- Add `PlaytestFlowUtility.TryReturnToMainMenu()` and `PlaytestEscapeHandler` on `UI_Canvas.prefab`.

## Out of scope

- Confirmation dialog on Escape.
- Legacy `HUDController` scene menu routing.
- Diagnostic/history panel layout changes from Phase 2.

## Approved implementation steps

1. ✅ `SharedHistorySO.ClearAllLoaded()` + 20-entry retention cap (no renumber on trim).
2. ✅ `PlaytestSceneLoadUtility.PrepareForSceneLoad()` before every single-scene load.
3. ✅ `SceneTransitionTrigger` calls prepare hook before load.
4. ✅ `HistoryBoardController` bottom-aligns body TMP in `Awake`.
5. ✅ `PlaytestFlowUtility` + `PlaytestEscapeHandler` on `UI_Canvas`.
6. ✅ `GameOverSceneController` delegates restart to shared utility.

## Testing checklist

- ⬜ Start from main menu → enter tutorial.
- ⬜ Submit 25+ attempts → newest rows visible on both history panels.
- ⬜ Transition to next puzzle → history empty, next attempt is #1.
- ⬜ Press Escape mid-puzzle → StartScene, no stale history on restart.
- ⬜ GameOver restart still returns to StartScene.
- ⬜ Unity compile + console clean.

## Rollback notes

Revert `SharedHistorySO`, `PlaytestSceneLoadUtility`, `SceneTransitionTrigger`, `HistoryBoardController`, `PlaytestFlowUtility`, `PlaytestEscapeHandler`, `GameOverSceneController`, and `UI_Canvas.prefab`.
