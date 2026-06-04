---
name: Result Light Sine Indicator Pulse
overview: Add a ResultLight-only component on ResultLight.prefab that sine-pulses the active color subject’s IndicatorLight intensity and retargets when MultiDimension selection changes.
todos:
  - id: multidimension-subject-root-api
    content: Add TryGetSubjectRoot(int, out GameObject) on MultiDimension for child lookup without duplicating subject array
    status: completed
  - id: add-result-light-pulse-script
    content: Add ResultLightIndicatorPulseController with sine min/max intensity, active-subject-only, IndicatorLight child resolve
    status: completed
  - id: update-result-light-prefab
    content: Add component to ResultLight.prefab root; default min/max to match existing point light (e.g. 0.5–3)
    status: completed
  - id: validate-prefab-instances
    content: Play Mode on Tutorial - Visual; cycle RED/ORNG/GREN via SplitMetric or puzzle; confirm pulse follows active subject only
    status: pending
isProject: false
---

# Result light sine indicator pulse

**Date:** 2026-06-04  
**Scope:** Visual polish on [`Assets/WhoWiredThis/Prefabs/Visualizer/ResultLight.prefab`](Assets/WhoWiredThis/Prefabs/Visualizer/ResultLight.prefab) only.  
**Out of scope:** `ResultLightController` (puzzle success/failure), `SplitMetricResultLightsController`, other visualizer prefabs, changes to puzzle logic.

## Goal

On each **ResultLight** bulb, animate the **Unity `Light`** on the **active color subject’s** child `IndicatorLight` so **intensity** oscillates between configurable **min** and **max** using a **sine** curve. When [`MultiDimension`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimension.cs) changes the selected subject (RED / ORNG / GREN), the controller **retargets** to that subject’s `IndicatorLight` and continues pulsing there.

**Approved behavior (user):**
- Pulse **only** while that bulb’s **active subject** is selected (inactive subjects stay hidden via `MultiDimension` — no multi-light pulse).
- **Do not** pulse the general **OFF** object — **color subjects only**.

## Prefab structure (today)

```
ResultLight (root: MultiDimension)
├── OFF (generalObject — out of scope for pulse)
│   └── IndicatorLight (Light)
├── Red (subject 0)
│   └── IndicatorLight (Light, intensity ≈ 3)
├── Orange (subject 1)
│   └── IndicatorLight
└── Green (subject 2)
    └── IndicatorLight
```

[`SplitMetricResultLightsController`](Assets/WhoWiredThis/Scripts/Puzzles/Common/SplitMetricResultLightsController.cs) drives index via `MultiDimension.SetSelection` — no change required; pulse script **reads** the active index.

## Design

### New script: `ResultLightIndicatorPulseController`

**Location:** [`Assets/WhoWiredThis/Scripts/Puzzles/Common/ResultLightIndicatorPulseController.cs`](Assets/WhoWiredThis/Scripts/Puzzles/Common/ResultLightIndicatorPulseController.cs)  
**Namespace:** `WhoWiredThis.Puzzles.Common` (same area as other result-light helpers).

**Placement:** Component on **ResultLight prefab root** (same GameObject as `MultiDimension`).

**Serialized fields**

| Field | Purpose |
|-------|---------|
| `MultiDimension multiDimension` | Auto-resolve `GetComponent<MultiDimension>()` in `Awake` if null |
| `string indicatorChildName` | Default `"IndicatorLight"` — direct child of each **subject** root |
| `float minIntensity` | Floor of sine (e.g. `0.5`) |
| `float maxIntensity` | Ceiling (e.g. `3`, matches prefab default) |
| `float pulseSpeed` | Radians/sec multiplier for `sin(Time.time * pulseSpeed)` |
| `bool pulseOnlyWhenSubjectActive` | Default `true` (locked to approved behavior; can omit toggle and hardcode) |

**Sine mapping**

```csharp
float t = 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed)); // 0..1
activeLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
```

Use **unscaled** time unless you later need pause support (`Time.deltaTime` in play mode is fine).

**Subject change detection**

Each `Update` (or `LateUpdate` after `MultiDimension.ApplyConfiguration`):

1. `int index = multiDimension.GetCurrentIndexForSolutionCheck();`
2. If `index != lastIndex` → `RetargetIndicatorLight(index)`:
   - Resolve subject root via **`MultiDimension.TryGetSubjectRoot(index, out GameObject root)`** (new API below).
   - `root.transform.Find(indicatorChildName)` → `GetComponent<Light>()`.
   - Null-guard + `Debug.LogWarning` once per missing path.
   - Optional: store previous light’s **rest** intensity on retarget (use max as baseline).
3. If `activeLight != null` and subject GameObject is active → apply sine to **only** that `Light`.
4. Do **not** iterate all subjects for intensity when `pulseOnlyWhenSubjectActive` (approved).

**Interaction with visibility**

`MultiDimension` deactivates inactive subject roots — inactive `IndicatorLight` components are off-tree. Retargeting on index change is sufficient; no need to pulse hidden lights.

### Small `MultiDimension` API addition (required)

`GetSubjectGameObject` is private today. Add:

```csharp
public bool TryGetSubjectRoot(int index, out GameObject subjectRoot)
```

- Returns `false` for out-of-range / null subject entries.
- Used only for **read** access; does not change selection semantics.

Keeps pulse script from duplicating the `subjects[]` array or fragile `transform.GetChild(index)` ordering.

## Prefab change

- Add **`ResultLightIndicatorPulseController`** to [`ResultLight.prefab`](Assets/WhoWiredThis/Prefabs/Visualizer/ResultLight.prefab) root.
- Leave **`MultiDimension`** and existing hierarchy unchanged.
- Defaults: `minIntensity = 0.5`, `maxIntensity = 3`, `pulseSpeed = 2` (tune in Inspector after visual pass).
- **No** new references on panel prefabs — nested `ResultLight-Left/Middle/Right` instances inherit from prefab.

## What we will not change

- [`ResultLightController.cs`](Assets/WhoWiredThis/Scripts/Puzzles/Common/ResultLightController.cs) — different use case (single puzzle success/failure); not on this prefab.
- [`SplitMetricResultLightsController`](Assets/WhoWiredThis/Scripts/Puzzles/Common/SplitMetricResultLightsController.cs) — metric colors stay index-based; pulse is independent visual layer on `Light.intensity`.
- Scene-only bridge objects in Tutorial - Visual.

## Testing checklist

- ⬜ Unity compiles; `read_console` shows no new errors.
- ⬜ **Tutorial - Visual** Play Mode: each `ResultLight-Left/Middle` on both panels pulses on the **visible** color only.
- ⬜ Change metric via solve attempts / `SetSelection` (0→1→2): pulse **moves** to the new subject’s `IndicatorLight` without stuck intensity on the old one.
- ⬜ OFF/general object: **no** sine on its light when not a selected subject.
- ⬜ Optional: temporarily set `pulseSpeed = 0` sanity check → intensity holds at mid-lerp.

## Rollback

Remove component from `ResultLight.prefab`; delete `ResultLightIndicatorPulseController.cs`; revert `MultiDimension.TryGetSubjectRoot` if unused elsewhere.

## Risks

| Risk | Mitigation |
|------|------------|
| Missing `IndicatorLight` under a subject | Warn in `RetargetIndicatorLight`; skip pulse until valid |
| URP additional light data | Only adjust `Light.intensity`; prefab already uses point lights |
| Double control with old `ResultLightController` on same object | Not used on this prefab; document “do not add both on one bulb” |

## Implementation order

1. `MultiDimension.TryGetSubjectRoot`
2. `ResultLightIndicatorPulseController`
3. Prefab component + default tuning
4. Play Mode validation on Tutorial - Visual
