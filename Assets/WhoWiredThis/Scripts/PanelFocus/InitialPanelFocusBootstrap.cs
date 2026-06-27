using System.Collections;
using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.PanelFocus
{
    [System.Serializable]
    public sealed class PlayerStartupFocusBinding
    {
        [Tooltip("This player's focus driver on their FirstPerson player root.")]
        [SerializeField]
        private PlayerPanelFocusController focus;

        [Tooltip("This player's main puzzle board PanelFocusController.")]
        [SerializeField]
        private PanelFocusController panel;

        [Tooltip("Optional diagnostic PanelFocusController for this player; leave empty until wired.")]
        [SerializeField]
        private PanelFocusController diagnostic;

        public PlayerPanelFocusController Focus => focus;
        public PanelFocusController Panel => panel;
        public PanelFocusController Diagnostic => diagnostic;

        internal void MigrateFromLegacy(
            PlayerPanelFocusController legacyFocus,
            PanelFocusController legacyPanel)
        {
            if (focus == null && legacyFocus != null)
            {
                focus = legacyFocus;
            }

            if (panel == null && legacyPanel != null)
            {
                panel = legacyPanel;
            }
        }
    }

    /// <summary>
    /// Optionally puts each player into panel focus on play so cameras use
    /// <see cref="PanelFocusController.GetCameraSnapPose"/>. Toggle <see cref="enterFocusOnStartup"/> in the Inspector.
    /// When operator diagnostics are wired, the startup operator frames their panel and the partner frames their diagnostic.
    /// </summary>
    public class InitialPanelFocusBootstrap : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Header("Startup")]
        [Tooltip("When off, players start in normal first-person. When on, startup focus runs after one frame.")]
        [SerializeField]
        private bool enterFocusOnStartup = true;

        [Tooltip("Which player operates the puzzle panel at startup. Used only when operator panel and partner diagnostic are assigned.")]
        [SerializeField]
        private AllowedPlayerTag startupOperatorPlayer = AllowedPlayerTag.Player_A;

        [Header("Player A")]
        [SerializeField]
        private PlayerStartupFocusBinding playerA = new PlayerStartupFocusBinding();

        [Header("Player B")]
        [SerializeField]
        private PlayerStartupFocusBinding playerB = new PlayerStartupFocusBinding();

        // Legacy flat references kept for existing scene YAML; copied into bindings on deserialize.
        [HideInInspector, SerializeField]
        private PlayerPanelFocusController playerAFocus;

        [HideInInspector, SerializeField]
        private PanelFocusController playerAPanel;

        [HideInInspector, SerializeField]
        private PlayerPanelFocusController playerBFocus;

        [HideInInspector, SerializeField]
        private PanelFocusController playerBPanel;

        private void Start()
        {
            if (!enterFocusOnStartup)
            {
                return;
            }

            StartCoroutine(EnterFocusWhenReady());
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (playerA == null)
            {
                playerA = new PlayerStartupFocusBinding();
            }

            if (playerB == null)
            {
                playerB = new PlayerStartupFocusBinding();
            }

            playerA.MigrateFromLegacy(playerAFocus, playerAPanel);
            playerB.MigrateFromLegacy(playerBFocus, playerBPanel);
        }

        private IEnumerator EnterFocusWhenReady()
        {
            // Defer until after Awake/OnEnable on players, cameras, and panel instances.
            yield return null;

            if (UsesOperatorDiagnosticMode())
            {
                EnterOperatorDiagnosticFocus();
                yield break;
            }

            TryEnterStartupFocus(playerA.Focus, playerA.Panel, "Player A");
            TryEnterStartupFocus(playerB.Focus, playerB.Panel, "Player B");
        }

        private bool UsesOperatorDiagnosticMode()
        {
            AllowedPlayerTag operatorPlayer = NormalizeStartupOperator();
            if (operatorPlayer == AllowedPlayerTag.Player_A)
            {
                return playerA.Panel != null && playerB.Diagnostic != null;
            }

            return playerB.Panel != null && playerA.Diagnostic != null;
        }

        private void EnterOperatorDiagnosticFocus()
        {
            AllowedPlayerTag operatorPlayer = NormalizeStartupOperator();

            if (operatorPlayer == AllowedPlayerTag.Player_A)
            {
                TryEnterStartupFocus(playerA.Focus, playerA.Panel, "Player A (operator panel)");
                TryEnterStartupFocus(playerB.Focus, playerB.Diagnostic, "Player B (diagnostic)");
                return;
            }

            TryEnterStartupFocus(playerB.Focus, playerB.Panel, "Player B (operator panel)");
            TryEnterStartupFocus(playerA.Focus, playerA.Diagnostic, "Player A (diagnostic)");
        }

        private AllowedPlayerTag NormalizeStartupOperator()
        {
            if (startupOperatorPlayer == AllowedPlayerTag.Player_A ||
                startupOperatorPlayer == AllowedPlayerTag.Player_B)
            {
                return startupOperatorPlayer;
            }

            Debug.LogWarning(
                $"[InitialPanelFocusBootstrap] startupOperatorPlayer must be Player_A or Player_B; got {startupOperatorPlayer}. Using Player_A.",
                this);
            return AllowedPlayerTag.Player_A;
        }

        private static void TryEnterStartupFocus(
            PlayerPanelFocusController focus,
            PanelFocusController panel,
            string label)
        {
            if (focus == null || panel == null)
            {
                Debug.LogWarning(
                    $"[InitialPanelFocusBootstrap] Skipping {label} startup focus because focus or target reference is missing.");
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
