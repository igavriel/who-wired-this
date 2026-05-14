---
name: Dimension Visibility Clean Plan
overview: Implement a layer-based per-object dimension visibility system in LocalCoOp with SHADOW-only fallback for non-owners, while removing old ghost/replacement artifacts and avoiding ScriptableObjects.
todos:
  - id: rollback-old-visibility
    content: Remove previous replacement/ghost visibility scripts/assets/layers and scene leftovers
    status: completed
  - id: create-dimension-component
    content: Implement DimensionVisibilityObject with PlayerAVisability/PlayerBVisability and OBJECT/SHADOW layer assignment
    status: completed
  - id: configure-cameras-and-physics
    content: Set camera masks and physics matrix for DimensionA/DimensionB + player layers
    status: completed
  - id: author-prefabs-and-scene
    content: Configure dimension object prefabs/scene roots with OBJECT/SHADOW child arrays
    status: completed
  - id: playmode-validation
    content: Run LocalCoop validation for visibility and collision behavior
    status: completed
isProject: false
---

# Dimension Visibility Reset (Clean)

## Goal

Replace the previous ghost/replacement visibility approach with a per-object dimension system where:

- Owner player sees `OBJECT` only.
- Non-owner player sees `SHADOW` only.
- Hidden `OBJECT` has no render and no collision for the non-owner player.

## Confirmed Decisions

- No ScriptableObjects in the new system.
- Non-owner view uses SHADOW (not full disappearance).
- SHADOW is a child under the same prefab/root as OBJECT.
- Shared objects do not use the dimension component.

## Scope

- Keep LocalCoOp prototype and player setup.
- Remove only prior visibility-system artifacts that conflict with this model.

## Implementation Steps

### 1) Roll back previous visibility system artifacts

- Remove old scripts/assets tied to replacement/ghost pipeline:
  - [/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerPlayerVisibilityManager.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerPlayerVisibilityManager.cs)
  - [/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerObjectVisibility.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/PerObjectVisibility.cs)
  - [/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/VisibilityProfile.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/VisibilityProfile.cs)
  - old ghost/replacement visibility assets/materials under `Assets/WhoWiredThis/Data/Visibility` and `Assets/WhoWiredThis/Materials` (if still referenced)
- In LocalCoOp scene(s), remove manager objects and sample duplicates used only by prior replacement rendering.
- Remove unused legacy layers (`P1Replacement`, `P2Replacement`) if unreferenced.

### 2) Add the new per-object component

- Create [/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/DimensionVisibilityObject.cs](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/DimensionVisibilityObject.cs).
- Define enum:
  - `PlayerAVisability`
  - `PlayerBVisability`
- Inspector fields:
  - `mode`
  - `Renderer[] objectRenderers`
  - `Collider[] objectColliders`
  - `Renderer[] shadowRenderers`
- Runtime behavior (on init and validate-safe):
  - `PlayerAVisability`:
    - OBJECT renderers/colliders -> `DimensionA`
    - SHADOW renderers/colliders -> `DimensionB`
  - `PlayerBVisability`:
    - OBJECT renderers/colliders -> `DimensionB`
    - SHADOW renderers/colliders -> `DimensionA`
- Include guardrails:
  - Auto-collect from children when arrays are empty (optional convenience).
  - Never move shared/global objects; component applies only to tagged dimension objects.

### 3) Camera culling configuration

- In LocalCoOp camera setup:
  - Player A camera mask includes shared/default + `DimensionA`; excludes `DimensionB`.
  - Player B camera mask includes shared/default + `DimensionB`; excludes `DimensionA`.
- Verify minimap/special cameras are not accidentally filtering required shared layers.

### 4) Physics layer matrix configuration

- Ensure players do not collide with hidden OBJECTs of opposite dimension:
  - Player A layer collides with shared/default + `DimensionA`, not `DimensionB`.
  - Player B layer collides with shared/default + `DimensionB`, not `DimensionA`.
- Ensure SHADOW colliders are disabled or non-blocking so they remain visual-only.

### 5) Prefab and scene authoring workflow

- For each dimension object prefab/root:
  - Keep OBJECT and SHADOW as children of the same root.
  - Add `DimensionVisibilityObject` on root.
  - Assign OBJECT renderer/collider arrays and SHADOW renderer arrays.
  - Set mode to `PlayerAVisability` or `PlayerBVisability`.
- Shared objects:
  - Do not add `DimensionVisibilityObject`.
  - Keep on shared/default layers.

### 6) Scene validation (LocalCoOp)

- Place at least 5 test objects:
  - 2 x `PlayerAVisability`
  - 2 x `PlayerBVisability`
  - 1 x shared object (no component)
- Playmode checks:
  - A sees/collides with A OBJECTs + shared.
  - B sees/collides with B OBJECTs + shared.
  - Non-owner sees SHADOW only.
  - No invisible collision blockers from hidden OBJECTs.

## Risks and Mitigations

- Layer assignment drift in prefabs/scenes -> keep assignment centralized in component init.
- Misconfigured camera masks -> verify both main cameras and utility cameras.
- Existing references to removed visibility scripts -> remove scene references before deleting assets.

## Out of Scope

- Dynamic runtime ownership switching between A/B.
- Networked replication or authoritative ownership sync.
- Designer tooling/editor windows beyond inspector workflow.

