---
name: plan-archive
description: >-
  Keeps Cursor CreatePlan markdown under `.cursor/plan/` and updates the ordered
  table in `.cursor/plan/README.md` (link, short description, optional date).
  Use when creating or revising plans, when the user mentions `.cursor/plan`,
  plan README, archiving CreatePlan output, or syncing plans from
  `~/.cursor/plans/`.
disable-model-invocation: true
---

# Plan archive (`.cursor/plan/`)

## Goal

- **Store** project-relevant plan files in **`.cursor/plan/`** (repo root), same basename as Cursor’s global cache (`~/.cursor/plans/*.plan.md`) when applicable.
- **Maintain** **[`.cursor/plan/README.md`](../plan/README.md)** as the index: ordered table with **markdown link** and **short description**, matching whatever columns the README currently uses (if a **Date** column exists, fill `YYYY-MM-DD` when known, else `—`).

Path note: this skill file lives at `.cursor/skills/plan-archive/SKILL.md`; the README is at `.cursor/plan/README.md` (sibling of `skills/`, not inside it).

## When to run

After **any** of:

- A **CreatePlan** is produced or revised for this repo.
- The user asks to **save**, **archive**, **sync**, or **copy** a plan into the project.
- Plan content was edited in `~/.cursor/plans/` and should be mirrored locally.

## Steps

### 1. Ensure the plan file exists under `.cursor/plan/`

- Target path: **`.cursor/plan/<name>.plan.md`** (e.g. `tutorial_stage_manager_4d8fbac0.plan.md`).
- If Cursor only wrote to **`~/.cursor/plans/`**, **copy** (or move, if the user asked to relocate) into `.cursor/plan/` so the repo stays the source of truth for team-visible plans.
- If the plan was created only in chat, **write** the full markdown (including YAML frontmatter) to `.cursor/plan/<name>.plan.md` using a stable, descriptive filename plus hash suffix if that matches existing Cursor naming.

### 2. Update `.cursor/plan/README.md` table

Open **[`.cursor/plan/README.md`](../plan/README.md)** and:

1. **Table** — Keep one row per plan file in the main ordered list. Match the README header row exactly. Typically:
   - **`#`** — Run / priority order (adjust if the user specifies a different sequence).
   - **Plan (link)** — Markdown link: `[filename](filename)` (relative to `README.md` in the same folder).
   - **Short description** — One line from the plan’s YAML **`overview:`** (strip surrounding quotes, collapse `\n`, truncate ~120 characters, escape `|` in table cells).
   - **Date** (optional column) — If present: `YYYY-MM-DD` when the user or conversation gives a date; otherwise **`—`**. The README may explain mtime via `stat` (see existing note there).

2. **New plan** — Append a row (or insert at the user’s requested `#`) and renumber following rows if needed.

3. **Removed / renamed plan** — Remove or fix the row and link; renumber `#` for consistency.

4. **Do not** paste the full plan body into the README; **links + short description only**.

### 3. Keep `docs/cursor-plans-index.md` honest

If that file exists, it should **point** to `.cursor/plan/README.md` for the table (do not duplicate the full table unless the user asks).

### 4. Refresh block (optional)

Preserve the existing **“Refresh plans from Cursor cache”** shell snippet in `README.md` unless the user wants it changed. Filter for this repo can stay:

`WhoWiredThis|who-wired-this|Split Tutorial|MultiDimension`

Broaden only if the user asks to archive more plans.

## Checklist

```text
Plan archive progress:
- [ ] Plan file present as .cursor/plan/<name>.plan.md
- [ ] README.md table updated (link, description; date column if used)
- [ ] # column order matches user intent
- [ ] docs/cursor-plans-index.md still points at README if present
```
