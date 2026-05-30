---
name: unity-mcp-preflight-cursor
description: Use before the first Unity MCP operation for Who Wired This scene, prefab, hierarchy, console, compile, or serialized-field work. This is a Codex wrapper around `.cursor/skills/unity-mcp-preflight/SKILL.md`.
---

# Unity MCP Preflight Cursor Wrapper

Read `.cursor/skills/unity-mcp-preflight/SKILL.md` before the first Unity MCP
call in a task that depends on live Unity Editor state.

Codex adaptation:

- Use the Unity MCP tools available in this session, such as `manage_scene`,
  `read_console`, `refresh_unity`, or related resource reads.
- Treat a transport error, missing Unity project, unavailable editor, or stale
  console/compile state as a failed preflight.
- If preflight fails, ask whether to continue using repo files/manual Inspector
  notes only, or stop until the Unity MCP connection is fixed.
