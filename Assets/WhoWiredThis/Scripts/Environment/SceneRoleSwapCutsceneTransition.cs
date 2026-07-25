using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WhoWiredThis.Core;
using WhoWiredThis.Scenes;
using WhoWiredThis.UI;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// Role-swap round trip (cut-scene mode). When the Phase-1 operator (Player A) solves,
    /// waits <see cref="delaySeconds"/> so the partner reads the solution diagnostic, flags
    /// <see cref="SceneRoleState"/> for Player B operator, then fades and loads the configured cut scene.
    /// The cut scene returns to the puzzle scene, which now starts in Phase 2 (Player B operator).
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneRoleSwapCutsceneTransition : MonoBehaviour
    {
        private const string LogPrefix = "[SceneRoleSwapCutsceneTransition]";

        [Header("Completion source")]
        [Tooltip("Stage manager whose OnPhaseOneSolved triggers the round trip. Resolved if left empty.")]
        [FormerlySerializedAs("tutorialStageManager")]
        [SerializeField] private SceneStageManager sceneStageManager;

        [Header("Reveal delay")]
        [Tooltip("Seconds the partner reads the solution diagnostic before the cut scene loads.")]
        [SerializeField] private float delaySeconds = 3f;

        [Tooltip("Use unscaled time so the delay is unaffected by Time.timeScale.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Flow")]
        [SerializeField] private SceneFlowBootstrapConfig flowBootstrap;

        [Tooltip("Cut scene loaded after Player A solves.")]
        [SerializeField] private PlaytestSceneId targetCutScene = PlaytestSceneId.CutSceneTutorialSwap;

        [SerializeField] private bool ignoreWhenAlreadyInTargetScene = true;
        [SerializeField] private bool loadOnce = true;

        [Header("Fade")]
        [SerializeField] private float fadeOutDurationSeconds = 1f;
        [SerializeField] private SceneTransitionFadeOverlay[] fadeOverlays;

        private bool armed;
        private bool hasLoaded;
        private Coroutine routine;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnPhaseOneSolved += HandlePhaseOneSolved;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} sceneStageManager is not assigned on '{name}'.", this);
            }
        }

        private void OnDisable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnPhaseOneSolved -= HandlePhaseOneSolved;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private void HandlePhaseOneSolved()
        {
            if (armed || (loadOnce && hasLoaded))
            {
                return;
            }

            armed = true;
            Debug.Log($"{LogPrefix} Player A solved. Reading diagnostic for {delaySeconds:F1}s before cut scene.", this);
            routine = StartCoroutine(RunAfterDelay());
        }

        private IEnumerator RunAfterDelay()
        {
            if (delaySeconds > 0f)
            {
                if (useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(delaySeconds);
                }
                else
                {
                    yield return new WaitForSeconds(delaySeconds);
                }
            }

            routine = null;
            LoadCutScene();
        }

        private void LoadCutScene()
        {
            ResolveReferences();

            if (!TryResolveTargetSceneName(out string sceneName))
            {
                Debug.LogWarning($"{LogPrefix} Could not resolve a scene name for '{targetCutScene}'.", this);
                armed = false;
                return;
            }

            if (!SceneTransitionUtility.TryBeginTransitionWithFade(
                    this,
                    sceneName,
                    fadeOutDurationSeconds,
                    fadeOverlays,
                    ignoreWhenAlreadyInTargetScene,
                    out string error))
            {
                if (!string.IsNullOrEmpty(error) && error != "Already in target scene.")
                {
                    Debug.LogWarning($"{LogPrefix} Transition blocked: {error}", this);
                }

                armed = false;
                return;
            }

            SceneRoleState.MarkSwapToPlayerBOperator();
            Debug.Log($"{LogPrefix} Loading '{sceneName}'; next scene load = Player B operator.", this);
            hasLoaded = true;
            armed = false;
        }

        private bool TryResolveTargetSceneName(out string sceneName)
        {
            sceneName = null;
            ResolveFlowBootstrap();

            if (flowBootstrap == null || flowBootstrap.FlowConfig == null)
            {
                return false;
            }

            return flowBootstrap.FlowConfig.TryGetSceneName(targetCutScene, out sceneName);
        }

        private void ResolveReferences()
        {
            if (sceneStageManager == null)
            {
                sceneStageManager = FindFirstObjectByType<SceneStageManager>();
            }

            ResolveFlowBootstrap();
            ResolveFadeOverlays();
        }

        private void ResolveFlowBootstrap()
        {
            if (flowBootstrap == null)
            {
                flowBootstrap = SceneFlowBootstrapConfig.FindBootstrap();
            }
        }

        private void ResolveFadeOverlays()
        {
            if (fadeOverlays != null && fadeOverlays.Length > 0 && !AllOverlaysNull(fadeOverlays))
            {
                return;
            }

            PlayerHudView hudA = FindPlayerHud("A");
            PlayerHudView hudB = FindPlayerHud("B");
            var overlays = new List<SceneTransitionFadeOverlay>(2);
            TryAddOverlay(hudA, overlays);
            TryAddOverlay(hudB, overlays);
            if (overlays.Count > 0)
            {
                fadeOverlays = overlays.ToArray();
            }
        }

        private static PlayerHudView FindPlayerHud(string playerSuffix)
        {
            PlayerHudView[] hudViews = FindObjectsByType<PlayerHudView>(FindObjectsSortMode.None);
            for (int i = 0; i < hudViews.Length; i++)
            {
                PlayerHudView hud = hudViews[i];
                if (hud == null)
                {
                    continue;
                }

                string hudName = hud.name;
                if (hudName.Contains($"_{playerSuffix}") || hudName.EndsWith(playerSuffix))
                {
                    return hud;
                }
            }

            return null;
        }

        private static void TryAddOverlay(PlayerHudView hud, List<SceneTransitionFadeOverlay> overlays)
        {
            if (hud == null)
            {
                return;
            }

            SceneTransitionFadeOverlay overlay = hud.FadeOverlay;
            if (overlay != null && !overlays.Contains(overlay))
            {
                overlays.Add(overlay);
            }
        }

        private static bool AllOverlaysNull(SceneTransitionFadeOverlay[] overlays)
        {
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
