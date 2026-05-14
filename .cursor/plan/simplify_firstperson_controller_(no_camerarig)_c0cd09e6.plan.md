---
name: Simplify FirstPerson Controller (No CameraRig)
overview: Refactor FirstPersonController to require explicit camera/input assignments, remove CameraRig dependency and all fallback paths, then verify Single/Duel scenes through MCP play-mode checks.
todos: []
isProject: false
---

# Simplify FirstPerson Controller (No CameraRig)

## Goal
Refactor FirstPerson movement to work with the rebuilt prefab structure (player + camera only), remove `FirstPersonCameraRig` dependency from `FirstPersonController`, and enforce required references with asserts and no fallback paths.

## Scope
- Update controller logic in [`/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs).
- Keep `FirstPersonCameraRig` script untouched unless a cleanup pass is requested later.

## Implementation Steps
- In [`/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs):
  - Remove serialized `cameraRig` field and all `FirstPersonCameraRig` references.
  - Add serialized `Camera playerCamera` field (required explicit assignment in prefab).
  - Add `Debug.Assert(...)` checks in `Awake` for:
    - `_characterController` presence (still obtained via `GetComponent<CharacterController>()`)
    - `inputBindings` assigned
    - `playerCamera` assigned
  - Remove all fallback behavior (`if null then ...`) for input keys, camera forward, and interaction camera.
  - Use bound keys directly from `inputBindings` (no default key fallbacks).
  - Keep keyboard behavior:
    - left/right rotates player yaw smoothly (`transform.Rotate` with `turnSpeed * deltaTime`)
    - forward/back moves along `playerCamera.transform.forward` projected to XZ plane.
  - Keep gravity/grounding flow and `CharacterController.Move`.
  - Keep interaction raycast and use `playerCamera` for ray origin/direction.

- Optional tiny cleanup in same file:
  - Rename key properties/locals for clarity if needed (`TurnLeftKey`/`TurnRightKey` style), while preserving serialized data compatibility.

## Validation
- Run diagnostics for updated script(s) with lints.
- MCP runtime validation loop:
  - Clear console.
  - Load `Assets/FirstPerson/Scenes/FirstPerson_Single.unity` → Play → check warnings/errors → Stop.
  - Clear console.
  - Load `Assets/FirstPerson/Scenes/FirstPerson_Duel.unity` → Play → check warnings/errors → Stop.
  - If issues appear, patch and re-run until clean.

## Expected Outcome
- `FirstPersonController` is self-contained for player+camera prefabs.
- No hidden fallbacks: missing setup is immediately visible via asserts.
- Movement and interaction behavior remains keyboard-driven and scene-verified through MCP.