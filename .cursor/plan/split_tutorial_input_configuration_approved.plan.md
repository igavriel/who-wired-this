---
name: Split Tutorial input configuration (approved)
overview: "Scene-only overrides in Split Tutorial.unity: sync MultiDimension displayName + world TMP per state; vocabulary POWER/FLOW and VALVE/LOAD; solutions DIM+HIG and OPN+MID; verify history inputOrder; fix swapped diagnostic solvedMessage; no prefab or TutorialStageManager changes."
todos:
  - id: mcp-inspect-tmp
    content: Unity MCP — list TMP on LeftKnob, RightSlider, LeftSlider, RightKnob instances (no guessed paths)
    status: completed
  - id: player-a-labels
    content: "Player A: subjects displayName OFF/DIM/BRT + LOW/MID/HIG; matching TMP text; correctIndex (1,2)"
    status: completed
  - id: player-b-labels
    content: "Player B: CLS/HLF/OPN + LOW/MID/HIG; matching TMP; correctIndex (2,1)"
    status: completed
  - id: adapters-diag
    content: "Confirm HistoryAdapter inputOrder vs puzzleElements; fix solvedMessage swap (Inspector only)"
    status: completed
  - id: verify-build
    content: "Playtest solve both sides, history tokens, glass lock; read_console compile"
    status: pending
isProject: true
approved: true
---

# Split Tutorial input configuration (approved)

## Scope and constraints

- **Scene file only**: [`Assets/Scenes/Split Tutorial.unity`](Assets/Scenes/Split Tutorial.unity) — use **Prefab Instance** property overrides.
- **Do not modify** [`MultiDimension_Knob.prefab`](Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_Knob.prefab) or [`MultiDimension_Slider.prefab`](Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_Slider.prefab).
- **Do not change**: [`TutorialStageManager`](Assets/WhoWiredThis/Scripts/Tutorial/TutorialStageManager.cs), scoring, tutorial instructional copy, high scores, completion UI, or diagnostic **logic** (only Inspector strings on adapters / display).

## Architecture reference (unchanged)

- States: [`MultiDimension`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimension.cs) `subjects[]` + `activeSubjectIndex`; cycle via [`MultiDimensionSubjectCycler`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionSubjectCycler.cs).
- Solution: [`MultiDimensionPuzzelManager`](Assets/WhoWiredThis/Scripts/Visibility/MultiDimensionPuzzelManager.cs) `puzzleElements[].correctIndex` vs `GetCurrentIndexForSolutionCheck()`.
- History: [`MultiDimensionHistoryAdapter`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs) uses `GetSubjectDisplayName` for each `inputOrder` element — must match **visible** labels if operators and historians align.

## Scene object map (Split Tutorial)

| Side | Panel | PuzzleManager | Input 1 (`puzzleElements[0]`) | Input 2 (`puzzleElements[1]`) |
|------|--------|-----------------|-------------------------------|-------------------------------|
| Player A / Blue UI | `Player1_Panel` | `PuzzleManager` | **LeftKnob** — POWER | **RightSlider** — FLOW |
| Player B / Red UI | `Player2_Panel` | `PuzzleManager` | **LeftSlider** — VALVE | **RightKnob** — LOAD |

## Approved vocabulary and `correctIndex`

Assume **subject index order** matches the list order below (index `0` = first token, `1` = second, `2` = third). Reorder `subjects` in the Inspector if the physical cycle order differs so that **cycling order** stays sensible; then set `correctIndex` to the index of the winning token.

### Player A

| Input | Role | Index | `displayName` (and TMP text) |
|-------|------|-------|------------------------------|
| 1 — LeftKnob | POWER | 0 / 1 / 2 | **OFF** / **DIM** / **BRT** |
| 2 — RightSlider | FLOW | 0 / 1 / 2 | **LOW** / **MID** / **HIG** |

- **Solution**: POWER = **DIM** → index **1**; FLOW = **HIG** → index **2**.
- **`MultiDimensionPuzzelManager` (Player A)**: `correctIndex` for elements **[0, 1]** = **`(1, 2)`** (verify after subjects array order is finalized).

### Player B

| Input | Role | Index | `displayName` (and TMP text) |
|-------|------|-------|------------------------------|
| 1 — LeftSlider | VALVE | 0 / 1 / 2 | **CLS** / **HLF** / **OPN** |
| 2 — RightKnob | LOAD | 0 / 1 / 2 | **LOW** / **MID** / **HIG** |

- **Solution**: VALVE = **OPN** → index **2**; LOAD = **MID** → index **1**.
- **`MultiDimensionPuzzelManager` (Player B)**: `correctIndex` for elements **[0, 1]** = **`(2, 1)`**.

## Visual TextMeshPro requirement (mandatory)

For **each** of the four inputs and **each** of the three states:

1. Set `MultiDimension.subjects[i].displayName` to the exact **3-letter** token from the tables above.
2. Use **Unity MCP** (`manage_gameobject` / hierarchy) to locate the **actual** `TextMeshPro` (or `TextMeshProUGUI` if any) on that subject instance — **do not guess child names**.
3. Set the TMP **text** to the **same** token as `displayName` for that index.
4. After edits, a successful attempt row in Shared History must show **space-separated** tokens matching what the operator sees on the controls (e.g. Player A solve: **`DIM HIG`**; Player B: **`OPN MID`** — exact order follows `inputOrder` / element order).

## History adapter

On **`Player1_Panel`** and **`Player2_Panel`**, [`MultiDimensionHistoryAdapter`](Assets/WhoWiredThis/Scripts/Puzzles/Common/MultiDimensionHistoryAdapter.cs) **`inputOrder`** must reference the **same two `MultiDimension` components** in the **same order** as `PuzzleManager.puzzleElements` (LeftKnob then RightSlider on A; LeftSlider then RightKnob on B). Fix only if drift is found.

## Diagnostic solvedMessage (Inspector only)

Prior audit suggested the solved messages may be swapped.

Important:
The solvedMessage must describe the puzzle side that was solved, not necessarily the panel that displays the message.

Use the existing diagnostic routing:
- If Player1_Panel Diagnostic displays Player B / Red puzzle results, its solvedMessage should refer to RED / Player B side.
- If Player2_Panel Diagnostic displays Player A / Blue puzzle results, its solvedMessage should refer to BLUE / Player A side.

Fix string fields only.
Do not change diagnostic routing or C# logic.

## Implementation checklist (execution phase)

1. Unity MCP: active instance, load **Split Tutorial** scene if needed; collect TMP component paths on all four prefab instances.
2. Apply all `displayName` + TMP overrides on instances under **Split Tutorial.unity** only.
3. Set **`correctIndex`** `(1, 2)` and `(2, 1)` on the two `PuzzleManager` components (re-validate if subject order changes).
4. Verify **`inputOrder`** on both `MultiDimensionHistoryAdapter` components.
5. Fix swapped **`solvedMessage`** on `MultiDimensionDiagnosticAdapter` instances.
6. Save scene; play mode: both solves, history shows **DIM HIG** / **OPN MID**, glass overlay / action lock unchanged.
7. **`read_console`**: no compile errors; note any new warnings.
8. **Post-implementation report** (required): list every TMP object updated (scene path / MCP id); list every `displayName` value set; confirm visual = history; confirm compile.

## Post-implementation verification (from approval brief)

- Shared History shows new **3-letter** values for attempts.
- Both sides still solve with the configured indices.
- Glass overlay / action locking still behaves as before.
- Unity compiles; console checked for errors.
