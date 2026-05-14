---
name: Dimension Visibility Reset
overview: Replace the previous ghost/replacement visibility approach with a per-object dimension system where objects are visible to Player A, Player B, or both, and hidden objects are fully absent (no render and no collision) for the non-owner player. Also roll back prior visibility-system artifacts that are no longer needed.
todos:
  - id: rollback-old-visibility-system
    content: Remove previous visibility scripts/assets/scene objects and unused replacement layers
    status: completed
  - id: add-dimension-scriptableobjects
    content: Add two ScriptableObjects (PlayerADimensionData and PlayerBDimensionData) that each store an array of owned objects
    status: completed
  - id: add-dimension-component
    content: Add DimensionVisibilityObject script and register objects into PlayerA/PlayerB ScriptableObject arrays
    status: pending
  - id: configure-camera-and-physics
    content: Set camera culling masks and physics matrix so hidden-dimension objects are not visible and not collidable
    status: pending
  - id: tag-test-objects
    content: Configure multiple scene objects in Inspector using the new dimension component
    status: pending
  - id: playmode-verify
    content: Verify visibility/collision behavior for both players in LocalCoop
    status: pending
isProject: false
---

# Per-Object Dimension Visibility Plan

## Target Behavior

- A Dimension object is a prefab which can be later have several variants.
  - the prefab will include the object it self (OBJECT)
  - the prefab will have a plan which will mark the object boundries on the floor (SHADOW)
- An object can be configured in Inspector as: (NOTE not all object have this behaviour).
  - `PlayerAOnly`
  - `PlayerBOnly`
- If a player is allowed to see an object, that object is effectively in that player’s dimension:
  - Rendered by that player camera (OBJECT)
  - the player will not see the SHADOW plane

- else If a player is not allowed to see an object, that object is effectively not in that player’s dimension:
  - Not rendered by that player camera (OBJECT)
  - the player will see only the SHADOW plane
- the plane is actually the ghost visual
- to simplify the implementation
  - use a layer for playerA and a layer for playerB 
  - ket the prefab parent to set the layers of its child objects by using the parent selection (A or B).
  - this mean. that the layers can be set when launching. the scene.
- 

## Rollback Scope (Previous Visibility Attempt)

Rollback only prior visibility-system artifacts, keep the existing LocalCoop scene/player prototype.

### Remove no-longer-needed assets/scripts

- Delete scripts:
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerPlayerVisibilityManager.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerPlayerVisibilityManager.cs)`
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerObjectVisibility.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerObjectVisibility.cs)`
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/VisibilityProfile.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/VisibilityProfile.cs)`
- Delete profile/material assets:
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/OnlyPlayer1_Ghost.asset](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/OnlyPlayer1_Ghost.asset)`
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/OnlyPlayer2_Ghost.asset](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/OnlyPlayer2_Ghost.asset)`
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/VisibleToBoth_Default.asset](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Data/Visibility/VisibleToBoth_Default.asset)`
  - `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Materials/VisibilityGhostMaterial.mat](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Materials/VisibilityGhostMaterial.mat)`
- In scene cleanup (`LocalCoop`):
  - Remove `PerPlayerVisibilityManager` GameObject
  - Remove sample duplicate replacement object(s) created for ghost rendering
- Remove any now-unused layers that were added for prior approach (`P1Replacement`, `P2Replacement`) if no longer referenced.

## New Implementation Approach

Use **layers + a per-object component + prefabs** + scripts.

### 1) no need dimension ScriptableObjects

### 2) Add new component

Create:

- `[/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/DimensionVisibilityObject.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/DimensionVisibilityObject.cs)`

Inspector fields:

- `DimensionVisibilityMode mode` enum: `PlayerAVisability`, `PlayerBVisability`,
- `Renderer[] renderersToControl - assign on inspector`
- `Collider[] collidersToControl - assign on inspector`
- `DimensionObjectsData playerAData`
- `DimensionObjectsData playerBData`

Behavior:

- `PlayerAVisability`: place renderers/colliders on layer `DimensionA`
- `PlayerBVisability`: place renderers/colliders on layer `DimensionB`

### 3) Camera mask setup

In `LocalCoOp`:

- Player A camera culling mask includes: `Default/Shared + DimensionA`
- Player B camera culling mask includes: `Default/Shared + DimensionB`
- Excludes opposite dimension layer.

### 4) Physics matrix setup

In Project Physics layer collision matrix:

- Player A layer collides with `Default/Shared + DimensionA`, not `DimensionB`
- Player B layer collides with `Default/Shared + DimensionB`, not `DimensionA`

### 5) Per-object Inspector workflow

For each configurable object:

1. Add `DimensionVisibilityObject`.
2. Assign `PlayerADimensionData` and `PlayerBDimensionData` assets.
3. Assign mode (`PlayerAOnly`, `PlayerBOnly`).
4. Confirm renderers/colliders list.

## Scene Validation Steps

- Place 5 test objects in `LocalCoOp`:
  - 2 `PlayerAVisability`
  - 2 `PlayerBVisability`
  - 1 `General object`
- Validate in Play Mode:
  - A sees/collides only A+General
  - B sees/collides only B+General
  - No invisible blocking from hidden-dimension objects

## Why this design

- Purely Inspector-driven per object.
- Matches your requirement that hidden objects are completely unknown to the other player.
- Avoids material replacement and duplicate renderers.
- ScriptableObjects provide a single source of truth for each dimension’s object roster (A list and B list).

