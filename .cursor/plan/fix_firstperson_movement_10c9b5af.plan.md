---
name: Fix FirstPerson Movement
overview: Adjust FirstPerson controls so forward/back follows camera-facing direction while left/right performs smooth yaw-only camera rotation, then validate in Single and Dual scenes via Unity MCP play-mode checks.
todos:
  - id: update-fp-controller
    content: Refactor FirstPerson controller movement/rotation to camera-forward + smooth yaw keys
    status: completed
  - id: camera-rig-yaw-hook
    content: Add/adjust camera rig yaw-only API if needed by controller
    status: completed
  - id: validate-and-mcp-test
    content: Run lints and MCP play-mode checks on Single and Dual scenes, then resolve any errors
    status: completed
isProject: false
---

# Fix FirstPerson Keyboard Movement + MCP Validation

## Goal
Update FirstPerson controls to match required behavior:
- `up/down` moves along current camera forward/back direction
- `left/right` rotates view smoothly by yaw only
- no mouse dependency

## Implementation Steps
- Update [`/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonController.cs)
  - Replace current tank-style `transform.Rotate(...)` + `transform.forward` movement coupling with camera-driven movement/turn logic.
  - Use `cameraRig.PlayerCamera.transform.forward` projected to XZ plane for forward/back move vector.
  - Apply smooth yaw rotation from left/right input (using configurable turn speed and `Time.deltaTime`) to the view root via camera rig, not by changing movement keys into strafe.
  - Keep gravity + `CharacterController.Move(...)` flow intact.
  - Keep interact raycast unchanged.

- Optionally extend [`/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonCameraRig.cs`](/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson/Scripts/FirstPersonCameraRig.cs)
  - If needed for cleaner control, add a dedicated yaw method (e.g. `ApplyYaw(float yawDelta)`) so controller can rotate view without pitch.
  - Reuse existing rig references (`yawRoot`, `playerCamera`) and avoid introducing mouse axis input.

- Validate compile/lint
  - Run diagnostics for edited scripts and ensure no C# errors.

## MCP Test Loop
- Clear Unity console.
- Load `Assets/FirstPerson/Scenes/FirstPerson_Single.unity`, enter Play Mode, verify no warnings/errors related to FirstPerson input/movement, stop Play Mode.
- Clear console.
- Load `Assets/FirstPerson/Scenes/FirstPerson_Duel.unity`, enter Play Mode, verify no warnings/errors, stop Play Mode.
- If errors appear, patch scripts and repeat until clean.

## Notes
- Keep control bindings compatible with existing `PlayerControlBindings` assets (no required asset migration).
- Preserve current interaction button behavior (`InteractKey` + raycast).