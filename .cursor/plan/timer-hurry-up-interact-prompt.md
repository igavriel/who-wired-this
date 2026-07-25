---
name: Timer hurry-up interact prompt
overview: Keep Game Over on level timer expiry; show a 10→0 HURRY UP alert on both players’ interact prompt line during the final seconds.
date: 2026-07-25
status: implemented
---

# Timer hurry-up interact prompt

## Task name

Level timer hurry-up alert on interact prompt + confirm Game Over on expiry

## Date

2026-07-25

## Scope

- Confirm / keep **Game Over when level countdown hits 0** (already in `TimerManager.HandleLevelExpired` → `PlaytestFlowUtility.TryEndRunAndLoadGameOver`).
- Show a **hurry-up alert on the interact prompt line** for both players while remaining time is **10 … 1** (and optionally **0** for one frame before scene load).
- Optional tunable on `GameConfigSO` for the warning window (default **10** seconds).

## Out of scope

- Changing TopBar timer formatting
- New TMP objects or prefab layout changes (reuse existing `interactPromptText`)
- Audio / color flash (follow-up if wanted)
- Pausing the timer during cutscenes beyond current behavior

## Current behavior (verified)

| Piece | Status |
|-------|--------|
| 8:00 countdown | `TimerManager.StartLevelCountdown` via `GameConfig.SceneTimeCapSeconds` |
| Expire → Game Over | `TimerManager.HandleLevelExpired` (active run + gameplay level) |
| Interact prompt line | `PlayerHudView.interactPromptText` / `HUDController.interactPromptText` set by `PlayerActions` |

**Conflict:** `PlayerActions` rewrites the prompt when near an interactable. Hurry-up must win during the final window, or it will be overwritten every frame.

## Approved approach

1. **`GameConfigSO`** — add `hurryUpSeconds = 10` under Team score (Inspector-tunable).
2. **`PlayerHudView`** (+ legacy `HUDController` if still used) — add urgency override:
   - `SetUrgencyPrompt(string)` / clear when null
   - `SetInteractPrompt` still stores interact text
   - Display priority: **urgency wins** when set; otherwise normal interact prompt
3. **`SharedHudPresenter`** — on `OnTimerUpdated`, when countdown active and `remaining <= hurryUpSeconds`:
   - Push both views: e.g. `HURRY UP! 10` … `HURRY UP! 1` (ceil/floor of remaining seconds, clamped 0–hurryUp)
   - When remaining &gt; window or countdown inactive: clear urgency so interact prompts return
4. **No change** to Game Over path unless Play Mode shows it not firing (then fix guards only).

### Copy (locked)

```
HURRY UP! {n}
```

Where `{n}` is integer seconds remaining from **10** down to **1** (and `0` only if still visible before load).

## Approved implementation steps

1. Add `hurryUpSeconds` to `GameConfigSO` (+ asset default 10).
2. Extend `PlayerHudView` with urgency override + refresh helper.
3. Mirror minimal support on `HUDController` if dual-HUD still falls back to it.
4. Drive urgency from `SharedHudPresenter.HandleTimerUpdated` using `TimerManager.IsCountdownActive` + remaining seconds.
5. Compile check; Play Mode: let timer hit final 10s (or temporarily set `sceneTimeCapSeconds` / start mid-countdown for test).

## Testing checklist

- ⬜ At remaining ≤ 10: both HUDs show `HURRY UP! N` on interact prompt line
- ⬜ Near an interactable, hurry text still shows (urgency wins)
- ⬜ Above 10s: normal interact prompts work again
- ⬜ At 0: Game Over loads (abandoned run) on Tutorial / Pipes / Signal
- ✅ Unity compiles clean

## Risks

- Urgency winning hides interact hints for last 10s — intentional per request.
- Cutscene / non-gameplay scenes should not show hurry (gated by `IsCountdownActive`).

## Rollback notes

Revert `GameConfigSO` field, `PlayerHudView`, `SharedHudPresenter` (+ optional `HUDController`) via Git. Timer Game Over path unchanged if left alone.
