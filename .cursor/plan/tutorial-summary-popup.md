---
name: Tutorial Summary Popup
overview: Tutorial-only summary popup on both player HUDs at OnTutorialCompleted, built from TutorialMetricsTracker.GetSnapshot(); scene wiring in Tutorial.unity only.
status: implemented
date: 2026-05-16
---

# Tutorial Summary Popup

## Task name

Tutorial Summary Popup — team metrics summary on dual HUD at tutorial completion.

## Date

2026-05-16

## Scope

- [`TutorialSummaryPopupPresenter.cs`](Assets/WhoWiredThis/Scripts/Tutorial/TutorialSummaryPopupPresenter.cs) — subscribe `OnTutorialCompleted`, one-frame defer, `BuildSummaryText(snapshot)`, dual `PlayerHudView.ShowPopup`
- [`Tutorial.unity`](Assets/Scenes/Tutorial.unity) — component on `TutorialStageManager` GameObject; wire stage manager, metrics tracker, `PlayerHud_A` / `PlayerHud_B`

## Out of scope

- Scoring, high scores, main menu, restart/reset
- Puzzle logic, diagnostics/history, action lock
- `UI_Canvas.prefab`, `Managers.prefab`, `Tutorial Backup.unity`
- Generic `PuzzleSummaryPresenter` framework
- New popup UI or close-button behavior changes

## Approved implementation steps

1. Add `TutorialSummaryPopupPresenter` with isolated `BuildSummaryText` / `FormatMmSs`; tutorial-specific comment for future generalization
2. Subscribe to `TutorialStageManager.OnTutorialCompleted`; defer one frame before `GetSnapshot()`
3. Show same summary on `playerHudViewA` and `playerHudViewB`
4. Scene-only wiring on existing `TutorialStageManager` object

## Testing checklist

- ⬜ Complete tutorial — both players get same summary popup
- ⬜ Values match `TutorialMetricsTracker` debug fields
- ⬜ Close A only — B stays open; reverse
- ⬜ Per-player interact prompt still works after dismiss
- ⬜ Shared TopBar still works
- ⬜ No scoring / high score behavior
- ⬜ Console compile clean

## Rollback notes

Revert `TutorialSummaryPopupPresenter.cs` (+ `.meta`), `Tutorial.unity` component block on `TutorialStageManager`. No prefab changes.
