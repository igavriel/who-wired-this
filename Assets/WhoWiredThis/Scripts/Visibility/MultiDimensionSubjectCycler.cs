using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Player;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Interactable that cycles <see cref="MultiDimension"/> subject indices when the interacting player
    /// matches this volume's dimension layer (DimensionA / DimensionB / Default). Default-layer colliders
    /// pass the dimension gate for both players; Case 2 still restricts who may advance via
    /// <see cref="MultiDimension.AdvanceIndexForPlayer"/>.
    /// </summary>
    public class MultiDimensionSubjectCycler : MonoBehaviour, IInteractable
    {
        [Header("Target")]
        [SerializeField]
        private MultiDimension multiDimension;

        [Header("Detection")]
        [Tooltip("If unset, uses a Collider on this GameObject. Layer must match the interactable surface after MultiDimension applies (DimensionA, DimensionB, or Default).")]
        [SerializeField]
        private Collider dimensionProbe;

        [Header("Prompt")]
        [SerializeField]
        private string promptText = "$INTERACT$ Cycle subject";

        private void Awake()
        {
            if (dimensionProbe == null)
            {
                dimensionProbe = GetComponent<Collider>();
            }
        }

        public string GetPromptText()
        {
            if (multiDimension == null)
            {
                return promptText;
            }

            int idx = multiDimension.CurrentMode == MultiDimension.MultiDimensionMode.SplitPlayers
                ? -1
                : multiDimension.GetCurrentIndexForSolutionCheck();
            if (idx < 0)
            {
                return promptText;
            }

            string label = multiDimension.GetSubjectDisplayName(idx);
            return string.IsNullOrEmpty(label) ? promptText : $"{promptText} — {label}";
        }

        public void Interact(GameObject interactor)
        {
            if (multiDimension == null || interactor == null)
            {
                return;
            }

            if (!TryResolveInteractorPlayer(interactor.transform, out AllowedPlayerTag playerTag))
            {
                return;
            }

            if (dimensionProbe == null)
            {
                Debug.LogWarning($"[{nameof(MultiDimensionSubjectCycler)}] No Collider; assign {nameof(dimensionProbe)} or add one on '{name}'.", this);
                return;
            }

            if (!PassesDimensionGate(playerTag, dimensionProbe.gameObject.layer))
            {
                return;
            }

            multiDimension.AdvanceIndexForPlayer(playerTag);
        }

        private static bool TryResolveInteractorPlayer(Transform start, out AllowedPlayerTag playerTag)
        {
            return PlayerInteractorResolver.TryResolve(start, out playerTag);
        }

        /// <summary>
        /// Default layer: both players pass. DimensionA: Player A only. DimensionB: Player B only.
        /// </summary>
        private static bool PassesDimensionGate(AllowedPlayerTag player, int colliderLayer)
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer < 0)
            {
                defaultLayer = 0;
            }

            if (colliderLayer == defaultLayer)
            {
                return player == AllowedPlayerTag.Player_A || player == AllowedPlayerTag.Player_B;
            }

            if (!MultiDimensionLayerUtility.TryResolveDimensionLayers(out int dimA, out int dimB))
            {
                return false;
            }

            if (colliderLayer == dimA)
            {
                return player == AllowedPlayerTag.Player_A;
            }

            if (colliderLayer == dimB)
            {
                return player == AllowedPlayerTag.Player_B;
            }

            return false;
        }
    }
}
