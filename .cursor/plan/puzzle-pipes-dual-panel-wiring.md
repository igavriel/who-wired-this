---
task: Puzzle Pipes dual-panel wiring fix
date: 2026-05-30
status: validated
related_assets: Assets/Scenes/Game/Puzzle Pipes.unity, Assets/WhoWiredThis/Prefabs/Panels/Player1_Pipes_Panel.prefab
---

# Puzzle Pipes dual-panel wiring fix

## Scope

Repair [Puzzle Pipes.unity](../Assets/Scenes/Game/Puzzle%20Pipes.unity) so Player A/B each control their own panel (focus, input cycling, submit) and cross-partner diagnostics match Tutorial/Signal.

## Out of scope

- Puzzle logic / correctIndex changes
- Room5x5 prefab changes
- Separate Player2 prefab (keep duplicate-instance pattern)

## Root causes

1. **Stale Tutorial prefab modifications** on `Player2_Pipes_Panel B` (wrong guid `69eff0b70…` entries)
2. **Missing Send lever turn-lock colliders** (`actionColliders[3]` null on both sides)
3. **Prefab gaps** — `panelActionLock` null on `PanelFocusController`, bridge, `SolveInteractProxy`; `includeExitInFocusCycle` default 1
4. **Editor tools** partially target legacy `Player1_Panel` / `Buttons/VALVE` paths

## Approved implementation steps

### 1. Prefab ([Player1_Pipes_Panel.prefab](../Assets/WhoWiredThis/Prefabs/Panels/Player1_Pipes_Panel.prefab))

- `PanelFocusController`: `panelActionLock` → root `PanelActionLock`; `includeExitInFocusCycle` = 0
- `MultiDimensionPuzzleInteractableBridge` + `SolveInteractProxy`: `panelActionLock` → same

### 2. Scene ([Puzzle Pipes.unity](../Assets/Scenes/Game/Puzzle%20Pipes.unity))

- Panel B `Board-B`: wire `solveButton.interactableReference` + `boardRenderer` (Pipes guid overrides)
- Remove orphaned Tutorial-guid modification entries from both panel instances
- `TutorialStageManager`: wire `actionColliders[3]` to each Submit Lever collider
- Re-run cross-partner diagnostic wire; verify `SubmittedCombinationMultiDimensionBridge.puzzleManager` is local per panel

### 3. Editor tools

- Extend [PipePressurePuzzlePipesWireTool.cs](../Assets/WhoWiredThis/Editor/PipePressurePuzzlePipesWireTool.cs): `boardRenderer` in `EnsurePanelFocusReady`, turn locks + validation for `Player1_Pipes_Panel A` / `Player2_Pipes_Panel B`
- Menu: **Wire Puzzle Pipes Cross-Partner Diagnostic And Focus** + validation menus

## Testing checklist

- ✅ MCP **Wire Puzzle Pipes Full Scene** run; scene saved
- ✅ Validation 0 (Phase 1) + 2 (Submit Lever) pass via MCP
- ⬜ Player A: cycle inputs + Send; diagnostic on Player B panel (manual playtest)
- ⬜ Player B: cycle inputs + Send; diagnostic on Player A panel (manual playtest)
- ⬜ Turn-lock blocks Send while waiting; selection still moves (manual playtest)
- ⚠️ Console warnings: `Submit-Source-A Display-B` 4-state vs 3-state display mismatch (pre-existing bridge config)

## Rollback notes

Git revert Puzzle Pipes.unity, Player1_Pipes_Panel.prefab, and editor wire tools.
