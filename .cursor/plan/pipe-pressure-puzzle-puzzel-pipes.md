---
task: Pipe Pressure Puzzle — Puzzel Pipes
date: 2026-05-17
status: in_progress
phase_1: implemented
phase_2: implemented
phase_3: validated
related_assets: Assets/Scenes/Puzzel Pipes.unity, MultiDimension_Knob_4State, MultiDimension_Slider_4State, MultiDimension_ButtonText_4State, PipePressurePuzzelPipesWireTool.cs, PipePressurePhase1ValidationTool.cs, MultiDimensionDiagnosticAdapter.cs, DiagnosticDisplayController.cs
---

# Pipe Pressure Puzzle — Puzzel Pipes implementation plan

**Keep this plan** as the source of truth for Phases 2–6.

**Design choices (locked in):** separate 2×3 puzzles per player; fixed solutions first; **2** component hints per failed attempt (Phase 3); visualizer visible to **both** players; **alternating** turns with glass overlay.

**4-state prefabs:** `MultiDimension_Knob_4State`, `MultiDimension_Slider_4State`, `MultiDimension_ButtonText_4State` — do not edit old 3-state prefabs.

**Tools:** Wire — `Who Wired This → Pipe Pressure → Wire Puzzel Pipes Scene`. History headers (Phase 2) — `Apply Puzzel Pipes History Headers (Phase 2)`. Validate — `Validate Phase 1 (Puzzel Pipes)` (includes Phase 2 header width on both panel HistoryPanels).

**Workflow:** Update this plan (and [README](README.md) row) whenever a phase step lands — status table, progress log, and relevant checklists.

---

## Progress log

| Date | Change |
|------|--------|
| 2026-05-17 | Phase 1: scene wired (6×4-state inputs, managers, history order, focus, turn locks); editor wire + validate tools added. |
| 2026-05-17 | Symbolic `ButtonText_4State` rule locked — no TMP/displayName parity on FLOW/ROUTE; validation skips via `UsesSymbolicButtonTextVisuals`. |
| 2026-05-17 | Phase 2: widened `headerLine` / `separatorLine` on `Player1_Panel/HistoryPanel` and `Player2_Panel/HistoryPanel` only (17-char INPUT). Play Mode: Blue/Red rows padded; console clean. |
| 2026-05-17 | Validation: history checks scoped to both panel HistoryPanels; `ResetPuzzelPipesSolveStateForValidation()` after play-mode solves. |
| 2026-05-17 | **Phase 3 planned** — component-based diagnostic adapter; plan only (no code/scene changes yet). |
| 2026-05-17 | **Phase 3 implemented** — `ComponentDiagnosticAdapter`, manager API, `SetDiagnosticBody`, Puzzel Pipes wired; legacy diagnostic disabled on scene only. |
| 2026-05-17 | **Phase 3 validated** — full checklist (history, diagnostics, turn lock, completion, Tutorial isolation); Play Mode MCP + scene inspection. |

---

## Phase status

| Phase | Status | Notes |
|-------|--------|-------|
| 1 — 3×4 inputs + wiring | **implemented** | See [Phase 1 validation](#phase-1-validation-2026-05-17) |
| 2 — History 3×5 columns | **implemented** | Scene-only header/separator on both panel HistoryPanels; see [Phase 2](#phase-2--shared-history-3-inputs--5-char-tokens) |
| 3 — Component diagnostic | **validated** | See [Phase 3 plan](#phase-3-plan--component-diagnostic) |
| 4 — Result visualizer | planned | |
| 5 — Randomized solution | planned | |
| 6 — Balance / hints | planned | |

---

## Phase 1 validation (2026-05-17)

Validated via Unity MCP + editor menu `PipePressurePhase1ValidationTool` (structural / simulated checks). Play Mode used briefly; full two-player glass/focus pass still recommended manually.

### Results summary

| # | Check | Result | Notes |
|---|--------|--------|-------|
| 1 | 4 states per input | **PASS** | All six inputs `SubjectCount == 4` |
| 2 | Cycle through 4 states | **PASS** | `AdvanceIndexForPlayer` reaches 4 indices (edit-mode) |
| 3 | TMP matches `displayName` | **PASS** (scoped) | Knob/slider **PASS**; FLOW/ROUTE **PASS** via symbolic ButtonText_4State rule (see below) |
| 4 | Blue SEND → 3 history tokens | **PASS** (simulated) | Raw text `HALF MID RGHT`; adapter `inputOrder` ×3 |
| 5 | Red SEND → 3 history tokens | **PASS** (simulated) | Raw text `OPEN HIGH LOOP` |
| 6 | Panel focus 3 + Solve + Exit | **PASS** (wired) | `interactableButtons` ×3; Exit present (inactive in hierarchy) |
| 7 | Turn lock / glass (3 inputs) | **PASS** (wired) | `actionColliders` ×4 per bundle (3 inputs + Send) |
| 8 | Blue solution | **PASS** (simulated) | `TryCheckSolution` succeeds at indices 2/1/2 |
| 9 | Red solution | **PASS** (simulated) | `TryCheckSolution` succeeds at indices 3/2/3 |
| 10 | Console errors | **PASS** | No compile errors; no new runtime errors during checks |

### ButtonText_4State — symbolic visual labels (by design)

`MultiDimension_ButtonText_4State` intentionally uses **symbolic** LCD TMP on the control while `displayName` / Shared History use **readable** tokens:

| Index | `displayName` (history) | Visible TMP (control) |
|-------|-------------------------|------------------------|
| 0 | LEFT | `<<<` |
| 1 | MID | `[||]` |
| 2 | RGHT | `>>>` |
| 3 | LOOP | `(O)` |

**Do not** change `MultiDimension_ButtonText_4State` prefab or FLOW/ROUTE scene TMP overrides to match `displayName`. `PipePressurePhase1ValidationTool` skips TMP≠displayName for this prefab type (and FLOW/ROUTE names).

Knob/slider inputs still **PASS** visible TMP = `displayName` (e.g. VALVE shows SHUT/LOW/HALF/OPEN).

### Phase 1 — manual Play Mode still recommended

- Panel focus wrap across VALVE → PRESS → FLOW → Solve → Exit
- Glass blocks non-operator on all three colliders
- Live SEND rows on both HistoryPanel instances (both read same `SharedHistorySO`)

---

## Phase 2 — Shared History (3 inputs × 5-char tokens)

**Status: implemented (2026-05-17).** Minimal scope: scene-only header/separator on both Puzzel Pipes HistoryPanels; no Tutorial changes; no global `HistoryPanel.prefab` edit.

### 1. Existing history flow

```mermaid
sequenceDiagram
  participant PM as MultiDimensionPuzzelManager
  participant Ad as MultiDimensionHistoryAdapter
  participant SO as SharedHistorySO
  participant HB as HistoryBoardController

  PM->>Ad: OnAttemptSubmitted(result)
  Ad->>Ad: BuildInputText (unpadded labels, space-separated)
  Ad->>SO: AddEntry(actor, inputText, status)
  SO->>HB: OnChanged
  HB->>HB: Render → FormatInputCell pads tokens
```

| Piece | Role |
|-------|------|
| [`MultiDimensionHistoryAdapter.BuildInputText`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs) | Walks `inputOrder[]`; appends `GetSubjectDisplayName(index)` per token with **single spaces** — **no padding** |
| [`SharedHistorySO`](Assets/WhoWiredThis/Scripts/Data/Puzzels/SharedHistorySO.cs) | Stores `HistoryEntry.inputText` as raw string; resets on play |
| [`HistoryBoardController`](Assets/WhoWiredThis/Scripts/Puzzles/Common/HistoryBoardController.cs) | Owns `titleText`, `bodyText`, `headerLine`, `separatorLine`; calls `FormatInputCell` at render |
| **Puzzel Pipes** | One shared `SharedHistorySO` asset; **two** `HistoryPanel` instances (Player1 + Player2) both render it |
| **Font** | `VT323-Regular SDF` on body (monospace-style; good for columns) |

**Puzzel Pipes headers (scene overrides on both panel HistoryPanels):**

```
 # | SIDE | INPUT             | STATUS
===+======+===================+========
```

**Formatting:** `HistoryBoardController` uses `InputTokenWidth = 5` and `PadRight(5)` per token in `FormatInputCell`. Verified:

- `HALF MID RGHT` → `HALF  MID   RGHT` (17 characters)
- `OPEN HIGH LOOP` → `OPEN  HIGH  LOOP`

**Deferred:** stale `inputSeparator` prefab overrides on HistoryPanel instances — harmless.

### 2. Formatting rules (unchanged)

| Concern | Recommendation |
|---------|----------------|
| Where to pad | **Keep** padding only in `HistoryBoardController.FormatInputCell` (already true) |
| Method | `PadRight(5)` per token; single space between tokens (already true) |
| Labels longer than 5 | **Truncate** to 5 in `FormatInputCell` (already true); add **one** `Debug.LogWarning` per distinct truncated token (optional, editor/play once) |
| Labels shorter than 5 | Pad right with spaces in history only (already true) |
| `displayName` / TMP | Never pad; trim only |

**No change required** to `MultiDimensionHistoryAdapter` for basic 3-token support (Phase 1 wired `inputOrder` ×3).

### 3. Configuration

| Question | Recommendation |
|----------|----------------|
| Serialize `inputTokenWidth = 5`? | **Optional improvement:** `[SerializeField, Min(1)] int inputTokenWidth = 5` on `HistoryBoardController` — allows per-board override without code fork |
| Adapter vs board? | **HistoryBoardController only** — adapter stays unaware of column layout |
| All scenes vs Puzzel Pipes only? | **Default 5** is already global const; Tutorial uses 2 tokens → still fits widened logic. **Header/separator overrides: Puzzel Pipes scene only** — do not change `HistoryPanel.prefab` defaults unless Tutorial boards are visually re-tested |
| Avoid breaking Tutorial | Do not narrow `InputTokenWidth`; only widen Puzzel Pipes **scene** header strings. Tutorial rows remain 2×5 + space = 11 chars under a 13-char INPUT header (still fits) |

### 4. Scene changes (done)

Applied on **both** `Player1_Panel/HistoryPanel` and `Player2_Panel/HistoryPanel` only. Tutorial scenes and global `HistoryPanel.prefab` unchanged.

### 5. Testing (Phase 2)

| # | Test | Result |
|---|------|--------|
| 1 | Blue submit → 3 readable tokens | **PASS** (Play Mode / MCP) — raw `HALF MID RGHT`, rendered `HALF  MID   RGHT` |
| 2 | Red submit → 3 readable tokens | **PASS** — raw `OPEN HIGH LOOP`, rendered `OPEN  HIGH  LOOP` |
| 3 | Tokens align under widened INPUT header | **PASS** (both panel HistoryPanels) |
| 4 | FLOW/ROUTE symbolic TMP unchanged | **PASS** — e.g. `<<<` on control; history uses `RGHT` etc. |
| 5 | Console errors | **PASS** during automated submit |
| 6 | Tutorial regression (2-token row) | **Not run** — Tutorial headers unchanged by design |
| 7 | Live SEND via UI (focus, glass) | **Pending** — manual sign-off |

### 6. Risks

| Risk | Mitigation |
|------|------------|
| VT323 still drifts vs true monospace | Accept minor drift; avoid proportional fonts |
| Tutorial header looks sparse for 2 tokens | Leave Tutorial headers unchanged |
| 17-char INPUT wider than mesh | Scene-only RectTransform tweak |
| Stale `inputSeparator` confuses inspectors | Remove overrides |
| FLOW/ROUTE symbolic TMP | **By design** — not a validation failure |

### 7. Phase 2 implementation (completed)

| Step | Task | Status |
|------|------|--------|
| 2a | Validation: allow symbolic ButtonText_4State visuals | Done (`UsesSymbolicButtonTextVisuals`) |
| 2b | Puzzel Pipes: widen `headerLine` / `separatorLine` on `Player1_Panel/HistoryPanel` and `Player2_Panel/HistoryPanel` | Done (menu + scene YAML) |
| 2c | Remove stale `inputSeparator` prefab overrides | Deferred (harmless) |
| 2d | (Optional) Serialize `inputTokenWidth` | Not done (out of minimal scope) |
| 2e | Play Mode: Blue + Red submit | Done — raw `HALF MID RGHT` / `OPEN HIGH LOOP`; rendered `HALF  MID   RGHT` / `OPEN  HIGH  LOOP` |
| 2f | Plan + README | Done |
| 2g | Validation: scoped history paths + solve reset before validate | Done |

**Menu:** `Who Wired This → Pipe Pressure → Apply Puzzel Pipes History Headers (Phase 2)`

**Constants** (in `PipePressurePuzzelPipesWireTool`):

- `headerLine`: ` # | SIDE | INPUT             | STATUS` (INPUT segment = 17 chars)
- `separatorLine`: `===+======+===================+========`

---

## Phase 3 plan — Component diagnostic

**Status: implemented (2026-05-17).**

**Goal:** Replace Tutorial-style Bulls/Cows diagnostic on **Puzzel Pipes only** with pipe-machine component hints (max **2** component lines per failed attempt + one system line). Tutorial scenes keep `MultiDimensionDiagnosticAdapter`.

**Out of scope for Phase 3:** visualizer, randomized solution, scoring, main menu, global prefab edits, Tutorial scene changes.

**Git:** Working tree is dirty — commit or confirm safe state before implementation (per `unity-poc-workflow.mdc` §10).

---

### 1. Existing diagnostic flow (inspected)

```mermaid
sequenceDiagram
  participant Solve as SolveInteractProxy / Bridge
  participant Proc as ProcessingFeedbackController
  participant PM as MultiDimensionPuzzelManager
  participant Ad as MultiDimensionDiagnosticAdapter
  participant DDC as DiagnosticDisplayController

  Solve->>Proc: PlayProcessingRoutine (suppress body)
  Proc->>PM: TryCheckSolutionFromInteractor
  PM->>PM: Compare indices, RaiseAttemptSubmitted
  PM->>Ad: OnAttemptSubmitted(result)
  Ad->>PM: TryGetDiagnosticSnapshot (Bulls/Cows)
  Ad->>DDC: SetDiagnosticResult or SetSuccess
```

| Piece | Puzzel Pipes today |
|-------|-------------------|
| **Adapter** | `MultiDimensionDiagnosticAdapter` on **`Player1_Panel`** and **`Player2_Panel`** (enabled) |
| **Puzzle managers** | Player1 adapter → `Player1_Panel/PuzzleManager` (Blue: VALVE/PRESS/FLOW, correct 2/1/2). Player2 adapter → `Player2_Panel/PuzzleManager` (Red: GATE/PUMP/ROUTE, correct 3/2/3). |
| **Diagnostic displays** | **Cross-panel wiring** (same as Tutorial split layout): Player1 adapter → `diagnosticDisplay` **45800108** (`TutorialStageManager.playerBDiagnosticDisplay`). Player2 adapter → **1975054485** (`playerADiagnosticDisplay`). Each operator’s SEND updates the display the stage manager labels for the **partner** side. |
| **Processing** | `ProcessingFeedbackController` on each panel; same `diagnosticDisplay` as diagnostic adapter (`READING SIGNAL…` / `CHECKING SETTINGS…` / `UPDATING HISTORY…`). |
| **Commit mode** | `updateContinuously: 0` — no live preview; updates on `OnAttemptSubmitted` only. |
| **Metrics shown** | `SETTINGS OK: n/3`, `PLACES OK: m/3` (Bulls/Cows via [`TryGetDiagnosticSnapshot`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs)) |
| **Clue text** | `NO MATCHING SETTINGS…`, `PARTIAL MATCH…`, `CORRECT SETTINGS, WRONG ORDER.`, etc. |
| **Solved** | `A-SIDE CALIBRATED` / `B-SIDE CALIBRATED` via `SetSuccess` |
| **History** | Unaffected — `MultiDimensionHistoryAdapter` on same panels, shared `SharedHistorySO` |

**Tutorial isolation:** `MultiDimensionDiagnosticAdapter` remains on Tutorial / Split Tutorial scenes. Phase 3 only **disables** the two adapters on **Puzzel Pipes** and adds the new component adapter there.

---

### 2. Proposed new component

**Name:** `ComponentDiagnosticAdapter` (namespace `WhoWiredThis.Puzzles.Common`).

**Responsibilities:**

- Subscribe to one `MultiDimensionPuzzelManager.OnAttemptSubmitted` (`OnEnable` / `OnDisable`), same pattern as [`MultiDimensionDiagnosticAdapter`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionDiagnosticAdapter.cs).
- On each attempt, for each configured component (parallel to `puzzleElements` / `inputOrder`):
  - Read `result.SubmittedIndices[i]`.
  - Read correct index via small manager API (see §3) or matching `ComponentDiagnosticDefinition` slot.
  - Classify: **Correct**, **TooLow**, **TooHigh** (ordered), **Mismatch** (categorical).
- Build body text:
  - **Solved:** configured success message → `DiagnosticDisplayController.SetSuccess`.
  - **Failed:** one **system** line + up to **two** component hint lines + optional **tell partner** line → new body-only API or equivalent (see §6).
- **No** `TryGetDiagnosticSnapshot` / SETTINGS OK / PLACES OK on Puzzel Pipes.
- **No** `updateContinuously` — commit-only like current Puzzel Pipes setup.
- Optional: skip `MachineFeedbackTextController` flavor lines for pipe tone (or one pipe-themed flavor list later).

**Not responsible for:** history rows, turn lock, processing timing, puzzle solve logic.

---

### 3. Data access plan

| Source | Available today | Gap |
|--------|-----------------|-----|
| [`MultiDimensionAttemptResult`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionAttemptResult.cs) | `SubmittedIndices[]`, `IsSolved`, `Actor` / `ActorLabel`, `PublicStatus` | No per-slot correct indices |
| [`MultiDimensionPuzzleElement`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs) | `Element`, `CorrectIndex` (public getters) | Parent `puzzleElements[]` is **private** |
| Manager | `TryGetDiagnosticSnapshot` (Bulls/Cows) | Wrong model for Phase 3 |

**Recommended minimum API** on `MultiDimensionPuzzelManager` (read-only):

```csharp
public int PuzzleElementCount { get; }
public bool TryGetPuzzleElement(int index, out MultiDimension element, out int correctIndex);
```

- Implementation walks existing `puzzleElements` (no new serialized data).
- Adapter maps `result.SubmittedIndices[i]` to `correctIndex` by **shared index** `i`.
- **No reflection**, no SerializedObject hacks in play mode.

**Config vs manager order:** `ComponentDiagnosticAdapter` should use an explicit `ComponentDiagnosticDefinition[]` where each entry references a `MultiDimension` and documents `displayName` + type. At runtime, resolve slot index by matching `input` reference to `TryGetPuzzleElement` element (or require config order === `puzzleElements` order and validate in editor). Prefer **reference match** with a one-time editor validation warning on mismatch.

**Future randomization (Phase 5):** `correctIndex` stays on the manager’s `puzzleElements`; adapter always reads live correct index per attempt — no hardcoded solution in the adapter.

---

### 4. Component configuration model

```csharp
public enum ComponentDiagnosticType { Ordered, Categorical }

[Serializable]
public class ComponentDiagnosticDefinition
{
    public MultiDimension input;           // same refs as puzzleElements / inputOrder
    public string displayName;             // e.g. VALVE, PRESS, FLOW
    public ComponentDiagnosticType type;
    [TextArea] public string correctText;    // e.g. VALVE LOOKS STABLE.
    [TextArea] public string tooLowText;     // ordered only
    [TextArea] public string tooHighText;    // ordered only
    [TextArea] public string mismatchText;   // categorical (and optional ordered fallback)
    public bool eligibleForHints = true;
}
```

**Puzzel Pipes defaults (Inspector copy, per panel):**

| Slot | Input | Type | correct | too low | too high | mismatch |
|------|-------|------|---------|---------|----------|----------|
| 0 | VALVE / GATE | Ordered | `{NAME} LOOKS STABLE.` | `{NAME} TOO CLOSED.` or `TOO LOW.` | `{NAME} TOO OPEN.` or `TOO HIGH.` | (unused) |
| 1 | PRESS / PUMP | Ordered | `{NAME} LOOKS STABLE.` | `PRESSURE TOO LOW.` / `PUMP OUTPUT TOO LOW.` | `PRESSURE TOO HIGH.` / `PUMP OUTPUT TOO HIGH.` | (unused) |
| 2 | FLOW / ROUTE | Categorical | `FLOW ROUTE LOOKS STABLE.` / `ROUTE LOOKS STABLE.` | — | — | `FLOW ROUTE DOES NOT MATCH.` / `ROUTE DOES NOT MATCH.` |

`{NAME}` = `displayName` field (VALVE, PRESS, FLOW / GATE, PUMP, ROUTE).

**Classification rules:**

- **Ordered:** `submitted == correct` → Correct; `<` → TooLow; `>` → TooHigh.
- **Categorical:** `submitted == correct` → Correct; else → Mismatch (never emit too low/high).

---

### 5. Hint selection strategy (v1 — deterministic)

**Per failed attempt:**

1. Classify all components.
2. **System line** (one), from counts:
   - 0 correct → `PIPE RESPONSE IS UNSTABLE.`
   - 1 correct → `PARTIAL CALIBRATION. ONE COMPONENT HOLDS.`
   - 2 correct → `NEARLY STABLE. ONE COMPONENT STILL OFF.`
   - (3 correct on failure path should not occur if manager logic is consistent.)
3. **Component hints — exactly 2 lines** when possible:
   - If **any** Correct: include **one** correct hint (first correct in config order).
   - If **any** wrong: include **one** wrong hint (first wrong in config order).
   - If **no** Correct: include **two** wrong hints (first two wrong in config order).
   - If only one wrong exists (shouldn’t with 3 inputs): show one wrong + system only.
4. **Tell partner** (optional footer): `TELL YOUR PARTNER WHAT YOU LEARNED.` when `IsSolved == false` and at least one hint shown.

**Explicitly not in v1:** rotation, progressive widening, hiding correct names, attempt-index-based shuffle.

**Solved attempt:** system + component hints skipped; only solved message.

---

### 6. Formatting & display API

**Body layout (failed):**

```
PIPE RESPONSE IS UNSTABLE.

VALVE LOOKS STABLE.
PRESSURE IS TOO HIGH.

TELL YOUR PARTNER WHAT YOU LEARNED.
```

**Minimal `DiagnosticDisplayController` change (recommended):**

Add `SetDiagnosticBody(string message)` — sets `DisplayState.Result`, writes body, applies result lamp. Keeps display render-only; avoids fake `SETTINGS OK` metrics.

**Default copy — Player A (Blue panel adapter)**

| Case | Text |
|------|------|
| Solved | `PIPE LINE CALIBRATED.` (or **SIDE** — see questions) |
| System (0 correct) | `PIPE RESPONSE IS UNSTABLE.` |
| System (partial) | `PARTIAL CALIBRATION. ONE COMPONENT HOLDS.` / `NEARLY STABLE. ONE COMPONENT STILL OFF.` |
| Ordered too low | `{NAME} TOO CLOSED.` (valve) / `PRESSURE TOO LOW.` |
| Ordered too high | `{NAME} TOO OPEN.` / `PRESSURE TOO HIGH.` |
| Categorical mismatch | `FLOW ROUTE DOES NOT MATCH.` |
| Ordered stable | `{NAME} LOOKS STABLE.` / `PRESSURE LOOKS STABLE.` |
| Partner | `TELL YOUR PARTNER WHAT YOU LEARNED.` |

**Player B:** same templates with GATE / PUMP / ROUTE display names; solved `PIPE LINE CALIBRATED.` or `B-SIDE PIPE CALIBRATED.`

**Processing lines (optional Phase 3b, scene-only):** change `ProcessingFeedbackController` messages on Puzzel Pipes to `READING PRESSURE…` / `CHECKING PIPE STATE…` / `UPDATING HISTORY…` — not required for first pass.

---

### 7. Scene wiring (Puzzel Pipes only)

| Step | Action |
|------|--------|
| A | Add `ComponentDiagnosticAdapter` to **`Player1_Panel`** and **`Player2_Panel`** (same GameObjects as history/diagnostic today). |
| B | **Disable** (or remove) existing `MultiDimensionDiagnosticAdapter` on both panels — prevents overwrite. |
| C | Wire `puzzleManager` → local `PuzzleManager`. |
| D | Wire `diagnosticDisplay` → **keep current cross-panel refs**: Player1 → **45800108**, Player2 → **1975054485**. |
| E | Assign `components[]` ×3 per panel (VALVE/PRESS/FLOW or GATE/PUMP/ROUTE) with types and strings from §4. |
| F | Leave `ProcessingFeedbackController` and `TutorialStageManager` diagnostic refs unchanged. |
| G | Leave `MultiDimensionHistoryAdapter` unchanged. |

**Do not** edit Tutorial.unity, Split Tutorial, or global Diagnostic prefab unless later approved.

**Optional editor menu:** `Who Wired This → Pipe Pressure → Wire Component Diagnostic (Puzzel Pipes)` — mirrors Phase 1/2 tools; assigns refs and disables old adapters.

---

### 8. Testing plan

| # | Test | Expected |
|---|------|----------|
| 1 | Blue wrong (e.g. SHUT/LOW/LEFT) | System + 2 wrong component lines; **no** SETTINGS OK / PLACES OK |
| 2 | Blue partial (e.g. HALF/LOW/LEFT) | System partial + 1 stable + 1 wrong |
| 3 | Blue solve HALF/MID/RGHT | `PIPE LINE CALIBRATED.` (or approved solved string) |
| 4 | Red wrong / partial / solve | Same pattern with GATE/PUMP/ROUTE copy |
| 5 | History | Rows still 3 tokens, padded; statuses CALIBRATED / UNSTABLE |
| 6 | Turn lock / glass | Alternation unchanged |
| 7 | Tutorial scene spot-check | Still SETTINGS OK / PLACES OK |
| 8 | Console | No errors on SEND |
| 9 | Partner display | Blue SEND updates display on partner wiring (45800108); confirm readable at Red station |

---

### 9. Risks

| Risk | Mitigation |
|------|------------|
| Correct index not accessible | Add `TryGetPuzzleElement` API (§3) |
| Old adapter still enabled | Disable `MultiDimensionDiagnosticAdapter` on Puzzel Pipes only |
| Cross-panel display confusion | Document refs; test from both physical stations |
| Too easy (2 hints) | Fixed v1 rule; tune copy before adding rotation |
| Too hard | Stable hint confirms one component; partner line encourages talk |
| Config order ≠ `puzzleElements` | Reference-based slot lookup + editor validate |
| Phase 5 random solutions | Read `correctIndex` from manager each attempt |
| `SetDiagnosticResult` mismatch | Use `SetDiagnosticBody` instead of fake metrics |
| Processing suppress race | Keep adapter write **after** `OnAttemptSubmitted` (post-check), same as today |

---

### 10. Questions (please answer before implementation)

1. **Solved text:** `PIPE LINE CALIBRATED.` vs `A-SIDE CALIBRATED` / `B-SIDE CALIBRATED` vs `SIDE CALIBRATED`?
2. **Component names in hints:** Use literal names (`VALVE`, `PRESS`) or atmospheric only (`THE VALVE`, no names)?
3. **Reveal correct components:** v1 plan **does** show one stable line when any component is correct — OK?
4. **FLOW/ROUTE:** Categorical **mismatch only** (no too low/high) — confirm?
5. **Hint selection:** Deterministic (first correct + first wrong in config order) — OK, or prefer rotating wrong hints across attempts?
6. **Partner line:** Include `TELL YOUR PARTNER…` on every failed attempt?
7. **Valve wording:** `TOO CLOSED` / `TOO OPEN` vs `TOO LOW` / `TOO HIGH` for VALVE/GATE?

---

### 11. Implementation steps (completed)

| Step | Task | Status |
|------|------|--------|
| 3.1 | Git baseline confirmed by user | Done |
| 3.2 | `MultiDimensionPuzzelManager`: `PuzzleElementCount` + `TryGetPuzzleElement` | Done |
| 3.3 | `DiagnosticDisplayController.SetDiagnosticBody` | Done |
| 3.4 | `ComponentDiagnosticAdapter` + definitions | Done |
| 3.5 | Menu: `Wire Puzzel Pipes Component Diagnostic (Phase 3)` | Done |
| 3.6 | Legacy `MultiDimensionDiagnosticAdapter` disabled on Puzzel Pipes | Done (`m_Enabled: 0`) |
| 3.7–3.8 | Play Mode MCP: Blue/Red wrong, partial, solve | **PASS** — no SETTINGS/PLACES |
| 3.9 | Tutorial scene | Unchanged (legacy adapter still on Tutorial) |
| 3.10 | Plan + README | Done |

**Menu:** `Who Wired This → Pipe Pressure → Wire Puzzel Pipes Component Diagnostic (Phase 3)`

### Phase 3 validation (2026-05-17)

**Method:** Fresh Play Mode on `Puzzel Pipes.unity` (MCP `execute_code` sequence) + edit-mode scene YAML inspection. No code or scene changes during validation.

| # | Check | Result |
|---|--------|--------|
| 1 | Blue SEND → 3 history tokens | **PASS** — e.g. raw `SHUT LOW LEFT` |
| 2 | Red SEND → 3 history tokens | **PASS** — e.g. raw `SHUT LOW LEFT` (Red indices) |
| 3 | History readable (3 tokens, padded render) | **PASS** — body contains `SHUT` after render |
| 4 | Blue wrong / partial / solve diagnostics | **PASS** — pipe copy; partial: VALVE STABLE + PRESSURE TOO LOW; solve: `PIPE LINE CALIBRATED.` |
| 5 | Red wrong / partial / solve diagnostics | **PASS** — GATE TOO CLOSED; partial: GATE STABLE; solve: `PIPE LINE CALIBRATED.` |
| 6 | No SETTINGS / PLACES on Puzzel Pipes | **PASS** |
| 7 | Waiting player (Red at start) blocked on 3 inputs | **PASS** — `PanelActionLock` + GATE/PUMP/ROUTE colliders disabled |
| 8 | Turn switch after Blue solves | **PASS** — `CurrentStage` → PlayerBOperator; Blue locked |
| 9 | Red operates after Blue solves | **PASS** — Red unlocked; GATE collider enabled |
| 10 | Completion after Red solves | **PASS** — `CurrentStage` → Complete (2) |
| 11 | Tutorial still SETTINGS / PLACES | **PASS** — `Tutorial.unity`: legacy adapter **enabled**, no `ComponentDiagnosticAdapter` |
| 12 | Console errors | **PASS** — 0 errors during validation |

**Note:** Checklist item 10 `completionRaised` confirmed via `CurrentStage == Complete` after Red solve. Manual UI pass (glass overlay, live SEND) still listed under Phase 1 checklist.

### Phase 3 test results (implementation smoke — Play Mode / MCP)

| Test | Result |
|------|--------|
| Blue wrong (0/0/0) | **PASS** — UNSTABLE + VALVE TOO CLOSED + PRESSURE TOO LOW + partner line; no SETTINGS/PLACES |
| Blue partial (2/0/0) | **PASS** — ONE PIPE SECTION + VALVE STABLE + PRESSURE TOO LOW |
| Blue solve (2/1/2) | **PASS** — `PIPE LINE CALIBRATED.` |
| Red wrong / partial / solve | **PASS** — GATE/PUMP/ROUTE copy; solved message |
| Console | **PASS** — no compile/runtime errors |

**Rollback:** Re-enable `MultiDimensionDiagnosticAdapter`, remove/disable `ComponentDiagnosticAdapter`, revert manager/display API if unused elsewhere; `git checkout -- Assets/Scenes/Puzzel\ Pipes.unity` + new scripts.

---

## Later phases (summary)

- **Phase 4:** `SubmittedCombinationVisualizer`, both players
- **Phase 5:** `RandomPuzzleSolutionAssigner`
- **Phase 6:** Hint cooldown / balance

---

## Phase 1 implementation checklist

- [x] Six inputs from 4-state prefabs
- [x] `displayName` order correct on all inputs
- [x] `correctIndex` Blue 2/1/2, Red 3/2/3
- [x] Managers, history order, panel focus, turn locks
- [x] Wire + validate editor tools
- [x] FLOW/ROUTE symbolic TMP allowed; history uses `displayName`
- [x] Phase 2 history headers (both panel HistoryPanels)
- [x] Phase 3 component diagnostic (Puzzel Pipes only)
- [ ] Full Play Mode sign-off (focus, glass, live SEND via UI)

## Rollback

Git revert scene + `Assets/WhoWiredThis/Editor/PipePressure*.cs` as needed. Do not revert 4-state prefabs or Tutorial scenes.
