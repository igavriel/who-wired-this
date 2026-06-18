---
task: Puzzle Signal result feedback split (operator visual / diagnostic lights)
date: 2026-05-30
status: implemented
related_assets: Assets/Scenes/Game/Puzzle Signal.unity, Assets/WhoWiredThis/Prefabs/Panels/Player1_Signal_Panel.prefab
---

# Puzzle Signal result feedback split

## Scope

Per-panel result groups on **Puzzle Signal.unity**:

| Group | Who sees it | Where it lives | When it updates |
|-------|-------------|----------------|-----------------|
| `ResultVisual_Root` | **Current player** (operator) | Operator's panel | On that player's submit |
| `ResultLight` | **Diagnostic player** (partner) | **Partner's panel** | When the other player submits |

Unlike Puzzle Pipes (visual on partner diagnostic), Signal keeps the wave/freq/gain readout on the operator side. Lights follow the Pipes cross-panel pattern.

## Out of scope

- New prefabs or runtime visibility toggling per turn
- Tutorial scene changes

## Root cause (first fix)

`SplitResultPipesController` drove `ResultLight` on the **operator** panel. In dual-display setup the diagnostic player looks at their own panel — partner-layer objects on the operator side do not read correctly. Both groups appeared on the operator screen.

## Approved implementation steps

1. **Result visual** — `SubmittedCombinationMultiDimensionBridge` on operator panel; `visibleToPlayer` = operator; displays under local `ResultVisual_Root`.
2. **Result lights** — scene `PuzzleSignal_ResultLights` bridges (mirror Pipes):
   - `Bridge_A_to_B_lights`: A's puzzle → **Panel B** `ResultLight-B`, visible to Player B
   - `Bridge_B_to_A_lights`: B's puzzle → **Panel A** `ResultLight-A`, visible to Player A
3. Local `ResultLight` on each panel tagged for that panel's diagnostic player (idle state only until cross-bridge fires).
4. Wire tool: `SignalCalibrationPuzzleSignalResultWireTool.cs`; included in full-scene MCP wire.

## Testing checklist

- ⬜ Player A submits: A sees `ResultVisual_Root-A`; B sees colored lamps on **Panel B**
- ⬜ Player B submits: B sees `ResultVisual_Root-B`; A sees colored lamps on **Panel A**
- ⬜ Operator does not see partner's result lights update on their own submit

## Rollback notes

Revert `SignalCalibrationPuzzleSignalResultWireTool.cs`, re-run previous wire, or git revert Puzzle Signal scene.
