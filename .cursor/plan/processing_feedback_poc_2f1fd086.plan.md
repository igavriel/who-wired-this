---
name: Processing feedback POC
overview: Add a small `ProcessingFeedbackController` (TMP + optional roots + timing) and optionally wire it from `MultiDimensionPuzzleInteractableBridge` so Activate runs a short scripted sequence, hides only the Diagnostic visual, delays `TryCheckSolutionFromInteractor`, then restores UI—leaving puzzle math, events, and adapters unchanged.
todos:
  - id: add-processing-feedback
    content: Create ProcessingFeedbackController.cs (TMP, roots, messages, timePerMessage, activateInteractable, IEnumerator PlayProcessingRoutine, guards)
    status: completed
  - id: bridge-coroutine
    content: Extend MultiDimensionPuzzleInteractableBridge with optional processingFeedback + RunActivateFlow coroutine + re-entrancy guard
    status: completed
  - id: scene-wire
    content: Author Processing UI per panel in scene/prefab and assign ProcessingFeedbackController + bridge references (manual in Editor)
    status: completed
isProject: false
---

# Processing feedback after Activate (multi-dimension POC)

## Why integrate at the bridge (least invasive)

- **Activate path:** [`MultiDimensionPuzzleInteractableBridge.Interact`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs) is the **only** gameplay caller of [`TryCheckSolutionFromInteractor`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs) (repo-wide grep).
- **Today:** `Interact` calls the manager immediately → [`RaiseAttemptSubmitted`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs) → [`MultiDimensionDiagnosticAdapter`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionDiagnosticAdapter.cs) and [`MultiDimensionHistoryAdapter`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs) react on the same frame.
- **Chosen flow (matches your preference):** delay the **manager check** until after processing, so **no changes** to combination logic, diagnostic snapshot math, or event payload. History still updates only when the event fires (after processing); per your answer, **only the Diagnostic visual is hidden** during processing—the Shared History surface stays visible.

**Alternative you asked about (“check first, delay visible update”):** would require buffering `MultiDimensionAttemptResult` and deferring `RefreshDisplay` / `AddEntry` in adapters, or splitting “compute” from “notify”—more moving parts. **Not recommended** for this task.

## New script: `ProcessingFeedbackController`

**Path:** [`Assets/WhoWiredThis/Scripts/Puzzles/Common/ProcessingFeedbackController.cs`](Assets/WhoWiredThis/Scripts/Puzzles/Common/ProcessingFeedbackController.cs) (alongside other puzzle/common UI helpers).

**Inspector (all `[SerializeField]`, null-safe warnings):**

| Field | Purpose |
|-------|---------|
| `GameObject processingRoot` | Parent for processing UI (TMP lives under it). Hidden when idle. |
| `TMP_Text processingMessageText` | Lines shown in sequence (world TMP matches [`DiagnosticDisplayController`](Assets/WhoWiredThis/Scripts/Puzzles/Common/DiagnosticDisplayController.cs) style). |
| `GameObject diagnosticDisplayRoot` | **Optional.** Set inactive during processing; reactivated after. Hides live diagnostic without touching adapter logic. |
| `string[] processingMessages` | Default three lines in editor: READING SIGNAL…, CHECKING SETTINGS…, UPDATING HISTORY… |
| `float timePerMessage` | Seconds each line is shown (configurable). |
| `MonoBehaviour activateInteractable` | **Optional.** Same object as the Solve/Activate `IInteractable` (e.g. the bridge’s solve button behaviour). `enabled = false` at start of sequence; after `TryCheck`, set `enabled = true` only if `puzzleManager != null && !puzzleManager.Solved` (on success the manager already disables the solve interactable via [`DisableInteractionsAfterSolve`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs)). |

**API:** `public IEnumerator PlayProcessingRoutine()` — yields `WaitForSeconds(timePerMessage)` per message, updates `processingMessageText`, toggles roots. No `GameObject.Instantiate`.

**Internals:** guard `isRunning` so overlapping Activates are ignored with a warning; validate refs in `PlayProcessingRoutine` and log warnings then early-finish.

## Change to existing script: `MultiDimensionPuzzleInteractableBridge`

**File:** [`Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs)

- Add **optional** `[SerializeField] private ProcessingFeedbackController processingFeedback;` (default null → **current behavior unchanged**).
- Replace immediate `TryCheckSolutionFromInteractor` in `Interact` with `StartCoroutine(RunActivateFlow(interactor))` where the coroutine:
  1. If `processingFeedback != null`: `yield return processingFeedback.PlayProcessingRoutine();`
  2. Else: no yield.
  3. Call existing `target.TryCheckSolutionFromInteractor(interactor);` (unchanged).
- Re-fetch `target` after processing in case of teardown; null-guard as today.
- **Spam guard:** while coroutine running, ignore duplicate `Interact` (bool on bridge + warning).

**No prefab breakage:** new field is optional; existing prefabs/scenes without assignment behave as today.

## Scene / Inspector work (you author objects; we wire fields)

**Per panel (Blue / Red)—duplicate setup:**

1. Under that panel’s hierarchy, add a **Processing** group: e.g. `ProcessingBlock` root + child `TMP` (3D TextMeshPro) for the cycling lines. Style like diagnostic. Start with `processingRoot` **inactive** if you want no flash before first Activate; the routine will `SetActive(true/false)`.
2. Add **`ProcessingFeedbackController`** on a suitable static object (e.g. same GameObject as the bridge, or the processing root—your choice). Assign:
   - `processingRoot`, `processingMessageText`
   - `diagnosticDisplayRoot` = the GameObject you want hidden (e.g. parent of the Diagnostic TMP / lamp), **not** necessarily the adapter’s GameObject
   - `processingMessages` / `timePerMessage`
   - `activateInteractable` = the **Solve** / Activate `MonoBehaviour` that implements `IInteractable` for this side (same reference family as `solveButtonInteractable` on the manager—Inspector drag of that component)
3. On **`MultiDimensionPuzzleInteractableBridge`** for that panel: assign the matching `ProcessingFeedbackController`.

**Two sides:** Blue panel bridge → feedback A; Red panel bridge → feedback B (separate components, separate TMPs and optional diagnostic roots).

## Testing

1. Play scene with both bridges’ `processingFeedback` assigned.
2. Press Activate once: diagnostic area hides (history still visible), processing lines cycle, then diagnostic shows new result and history gains a row when the attempt completes.
3. Spam Activate during processing: second press should log warning and do nothing until the sequence ends.
4. Solve puzzle: after success, Activate stays disabled (manager + our re-enable guard).
5. Remove `processingFeedback` reference on one bridge: that side behaves exactly as before (instant check).

## Deliverables summary (for your “after implementation” list)

1. **Scripts:** Add `ProcessingFeedbackController.cs`; edit `MultiSceneDimensionPuzzleInteractableBridge.cs` (typo fix: file is `MultiDimensionPuzzleInteractableBridge.cs`).
2. **GameObjects:** Per panel, one processing root + TMP (you create); optional grouping for diagnostic hide root.
3. **Inspector:** Wire `ProcessingFeedbackController` fields; wire `processingFeedback` on each `MultiDimensionPuzzleInteractableBridge`.
4. **Test:** Steps above in Play mode.
