---
task: Puzzle Signal V2 panel wiring migration (OLD → V2 variants)
date: 2026-07-10
status: implemented
related:
  - Assets/Scenes/Game/Puzzle Signal.unity
  - Assets/WhoWiredThis/Prefabs/Panels/Signal_A_V2 Variant.prefab
  - Assets/WhoWiredThis/Prefabs/Panels/Signal_B_V2 Variant.prefab
  - Assets/WhoWiredThis/Editor/SignalCalibrationPuzzleSignalWireTool.cs
  - Assets/WhoWiredThis/Editor/SignalCalibrationPuzzleSignalResultWireTool.cs
  - Assets/WhoWiredThis/Editor/SignalCalibrationPhase1ValidationTool.cs
  - Assets/WhoWiredThis/Editor/PuzzlePipesV2WiringMigrationCore.cs
  - .cursor/plan/puzzle-pipes-v2-panel-wiring-migration.md
---

# Puzzle Signal V2 panel wiring migration

## Task name

Copy all C# script wiring from **`Player1_Signal_Panel-A`** / **`Player2_Signal_Panel-B`** onto **`Signal_A_V2 Variant`** / **`Signal_B_V2 Variant`** in `Puzzle Signal.unity`, enable **dual diagnostic surfaces** (same pattern as Puzzle Pipes), then **remove the OLD panel roots**. **Do not change transforms** on V2 panels.

## Date

2026-07-10

## User decisions (confirmed)

| Topic | Decision |
|-------|----------|
| Dual diagnostics | ✅ Yes — legacy `DiagnosticPanel-A/B` for local rules; `DiagnosticPanel Monitor-*` for partner submit hints (Pipes pattern) |
| Transforms | ✅ Wiring only — leave V2 panel positions/rotation/scale as-is |
| Delete OLD panels | ✅ After migration + validation passes (same as Pipes) |

---

## Scope

- Scene-only migration on `Assets/Scenes/Game/Puzzle Signal.unity`.
- Copy serialized references from OLD panel hierarchy → V2 by **relative child name** (reuse/adapt `PuzzlePipesV2WiringMigrationCore` pattern).
- Remap **scene-level** references still pointing at OLD puzzle managers, focus boards, bridges, and diagnostics.
- Wire **dual diagnostic surfaces** on V2 (rules vs Monitor hints).
- Update Signal editor tools to use V2 panel names.
- Add backup/restore tooling + rollback script (mirror Pipes).
- Delete `Player1_Signal_Panel-A` and `Player2_Signal_Panel-B` after validation.

## Out of scope

- Moving, rotating, or scaling any GameObject / Transform.
- Prefab asset edits unless a scene instance override cannot express the wiring.
- Puzzle logic, `correctIndex` baselines, randomization rules, or history header text changes.
- `simultaneousOperators` behavior change (Signal stays **both players operate at once**).
- Role-swap cutscene (Signal keeps `roleSwapMode: InScene`).
- Art/layout changes on `New Diagnostic`, rack, or ISD controller meshes.

---

## Current state (MCP audit, 2026-07-10)

Both panel pairs exist at scene root. **All gameplay wiring still targets OLD panels.**

| Area | OLD (`Player1_Signal_Panel-A` / `Player2_Signal_Panel-B`) | V2 (`Signal_A_V2 Variant` / `Signal_B_V2 Variant`) |
|------|-------------------------------------------------------------|------------------------------------------------------|
| Scene presence | ✅ active | ✅ active (already placed) |
| `SceneStageManager` puzzle managers | ✅ wired | ❌ |
| `RandomPuzzleSolutionAssigner` | ✅ wired | ❌ |
| `TutorialMetricsTracker` puzzle refs | ✅ wired | ❌ |
| `InitialPanelFocusBootstrap` board cameras | ✅ `Board-A/B` on OLD | ❌ |
| Cross-partner `ComponentDiagnosticAdapter` / `ProcessingFeedbackController` | ✅ partner `DiagnosticPanel-*` on OLD | ❌ / partial defaults |
| `SplitResultPipesController` bridges (`PuzzleSignal_ResultLights`) | ✅ OLD puzzle managers + OLD element refs | ❌ |
| `SignalCalibrationPuzzleSignalResultWireTool` | ✅ targets OLD panel names | ❌ |
| Turn-lock colliders (`SceneStageManager` bundles) | ✅ OLD colliders | ❌ |
| Dual diagnostics (Monitor hints) | N/A (single surface) | ⬜ Monitor exists under `New Diagnostic/` but unwired |

### Hierarchy mapping

Relative paths largely match (same as Pipes migration). V2 adds `New Diagnostic/` and renames legacy diagnostic child.

| OLD root | V2 root | Notes |
|----------|---------|-------|
| `Player1_Signal_Panel-A` | `Signal_A_V2 Variant` | Migrate then delete OLD |
| `Player2_Signal_Panel-B` | `Signal_B_V2 Variant` | Migrate then delete OLD |
| `DiagnosticPanel-A` | `_OLD_DiagnosticPanel-A` | Same role; inactive on V2 prefab |
| `Diagnostic Adapter-A` | `Diagnostic Adapter-A` | ✅ same name |
| `Buttons-A` → `FREQ`/`GAIN`/`WAVE` | same under V2 | ✅ 5-state inputs |
| `Buttons-B` → `TUNE`/`AMP`/`MODE` | same under V2 | ✅ |
| — | `New Diagnostic/DiagnosticPanel Monitor` | **V2 only** — partner hints target |

### Input mapping (unchanged)

**Panel A:** `FREQ`, `GAIN`, `WAVE` — baseline `correctIndex` `[2, 2, 2]`  
**Panel B:** `TUNE`, `AMP`, `MODE` — baseline `[3, 2, 3]`

### Scene-level systems to remap after copy

| System | Property / area |
|--------|-----------------|
| `SceneStageManager` | `playerAPuzzleManager`, `playerBPuzzleManager`, `playerA/BPanelLock`, `playerA/BDiagnosticDisplay` |
| `RandomPuzzleSolutionAssigner` | `playerAPuzzleManager`, `playerBPuzzleManager` |
| `TutorialMetricsTracker` | puzzle manager refs |
| `InitialPanelFocusBootstrap` | `playerA/B.panelCamera`, `diagnosticCamera`, legacy panel refs |
| `PuzzleSignal_ResultLights` bridges | `SplitResultPipesController.puzzleManager`, `elementLights.sourceElement` |
| `SubmittedCombinationMultiDimensionBridge` | local `puzzleManager`, display slots |
| Result visual roots | operator `ResultVisual_Root-*` on own V2 panel |

---

## Gaps to fix (wiring only)

1. ⬜ Bulk copy serialized refs OLD → V2 (`PuzzlePipesV2WiringMigrationCore` + Signal-specific child aliases).
2. ⬜ `SceneStageManager` → V2 `PuzzleManager-A/B` + V2 turn-lock colliders + **legacy** `DiagnosticPanel-A/B` for rule body copy.
3. ⬜ **Dual diagnostics:** partner hints → `DiagnosticPanel Monitor-B` (when A operates) and `Monitor-A` (when B operates); re-wire `ComponentDiagnosticAdapter` + `ProcessingFeedbackController` on V2 roots and child adapters.
4. ⬜ `InitialPanelFocusBootstrap` → V2 `Board-A/B` + diagnostic cameras on legacy panels (or Monitor per focus policy — match Pipes).
5. ⬜ `PuzzleSignal_ResultLights` bridges → V2 puzzle managers + V2 `MultiDimension` source elements.
6. ⬜ `SubmittedCombinationMultiDimensionBridge` display slots on V2 (ResultLight children).
7. ⬜ Re-run full Signal wire menu on V2 (`Wire Puzzle Signal Full Scene` adapted).
8. ⬜ Update editor tool constants: `SignalCalibrationPuzzleSignalWireTool`, `SignalCalibrationPhase1ValidationTool`, `SignalCalibrationPuzzleSignalResultWireTool`, `SignalCalibrationSignalSubmitValidationTool` (if panel-name gated).
9. ⬜ Delete OLD panel roots after validation.

---

## Implementation steps (ordered)

### Phase 0 — Safety + tooling

1. ⬜ **Git:** confirm working tree clean or user-approved dirty state before risky scene edit.
2. ⬜ Add `PuzzleSignalV2WiringMigrationTool.cs` (backup / restore / migrate / delete OLD) — mirror `PuzzlePipesV2WiringMigrationTool`.
3. ⬜ Add `PuzzleSignalV2WiringMigrationCore.cs` OR extend shared core with Signal aliases:
   - `DiagnosticPanel-A` ↔ `_OLD_DiagnosticPanel-A`
   - `DiagnosticPanel-B` ↔ `_OLD_DiagnosticPanel-B`
   - Panel root map: OLD names → V2 names
4. ⬜ Add `scripts/rollback-puzzle-signal-v2-wiring.sh` pointing at backup scene.
5. ⬜ Backup: `Assets/Scenes/Game/_BACKUP_2026-07-10/Puzzle Signal.pre-v2-wiring.unity`

### Phase 1 — Migration

6. ⬜ Run backup, then batch migrate OLD → V2 (no transform copy).
7. ⬜ `RemapSceneStageManagerDiagnostics` → legacy `DiagnosticPanel-A/B` under V2 (not Monitor).
8. ⬜ Add `WireDualDiagnosticSurfacesForSignalPanels()` to `SignalCalibrationPuzzleSignalWireTool` (mirror Pipes):
   - Rules: `SceneStageManager.playerA/BDiagnosticDisplay` → V2 `DiagnosticPanel-A/B`
   - Hints: adapters + feedback → partner `DiagnosticPanel Monitor-*`
9. ⬜ Re-wire `InitialPanelFocusBootstrap`, result bridges, random assigner, metrics tracker to V2 managers.
10. ⬜ Run `MCP/Wire Puzzle Signal Full Scene` + `MCP/Wire Puzzle Signal Result Feedback` on V2 names.

### Phase 2 — Editor tool updates

11. ⬜ Change panel constants to `Signal_A_V2 Variant` / `Signal_B_V2 Variant` in wire + validation + result tools.
12. ⬜ Update `ResetSignalSolveStateForValidation` panel name list.
13. ⬜ Unity compile + `read_console` — 0 errors.

### Phase 3 — Delete OLD + save

14. ⬜ Run Phase 1 validator on V2 — expect PASS.
15. ⬜ Run result feedback wire + submit validation — expect PASS.
16. ⬜ Delete `Player1_Signal_Panel-A` and `Player2_Signal_Panel-B` from scene.
17. ⬜ Re-run validators; save scene.

---

## Test plan

### Automated (editor / MCP)

| Step | Menu / command | Expect |
|------|----------------|--------|
| Compile | Unity MCP `refresh_unity` + `read_console` | ✅ 0 errors |
| Phase 1 structural | `Who Wired This / Signal Calibration / MCP/0. Phase 1 (Puzzle Signal)` | ✅ ALL CHECKS PASSED |
| Result feedback | `Who Wired This / Signal Calibration / MCP/Wire Puzzle Signal Result Feedback` | ✅ 0 issues |
| Submit validation | `Who Wired This / Signal Calibration / Validation/` submit tool (if applicable) | ✅ PASS |
| Full wire smoke | `MCP/Wire Puzzle Signal Full Scene` | ✅ console success |

### Manual Play Mode (`Puzzle Signal.unity`)

- ⬜ Both players can focus their V2 boards (`Board-A` / `Board-B`).
- ⬜ **Simultaneous operators:** both panels interactive at once (glass overlays off).
- ⬜ A adjusts `FREQ`/`GAIN`/`WAVE`; B sees hints on **Monitor-B** (not only legacy panel).
- ⬜ B adjusts `TUNE`/`AMP`/`MODE`; A sees hints on **Monitor-A**.
- ⬜ Submit on either side: partner result lights + operator result visual on correct V2 panel.
- ⬜ Shared history updates on both sides.
- ⬜ Solve both puzzles → completion popup → dismiss → `GameOverScene` chain (if testing full run).
- ⬜ No console errors; no duplicate OLD+V2 panel interaction.

### Negative / regression

- ⬜ `Puzzle Pipes.unity` untouched.
- ⬜ Git diff: `Puzzle Signal.unity` — no accidental transform edits on V2 roots.
- ⬜ Rollback script restores pre-migration scene.

---

## Rollback notes

- Primary: `git checkout` migration commit or run restore menu / `scripts/rollback-puzzle-signal-v2-wiring.sh`.
- Backup path: `Assets/Scenes/Game/_BACKUP_2026-07-10/Puzzle Signal.pre-v2-wiring.unity`.
- Do not delete OLD panels until Phase 1 validator passes on V2.

## Related plans

- [puzzle-pipes-v2-panel-wiring-migration.md](puzzle-pipes-v2-panel-wiring-migration.md) — template for migration core, dual diagnostics, delete-OLD flow ✅
- [puzzle-signal-dual-panel-wiring.md](puzzle-signal-dual-panel-wiring.md) — original OLD-panel wiring (superseded after this migration)
- [puzzle-signal-result-feedback-split.md](puzzle-signal-result-feedback-split.md) — result bridges; must be re-targeted to V2
- [puzzle-signal-v1.md](puzzle-signal-v1.md) — phase history; update status after migration

## Approval

⬜ **Do not implement until user approves this plan** (or says **implement now**).
