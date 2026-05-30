using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.PanelFocus;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Shared playtest flow helpers for returning to the main menu and resetting cross-scene state.
    /// </summary>
    public static class PlaytestFlowUtility
    {
        private const string DefaultStartSceneName = "StartScene";

        private static bool isReturningToMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedReset()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isReturningToMenu = false;
        }

        public static bool TryReturnToMainMenu(out string error)
        {
            return TryReturnToMainMenu(DefaultStartSceneName, out error);
        }

        public static bool TryReturnToMainMenu(string startSceneName, out string error)
        {
            error = null;

            if (isReturningToMenu)
            {
                return false;
            }

            isReturningToMenu = true;
            ExitAllPanelFocus();

            PlaytestRunTotal.ResetRun();

            if (!PlaytestSceneLoadUtility.TryLoadSingleScene(startSceneName, out error))
            {
                Debug.LogError($"[PlaytestFlowUtility] Failed to load '{startSceneName}': {error}");
                isReturningToMenu = false;
                return false;
            }

            return true;
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
