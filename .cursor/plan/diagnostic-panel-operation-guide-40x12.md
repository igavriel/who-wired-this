---
name: Diagnostic panel operation guide
overview: Formatter-driven per-player operation guides (40×12 operator and reader); fix DiagnosticPanel prefab + Tutorial SceneStageManager wiring.
date: 2026-07-25
status: implemented
---

# Diagnostic panel operation guide

## Task name

Per-player diagnostic operation guides — prefab + Tutorial

## Date

2026-07-25

## Scope

- Add `PanelOperationGuideFormatter` (single source of truth for all guide text)
- Fix [`DiagnosticPanel.prefab`](Assets/WhoWiredThis/Prefabs/Panels/DiagnosticPanel.prefab) default waiting copy
- Wire [`Tutorial.unity`](Assets/Scenes/Game/Tutorial.unity) per player and per role via `SceneStageManager`
- Hook `DiagnosticDisplayController.SetWaiting()` for prefab Awake fallback

## Out of scope

- Pipes / Signal scenes in this pass (same formatter; wire in a follow-up)
- `PuzzleDiagnosticStartupSequence` / Monitor attempt logs (`BuildStandbyBody`)
- Glass overlay text (`ScenePanelLockBundle.waitingOverlayText`)
- Panel-focus binding changes
- OPEN (**E**) and **Exit** instructions (removed from copy)

## Approved approach

**Use the small code path** — not Inspector paste. One formatter + thin hooks on existing components. Avoid duplicating 13-line YAML blocks across prefabs/scenes.

| File | Change |
|------|--------|
| **New** [`PanelOperationGuideFormatter.cs`](Assets/WhoWiredThis/Scripts/Puzzles/Common/PanelOperationGuideFormatter.cs) | `BuildOperatorGuide(AllowedPlayerTag)`, `BuildReaderGuide(AllowedPlayerTag)`; width 40; **12 lines** each; uses `ComponentDiagnosticLogFormatter` |
| [`DiagnosticDisplayController.cs`](Assets/WhoWiredThis/Scripts/Puzzles/Common/DiagnosticDisplayController.cs) | Add `OperationGuideRole` enum (`None`, `Operator`, `Reader`) + `guidePlayer` (`AllowedPlayerTag`); when role ≠ `None`, `SetWaiting()` writes formatter output |
| [`SceneStageManager.cs`](Assets/WhoWiredThis/Scripts/Scene/SceneStageManager.cs) | Add `[SerializeField] bool useFormattedOperationGuides = true`; when true, intro/post-solve bodies come from formatter (ignore legacy TextArea prose) |

No new prefab GameObjects. Optional enum fields on existing `DiagnosticDisplayController` instances only.

## Confirmed controls

| Player | Select control | Activate / change |
|--------|----------------|-------------------|
| **Blue (A)** | A / D | W / S or Left Ctrl |
| **Red (B)** | ← / → | ↑ / ↓ or Right Ctrl |

Panel focus: select cycles highlighted **controls**; activate **changes** the highlighted control; select **SUBMIT** slot, then activate to test.

## Locked copy (formatter output)

Width **40 chars** — **every line exactly 40 characters**. Label/status rows use `FormatLabelStatus` (label + dots + value). Prose rows and headers use `PadRight` (text + trailing dots). Separators = 40× `-`.

### Blue operator (12 lines)

```
OPERATOR GUIDE - BLUE...................
----------------------------------------
SELECT.............................A / D
ACTIVATE..............W / S OR LEFT CTRL
SUBMIT...........SELECT SUBMIT, ACTIVATE
TALK....................BEFORE EACH TEST
CHECK.................HISTORY AFTER SEND
----------------------------------------
TELL PARTNER EACH SETTING...............
PARTNER READS THEIR MONITOR.............
----------------------------------------
STATUS.............................READY
```

### Red operator (12 lines)

```
OPERATOR GUIDE - RED....................
----------------------------------------
SELECT...............LEFT / RIGHT ARROWS
ACTIVATE.........UP / DOWN OR RIGHT CTRL
SUBMIT...........SELECT SUBMIT, ACTIVATE
TALK....................BEFORE EACH TEST
CHECK.................HISTORY AFTER SEND
----------------------------------------
TELL PARTNER EACH SETTING...............
PARTNER READS THEIR MONITOR.............
----------------------------------------
STATUS.............................READY
```

### Blue reader (11 lines)

```
READER GUIDE - BLUE.....................
----------------------------------------
WAIT..................FOR PARTNER SUBMIT
READ.................DIAGNOSTIC OUT LOUD
REPEAT..............KEY LINES TO PARTNER
----------------------------------------
YOU CANNOT CHANGE CONTROLS..............
HELP OPERATOR FIX MISTAKES..............
----------------------------------------
STATUS...................AWAITING SUBMIT
----------------------------------------
```

### Red reader (11 lines)

```
READER GUIDE - RED......................
----------------------------------------
WAIT..................FOR PARTNER SUBMIT
READ.................DIAGNOSTIC OUT LOUD
CHECK..................HISTORY FOR CLUES
----------------------------------------
YOU CANNOT CHANGE CONTROLS..............
HELP OPERATOR FIX MISTAKES..............
----------------------------------------
STATUS...................AWAITING SUBMIT
----------------------------------------
```

Reader body matches for both players; header label differs (`BLUE` / `RED`).

## Tutorial wiring

[`Tutorial.unity`](Assets/Scenes/Game/Tutorial.unity) uses **`SceneStageManager` only** (no `PuzzleDiagnosticStartupSequence`).

Flow: Stage 1 → A operates, B reads. After A solves (cutscene round-trip) → B operates, A reads.

| When | Blue (A) Rules panel | Red (B) Rules panel | Mechanism |
|------|----------------------|---------------------|-----------|
| Stage start | Blue **operator** | Red **reader** | `useFormattedOperationGuides` + stage → `ApplyIntroDiagnosticBodies` |
| After A solved | Blue **reader** | Red **operator** | `ApplyRoleSwitchDiagnosticBodies` |
| Awake flash (~1 frame) | Blue operator | Red reader | `DiagnosticDisplayController` `guideRole` + `guidePlayer` on each panel instance |

**Existing scene refs (do not rewire):**

- `playerADiagnosticDisplay` → DiagnosticPanel on Tutorial **A** side
- `playerBDiagnosticDisplay` → DiagnosticPanel on Tutorial **B** side

**SceneStageManager logic when `useFormattedOperationGuides`:**

- Intro: A → `BuildOperatorGuide(Player_A)`; B → `BuildReaderGuide(Player_B)`
- Post A-solve: A → `BuildReaderGuide(Player_A)`; B → `BuildOperatorGuide(Player_B)`

## Approved implementation steps

1. **Formatter** — Add `PanelOperationGuideFormatter.cs` with `Width = 40`, `GuideLines = 12`; every output line padded to 40 via `FormatLabelStatus` or `PadRight`; final pass through `FitToScreen`.

2. **DiagnosticDisplayController** — Add `OperationGuideRole` + `guidePlayer`; `SetWaiting()` calls formatter when role set; else legacy `waitingText`.

3. **SceneStageManager** — Add `useFormattedOperationGuides`; refactor `ApplyIntroDiagnosticBodies` / `ApplyRoleSwitchDiagnosticBodies` to use formatter when enabled.

4. **DiagnosticPanel.prefab** — Remove legacy ERROR prose from `waitingText`; set `guideRole = Operator`, `guidePlayer = Player_A` as sensible prefab default (instances override).

5. **Tutorial.unity** — On SceneStageManager: enable `useFormattedOperationGuides`. On each DiagnosticPanel instance: set `guidePlayer` (A side = Player_A, B side = Player_B) and `guideRole` matching Awake row in table above. Remove scene `waitingText` YAML override (~line 6734) that still contains ERROR prose.

6. **TMP** — Confirm Body_TMP on DiagnosticPanel: wrap off, monospace SDF (match existing puzzle diagnostics). No prefab layout resize unless Play Mode shows clip.

7. **Validate** — Unity compile; Play Mode Tutorial: both displays, stage start + after role switch; no ERROR flash; line counts correct.

## Assets touched (this pass)

| Asset | Action |
|-------|--------|
| `Assets/WhoWiredThis/Scripts/Puzzles/Common/PanelOperationGuideFormatter.cs` | Add |
| `Assets/WhoWiredThis/Scripts/Puzzles/Common/DiagnosticDisplayController.cs` | Modify |
| `Assets/WhoWiredThis/Scripts/Scene/SceneStageManager.cs` | Modify |
| `Assets/WhoWiredThis/Prefabs/Panels/DiagnosticPanel.prefab` | Modify `waitingText` + guide enums |
| `Assets/Scenes/Game/Tutorial.unity` | SceneStageManager flag + DiagnosticPanel instance guide fields |

## Testing checklist

- ✅ Tutorial start: Blue = operator guide; Red = reader guide
- ✅ After role switch: Blue = reader; Red = operator
- ✅ Every line exactly 40 chars (trailing dots on prose rows; dot leaders on label rows)
- ⬜ No TMP wrap; monospace columns align (Play Mode)
- ✅ No flash of “ERROR: LOCAL DATA INCOMPLETE” on load (prefab + scene override removed)
- ✅ Unity compiles with zero errors

## Risks

- **12-line grids** — operator and reader both 12×40; existing Body_TMP rect should fit (was sized for 12-line puzzle logs).
- **Awake vs stage copy** — `SetWaiting()` runs before `SceneStageManager`; guide enums on instances must match first-stage role to avoid one-frame wrong text.
- **Cutscene round-trip** — post-solve bodies must fire after `SceneRoleState` reload; verify on Tutorial with `roleSwapMode = CutSceneRoundTrip`.

## Rollback notes

Revert formatter + controller + SceneStageManager + Tutorial scene/prefab via Git. Legacy TextArea prose on Tutorial remains in git history if needed.

## Follow-up (not this pass)

- Pipes / Signal: enable `useFormattedOperationGuides` on their `SceneStageManager` + `PuzzleDiagnosticStartupSequence.operatorMonitorBody` from formatter
- Reader monitor: keep puzzle metric standby logs unchanged
