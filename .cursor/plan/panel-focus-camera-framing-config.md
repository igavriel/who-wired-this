---
name: Panel Focus Camera Framing Config
overview: Unify panel focus camera framing across Tutorial and Tutorial - Visual via PanelFocusController Inspector fields, with renderer-driven orientation as the default.
date: 2026-05-30
status: implemented
---

# Panel focus camera framing configuration

## Task name

Configurable camera focus framing for Tutorial + Tutorial - Visual

## Date

2026-05-30

## Problem (manual test failure)

In **Tutorial - Visual**, `PanelFocusController` lives on **Board-A/B** (legacy 76° tilted collider mesh) while **Board Renderer** points at **Plane-Transparent** under **Table_Operations**. `GetCameraSnapPose` previously used **Board's rotation** with **Plane-Transparent bounds center**, so the camera snapped near the panel root looking toward the floor instead of framing the transparent screen.

**Tutorial.unity** worked because Board and boardRenderer are the **same GameObject** (rotation and bounds aligned).

## Scope

- Fix [`PanelFocusController.GetCameraSnapPose`](../../Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs) to derive orientation/extents from the framing target, not always `this.transform`
- Add optional **Framing Transform** Inspector override
- Keep existing **Board Renderer**, **Frame Fill Percent**, **Extra Distance** fields
- No prefab apply required; Tutorial scenes keep current wiring

## Out of scope

- Moving button highlight anchors (unless broken after retest)
- Replacing Board-A collider with mesh collider on Table_Operations (follow-up if interact raycast misses)
- Runtime instantiation of focus anchors

## Implementation (done)

### Framing resolution order

1. **Framing Transform** (optional explicit override)
2. Else **Board Renderer → transform** (Plane-Transparent in Visual; same object in Tutorial)
3. Else **PanelFocusController transform** (Board fallback)

### Inspector fields (`PanelFocusController` → Camera Framing)

| Field | Tutorial | Tutorial - Visual |
|-------|----------|-------------------|
| **Board Renderer** | Board's own MeshRenderer (same object) | **Plane-Transparent-A/B** MeshRenderer (scene instance override) |
| **View Axis** | **Forward** (default) | **Down** (plane mesh normal points -Z; camera approaches from +Z) |
| **Framing Transform** | Leave empty | Leave empty |
| **Frame Fill Percent** | 54 | 54 |
| **Extra Distance** | 0.02 | 0.02 |

### Root cause (MCP verified)

1. Scene `boardRenderer` YAML override used prefab asset fileID — resolved to prefab path, not MeshRenderer (null/wrong at runtime).
2. Plane mesh uses rotated axes; **Forward** view axis pointed camera at floor (Y=-5).
3. Bounds extent math used local X/Y on tilted plane — distance too far (~7m vs ~2m).

### Code fix (PanelFocusController)

- `PanelFocusViewAxis` enum: Forward / Back / Up / Down / Right / Left
- Framing from board-renderer transform (not Board collider)
- View-plane bounds projection when view axis ≠ Forward or renderer is on another object

### Scene wiring checklist

**Tutorial.unity** — no change required (inline Board; boardRenderer on same object).

**Tutorial - Visual.unity** — already has scene overrides:

- `Player1_Panel-A` / `Player2_Panel-B` → `Board-A/B` → `boardRenderer` → `Plane-Transparent` MeshRenderer

If re-enter interact misses the 3D mesh, adjust **Board-A/B** BoxCollider position/size in the scene instance (not prefab asset).

## Testing checklist

- ⬜ Tutorial: Play → both players start focused; camera frames panel; exit/re-enter; controls work
- ⬜ Tutorial - Visual: Play → cameras frame Plane-Transparent on Table_Operations (not floor)
- ⬜ Both: player isolation (A cannot focus B panel)
- ⬜ Tune Frame Fill Percent on Visual only if panel too small/large in view

## Rollback

- Revert [`PanelFocusController.cs`](../../Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs) to use `transform.rotation` only
- Scene `boardRenderer` overrides in Tutorial - Visual remain harmless
