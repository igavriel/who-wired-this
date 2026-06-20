---
task: Puzzle Pipes completion popup → CutScene-Pipe-Signal transition
date: 2026-05-30
status: implemented
overview: Retarget Puzzle Pipes completion popup dismiss to load CutScene-Pipe-Signal via CompletionPopupSceneTransition; add cutscene to build settings; cutscene exits to Puzzle Signal.
related_assets: Assets/Scenes/Game/Puzzle Pipes.unity, Assets/Scenes/Game/CutScene-Pipe-Signal.unity
---

# Puzzle Pipes completion popup → CutScene-Pipe-Signal transition

## Task name

Puzzle Pipes completion popup dismiss → configurable cutscene load (`CutScene-Pipe-Signal`).

## Date

2026-05-30

## Scope

When players complete **Puzzle Pipes** and either player dismisses the summary popup (Close or **Action**), dual-HUD fade then load **`CutScene-Pipe-Signal`**. Cutscene exit (existing `CinemachinePrioritySceneTransition` on **`Next Scene Selector`**) loads **`Puzzle Signal`**.

### Updated playtest chain (Pipes slice)

```text
… → CutScene-Tutorial-Pipe → Puzzle Pipes → CutScene-Pipe-Signal → Puzzle Signal → …
```

## Out of scope

- Puzzle Signal → GameOver (or cutscene) — follow-up plan.
- New runtime scripts; reuses `CompletionPopupSceneTransition` + `CinemachinePrioritySceneTransition`.

## Approved implementation steps

1. ✅ `Puzzle Pipes.unity` — `CompletionPopupSceneTransition.targetSceneName` = **`CutScene-Pipe-Signal`** on `TutorialStageManager` GO.
2. ✅ `EditorBuildSettings` — enable **`CutScene-Pipe-Signal.unity`** after Puzzle Pipes, before Puzzle Signal.
3. ✅ `TutorialCompletionTransitionWireTool` — Pipes menu default **`CutScene-Pipe-Signal`**; Tutorial menu aligned to **`CutScene-Tutorial-Pipe`**.
4. ✅ Walk-through `NextLevel` `SceneTransitionTrigger` — remains disabled; target updated to cutscene name for consistency.

## Testing checklist

- ⬜ Complete Puzzle Pipes — summary popup on both HUDs.
- ⬜ Dismiss via Action or Close → fade → **`CutScene-Pipe-Signal`** loads.
- ⬜ Cutscene plays → **`Puzzle Signal`** loads.
- ⬜ Walk `NextLevel` trigger does not double-load.
- ⬜ `PlaytestRunTotal` records Pipes scene time before transition.

## Rollback notes

- Revert `EditorBuildSettings`, `Puzzle Pipes.unity` target, wire tool strings.
- Cutscene scene unchanged (already wired to Puzzle Signal).
