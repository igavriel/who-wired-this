---
name: Project Rules And Skills
overview: Create a strict, safety-first set of project-specific Cursor rules and one scene-setup skill tailored to Unity workflows, local multiplayer patterns, and ScriptableObject configuration in this repo.
todos:
  - id: define-rule-scope
    content: Draft precise rule boundaries and avoid overlap with existing 3 rules
    status: completed
  - id: author-safety-rule
    content: Create strict safety/validation rule file for Unity edit workflows
    status: completed
  - id: author-multiplayer-so-rule
    content: Create local multiplayer + ScriptableObject architecture rule file
    status: completed
  - id: author-scene-setup-skill
    content: Create scene setup SKILL.md with step-by-step checklist
    status: completed
  - id: align-and-polish
    content: Ensure wording consistency and strictness across all new docs
    status: completed
isProject: false
---

# Project Rules and Skills v1

## Objective
Define a strict baseline for AI behavior in this Unity repo by extending existing rules and adding one practical skill for repeatable scene setup/wiring.

## What I will add
- Extend rule coverage in [.cursor/rules/unity-csharp.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/unity-csharp.mdc) with stricter validation checkpoints (compile/console checks, safe reference wiring, no hidden runtime object creation for gameplay-critical objects).
- Add a new strict workflow rule focused on safe Unity iteration in [.cursor/rules/who-wired-this-safety-validation.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/who-wired-this-safety-validation.mdc):
  - pre-change checks (scene/context inspection)
  - post-change checks (compile + console)
  - edit constraints for prefabs/scenes/data assets
  - explicit “stop and ask” triggers when uncertain
- Add a focused architecture rule in [.cursor/rules/who-wired-this-localmultiplayer-scriptableobjects.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/who-wired-this-localmultiplayer-scriptableobjects.mdc) for:
  - local multiplayer/split-view consistency expectations
  - player variant prefab handling
  - ScriptableObject-driven configuration boundaries
- Create a reusable scene setup skill in [.cursor/skills/unity-scene-setup/SKILL.md](/Users/ilang/git/unity/who-wired-this/.cursor/skills/unity-scene-setup/SKILL.md) that gives a step-by-step playbook for:
  - creating/loading scene
  - ensuring Camera + Directional Light
  - wiring player slots and required managers
  - validating compile/console and saving assets

## Existing context I will leverage
- Current C# conventions in [.cursor/rules/unity-csharp.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/unity-csharp.mdc)
- Project architecture contract in [.cursor/rules/who-wired-this-architecture.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/who-wired-this-architecture.mdc)
- Input/UI ownership guidance in [.cursor/rules/who-wired-this-input-and-ui.mdc](/Users/ilang/git/unity/who-wired-this/.cursor/rules/who-wired-this-input-and-ui.mdc)
- Tutorial and first-person patterns under [Assets/WhoWiredThis/Scripts/Tutorial](/Users/ilang/git/unity/who-wired-this/Assets/WhoWiredThis/Scripts/Tutorial) and [Assets/FirstPerson](/Users/ilang/git/unity/who-wired-this/Assets/FirstPerson)

## Acceptance criteria
- Rules are strict, specific, and non-overlapping with clear “must/should” behavior.
- New rules encode safety-first execution and local multiplayer + ScriptableObject conventions.
- Scene setup skill is actionable, ordered, and usable as a direct execution checklist.
- New files are easy to discover and named by intent.