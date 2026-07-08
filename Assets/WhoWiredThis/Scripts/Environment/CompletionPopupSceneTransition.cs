using System.Collections.Generic;
using UnityEngine;
using WhoWiredThis.Core;
using UnityEngine.Serialization;
using WhoWiredThis.Scenes;
using WhoWiredThis.UI;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// After puzzle completion, loads the next configured scene when either player dismisses the summary popup.
    /// Fades out all wired HUD overlays for <see cref="fadeOutDurationSeconds"/> before loading.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompletionPopupSceneTransition : MonoBehaviour
    {
        private const string LogPrefix = "[CompletionPopupSceneTransition]";

        [Header("Completion source")]
        [FormerlySerializedAs("tutorialStageManager")]
        [SerializeField] private SceneStageManager sceneStageManager;

        [Header("Popup panels (per-player HUD MessagePanel)")]
        [SerializeField] private MessagePanel completionPopupPanelA;
        [SerializeField] private MessagePanel completionPopupPanelB;

        [Header("Flow")]
        [SerializeField] private PlaytestSceneFlowBootstrap flowBootstrap;
        [SerializeField] private bool ignoreWhenAlreadyInTargetScene = true;
        [SerializeField] private bool loadOnce = true;

        [Header("Fade")]
        [SerializeField] private float fadeOutDurationSeconds = 1f;
        [SerializeField] private SceneTransitionFadeOverlay[] fadeOverlays;

        private bool armedForCompletionPopup;
        private bool hasLoaded;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageCompleted += HandleStageCompleted;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} sceneStageManager is not assigned on '{name}'.", this);
            }

            SubscribePopup(completionPopupPanelA, true);
            SubscribePopup(completionPopupPanelB, false);
        }

        private void OnDisable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageCompleted -= HandleStageCompleted;
            }

            UnsubscribePopup(completionPopupPanelA);
            UnsubscribePopup(completionPopupPanelB);
        }

        private void HandleStageCompleted()
        {
            ResolveReferences();
            UnsubscribePopup(completionPopupPanelA);
            UnsubscribePopup(completionPopupPanelB);
            SubscribePopup(completionPopupPanelA, true);
            SubscribePopup(completionPopupPanelB, false);
            armedForCompletionPopup = true;
            Debug.Log($"{LogPrefix} Armed — dismiss the summary popup to continue to '{GetNextSceneNameForLog()}'.", this);
        }

        private void HandlePopupHidden()
        {
            if (!armedForCompletionPopup || (loadOnce && hasLoaded))
            {
                return;
            }

            ResolveFlowBootstrap();
            Debug.Log($"{LogPrefix} Summary popup dismissed — starting fade to '{GetNextSceneNameForLog()}'.", this);

            PlaytestSceneFlowBootstrap bootstrap = flowBootstrap;
            if (bootstrap == null)
            {
                Debug.LogWarning($"{LogPrefix} PlaytestSceneFlowBootstrap not found.", this);
                return;
            }

            if (!bootstrap.TryLoadNextScene(
                    this,
                    fadeOutDurationSeconds,
                    fadeOverlays,
                    ignoreWhenAlreadyInTargetScene,
                    preferFadeWhenAvailable: true,
                    out string error))
            {
                if (!string.IsNullOrEmpty(error) && error != "Already in target scene.")
                {
                    Debug.LogWarning($"{LogPrefix} Transition blocked: {error}", this);
                }

                return;
            }

            hasLoaded = true;
            armedForCompletionPopup = false;
            UnsubscribePopup(completionPopupPanelA);
            UnsubscribePopup(completionPopupPanelB);
        }

        private void SubscribePopup(MessagePanel panel, bool warnIfMissing)
        {
            if (panel == null)
            {
                if (warnIfMissing)
                {
                    Debug.LogWarning($"{LogPrefix} completion popup panel reference is missing on '{name}'.", this);
                }

                return;
            }

            panel.PopupHidden += HandlePopupHidden;
        }

        private void UnsubscribePopup(MessagePanel panel)
        {
            if (panel != null)
            {
                panel.PopupHidden -= HandlePopupHidden;
            }
        }

        private void ResolveReferences()
        {
            if (sceneStageManager == null)
            {
                sceneStageManager = FindFirstObjectByType<SceneStageManager>();
            }

            PlayerHudView hudA = FindPlayerHud("A");
            PlayerHudView hudB = FindPlayerHud("B");

            if (completionPopupPanelA == null && hudA != null)
            {
                completionPopupPanelA = hudA.MessagePanel;
            }

            if (completionPopupPanelB == null && hudB != null)
            {
                completionPopupPanelB = hudB.MessagePanel;
            }

            if (fadeOverlays == null || fadeOverlays.Length == 0 || AllOverlaysNull(fadeOverlays))
            {
                var overlays = new List<SceneTransitionFadeOverlay>(2);
                TryAddOverlay(hudA, overlays);
                TryAddOverlay(hudB, overlays);
                fadeOverlays = overlays.ToArray();
            }

            ResolveFlowBootstrap();
        }

        private void ResolveFlowBootstrap()
        {
            if (flowBootstrap == null)
            {
                flowBootstrap = PlaytestSceneFlowBootstrap.FindBootstrap();
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

        private string GetNextSceneNameForLog()
        {
            ResolveFlowBootstrap();
            if (flowBootstrap != null && flowBootstrap.TryGetNextSceneName(out string sceneName))
            {
                return sceneName;
            }

            return "(not configured)";
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
