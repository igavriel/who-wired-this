---
name: unity-scene-setup-cursor
description: Use when creating or updating Unity scenes, prefabs, UI architecture, cameras, input routing, scoring, menu flow, or local multiplayer wiring in Who Wired This. This is a Codex wrapper around `.cursor/skills/unity-scene-setup/SKILL.md`.
---

# Unity Scene Setup Cursor Wrapper

Read `.cursor/skills/unity-scene-setup/SKILL.md` before planning or changing
Unity scenes, prefabs, UI architecture, cameras, input routing, scoring, menus,
or shared player systems.

Also read these Cursor rules as needed:

- `.cursor/rules/unity-poc-workflow.mdc`
- `.cursor/rules/who-wired-this-safety-validation.mdc`
- `.cursor/rules/who-wired-this-localmultiplayer-scriptableobjects.mdc`
- `.cursor/rules/who-wired-this-architecture.mdc`

Codex adaptation:

- Use available Unity MCP tools instead of Cursor-specific MCP commands.
- Keep risky scene/prefab changes small and prefer dev scenes or variants when
  the Cursor workflow requires them.
- Report changed scene/prefab paths, validation status, and rollback notes.
