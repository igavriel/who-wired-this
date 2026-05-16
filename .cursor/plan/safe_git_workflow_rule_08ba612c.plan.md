---
name: Safe Git Workflow Rule
overview: Add a new "Safe Development Workflow with Git" section to the always-applied Unity POC workflow rule, scoped to risky Unity work. Single-file edit; no Unity assets or gameplay scripts.
todos:
  - id: append-section-10
    content: "Append ## 10. Safe Development Workflow with Git to unity-poc-workflow.mdc"
    status: pending
  - id: confirm-scope
    content: Show new section to user; confirm no Unity scenes/prefabs/gameplay scripts changed
    status: pending
isProject: false
---

# Safe Development Workflow Rule Update

## Scope

**In scope:** Edit only [`.cursor/rules/unity-poc-workflow.mdc`](.cursor/rules/unity-poc-workflow.mdc).

**Out of scope (unchanged):** Unity scenes, prefabs, C# gameplay scripts, `.cursor/plan/`, or other rule files.

## Placement

Add **## 10. Safe Development Workflow with Git** after the existing **## 9. Project context (POC)** block (end of file). This keeps current section numbers stable and treats the new content as an additive guardrail for **risky** work without rewriting sections 1–9.

Optionally extend the YAML frontmatter `description` line to mention Git-safe workflow for risky changes (one short phrase). Not required for correctness.

## Relationship to existing sections

The new section **specializes** existing rules for high-risk changes; it does not replace them:

| Existing | New section reinforces |
|----------|-------------------------|
| §1 Planning first | §10.1 Plan first (MCP inspection, plan-only until approval) |
| §5 Scene and prefab rules | §10.3–4 Dev scenes / prefab variants vs in-place edits |
| §7–8 Compile + response format | §10.6 Verification + rollback in Git terms |

When work is **not** risky (small script fix, docs, rule edits), agents continue following §1–9 only; §10 applies when the task touches the listed risk areas.

## Section content (to insert verbatim in structure)

**Trigger — apply §10 when work involves any of:**

- Scene architecture
- Prefab architecture
- UI canvas refactors
- Cameras or displays
- Input routing
- Scoring systems
- Menus
- Save/load or PlayerPrefs
- Systems shared by both players

**### 1. Plan first**

- Inspect project and relevant assets with Unity MCP where possible.
- Ask clarifying questions if ownership, routing, prefab usage, display setup, or expected behavior is ambiguous.
- Return an implementation plan only; do not implement until the user approves.

**### 2. Git safety**

- Before risky implementation, remind the user to commit or confirm the working tree is safe.
- Git is the primary rollback mechanism; do not create manual backup prefabs/scenes by default.
- If the working tree is dirty and the task is risky, warn before implementation.
- Do not run destructive Git commands; do not reset, checkout, clean, revert, or delete user changes unless explicitly instructed.

**### 3. Preserve working scenes during risky refactors**

- Do not directly modify a known-working scene unless explicitly approved.
- Prefer duplicating into a dedicated dev scene (e.g. `Split Tutorial_UIRefactor`, `Split Tutorial_Score`, `Split Tutorial_Menu`).
- Merge back into the main working scene only with explicit approval.

**### 4. Preserve working prefabs**

- Do not directly modify working prefabs unless explicitly approved.
- Prefer prefab variants or duplicated prototype prefabs for experimental/reusable work.
- If an existing prefab must change, state which prefab and why.
- Avoid `Old`/`Backup`/`Copy` backup prefabs unless requested.

**### 5. Small steps**

- Break large refactors into small steps; each step compiles and is testable.
- Do not combine unrelated changes in one implementation.

**### 6. Verification**

- After implementation: check Unity compile / console.
- Report what changed, which scenes changed, which prefabs changed, how to test, and rollback notes (prefer Git terms).

**### 7. User approval**

- If the plan requires modifying a working scene or working prefab, ask for explicit approval before implementation.

## Implementation step (after you approve)

1. Open [`.cursor/rules/unity-poc-workflow.mdc`](.cursor/rules/unity-poc-workflow.mdc).
2. Append section 10 with the seven subsections above (using `##` / `###` headings consistent with the file’s current style).
3. Optionally bump frontmatter `description` to include “Git-safe workflow for risky changes”.
4. Reply with the full new section text and explicit confirmation: no scenes, prefabs, or gameplay scripts modified.

## Verification

- Diff is a single `.mdc` file under `.cursor/rules/`.
- `git status` shows only that rule file (no `Assets/` changes).
