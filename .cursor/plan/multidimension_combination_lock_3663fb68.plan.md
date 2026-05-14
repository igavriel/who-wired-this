---
name: MultiDimension combination lock
overview: Add a combination-lock script that monitors an array of MultiDimension objects, evaluates their current indexes using internal logic, and permanently disables interaction after solve.
todos:
  - id: add-multidimension-read-api
    content: Add mode/index read API to MultiDimension for solution checking (Case2/Case3 values, Case1 ignored sentinel).
    status: completed
  - id: add-combination-lock
    content: Create MultiDimensionCombinationLock implementing IInteractable, internal solution compare logic, and solved gate.
    status: completed
  - id: lock-after-solve
    content: Disable configured interactable components after solve so state can no longer change.
    status: completed
  - id: verify-runtime
    content: Compile and run scenario test (2/3/4 sizes with solution 1,2,3), then verify no response after solve.
    status: completed
isProject: false
---

# MultiDimension Combination Solver Plan

## Goal
Create a script that validates a combination across multiple `MultiDimension` objects and locks the system once solved.

## Scope and behavior
- Track an ordered array of [`MultiDimension.cs`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimension.cs).
- Store required solution indices (same order as array).
- On check:
  - **Case 2 (`ExclusiveSinglePlayer`)**: test `exclusiveSubjectIndex`.
  - **Case 3 (`AllPlayers`)**: test `sharedSubjectIndex`.
  - **Case 1 (`SplitPlayers`)**: ignore for now (entry treated as non-participating).
- If all participating entries match solution, mark solved and stop interaction responses permanently.
- Partial match: no side effects.

## Required code changes
- Update [`MultiDimension.cs`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimension.cs) with read-only runtime accessors needed for validation:
  - `CurrentMode` getter.
  - `GetCurrentIndexForSolutionCheck()` (or equivalent) that returns:
    - Case 2: effective exclusive index
    - Case 3: effective shared index
    - Case 1: ignored sentinel (e.g. `-1`) to support “ignore for now”.
- Add new script in visibility domain, e.g. [`MultiDimensionCombinationLock.cs`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionCombinationLock.cs):
  - Serialized arrays:
    - `MultiDimension[] targets`
    - `int[] requiredIndices`
  - Serialized flags/state:
    - `bool solved` (read-only at runtime in Inspector)
  - Public method `TryCheckSolution()`:
    - Validate array lengths and nulls.
    - Compare each participating target’s current index with required index.
    - If all pass: set solved and disable responses.
  - `IInteractable` integration:
    - `GetPromptText()` returns active prompt until solved, then solved text.
    - `Interact(GameObject interactor)` no-ops when solved; otherwise triggers `TryCheckSolution()` (and optionally check+feedback path).

## Stop-response strategy after solve
- Primary: gate all interaction in combination script (`if (solved) return;`).
- If this script also controls child cyclers, disable linked components on solve:
  - optional serialized `MonoBehaviour[] interactionsToDisable` (expecting `IInteractable` implementers).
  - On solve, disable each referenced component so objects stop reacting.

## Validation plan
- Compile C# and resolve errors.
- Runtime test scene:
  - Configure 3 targets (sizes 2,3,4) and solution `1,2,3`.
  - Confirm unsolved/partial does nothing.
  - Confirm exact match sets solved.
  - Confirm post-solve interactions no longer change states.

## Data flow
```mermaid
flowchart LR
  Player[PlayerActions] -->|Interact| CombLock[IInteractable CombinationLock]
  CombLock -->|TryCheckSolution| MD1[MultiDimension #1]
  CombLock -->|TryCheckSolution| MD2[MultiDimension #2]
  CombLock -->|TryCheckSolution| MD3[MultiDimension #3]
  CombLock -->|onSolved| Disable[Disable linked interactables]
```
