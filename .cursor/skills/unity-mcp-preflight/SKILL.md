---
name: unity-mcp-preflight
description: >-
  Runs a Unity MCP connectivity check before scene/prefab/MCP work in who-wired-this.
  If MCP fails, asks whether to continue without MCP or stop to fix the connection.
  Use at the start of any task that would use Unity MCP (implementation, planning with
  hierarchy inspection, scene/prefab wiring, compile/console validation via MCP).
---

# Unity MCP preflight

## When to run

Run this **once per conversation** before the first Unity MCP call, and again if a prior MCP call failed or `STATUS.md` reports an error.

**Run preflight when the task may use Unity MCP**, including:

- Scene or prefab inspection or edits
- Hierarchy / serialized field verification
- `read_console`, `editor_state`, `manage_scene`, `manage_gameobject`, `manage_prefabs`, etc.
- Implementation or planning that depends on MCP instead of guessing object paths

**Skip preflight** when the task is clearly MCP-independent (e.g. pure C# logic with no scene/prefab verification, docs-only, git-only).

## Preflight steps (in order)

1. **Read server status** (if present):  
   `mcps/user-unityMCP/STATUS.md` under the Cursor project MCP folder.  
   If it says the server errored, treat that as a failed preflight signal (still run step 3).

2. **Confirm descriptors exist**:  
   At least one tool under `mcps/user-unityMCP/tools/` (e.g. `read_console.json`).  
   If the tools folder is missing or empty, MCP is not wired for this session → **failed**.

3. **Live probe** (read schema first, then call):
   - **Preferred:** `FetchMcpResource` — server `user-unityMCP`, URI `mcpforunity://editor/state` (resource `editor_state`).
   - **Fallback tool:** `CallMcpTool` — server `user-unityMCP`, tool `read_console`, arguments `{ "action": "get", "count": "1", "types": ["error"] }`.

   **Pass:** probe returns without transport/server errors and payload looks like real Unity editor data (not “server does not exist”, auth failure, or empty tool list).

   **Fail:** any error, timeout, “MCP server does not exist”, “No MCP servers available”, missing tools, or `STATUS.md` errored.

## If preflight fails — stop and ask

Do **not** call other Unity MCP tools until the user decides.

Present a short failure summary (what step failed, exact error snippet).

Ask the user **exactly this choice** (use **AskQuestion** when available):

| Option | Meaning |
|--------|---------|
| **Continue without MCP** | Proceed using repo files, grep, and manual Inspector steps only. State assumptions clearly; do not invent hierarchy paths. |
| **Stop and fix connection** | Halt implementation/planning that needs MCP. User fixes Unity Editor + MCP package + Cursor MCP settings, then retries. |

Suggested fix hints (only if user chose stop):

- Unity Editor open on **this** project (`who-wired-this`)
- Package `com.coplaydev.unity-mcp` connected in the Editor
- Cursor **Settings → MCP** — `user-unityMCP` / unityMCP enabled and healthy
- Re-run preflight after reconnect

## After user choice

### Continue without MCP

- Do not use `CallMcpTool` / `FetchMcpResource` for Unity for the rest of the task unless the user asks to re-test MCP.
- Prefer reading `.unity` / `.prefab` YAML and existing scripts; label unverified wiring as **manual Inspector required**.
- In the implementation report, note: **MCP unavailable — manual verification required.**

### Stop and fix connection

- Do not implement scene/prefab/MCP-dependent steps.
- You may still answer questions from repo context if helpful.
- Wait for the user to confirm MCP is working, then re-run preflight from step 1.

## Pass record

When preflight passes, briefly note once: **Unity MCP preflight OK** (`editor_state` or `read_console` probe). Then use MCP per `unity-poc-workflow` and other project rules.

## Related

- Server id: `user-unityMCP` (display name may be `unityMCP`)
- Project rule: `.cursor/rules/unity-poc-workflow.mdc` §2
- Risky scene work: `.cursor/skills/unity-scene-setup/SKILL.md`
