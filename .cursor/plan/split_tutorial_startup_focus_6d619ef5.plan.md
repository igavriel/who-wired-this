---
name: Split tutorial startup focus
overview: Add a small scene-driven bootstrap component that, on play, optionally puts each `PlayerPanelFocusController` into focus on its matching `PanelFocusController` (Inspector bool `enterFocusOnStartup` / `applyOnStartup` to enable or skip). Cameras use existing `GetCameraSnapPose` framing from [PanelFocusController.cs](Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs). Wire it on a dedicated scene object in [Split Tutorial.unity](Assets/Scenes/Split%20Tutorial.unity) via the Inspector.
todos:
  - id: add-bootstrap-script
    content: Add InitialPanelFocusBootstrap.cs with four SerializeField refs, a serialized enterFocusOnStartup (or applyOnStartup) bool with Tooltip, and Start() that no-ops when false; otherwise TryEnterFocus for A and B
    status: pending
  - id: wire-scene
    content: "In Split Tutorial.unity: create root GameObject, add component, assign FirstPersonPlayer_A/B PlayerPanelFocusController and Player1/2_Panel Board PanelFocusController references"
    status: pending
isProject: false
---

# Split Tutorial: start both players in panel focus

## Context (how framing works today)

- **[`PanelFocusController`](Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs)** exposes `GetCameraSnapPose(Camera, out Vector3, out Quaternion)` using `boardRenderer`, `frameFillPercent`, FOV, and aspect to place the camera in front of the board.
- **[`PlayerPanelFocusController`](Assets/WhoWiredThis/Scripts/PanelFocus/PlayerPanelFocusController.cs)** calls that in `TryEnterFocus`: caches local camera pose, applies world snap pose, disables `FirstPersonController` and `PlayerActions`, then calls `panel.OnFocusEntered(this)` so selection UI and `Update` navigation work.

Scene wiring you already have in [Split Tutorial.unity](Assets/Scenes/Split%20Tutorial.unity):

| Root object | `PanelFocusController` | `allowedPlayerId` (enum) |
|-------------|------------------------|--------------------------|
| `Player1_Panel` → child **Board** | on Board | `Player_A` (1) |
| `Player2_Panel` → child **Board** | on Board | `Player_B` (2) |

Players: `FirstPersonPlayer_A` and `FirstPersonPlayer_B` each have `PlayerPanelFocusController` with matching `playerId` and camera references (already present in the scene YAML).

## Implementation approach

Add a **new bootstrap script** (no change required to `PanelFocusController` math; optional one-line reuse is already via `TryEnterFocus`).

**Suggested type name / location:** `InitialPanelFocusBootstrap` in [`Assets/WhoWiredThis/Scripts/PanelFocus/`](Assets/WhoWiredThis/Scripts/PanelFocus/) (same feature area as the existing controllers).

**Serialized fields (Inspector-configured):**

- **`bool enterFocusOnStartup`** (name can be `applyOnStartup`—pick one): when **unchecked**, `Start()` does nothing (players stay in normal first-person). When **checked**, run the bootstrap. Default **true** for Split Tutorial; set false to reuse the same scene object in builds or variants without auto-focus. Add a `[Tooltip(...)]` explaining this.
- `PlayerPanelFocusController playerAFocus` → `FirstPersonPlayer_A`’s component  
- `PanelFocusController playerAPanel` → `Player1_Panel/Board`’s `PanelFocusController`  
- `PlayerPanelFocusController playerBFocus` → `FirstPersonPlayer_B`’s component  
- `PanelFocusController playerBPanel` → `Player2_Panel/Board`’s `PanelFocusController`  

**Note:** Unchecking the **MonoBehaviour’s** component checkbox in the Inspector also prevents `Start()` from running entirely; the serialized bool is still useful so you can **leave the component enabled** (refs stay assigned, other future logic could run) while toggling only the startup focus behavior.

**Lifecycle:** In `Start()`, if `!enterFocusOnStartup`, return. Otherwise call `playerAFocus.TryEnterFocus(playerAPanel)` and `playerBFocus.TryEnterFocus(playerBPanel)`. Each player has its own `lastStateChangeFrame`, so both can enter on the same frame.

**Optional hardening (if cameras/viewports are adjusted in `Start` elsewhere):** If framing looks wrong on the first frame only, switch to a coroutine `Start()` → `yield return null` then enter focus so `Camera.aspect` / viewport matches runtime layout. Only add this if you observe a one-frame glitch; [`CameraViewportPresetApplier`](Assets/WhoWiredThis/Core/CameraViewportPresetApplier.cs) on the player cameras may run in the same phase.

## Where to put it in the scene

1. Open **Split Tutorial**.
2. Create an empty GameObject at the scene root (e.g. `SplitTutorial_InitialFocus`).
3. Add the new `InitialPanelFocusBootstrap` component.
4. Assign the four references above (drag **Board** under each panel for the panel controllers, drag each **FirstPersonPlayer_*** root for the focus controllers).

No need to attach the script to the players or panels themselves unless you prefer that for organization; a **single orchestrator object** keeps tutorial-specific startup out of the reusable player prefab.

## Inspector checklist (framing)

On each **Board**’s `PanelFocusController`, the values you already use (`frameFillPercent`, `boardRenderer`, `extraDistance`) define how `GetCameraSnapPose` behaves; the bootstrap does not duplicate those numbers—it only triggers the same path as walking up and interacting.

## Out of scope / not needed for your choice

- You confirmed **Player 2 frames `Player2_Panel`**, so there is **no** `allowedPlayerId` conflict and **no** change to `PanelFocusController`’s single `activeController` model.
- If you ever need **both** players in full focus UI on the **same** `PanelFocusController` instance, that would require a larger refactor (multiple active controllers / selection ownership); not part of this plan.
