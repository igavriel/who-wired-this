using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.UI;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Shared scene transition helpers: optional fade-out, playtest timing, and load guards.
    /// </summary>
    public static class SceneTransitionUtility
    {
        private static bool isTransitionActive;

        static SceneTransitionUtility()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static bool IsTransitionActive => isTransitionActive;

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isTransitionActive = false;
        }

        public static bool TryBeginTransitionWithFade(
            MonoBehaviour coroutineHost,
            string targetSceneName,
            float fadeOutDurationSeconds,
            SceneTransitionFadeOverlay[] fadeOverlays,
            bool ignoreWhenAlreadyInTargetScene,
            out string error)
        {
            error = null;

            if (coroutineHost == null)
            {
                error = "Coroutine host is null.";
                return false;
            }

            if (isTransitionActive)
            {
                error = "A scene transition is already in progress.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                error = "Target scene name is empty.";
                return false;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (ignoreWhenAlreadyInTargetScene &&
                string.Equals(activeSceneName, targetSceneName, StringComparison.Ordinal))
            {
                error = "Already in target scene.";
                return false;
            }

            if (!PlaytestSceneLoadUtility.CanStreamScene(targetSceneName))
            {
                error = $"Scene '{targetSceneName}' is not in Build Settings.";
                Debug.LogWarning($"[SceneTransitionUtility] {error}");
                return false;
            }

            isTransitionActive = true;
            coroutineHost.StartCoroutine(RunTransitionRoutine(
                targetSceneName,
                fadeOutDurationSeconds,
                fadeOverlays,
                activeSceneName));
            return true;
        }

        public static bool TryLoadSceneImmediate(string targetSceneName, bool ignoreWhenAlreadyInTargetScene, out string error)
        {
            error = null;

            if (isTransitionActive)
            {
                error = "A scene transition is already in progress.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                error = "Target scene name is empty.";
                return false;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (ignoreWhenAlreadyInTargetScene &&
                string.Equals(activeSceneName, targetSceneName, StringComparison.Ordinal))
            {
                return true;
            }

            if (!PlaytestSceneLoadUtility.CanStreamScene(targetSceneName))
            {
                error = $"Scene '{targetSceneName}' is not in Build Settings.";
                Debug.LogWarning($"[SceneTransitionUtility] {error}");
                return false;
            }

            isTransitionActive = true;
            LoadSceneInternal(targetSceneName, activeSceneName);
            return true;
        }

        private static IEnumerator RunTransitionRoutine(
            string targetSceneName,
            float fadeOutDurationSeconds,
            SceneTransitionFadeOverlay[] fadeOverlays,
            string activeSceneName)
        {
            ExitAllPanelFocus();
            yield return FadeOutAllOverlays(fadeOverlays, fadeOutDurationSeconds);
            LoadSceneInternal(targetSceneName, activeSceneName);
        }

        private static IEnumerator FadeOutAllOverlays(SceneTransitionFadeOverlay[] fadeOverlays, float fadeOutDurationSeconds)
        {
            if (fadeOverlays == null || fadeOverlays.Length == 0)
            {
                yield break;
            }

            int running = 0;
            for (int i = 0; i < fadeOverlays.Length; i++)
            {
                SceneTransitionFadeOverlay overlay = fadeOverlays[i];
                if (overlay == null)
                {
                    continue;
                }

                running++;
                overlay.StartCoroutine(RunOverlayFade(overlay, fadeOutDurationSeconds, () => running--));
            }

            if (running <= 0)
            {
                yield break;
            }

            while (running > 0)
            {
                yield return null;
            }
        }

        private static IEnumerator RunOverlayFade(
            SceneTransitionFadeOverlay overlay,
            float fadeOutDurationSeconds,
            Action onComplete)
        {
            yield return overlay.FadeOutRoutine(fadeOutDurationSeconds);
            onComplete?.Invoke();
        }

        private static void LoadSceneInternal(string targetSceneName, string activeSceneName)
        {
            if (ShouldCompleteGameplaySceneForTotal(activeSceneName, targetSceneName))
            {
                ScoreManager.CompleteCurrentScene(activeSceneName);
            }

            if (string.Equals(targetSceneName, PlaytestFlowUtility.GameOverSceneName, StringComparison.Ordinal))
            {
                Debug.Log("[SceneTransitionUtility] Loading GameOverScene with run summary.");
                if (!PlaytestFlowUtility.TryEndRunAndLoadGameOver(abandoned: false, out string gameOverError))
                {
                    Debug.LogWarning($"[SceneTransitionUtility] Failed to load GameOverScene: {gameOverError}");
                    isTransitionActive = false;
                }

                return;
            }

            ExitAllPanelFocus();
            PlaytestSceneLoadUtility.PrepareForSceneLoad();
            Debug.Log($"[SceneTransitionUtility] Loading scene '{targetSceneName}'.");
            if (!PlaytestSceneLoadUtility.TryLoadSingleScene(targetSceneName, out string loadError, clearSharedHistory: true))
            {
                Debug.LogWarning($"[SceneTransitionUtility] Failed to load '{targetSceneName}': {loadError}");
                isTransitionActive = false;
            }
        }

        /// <summary>
        /// Gameplay levels count toward the run total only when permanently leaving them.
        /// Role-swap cutscenes (<c>*Swap</c>) are temporary round-trips and must not finalize the level.
        /// </summary>
        private static bool ShouldCompleteGameplaySceneForTotal(string activeSceneName, string targetSceneName)
        {
            if (!ScoreManager.IsGameplayLevel(activeSceneName))
            {
                return false;
            }

            if (IsRoleSwapCutscene(targetSceneName))
            {
                Debug.Log(
                    $"[SceneTransitionUtility] Skipping CompleteCurrentScene for '{activeSceneName}' " +
                    $"(temporary role-swap leave to '{targetSceneName}').");
                return false;
            }

            return true;
        }

        private static bool IsRoleSwapCutscene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) &&
                   sceneName.IndexOf("Swap", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ExitAllPanelFocus()
        {
            PlayerPanelFocusController[] controllers =
                UnityEngine.Object.FindObjectsByType<PlayerPanelFocusController>(FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                PlayerPanelFocusController controller = controllers[i];
                if (controller != null && controller.IsFocused)
                {
                    controller.ExitFocus();
                }
            }
        }
    }
}
