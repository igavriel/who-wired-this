---
name: Refactor puzzle interaction routing
overview: Refactor `MultiDimensionPuzzelManager` so puzzle execution is triggered through an assigned external interactable while preserving current solve behavior and actor attribution (P1/P2). Keep the manager focused on puzzle-state logic, not direct scene interaction.
todos:
  - id: add-interactable-bridge
    content: Add a new `IInteractable` bridge component that forwards interaction to the puzzle manager with runtime null guards and interface-safe inspector assignment.
    status: completed
  - id: extract-manager-entrypoint
    content: Refactor `MultiDimensionPuzzelManager` to expose a public actor-preserving trigger method and remove direct `IInteractable` implementation-specific surface.
    status: completed
  - id: rewire-scene-prefab
    content: Point relevant interactable references to the bridge component in scene/prefab wiring.
    status: completed
  - id: verify-behavior
    content: Validate compile and run-time parity for solve/fail flow, disable-on-solve behavior, and actor labels in emitted attempt results.
    status: completed
isProject: false
---

# Refactor MultiDimension Trigger Ownership

## Goal
Make `MultiDimensionPuzzelManager` non-interactable as a direct player target, and move click/interaction entry to a separate interactable bridge object that calls the same puzzle-check logic with preserved actor identity.

## Current Behavior To Preserve
- Interaction currently enters through `Interact(GameObject interactor)` in [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs).
- Actor resolution and solve execution are coupled at this seam:
  - resolve actor via `PlayerInteractorResolver.TryResolve(...)`
  - run `TryCheckSolutionWithActor(actor)`
- All side effects (materials, lock, disable linked interactables, events, retry history) are already centralized in `TryCheckSolutionWithActor(...)` and must remain unchanged.

## Implementation Plan
- Introduce a dedicated interactable bridge component (new file under `Visibility` or `Puzzles/Common`) that implements `IInteractable` and holds a serialized puzzle-manager reference.
- Enforce inspector safety on the bridge manager reference using the existing attribute/drawer pattern from [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Util/RequireInterfaceAttribute.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Util/RequireInterfaceAttribute.cs) and [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Editor/RequireInterfaceDrawer.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Editor/RequireInterfaceDrawer.cs).
- In `MultiDimensionPuzzelManager`, expose a public actor-preserving entrypoint (e.g. `TryCheckSolutionFromInteractor(GameObject interactor)`) that performs current actor resolution and forwards into `TryCheckSolutionWithActor(...)`.
- Remove direct `IInteractable` implementation from `MultiDimensionPuzzelManager` and its prompt fields/methods that are only needed when the manager itself is interactable.
- Keep all existing solve/state methods and event payload behavior unchanged so adapters like diagnostic/history continue to function without contract changes.
- Update any scene/prefab references so player click targets point to the new bridge interactable component rather than manager direct interaction.

## Key Files
- Primary refactor target: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs)
- New bridge component: `Assets/WhoWiredThis/Scripts/Visibility/<new interactable bridge>.cs`
- Interface pattern reference: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/PanelFocus/PanelFocusController.cs)

## Validation
- Confirm compile success after script changes.
- Verify click on bridge object triggers the same solve behavior as before (success/fail materials, lock behavior, events).
- Verify actor labels remain correct (`P1`/`P2`) when each player triggers interaction.
- Verify manager object itself no longer appears/acts as direct interactable entry.