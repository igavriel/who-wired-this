---
task: Editor validation console output (MCP-friendly)
date: 2026-05-23
status: implemented
scope: Assets/WhoWiredThis/Editor/*Validation*.cs, EditorValidationConsoleReporter.cs
---

# Editor validation console output (MCP-friendly)

## Scope

Replace blocking `EditorUtility.DisplayDialog` on validation menu items with structured Unity Console output so Unity MCP can run validations without waiting for manual OK.

Affected validators (current codebase):

- `PipePressurePhase1ValidationTool.cs`
- `PipePressurePhase4ValidationTool.cs`
- `PipePressurePhase5ValidationTool.cs`

New shared helper:

- `EditorValidationConsoleReporter.cs` under `Assets/WhoWiredThis/Editor/`

## Out of scope

- Gameplay, scenes, prefabs, puzzle logic
- `PipeResultVisualPolishTool` confirmation dialog (not a validation tool)
- Reintroducing removed Signal Calibration validators

## Approved implementation steps

1. Add `EditorValidationConsoleReporter` — parse PASS/FAIL/WARN lines from validation report text; log summary + per-failure `LogError` + full report body via `Debug.Log`.
2. Refactor each Pipe Pressure Phase 1/4/5 validator to build the same report string as today, then call the reporter instead of `EditorUtility.DisplayDialog` on the default menu path.
3. Return a non-zero issue count (or boolean) from validation runs so menu items can optionally short-circuit follow-up steps.
4. Add optional duplicate menu items with suffix **`With Dialog`** that keep the existing popup for manual Inspector review.
5. Verify Unity MCP `read_console` can read summary and failure lines after running each default menu item.

## Testing checklist

- [x] Run each validation menu via Unity MCP — no popup on default path (Phase 1 verified)
- [x] Console shows summary + full report
- [x] MCP `read_console` can read results
- [ ] Optional `With Dialog` menus still show popup (manual OK)

## Rollback

Revert Editor scripts under `Assets/WhoWiredThis/Editor/` (remove `EditorValidationConsoleReporter.cs` and restore direct `DisplayDialog` calls in validators).
