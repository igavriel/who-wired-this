---
name: Pipes MultiDimension Display Bridge
overview: Replace SubmittedCombinationVisualizer wiring in Puzzle Pipes (V1 archive, active scene, and pipes panel prefab) with a thin bridge that drives partner ResultVisual MultiDimension displays via SetSelection on submitted indices.
date: 2026-05-30
status: implemented
---

# Replace SubmittedCombinationVisualizer with MultiDimension display bridge (Pipes)

## Task name

Pipes MultiDimension display bridge

## Date

2026-05-30

## Scope

| Asset | Action |
|-------|--------|
| `Assets/Scenes/Game/OLD/Puzzle Pipes V1.unity` | Remove 2× `SubmittedCombinationVisualizer`; add bridge; wire display `MultiDimension`s |
| `Assets/Scenes/Game/Puzzle Pipes.unity` | Same (2 operator panels) |
| `Assets/WhoWiredThis/Prefabs/Panels/Player1_Pipes_Panel.prefab` | Remove `Combination Visualize Panel-A` + `SubmittedCombinationVisualizer` |
| `PipePressurePuzzlePipesWireTool` | Stop creating primitive `stateVisuals`; wire bridge + display refs |
| `PipePressurePhase4ValidationTool` | Validate bridge + `MultiDimension` active subject instead of visualizer slots |

## Out of scope

- Puzzle Signal scenes/tools (still use `SubmittedCombinationVisualizer` until a follow-up)
- Do **not** delete `SubmittedCombinationVisualizer.cs`

## Approved implementation steps

- ✅ Add `SubmittedCombinationMultiDimensionBridge.cs` (`sourceInput` + `display` slots, `SetSelection` on attempt)
- ✅ Update `Player1_Pipes_Panel.prefab`: remove visualizer object
- ✅ Migrate `Puzzle Pipes.unity` and `OLD/Puzzle Pipes V1.unity` to bridges
- ✅ Refactor wire tool Phase 4 + batch entry point
- ✅ Refactor Phase 4 validation for bridge + `GetCurrentIndexForSolutionCheck()`
- ⚠️ Play Mode + Phase 4 validation menu (requires Unity Editor reload)

## Testing checklist

- ⚠️ Unity compiles; MCP `read_console` zero errors
- ⚠️ **Puzzle Pipes** Play Mode: Player A submits → partner B `ResultVisual_Root` shows A’s valve/press/flow **states**
- ⚠️ Player B submits → A partner readout updates symmetrically
- ⚠️ Panel focus + dimension layers still correct (`visibleToPlayer` on displays)
- ⚠️ Run **Pipe Pressure Phase 4** validation menu on Puzzle Pipes
- ⚠️ Spot-check **OLD/Puzzle Pipes V1** opens without missing-script errors

## Rollback

Re-add `SubmittedCombinationVisualizer` via git revert; re-run legacy wire tool if primitive visuals were deleted.

## Risks

| Risk | Mitigation |
|------|------------|
| Display prefab subject count ≠ operator input count | Editor `OnValidate` warns per slot; clamp at runtime |
| V1 archive diverges from active scene | Same bridge wiring applied to both |
| Signal scenes still depend on visualizer | Out of scope; script kept in repo |
