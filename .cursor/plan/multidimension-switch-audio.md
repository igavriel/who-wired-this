---
overview: Optional per-control switch audio on MultiDimension — random clip + subtle pitch/volume variation when the player advances the subject index; skip when unconfigured.
date: 2026-07-11
status: implemented
---

# MultiDimension switch audio

## Task name

Optional switch-change sounds on `MultiDimension` controls.

## Date

2026-07-11

## Scope

- Per-`MultiDimension` Inspector option: list of `AudioClip`s, optional `AudioSource`, subtle pitch/volume randomization.
- Play **one random clip** when the **player cycles** the control (subject index changes via `AdvanceIndexForPlayer`).
- If clips list is empty / all null / no usable `AudioSource` → **silently skip** (no warnings in normal play).
- Small reusable playback helper (not a scene singleton).

## Out of scope

- Retrofitting every MultiDimension prefab with clips (wiring is per-prefab, optional).
- Sounds on programmatic index changes (`SetSelection` / `SetActiveSubjectIndex` from lamps, diagnostic readouts, submit-lever feedback, combination bridge).
- New global audio mixer bus, pooling system, or addressables pipeline.
- Reverse-cycle / `RetreatIndexForPlayer` (not in repo yet; hook the same path when added).

## TLDR

Add an optional **Audio** block on each `MultiDimension`. When a **player** advances the switch (`AdvanceIndexForPlayer` → index actually changes), pick a **random clip** from the list and `PlayOneShot` with **small pitch/volume jitter**. No clips configured = no sound. Use a tiny **`MultiDimensionSwitchAudioPlayer`** helper; do **not** play on lamp/readout/lever programmatic updates.

## Architecture

```
MultiDimensionSubjectCycler.Interact()
        │
        ▼
MultiDimension.AdvanceIndexForPlayer()   ← only user-driven entry point
        │  (index changes?)
        ▼
MultiDimensionSwitchAudioPlayer.TryPlay(settings)
        │  random clip + pitch/vol variation
        ▼
AudioSource.PlayOneShot(...)
        │
        ▼
SetSelection(...) → ApplyConfiguration()   (existing visibility; unchanged)
```

### Why only `AdvanceIndexForPlayer`?

`SetSelection` / `SetActiveSubjectIndex` are also used by:

- `DiagnosticDisplayController` (lamp states)
- `SubmittedCombinationMultiDimensionBridge` (passive readout)
- `SplitResultPipesController` / `SplitResultTutorialController`
- `SubmitLeverMultiDimensionFeedback`

Those are **not** player knob/switch interactions and should stay silent unless we add an explicit opt-in later.

## Proposed files

| File | Action |
|------|--------|
| `Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionSwitchAudioSettings.cs` | **Add** — `[Serializable]` settings struct/class |
| `Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionSwitchAudioPlayer.cs` | **Add** — static `TryPlay` helper |
| `Assets/WhoWiredThis/Scripts/Visibility/MultiDimension.cs` | **Modify** — serialized audio settings + call site in `AdvanceIndexForPlayer` |
| `Assets/WhoWiredThis/Editor/MultiDimensionEditor.cs` | **Optional** — foldout / ranges for audio section (default `PropertyField` is fine for v1) |

## Inspector design (`MultiDimensionSwitchAudioSettings`)

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `enabled` | `bool` | `true` | Master toggle per control |
| `audioSource` | `AudioSource` | null | Optional; fallback `GetComponent<AudioSource>()` on same GameObject |
| `clips` | `AudioClip[]` | empty | Random pick among non-null entries |
| `pitchMin` / `pitchMax` | `float` | `0.94` / `1.06` | Subtle variation |
| `volumeMin` / `volumeMax` | `float` | `0.88` / `1.0` | Subtle variation |

**Skip rules (no log spam):**

- `enabled == false`
- `clips` null or length 0
- all clips null
- no `AudioSource` resolved
- index did not change (advance blocked by lock, wrong player, or `SubjectCount == 0`)

**Dev-only warning:** if `enabled` and clips assigned but no `AudioSource` found — `Debug.LogWarning` once per component (optional, editor/play mode).

## Playback helper behavior

`MultiDimensionSwitchAudioPlayer.TryPlay(in MultiDimensionSwitchAudioSettings settings)`:

1. Validate skip rules.
2. Pick `clip = clips[Random.Range(0, clips.Length)]` (retry or filter nulls).
3. `pitch = Random.Range(pitchMin, pitchMax)`.
4. `volume = Random.Range(volumeMin, volumeMax)`.
5. `audioSource.pitch = pitch` then `PlayOneShot(clip, volume)` **or** use `PlayOneShot` volume arg and reset pitch after (match `ActivateButtonFeedbackController` style).

Prefer resetting `audioSource.pitch` to `1f` after one-shot so other sounds on the same source are unaffected.

## `MultiDimension.cs` change (minimal)

In `AdvanceIndexForPlayer`:

```csharp
int previousIndex = activeSubjectIndex;
// ... existing guards ...
activeSubjectIndex = (activeSubjectIndex + 1) % n;
if (previousIndex != activeSubjectIndex)
{
    MultiDimensionSwitchAudioPlayer.TryPlay(switchAudio);
}
SetSelection(visibleToPlayer, activeSubjectIndex);
```

Add header:

```csharp
[Header("Audio (optional)")]
[SerializeField] private MultiDimensionSwitchAudioSettings switchAudio;
```

No change to public API signatures.

## Prefab / scene wiring (manual, per control)

For each knob/slider/switch that should click:

1. Ensure an `AudioSource` on the control root (or child); **Play On Awake** off, **Spatial Blend** as desired.
2. Assign 1–N switch clips (e.g. mechanical click variants).
3. Leave audio empty on controls that should stay silent (lamps, passive displays).

Reference pattern: `ActivateButtonFeedbackController` (`AudioSource` + `AudioClip`).

## Risks

| Risk | Mitigation |
|------|------------|
| Lamp/readout index changes play clicks | Restrict trigger to `AdvanceIndexForPlayer` only |
| Double sound if cycler + another feedback both fire | v1: only MultiDimension plays; document; defer bridge changes |
| `OnValidate` / `ApplyConfiguration` noise | Do not hook audio in `ApplyConfiguration` |
| Rapid cycling stacks `PlayOneShot` | Acceptable for v1; optional cooldown field later |

## Approved implementation steps

1. ✅ Add `MultiDimensionSwitchAudioSettings` + `MultiDimensionSwitchAudioPlayer`.
2. ✅ Wire `AdvanceIndexForPlayer` with index-change guard.
3. ✅ Unity compile + console check.
4. ✅ Wire **one** test prefab (e.g. `MultiDimension_Knob_3State`) with clips + `AudioSource`.
5. ⚠️ Manual Play Mode: cycle control on correct player panel; confirm random clip + variation; confirm lamp/readout still silent.
6. ⬜ Roll clips to other controls as needed (not blocking for v1 code).

## Testing checklist

- ⬜ Control with clips: each player advance plays a sound.
- ⬜ Control with empty clips: silent, no errors.
- ⬜ `interactionLocked == true`: no sound, no index change.
- ⬜ Diagnostic lamp `SetSelection`: no sound.
- ⬜ Submit lever programmatic `SetActiveSubjectIndex`: no sound.
- ⬜ Two players: wrong-player advance still blocked (existing behavior) and silent.

## Rollback notes

- Revert the three script files; remove optional `AudioSource` / clip assignments from prefabs.
- No scene architecture or puzzle-manager contract changes.
