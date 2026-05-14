---
name: Standalone Interact Detector
overview: Add a new standalone Unity script that auto-activates interactables from an explicit Inspector list when a specified detector object is within range, without modifying the existing third-person controller.
todos:
  - id: create-detector-script
    content: Create new AutoInteractDetector MonoBehaviour with inspector fields for detector origin, range, explicit list, and cooldown.
    status: completed
  - id: implement-nearest-selection
    content: Implement nearest-in-range selection from explicit interactable GameObject list and resolve IInteractable components safely.
    status: completed
  - id: implement-auto-activation-guard
    content: Add auto-activation with cooldown/debounce so interactions are not fired every frame while in range.
    status: completed
  - id: add-gizmo-support
    content: Add optional Gizmos radius drawing for detector setup in scene view.
    status: completed
isProject: false
---

# Standalone Interaction Detector Plan

## Goal
Create a new script only (no edits to existing player/controller scripts) that can be attached to a new object under `ThirdPersonGroup` and auto-activates nearby interactables.

## Implementation
- Add a new MonoBehaviour script at [Assets/WhoWiredThis/Scripts/Player/AutoInteractDetector.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/AutoInteractDetector.cs).
- Keep it in namespace `WhoWiredThis.Player` to match project conventions.
- Expose Inspector fields:
  - `Transform detectorOrigin` (the object whose position is checked; defaults to this transform if unset).
  - `float interactRange`.
  - `List<GameObject> interactableObjects` (explicit list only, per your choice).
  - Optional cooldown (`float activationCooldown`) to prevent repeated triggers every frame while in range.
- Runtime behavior:
  - Each `Update`, scan only objects in `interactableObjects`.
  - Find nearest valid entry in range that implements `IInteractable` (via `GetComponent` / parent fallback).
  - Auto-call `Interact(detectorOrigin.gameObject)` on that target when in range.
  - Use per-target cooldown timestamp so the same object is not spam-triggered continuously.
- Keep interaction fully decoupled from [Assets/WhoWiredThis/Scripts/Player/ThirdPersonController.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/ThirdPersonController.cs) (no edits).
- Add optional `OnDrawGizmosSelected` radius visualization for easier scene setup.

## Scene Setup (after script exists)
- Create a child object under `ThirdPersonGroup` (e.g. `InteractionDetector`).
- Attach `AutoInteractDetector`.
- Assign `detectorOrigin` (or leave default).
- Fill `interactableObjects` with the exact interactable scene objects.
- Tune `interactRange` and cooldown.

## Notes
- This keeps your current third-person movement/animation controller intact.
- Because activation is automatic, cooldown is important to avoid re-trigger loops for objects that stay in range.