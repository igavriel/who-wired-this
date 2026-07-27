---
name: GameOver auto-return timer
overview: After 30s on GameOverScene with no OK/Restart/CTRL, auto-load StartScene using the existing restart path.
date: 2026-07-28
status: implemented
---

# GameOverScene 30s auto-return to Start

## Task name

GameOver idle timeout → StartScene

## Date

2026-07-28

## Scope

- On `GameOverScene`, if the player does **not** confirm within **30 seconds**, automatically return to `StartScene`
- “OK” in this flow = existing confirm actions already wired on `GameOverSceneController`:
  - `restartButton` click (`RestartButton` on `UI-Canvas-GameOver`)
  - Player A/B action keys (`LeftControl` / `RightControl`)
- Auto-return must use the **same** navigation path as manual restart (`HandleRestartClicked` → bootstrap `PlaytestSceneId.StartScene` / `PlaytestFlowUtility.TryReturnToMainMenu`)
- Timeout duration configurable via `[SerializeField]` (default **30**)
- Cancel / stop the idle timer as soon as a confirm action runs (`hasRestarted` already guards double-load)

## Out of scope

- Renaming Restart button label to “OK” (unless requested later)
- Visible countdown UI (optional follow-up; not required for v1)
- Changing quit behavior (`quitButton` stays hidden as today)
- Changing run-summary layout or scoring display
- Auto-timeout on StartScene or other scenes

## Approved implementation steps

1. ✅ Extend `GameOverSceneController` with `idleReturnSeconds` + realtime coroutine; call `HandleRestartClicked` on timeout; cancel on confirm
2. ✅ No new scene objects (code-only; default 30s)
3. ✅ Tooltip on Inspector field
4. ⬜ Play Mode smoke on `GameOverScene`

## Risks

- Shared player/menu flow: must not double-load Start if CTRL and timeout race (existing `hasRestarted` flag covers this)
- Dirty working tree already has unrelated Start/YouTube changes — commit or confirm safe state before implementing
- Do not modify working Game Over prefab unless a countdown label is added later

## Testing checklist

- ⬜ Enter GameOver; wait 30s with no input → loads StartScene
- ⬜ Press Restart / CTRL before 30s → loads StartScene immediately; no second load / no errors
- ⬜ Auto-return still shows Start intro/trailer sequence normally
- ⬜ Compile clean (no new console errors)

## Rollback notes

- Revert `GameOverSceneController.cs` (and any scene override of `idleReturnSeconds` if serialized later)
