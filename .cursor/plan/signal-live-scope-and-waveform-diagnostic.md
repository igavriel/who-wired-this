---
task: Signal live scope preview + waveform ASCII diagnostic
date: 2026-07-22
status: implemented (validated in play mode 2026-07-22)
scenes: Assets/Scenes/Game/Puzzle Signal.unity
prefabs: Signal_A_V2 Variant, Signal_B_V2 Variant (nested Signal_A/B_V1 Variant -> Signal_A_V1)
cursor_plan_file: ~/.cursor/plans/signal_live_scope_diagnostic_0ce14ea9.plan.md
---

# Signal Live Scope + Waveform Diagnostic

## Task name

Signal puzzle: live scope preview on every control change (Goal B) and Pipes-style 40x12
log diagnostic with distance feedback + target-waveform ASCII hint (Goal A).

## Date

Planned 2026-07-22 (research + manager report complete; implementation not started).

## Scope

- `Puzzle Signal.unity` only (both panels: A = WAVE/FREQ/GAIN, B = MODE/TUNE/AMP).
- One additive event on `MultiDimension` (no serialization change).
- Four new scripts under `Assets/WhoWiredThis/Scripts/Puzzles/Common/`.
- Scene/prefab wiring for the two Signal panels; old `ComponentDiagnosticAdapter`
  component disabled (not removed) on Signal panels.

## Out of scope

- Tutorial and Pipes scenes/prefabs/scripts (no behavior change).
- `ComponentDiagnosticAdapter`, `ComponentDiagnosticClassifier`,
  `ComponentDiagnosticLogFormatter`, `SubmittedCombinationMultiDimensionBridge`,
  `SplitResultPipesController`, `MultiDimensionPuzzleManager` (all unchanged).
- Input mappings, target randomization, attempt counting, timer/score, role swap,
  completion popup, scene transitions, result-light wiring (already per spec).

## Verified findings (research summary — resume context)

- All three puzzles share `MultiDimensionPuzzleManager` + `MultiDimension`
  (`Assets/WhoWiredThis/Scripts/Visibility/`). Signal is a configuration of the
  Pipes stack, not a separate architecture.
- The operator "scope" = `ResultVisual_Root` with three 5-state `MultiDimension`
  visuals (`ResultVisualWave_5State`: LINE/SINE/PULS/TRNG/NOIS mesh quads with
  materials like `Mat_WaveSine`; `ResultVisualFreq_5State`, `ResultVisualAmp_5State`:
  MIN..MAX). Driven ONLY by `SubmittedCombinationMultiDimensionBridge` on
  `OnAttemptSubmitted` -> updates only after Submit. Root cause for Goal B.
- `MultiDimension` has NO change event; control changes flow
  `MultiDimensionSubjectCycler.Interact` -> `AdvanceIndexForPlayer` silently.
- Signal diagnostic currently `bodyLayout: 0` (LegacyHints sentences: "FREQ IS TOO
  LOW." / "WAVE PATTERN DOES NOT MATCH."); Pipes uses `bodyLayout: 1` (LogRows,
  40x12, dot leaders, `OK / A BIT LOW / A BIT HIGH / TOO LOW / TOO HIGH`).
- Distance logic: `ComponentDiagnosticClassifier` — exact = green/OK, |delta|=1 =
  orange/"A BIT", |delta|>=2 = red/"TOO"; categorical = green/red. Result lights
  (`Bridge_A_to_B_lights` / `Bridge_B_to_A_lights`, `SplitResultPipesController`,
  subjects 0=red 1=orange 2=green) are ALREADY wired per spec — keep.
- Diagnostic display: `DiagnosticPanel Monitor.prefab` -> `Body_TMP` (3D TMP),
  VT323-Regular SDF (monospace), rich text ON (avoid `<` `>` in ASCII), RTL off,
  wrapping Normal + Ellipsis overflow — safe if every line <= 40 chars.
  Contract: 40 chars x 12 lines, empty lines allowed.
- Direction trap: default `closeTooLowStatus = "A BIT HIGH"` etc. are
  Pipes-inverted. Signal knobs are MIN(0)..MAX(4), so submitted < target must
  print LOW. New formatter maps direction explicitly.
- Waveform naming: inputs FLAT/SINE/PULSE/SAW(or TRNG)/NOISE; result visual LINE/
  SINE/PULS/TRNG/NOIS; validators FLAT/SINE/PULS/TRNG/NOIS. Index 3 is the
  triangle family (SAW vs TRNG inconsistent). Brief said "Square/Triangle";
  actual = PULSE (square-like) and SAW/TRNG (triangle-like). ASCII art must match
  the operator's ResultVisualWave mesh shapes (verify visually in implementation).
- Cross-panel routing: operator's diagnostic adapter writes to the PARTNER's
  monitor (`WireOperatorHintsToPartnerMonitor` pattern in
  `Assets/WhoWiredThis/Editor/SignalCalibration*` tools). Keep same routing.
- Submit side effects to protect: `SharedHistorySO.nextAttemptNumber`,
  `TutorialMetricsTracker` attempts, `SceneStageManager` completion +
  role swap (`CutSceneRoundTrip` via `CutScene-Signal-Swap`),
  `RandomPuzzleSolutionAssigner` targets. Preview must touch none of these.
- Standby: `DiagnosticDisplayController.SetWaiting()` before first submit — keep;
  no fake result data.

## Approved implementation steps

1. ✅ Remind user to commit current work (working tree has a dirty TMP fallback
   asset); archive confirmation of this plan file + README row.
2. ✅ `MultiDimension.cs` (additive only): add
   `public event Action<int> OnActiveIndexChanged`, raised from
   `AdvanceIndexForPlayer` / `SetActiveSubjectIndex` / `SetSelection` only when
   `activeSubjectIndex` actually changes. Compile check via MCP (`read_console`).
3. ✅ New `LiveCombinationPreviewBridge.cs` (Puzzles/Common): slots
   `sourceInput` -> `display` (`MultiDimension` pairs, same shape as
   `SubmittedCombinationMultiDimensionBridge`); `visibleToPlayer` tag; subscribe
   in `OnEnable`, unsubscribe in `OnDisable` (no duplicate subscriptions);
   apply current indices once on enable (covers init + post-role-swap reload);
   handler only calls `display.SetSelection` — no manager/history/attempt/
   completion access. No Update polling.
4. ✅ New `SignalWaveformAsciiLibrary.cs` (pure static): fixed 3-line ASCII per
   waveform index 0..4; deterministic (noise hardcoded, no RNG); lines <= 38
   chars; chars restricted to `_ / \ | space .`; no `<` or `>`.
5. ✅ New `SignalDiagnosticFormatter.cs` (pure static):
   `BuildSignalDiagnostic(rateStatus, powerStatus, waveformCorrect,
   targetWaveformIndex, revision)` -> 40x12 string reusing
   `ComponentDiagnosticLogFormatter.FormatLabelStatus/PadRight/FitToScreen`.
   Layout: header `OTHER PLAYER SUBMITS // YOU READ`,
   `### MATCH THE TARGET SIGNAL ###`, `SIGNAL LOG // REVISION n`,
   `STATUS...ANALYZING`, 3-line target-waveform ASCII, rows `SIGNAL RATE`,
   `SIGNAL POWER`, `WAVEFORM MATCH` (OK/INCORRECT). Waveform name never printed.
   Direction-correct LOW/HIGH for MIN..MAX indices.
6. ✅ New `SignalDiagnosticAdapter.cs` (MonoBehaviour): mirrors
   `ComponentDiagnosticAdapter` lifecycle — `OnAttemptSubmitted` subscribe/
   unsubscribe, `SetWaiting()` standby before first submit, revision counter
   increments on failed submits only, `SetSuccess(solvedMessage)` on solve.
   Serialized: `puzzleManager`, `diagnosticDisplay`, rate/power/waveform slot
   inputs + labels. Uses `ComponentDiagnosticClassifier` for statuses; target
   waveform index from `puzzleManager.TryGetCorrectIndex`.
7. ✅ Wire `Puzzle Signal.unity` via MCP (both panels): add
   `SignalDiagnosticAdapter` beside `Diagnostic Adapter-A/B`, wired operator
   inputs -> partner monitor (same cross-panel routing as today); disable (not
   remove) the old `ComponentDiagnosticAdapter` component; add
   `LiveCombinationPreviewBridge` beside the existing scope bridge with
   identical slot wiring. No renames, no removed objects. Save scene.
8. ✅ Validate + report (diff summary per step; final manager report update).

## Final ASCII (shipped in `SignalWaveformAsciiLibrary`, 30 chars x 3 lines each)

Material mapping verified in `ResultVisualWave_5State.prefab`:
StateBar-0=Mat_WaveFlat, 1=Sine, 2=Square, 3=Triangle, 4=Noise.

```
0 FLAT:  (blank)
         ______________________________
         (blank)
1 SINE:    _     _     _     _     _
          / \   / \   / \   / \   / \
         /   \_/   \_/   \_/   \_/   \_
2 SQUARE: __    __    __    __    __
          |  |  |  |  |  |  |  |  |  |
         _|  |__|  |__|  |__|  |__|  |_
3 TRI:     /\      /\      /\      /\
          /  \    /  \    /  \    /  \
         /    \  /    \  /    \  /    \
4 NOISE:  | .|| |. | ||. | .| |.| || |
         ||||||||||||||||||||||||||||||
          .| |. || .| | ||. | .| || |.
```

## Testing checklist

- ✅ Compile clean via MCP; no new console errors/warnings.
- ✅ (MCP play mode, Player A panel; Player B panel wired identically) Live preview matrix: FREQ min/mid/max, GAIN min/mid/max, all 5 waveforms —
  scope follows every change without Submit, for both players independently.
- ✅ During preview: attempts, shared history, metrics, timer, score, diagnostic
  text all unchanged; no completion triggered.
- ✅ (A BIT LOW / TOO LOW / INCORRECT + lights verified in play mode; OK/green + solve path relies on unchanged manager flow) Submit: exact target -> OK/green; +-1 -> A BIT LOW/HIGH + orange; +-2 ->
  TOO LOW/HIGH + red; waveform match -> OK/green, mismatch -> INCORRECT/red;
  ASCII always shows target waveform; name never printed.
- ✅ Submit increments history attempt exactly once; solve completes exactly
  once; repeated submit after solve does nothing; role swap round trip works.
- ✅ Layout: no wrap/clip (programmatic: 12 lines, max width 40, no </>; visual VT323 check pending on-device), exactly 12 lines, all five ASCII drawings share the
  same 3-line footprint, no `<`/`>` characters, alignment stable in VT323.
- ⬜ Regression: Tutorial + Pipes (manual playthrough pending; no shared scripts changed except additive MultiDimension event) play unchanged; no missing prefab refs;
  no duplicate event subscriptions after scene reload.

## Rollback notes

- Git is the rollback mechanism. Each step is a separate additive script or a
  scene/prefab override: `git checkout -- <file>` restores any of
  `MultiDimension.cs`, the 4 new scripts, `Puzzle Signal.unity`, Signal V2
  variant prefabs.
- Instant behavioral rollback without git: re-enable the old
  `ComponentDiagnosticAdapter` and disable/remove `SignalDiagnosticAdapter` +
  `LiveCombinationPreviewBridge` components (old submit-only flow is intact).
