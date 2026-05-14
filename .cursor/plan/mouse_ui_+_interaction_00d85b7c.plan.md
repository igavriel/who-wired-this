---
name: Mouse UI + Interaction
overview: Enable always-visible mouse usage in SampleScene for UI clicks, while supporting nearby interactable activation by Left Click and E with UI-priority click blocking.
todos:
  - id: update-playeractions-input
    content: Extend PlayerActions interaction input to support Left Click + E and keep nearest interactable/HUD behavior.
    status: completed
  - id: add-ui-priority-guard
    content: Block world interaction on mouse clicks when pointer is over UI using EventSystem.IsPointerOverGameObject().
    status: completed
  - id: enforce-cursor-visibility
    content: Set cursor unlocked/visible in PlayerActions Start for scene-level reliability.
    status: completed
  - id: verify-samplescene-wiring
    content: Confirm SampleScene uses PlayerActions and has one active EventSystem for UI click handling.
    status: completed
isProject: false
---

# Mouse UI And Nearby Interaction Plan

## Goal
Make the mouse usable in `SampleScene` for UI/menu buttons, and allow nearby interactables to be activated by either Left Click or `E`, while preventing world interaction when clicking on UI.

## Current Findings
- Cursor visibility is already set in [CameraOrbit.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/CameraOrbit.cs) (`Cursor.lockState = None`, `Cursor.visible = true` in `Start`).
- Interaction logic currently lives in [PlayerActions.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs) and triggers on `E` only.
- `SampleScene` already has an EventSystem (`StandaloneInputModule`), so UI click routing exists.

## Implementation Steps
- Update [PlayerActions.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs):
  - Add `using UnityEngine.EventSystems;`.
  - In interaction handling, treat activation input as: `Input.GetKeyDown(KeyCode.E)` OR `Input.GetMouseButtonDown(0)`.
  - Before mouse-triggered world interaction, check `EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()`.
  - If pointer is over UI and input is left click, skip world interaction (UI-priority behavior).
  - Keep nearest-interactable selection and HUD prompt update logic unchanged.
- Add a defensive cursor setup in [PlayerActions.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs):
  - In `Start`, enforce `Cursor.lockState = CursorLockMode.None; Cursor.visible = true;`.
  - This guarantees behavior in `SampleScene` even if another component later changes cursor state.
- Validate scene wiring in [SampleScene.unity](/Users/ilang/git/unity/who-wired-this/Assets/Scenes/SampleScene.unity):
  - Ensure active player object uses `PlayerActions` for interaction.
  - Ensure one active EventSystem remains.

## Verification
- In Play Mode:
  - Cursor remains visible.
  - UI buttons/menu are clickable by left mouse.
  - Left-click in world activates nearest interactable when in range.
  - Pressing `E` also activates nearest interactable.
  - Clicking on UI does not also activate nearby world interactables.