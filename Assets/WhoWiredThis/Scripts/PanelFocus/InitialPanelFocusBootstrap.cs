using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using WhoWiredThis.Enums;

namespace WhoWiredThis.PanelFocus
{
    [System.Serializable]
    public sealed class PlayerStartupFocusBinding
    {
        [Tooltip("This player's focus driver on their FirstPerson player root.")]
        [SerializeField]
        private PlayerPanelFocusController focus;

        [Tooltip("Camera framing target on the board (same GameObject as PanelFocusController).")]
        [SerializeField]
        private PanelFocusCamera panelCamera;

        [Tooltip("Optional diagnostic camera framing target for partner readout at startup.")]
        [SerializeField]
        private PanelFocusCamera diagnosticCamera;

        [FormerlySerializedAs("panel")]
        [HideInInspector]
        [SerializeField]
        private PanelFocusController legacyPanel;

        [FormerlySerializedAs("diagnostic")]
        [HideInInspector]
        [SerializeField]
        private PanelFocusController legacyDiagnostic;

        public PlayerPanelFocusController Focus => focus;
        public PanelFocusCamera PanelCamera => ResolvePanelCamera();
        public PanelFocusCamera DiagnosticCamera => ResolveDiagnosticCamera();

        internal void MigrateFromLegacy(
            PlayerPanelFocusController legacyFocus,
            PanelFocusController legacyPanelRef)
        {
            if (focus == null && legacyFocus != null)
            {
                focus = legacyFocus;
            }

            if (legacyPanel == null && legacyPanelRef != null)
            {
                legacyPanel = legacyPanelRef;
            }
        }

        public PanelFocusController ResolvePanelController(bool diagnostic = false)
        {
            if (diagnostic)
            {
                return ResolveControllerFromCamera(ResolveDiagnosticCamera(), legacyDiagnostic);
            }

            return ResolveControllerFromCamera(ResolvePanelCamera(), legacyPanel);
        }

        private PanelFocusCamera ResolvePanelCamera()
        {
            if (panelCamera != null)
            {
                return panelCamera;
            }

            if (legacyPanel != null)
            {
                return legacyPanel.GetComponent<PanelFocusCamera>();
            }

            return null;
        }

        private PanelFocusCamera ResolveDiagnosticCamera()
        {
            if (diagnosticCamera != null)
            {
                return diagnosticCamera;
            }

            if (legacyDiagnostic != null)
            {
                return legacyDiagnostic.GetComponent<PanelFocusCamera>();
            }

            return null;
        }

        private static PanelFocusController ResolveControllerFromCamera(
            PanelFocusCamera camera,
            PanelFocusController legacyController)
        {
            if (camera != null)
            {
                PanelFocusController fromCamera = camera.GetComponent<PanelFocusController>();
                if (fromCamera != null)
                {
                    return fromCamera;
                }
            }

            return legacyController;
        }
    }

    /// <summary>
    /// Optionally puts each player into panel focus on play using board
    /// <see cref="PanelFocusCamera"/> framing. Toggle <see cref="enterFocusOnStartup"/> in the Inspector.
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
            yield return null;

            if (UsesOperatorDiagnosticMode())
            {
                EnterOperatorDiagnosticFocus();
                yield break;
            }

            TryEnterStartupFocus(playerA.Focus, playerA, diagnostic: false, "Player A");
            TryEnterStartupFocus(playerB.Focus, playerB, diagnostic: false, "Player B");
        }

        private bool UsesOperatorDiagnosticMode()
        {
            AllowedPlayerTag operatorPlayer = NormalizeStartupOperator();
            if (operatorPlayer == AllowedPlayerTag.Player_A)
            {
                return playerA.ResolvePanelController() != null && playerB.DiagnosticCamera != null;
            }

            return playerB.ResolvePanelController() != null && playerA.DiagnosticCamera != null;
        }

        private void EnterOperatorDiagnosticFocus()
        {
            AllowedPlayerTag operatorPlayer = NormalizeStartupOperator();

            if (operatorPlayer == AllowedPlayerTag.Player_A)
            {
                TryEnterStartupFocus(playerA.Focus, playerA, diagnostic: false, "Player A (operator panel)");
                TryEnterStartupFocus(playerB.Focus, playerB, diagnostic: true, "Player B (diagnostic)");
                return;
            }

            TryEnterStartupFocus(playerB.Focus, playerB, diagnostic: false, "Player B (operator panel)");
            TryEnterStartupFocus(playerA.Focus, playerA, diagnostic: true, "Player A (diagnostic)");
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
            PlayerStartupFocusBinding binding,
            bool diagnostic,
            string label)
        {
            if (focus == null || binding == null)
            {
                Debug.LogWarning(
                    $"[InitialPanelFocusBootstrap] Skipping {label} startup focus because focus or binding is missing.");
                return;
            }

            PanelFocusController panel = binding.ResolvePanelController(diagnostic);
            if (panel == null)
            {
                Debug.LogWarning(
                    $"[InitialPanelFocusBootstrap] Skipping {label} startup focus because no PanelFocusController could be resolved from the camera binding.");
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
