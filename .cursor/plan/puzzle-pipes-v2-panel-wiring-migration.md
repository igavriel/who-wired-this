---
task: Puzzle Pipes V2 panel wiring migration (OLD → V2 variants)
date: 2026-07-09
status: validated
related:
  - Assets/Scenes/Game/Puzzle Pipes.unity
  - Assets/WhoWiredThis/Prefabs/Panels/Pipes_A V2 Variant.prefab
  - Assets/WhoWiredThis/Prefabs/Panels/Pipes_B V2 Variant.prefab
  - Assets/WhoWiredThis/Editor/PipePressurePuzzlePipesWireTool.cs
  - Assets/WhoWiredThis/Editor/PuzzlePipesV2WiringMigrationTool.cs
---

# Puzzle Pipes V2 panel wiring migration

## Task name

Copy all C# script wiring from inactive `_OLD_Player1_Pipes_Panel A` / `_OLD_Player2_Pipes_Panel B` onto active `Pipes_A V2 Variant` / `Pipes_B V2 Variant` in `Puzzle Pipes.unity`. **Do not change transforms** (position / rotation / scale).

## Date

2026-07-09

## Scope

- Scene-only migration on `Assets/Scenes/Game/Puzzle Pipes.unity`.
- Rewire serialized references on gameplay/UI scripts under the two V2 panel roots.
- Update scene-level references that still implicitly depended on OLD panel object paths (if any remain broken after panel-root migration).
- Add editor backup/restore tooling before migration runs.
- Update `PipePressurePuzzlePipesWireTool` panel name constants to V2 scene object names (follow-up, same PR).

## Out of scope

- Moving, rotating, or scaling any GameObject / Transform.
- Prefab asset edits (`Pipes_A/B V2 Variant.prefab`) unless a scene instance override cannot be expressed in the scene alone.
- Puzzle logic, `correctIndex`, randomization rules, or history header text changes.
- Art/layout changes on `New Diagnostic`, `Rack Variant`, or table geometry.
- Deleting `_OLD_*` panels unless user approves (see conflicts).

---

## Current state (MCP audit, 2026-07-09)

Both panel pairs share nearly the same child names. V2 adds `New Diagnostic/` (with `DiagnosticPanel Monitor` + `Rack Variant`).

| Area | Status |
|------|--------|
| `TutorialStageManager` puzzle managers | ✅ Already `PuzzleManager-A` / `PuzzleManager-B` on V2 |
| `InitialPanelFocusBootstrap` | ✅ Already `Board-A` / `Board-B` on V2 + diagnostic cameras |
| `RandomPuzzleSolutionAssigner` | ✅ Already V2 puzzle managers |
| `SplitResultPipesController` bridges | ✅ Already V2 puzzle managers + V2 source elements |
| `TutorialStageManager` turn-lock colliders | ✅ Already V2 `panelActionLock`, glass, overlay text |
| `PanelFocusController` on V2 boards | ✅ Wired to V2 inputs + solve lever + action lock |
| Cross-partner diagnostics | ❌ Broken / partial (see gaps) |
| `SubmittedCombinationMultiDimensionBridge` display slots | ❌ V2 slots have `display: null` (OLD had ResultLight-*) |
| Child `ComponentDiagnosticAdapter` components | ⚠️ Partially wired on V2 children; partner display wrong |
| Root `ComponentDiagnosticAdapter` | ❌ Wrong or empty vs OLD |
| `ProcessingFeedbackController` partner display | ❌ V2 A/B null or wrong |
| `TutorialStageManager` diagnostic body targets | ⚠️ Point to `DiagnosticPanel Monitor` (not OLD `DiagnosticPanel-A/B`) |

### Hierarchy mapping (relative paths match)

| OLD root | V2 root | Notes |
|----------|---------|-------|
| `_OLD_Player1_Pipes_Panel A` | `Pipes_A V2 Variant` | V2 active |
| `_OLD_Player2_Pipes_Panel B` | `Pipes_B V2 Variant` | V2 active |
| `Diagnostic Adapter-A` | `_OLD_DiagnosticPanel-A` | Same role, renamed child |
| `Diagnostic Adapter-B` | `_OLD_Diagnostic Adapter-B` | Same role, renamed child |
| `Buttons-A` / `Board-A` / etc. | same names under V2 | ✅ |
| — | `New Diagnostic/` | **V2 only** — conflict surface |

### Input name mapping

**Panel A (both OLD and V2):** `PRESS`, `FLOW`, `VALVE`

**Panel B (both OLD and V2):** `ValveV1_4State-B`, `Fader_4State-B`, `ValveV2_4State-B`  
(not `GATE`/`PUMP`/`ROUTE` — those are legacy `Player2_Panel` names in the wire tool)

---

## Gaps to fix (wiring only)

### Per panel root (`Pipes_A V2 Variant` / `Pipes_B V2 Variant`)

| Component | Copy from OLD | Target on V2 |
|-----------|---------------|--------------|
| `ProcessingFeedbackController.diagnosticDisplay` | partner `DiagnosticPanel-B/A` | partner panel equivalent (**user choice: Monitor vs legacy panel**) |
| `ComponentDiagnosticAdapter` (root) | partner display ref + copy strings | same pattern as OLD (components stay empty on root) |
| `MultiDimensionHistoryAdapter` | verify puzzleManager, historyBoard, inputOrder | map OLD inputs → V2 inputs by **child name** |
| `PanelActionLock` | already on V2 root | ensure all children reference **V2** lock (not OLD fileID) |

### Child diagnostic adapter

| Panel | OLD child | V2 child |
|-------|-----------|----------|
| A | `Diagnostic Adapter-A` | `_OLD_DiagnosticPanel-A` |
| B | `Diagnostic Adapter-B` | `_OLD_Diagnostic Adapter-B` |

Copy from OLD child adapter → V2 child adapter:

- `puzzleManager` → local V2 `PuzzleManager-A/B`
- `diagnosticDisplay` → **partner** diagnostic surface (per user choice)
- `components[]` → remap each `input` to same-named child under V2 operator panel
- All diagnostic copy strings / enums / `eligibleForHints`

### `SubmittedCombinationMultiDimensionBridge` (`Submit-Source-A Display-B`)

Copy from OLD → V2:

- `puzzleManager` → local V2 manager
- `visibleToPlayer` → same enum value
- Each slot: `sourceInput` by name, `display` → partner `ResultLight-Upper/Middle/Lower` on V2 partner panel

### Partner visibility

On partner `DiagnosticDisplayController` parent (`MultiDimensionRecursive`):

- A's partner display visible to `Player_B`
- B's partner display visible to `Player_A`

(Already correct on `DiagnosticPanel-A/B`; re-apply if target surface changes.)

### Scene-level (`TutorialStageManager`)

Re-verify after panel migration:

- `playerADiagnosticDisplay` / `playerBDiagnosticDisplay` → chosen diagnostic surface per side
- `playerAPanelLock` / `playerBPanelLock` → still V2 colliders + glass (likely no change)

### Explicitly **not** touched

- Any `Transform`, `RectTransform`, `MeshFilter` mesh refs used only for rendering
- `_OLD_*` panel transforms
- Room / environment / lighting

---

## Approved implementation steps

### Phase 0 — Safety (before any wiring)

1. ⬜ Add `PuzzlePipesV2WiringMigrationTool.cs` with:
   - **Backup Scene** → copies `Puzzle Pipes.unity` to `Assets/Scenes/Game/_BACKUP_2026-07-09/Puzzle Pipes.pre-v2-wiring.unity`
   - **Restore From Backup** → restores scene from that backup file
2. ⬜ Add shell rollback helper: `scripts/rollback-puzzle-pipes-v2-wiring.sh` (`git checkout --` on scene)
3. ⬜ User confirms git working tree is safe / committed

### Phase 1 — Editor migration tool

4. ⬜ Implement `MigrateOldPanelsToV2()` in `PuzzlePipesV2WiringMigrationTool`:
   - Resolve panels by scene object name (not `GameObject.Find` on inactive if fragile — use `FindObjectsByType` + name filter or serialized scene scan)
   - Build **name-based maps** under each panel root (`Transform` walk by relative path / child name)
   - For each wiring field: read OLD `SerializedProperty`, resolve equivalent V2 object by mapped child name, write to V2
   - **Skip** any property on `Transform` / `RectTransform`
   - Log every remapped reference; warn on unmapped OLD refs
5. ⬜ Re-run existing cross-partner wire helpers from `PipePressurePuzzlePipesWireTool` **after** copy, using updated panel names:
   - `Pipes_A V2 Variant` / `Pipes_B V2 Variant`
   - Panel B input names: `ValveV1_4State-B`, `Fader_4State-B`, `ValveV2_4State-B`

### Phase 2 — Scene validation

6. ⬜ Unity compile + `read_console` (no new errors)
7. ⬜ Run existing MCP validators:
   - `WhoWiredThis/Pipe Pressure/MCP/Wire Puzzle Pipes Full Scene` (or validation-only pass)
   - Phase 1 + Submit lever validation menus
8. ⬜ Manual Play Mode checklist (below)

### Phase 3 — Cleanup (only if user approves)

9. ⬜ Optionally delete or keep inactive `_OLD_Player1/2` panels (per user choice)

---

## Conflicts (resolved)

| # | Conflict | Decision |
|---|----------|----------|
| 1 | Diagnostic surface | **DiagnosticPanel-A / DiagnosticPanel-B** (legacy panels; match OLD) |
| 2 | OLD panels after migration | **Delete** `_OLD_Player1/2` panels after validation passes |

---

## Conflicts (need user decision)

_Resolved — see above._

---

## Rollback

### Automatic (editor)

- Menu: **Who Wired This / Pipe Pressure / Restore Puzzle Pipes Pre-V2 Wiring Backup**
- Restores from `Assets/Scenes/Game/_BACKUP_2026-07-09/Puzzle Pipes.pre-v2-wiring.unity`

### Git (shell)

```bash
./scripts/rollback-puzzle-pipes-v2-wiring.sh
# or
git checkout -- "Assets/Scenes/Game/Puzzle Pipes.unity"
```

### Git (full revert of migration commit)

Revert the migration commit if editor tool + scene were committed together.

---

## Testing checklist

- ⬜ Backup file exists before migration runs
- ⬜ Restore backup returns scene to pre-migration state
- ⬜ No Transform changes in diff (only MonoBehaviour serialized fields / scene refs)
- ⬜ Player A: panel focus, cycle `PRESS`/`FLOW`/`VALVE`, Send works
- ⬜ Player B sees diagnostic feedback on chosen surface
- ⬜ Player B: cycle `ValveV1_4State-B` / `Fader_4State-B` / `ValveV2_4State-B`, Send works
- ⬜ Player A sees partner diagnostic feedback
- ⬜ Result lights on partner panel update on submit
- ⬜ Turn-lock blocks inputs while partner operates; glass overlay shows
- ⬜ `TutorialStageManager` stage body copy appears on correct display
- ⬜ Completion popup + cutscene transition still work
- ⬜ Console: no new missing-reference warnings from migration

---

## Files likely touched

| File | Change |
|------|--------|
| `Assets/Scenes/Game/Puzzle Pipes.unity` | Rewired V2 panel + scene refs |
| `Assets/WhoWiredThis/Editor/PuzzlePipesV2WiringMigrationTool.cs` | **New** — backup, restore, migrate |
| `scripts/rollback-puzzle-pipes-v2-wiring.sh` | **New** — git rollback helper |
| `Assets/WhoWiredThis/Editor/PipePressurePuzzlePipesWireTool.cs` | Update panel name constants to V2 |
| `.cursor/plan/README.md` | Index row |
