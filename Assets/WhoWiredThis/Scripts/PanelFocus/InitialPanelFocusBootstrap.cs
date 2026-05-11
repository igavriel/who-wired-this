using UnityEngine;

namespace WhoWiredThis.PanelFocus
{
    /// <summary>
    /// Optionally puts each player into panel focus on play so cameras use
    /// <see cref="PanelFocusController.GetCameraSnapPose"/>. Toggle <see cref="enterFocusOnStartup"/> in the Inspector.
    /// </summary>
    public class InitialPanelFocusBootstrap : MonoBehaviour
    {
        [Tooltip("When off, players start in normal first-person. When on, both TryEnterFocus calls run in Start.")]
        [SerializeField]
        private bool enterFocusOnStartup = true;

        [SerializeField]
        private PlayerPanelFocusController playerAFocus;

        [SerializeField]
        private PanelFocusController playerAPanel;

        [SerializeField]
        private PlayerPanelFocusController playerBFocus;

        [SerializeField]
        private PanelFocusController playerBPanel;

        private void Start()
        {
            if (!enterFocusOnStartup)
            {
                return;
            }

            if (playerAFocus != null && playerAPanel != null)
            {
                playerAFocus.TryEnterFocus(playerAPanel);
            }

            if (playerBFocus != null && playerBPanel != null)
            {
                playerBFocus.TryEnterFocus(playerBPanel);
            }
        }
    }
}
