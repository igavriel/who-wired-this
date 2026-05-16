---
name: Phase 4A Popup Foundation
overview: Per-player MessagePanel foundation in dual HUD prototype — multi-instance-safe MessagePanel, PlayerHudView popup API, interact-key dismiss, PerPlayer prefab variant, dev test harness.
status: implemented
date: 2026-05-16
---

# Phase 4A: Per-Player MessagePanel Foundation

## Task name

Dual HUD refactor — per-player popup foundation (Phase 4A).

## Date

2026-05-16

## Scope

- [`MessagePanel.cs`](Assets/WhoWiredThis/Scripts/UI/MessagePanel.cs) — `registerAsSingleton` flag; skip global keyboard when false
- [`PlayerHudView.cs`](Assets/WhoWiredThis/Scripts/UI/PlayerHudView.cs) — `messagePanel`, `ShowPopup` / `HidePopup` / `IsPopupOpen`
- [`PlayerActions.cs`](Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs) — dismiss own popup on interact key; skip interactable that frame
- New [`UI_PopupMessagePanel_PerPlayer.prefab`](Assets/WhoWiredThis/Prefabs/Game/UI_PopupMessagePanel_PerPlayer.prefab) (duplicate of base; base unchanged)
- [`UI_Canvas_DualPlayer_Prototype.prefab`](Assets/WhoWiredThis/Prefabs/Game/UI_Canvas_DualPlayer_Prototype.prefab) — PerPlayer nested popups; root singleton MessagePanel removed; test harness
- Dev-only [`PlayerHudPopupTestHarness.cs`](Assets/WhoWiredThis/Scripts/UI/PlayerHudPopupTestHarness.cs) on prototype root

## Out of scope (Phase 4B+)

- `PlayerHudMessageRouter`, interactable caller migration, `HUDController` help/menu routing, broadcast API

## Approved implementation steps

1. `MessagePanel`: `registerAsSingleton` (default `true`); per-player instances use `false`; no global `Update` input when false; `OnDestroy` clears singleton
2. `PlayerHudView`: serialized `MessagePanel`; auto-cache from children; popup API with null warnings
3. `PlayerActions`: if `playerHudView.IsPopupOpen` and interact pressed → `HidePopup()` and return (no interactable)
4. Create `UI_PopupMessagePanel_PerPlayer.prefab` — do not modify `UI_PopupMessagePanel.prefab` (only used by dual prototype)
5. Dual prototype: swap nested popup source to PerPlayer prefab; remove root `MessagePanel`; add `PlayerHudPopupTestHarness`
6. Test: F9 / F10 in Play Mode; Context Menu on harness

## Testing checklist

- [ ] F9 → Player A popup on Display 0 only
- [ ] F10 → Player B popup on Display 1 only
- [ ] Both open simultaneously; independent content
- [ ] Player A interact (Keypad Enter) closes A only
- [ ] Player B interact (Keypad .) closes B only
- [ ] Phase 3 interact prompts still per-player
- [ ] Phase 2 shared top bar still works
- [ ] `UI_Canvas.prefab` and `Split Tutorial.unity` unchanged by this phase
- [ ] Console compile clean

## Rollback notes

Revert commits touching MessagePanel, PlayerHudView, PlayerActions, PerPlayer prefab, dual prototype prefab, test harness. Production canvas/scene untouched.

## Prefab safety note

`UI_PopupMessagePanel.prefab` is referenced only by `UI_Canvas_DualPlayer_Prototype`. Production `UI_Canvas` uses an inline hierarchy. Per-player variant avoids changing the shared visual prefab used elsewhere.
