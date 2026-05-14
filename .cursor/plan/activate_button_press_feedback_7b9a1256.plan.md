---
name: Activate button press feedback
overview: Add a small optional `ActivateButtonFeedbackController` that runs a short scale/offset/highlight/audio coroutine, and yield it at the start of `MultiDimensionPuzzleInteractableBridge.RunActivateFlow` before `ProcessingFeedbackController` and `TryCheckSolutionFromInteractor`—no puzzle, diagnostic, or history logic changes.
todos:
  - id: add-feedback-script
    content: Create ActivateButtonFeedbackController.cs (Transform/RectTransform scale, optional UI Button/Graphic/GameObject highlight, AudioSource, IEnumerator routine, null guards)
    status: pending
  - id: bridge-yield-order
    content: Add optional pressFeedback to MultiDimensionPuzzleInteractableBridge; yield PlayPressFeedbackRoutine before processing then existing TryCheck flow
    status: pending
  - id: manual-scene-wire
    content: Document Editor wiring for Blue/Red Solve roots + bridge.pressFeedback (optional YAML in Split scenes if desired)
    status: pending
  - id: mcp-test-pass
    content: "After wiring scenes: verify via Unity MCP (compile clean read_console, play/stop, logs for bridge order, optional game_view screenshot)"
    status: pending
isProject: false
---

# Activate button press feedback (non-invasive)

## Reality check vs your spec

Solve/Activate in this project is driven by **`IInteractable`**, not necessarily **`UnityEngine.UI.Button`**. [`PanelFocusController.ActivateSelected`](Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs) forwards to **`solveButton.Interactable.Interact(...)`**, which is typically [`MultiDimensionPuzzleInteractableBridge`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs). The bridge already runs **`RunActivateFlow`**: optional **processing** then **`TryCheckSolutionFromInteractor`**, with coroutines hosted on **`PuzzleTarget`** (manager) when possible so disabling the bridge during processing does not stop the flow.

The new feedback script will therefore target a **`Transform`** (works for **3D** mesh hierarchy or **RectTransform** on a canvas) and treat **`UnityEngine.UI.Button`** / **`Graphic`** as **optional** so the same script works for Blue/Red whether or not they use Canvas UI.

## 1. Add `ActivateButtonFeedbackController`

**New file:** [`Assets/WhoWiredThis/Scripts/Visibility/ActivateButtonFeedbackController.cs`](Assets/WhoWiredThis/Scripts/Visibility/ActivateButtonFeedbackController.cs) (same assembly as the bridge; namespace `WhoWiredThis.Visibility`).

**Serialized fields (Inspector):**

| Purpose | Field |
|--------|--------|
| Target to animate | `[SerializeField] Transform visualRoot` (assign the mesh root, `RectTransform`, or the object whose `localScale` should pulse) |
| Optional UI lockout | `[SerializeField] UnityEngine.UI.Button uiButton` (nullable) |
| Optional flash | `[SerializeField] UnityEngine.UI.Graphic highlightGraphic` **or** `[SerializeField] GameObject highlightObject` — pick **one** path in code: if `highlightGraphic` set, lerp `Color` alpha; else if `highlightObject` set, toggle active for `highlightFlashDuration`; else skip |
| Optional audio | `[SerializeField] AudioSource clickAudio` + optional `AudioClip clickClip` (or `Play()` on source only if clip is baked in) |
| Tunables | `pressedScale` (default **0.92**), `pressDuration` (**0.08**), `releaseDuration` (**0.10**), `highlightFlashDuration` (**0.15**), `disableButtonWhileAnimating` (**true**) |
| Optional inward move | `[SerializeField] bool useAnchoredPositionOffset` + `Vector2 anchoredOffset` **only when** `visualRoot is RectTransform`; else optional `Vector3 localPositionOffset` for 3D |

**Awake:** Cache `originalLocalScale`, and if `RectTransform`, cache `originalAnchoredPosition`; null-guard `visualRoot` with warning.

**Public API:** `public IEnumerator PlayPressFeedbackRoutine()` — coroutine-friendly so the bridge can `yield return pressFeedback.PlayPressFeedbackRoutine();`

**Behavior inside routine:**

1. If `disableButtonWhileAnimating` and `uiButton != null`, set `uiButton.interactable = false`.
2. Play `clickAudio` once if assigned (null-safe).
3. Lerp **scale** toward `originalLocalScale * pressedScale` over `pressDuration` (use `Time.unscaledDeltaTime` / realtime so it matches pause behavior used elsewhere).
4. Flash highlight (graphic alpha or GameObject active) over `highlightFlashDuration` without blocking the whole press if you prefer overlap; simplest is: flash **after** press-in or **during** release—document one clear order (e.g. flash on peak of press).
5. Lerp back scale (and optional position) over `releaseDuration`.
6. If step 1 disabled the UI button, set `uiButton.interactable = true` **only if** you disabled it here—do **not** re-enable the **`MultiDimensionPuzzleInteractableBridge`** `MonoBehaviour`; that remains under **`ProcessingFeedbackController`** / **`RestoreActivateIfNeeded`**.

**No** `Instantiate`, **no** `FindObjectOfType`, **no** scene name strings.

## 2. Integrate at the single choke point (bridge)

**Edit:** [`MultiDimensionPuzzleInteractableBridge.cs`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs)

- Add optional `[SerializeField] ActivateButtonFeedbackController pressFeedback;` under **Optional feedback**.
- At the **start** of `RunActivateFlow`’s `try` block (after `activateFlowRunning = true` is already set in today’s code), insert:

  `if (pressFeedback != null) { yield return pressFeedback.PlayPressFeedbackRoutine(); }`

  **before** `if (processingFeedback != null) yield return processingFeedback.PlayProcessingRoutine();`

- If `pressFeedback` is **null**, behavior is unchanged.

This satisfies: **press → processing (if any) → existing check** without touching [`MultiDimensionPuzzelManager`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs), adapters, or history.

**Conflict check:** `ProcessingFeedbackController` may set `activateInteractable.enabled = false` on the **bridge** (same object as `IInteractable`). Press feedback must **not** toggle `bridge.enabled`; it only animates **`visualRoot`** and optional **`uiButton.interactable`**. That avoids breaking the coroutine host pattern.

## 3. Scene / prefab wiring (manual, as you prefer)

For **each** Solve/Activate bridge instance (Blue and Red):

1. Add **`ActivateButtonFeedbackController`** on a stable GameObject (often the **Solve button root** or a small child under it).
2. Assign **`visualRoot`** to the transform you want scaled (child mesh, frame, or `RectTransform`).
3. Optionally assign **`uiButton`** only if that Solve control is a real UI `Button` (may be empty in current 3D setup).
4. Optionally assign **`highlightGraphic`** or **`highlightObject`** for the frame flash you add in the scene.
5. Optionally assign **`clickAudio`** (+ clip if needed).
6. On the same object’s **`MultiDimensionPuzzleInteractableBridge`**, assign **`pressFeedback`** to this controller.

No YAML edits are strictly required in the plan deliverable if you prefer wiring in Editor; optionally we can pre-wire known scenes in a follow-up.

## 4. Testing (puzzle logic unchanged)

### 4.1 Manual in-Editor checks

1. Open a scene with two panels (e.g. Split Tutorial).
2. Confirm **one** press animation plays per Activate, then existing **processing lines** (if assigned), then diagnostic/history update as today.
3. Mash Activate during solve flow: second press should still be ignored while `activateFlowRunning` is true (existing guard).
4. Solve the puzzle: Activate should remain disabled per existing manager/bridge behavior; press feedback should not throw when `pressFeedback` is null on other scenes.

### 4.2 Automated / agent verification with **Unity MCP** (user-unityMCP)

Use MCP **after** scripts compile and scenes are wired. Follow the server’s workflow: resolve **active Unity instance** if multiple; after script edits use **`read_console`** for compile errors before relying on Play Mode.

| Step | MCP tool / resource | What to verify |
|------|---------------------|----------------|
| A. Compilation | **`read_console`** (`action: get`, filter `error` / `warning`, modest `count`) | No new C# errors from `ActivateButtonFeedbackController` or bridge changes. |
| B. Editor state | **`mcpforunity://editor_state`** (resource) or equivalent project state read | Editor not stuck compiling; know `isPlaying` before driving play. |
| C. Enter Play Mode | **`manage_editor`** (`action: play`) | Scene runs; stop with `action: stop` when done. |
| D. Runtime logs | **`read_console`** after triggering Blue then Red **Activate** from the game (or note that human must press if MCP cannot simulate input) | Expect existing **`[MultiDimensionPuzzleInteractableBridge]`** “starting activate flow” log **after** press feedback would complete; optional temporary **`Debug.Log`** in bridge (only if needed during bring-up) to confirm order: **press → processing → TryCheck**—remove before ship if added. |
| E. Visual sanity (optional) | **`manage_camera`** (`action: screenshot`, `capture_source: game_view`, `include_image: true`, sensible `max_resolution`) | Confirms Game view is rendering; **pick the main gameplay camera** if the tool defaults to a wrong camera (e.g. minimap)—adjust `camera` / `target` per tool schema. |
| F. Hierarchy spot-check (optional) | **`find_gameobjects`** / **`manage_gameobject`** (read-only queries per tool docs) | `pressFeedback` assigned on both bridges if validating scene YAML from agent side. |

**Limits:** MCP cannot replace human judgment of **scale/highlight timing**; use **Game view + Scene view** for feel. If **Play** cannot be driven safely in CI, skip step C and rely on A + manual play.

## 5. Deliverable summary (what you will tell the user after implementation)

After coding, the implementation response should list exactly:

1. **Scripts added/changed** (new `ActivateButtonFeedbackController.cs`, edited `MultiDimensionPuzzleInteractableBridge.cs`).
2. **Where to put the component** (on each Solve/Activate hierarchy; bridge references it).
3. **Blue Inspector fields** (visual root, optional UI button, optional highlight, optional audio, tunables, bridge `pressFeedback` link).
4. **Red Inspector fields** (same pattern, different object references).
5. **Test steps** (press once → see animation → processing if any → result; unsolved → Activate works again; solved → stays off), **plus** MCP steps in **§4.2** (`read_console`, `manage_editor` play/stop, optional `manage_camera` screenshot).
