# Puzzle Pipes submit lever, exit focus, and popup action

**Date:** 2026-05-30  
**Status:** implemented

## Scope

- Wire `Submit_Lever_2State-A` as real solve control with ON / timed OFF / latched ON feedback
- Remove Exit from panel focus cycle in Puzzle Pipes scene only
- Allow Action key to dismiss completion HUD popup while in panel focus

## Out of scope

- Tutorial / Signal scene Exit focus behavior
- Changing input button cyclers (ValveV1, Fader, ValveV2)

## Approved implementation steps

1. Add `SubmitLeverMultiDimensionFeedback` and hook in `MultiDimensionPuzzleInteractableBridge`
2. Add `SolveInteractProxy` + lever feedback on `Player1_Pipes_Panel.prefab`
3. Add `includeExitInFocusCycle` on `PanelFocusController`; scene override `false` on both pipes panels
4. Dismiss HUD popup on Action in `PlayerPanelFocusController` while focused
5. Editor validation menu + MCP menu for submit/focus wiring

## Testing checklist

- [ ] Play Puzzle Pipes: focus cycle is inputs → Solve only (no Exit slot)
- [ ] Submit wrong combo: lever ON → processing → OFF after ~1s
- [ ] Submit correct combo: lever stays ON
- [ ] Both sides complete: Action dismisses summary popup while focused
- [ ] Tutorial scene: Exit still in focus cycle

## Rollback notes

Revert prefab [`Player1_Pipes_Panel.prefab`](../Assets/WhoWiredThis/Prefabs/Panels/Player1_Pipes_Panel.prefab), scene [`Puzzle Pipes.unity`](../Assets/Scenes/Game/Puzzle%20Pipes.unity), and scripts under `PanelFocus/` and `Visibility/`.
