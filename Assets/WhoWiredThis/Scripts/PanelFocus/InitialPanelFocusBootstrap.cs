using System.Collections;
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

            StartCoroutine(EnterFocusWhenReady());
        }

        private IEnumerator EnterFocusWhenReady()
        {
            // Defer until after Awake/OnEnable on players, cameras, and panel instances.
            yield return null;

            TryEnterStartupFocus(playerAFocus, playerAPanel, "Player A");
            TryEnterStartupFocus(playerBFocus, playerBPanel, "Player B");
        }

        private static void TryEnterStartupFocus(
            PlayerPanelFocusController focus,
            PanelFocusController panel,
            string label)
        {
            if (focus == null || panel == null)
            {
                Debug.LogWarning(
                    $"[InitialPanelFocusBootstrap] Skipping {label} startup focus because focus or panel reference is missing.");
                return;
            }

            if (focus.TryEnterFocus(panel))
            {
                return;
            }

            Debug.LogWarning(
                $"[InitialPanelFocusBootstrap] {label} failed to enter focus on '{panel.name}' " +
                $"(playerId={focus.PlayerId}, allowedPlayerId={panel.AllowedPlayerId}).",
                panel);
        }
    }
}
