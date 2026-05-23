---
task: Puzzle Signal v1 — signal calibration puzzle
date: 2026-05-23
status: in_progress
scene: Assets/Scenes/Puzzle Signal.unity
overview: Convert Puzzle Signal from a Pipes clone into 3×5-state signal puzzle (FREQ/GAIN/WAVE vs TUNE/AMP/MODE) using existing systems, scene-only wiring, minimal visualizer, and Signal Calibration editor tools.
related_assets: Assets/Scenes/Puzzle Signal.unity, Assets/Scenes/Puzzle Pipes.unity, MultiDimension_*_5State prefabs, ComponentDiagnosticAdapter, SubmittedCombinationVisualizer, RandomPuzzleSolutionAssigner
---

# Puzzle Signal v1 — Implementation Plan

## Current codebase snapshot (2026-05-23)

**Phases 1–2 complete (2026-05-23).** `Puzzle Signal.unity` diverges from `Puzzle Pipes.unity`. Six **5-state** inputs with signal labels and signal `ComponentDiagnosticAdapter` copy. 4-slot visualizers remain for Phase 4.

| Area | Planned (Signal) | Actual in repo |
|------|------------------|----------------|
| Scene path | `Assets/Scenes/Puzzle Signal.unity` | ✅ |
| Divergence from Pipes | Signal-only edits | ✅ Scenes differ; Pipes unchanged |
| Input count / states | 3 × **5** per side | ✅ `Knob_5State`, `Slider_5State`, `ButtonText_5State` instances |
| Blue labels | FREQ, GAIN, WAVE | ✅ |
| Red labels | TUNE, AMP, MODE | ✅ |
| Diagnostics | Signal copy on `ComponentDiagnosticAdapter` | ✅ Phase 2 (2026-05-23) |
| Visualizer | 5 `stateVisuals` per slot | ❌ 4 visuals per slot (Phase 4; runtime warns until fixed) |
| Randomizer | `enableRandomization`, indices 0..4 | ✅ Assigner wired; Phase 3 validator (2026-05-23) |
| History | 3 tokens via `inputOrder` | ✅ Rebound to FREQ/GAIN/WAVE and TUNE/AMP/MODE |
| Signal Calibration editor | Wire + Phase 1 validate | ✅ `SignalCalibrationPuzzleSignalWireTool`, `SignalCalibrationPhase1ValidationTool` |
| Build settings | — | Puzzle Signal / Puzzle Pipes **not** in `EditorBuildSettings` |

**Reuse already on the duplicate scene (from Pipes):** `MultiDimensionPuzzleManager`, `MultiDimensionHistoryAdapter`, `ComponentDiagnosticAdapter`, `SubmittedCombinationVisualizer`, `RandomPuzzleSolutionAssigner`, `ProcessingFeedbackController`, disabled legacy `MultiDimensionDiagnosticAdapter`, turn-lock via `TutorialStageManager`. Reuse these components when implementing Signal; change **serialized copy, prefab instances, indices, and visual slot counts** only.

**Type names:** Runtime manager is `MultiDimensionPuzzleManager` (`WhoWiredThis.Visibility`), not `MultiDimensionPuzzelManager`.

---

## Scope

- **Scene only:** [Assets/Scenes/Puzzle Signal.unity](Assets/Scenes/Puzzle Signal.unity)
- 3 inputs × 5 states per side (125 combinations per player)
- Reuse: `MultiDimensionPuzzleManager`, `ComponentDiagnosticAdapter`, `RandomPuzzleSolutionAssigner`, `SubmittedCombinationVisualizer`, history, turn flow (same stack as [pipe-pressure-puzzle-puzzel-pipes.md](pipe-pressure-puzzle-puzzel-pipes.md))
- Swap prefab instances to **`MultiDimension_Knob_5State`**, **`MultiDimension_Slider_5State`**, **`MultiDimension_ButtonText_5State`** (already authored)
- Minimal signal visualizer (5 passive states per slot)
- New editor menus: **Who Wired This / Signal Calibration** (wire + validation; mirror Pipe Pressure MCP/console pattern from [editor-validation-console-output.md](editor-validation-console-output.md))

## Out of scope

- Simultaneous mode, main menu, scoring
- Changes to Puzzle Pipes, Tutorial, global input/HUD/Diagnostic/History prefabs
- Runtime system refactors (unless blocked — ask first)
- Visual polish / animation beyond minimal readout
- Converting **Puzzle Pipes** to 5-state (stays 4-state per pipe-pressure plan)

## Critical safety

- **Only** edit `Puzzle Signal.unity` for scene work; leave `Puzzle Pipes.unity` unchanged.
- Before editing Signal, confirm `diff Puzzle\ Signal.unity Puzzle\ Pipes.unity` is empty (or document intentional drift after each phase).
- Git is rollback: `git checkout -- "Assets/Scenes/Puzzle Signal.unity"` plus any new `SignalCalibration*.cs` editor files.

## Player inputs (target)

| Side | Input | Prefab | States | Diagnostic |
|------|-------|--------|--------|------------|
| Blue | FREQ | Knob_5State | MIN…MAX | Ordered |
| Blue | GAIN | Slider_5State | MIN…MAX | Ordered |
| Blue | WAVE | ButtonText_5State | FLAT,SINE,PULS,TRNG,NOIS | Categorical |
| Red | TUNE | Knob_5State | MIN…MAX | Ordered |
| Red | AMP | Slider_5State | MIN…MAX | Ordered |
| Red | MODE | ButtonText_5State | FLAT,SINE,PULS,TRNG,NOIS | Categorical |

5-state vocabulary on prefabs: see [multidimension-5state-prefab-variant-chain.md](multidimension-5state-prefab-variant-chain.md).

## Diagnostic copy (scene-only, target)

**System:** `SIGNAL LINK CALIBRATED.` / `SIGNAL IS UNSTABLE.` / `ONE SIGNAL CHANNEL RESPONDS.` / `SIGNAL IS CLOSE.` / `TELL YOUR PARTNER WHAT YOU LEARNED.`

**Components:** FREQ/TUNE/GAIN/AMP stable + too low/high; WAVE/MODE pattern matches / does not match.

(Signal copy on `Player1_Panel` / `Player2_Panel` `ComponentDiagnosticAdapter` — Phase 2.)

## Randomization

- `RandomPuzzleSolutionAssigner`: `enableRandomization = true`
- Indices **0..4** per slot after 5-state swap; seed/debug Inspector-only
- Re-run / extend Phase 5 validation pattern for Signal (today’s `PipePressurePhase5ValidationTool` is **Puzzle Pipes–only**)

## Validation menus (to implement)

Mirror Pipe Pressure layout under **Who Wired This / Signal Calibration/**:

- Wire Puzzle Signal
- Validate Phase 1 (labels, 5 subjects, `inputOrder`, turn locks)
- Validate Randomized Solution (0..4)
- Validate Result Visualizer (5 visuals per slot)

Use `EditorValidationConsoleReporter` for MCP-friendly default paths (same as Pipe Pressure).

## Approved phases

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Git + plan archive | ✅ Plan archived; editor tools added; user should commit before risky follow-up |
| 1 | Scene reconfiguration (3×5 prefabs, labels, `puzzleElements` / focus bindings) | ✅ Wired + MCP validation passed |
| 2 | Signal diagnostics (`ComponentDiagnosticAdapter` copy) | ✅ Wired + Phase 2 validator |
| 3 | Randomizer 0..4 + Signal validation tools | ✅ `SignalCalibrationPhase3ValidationTool` + wire menu |
| 4 | Minimal signal visualizer (5 `stateVisuals` per slot) | ⬜ Not started |
| 5 | Final QA + docs | ⬜ Not started |

## Testing checklist

- ✅ 6 inputs × 5 states; labels FREQ/GAIN/WAVE and TUNE/AMP/MODE (edit-mode structural — Signal Phase 1 validator)
- ✅ History: 3 tokens per attempt (`inputOrder` rebound)
- ✅ Randomizer 0..4; generator + apply verified (Signal Phase 3 validator)
- ✅ Signal diagnostic copy (not pipe)
- ⬜ Visualizer: 5 `stateVisuals` per slot
- ⚠️ Turn flow works (manual Play Mode) — inherited from Pipes clone; re-test after Signal wiring
- ✅ Puzzle Pipes + Tutorial unchanged on disk (Signal must not modify Pipes)
- ✅ No compile errors (baseline); re-check after editor tools added

## Rollback

```bash
git checkout -- "Assets/Scenes/Puzzle Signal.unity" \
  Assets/WhoWiredThis/Editor/SignalCalibration*.cs
```

Cross-reference: [puzzel-pipes-randomized-solution-phase5.md](puzzel-pipes-randomized-solution-phase5.md), [multidimension-5state-prefab-variant-chain.md](multidimension-5state-prefab-variant-chain.md), [pipe-pressure-puzzle-puzzel-pipes.md](pipe-pressure-puzzle-puzzel-pipes.md)
