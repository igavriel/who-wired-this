using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// Escape-to-menu for playtest puzzle scenes. First Escape opens a confirmation popup on both
    /// player HUDs; second Escape confirms exit; Action (per-player interact key) cancels.
    /// </summary>
    public class PlaytestEscapeHandler : MonoBehaviour
    {
        private const string StartSceneName = "StartScene";
        private const string GameOverSceneName = "GameOverScene";

        private const string ExitConfirmationMessage =
            "Return to main menu?\n\nPress Esc again to exit\nPress Action to continue";

        private static PlaytestEscapeHandler instance;

        [SerializeField] private KeyCode menuKey = KeyCode.Escape;
        [SerializeField] private KeyCode playerAActionKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode playerBActionKey = KeyCode.RightControl;
        [SerializeField] private SharedHudPresenter sharedHudPresenter;

        private bool isExitConfirmationOpen;

        public static bool IsExitConfirmationOpen =>
            instance != null && instance.isExitConfirmationOpen;

        private void Awake()
        {
            instance = this;

            if (sharedHudPresenter == null)
            {
                sharedHudPresenter = GetComponent<SharedHudPresenter>();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (IsBookendScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            if (isExitConfirmationOpen)
            {
                HandleExitConfirmationInput();
                return;
            }

            if (!Input.GetKeyDown(menuKey) || ShouldIgnoreEscape())
            {
                return;
            }

            OpenExitConfirmation();
        }

        /// <summary>
        /// Called from <see cref="PlayerActions"/> when Action is pressed while a HUD popup is open.
        /// Returns true when the exit confirmation was cancelled (both HUD popups hidden).
        /// </summary>
        public static bool TryCancelExitConfirmationFromAction()
        {
            if (instance == null || !instance.isExitConfirmationOpen)
            {
                return false;
            }

            instance.CancelExitConfirmation();
            return true;
        }

        private void HandleExitConfirmationInput()
        {
            if (Input.GetKeyDown(menuKey))
            {
                Debug.Log("[PlaytestEscapeHandler] Exit confirmed. Returning to main menu.");
                isExitConfirmationOpen = false;
                PlaytestFlowUtility.TryReturnToMainMenu(out _);
                return;
            }

            if (Input.GetKeyDown(playerAActionKey) || Input.GetKeyDown(playerBActionKey))
            {
                CancelExitConfirmation();
            }
        }

        private void OpenExitConfirmation()
        {
            isExitConfirmationOpen = true;
            ShowConfirmationOnBothHuds();
            Debug.Log("[PlaytestEscapeHandler] Exit confirmation opened on both player HUDs.");
        }

        private void CancelExitConfirmation()
        {
            isExitConfirmationOpen = false;
            HideConfirmationOnBothHuds();
            Debug.Log("[PlaytestEscapeHandler] Exit confirmation cancelled.");
        }

        private void ShowConfirmationOnBothHuds()
        {
            PlayerHudView hudA = ResolvePlayerHudA();
            PlayerHudView hudB = ResolvePlayerHudB();

            hudA?.ShowPopup(ExitConfirmationMessage);
            hudB?.ShowPopup(ExitConfirmationMessage);
        }

        private void HideConfirmationOnBothHuds()
        {
            ResolvePlayerHudA()?.HidePopup();
            ResolvePlayerHudB()?.HidePopup();
        }

        private PlayerHudView ResolvePlayerHudA()
        {
            if (sharedHudPresenter != null)
            {
                return sharedHudPresenter.PlayerHudViewA;
            }

            return null;
        }

        private PlayerHudView ResolvePlayerHudB()
        {
            if (sharedHudPresenter != null)
            {
                return sharedHudPresenter.PlayerHudViewB;
            }

            return null;
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
