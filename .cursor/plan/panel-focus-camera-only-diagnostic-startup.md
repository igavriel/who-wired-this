---
name: Panel focus camera-only diagnostic startup
overview: Add TryEnterCameraFocus on PlayerPanelFocusController and bootstrap diagnostic branch so observer startup uses PanelFocusCamera only (movement locked) without a dummy PanelFocusController on Diagnostic-Focus.
date: 2026-06-27
status: implemented
---

# Panel focus — camera-only diagnostic startup

## Task name

Camera-only diagnostic startup focus (observer locked view, no `PanelFocusController` on diagnostic surfaces).

## Date

2026-06-27

## Scope

- `PlayerPanelFocusController.TryEnterCameraFocus(PanelFocusCamera)` — snap camera, disable FPS/`PlayerActions`, no panel selection or `OnFocusEntered`.
- `InitialPanelFocusBootstrap` — when `diagnostic == true` and no `PanelFocusController` resolves from the diagnostic camera binding, call camera-only focus instead of skipping.
- Preserve full `TryEnterFocus(panel)` when a diagnostic `PanelFocusController` is present (backward compatible).
- Update Inspector tooltips on bootstrap diagnostic camera field.

## Out of scope

- Exit/re-enter UX for camera-only focus at runtime (startup only for now).
- Prefab/scene wiring changes (Tutorial `diagnosticCamera` refs already assigned).
- Removing empty diagnostic `PanelFocusController` from any prior workaround prefabs (reverted).

## Approved implementation steps

1. ✅ Add `isCameraOnlyFocus` + `TryEnterCameraFocus` on `PlayerPanelFocusController`; skip panel input in `Update` when camera-only; `ExitFocus` restores without `OnFocusExited`.
2. ✅ Branch `TryEnterStartupFocus` for diagnostic camera-only vs full panel focus.
3. ✅ Unity compile + Play Mode spot-check Tutorial operator/diagnostic startup.

## Testing checklist

- ✅ Unity compiles with zero errors
- ⚠️ Tutorial Play Mode — Player A full panel focus; Player B camera-only on `Diagnostic-Focus` (no bootstrap skip warning)
- ⚠️ Legacy scenes with diagnostics unset — both-panels mode unchanged

## Rollback notes

Revert `PlayerPanelFocusController.cs` and `InitialPanelFocusBootstrap.cs`; no prefab changes required.
