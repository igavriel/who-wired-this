---
task: Puzzle input state labels — 3-char to max 5-char tokens
date: 2026-05-16
status: implemented
related_assets: Assets/Scenes/Tutorial.unity, HistoryBoardController.cs, MultiDimensionHistoryAdapter.cs, MultiDimension_Knob.prefab, MultiDimension_Slider.prefab
---

# Puzzle input state labels — max 5 characters + Shared History alignment

## Scope

- Rename tutorial/puzzle **state labels** (`MultiDimension.subjects[].displayName` + matching world-space **TextMeshPro** labels) from the current **3-character** convention to a **maximum of 5 characters**.
- Widen **Shared History** INPUT column token formatting from width **3** to width **5** (padding at render time only).
- Primary target: **`Assets/Scenes/Tutorial.unity`** (current working tutorial scene; same wiring as `Tutorial Backup.unity` and `Assets/Scenes/Puzzles/Split Tutorial.unity`).

## Out of scope

- Puzzle **logic** (`correctIndex`, `MultiDimensionPuzzelManager` validation, scoring, diagnostics text, tutorial stage flow, high scores, main menu).
- Changing **global** `MultiDimension_*` prefabs without explicit approval (see scene/prefab strategy).
- Other puzzle scenes (`Floor_Puzzle`, `A17_PolarityPanel`, color-button prefabs, etc.) unless explicitly approved in a follow-up.
- Runtime auto-sync from `displayName` to TMP (not present today; not introducing unless requested).

## Approved implementation steps

*(Pending user approval — do not implement until approved.)*

### Phase 1 — Code-only history formatting (compile + manual smoke)

1. In `HistoryBoardController.cs`:
   - Change `InputTokenWidth` from `3` to `5` (prefer `[SerializeField] private int inputTokenWidth = 5` with `[Min(1)]` so old scenes can stay at 3 if needed).
   - Keep padding/truncation in `FormatInputCell` only (do **not** pad `displayName` in `MultiDimension` or `MultiDimensionHistoryAdapter.BuildInputText`).
   - On truncate (`token.Length > inputTokenWidth`), log `Debug.LogWarning` once per distinct token in editor/play mode (optional, low noise).
2. Update **header/separator strings** where they define INPUT column width:
   - `HistoryPanel.prefab` defaults: widen `headerLine` / `separatorLine` INPUT segment for ~11 chars (two 5-wide tokens + space).
   - Scene overrides on `Tutorial.unity` history boards if present.
3. Compile-check; submit a test row via play mode and confirm column alignment.

### Phase 2 — Tutorial scene label + TMP sync (scene instances only)

4. Using Unity MCP (or Inspector), for **each** of the four tutorial inputs below, per subject index:
   - Set `MultiDimension.subjects[i].displayName` to the approved 5-char vocab.
   - Set the matching TMP `m_text` on the subject’s label object (see prefab fileIDs in plan body).
   - Verify `displayName ==` visible TMP text (no trailing spaces on either).
5. Do **not** change `correctIndex` or `inputOrder` unless a bug is found (order is index-based).

### Phase 3 — Visual fit verification

6. Play-mode check each state: OFF/DIM/BRITE, LOW/MID/HIGH, CLOSE/HALF/OPEN — labels readable, not clipped.
7. If 5 chars clip on knob/slider bezels: minor TMP `fontSize` or RectTransform width tweak on **scene overrides only** first.

### Phase 4 — Regression testing

8. Run full testing checklist (below).

## Testing checklist

- [ ] Submit attempts with 3-char-equivalent labels (LOW, MID, OFF) after rename to 3–4 char names — history aligns.
- [ ] Submit with 4-char labels (OPEN, HIGH) — history aligns.
- [ ] Submit with 5-char labels (BRITE, CLOSE) — history aligns; no clip in INPUT column.
- [ ] Visual button/knob/slider labels match history **semantic** labels (history shows padded tokens; controls do not show trailing spaces).
- [ ] Player A and Player B puzzles still solve at intended `correctIndex` values.
- [ ] Diagnostics unchanged (metrics/messages).
- [ ] Interact prompts show unpadded `GetSubjectDisplayName` (e.g. `Cycle subject — OPEN`).
- [ ] No extra spaces in non-history UI.
- [ ] Unity console: no new errors; note any truncate warnings.

## Rollback notes

- **Git**: revert `HistoryBoardController.cs` + scene YAML + optional `HistoryPanel.prefab` header strings.
- **History width**: if serialized `inputTokenWidth` is added, set back to `3` on boards without relabeling scenes.
- **Labels**: scene prefab overrides are independent of global prefabs if global prefabs were not edited.

---

## 1. Existing label flow summary

### Visual input text

- World-space **TMP** labels live on subject child objects inside `MultiDimension_Knob.prefab` and `MultiDimension_Slider.prefab`.
- Labels are **authored independently** of `displayName` (no runtime binder). Tutorial scene applies **prefab instance overrides** for both `m_text` and `subjects[].displayName`.
- Known inconsistency: `MultiDimension_Slider.prefab` has `displayName: BRT` but one TMP child uses `m_text: BRIGHT` (prefab default) — tutorial scenes override to `BRT` for alignment.

### `displayName` storage

- `MultiDimensionSubject.displayName` (serialized string) on `MultiDimension.subjects[]`.
- `MultiDimension.GetSubjectDisplayName(index)` returns `displayName` if non-empty, else subject `GameObject.name`.

### Shared History path

```
MultiDimensionPuzzelManager.TryCheckSolution*
  → OnAttemptSubmitted(MultiDimensionAttemptResult)
  → MultiDimensionHistoryAdapter.BuildInputText
       → foreach inputOrder[i]: md.GetSubjectDisplayName(submittedIndex)
       → space-joined raw string (e.g. "LOW MID")
  → SharedHistorySO.AddEntry(actor, inputText, status)
  → HistoryBoardController.Render
       → FormatInputCell(entry.inputText)
            → split tokens, PadRight(3) / truncate to 3 today
```

### Logic dependency

- **Puzzle solve checks use indices only** (`MultiDimensionPuzzleElement.correctIndex`, `GetCurrentIndexForSolutionCheck()`).
- **History adapter** uses `inputOrder[]` only for **label lookup**, same order as puzzle elements (must stay aligned).
- **Diagnostics** (`MultiDimensionDiagnosticAdapter`) use numeric recognized/aligned counts — **not** state strings.
- **Interact prompts** (`MultiDimensionSubjectCycler.GetPromptText`) append **unpadded** `GetSubjectDisplayName`.

---

## 2. Proposed 5-character naming convention (Tutorial)

| Control | Player | Scene instance | Prefab | Current → Proposed |
|---------|--------|----------------|--------|-------------------|
| **POWER** | A | `LeftKnob` (`1009292064`) | `MultiDimension_Knob` | OFF → **OFF** (3), DIM → **DIM** (3), BRT → **BRITE** (5) |
| **FLOW** | A | `RightSlider` (`1766526287`) | `MultiDimension_Slider` | LOW → **LOW** (3), MID → **MID** (3), HIG → **HIGH** (4) |
| **VALVE** | B | `LeftSlider` (`2062539942`) | `MultiDimension_Slider` | CLS → **CLOSE** (5), HLF → **HALF** (4), OPN → **OPEN** (4) |
| **LOAD** | B | `RightKnob` (`38173451`) | `MultiDimension_Knob` | LOW → **LOW** (3), MID → **MID** (3), HIG → **HIGH** (4) |

**Rejected / alternate forms (>5 chars):**

| Idea | Chars | Use instead |
|------|-------|-------------|
| BRIGHT | 6 | **BRITE** (5) |
| CLOSED | 6 | **CLOSE** (5) |
| HALFWAY | 7 | **HALF** (4) |

**Puzzle wiring (Tutorial.unity — do not change indices):**

| Manager | `puzzleElements` / `inputOrder` | Elements |
|---------|----------------------------------|----------|
| Player A (`2097925730`) | Knob `1009292066`, Slider `1766526291` | FLOW + POWER order in history |
| Player B (`1340959902`) | Slider `2062539944`, Knob `38173453` | VALVE + LOAD |

---

## 3. History formatting strategy

### Where to implement

- **Primary:** `HistoryBoardController.FormatInputCell` — already owns `InputTokenWidth`, split, `PadRight`, and truncate.
- **Do not** pad in `MultiDimensionHistoryAdapter.BuildInputText` (keeps stored `inputText` human-readable; avoids polluting other consumers).
- **Do not** add trailing spaces to `displayName`.

### Padding / truncation

- Shorter than 5: `token.PadRight(5)` (e.g. `LOW` → `LOW  `).
- Longer than 5: `token.Substring(0, 5)` + `Debug.LogWarning` (e.g. accidental `BRIGHT` → `BRIGH`).
- Empty token: five spaces (existing pattern).

### TMP / spaces

- `bodyText.textWrappingMode = NoWrap` and `Overflow` already set — spaces are preserved in the string.
- **Font:** `HistoryPanel.prefab` body/title use **VT323-Regular SDF** (`guid: 0fa373e2af9b045ba822079a9fd0c9ef`) — **monospace-friendly**; proportional padding is reasonable without font change.
- If alignment still drifts: widen INPUT segment in `headerLine` / `separatorLine` (e.g. two 5-char tokens + delimiter ≈ 11 chars under INPUT).

### Configurable width

- Recommend **`[SerializeField] int inputTokenWidth = 5`** on `HistoryBoardController` (replaces `const`).
- Old scenes/prefabs defaulting to 3 can keep `3` until relabeled.
- Optional shared constant in a small static helper only if multiple formatters appear later (not required now).

---

## 4. Scene / prefab update strategy

| Approach | Safety | Recommendation |
|----------|--------|----------------|
| **Tutorial.unity scene overrides only** | Highest — matches `split_tutorial_input_configuration_approved` precedent | **Default for implementation** |
| `Split Tutorial.unity` / `Tutorial Backup.unity` | Medium — duplicates | Update only if user wants parity |
| Global `MultiDimension_Knob` / `MultiDimension_Slider` prefabs | Low — affects all scenes using prefab defaults | **Requires explicit approval** |
| `HistoryPanel.prefab` header width | Medium — affects all instances | Small, safe if INPUT column widened; approve with Phase 1 |

**Git:** working tree is dirty; commit or stash before risky edits per `unity-poc-workflow.mdc` §10.

---

## 5. Visual TextMeshPro sync strategy (MCP checklist)

For each scene instance, update **both** `subjects[i].displayName` and TMP `m_text` on the label component tied to that subject.

### Knob (`MultiDimension_Knob`) — TMP fileIDs in prefab

| Subject index | Typical child | TMP component fileID (prefab) |
|---------------|---------------|--------------------------------|
| 0 | LOW state | `4142557081475261460` |
| 1 | MID state | `8131763786430075750` |
| 2 | HIGH state | `5783659413463873283` |

### Slider (`MultiDimension_Slider`)

| Subject index | TMP fileID (prefab) |
|---------------|---------------------|
| 0 | `669892471574922950` |
| 1 | `2310129904972887267` |
| 2 | (third label — confirm via MCP `get_components` on OFF/BRITE subject) |

### Tutorial instances to touch

1. `LeftKnob` / `1009292064` — POWER — Player A panel parent `1965086817`
2. `RightSlider` / `1766526287` — FLOW
3. `LeftSlider` / `2062539942` — VALVE — Player B panel parent `706340101`
4. `RightKnob` / `38173451` — LOAD

**Verification:** MCP `manage_gameobject` / `get_components` with `include_properties=true` on each instance; compare `displayName` array to each subject’s TMP `text` field.

---

## 6. Code changes needed

| File | Change |
|------|--------|
| `HistoryBoardController.cs` | `inputTokenWidth = 5` (serialized); use in `FormatInputCell`; optional truncate warning |
| `MultiDimensionHistoryAdapter.cs` | **No change** expected |
| `MultiDimension.cs` | **No change** |
| `HistoryPanel.prefab` | Widen `headerLine` / `separatorLine` INPUT column (optional Phase 1) |
| Scenes | Label overrides only (Phase 2) |

**Backward compatibility:** Boards with old 3-char labels and `inputTokenWidth = 3` remain valid. Mixed-width history during migration is acceptable if scenes are updated in one pass per scene.

---

## 7. Risks

1. **TMP bezel clip** — knob/slider labels use `fontSize: 2` world units; 5 chars may need RectTransform or font tweaks on scene instances.
2. **Header/separator drift** — fixed-width tokens can outgrow `INPUT   ` header; update header lines with code change.
3. **Truncation surprise** — labels >5 silently clip unless warnings enabled.
4. **Prefab drift** — editing global prefabs breaks non-tutorial scenes still on 3-char vocab.
5. **Manual dual-field sync** — missing TMP or `displayName` update causes prompt/history/control mismatch.
6. **Slider BRIGHT legacy** — prefab default `BRIGHT` can resurface if overrides are reverted.

---

## 8. Questions for user (before implementation)

1. **Scope:** Tutorial.unity only, or also `Split Tutorial.unity` / `Tutorial Backup.unity`?
2. **Prefabs:** Scene overrides only, or update `MultiDimension_Knob` / `MultiDimension_Slider` defaults globally?
3. **History width:** Hardcode 5, or serialized `inputTokenWidth` per board (recommended)?
4. **Overflow policy:** Auto-truncate with warning, or editor-time reject/warn on `displayName` length > 5?
5. **Font:** Is VT323 on all history boards acceptable, or should any board switch to a different monospace SDF?
6. **Vocabulary:** Approve table in §2 (BRITE vs BRIGHT, CLOSE vs CLOSED, etc.)?

---

## 9. Proposed implementation phases (summary)

| Phase | Deliverable | Touches |
|-------|-------------|---------|
| **1** | History width 5 + header tweak | Code + optional `HistoryPanel.prefab` |
| **2** | Label rename + TMP sync | `Tutorial.unity` instances only (unless approved wider) |
| **3** | Visual fit pass | Scene TMP/layout overrides |
| **4** | Regression test | Play mode, both players, history + solve + diagnostics |
