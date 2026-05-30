---
name: who-wired-this-cursor-bridge
description: Use when working in the Who Wired This Unity repo to load the relevant Cursor rules as the canonical project guidance for Codex. This wrapper points to `.cursor/rules/` and Cursor skills instead of duplicating their content.
---

# Who Wired This Cursor Bridge

## Purpose

Use this skill as the Codex entry point for project-specific behavior in this
repo. Cursor files remain canonical; this wrapper only tells Codex which files
to read and how to map them into Codex work.

## Required Sources

Read the files that match the task:

- Always for Unity work: `.cursor/rules/unity-poc-workflow.mdc`
- C# scripts: `.cursor/rules/unity-csharp.mdc`
- `Assets/WhoWiredThis/Scripts/**/*.cs`: `.cursor/rules/who-wired-this-architecture.mdc`
- Safety-sensitive Unity assets: `.cursor/rules/who-wired-this-safety-validation.mdc`
- Input or UI scripts: `.cursor/rules/who-wired-this-input-and-ui.mdc`
- Interface-backed Inspector refs: `.cursor/rules/interface-inspector-enforcement.mdc`
- Player parity or data/config work: `.cursor/rules/who-wired-this-localmultiplayer-scriptableobjects.mdc`
- Commit or PR requests: `.cursor/rules/conventional-commits.mdc`

## Cursor Skills

- Scene/prefab setup: `.cursor/skills/unity-scene-setup/SKILL.md`
- Unity MCP preflight: `.cursor/skills/unity-mcp-preflight/SKILL.md`
- Plan archiving: `.cursor/skills/plan-archive/SKILL.md`

Load only the relevant skill file when the task needs that workflow.

## Codex Adaptation

- Cursor-specific tool names such as `FetchMcpResource`, `CallMcpTool`, or
  `AskQuestion` should be translated to the available Codex/Unity MCP tools.
- Keep plan archives in `.cursor/plan/` because Cursor and Codex share this repo.
- If a Cursor rule conflicts with a higher-priority Codex/developer instruction,
  follow the higher-priority instruction and mention the conflict when useful.
