---
name: Phase 3 Interact Prompts
overview: Route interact prompts from each player's PlayerActions to that player's PlayerHudView (InteractPrompt_A / InteractPrompt_B) via serialized reference, with legacy fallback to HUDController.Instance.
status: implemented
date: 2026-05-16
---

# Phase 3: Per-Player Interact Prompt Routing

## Task name

Dual HUD refactor — per-player interact prompt routing (Phase 3).

## Date

2026-05-16

## Scope

- Per-player interact prompt text in `Split Tutorial_UIRefactor.unity` + `UI_Canvas_DualPlayer_Prototype.prefab`
- `PlayerHudView` + `PlayerActions` script changes only

## Out of scope

Popup/MessagePanel, interactable refactors, scoring, menus, diagnostic/history, action lock, tutorial puzzle logic, production `Split Tutorial.unity` / `UI_Canvas.prefab`.

## Approved implementation steps

1. **PlayerHudView** — Add serialized `interactPromptText`, `SetInteractPrompt(string)`, `ClearInteractPrompt()` (match `HUDController` show/hide behavior).
2. **PlayerActions** — Optional `playerHudView`; route prompts via helper with `HUDController.Instance` fallback; `OnDisable` clears assigned view or singleton.
3. **Prototype prefab** — Wire `PlayerHudView.interactPromptText` on TopBar_A/B to `InteractPrompt_A`/`InteractPrompt_B` TMP; clear `HUDController.interactPromptText`.
4. **Refactor scene** — Scene overrides: `FirstPersonPlayer_A` → PlayerHud A; `FirstPersonPlayer_B` → PlayerHud B.
5. **Validate** — Dual-display prompt tests; legacy scene unchanged; console clean.

## Testing checklist

- [ ] Player A near interactable, B away → Display 0 prompt only
- [ ] Player B near interactable, A away → Display 1 prompt only
- [ ] Both near different interactables → each display shows own prompt
- [ ] Both away → both hidden
- [ ] Panel focus disables `PlayerActions` → that player's prompt clears
- [ ] `Split Tutorial` + `UI_Canvas.prefab` unchanged
- [ ] Popup/MessagePanel unchanged

## Rollback notes

Revert commits touching `PlayerHudView.cs`, `PlayerActions.cs`, `UI_Canvas_DualPlayer_Prototype.prefab`, and `Split Tutorial_UIRefactor.unity`. Production scene/prefab were not modified.

## Implementation summary

- Routing: serialized `PlayerHudView` on `PlayerActions` (scene overrides only).
- Legacy: null `playerHudView` → `HUDController.Instance.SetInteractPrompt`.
- Singleton prompt on prototype detached to prevent Display 0 bleed.
