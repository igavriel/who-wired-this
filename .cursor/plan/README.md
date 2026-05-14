# Cursor plan archive (`.cursor/plan/`)

Local copies of Cursor **CreatePlan** markdown for this repo (same filenames as `~/.cursor/plans/`).

## Order you ran them (best-effort)

The table below is ordered **bottom-up for this POC**: third-person / first-person foundations → input → visibility / `MultiDimension` → puzzle bridge / diagnostics / history / processing → panel focus → Split Tutorial bootstrap → tutorial stage manager → other experiments (A17, floor matrix, co-op tutorial, etc.). **Adjust the `#` column** in Git if your real sequence differed.

**Dates:** Cursor plan YAML has **no `date` field**. The **Date** column is left as `—` unless you fill it manually. On macOS you can stamp last-modified with:

`stat -f '%Sm' -t '%Y-%m-%d' .cursor/plan/<filename>.plan.md`

| # | Plan (link) | Short description |
|---|-------------|-------------------|
| 1 | [astra_coop_third_person_08b091b5.plan.md](astra_coop_third_person_08b091b5.plan.md) | Package ThirdPersonMixamo with namespaces, bindings, sample scenes, and local duel sample. |
| 2 | [firstperson_prototype_setup_d8c7d4e1.plan.md](firstperson_prototype_setup_d8c7d4e1.plan.md) | Add internal `FirstPerson` feature package mirroring ThirdPersonMixamo patterns. |
| 3 | [fix_firstperson_movement_10c9b5af.plan.md](fix_firstperson_movement_10c9b5af.plan.md) | Forward/back vs camera; left/right yaw; validate Single/Dual via MCP. |
| 4 | [simplify_firstperson_controller_(no_camerarig)_c0cd09e6.plan.md](simplify_firstperson_controller_(no_camerarig)_c0cd09e6.plan.md) | Require explicit camera/input; remove CameraRig dependency and fallbacks. |
| 5 | [unified_input_system_plan_2db07ccc.plan.md](unified_input_system_plan_2db07ccc.plan.md) | Migrate gameplay input to Unity Input System for keyboard, mouse, and controllers. |
| 6 | [mouse_ui_+_interaction_00d85b7c.plan.md](mouse_ui_+_interaction_00d85b7c.plan.md) | Visible mouse for UI; Left Click + E for interactables with UI blocking priority. |
| 7 | [standalone_interact_detector_163b8599.plan.md](standalone_interact_detector_163b8599.plan.md) | Standalone script activating listed interactables when a detector is in range. |
| 8 | [project_rules_and_skills_9e4a8bb3.plan.md](project_rules_and_skills_9e4a8bb3.plan.md) | Cursor rules + scene-setup skill for Unity, local multiplayer, and SO safety. |
| 9 | [per-player_visibility_system_e540a24a.plan.md](per-player_visibility_system_e540a24a.plan.md) | Manager-driven split visibility: real vs replacement materials per player. |
| 10 | [dimension_visibility_clean_plan_df01cb00.plan.md](dimension_visibility_clean_plan_df01cb00.plan.md) | Layer-based dimension visibility in LocalCoOp; remove ghost/replacement artifacts. |
| 11 | [dimension_visibility_reset_f1e7ff24.plan.md](dimension_visibility_reset_f1e7ff24.plan.md) | Per-object dimension visibility; hidden = no render/collision for non-owner. |
| 12 | [switchable_subject_prefab_3dd51d63.plan.md](switchable_subject_prefab_3dd51d63.plan.md) | `MultiDimension` inspector component: subject array, modes, layers (new files only). |
| 13 | [multidimension_combination_lock_3663fb68.plan.md](multidimension_combination_lock_3663fb68.plan.md) | Combination-lock script over `MultiDimension` array; disable interaction after solve. |
| 14 | [multidimension_interact_cycle_638bb7c5.plan.md](multidimension_interact_cycle_638bb7c5.plan.md) | `IInteractable` cycles `MultiDimension` indices with player/dimension gating. |
| 15 | [refactor_puzzle_interaction_routing_d2ead4f6.plan.md](refactor_puzzle_interaction_routing_d2ead4f6.plan.md) | Trigger puzzle via external interactable; keep manager as state machine. |
| 16 | [bridge_to_puzzlemanager_refactor_b6c3d030.plan.md](bridge_to_puzzlemanager_refactor_b6c3d030.plan.md) | Move bridge + feedback onto `PuzzleManager`; Solve proxy forwards `Interact`. |
| 17 | [diagnostic_display_ddddc223.plan.md](diagnostic_display_ddddc223.plan.md) | World-space diagnostic like history board; `DiagnosticDisplayController` API. |
| 18 | [world_history_board_e29554d2.plan.md](world_history_board_e29554d2.plan.md) | Two-stage world TMP history board + adapter from `MultiDimensionPuzzelManager` events. |
| 19 | [history_board_refactor_audit_a28db568.plan.md](history_board_refactor_audit_a28db568.plan.md) | Audit history flow; separate shared data from per-board rendering safely. |
| 20 | [processing_feedback_poc_2f1fd086.plan.md](processing_feedback_poc_2f1fd086.plan.md) | Processing lines + delay before `TryCheckSolutionFromInteractor` in bridge flow. |
| 21 | [processing_on_body_tmp_af3c4dfe.plan.md](processing_on_body_tmp_af3c4dfe.plan.md) | Drive diagnostic `Body_TMP` only; suppress adapter overwrites; remove extra TMP. |
| 22 | [activate_button_press_feedback_7b9a1256.plan.md](activate_button_press_feedback_7b9a1256.plan.md) | Optional press coroutine before processing + check in `RunActivateFlow`. |
| 23 | [diagnostic_after_solve_flow_4a02306d.plan.md](diagnostic_after_solve_flow_4a02306d.plan.md) | Waiting state until Solve; then processing, then show real diagnostic result. |
| 24 | [rebuild_panel_one_scene_9ea2a622.plan.md](rebuild_panel_one_scene_9ea2a622.plan.md) | New scene from Split Puzzle with full Player A panel (knobs, solve, diagnostic, history). |
| 25 | [panel_focus_mode_test_99d2c9e2.plan.md](panel_focus_mode_test_99d2c9e2.plan.md) | Panel focus test scene: two panels, camera snap, movement off until exit. |
| 26 | [simplify_panel_focus_fix_45250b08.plan.md](simplify_panel_focus_fix_45250b08.plan.md) | Collapse panel-focus stack; variable buttons + Exit; border selection frame. |
| 27 | [split_tutorial_startup_focus_6d619ef5.plan.md](split_tutorial_startup_focus_6d619ef5.plan.md) | `InitialPanelFocusBootstrap`: optional both players in panel focus on play. |
| 28 | [tutorial_stage_manager_4d8fbac0.plan.md](tutorial_stage_manager_4d8fbac0.plan.md) | Tutorial stages from `OnAttemptSubmitted`; locks + glass; `DefaultExecutionOrder`. |
| 29 | [retarget_easystart_to_myplayer_d616e5fc.plan.md](retarget_easystart_to_myplayer_d616e5fc.plan.md) | Move EasyStart third-person behavior onto MyPlayer in TestScene with rollback path. |
| 30 | [floor_matrix_puzzle_b341240f.plan.md](floor_matrix_puzzle_b341240f.plan.md) | Floor-color matrix puzzle mirroring A17 engage/score; shared helper. |
| 31 | [a17_polarity_panel_c5b54005.plan.md](a17_polarity_panel_c5b54005.plan.md) | A17 polarity panel scene; `IInteractable`, material swap, points scoring. |
| 32 | [coop_calibration_tutorial_plan_89be5bca.plan.md](coop_calibration_tutorial_plan_89be5bca.plan.md) | Two-phase local co-op tutorial machine; duplicate Starter FP scene as base. |
| 33 | [firstperson_tutorial_room_plan_bbd75af7.plan.md](firstperson_tutorial_room_plan_bbd75af7.plan.md) | Minimal asymmetric co-op tutorial room reusing FP + interaction + visibility. |
| 34 | [split_tutorial_input_configuration_approved.plan.md](split_tutorial_input_configuration_approved.plan.md) | Approved Split Tutorial inputs: scene-only TMP + displayName sync, vocab, correctIndex, history order, diagnostic solved copy. |
| 35 | [tutorial_diagnostic_body_tmp_b9a6931a.plan.md](tutorial_diagnostic_body_tmp_b9a6931a.plan.md) | Tutorial Body_TMP copy at stage boundaries only; thin `SetInstructionBody` on diagnostic display; `TutorialStageManager` owns refs and strings. |
| 36 | [tutorial_metrics_tracker_267eef4c.plan.md](tutorial_metrics_tracker_267eef4c.plan.md) | Split Tutorial: `TutorialMetricsTracker` + `TutorialStageManager` lifecycle/stage events; `Time.realtimeSinceStartup`; snapshot API; no scoring/UI/Body_TMP. |

## Refresh plans from Cursor cache

Then edit this `README.md` table (order / dates / descriptions) as you like.
