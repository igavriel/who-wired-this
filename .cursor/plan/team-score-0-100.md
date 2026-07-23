---
name: Team score 0-100
overview: Remove best-time/crew-rank; add team score 0–100 from time (2/5/8 min bands) + attempts (−2 each); 8:00 per-level TopBar countdown.
date: 2026-07-23
status: implemented
---

# Team score 0–100 + per-level 8:00 countdown

## Task name

Remove best time/rank; add team score 0–100; TopBar level countdown

## Date

2026-07-23

## Scope

- Remove PlayerPrefs best-time tracking and UI wiring (`PlaytestBestTimeSeconds`, boss-reset-best, RANK/BEST grid lines, `GetCrewRank`)
- Keep the existing 50×12 run summary table (status + per-level Blue/Red + totals)
- Add a deterministic **team score 0–100** from per-scene time (2/5/8 min) + total run **attempts** (every submit counts)
- Surface score on the summary grid as a `SCORE` line
- TopBar timer: **per-level reverse countdown 8:00 → 0:00**, reset each gameplay level
- Countdown expire → Game Over (abandoned)

## Out of scope

- Custom end-game cutscene beyond timeout → Game Over
- Persistent high-score leaderboard
- Retries in score or Game Over UI

## Locked score formula

```
TimeScore_i = piecewise(clamp(SceneTotalSeconds_i, 0, 480))
AvgTimeScore = average over Tutorial / Puzzle Pipes / Puzzle Signal (missing = 0)

totalAttempts = sum Blue.Attempts + Red.Attempts (all levels)
AttemptScore = max(0, 100 - 2 * totalAttempts)

TeamScore = round(0.5 * AvgTimeScore + 0.5 * AttemptScore) clamped 0..100
```

Time bands: ≤2:00 → 100; 2–5 min lerp 100→50; 5–8 min lerp 50→0; >8:00 → 0.

## Approved implementation steps

1. ✅ `PlaytestTeamScoreCalculator.cs`
2. ✅ Strip best-time / crew-rank from GameOver + Start controllers
3. ✅ Summary grid: SCORE, attempts/time cells, ATTEMPTS + SCENES footer
4. ✅ `TimerManager` level countdown; HUD shows remaining; expire → Game Over
5. ⬜ Manual Play Mode sign-off

## Testing checklist

- ⬜ Game Over shows SCORE + attempts/time table; no retries; ATTEMPTS + SCENES footer
- ⬜ End-test B (5:00×3, 10 attempts) → SCORE 65
- ⬜ TopBar 08:00 countdown per level; resets on scene change
- ⬜ 0:00 → Game Over abandoned
- ⬜ Dual TopBars in sync

## Rollback notes

Revert scorer + Game Over/Start/formatter + TimerManager/HUD changes via Git.
