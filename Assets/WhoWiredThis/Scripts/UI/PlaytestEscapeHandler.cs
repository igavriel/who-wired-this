using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// Escape-to-menu for playtest puzzle scenes using UI_Canvas. Skips StartScene and GameOverScene.
    /// </summary>
    public class PlaytestEscapeHandler : MonoBehaviour
    {
        private const string StartSceneName = "StartScene";
        private const string GameOverSceneName = "GameOverScene";

        [SerializeField] private KeyCode menuKey = KeyCode.Escape;

        private void Update()
        {
            if (!Input.GetKeyDown(menuKey))
            {
                return;
            }

            if (ShouldIgnoreEscape())
            {
                return;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (IsBookendScene(activeSceneName))
            {
                return;
            }

            Debug.Log("[PlaytestEscapeHandler] Escape pressed. Returning to main menu.");
            PlaytestFlowUtility.TryReturnToMainMenu(out _);
        }

        private static bool ShouldIgnoreEscape()
        {
            MessagePanel legacyPanel = MessagePanel.Instance;
            if (legacyPanel != null && legacyPanel.IsVisible)
            {
                return true;
            }

            return false;
        }

        private static bool IsBookendScene(string sceneName)
        {
            return string.Equals(sceneName, StartSceneName, System.StringComparison.Ordinal) ||
                   string.Equals(sceneName, GameOverSceneName, System.StringComparison.Ordinal);
        }
    }
}
