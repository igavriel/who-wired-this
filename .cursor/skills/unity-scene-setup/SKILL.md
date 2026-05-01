---
name: unity-scene-setup
description: Builds or updates Unity scenes with strict safety checks, required camera/light baseline, local multiplayer parity, and ScriptableObject-based wiring. Use when creating new scenes, wiring prefabs, or setting up tutorial/first-person scene flow.
disable-model-invocation: true
---

# Unity Scene Setup

## Purpose
Use this skill to create or update scenes in `who-wired-this` with a safety-first workflow and predictable wiring.

## Execution Checklist

Copy and track this checklist:

```text
Scene Setup Progress:
- [ ] Step 1: Confirm scope and target scene
- [ ] Step 2: Inspect existing architecture and required prefabs/data
- [ ] Step 3: Ensure Camera + Directional Light baseline
- [ ] Step 4: Place/wire gameplay objects and player slots
- [ ] Step 5: Validate local multiplayer parity and viewport behavior
- [ ] Step 6: Validate ScriptableObject references and asset placement
- [ ] Step 7: Run compile + console checks, fix issues
- [ ] Step 8: Save scene/assets and report what changed
```

## Step-by-Step Workflow

### Step 1: Confirm scope and target scene
- Identify whether this is a new scene or updates to an existing scene.
- Confirm whether flow is single-player first-person, local duel, tutorial, or hybrid.
- If requested behavior is ambiguous and affects player parity or data ownership, stop and ask.

### Step 2: Inspect existing architecture and dependencies
- Check current managers, player prefabs, tutorial coordinators, and station config assets in use.
- Reuse existing systems (`PlayerActions`, tutorial configurators, existing manager contracts) before adding new systems.
- Prefer minimal deltas to existing prefabs and scene hierarchy.

### Step 3: Ensure baseline scene essentials
- Ensure one active Camera suitable for the target topology.
- Ensure one main Directional Light exists.
- Keep scene references inspector-driven; avoid runtime lookup-based scene bootstrap for core gameplay objects.

### Step 4: Place and wire gameplay objects
- Add required prefabs from existing project prefab roots before creating new prefabs.
- Wire serialized references explicitly in Inspector-facing fields.
- For tutorial/first-person scene setup, keep player-related objects linked through known adapters/configurators.

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
- Highlight any unresolved risks, unknowns, or follow-up tasks.

## Guardrails
- Do not introduce parallel architecture when existing project systems already cover the need.
- Do not use broad scene-wide edits when focused prefab/serialized updates are sufficient.
- Do not ship scene setup changes without compile + console verification.
