---
name: Tutorial Visual Panel Focus
overview: Scene-local boardRenderer wiring plus PanelFocusController framing fix so camera uses Plane-Transparent orientation in Tutorial - Visual.
date: 2026-05-30
status: implemented
---

# Tutorial - Visual panel focus wiring

## Task name

Tutorial - Visual panel focus (Plane-Transparent camera framing)

## Date

2026-05-30

## Root cause (manual test)

Camera looked at the floor because `GetCameraSnapPose` used **Board-A's 76° rotation** while centering on **Plane-Transparent** bounds. Fixed in [`panel-focus-camera-framing-config.md`](panel-focus-camera-framing-config.md).

## Scope

- Scene-only `boardRenderer` overrides in [`Tutorial - Visual.unity`](../../Assets/Scenes/Tutorial%20-%20Visual.unity)
- Framing fix in [`PanelFocusController.cs`](../../Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs)

## Testing checklist

- ⬜ Tutorial - Visual: cameras frame Plane-Transparent (not floor)
- ⬜ Tutorial: unchanged behavior
- ⬜ Exit/re-enter, selection, Solve, Exit, player isolation

## Rollback

- Revert scene + `PanelFocusController.cs` from Git
