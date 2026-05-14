---
name: Retarget EasyStart To MyPlayer
overview: Move EasyStart Third Person movement + animation behavior from the existing ThirdPersonController setup onto MyPlayer (astra-prefab) in TestScene, while preserving a rollback path and verifying play behavior with focused manual tests.
todos:
  - id: backup-scene
    content: Create a backup of TestScene before migration changes.
    status: completed
  - id: swap-controller-stack
    content: Replace MyPlayer Mixamo controller components with EasyStart ThirdPersonController and keep CharacterController tuned.
    status: completed
  - id: retarget-animator
    content: Bind PlayerAnimator.controller to astra Animator and validate parameter/rig compatibility.
    status: completed
  - id: fix-tag-camera-deps
    content: Apply Player tag and verify camera-relative movement dependencies.
    status: completed
  - id: playmode-test-pass
    content: Run movement/animation/regression checks and finalize by deactivating duplicate ThirdPersonController object.
    status: completed
isProject: false
---

# Retarget EasyStart Controller to MyPlayer

## Goal
Use EasyStart's movement/animation stack (`ThirdPersonController` + `PlayerAnimator.controller`) on `MyPlayer` in `TestScene`, replacing the current Mixamo-specific controller pipeline.

## Current State (verified)
- EasyStart prefab root uses:
  - [`Assets/EasyStart Third Person Controller/Scripts/ThirdPersonController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/EasyStart Third Person Controller/Scripts/ThirdPersonController.cs)
  - Animator controller [`Assets/EasyStart Third Person Controller/Prefabs/Source/Animations/PlayerAnimator.controller`](/Users/ilang/git/unity/who-wired-this/Assets/EasyStart Third Person Controller/Prefabs/Source/Animations/PlayerAnimator.controller)
- `TestScene` currently has:
  - `ThirdPersonController` prefab instance (EasyStart) already present
  - `MyPlayer` with `astra-prefab` child and Mixamo stack (`ThirdPersonMixamo.PlayerController`, `ThirdPersonAnimatorBridge`, `ThirdPersonPlayerAudio`) configured via scene YAML in [`Assets/ThirdPersonMixamo/TestScene.unity`](/Users/ilang/git/unity/who-wired-this/Assets/ThirdPersonMixamo/TestScene.unity)

## Implementation Plan
1. **Create safe rollback snapshot**
   - Duplicate `TestScene` as a backup scene variant before migration.
   - Keep `ThirdPersonController` instance temporarily as reference until verification passes.

2. **Retarget `MyPlayer` to EasyStart movement logic**
   - On `MyPlayer`, remove/disable Mixamo movement stack:
     - `ThirdPersonMixamo.PlayerController`
     - `ThirdPersonMixamo.ThirdPersonAnimatorBridge`
     - `ThirdPersonMixamo.ThirdPersonPlayerAudio` (optional to keep if desired)
   - Ensure `CharacterController` remains on `MyPlayer` and tune values to EasyStart-compatible defaults (height/center/radius as needed).
   - Add `ThirdPersonController` component from EasyStart to `MyPlayer`.

3. **Retarget animator on astra rig**
   - Find the actual `Animator` used by the `astra-prefab` hierarchy (likely child, not root).
   - Assign EasyStart animator controller (`PlayerAnimator.controller`) to that animator.
   - Confirm animator parameters expected by EasyStart logic exist and update in Play Mode (`run`, `sprint`, `air`, `crouch`).
   - Validate avatar/humanoid mapping on astra importer so clips animate the rig correctly (no T-pose).

4. **Resolve camera/tag dependencies**
   - Set `MyPlayer` tag to `Player` because EasyStart camera script and logic assume this.
   - Keep current camera rig initially, then verify camera-relative movement is correct (`Camera.main` usage in EasyStart controller).
   - If movement direction is wrong, align to EasyStart camera setup or explicitly set the main camera used by gameplay.

5. **Deactivate duplicate player source**
   - After `MyPlayer` is validated, disable or remove scene `ThirdPersonController` instance so only one controllable player remains.
   - Reconfirm there is only one `Player` tag object and one active movement script instance.

## Testing Plan
- **Compile/scripting sanity**
  - Enter Play Mode and confirm no console errors from missing animator/component references.

- **Movement parity tests**
  - Walk (`WASD`): character moves and rotates toward camera-relative direction.
  - Sprint (`Left Shift` / `Fire3`): speed increase and sprint animation trigger.
  - Jump (`Space` / `Jump` axis): jump arc works; `air` animation true while airborne.
  - Crouch (`Left Ctrl`): crouch toggle and movement slowdown apply.

- **Animator behavior checks**
  - Verify transitions idle/walk/run/sprint/air/crouch on `astra-prefab` without broken pose.
  - Confirm no foot sliding worse than baseline; root motion remains disabled.

- **Scene integrity checks**
  - Confirm only `MyPlayer` receives input.
  - Confirm camera follow/aim remains stable and no object clipping regressions around start area.

- **Regression checks**
  - Reopen scene and rerun brief smoke test to ensure serialized references persisted.

## Acceptance Criteria
- `MyPlayer` (with `astra-prefab`) uses EasyStart movement logic and animation controller.
- Character responds to move/sprint/jump/crouch with matching animation states.
- No console errors/warnings related to missing animator/controller references.
- Old EasyStart `ThirdPersonController` scene instance is no longer the active playable character.