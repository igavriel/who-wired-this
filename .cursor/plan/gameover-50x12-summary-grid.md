---
name: Game Over 50x12 Summary Grid
overview: Reformat the Game Over run summary into a fixed monospace 50×12 character grid so all status, per-level Blue/Red retries+times, and totals fit and display correctly on the GameOver canvases.
date: 2026-07-23
status: implemented
---

# Game Over 50×12 Summary Grid

## Task name

Game Over run-summary 50×12 character grid

## Date

2026-07-23

## Scope

- Reformat `PlaytestRunSummary.FormatDisplayText()` into a **fixed 50 columns × 12 rows** monospace block
- Reuse the existing diagnostic pad/dot-leader pattern (`ComponentDiagnosticLogFormatter`)
- Apply the block to both dual-display GameOver TMP targets reliably
- Ensure TMP settings do not wrap/clip mid-grid (overflow truncate or overflow; wrap off; monospace font)

## Out of scope

- High-score leaderboard UI
- Changing best-time PlayerPrefs logic
- Redesigning Restart button / title / background art
- Full GameOver canvas visual redesign beyond the summary text block

## Why current display fails

1. **No dedicated `RunSummaryText`** on [`UI-Canvas-GameOver.prefab`](Assets/WhoWiredThis/Prefabs/Game/UI-Canvas-GameOver.prefab) — `ApplyRunSummaryDisplays()` falls back to **`CrewRankText`**.
2. **Lines are too wide** for a 50-col grid, e.g.  
   `Tutorial — Blue: 0 retries 00:07 | Red: 1 retry 00:06 | Scene 00:06` (~72 chars).
3. **Too many rows** (header + blanks + status + per-level prose) routinely exceed **12 lines**.
4. **Duplicate fields**: controller still sets `CompletionTimeText` / `BestTimeText` / `CrewRankText`, then overwrites `CrewRankText` with the long summary → clipped / overlapping info.

## Approved layout (exactly 50×12)

Header/status/footer rows still use `.` label leaders. The **level table is centered with spaces** — no trailing `.....` on table rows.

### Level data row (locked columns, centered)

Core content is **43 chars**, centered in 50 with **3 spaces left + 4 spaces right** (no dots):

```
|   Tutorial          03 / 01:12     02 / 00:48    |
 0--------1---------2---------3---------4---------5
 01234567890123456789012345678901234567890123456789
```

| Region | Cols (0-based) | Width | Content |
|--------|----------------|-------|---------|
| Left margin | 0–2 | 3 | spaces (center) |
| Level name | 3–20 | 18 | left-aligned (`Tutorial`, `Puzzle Pipes`, `Puzzle Signal`) |
| Blue | 21–30 | 10 | `RR / mm:ss` (e.g. `03 / 01:12`) |
| Gap | 31–35 | 5 | spaces |
| Red | 36–45 | 10 | `RR / mm:ss` (e.g. `02 / 00:48`) |
| Right margin | 46–49 | 4 | spaces (center) |

Empty / not-played cell: `00 / --:--` (still 10 chars).

Header row (same centering, no dots):

```
|   LEVEL             BLUE           RED           |
```

### Full 12-line screen

```
01|RUN SUMMARY..................................01:03|
02|STATUS...................................Abandoned|
03|LAST..................................Puzzle Pipes|
04|RANK...........................Certified Operators|
05|--------------------------------------------------|
06|   LEVEL             BLUE           RED           |
07|   Tutorial          03 / 01:12     02 / 00:48    |
08|   Puzzle Pipes      01 / 00:40     00 / 00:35    |
09|   Puzzle Signal     00 / --:--     00 / --:--    |
10|--------------------------------------------------|
11|TOTALS......................retries 5 / attempts 8|
12|BEST.............................01:03  scenes 2/3|
```

Status values shortened to fit: `Completed` / `Abandoned` / `Ended`.

## Approved implementation steps

1. **Formatter** — Add `PlaytestRunSummaryGridFormatter` (or fold into `PlaytestRunSummary`) that:
   - Constants `Width = 50`, `TotalLines = 12`
   - Builds the 12 lines above from `PlaytestRunSummaryData` + `ScoreManager.Levels` + crew rank string
   - Pads/truncates via `ComponentDiagnosticLogFormatter.FormatLabelStatus` / `FitToScreen`
   - Always emits **exactly** 12 newline-joined lines, each ≤50 chars
2. **`FormatDisplayText()`** — Delegate to the grid formatter (single source of truth for GameOver body).
3. **`GameOverSceneController`** — Prefer writing the grid to `CrewRankText` (current real target) **or** add/rename a `RunSummaryText` on the prefab; stop writing a conflicting short “Crew Rank: …” string when the grid already includes RANK. Keep `CompletionTimeText` / `BestTimeText` either:
   - **Option A (recommended):** leave them as redundant top labels (grid also has TIME/BEST), or
   - **Option B:** clear/hide them so only the 50×12 block carries run info  
   Default in implementation: **Option A** unless you prefer B.
4. **Prefab TMP hardening** (MCP on [`UI-Canvas-GameOver.prefab`](Assets/WhoWiredThis/Prefabs/Game/UI-Canvas-GameOver.prefab)):
   - Target text: `CrewRankText` (or new `RunSummaryText`)
   - `enableWordWrapping = false`
   - `overflowMode = Overflow` or `Truncate`
   - Monospace SDF font (match diagnostic boards if available)
   - Rect tall enough for 12 lines at chosen font size
5. **Compile + smoke test** — load GameOver with a fake summary (3 levels) and confirm both displays show a full 12×50 block with no wrap.

## Files likely to change

- [`Assets/WhoWiredThis/Scripts/Core/PlaytestRunSummary.cs`](Assets/WhoWiredThis/Scripts/Core/PlaytestRunSummary.cs)
- New (optional): `Assets/WhoWiredThis/Scripts/Core/PlaytestRunSummaryGridFormatter.cs`
- [`Assets/WhoWiredThis/Scripts/UI/GameOverSceneController.cs`](Assets/WhoWiredThis/Scripts/UI/GameOverSceneController.cs)
- [`Assets/WhoWiredThis/Prefabs/Game/UI-Canvas-GameOver.prefab`](Assets/WhoWiredThis/Prefabs/Game/UI-Canvas-GameOver.prefab)

## Risks

- **Font not monospace** → columns drift visually even if char count is 50.
- **Rect too short** → bottom rows clipped even with correct string.
- **Dual canvas instances** — must apply to every matching TMP on both displays (existing loop is fine if names are consistent).

## Testing checklist

- ✅ Formatter smoke: exactly 12 lines × 50 chars; centered table without dots
- ⬜ Abandoned run with Tutorial + Pipes partial: Blue/Red retries visible on GameOver canvases
- ⬜ Completed full run: Signal row populated; STATUS Completed; scenes 3/3
- ✅ Empty/missing level shows `00 / --:--` placeholders
- ⬜ Both Player A and Player B GameOver canvases show identical grid (Play Mode)
- ✅ Unity compiles; no TMP overflow/wrap of mid-line content

## Rollback notes

Revert formatter + controller + prefab TMP changes. Git is primary rollback.
