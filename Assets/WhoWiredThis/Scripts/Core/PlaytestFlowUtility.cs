using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Shared playtest flow helpers for ending a run, loading GameOverScene, and returning to the main menu.
    /// </summary>
    public static class PlaytestFlowUtility
    {
        public const string DefaultStartSceneName = "StartScene";
        public const string GameOverSceneName = "GameOverScene";

        private static bool isFlowTransitionActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedReset()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isFlowTransitionActive = false;
        }

        public static bool TryReturnToMainMenu(out string error)
        {
            return TryReturnToMainMenu(DefaultStartSceneName, out error);
        }

        public static bool TryReturnToMainMenu(string startSceneName, out string error)
        {
            error = null;

            if (isFlowTransitionActive)
            {
                return false;
            }

            isFlowTransitionActive = true;
            ExitAllPanelFocus();
            PlaytestRunSummary.Clear();
            TutorialRoleState.Reset();
            PlaytestRunTotal.ResetRun();

            if (!PlaytestSceneLoadUtility.TryLoadSingleScene(startSceneName, out error, clearSharedHistory: true))
            {
                Debug.LogError($"[PlaytestFlowUtility] Failed to load '{startSceneName}': {error}");
                isFlowTransitionActive = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Captures run summary (including shared history) before loading GameOverScene without clearing history early.
        /// </summary>
        public static bool TryEndRunAndLoadGameOver(bool abandoned, out string error)
        {
            error = null;

            if (isFlowTransitionActive)
            {
                return false;
            }

            isFlowTransitionActive = true;
            ExitAllPanelFocus();

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (ShouldCountSceneForPlaytestTotal(activeSceneName))
            {
                PlaytestRunTotal.CompleteCurrentScene(activeSceneName);
            }

            PlaytestRunSummary.Set(PlaytestRunSummaryBuilder.Build(abandoned));

            if (!PlaytestSceneLoadUtility.TryLoadSingleScene(GameOverSceneName, out error, clearSharedHistory: false))
            {
                Debug.LogError($"[PlaytestFlowUtility] Failed to load '{GameOverSceneName}': {error}");
                PlaytestRunSummary.Clear();
                isFlowTransitionActive = false;
                return false;
            }

            return true;
        }

        private static bool ShouldCountSceneForPlaytestTotal(string sceneName)
        {
            return string.Equals(sceneName, "Tutorial", System.StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Pipes", System.StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Signal", System.StringComparison.Ordinal);
        }

        private static void ExitAllPanelFocus()
        {
            PlayerPanelFocusController[] controllers =
                Object.FindObjectsByType<PlayerPanelFocusController>(FindObjectsSortMode.None);

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
