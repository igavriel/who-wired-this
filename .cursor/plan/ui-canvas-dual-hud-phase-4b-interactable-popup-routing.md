---
name: Phase 4B Interactable Popup Routing
overview: Route player-triggered interactable popups to the correct per-player PlayerHudView via PlayerHudPopupRouter; tutorial world interactables only; legacy MessagePanel.Instance fallback.
status: validated
date: 2026-05-16
---

# Phase 4B: Per-Player Interactable Popup Routing

## Task name

Dual HUD refactor — route real interactable popup messages to the correct player HUD (Phase 4B).

## Date

2026-05-16

## Scope

- [`PlayerHudPopupRouter.cs`](Assets/WhoWiredThis/Scripts/UI/PlayerHudPopupRouter.cs) — interactor-based routing
- [`PlayerActions.cs`](Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs) — read-only `PlayerHud` accessor only
- [`ClueInteractable`](Assets/WhoWiredThis/Scripts/Interactables/ClueInteractable.cs), [`Collectible`](Assets/WhoWiredThis/Scripts/Interactables/Collectible.cs), [`PuzzleSocket`](Assets/WhoWiredThis/Scripts/Interactables/PuzzleSocket.cs), [`TestButton`](Assets/WhoWiredThis/Scripts/Interactables/TestButton.cs)

## Out of scope

- `EngageButtonController`, `HUDController`, `MessagePanel` singleton behavior, production prefab/scene
- Broadcast popup, menu, scoring, diagnostic/history, tutorial puzzle logic, action lock, registry

## Approved implementation steps

1. Add `PlayerHudPopupRouter.Show(interactor, message)` — resolve `PlayerActions` → `PlayerHud` → `ShowPopup`; else `MessagePanel.Instance`; else warning
2. Add `PlayerActions.PlayerHud` read-only property
3. Migrate four interactables to router (TestButton threads `interactor` into private success/fail)
4. No prefab/scene wiring changes required (Phase 3/4A assignments sufficient)

## Testing checklist

- ✅ Player A clue → Display 0 only
- ✅ Player B clue → Display 1 only
- ✅ Collectible per player
- ✅ PuzzleSocket per player
- ✅ TestButton success/fail per player
- ✅ Independent popups; dismiss one does not close other
- ✅ Phase 3 prompts + Phase 2 shared top bar
- ✅ Manual Play Mode validation (user confirmed 2026-05-16)
- ✅ Console compile clean

## Rollback notes

Revert router script, `PlayerActions` accessor, and four interactable files. No production asset changes.
