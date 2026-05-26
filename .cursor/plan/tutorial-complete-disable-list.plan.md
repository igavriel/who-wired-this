---
task: Tutorial complete disable list
date: 2026-05-27
status: planned
---

# Tutorial completion follow-up (tutorial-first)

## Scope

- Scene: `Assets/Scenes/Tutorial.unity` only.
- Script: `Assets/WhoWiredThis/Scripts/Tutorial/TutorialStageManager.cs`.
- Add a simple inspector list of objects to disable when tutorial completion is reached.
- On completion, update diagnostics panels for both players with the same completion message.
- Unlock the exit door on completion.
- Keep double-click Exit behavior enforced for closing panel focus.

## Out of scope

- Puzzle scenes (`Puzzle Pipes`, `Puzzle Signal`) in this phase.
- Any changes to solve logic, attempt handling, or stage progression.

## Approved implementation steps

1. Add serialized field to `TutorialStageManager`:
   - `GameObject[] objectsToDisableOnComplete`.
2. Add serialized completion message field in `TutorialStageManager`:
   - `[TextArea] string completionMessage`.
   - Default value (editable in Inspector):
     - `PUZZLE COMPLETE`
     - `Synchronization confirmed.`
     - `The exit door is now unlocked.`
     - `Both players may proceed to the next room.`
     - `Double-click the Exit button to close this panel and return to the game.`
3. Add serialized references in `TutorialStageManager`:
   - `DiagnosticDisplayController playerADiagnosticDisplay` / `playerBDiagnosticDisplay` (reuse existing refs if already present).
   - Optional `SceneTransitionDoor` or existing door-lock reference used by the scene.
4. Add private helper method:
   - Iterate `objectsToDisableOnComplete`.
   - For each non-null item, call `SetActive(false)`.
   - Skip null entries safely.
5. Add helper to push completion message to both diagnostics:
   - Call `SetSuccess(completionMessage)` (or equivalent body update API used by your diagnostic panel).
   - Apply to both Player A and Player B panels.
6. Add helper to unlock exit door on completion.
7. In `RaiseCompletionOnce()`, run completion actions in this order:
   - mark completion
   - disable configured objects
   - update both diagnostics with completion message
   - unlock door
   - fire existing completion events (`OnTutorialCompleted`, `onTutorialCompletedUnity`)
8. In `Tutorial.unity`, assign:
   - `objectsToDisableOnComplete`
   - `completionMessage`
   - both diagnostics references
   - exit door reference
9. Ensure panel exit behavior remains double-click-only:
   - keep `requireDoubleClickToExit = true`
   - keep threshold configurable in Inspector
   - verify single-click does not exit when double-click mode is enabled.

## Testing checklist

- Play `Tutorial.unity` and verify listed objects start active.
- Complete tutorial (both sides solved in order).
- Verify listed objects become inactive once completion triggers.
- Verify both diagnostics show the same completion message.
- Verify message text comes from Inspector field (change text once and retest).
- Verify exit door is locked before completion and unlocked after completion.
- Verify single click on Exit does not close panel when double-click mode is enabled.
- Verify double click closes panel focus and returns to gameplay.
- Verify no regression in tutorial lock states or existing completion events.

## Rollback notes

- Revert `TutorialStageManager.cs`.
- Clear inspector assignments for `objectsToDisableOnComplete` in `Tutorial.unity`.
