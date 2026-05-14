---
name: History Board Refactor Audit
overview: Audit current History Board flow and propose the smallest safe refactor to separate shared history data from per-board rendering, without changing puzzle validation, button flow, or visibility systems.
todos:
  - id: audit-current-flow
    content: Confirm current history data flow and coupling points across manager, adapter, board renderer, and scene wiring.
    status: completed
  - id: design-shared-source
    content: Define minimal SharedHistorySO responsibilities and API that replace board-owned storage.
    status: completed
  - id: display-refactor-shape
    content: Define display-only responsibilities and identify reusable rendering code from HistoryBoardController.
    status: completed
  - id: integration-migration
    content: Plan adapter wiring change from single board target to shared source writes.
    status: completed
  - id: verify-multiboard
    content: Plan validation steps proving one submit updates multiple board displays from same source.
    status: completed
isProject: false
---

# History Board Data/Display Separation Audit

## A. Current Implementation

- **Scripts/classes involved**
  - Rendering + storage: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/Common/HistoryBoardController.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/Common/HistoryBoardController.cs)
  - Event adapter (manager -> board): [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs)
  - Attempt producer/validator: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs)
  - Attempt payload: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionAttemptResult.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionAttemptResult.cs)
  - Interact bridge into validator: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzleInteractableBridge.cs)

- **Scene objects/prefabs involved**
  - Prefab: [`/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Prefabs/Panels/HistoryPanel.prefab`](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Prefabs/Panels/HistoryPanel.prefab)
  - Scene instances: [`/Users/ilang/git/unity/who-wired-this/Assets/Scenes/Split Puzzle.unity`](/Users/ilang/git/unity/who-wired-this/Assets/Scenes/Split%20Puzzle.unity), [`/Users/ilang/git/unity/who-wired-this/Assets/Scenes/Tutorial.unity`](/Users/ilang/git/unity/who-wired-this/Assets/Scenes/Tutorial.unity)

- **Where data currently lives**
  - History entries are stored directly in `HistoryBoardController.entries` (`List<HistoryEntry>`), plus local counters/state (`nextAttemptNumber`, `viewOffset`, `userScrolled`).
  - Puzzle manager also has separate retry-log data (`retryStrings`) but this is not the board’s table source.

- **Where rendering currently happens**
  - `HistoryBoardController.Render()` writes to `titleText` and `bodyText` (TMP world text).
  - `AddEntry/Clear/Scroll*` all eventually call `Render()`.

- **Where submit/validator calls into history board**
  - `MultiDimensionPuzzleInteractableBridge.Interact()` -> `MultiDimensionPuzzelManager.TryCheckSolutionFromInteractor()`.
  - `MultiDimensionPuzzelManager` validates and emits `OnAttemptSubmitted`.
  - `MultiDimensionHistoryAdapter.HandleAttemptSubmitted()` builds `inputText` and calls `historyBoard.AddEntry(...)`.

## B. Problem Analysis

- **Why current implementation fits one board best**
  - The history list is owned by each `HistoryBoardController`, so each board maintains private state by design.
  - Adapter has a single serialized `historyBoard` target, so each adapter writes to one board instance.

- **What breaks if board object is duplicated as-is**
  - A duplicated board can remain stale unless a corresponding adapter points to it.
  - If both adapters are not wired consistently, boards can diverge (different manager/inputOrder/board refs).
  - Per-board counters and scroll state mean duplicated boards are not guaranteed to stay in sync.

- **Layer/visibility coupling status**
  - `HistoryBoardController` itself has no player/layer logic.
  - In split scene, ownership/visibility is handled around it (panel roots with `PanelFocusController.allowedPlayerId`, dimension/layer setup), so duplication risks are mostly inspector wiring errors, not rendering-script logic.

## C. Recommended Refactor (Smallest Clean Architecture)

### `SharedHistorySO` (or `SharedHistoryModel` as asset-backed ScriptableObject)
- **Responsibility**: single source of truth for history entries shared by all displays.
- **Fields**:
  - `List<HistoryEntry> entries`
  - `int nextAttemptNumber`
  - optional `int version`/change token (if useful for debug)
- **Public methods**:
  - `IReadOnlyList<HistoryEntry> Entries`
  - `int AddEntry(string actor, string inputText, string publicStatus)`
  - `void Clear()`
  - `event Action OnChanged`
- **Replaces/reuses**:
  - Moves list/counter ownership out of `HistoryBoardController`.
  - Reuses existing `HistoryEntry` shape initially for minimal risk.

### `HistoryEntry` data structure
- **Responsibility**: immutable-ish row payload for display.
- **Fields**:
  - `attemptNumber`, `actor`, `inputText`, `publicStatus`.
- **Public methods**: none required in first pass.
- **Replaces/reuses**:
  - Reuse existing `HistoryEntry` class (same namespace/file or extracted if needed later).

### `HistoryBoardDisplay` component (can be implemented by refactoring current `HistoryBoardController`)
- **Responsibility**: render-only view of shared history into local TMP fields, keep local UI state only (scroll offset, row limit).
- **Fields**:
  - existing TMP refs/title/layout settings
  - reference to `SharedHistorySO`
  - local display state: `viewOffset`, `userScrolled`
- **Public methods**:
  - `Refresh()` / `Render()` from shared source
  - `ScrollUp/ScrollDown/ScrollToLatest`
  - optional display-local `SetMaxVisibleRows`
- **Replaces/reuses**:
  - Reuse current formatting and TMP rendering code from `HistoryBoardController.Render()`.
  - Remove `AddEntry/Clear` data ownership from display (or keep temporary wrappers that forward to shared source during migration).

### Existing adapters/managers after refactor
- `MultiDimensionHistoryAdapter` remains the integration point from puzzle attempts.
- It should write to `SharedHistorySO.AddEntry(...)` instead of `HistoryBoardController.AddEntry(...)`.
- `MultiDimensionPuzzelManager` and button/validator flow remain unchanged.

## D. Migration Plan (No implementation yet)

1. Create shared history data source (`SharedHistorySO`) with `Entries`, `AddEntry`, `Clear`, `OnChanged`.
2. Move entry storage/counter logic from board controller into shared source; keep existing row format unchanged.
3. Convert current board script to display-only renderer (`HistoryBoardDisplay` behavior), preserving TMP formatting/scroll UX.
4. Update `MultiDimensionHistoryAdapter` to target shared source and stop calling a specific board for writes.
5. Add/duplicate a second physical board display object.
6. Assign both display instances to the same shared history source asset.
7. Validate: one submit attempt updates both boards automatically.

## E. Test Plan

- Project compiles after each migration step.
- Existing single board still renders entries.
- Adding a sample entry into shared source updates first board.
- Adding second board with same source shows identical rows.
- Real submit attempt (current validator flow) updates both boards.
- Calling `Clear` on shared source clears both boards.
- Display component does not calculate puzzle result.
- Display component does not read puzzle buttons or puzzle manager state directly.

## F. Questions / Assumptions

- **Assumption**: keeping `inputText` formatting (`inputOrder` + subject display names) inside `MultiDimensionHistoryAdapter` is acceptable for now; only storage/render ownership changes.
- **Assumption**: one shared source per puzzle instance (not global across unrelated puzzles/scenes) is intended.
- **Question**: should history persist when scene reloads in play mode, or reset on each scene load? (affects whether `SharedHistorySO` is runtime-reset in `OnEnable/Awake` or manually cleared by scene logic).
