using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// Loads a configured scene after a wired <see cref="CinemachineSplineDolly"/> reaches a trigger position.
    /// Place on any GameObject; assign the dolly in the Inspector.
    /// Optional hold delay, then dual-HUD fade (CutScene-Intro intro handoff).
    /// </summary>
    [DisallowMultipleComponent]
    public class CinemachinePrioritySceneTransition : MonoBehaviour
    {
        private const string LogPrefix = "[CinemachinePrioritySceneTransition]";

        [Header("Cinemachine")]
        [Tooltip("Spline dolly whose Position is watched. Assign explicitly; not resolved from this GameObject.")]
        [SerializeField] private CinemachineSplineDolly cinemachineSplineDolly;

        [Header("Trigger")]
        [Tooltip("Spline position that ends the intro (1 = normalized end of path).")]
        [SerializeField] private float triggerPosition = 1f;
        [SerializeField] private float delaySeconds = 1f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Target scene")]
        [SerializeField] private string targetSceneName = "Tutorial";
        [SerializeField] private bool ignoreWhenAlreadyInTargetScene = true;
        [SerializeField] private bool loadOnce = true;

        [Header("Fade")]
        [SerializeField] private float fadeOutDurationSeconds = 1f;
        [SerializeField] private SceneTransitionFadeOverlay[] fadeOverlays;

        private float lastPosition = float.NaN;
        private bool hasTriggered;
        private Coroutine transitionRoutine;

        private void Awake()
        {
            ResolveFadeOverlays();
        }

        private void OnEnable()
        {
            ResolveFadeOverlays();
            if (cinemachineSplineDolly == null)
            {
                Debug.LogWarning($"{LogPrefix} cinemachineSplineDolly is not assigned on '{name}'.", this);
                return;
            }

            lastPosition = ReadSplinePosition();
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
        }

        private void LateUpdate()
        {
            if (cinemachineSplineDolly == null || (loadOnce && hasTriggered))
            {
                return;
            }

            float currentPosition = ReadSplinePosition();
            if (float.IsNaN(currentPosition) || currentPosition == lastPosition)
            {
                return;
            }

            if (Mathf.Approximately(currentPosition, triggerPosition) &&
                !Mathf.Approximately(lastPosition, triggerPosition))
            {
                BeginTransition();
            }

            lastPosition = currentPosition;
        }

        private void BeginTransition()
        {
            if (loadOnce && hasTriggered)
            {
                return;
            }

            hasTriggered = true;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(RunTransitionRoutine());
        }

        private IEnumerator RunTransitionRoutine()
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

            ResolveFadeOverlays();

            if (ShouldUseFade())
            {
                Debug.Log($"{LogPrefix} Fading to '{targetSceneName}'.", this);
                if (!SceneTransitionUtility.TryBeginTransitionWithFade(
                        this,
                        targetSceneName,
                        fadeOutDurationSeconds,
                        fadeOverlays,
                        ignoreWhenAlreadyInTargetScene,
                        out string fadeError))
                {
                    if (!string.IsNullOrEmpty(fadeError) && fadeError != "Already in target scene.")
                    {
                        Debug.LogWarning($"{LogPrefix} Fade transition blocked: {fadeError}", this);
                    }
                }

                yield break;
            }

            Debug.LogWarning(
                $"{LogPrefix} Fade not configured; loading '{targetSceneName}' immediately after delay.",
                this);

            if (!SceneTransitionUtility.TryLoadSceneImmediate(
                    targetSceneName,
                    ignoreWhenAlreadyInTargetScene,
                    out string loadError) &&
                !string.IsNullOrEmpty(loadError) &&
                loadError != "Already in target scene.")
            {
                Debug.LogWarning($"{LogPrefix} Load blocked: {loadError}", this);
            }
        }

        private bool ShouldUseFade()
        {
            return fadeOutDurationSeconds > 0f &&
                   fadeOverlays != null &&
                   fadeOverlays.Length > 0 &&
                   !AllOverlaysNull(fadeOverlays);
        }

        private float ReadSplinePosition()
        {
            return cinemachineSplineDolly != null
                ? cinemachineSplineDolly.SplineSettings.Position
                : float.NaN;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cinemachineSplineDolly == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Assign a CinemachineSplineDolly reference on '{name}'.",
                    this);
            }
        }
#endif

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
