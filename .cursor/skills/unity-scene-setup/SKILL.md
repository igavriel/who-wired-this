---
name: unity-scene-setup
description: Builds or updates Unity scenes with strict safety checks, required camera/light baseline, local multiplayer parity, and ScriptableObject-based wiring. Use when creating new scenes, wiring prefabs, or setting up tutorial/first-person scene flow. Apply the risky-change gate for scenes, prefabs, UI architecture, cameras, input routing, save/load, scoring, or menu flow.
disable-model-invocation: true
---

# Unity Scene Setup

## Purpose

Use this skill to create or update scenes in `who-wired-this` with a safety-first workflow and predictable wiring.

## Risky change gate

**Apply this section before any implementation** when the task touches **scenes**, **prefabs**, **UI architecture**, **cameras**, **input routing**, **save/load**, **scoring**, or **menu flow** — even if the change looks small.

1. **Plan first.** Produce a short plan (scope, files/scenes/prefabs touched, risks, test idea). **Do not implement until the user approves**, unless they explicitly say **implement now** (or equivalent). For CreatePlan output, follow **`plan-archive`** (`.cursor/skills/plan-archive/SKILL.md`).
2. **Prefer a development scene.** Create a dedicated development scene or **duplicate** the working scene before modifying the known-good scene (e.g. copy `Split Tutorial.unity` → `Split Tutorial Dev.unity`).
3. **Do not edit working prefabs directly** unless the user explicitly approves touching that prefab.
4. **If a prefab must change**, prefer a **new variant** or **duplicate** for experimentation; merge back only after approval.
5. **Preserve the known-working setup.** Keep the production/tutorial scene and baseline prefabs intact unless the user asked to update them.
6. **Ask clarifying questions** before implementation if scene/prefab ownership, display routing, player parity, or which scene is “source of truth” is ambiguous.
7. **After implementation:** compile, check the Unity console (MCP `read_console` when available), and report **exact rollback notes** (scene/prefab paths, Git revert command, or steps to restore duplicates).

Configuration-only edits (serialized strings, Inspector values on a scene instance with no prefab/asset change) may skip duplicating the scene if the user scoped the task that way — still plan when impact is unclear.

## Execution Checklist

Copy and track this checklist:

```text
Scene Setup Progress:
- ⬜ Step 0: Risky-change gate — plan approved; dev scene/variant strategy confirmed
- ⬜ Step 1: Confirm scope and target scene
- ⬜ Step 2: Inspect existing architecture and required prefabs/data
- ⬜ Step 3: Ensure Camera + Directional Light baseline
- ⬜ Step 4: Place/wire gameplay objects and player slots
- ⬜ Step 5: Validate local multiplayer parity and viewport behavior
- ⬜ Step 6: Validate ScriptableObject references and asset placement
- ⬜ Step 7: Run compile + console checks, fix issues
- ⬜ Step 8: Save scene/assets, report changes + rollback notes
```

## Step-by-Step Workflow

### Step 0: Risky-change gate (when applicable)

- Classify the task against the **Risky change gate** triggers above.
- If risky: write the plan, propose dev scene / prefab variant approach, and wait for approval.
- Record which scene is canonical vs experimental before editing.

### Step 1: Confirm scope and target scene

- Identify whether this is a new scene, a **duplicate dev scene**, or updates to an existing scene.
- Confirm whether flow is single-player first-person, local duel, tutorial, or hybrid.
- If requested behavior is ambiguous and affects player parity, display routing, or data ownership, stop and ask.

### Step 2: Inspect existing architecture and dependencies

- Check current managers, player prefabs, tutorial coordinators, and station config assets in use.
- Reuse existing systems (`PlayerActions`, tutorial configurators, existing manager contracts) before adding new systems.
- Prefer minimal deltas; avoid editing shared/working prefabs without approval (see **Risky change gate**).

### Step 3: Ensure baseline scene essentials

- Ensure one active Camera suitable for the target topology.
- Ensure one main Directional Light exists.
- Keep scene references inspector-driven; avoid runtime lookup-based scene bootstrap for core gameplay objects.

### Step 4: Place and wire gameplay objects

- Add required prefabs from existing project prefab roots before creating new prefabs.
- Wire serialized references explicitly in Inspector-facing fields.
- For tutorial/first-person scene setup, keep player-related objects linked through known adapters/configurators.
- Prefer scene-instance overrides over modifying source prefabs when experimenting.

### Step 5: Enforce local multiplayer parity

- Validate Player A and Player B behavior symmetry where expected.
- If editing one player variant prefab, check whether matching updates are needed for the counterpart variant.
- Preserve compatibility with existing split/single viewport switching behavior.

### Step 6: Enforce ScriptableObject-driven configuration

- Keep tuning/setup data in ScriptableObject assets instead of hardcoded values in scene scripts.
- Place new data assets under existing domain folders and use deterministic naming by role/player.
- Ensure each scene object that requires config has an explicit ScriptableObject reference assigned.

### Step 7: Mandatory validation loop

- After script edits, wait for Unity compilation and confirm no new compile errors.
- Read console output and resolve new errors before finalizing.
- Treat missing-reference warnings as blockers for completion unless user explicitly accepts the risk.

### Step 8: Save and report

- Save modified scene(s), prefabs, and data assets.
- Summarize changed objects/assets and why each change was required.
- Include **rollback notes**: files to revert, duplicate scenes/prefabs to delete, or Inspector fields to restore.
- Highlight any unresolved risks, unknowns, or follow-up tasks.

## Guardrails

- Do not introduce parallel architecture when existing project systems already cover the need.
- Do not use broad scene-wide edits when focused prefab/serialized updates are sufficient.
- Do not ship scene setup changes without compile + console verification.
- Do not modify working prefabs or canonical tutorial/production scenes without explicit user approval; use duplicates, variants, or dev scenes first.
