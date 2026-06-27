using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// Declares this scene's <see cref="PlaytestSceneId"/> and loads the next scene from shared flow config.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlaytestSceneFlowBootstrap : MonoBehaviour
    {
        private const string LogPrefix = "[PlaytestSceneFlowBootstrap]";

        [SerializeField] private PlaytestSceneFlowConfigSO flowConfig;
        [SerializeField] private PlaytestSceneId sceneId;

        public static PlaytestSceneFlowBootstrap Instance { get; private set; }

        public PlaytestSceneFlowConfigSO FlowConfig => flowConfig;
        public PlaytestSceneId SceneId => sceneId;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static PlaytestSceneFlowBootstrap FindBootstrap()
        {
            return Instance != null ? Instance : FindFirstObjectByType<PlaytestSceneFlowBootstrap>();
        }

        public bool TryGetNextSceneName(out string sceneName)
        {
            sceneName = null;
            if (flowConfig == null)
            {
                Debug.LogWarning($"{LogPrefix} flowConfig is not assigned on '{name}'.", this);
                return false;
            }

            if (sceneId == PlaytestSceneId.None)
            {
                Debug.LogWarning($"{LogPrefix} sceneId is None on '{name}'.", this);
                return false;
            }

            return flowConfig.TryGetNextSceneName(sceneId, out sceneName);
        }

        public bool TryLoadNextScene(
            MonoBehaviour coroutineHost,
            float fadeOutDurationSeconds,
            SceneTransitionFadeOverlay[] fadeOverlays,
            bool ignoreWhenAlreadyInTargetScene,
            bool preferFadeWhenAvailable,
            out string error)
        {
            error = null;

            if (coroutineHost == null)
            {
                error = "Coroutine host is null.";
                return false;
            }

            if (!TryGetNextSceneName(out string targetSceneName))
            {
                error = $"No next scene configured for '{sceneId}'.";
                Debug.LogWarning($"{LogPrefix} {error}", this);
                return false;
            }

            Debug.Log($"{LogPrefix} Loading next scene '{targetSceneName}' from '{sceneId}'.", this);

            bool useFade = preferFadeWhenAvailable &&
                           fadeOutDurationSeconds > 0f &&
                           fadeOverlays != null &&
                           fadeOverlays.Length > 0;

            if (useFade)
            {
                return SceneTransitionUtility.TryBeginTransitionWithFade(
                    coroutineHost,
                    targetSceneName,
                    fadeOutDurationSeconds,
                    fadeOverlays,
                    ignoreWhenAlreadyInTargetScene,
                    out error);
            }

            return SceneTransitionUtility.TryLoadSceneImmediate(
                targetSceneName,
                ignoreWhenAlreadyInTargetScene,
                out error);
        }

        public bool TryLoadSceneById(
            PlaytestSceneId targetId,
            bool ignoreWhenAlreadyInTargetScene,
            out string error)
        {
            error = null;
            if (flowConfig == null)
            {
                error = "flowConfig is not assigned.";
                return false;
            }

            if (!flowConfig.TryGetSceneName(targetId, out string sceneName))
            {
                error = $"Scene name not configured for '{targetId}'.";
                return false;
            }

            return SceneTransitionUtility.TryLoadSceneImmediate(
                sceneName,
                ignoreWhenAlreadyInTargetScene,
                out error);
        }
    }
}
