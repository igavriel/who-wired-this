---
task: Puzzle startup diagnostic intro + standby (Pipes + Signal)
date: 2026-07-22
status: implemented
scenes: Assets/Scenes/Game/Puzzle Pipes.unity, Assets/Scenes/Game/Puzzle Signal.unity
---

# Puzzle startup diagnostics (Pipes + Signal)

## Task name

Dual-surface startup: Rules panel role intro (SceneStageManager) + Monitor standby log before first Submit.

## Scope

- Puzzle Pipes and Puzzle Signal only; Tutorial unchanged.
- `PuzzleDiagnosticStartupSequence.cs`, standby builders on existing adapters/formatters.
- Reactivate Signal V2 Rules panels; wire startup sequence on both panels per scene.

## Out of scope

- Tutorial scene; submit evaluation; live scope; result lights.

## Implementation

- ✅ Rules panel: existing `SceneStageManager` intro copy (unchanged wiring target).
- ✅ Monitor: `PuzzleDiagnosticStartupSequence` after 4s hold — reader gets 40×12 STANDBY log, operator gets short idle copy.
- ✅ Adapters no longer call `SetWaiting()` on enable (startup + submit own the Monitor).
- ✅ Signal `_OLD_DiagnosticPanel-*` reactivated where inactive.

## Testing checklist

- ⚠️ Cold start Pipes/Signal: Rules shows role intro; Monitor shows standby after hold.
- ⚠️ Reader Monitor updates on partner Submit; operator Monitor idle then partner submits in phase 2.
- ⚠️ Role swap boundary replays sequence; Tutorial regression.

## Rollback

- Disable `PuzzleDiagnosticStartupSequence` components; restore adapter `SetWaiting()` in OnEnable if needed.
