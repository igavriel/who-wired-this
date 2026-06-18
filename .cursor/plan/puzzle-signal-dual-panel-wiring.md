---
task: Puzzle Signal dual-panel wiring fix
date: 2026-05-30
status: validated
related_assets: Assets/Scenes/Game/Puzzle Signal.unity, Assets/WhoWiredThis/Prefabs/Panels/Player1_Signal_Panel.prefab
---

# Puzzle Signal dual-panel wiring fix

## Scope

Repair [Puzzle Signal.unity](../Assets/Scenes/Game/Puzzle%20Signal.unity) so Player A/B each control their own panel (focus, input cycling, submit) and cross-partner diagnostics match Tutorial/Pipes — mirroring [puzzle-pipes-dual-panel-wiring.md](puzzle-pipes-dual-panel-wiring.md).

## Out of scope

- Puzzle logic / correctIndex changes
- 5-state input prefab swaps (already on `Player1_Signal_Panel`)
- Separate Player2 prefab (keep duplicate-instance pattern)
- Phase 4+ visualizer work from puzzle-signal-v1

## Root causes

1. **Prefab gaps** — `panelActionLock` null on `PanelFocusController`, bridge, `SolveInteractProxy`
2. **Scene** — `TutorialStageManager.panelActionLock` null on both lock bundles; cross-partner `diagnosticDisplay` not wired on adapters/feedback
3. **Editor tools** — Signal wire/validation still target legacy `Player1_Panel` / `Assets/Scenes/Puzzle Signal.unity` (wrong path); no full-scene MCP wire menu

## Approved implementation steps

### 1. Prefab ([Player1_Signal_Panel.prefab](../Assets/WhoWiredThis/Prefabs/Panels/Player1_Signal_Panel.prefab))

- `PanelFocusController`, `MultiDimensionPuzzleInteractableBridge`, `SolveInteractProxy`: `panelActionLock` → root `PanelActionLock`

### 2. Scene ([Puzzle Signal.unity](../Assets/Scenes/Game/Puzzle%20Signal.unity))

- Run full wire on `Player1_Signal_Panel-A` / `Player2_Signal_Panel-B`
- Cross-partner diagnostics: A submits → B reads; B submits → A reads
- Turn-lock colliders from panel focus + submit lever; local bridge `puzzleManager` per panel
- `InitialPanelFocusBootstrap` → both boards

### 3. Editor tools

- Extend [SignalCalibrationPuzzleSignalWireTool.cs](../Assets/WhoWiredThis/Editor/SignalCalibrationPuzzleSignalWireTool.cs): **Wire Puzzle Signal Full Scene** + MCP menu; fix scene path to `Assets/Scenes/Game/Puzzle Signal.unity`
- Extend [SignalCalibrationPhase1ValidationTool.cs](../Assets/WhoWiredThis/Editor/SignalCalibrationPhase1ValidationTool.cs) for signal panel instance names
- Add [SignalCalibrationSignalSubmitValidationTool.cs](../Assets/WhoWiredThis/Editor/SignalCalibrationSignalSubmitValidationTool.cs) (mirror pipes submit validation)

## Testing checklist

- ✅ MCP **Wire Puzzle Signal Full Scene** run; scene saved
- ✅ Validation 0 (Phase 1) + 2 (Submit Lever) pass via MCP
- ⬜ Player A: cycle FREQ/GAIN/WAVE + Send; diagnostic on Player B panel (manual playtest)
- ⬜ Player B: cycle TUNE/AMP/MODE + Send; diagnostic on Player A panel (manual playtest)
- ⬜ Turn-lock blocks Send while waiting; selection still moves (manual playtest)

## Rollback notes

Git revert Puzzle Signal.unity, Player1_Signal_Panel.prefab, and Signal Calibration editor tools.
