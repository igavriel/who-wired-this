---
name: Game config SO merge
overview: Merge PlaytestSceneFlowConfigSO and PlaytestTeamScoreConfigSO into one GameConfigSO asset with a single GameConfigProvider on Managers.
date: 2026-07-25
status: implemented
---

# Game config ScriptableObject merge

## Task name

Combine scene-flow and team-score config into `GameConfigSO`

## Date

2026-07-25

## Scope

- Replace **`PlaytestSceneFlowConfigSO`** and **`PlaytestTeamScoreConfigSO`** with one **`GameConfigSO`** ScriptableObject (scene flow + scoring tunables).
- Replace **`PlaytestTeamScoreConfigProvider`** with **`GameConfigProvider`** (single active config on Managers).
- Consolidate to **one asset**: `Assets/WhoWiredThis/Data/GameConfig.asset` (migrate existing serialized data).
- Update runtime consumers, prefab wiring, and editor tools that reference the old types/paths.
- Archive this plan and update [`.cursor/plan/README.md`](README.md).

## Out of scope

- Renaming **`PlaytestSceneId`** → `GameSceneId` (enum values used in scenes/prefabs; separate pass if desired).
- Renaming **`PlaytestSceneFlowBootstrap`**, **`PlaytestFlowUtility`**, or other “Playtest*” runtime class names (behavior unchanged).
- Moving **`Data/Playtest/`** folder rename beyond the merged asset (optional cleanup follow-up).
- Gameplay logic changes to scene chain, score formula, or countdown behavior (data move only).

## Why merge

Two small ScriptableObjects both describe **global game rules** (where scenes go, how long levels run, how score is computed). One asset avoids duplicate Inspector wiring on Managers (today: `flowConfig` on hotkeys/bootstrap **and** `config` on score provider).

## Target design

### `GameConfigSO` (single ScriptableObject)

| Section | Fields | Methods (from flow SO) |
|--------|--------|-------------------------|
| **Scene flow** | `SceneEntry[]`, `PlaytestSceneId[] chainOrder` | `TryGetSceneName`, `TryGetNext`, `TryGetNextSceneName`, `TryGetSceneIdForSceneName`, `SetDefaultsForCurrentChain()` |
| **Team score** | `expertSeconds`, `newPlayerSeconds`, `sceneTimeCapSeconds`, `attemptPenalty` | Read-only properties + `OnValidate` ordering |

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Who Wired This/Game Config")]
public class GameConfigSO : ScriptableObject { ... }
```

Use `[Header("Scene flow")]` and `[Header("Team score")]` in Inspector.

### `GameConfigProvider` (Managers prefab)

- One `[SerializeField] GameConfigSO config` on Managers root.
- `public static GameConfigSO Active { get; }` with same fallback pattern as today’s score provider (runtime `CreateInstance` + defaults if unassigned).
- `[DefaultExecutionOrder(-200)]` so config is ready before `TimerManager` / flow bootstrap.

### Consumer access pattern

| Consumer | Today | After |
|----------|-------|-------|
| `PlaytestTeamScoreCalculator` | `PlaytestTeamScoreConfigProvider.Active` | `GameConfigProvider.Active` |
| `TimerManager.StartLevelCountdown` | score SO `SceneTimeCapSeconds` | `GameConfigProvider.Active.SceneTimeCapSeconds` |
| `PlaytestSceneFlowBootstrap` | local `flowConfig` | **`GameConfigProvider.Active`** (optional local override kept only if needed for orphan dev scenes) |
| `SceneHotkeySwitcher` | local `flowConfig` | **`GameConfigProvider.Active`** (remove duplicate serialized ref on Managers when provider is present) |

**Preferred wiring:** Managers holds the **only** serialized `GameConfig` reference. Bootstrap prefab instances inherit flow via provider at runtime (remove redundant `flowConfig` on `PlaytestSceneFlowBootstrap.prefab` if provider always exists in play scenes).

## Asset migration strategy

**Keep the existing flow config GUID** (`PlaytestSceneFlowConfig.asset` meta) to minimize prefab/scene churn:

1. Add `GameConfigSO.cs` with merged fields + methods.
2. Retarget **`PlaytestSceneFlowConfig.asset`** script to `GameConfigSO` and **copy in** score fields from `PlaytestTeamScoreConfig.asset` (120 / 300 / 480 / 2).
3. Rename asset file to **`GameConfig.asset`** (keep `.meta` GUID `8fea1a7749751465ea1cc6fbe0d90516`).
4. Delete `PlaytestTeamScoreConfig.asset` (+ meta) and old SO script files after compile passes.

Alternative (if retarget breaks YAML): create new `GameConfig.asset` and rewire Managers + bootstrap prefab once via MCP/Inspector.

## Approved implementation steps

1. **Add `GameConfigSO.cs`** under `Assets/WhoWiredThis/Scripts/Core/GameConfig/` — merge both SO bodies; rename default helper to `SetDefaultsForCurrentChain()`.

2. **Add `GameConfigProvider.cs`** — replace `PlaytestTeamScoreConfigProvider`; static `Active` returns full `GameConfigSO`.

3. **Migrate asset** — merge score fields into flow asset; rename to `GameConfig.asset`; verify Inspector shows both sections with current values.

4. **Update runtime references**
   - `PlaytestTeamScoreCalculator.cs`
   - `TimerManager.cs`
   - `PlaytestSceneFlowBootstrap.cs` — resolve flow via `GameConfigProvider.Active` (null-guard + warning)
   - `SceneHotkeySwitcher.cs` — same

5. **Update prefabs**
   - `Managers.prefab` — swap provider component; **one** `config` → `GameConfig.asset`; remove duplicate `flowConfig` on `SceneHotkeySwitcher` if provider covers it
   - `PlaytestSceneFlowBootstrap.prefab` — drop redundant `flowConfig` when provider-based

6. **Update editor tools**
   - `PlaytestSceneFlowSetupTool.cs` — path/type → `GameConfig`
   - `PuzzleSignalRoleSwapCutsceneWireTool.cs` — same

7. **Remove obsolete files**
   - `PlaytestSceneFlowConfigSO.cs`, `PlaytestTeamScoreConfigSO.cs`, `PlaytestTeamScoreConfigProvider.cs` (+ metas)
   - `PlaytestTeamScoreConfig.asset` (+ meta)

8. **Compile + validate** — Unity MCP console clean; smoke: Start → Tutorial countdown 8:00; scene hotkeys; Game Over score unchanged with default tunables.

## Files touched (expected)

| Path | Action |
|------|--------|
| `Scripts/Core/GameConfig/GameConfigSO.cs` | Add |
| `Scripts/Core/GameConfig/GameConfigProvider.cs` | Add |
| `Data/GameConfig.asset` | Migrate/rename from flow asset |
| `Scripts/Core/PlaytestTeamScoreCalculator.cs` | Use `GameConfigProvider` |
| `Scripts/Core/TimerManager.cs` | Use `GameConfigProvider` |
| `Scripts/Environment/PlaytestSceneFlowBootstrap.cs` | Use provider |
| `Scripts/Core/SceneHotkeySwitcher.cs` | Use provider |
| `Prefabs/Game/Managers.prefab` | Provider + single config ref |
| `Prefabs/Game/PlaytestSceneFlowBootstrap.prefab` | Optional flowConfig removal |
| `Editor/PlaytestSceneFlowSetupTool.cs` | Paths/types |
| `Editor/PuzzleSignalRoleSwapCutsceneWireTool.cs` | Paths/types |
| Old Playtest* config scripts/assets | Delete |

## Testing checklist

- ✅ Unity compiles with zero errors
- ✅ `GameConfig.asset` shows scene entries + chain order + score fields (values match pre-merge)
- ⬜ Play Mode: per-level TopBar countdown still starts at configured cap (default 8:00)
- ⬜ Scene flow: Start → Tutorial → … → Game Over chain unchanged
- ⬜ Dev hotkeys (if enabled) still load scenes from config
- ⬜ Game Over team score matches pre-merge for same run stats
- ⬜ No missing-reference warnings on Managers or bootstrap prefabs

## Risks

- **Duplicate config refs** — bootstrap + hotkeys + provider could drift if not all updated; prefer single provider source.
- **Asset GUID migration** — if meta is regenerated, every `flowConfig` reference must be rewired.
- **Domain reload** — provider static reset must remain on `SubsystemRegistration`.

## Rollback notes

Revert via Git: restore two SOs, two assets, old provider, and Managers prefab wiring. Merged asset YAML is reconstructable from the two current assets in history.

## Follow-up (optional)

- Rename `PlaytestSceneId` → `GameSceneId` with `[FormerlySerializedAs]` if desired
- Rename `Data/Playtest/` → `Data/Config/`
- Editor menu: **Who Wired This → Create Game Config** with merged defaults
