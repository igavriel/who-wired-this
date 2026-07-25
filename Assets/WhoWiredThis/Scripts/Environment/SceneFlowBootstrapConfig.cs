using UnityEngine;
using UnityEngine.Serialization;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// Declares this scene's <see cref="PlaytestSceneId"/> and loads the next scene from <see cref="GameConfigSO"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneFlowBootstrapConfig : MonoBehaviour
    {
        private const string LogPrefix = "[SceneFlowBootstrapConfig]";

        [Tooltip("Optional override. When unset, uses GameConfigProvider.Active.")]
        [FormerlySerializedAs("flowConfig")]
        [SerializeField]
        private GameConfigSO gameConfig;

        [SerializeField]
        private PlaytestSceneId sceneId;

        public static SceneFlowBootstrapConfig Instance { get; private set; }

        public GameConfigSO FlowConfig => ResolveConfig();
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

        public static SceneFlowBootstrapConfig FindBootstrap()
        {
            return Instance != null ? Instance : FindFirstObjectByType<SceneFlowBootstrapConfig>();
        }

        private GameConfigSO ResolveConfig()
        {
            if (gameConfig != null)
            {
                return gameConfig;
            }

            return GameConfigProvider.Active;
        }

        public bool TryGetNextSceneName(out string sceneName)
        {
            sceneName = null;
            GameConfigSO config = ResolveConfig();
            if (config == null)
            {
                Debug.LogWarning($"{LogPrefix} GameConfig is not available on '{name}'.", this);
                return false;
            }

            if (sceneId == PlaytestSceneId.None)
            {
                Debug.LogWarning($"{LogPrefix} sceneId is None on '{name}'.", this);
                return false;
            }

            return config.TryGetNextSceneName(sceneId, out sceneName);
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
            GameConfigSO config = ResolveConfig();
            if (config == null)
            {
                error = "GameConfig is not available.";
                return false;
            }

            if (!config.TryGetSceneName(targetId, out string sceneName))
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
