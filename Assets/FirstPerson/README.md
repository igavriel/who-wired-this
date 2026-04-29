# FirstPerson

Small internal first-person prototype package for keyboard-driven movement and interaction testing.

## Structure

- `Assets/FirstPerson/Scripts`
- `Assets/FirstPerson/Prefabs`
- `Assets/FirstPerson/Data`
- `Assets/FirstPerson/Scenes`

## Main assets

- Player prefabs:
  - `FirstPersonPlayer_A.prefab`
  - `FirstPersonPlayer_B.prefab`
- Binding assets:
  - `PlayerControlBindings_PlayerA.asset`
  - `PlayerControlBindings_PlayerB.asset`
- Scenes:
  - `FirstPerson_Single.unity`
  - `FirstPerson_Duel.unity`

## Controls

Configured through `PlayerControlBindings` ScriptableObject:

- Forward/Back: move along current camera facing direction
- Left/Right: rotate player/view yaw smoothly
- Interact: single key press
- Mouse look is not used in this prototype.

## Controller setup requirements

`FirstPersonController` is intentionally strict (no fallbacks). Each player prefab instance must have:

- `CharacterController` on the same GameObject
- `inputBindings` assigned
- `playerCamera` assigned

Missing setup is caught with assertions in `Awake`.

## Testing

1. Open `Assets/FirstPerson/Scenes/FirstPerson_Single.unity`.
2. Play and validate:
   - forward/back movement follows camera direction
   - left/right rotates view yaw
   - interact raycast works from camera
3. Open `Assets/FirstPerson/Scenes/FirstPerson_Duel.unity`.
4. Play and validate both players have camera-linked movement/rotation and interaction.

## Notes and extension points

- Feature is intentionally minimal and internal.
- No networking/polish systems are included in this prototype stage.
- Extend by adding richer interactables or swapping bindings assets.
