---
overview: Wire submit-button click audio on Signal, Pipes, and Tutorial panels via root prefabs (PuzzleManager ActivateButtonFeedbackController); all V1/V2 and A/B variants inherit.
date: 2026-07-12
status: implemented
---

# Signal, Pipes & Tutorial submit button audio (ActivateButtonFeedbackController)

## Task name

Wire `clickAudio` + `clickClip` on the Signal, Pipes, and Tutorial machine submit buttons via root panel prefabs so all nested variants inherit.

## Date

2026-07-12

## Scope

- **Scenes:** `Assets/Scenes/Game/Puzzle Signal.unity`, `Assets/Scenes/Game/Puzzle Pipes.unity`, `Assets/Scenes/Game/Tutorial.unity`
- **Fix locations:**
  - `Assets/WhoWiredThis/Prefabs/Panels/Signal_A_V1.prefab` ✅
  - `Assets/WhoWiredThis/Prefabs/Panels/Pipes_A V1.prefab` ✅
  - `Assets/WhoWiredThis/Prefabs/Panels/Tutorial_A V1.prefab` ✅
- **Components:** `PuzzleManager-A` → `ActivateButtonFeedbackController` (`clickAudio`, `clickClip`) + new `AudioSource` on same GameObject.
- **Behavior:** Click sound plays when player activates Solve (submit); already triggered by `MultiDimensionPuzzleInteractableBridge` → `PlayPressFeedbackRoutine()`.

## Out of scope

- Editing `Puzzle Signal.unity` directly (unless scene instances have stale overrides to revert).
- Editing `Signal_A_V2 Variant`, `Signal_B_V1 Variant`, `Signal_B_V2 Variant`, or nested submit-button mesh prefabs (`MultiDimension_Bombilla_1State 1`, etc.).
- Code changes to `ActivateButtonFeedbackController` or bridge scripts.
- Other panel types not listed above.

## Key insight (where audio lives)

Submit click audio is **not** on the visible Bombilla/button mesh. It lives on **`PuzzleManager-A`** (renamed **`PuzzleManager-B`** in the B-side variant):

```
Puzzle Signal.unity
  ├── Signal_A_V2 Variant  ──► … ──► Signal_A_V1 Variant ──► Signal_A_V1.prefab  ← FIX HERE
  └── Signal_B_V2 Variant  ──► … ──► Signal_B_V1 Variant ──► Signal_A_V1.prefab  (same root)
```

**Prefab-of-prefab chain:**

| Asset | Parents from |
|-------|----------------|
| `Signal_A_V1.prefab` | Base (no panel parent) |
| `Signal_A_V1 Variant.prefab` | `Signal_A_V1.prefab` |
| `Signal_B_V1 Variant.prefab` | `Signal_A_V1.prefab` |
| `Signal_A_V2 Variant.prefab` | `Rack Variant` + `Signal_A_V1 Variant` + `DiagnosticPanel Monitor` |
| `Signal_B_V2 Variant.prefab` | `Rack Variant` + `DiagnosticPanel Monitor` + `Signal_B_V1 Variant` |

One fix on **`Signal_A_V1.prefab`** propagates to both Blue and Red panels in Puzzle Signal.

**Runtime flow:**

```
Submit Button (SolveInteractProxy on nested Bombilla)
  → MultiDimensionPuzzleInteractableBridge on PuzzleManager
  → pressFeedback.PlayPressFeedbackRoutine()
  → ActivateButtonFeedbackController plays clickClip via clickAudio
  → (then puzzle processing continues)
```

`visualRoot` on the feedback controller already points at the Bombilla transform (press animation). Only **audio fields are empty** today (`clickAudio: null`, `clickClip: null`).

## Approved implementation steps

1. ✅ Open **`Assets/WhoWiredThis/Prefabs/Panels/Signal_A_V1.prefab`** in Prefab Mode (not the scene).
2. ✅ Select **`PuzzleManager-A`** in the hierarchy.
3. ✅ **Add Component → Audio Source** (if missing):
   - Play On Awake: **off**
   - Spatial Blend: **0** (2D UI-style feedback; avoids distance muting)
   - Volume: tune as needed (e.g. 0.7–1.0)
4. ✅ On **`Activate Button Feedback Controller`** (same object):
   - **Click Audio** → assign the `AudioSource` on `PuzzleManager-A`
   - **Click Clip** → assign your submit click `AudioClip` (project asset of your choice)
5. ✅ Save the prefab.
6. ⬜ Open **`Puzzle Signal.unity`**; select each panel instance (`Signal_A_V2 Variant`, `Signal_B_V2 Variant`).
7. ⬜ If Inspector shows **overrides** on `PuzzleManager` → `ActivateButtonFeedbackController` audio fields, **Revert** those overrides so inheritance applies.
8. ⚠️ Play Mode test (see checklist).

**Do not** add audio to `MultiDimension_Bombilla_1State 1` or other control prefabs for this feature.

## Testing checklist

- ⬜ Enter Play Mode in **Puzzle Signal** with both players / displays as usual.
- ⬜ Focus panel, interact with controls, press **Solve / Activate** on submit button.
- ⬜ Hear click at start of press animation (both A and B panels).
- ⬜ Confirm no double-play or missing sound when puzzle is already solved (bridge should gate).
- ⬜ Spot-check one other scene using `Signal_A_V1 Variant` if any exist.
- ⚠️ Optional: repeat same pattern on `Pipes_A V1.prefab` for pipe submit buttons.

## Rollback notes

- Revert `Signal_A_V1.prefab` in Git, or clear `clickAudio` / `clickClip` and remove added `AudioSource`.
- Scene overrides: Revert panel instances in `Puzzle Signal.unity` if accidentally edited there.
