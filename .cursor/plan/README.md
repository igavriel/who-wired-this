# Cursor plan archive (`.cursor/plan/`)

Local copies of Cursor **CreatePlan** markdown for this repo (same filenames as `~/.cursor/plans/`).

Plans are grouped by **category** below. Within each table, rows are ordered roughly in the sequence they were run for this POC (earlier → later). Adjust order in Git if your history differed.

**Checklist icons in plan bodies:** ✅ done · ⬜ todo · 🚧 in progress · ⚠️ optional/manual · ❌ blocked/cancelled (see [`plan-archive`](../skills/plan-archive/SKILL.md) skill).

---

## Project & Cursor tooling

| Plan | Short description |
|------|---------------------|
| [project_rules_and_skills_9e4a8bb3.plan.md](project_rules_and_skills_9e4a8bb3.plan.md) | Cursor rules + scene-setup skill for Unity, local multiplayer, and SO safety. |
| [safe_git_workflow_rule_08ba612c.plan.md](safe_git_workflow_rule_08ba612c.plan.md) | Add Git-safe workflow section to unity-poc-workflow for risky Unity changes. |
| [editor-validation-console-output.md](editor-validation-console-output.md) | Replace validation `DisplayDialog` with structured console output for Unity MCP; optional `With Dialog` menus. |

---

## Player movement & camera

| Plan | Short description |
|------|---------------------|
| [astra_coop_third_person_08b091b5.plan.md](astra_coop_third_person_08b091b5.plan.md) | Package ThirdPersonMixamo with namespaces, bindings, sample scenes, and local duel sample. |
| [firstperson_prototype_setup_d8c7d4e1.plan.md](firstperson_prototype_setup_d8c7d4e1.plan.md) | Add internal `FirstPerson` feature package mirroring ThirdPersonMixamo patterns. |
| [fix_firstperson_movement_10c9b5af.plan.md](fix_firstperson_movement_10c9b5af.plan.md) | Forward/back vs camera; left/right yaw; validate Single/Dual via MCP. |
| [simplify_firstperson_controller_(no_camerarig)_c0cd09e6.plan.md](simplify_firstperson_controller_(no_camerarig)_c0cd09e6.plan.md) | Require explicit camera/input; remove CameraRig dependency and fallbacks. |
| [retarget_easystart_to_myplayer_d616e5fc.plan.md](retarget_easystart_to_myplayer_d616e5fc.plan.md) | Move EasyStart third-person behavior onto MyPlayer in TestScene with rollback path. |
| [dual-viewport-display-swap-toggle.md](dual-viewport-display-swap-toggle.md) | **Implemented:** `toggleSwitchKey` on `DualSingleViewportSwitcher` swaps left/right display targets (D2/D1) in dual and single layouts. |

---

## Input & interaction

| Plan | Short description |
|------|---------------------|
| [unified_input_system_plan_2db07ccc.plan.md](unified_input_system_plan_2db07ccc.plan.md) | Migrate gameplay input to Unity Input System for keyboard, mouse, and controllers. |
| [mouse_ui_+_interaction_00d85b7c.plan.md](mouse_ui_+_interaction_00d85b7c.plan.md) | Visible mouse for UI; Left Click + E for interactables with UI blocking priority. |
| [standalone_interact_detector_163b8599.plan.md](standalone_interact_detector_163b8599.plan.md) | Standalone script activating listed interactables when a detector is in range. |

---

## Visibility & MultiDimension subjects

| Plan | Short description |
|------|---------------------|
| [per-player_visibility_system_e540a24a.plan.md](per-player_visibility_system_e540a24a.plan.md) | Manager-driven split visibility: real vs replacement materials per player. |
| [dimension_visibility_clean_plan_df01cb00.plan.md](dimension_visibility_clean_plan_df01cb00.plan.md) | Layer-based dimension visibility in LocalCoOp; remove ghost/replacement artifacts. |
| [dimension_visibility_reset_f1e7ff24.plan.md](dimension_visibility_reset_f1e7ff24.plan.md) | Per-object dimension visibility; hidden = no render/collision for non-owner. |
| [switchable_subject_prefab_3dd51d63.plan.md](switchable_subject_prefab_3dd51d63.plan.md) | `MultiDimension` inspector component: subject array, modes, layers (new files only). |
| [multi-dimension-puzzle-elements-inspector.md](multi-dimension-puzzle-elements-inspector.md) | Compact `ReorderableList` for `puzzleElements` and `MultiDimension.subjects` (reusable editor pattern; Unity 6 compile validated). |
| [multidimension-5state-prefab-variant-chain.md](multidimension-5state-prefab-variant-chain.md) | MultiDimension 5-state prefab chain validated; `_5State_Test` in Test Multi Dimensions approved (2026-05-23). Production scenes remain 4-state. |
| [multidimension-switch-audio.md](multidimension-switch-audio.md) | Optional per-control switch audio on MultiDimension: random clip + subtle pitch/volume when player advances subject index. |
| [signal-submit-button-audio.md](signal-submit-button-audio.md) | **Implemented:** submit click audio on root `Signal_A_V1`, `Pipes_A V1`, `Tutorial_A V1` prefabs; variants inherit. |

---

## Puzzle core (MultiDimension managers & routing)

| Plan | Short description |
|------|---------------------|
| [multidimension_combination_lock_3663fb68.plan.md](multidimension_combination_lock_3663fb68.plan.md) | Combination-lock script over `MultiDimension` array; disable interaction after solve. |
| [multidimension_interact_cycle_638bb7c5.plan.md](multidimension_interact_cycle_638bb7c5.plan.md) | `IInteractable` cycles `MultiDimension` indices with player/dimension gating. |
| [refactor_puzzle_interaction_routing_d2ead4f6.plan.md](refactor_puzzle_interaction_routing_d2ead4f6.plan.md) | Trigger puzzle via external interactable; keep manager as state machine. |
| [bridge_to_puzzlemanager_refactor_b6c3d030.plan.md](bridge_to_puzzlemanager_refactor_b6c3d030.plan.md) | Move bridge + feedback onto `PuzzleManager`; Solve proxy forwards `Interact`. |

---

## Diagnostics, history & processing feedback

| Plan | Short description |
|------|---------------------|
| [diagnostic_display_ddddc223.plan.md](diagnostic_display_ddddc223.plan.md) | World-space diagnostic like history board; `DiagnosticDisplayController` API. |
| [diagnostic-display-multidimension-lamps.plan.md](diagnostic-display-multidimension-lamps.plan.md) | Add configurable MultiDimension lamp state values to DiagnosticDisplayController for waiting, success, and error/result output. |
| [world_history_board_e29554d2.plan.md](world_history_board_e29554d2.plan.md) | Two-stage world TMP history board + adapter from `MultiDimensionPuzzelManager` events. |
| [history_board_refactor_audit_a28db568.plan.md](history_board_refactor_audit_a28db568.plan.md) | Audit history flow; separate shared data from per-board rendering safely. |
| [processing_feedback_poc_2f1fd086.plan.md](processing_feedback_poc_2f1fd086.plan.md) | Processing lines + delay before `TryCheckSolutionFromInteractor` in bridge flow. |
| [processing_on_body_tmp_af3c4dfe.plan.md](processing_on_body_tmp_af3c4dfe.plan.md) | Drive diagnostic `Body_TMP` only; suppress adapter overwrites; remove extra TMP. |
| [activate_button_press_feedback_7b9a1256.plan.md](activate_button_press_feedback_7b9a1256.plan.md) | Optional press coroutine before processing + check in `RunActivateFlow`. |
| [diagnostic_after_solve_flow_4a02306d.plan.md](diagnostic_after_solve_flow_4a02306d.plan.md) | Waiting state until Solve; then processing, then show real diagnostic result. |
| [tutorial-diagnostic-decode-matrix.md](tutorial-diagnostic-decode-matrix.md) | **Implemented (Tutorial only):** Mastermind-style hidden feedback in a 40×12 hex decode matrix; pure-C# `TutorialDiagnosticReport` + `TutorialDiagnosticController` + editor wire tool; cross-partner displays on `Tutorial.unity`. |

---

## Panel focus & panel scenes

| Plan | Short description |
|------|---------------------|
| [panel-focus-camera-framing-config.md](panel-focus-camera-framing-config.md) | **Implemented:** `PanelFocusController` uses board-renderer transform for snap orientation; optional Framing Transform; fixes Tutorial - Visual floor camera. |
| [panel-focus-camera-extract-tutorial-pilot.md](panel-focus-camera-extract-tutorial-pilot.md) | **Validated:** `PanelFocusCamera` extracted; PFC decoupled; bootstrap `panelCamera`; all panel prefabs + 19 bootstrap scenes migrated. |
| [tutorial-visual-panel-focus.md](tutorial-visual-panel-focus.md) | Scene-local `boardRenderer` → `Plane-Transparent` on Tutorial - Visual; pairs with framing config fix. |
| [rebuild_panel_one_scene_9ea2a622.plan.md](rebuild_panel_one_scene_9ea2a622.plan.md) | New scene from Split Puzzle with full Player A panel (knobs, solve, diagnostic, history). |
| [panel_focus_mode_test_99d2c9e2.plan.md](panel_focus_mode_test_99d2c9e2.plan.md) | Panel focus test scene: two panels, camera snap, movement off until exit. |
| [simplify_panel_focus_fix_45250b08.plan.md](simplify_panel_focus_fix_45250b08.plan.md) | Collapse panel-focus stack; variable buttons + Exit; border selection frame. |
| [panel-focus-ws-arrow-selection.md](panel-focus-ws-arrow-selection.md) | **Implemented:** Panel focus dual-axis — A/D or Left/Right select; W/S or Up/Down action (cycle/submit like Ctrl). |

---

## Split Tutorial

| Plan | Short description |
|------|---------------------|
| [tutorial-visual-result-lights-cross-wire.md](tutorial-visual-result-lights-cross-wire.md) | **Implemented:** cross-opponent SETTINGS/PLACES result lights on Tutorial - Visual via `SplitMetricResultLightsController` + wire tool. |
| [result-light-sine-indicator-pulse.md](result-light-sine-indicator-pulse.md) | **Implemented:** sine pulse on active subject `IndicatorLight`; `ResultLightIndicatorPulseController` on ResultLight.prefab. |
| [split_tutorial_startup_focus_6d619ef5.plan.md](split_tutorial_startup_focus_6d619ef5.plan.md) | `InitialPanelFocusBootstrap`: optional both players in panel focus on play. |
| [initial-panel-focus-bootstrap-operator-diagnostic.md](initial-panel-focus-bootstrap-operator-diagnostic.md) | **Implemented:** bootstrap per-player Focus/Panel/Diagnostic bindings, operator dropdown, asymmetric startup focus; legacy when diagnostics unset. |
| [panel-focus-camera-only-diagnostic-startup.md](panel-focus-camera-only-diagnostic-startup.md) | **Implemented:** `TryEnterCameraFocus` for observer diagnostic startup; movement locked without `PanelFocusController` on Diagnostic-Focus. |
| [tutorial_stage_manager_4d8fbac0.plan.md](tutorial_stage_manager_4d8fbac0.plan.md) | Tutorial stages from `OnAttemptSubmitted`; locks + glass; `DefaultExecutionOrder`. |
| [split_tutorial_input_configuration_approved.plan.md](split_tutorial_input_configuration_approved.plan.md) | Approved Split Tutorial inputs: scene-only TMP + displayName sync, vocab, correctIndex, history order, diagnostic solved copy. |
| [puzzle-input-labels-5char.md](puzzle-input-labels-5char.md) | Widen state labels to max 5 chars (all tutorial scenes + Knob/Slider prefabs) + Shared History token width 5. |
| [tutorial_diagnostic_body_tmp_b9a6931a.plan.md](tutorial_diagnostic_body_tmp_b9a6931a.plan.md) | Tutorial Body_TMP copy at stage boundaries only; thin `SetInstructionBody` on diagnostic display; `TutorialStageManager` owns refs and strings. |
| [tutorial_metrics_tracker_267eef4c.plan.md](tutorial_metrics_tracker_267eef4c.plan.md) | `TutorialMetricsTracker` + `TutorialStageManager` lifecycle/stage events; `Time.realtimeSinceStartup`; snapshot API; no scoring/UI/Body_TMP. |
| [tutorial-complete-disable-list.plan.md](tutorial-complete-disable-list.plan.md) | Minimal tutorial-only option: disable configured object list when tutorial completion fires. |
| [player-scene-transition-trigger.plan.md](player-scene-transition-trigger.plan.md) | Implemented reusable trigger relay: external collider routes Player A/B entry to scene load; validated in `Test_NextLevel`. |
| [ui_canvas_dual_hud_054ae6fb.plan.md](ui_canvas_dual_hud_054ae6fb.plan.md) | Dual-display adventure HUD refactor: prototype prefab + Split Tutorial_UIRefactor (phases 0–1 shell). |
| [ui-canvas-dual-hud-phase-3-interact-prompts.md](ui-canvas-dual-hud-phase-3-interact-prompts.md) | Phase 3: per-player interact prompts via PlayerHudView + PlayerActions; prototype + UIRefactor scene only. |
| [ui-canvas-dual-hud-phase-4a-popup-foundation.md](ui-canvas-dual-hud-phase-4a-popup-foundation.md) | Phase 4A: per-player MessagePanel foundation (PerPlayer prefab variant, PlayerHudView popup API, interact dismiss, F9/F10 test harness); dual prototype only. |
| [ui-canvas-dual-hud-phase-4b-interactable-popup-routing.md](ui-canvas-dual-hud-phase-4b-interactable-popup-routing.md) | Phase 4B: route Clue/Collectible/PuzzleSocket/TestButton popups via PlayerHudPopupRouter to per-player PlayerHudView; legacy Instance fallback. |
| [tutorial-summary-popup.md](tutorial-summary-popup.md) | Tutorial completion: `TutorialSummaryPopupPresenter` shows team metrics summary on both HUDs from `GetSnapshot()`; `Tutorial.unity` only. |
| [ui-canvas-dual-hud-promote-to-production.md](ui-canvas-dual-hud-promote-to-production.md) | Promote dual HUD to UI_Canvas.prefab; pilot Split Tutorial first; user approval before remaining scenes. |
| [playtest-flow-start-gameover-total-time.md](playtest-flow-start-gameover-total-time.md) | Implemented quick playtest flow: `StartScene -> Tutorial -> Puzzle Pipes -> Puzzle Signal -> GameOverScene` with summed scene-time total and best-time persistence. |
| [cutscene-intro-cinemachine-priority-scene-transition.md](cutscene-intro-cinemachine-priority-scene-transition.md) | **Implemented:** On `CutScene-Intro`, hold + dual-HUD fade out, then load `Tutorial` when intro Cinemachine dolly position reaches 1. |
| [tutorial-exit-hide-popup-scene-transition.md](tutorial-exit-hide-popup-scene-transition.md) | **Implemented (Tutorial + Puzzle Pipes + Puzzle Signal):** Hide Exit; fade out then load next scene on completion popup dismiss. |
| [tutorial-completion-cutscene-transition.md](tutorial-completion-cutscene-transition.md) | **Implemented (Tutorial only):** Retarget completion popup dismiss to load `CutScene-Tutorial-Pipe`; defer cutscene handoff on other scenes one-by-one. |
| [puzzle-pipes-completion-cutscene-transition.md](puzzle-pipes-completion-cutscene-transition.md) | **Implemented:** Puzzle Pipes completion popup dismiss loads `CutScene-Pipe-Signal`; cutscene exits to `Puzzle Signal`; scene added to build settings. |
| [playtest-scene-flow-scriptable-object.md](playtest-scene-flow-scriptable-object.md) | **Implemented:** Scene bootstrap sends `PlaytestSceneId`; flow SO chain picks next; MCP validator + logic tests. |
| [phase-1-history-clear-escape-menu.md](phase-1-history-clear-escape-menu.md) | Clear shared history on scene transitions; cap at 20 entries per puzzle; Escape returns to StartScene via PlaytestFlowUtility. |
| [tutorial-role-swap-cutscene-round-trip.md](tutorial-role-swap-cutscene-round-trip.md) | **Implemented (Tutorial only):** A solves → B reads solution diagnostic → ~3s delay → new `CutScene-Tutorial-Swap` → return to Tutorial with roles swapped (B operator/A diagnostic); static `TutorialRoleState` phase + opt-in `roleSwapMode`; new `CutSceneTutorialSwap` enum + `overrideTargetSceneId` on `CinemachinePrioritySceneTransition`. Pending manual Play Mode test. |

---

## Scenes & lighting

| Plan | Short description |
|------|---------------------|
| [test-room-light-interior-sun-lighting.md](test-room-light-interior-sun-lighting.md) | **Implemented (pilot):** Sun + `Wall Light_Lit` point fixtures on `Test_Room_Light`; `SciFi_WarehouseSettings`; URP additional lights 8; manual Generate Lighting + Play Mode sign-off. |
| [baked-lighting-all-scenes-pilot.md](baked-lighting-all-scenes-pilot.md) | **Implemented:** All Game Room5x5 scenes + cutscenes use `WhoWiredThis_WarehouseBakedSettings` and rebaked lightmaps; `StartScene`/`GameOverScene` unchanged. |

---

## Other puzzles & tutorial experiments

| Plan | Short description |
|------|---------------------|
| [pipe-pressure-puzzle-puzzel-pipes.md](pipe-pressure-puzzle-puzzel-pipes.md) | Puzzel Pipes Pipe Pressure: Phases 1–5 validated (runtime randomized `correctIndex`); Phase 6 planned. |
| [puzzel-pipes-randomized-solution-phase5.md](puzzel-pipes-randomized-solution-phase5.md) | Phase 5: `RandomPuzzleSolutionAssigner` + `PuzzleSolutionGenerator`; Puzzel Pipes only; Phase 1 fixed baseline unchanged. |
| [puzzel-pipes-result-visual-polish.md](puzzel-pipes-result-visual-polish.md) | Industrial pipe-machine polish for Phase 4 result visualizer rigs (scene-only, shared PipeVisualizer materials). |
| [pipes-multidimension-display-bridge.md](pipes-multidimension-display-bridge.md) | **Implemented:** Replace pipes `SubmittedCombinationVisualizer` with `SubmittedCombinationMultiDimensionBridge` on active + V1 scenes and pipes panel prefab. |
| [puzzle-pipes-submit-lever-exit-popup.md](puzzle-pipes-submit-lever-exit-popup.md) | **Implemented:** Submit lever solve proxy + ON/OFF feedback, scene-only Exit opt-out, Action dismisses completion popup in focus. |
| [puzzle-pipes-role-swap-cutscene-round-trip.md](puzzle-pipes-role-swap-cutscene-round-trip.md) | **Implemented** (partial validation): Scene* rename + Pipes cut-scene role swap + entry-reset; manual Play Mode + custom copy still open. |
| [puzzle-signal-role-swap-cutscene-round-trip.md](puzzle-signal-role-swap-cutscene-round-trip.md) | **Implemented:** Puzzle Signal `CutSceneRoundTrip` via `CutScene-Signal-Swap`; turn-based operators; flow config + build settings. |
| [puzzle-pipes-v2-panel-wiring-migration.md](puzzle-pipes-v2-panel-wiring-migration.md) | **Validated:** V2 panel wiring; **dual surfaces:** legacy `DiagnosticPanel-A/B` = local rules (TSM), `DiagnosticPanel Monitor-*` = partner submit hints. |
| [puzzle-pipes-dual-panel-wiring.md](puzzle-pipes-dual-panel-wiring.md) | **Validated:** Puzzle Pipes A/B panel focus, submit, turn-lock, cross-partner diagnostics on `Player1_Pipes_Panel` instances; MCP wire + Phase 1/Submit validation pass. |
| [puzzle-signal-v2-panel-wiring-migration.md](puzzle-signal-v2-panel-wiring-migration.md) | **Implemented:** `Signal_A/B_V2 Variant` wiring migrated; dual diagnostics; OLD panels deleted; Phase 1 + submit validators pass. |
| [puzzle-signal-dual-panel-wiring.md](puzzle-signal-dual-panel-wiring.md) | **Validated:** Puzzle Signal A/B panel focus, submit, turn-lock, cross-partner diagnostics on `Player1_Signal_Panel` instances; MCP wire + Phase 1/Submit validation pass. |
| [puzzle-signal-result-feedback-split.md](puzzle-signal-result-feedback-split.md) | **Implemented:** Operator sees `ResultVisual_Root` on own panel; diagnostic partner sees `ResultLight` on their panel (cross-panel bridges like Pipes). |
| [split-result-pipes-controller.md](split-result-pipes-controller.md) | **Implemented:** `SplitResultPipesController` — three partner result lights (red/orange/green per element) on Puzzle Pipes; replaces tutorial controller on pipes prefab. |
| [floor_matrix_puzzle_b341240f.plan.md](floor_matrix_puzzle_b341240f.plan.md) | Floor-color matrix puzzle mirroring A17 engage/score; shared helper. |
| [a17_polarity_panel_c5b54005.plan.md](a17_polarity_panel_c5b54005.plan.md) | A17 polarity panel scene; `IInteractable`, material swap, points scoring. |
| [coop_calibration_tutorial_plan_89be5bca.plan.md](coop_calibration_tutorial_plan_89be5bca.plan.md) | Two-phase local co-op tutorial machine; duplicate Starter FP scene as base. |
| [firstperson_tutorial_room_plan_bbd75af7.plan.md](firstperson_tutorial_room_plan_bbd75af7.plan.md) | Minimal asymmetric co-op tutorial room reusing FP + interaction + visibility. |
| [puzzle-signal-v1.md](puzzle-signal-v1.md) | **In progress:** Phases 1–3 done on `Puzzle Signal.unity`; Phases 4–5 (5-slot visualizer, QA) open. |

---

## Refresh plans from Cursor cache

Copy new plans from `~/.cursor/plans/` into `.cursor/plan/`, assign a category section above, and add a row to that category’s table.

```bash
# Example filter (adjust as needed)
ls ~/.cursor/plans/*WhoWiredThis* ~/.cursor/plans/*who-wired-this* 2>/dev/null
```

Filter hint used previously: `WhoWiredThis|who-wired-this|Split Tutorial|MultiDimension`
