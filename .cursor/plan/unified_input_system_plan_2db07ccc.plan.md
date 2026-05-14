---
name: Unified Input System Plan
overview: Migrate active gameplay input to a unified Unity Input System action layer so keyboard, mouse, and Xbox-compatible controllers work through the same mappings with minimal hard-coded keys.
todos:
  - id: create-input-bridge
    content: Add a reusable action-based input bridge component that exposes gameplay intents without keycode logic.
    status: completed
  - id: define-gameplay-actions
    content: Create/update Input Actions map with keyboard/mouse and gamepad bindings for gameplay/UI intents.
    status: completed
  - id: refactor-playeractions-input
    content: Replace hard-coded input polling in PlayerActions with input bridge queries while preserving interaction behavior.
    status: completed
  - id: refactor-cameraorbit-look
    content: Switch CameraOrbit look control to action-based look input with gamepad right-stick support.
    status: completed
  - id: wire-samplescene-input
    content: Wire PlayerInput + bridge in SampleScene and verify active control schemes and references.
    status: completed
  - id: validate-cross-device-flow
    content: Test keyboard/mouse and gamepad paths for interaction, UI menu, and camera controls.
    status: completed
isProject: false
---

# Unified Input System Plan

## Goal

Use one input pipeline for `PlayerActions` and camera/UI actions so keyboard, mouse, and controller inputs are mapped via actions (not hard-coded keys/buttons).

## Current State

- `PlayerActions` is fully hard-coded with `Input.GetKeyDown`/`GetMouseButtonDown` in [Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs).
- `CameraOrbit` is mouse-only (`Mouse X/Y`, right-click) in [Assets/WhoWiredThis/Scripts/Player/CameraOrbit.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/CameraOrbit.cs).
- Project already includes Input System assets/components (Starter Assets), so we can reuse that direction rather than re-inventing input handling.

## Implementation Steps

- Add a dedicated gameplay input bridge script (e.g. `PlayerInputBridge`) under `WhoWiredThis.Player`:
  - Holds action-driven state (`Move`, `Look`, `Interact`, `Inventory`, `Help`, `Menu`, `Slot1/2/3`, optional `Restart`, `ToggleSound`).
  - Exposes clean methods/properties (`WasInteractPressedThisFrame`, `WasInventoryPressedThisFrame`, etc.).
  - Avoids direct key codes in gameplay scripts.
- Create/update a project input actions asset for this game flow:
  - Action map `Gameplay`: `Move` (Vector2), `Look` (Vector2), `Interact` (Button), `Inventory` (Button), `Help` (Button), `Menu` (Button), `Slot1/2/3` (Buttons).
  - Bind keyboard/mouse + gamepad equivalents (Xbox-compatible generic mapping).
  - Use `PlayerInput` with control schemes `KeyboardMouse` and `Gamepad`.
- Refactor [PlayerActions.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs):
  - Replace all `Input.GetKeyDown` / mouse button checks with bridge queries.
  - Keep existing nearest-interactable/HUD prompt logic unchanged.
  - Keep UI-click guard behavior (don’t trigger world interaction when pointer is over UI for mouse-originated clicks).
- Refactor [CameraOrbit.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Player/CameraOrbit.cs):
  - Read look input from bridge (`Look` action) so right stick can rotate camera.
  - Keep current mouse UX (free cursor + optional look-while-held) as a configurable mode for desktop.
- Cursor/control-scheme behavior:
  - When using `KeyboardMouse`, keep visible cursor for your UI-heavy flow.
  - When active scheme is `Gamepad`, keep behavior stable (no forced cursor lock toggling loops).
- Scene wiring in [Assets/Scenes/SampleScene.unity](/Users/ilang/git/unity/who-wired-this/Assets/Scenes/SampleScene.unity):
  - Ensure one `PlayerInput` on active player root.
  - Attach bridge and assign actions asset.
  - Keep existing `HUDController` menu wiring; only swap input source.

## Suggested Default Bindings (Generic)

- `Move`: WASD + Left Stick
- `Look`: Mouse Delta + Right Stick
- `Interact`: `E` + Gamepad West (X)
- `Inventory`: `I` + Gamepad North (Y)
- `Help`: `H` 
- `Menu`: `Esc` + Gamepad Start/Menu
- `Slot1/2/3`: keyboard digits + D-pad Left/Up/Right (or shoulder cycling if preferred later)

## Verification

- Keyboard/mouse parity retained (all current actions still work).
- Xbox-compatible controller can:
  - open inventory/help/menu,
  - trigger interactables in range,
  - control camera look,
  - select inventory slots (based on chosen gamepad bindings).
- No compile errors; no duplicate input path conflicts in active scene objects.

