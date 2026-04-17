# ThirdPersonMixamo

Self-contained third-person sample package (Mixamo **astra** visual, forked movement/camera, local split-screen **duel / two-player** view). All gameplay scripts use **`namespace ThirdPersonMixamo`**.

## First-time setup (required once)

1. Open this Unity project in the Editor.
2. Run menu **ThirdPersonMixamo → Rebuild Package Assets (Prefab + Scenes)**.  
   This generates:
   - `Prefabs/ThirdPersonMixamoPlayer.prefab` (CharacterController + `PlayerController` + animator/audio helpers + nested `Assets/Mixamo/astra-prefab.prefab` with `ThirdPersonMixamoAnimator.controller`).
   - `ThirdPersonMixamo_Single.unity` — one player, full-screen camera, jump gym boxes.
   - `ThirdPersonMixamo_LocalDuel.unity` — two players, horizontal split viewports, **Player A / Player B** bindings, **one** `AudioListener` (left camera only).
   - Appends both scenes to **File → Build Settings** (non-destructive append).

If Unity was already running another instance, close it and run the menu command again.

## Architecture

| Piece | Role |
|-------|------|
| `PlayerControlBindings` | ScriptableObject: move / sprint / interact / **jump** keys. Assets: `Data/PlayerControlBindings_PlayerA.asset`, `PlayerControlBindings_PlayerB.asset`. |
| `PlayerController` | `CharacterController` movement, camera-relative WASD, sprint, gravity, **jump**, events `JumpStarted` / `Landed`. |
| `PlayerCameraRig` | Late-update follow camera: yaw lock option, distance/height, look-at offset. |
| `ThirdPersonAnimatorBridge` | Drives Starter-style Animator parameters (`Speed`, `Grounded`, `Jump`, `FreeFall`, `MotionSpeed`) from controller velocity. |
| `ThirdPersonPlayerAudio` | Footstep cadence + land one-shots using clips under `Audio/`. |
| `Animations/ThirdPersonMixamoAnimator.controller` | Duplicate of Starter sample controller; motion clips still reference Starter FBX GUIDs until you optionally remap copies. |
| `Audio/` | Duplicated footstep/land `.wav` files (new GUIDs). |

```text
ThirdPersonMixamo/
  ThirdPersonMixamo_Single.unity
  ThirdPersonMixamo_LocalDuel.unity
  Prefabs/          (generated player prefab)
  Animations/       (ThirdPersonMixamoAnimator.controller)
  Audio/            (duplicated SFX)
  Data/             (PlayerControlBindings Player A / B)
  Scripts/          (runtime types)
  Editor/           (ThirdPersonMixamoBuildMenu — rebuild generator)
  Doc/README.md
```

## Sample scenes

| Scene | Purpose |
|-------|---------|
| `ThirdPersonMixamo_Single.unity` | Single player, **Player A** keys (WASD, Left Shift sprint, **Space** jump). |
| `ThirdPersonMixamo_LocalDuel.unity` | **Player A** (WASD + Space jump) vs **Player B** (arrow keys + Right Shift sprint + **Keypad 0** jump). |

## Wiring your own scene

1. Instantiate `Prefabs/ThirdPersonMixamoPlayer.prefab` (after rebuild).
2. Add a ground collider (large cube/plane).
3. Create a **Camera** on its own GameObject:
   - Add `PlayerCameraRig`, assign **Target** = player root transform (the object with `CharacterController`).
   - Add `AudioListener` on that camera (single-player).
4. On `PlayerController`, assign **Camera Transform** = the camera’s transform.
5. Assign **Input Bindings** = a `PlayerControlBindings` asset (`Player A` or `Player B`).

### Local split-screen (two players)

- Duplicate cameras; set **Rect** on each `Camera` component to split the viewport (e.g. left `(0, 0, 0.5, 1)` and right `(0.5, 0, 0.5, 1)`).
- Each camera gets its own `PlayerCameraRig` targeting the correct player.
- **Disable** `AudioListener` on the second camera (Unity allows only one active listener).

## Controls (defaults)

**Player A:** W A S D, Left Shift sprint, Left Control interact, **Space** jump.  
**Player B:** Arrow keys, Right Shift sprint, Right Control interact, **Keypad 0** jump.

## QA checklist (quick)

- [ ] No compile errors after pull.
- [ ] Ran **Rebuild Package Assets** once.
- [ ] Single scene: move, sprint, jump, land SFX, camera follow.
- [ ] Two-player (`LocalDuel`) scene: split view, independent controls, no duplicate listener warning.
- [ ] Animator not pink / no missing controller on player.

## Extending

- Tune capsule on root (`CharacterController`), jump height on `PlayerController`, camera distance on `PlayerCameraRig`.
- Duplicate a `PlayerControlBindings` asset for a third profile (menu **Create → ThirdPersonMixamo → Player Control Bindings**).

This package is independent from `WhoWiredThis.Player.DuelController` / `DuelCameraRig` at the type level; keep fixes in sync manually if you need parity.
