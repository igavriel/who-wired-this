---
task: SplitResultPipesController
date: 2026-05-30
status: implemented
overview: Per-element partner result lights on Puzzle Pipes (Upper/Middle/Lower = red/orange/green for too-high/too-low/correct), replacing SplitResultTutorialController on pipes prefab.
related_assets: Assets/Scenes/Game/Puzzle Pipes.unity, Player1_Pipes_Panel.prefab, SplitResultPipesController.cs, PuzzlePipesResultLightsWireTool.cs
---

# SplitResultPipesController — Puzzle Pipes result lights

## Scope

- Add `SplitResultPipesController.cs` — three partner lamps driven by per-element diagnostic classification
- Shared `ComponentDiagnosticClassifier` for text + lights
- Remove `Bridge_lights` / `SplitResultTutorialController` from pipes prefab
- Scene bridges under `PuzzlePipes_ResultLights` (cross-opponent wiring)
- Editor wire + Phase 4 validation extension

## Out of scope

- Tutorial scenes and `SplitResultTutorialController`
- Diagnostic text copy, focus order, submit lever, display bridge

## Approved implementation steps

1. ✅ Add `ComponentDiagnosticClassifier` + `SplitResultPipesController.cs`
2. ✅ Refactor `ComponentDiagnosticAdapter` to use classifier
3. ✅ Remove tutorial controller stub from pipes prefab
4. ✅ Add `PuzzlePipesResultLightsWireTool`
5. ✅ Extend Phase 4 validation for result-light bridges
6. ✅ Wire Puzzle Pipes scene + MCP validation (ALL CHECKS PASSED)

## Testing checklist

- ✅ MCP `editor_state` — compiling
- ✅ MCP `read_console` — zero errors
- ✅ Run **Wire Puzzle Pipes Result Lights**; save scene
- ✅ Run **Pipes Result Lights** validation — ALL CHECKS PASSED
- ⚠️ Play Mode: partner lights update on submit only; red/orange/green per slot
- ⚠️ Tutorial still uses `SplitResultTutorialController` only

## Rollback

Revert new scripts, prefab, scene `PuzzlePipes_ResultLights`, and adapter classifier refactor.
