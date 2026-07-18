---
overview: Fix A-as-diagnostic ResultLights stuck on DimensionB — SubmittedCombinationMultiDimensionBridge was overwriting SplitResultPipesController layers.
date: 2026-07-18
status: implemented
---

# Pipes ResultLights SCBridge layer overwrite

## Task name

Stop legacy `SubmittedCombinationMultiDimensionBridge` from forcing panel-A ResultLights onto Player B’s layer when B is the operator.

## Date

2026-07-18

## Scope

- Identify who sets ResultLight layers on submit when roles are B-operator / A-diagnostic.
- Disconnect SCBridge display refs from ResultLights in `Puzzle Pipes.unity`.
- Prevent Phase 4 / Result Lights wire tools from re-wiring that conflict.

## Out of scope

- Rewriting diagnostic log / classifier.
- Changing camera culling masks.
- Removing SCBridge GameObjects entirely (slots kept; displays nulled).

## Approved implementation steps

1. Reproduce: after `Bridge_B_to_A` Apply → DimA; after SCBridge on B Apply → DimB.
2. Clear ResultLight `display` refs on both panel SCBridges; set B’s SCBridge `visibleToPlayer` to `Player_A`.
3. Update `PuzzlePipesResultLightsWireTool` to clear SCBridge ResultLight displays when wiring.
4. Update `PipePressurePuzzlePipesWireTool.WireResultVisualizerForPanels` to clear instead of assign ResultLights.
5. Play Mode verify both role directions keep partner lamps on the diagnostic viewer’s dimension.

## Testing checklist

- ✅ Reproduce SCBridge overwrite (Split → DimA, SC → DimB)
- ✅ Clear scene SCBridge ResultLight displays + fix B vis tag
- ✅ Play Mode sim: B operator → A lamps stay DimensionA, lampOn
- ✅ Play Mode sim: A operator → B lamps stay DimensionB, lampOn
- ⚠️ Manual Play Mode sign-off on both displays (user)
- ✅ Wire tools updated so Phase 4 / Result Lights menus do not restore SCBridge → ResultLight links

## Rollback notes

- Revert scene `Puzzle Pipes.unity` SCBridge slot display overrides.
- Revert editor tool changes in `PuzzlePipesResultLightsWireTool.cs` and `PipePressurePuzzlePipesWireTool.cs`.
