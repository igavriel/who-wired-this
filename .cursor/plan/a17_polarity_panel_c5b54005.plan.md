---
name: A17 Polarity Panel
overview: Build the A17 Polarity Control Panel puzzle as a new standalone scene, reusing the existing IInteractable system, material-swap pattern, and core managers. ScoreManager will be extended to support points-based (100-start) scoring.
todos:
  - id: phase1
    content: "Phase 1: Scene skeleton + PolaritySwitchController (5 switches, 3 states, material swap)"
    status: completed
  - id: phase2
    content: "Phase 2: A17PuzzleManager + EngageButtonController + ScoreManager extension"
    status: completed
  - id: phase3
    content: "Phase 3: ResultLightController + LCDDisplayController (world-space TMP)"
    status: completed
  - id: phase4
    content: "Phase 4: PuzzleConfigSO + LcdMessageBankSO + hint diagram wall"
    status: completed
isProject: false
---

# A17 Polarity Control Panel – Build Plan

## Reuse Candidates

- `**IInteractable**` ([Assets/WhoWiredThis/Scripts/Interactables/IInteractable.cs](Assets/WhoWiredThis/Scripts/Interactables/IInteractable.cs)) — implement on all A17 interactables; no changes needed
- `**PlayerActions` proximity detection** ([Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs](Assets/WhoWiredThis/Scripts/Player/PlayerActions.cs)) — zero changes; OverlapSphere + E/click already handles switches and buttons
- **Material-swap pattern** from `TestButton.cs` / `PuzzleSocket.cs` — reuse for switch states (neg/off/pos) and result light
- `**Mat_ButtonIdle` / `Mat_ButtonSuccess`** ([Assets/WhoWiredThis/Materials/](Assets/WhoWiredThis/Materials/)) — reuse directly for engage button and result light states
- `**GameManager.SolvePuzzle()**` — call on success; no changes needed
- `**ScoreManager**` ([Assets/WhoWiredThis/Scripts/Core/ScoreManager.cs](Assets/WhoWiredThis/Scripts/Core/ScoreManager.cs)) — extend to support points-based scoring (replace puzzle-count model)
- `**ItemData.cs**` — use as structural pattern for `PuzzleConfigSO`
- **TMP fonts PressStart2P** — use for world-space LCD TextMeshPro

## Scene & Script Locations

- New scene: `Assets/Scenes/A17_PolarityPanel.unity`
- New scripts: `Assets/WhoWiredThis/Scripts/PuzzleA17/`
- New ScriptableObjects: `Assets/WhoWiredThis/Data/A17/`
- New materials: `Assets/WhoWiredThis/Materials/` (3 switch-state mats + 2 light mats)

## Phases

**Phase 1 – Scene skeleton + switches**

- Goal: Player can walk up to 5 switches and cycle each through Negative / Off / Positive
- Deliverable: `A17_PolarityPanel.unity` with managers/player copied from SampleScene; `PolaritySwitchController` (implements `IInteractable`, cycles state, swaps material); 5 primitive switch GameObjects wired in inspector under `A17_Switches` parent
- Test: Interact with each switch; verify 3 visual states cycle correctly, prompt text updates

**Phase 2 – Engage button + puzzle manager**

- Goal: ENGAGE button validates solution and tracks attempts/score
- Deliverable: `A17PuzzleManager` (holds solution array, attempt count, scoring rules); `EngageButtonController` (implements `IInteractable`, calls manager); `ScoreManager` extended with `SetScore(int)` / `DeductScore(int)` / points-start support; `GameManager.SolvePuzzle()` called on success
- Test: Set switches to correct solution → success; set wrong → failure + score decrements after attempt 5

**Phase 3 – Result light + LCD display**

- Goal: Visual and text feedback for all states (idle / fail / success)
- Deliverable: `ResultLightController` (material swap on a primitive sphere + optional Point Light intensity toggle); `LCDDisplayController` (world-space `TextMeshPro` component, 3 message slots wired in inspector); `A17_LCD_Display` and `A17_ResultLight` GameObjects in scene
- Test: Success → green light + success message; failure → red light + system-style failure message; idle → default

**Phase 4 – ScriptableObjects + hint diagram**

- Goal: Externalize config and messages; add static hint diagram
- Deliverable: `PuzzleConfigSO` (solution int[5], score start/penalty/min, hint trigger attempt); `LcdMessageBankSO` (idle/fail/hint/success string arrays); `A17_HintDiagram_Wall` (Quad/plane with material displaying wiring diagram); all inspector references rewired to SOs
- Test: Change solution in SO → puzzle reacts correctly; hint message appears at attempt 5

## First Phase Recommendation

Start with **Phase 1** (switches only). It proves the multi-state `IInteractable` cycle pattern — which is new ground for this project (existing interactables are binary: pick up or press once). Getting the switch feel right first de-risks the core gameplay loop before any manager logic is written.