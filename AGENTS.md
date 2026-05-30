# Who Wired This - Codex Agent

This repository keeps its project AI rules in Cursor files. Treat the Cursor
configuration as the source of truth and read the relevant files before changing
scripts, scenes, prefabs, serialized assets, or plans.

## Cursor Sources

- General Unity POC workflow: `.cursor/rules/unity-poc-workflow.mdc`
- Unity/C# conventions: `.cursor/rules/unity-csharp.mdc`
- Project architecture: `.cursor/rules/who-wired-this-architecture.mdc`
- Safety and validation: `.cursor/rules/who-wired-this-safety-validation.mdc`
- Input and UI rules: `.cursor/rules/who-wired-this-input-and-ui.mdc`
- Interface-safe Inspector references: `.cursor/rules/interface-inspector-enforcement.mdc`
- Local multiplayer and ScriptableObject rules: `.cursor/rules/who-wired-this-localmultiplayer-scriptableobjects.mdc`
- Commit message format: `.cursor/rules/conventional-commits.mdc`

## Codex Skill Wrappers

Thin Codex wrappers live under `codex/skills/` and point back to the Cursor
skills instead of duplicating them:

- `codex/skills/who-wired-this-cursor-bridge/SKILL.md`
- `codex/skills/unity-scene-setup-cursor/SKILL.md`
- `codex/skills/unity-mcp-preflight-cursor/SKILL.md`
- `codex/skills/plan-archive-cursor/SKILL.md`

Use these wrappers as navigation aids. If a wrapper and a Cursor file disagree,
prefer the Cursor file unless a higher-priority Codex instruction conflicts.

## Working Rules For Codex

- Before implementation, inspect the relevant project files and Cursor rules.
- For Unity scene, prefab, UI architecture, camera, input, scoring, menu, or
  shared player-system changes, follow the risky-change workflow in
  `.cursor/rules/unity-poc-workflow.mdc` and `.cursor/skills/unity-scene-setup/SKILL.md`.
- Before using Unity MCP for scene/prefab inspection or validation, follow the
  intent of `.cursor/skills/unity-mcp-preflight/SKILL.md`, adapted to the
  Unity MCP tools available in this Codex session.
- When creating or materially revising a project plan, archive it under
  `.cursor/plan/` and update `.cursor/plan/README.md` according to
  `.cursor/skills/plan-archive/SKILL.md`, unless the user explicitly says not to.
- After C# edits, request Unity refresh/compilation when possible, read the
  console, and resolve newly introduced errors before final delivery.
- Preserve existing systems and Inspector-driven wiring. Avoid broad refactors,
  runtime-created gameplay objects, `GameObject.Find`, and new global lookup
  patterns unless the Cursor rules and surrounding code support that choice.
- Do not revert or overwrite existing user changes in the dirty Unity working
  tree. Work with them or report a blocker if they make the task impossible.
