using UnityEngine;
using WhoWiredThis.Player;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// Routes interactable popup text to the interacting player's <see cref="PlayerHudView"/>,
    /// with legacy fallback to <see cref="MessagePanel.Instance"/>.
    /// </summary>
    public static class PlayerHudPopupRouter
    {
        public static void Show(GameObject interactor, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (TryShowOnPlayerHud(interactor, message))
            {
                return;
            }

            if (MessagePanel.Instance != null)
            {
                MessagePanel.Instance.Show(message);
                return;
            }

            Debug.LogWarning(
                "[PlayerHudPopupRouter] No popup target: assign PlayerHudView on PlayerActions " +
                "or use a scene with MessagePanel singleton.");
        }

        private static bool TryShowOnPlayerHud(GameObject interactor, string message)
        {
            if (interactor == null)
            {
                return false;
            }

            PlayerActions playerActions = interactor.GetComponentInParent<PlayerActions>();
            if (playerActions == null)
            {
                return false;
            }

            PlayerHudView hud = playerActions.PlayerHud;
            if (hud == null)
            {
                return false;
            }

            hud.ShowPopup(message);
            return true;
        }
    }
}
