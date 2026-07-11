using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Applies dimension layers to this GameObject and its entire child hierarchy.
    /// Uses the same layer rules as <see cref="MultiDimension"/> via <see cref="MultiDimensionLayerUtility"/>
    /// (including PLACEHOLDER subtree handling).
    /// </summary>
    public class MultiDimensionRecursive : MonoBehaviour
    {
        [Header("Selection (Player_A / Player_B / Any_Player)")]
        [Tooltip("Player_A/Player_B route to dimension-specific layers. Any_Player uses Default (visible to all players).")]
        [SerializeField]
        private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        private void Awake()
        {
            ApplyConfiguration();
        }

#if UNITY_EDITOR
        private bool _onValidateApplyScheduled;

        private void OnValidate()
        {
            if (_onValidateApplyScheduled)
            {
                return;
            }

            _onValidateApplyScheduled = true;
            UnityEditor.EditorApplication.delayCall += DeferredOnValidateApply;
        }

        private void DeferredOnValidateApply()
        {
            UnityEditor.EditorApplication.delayCall -= DeferredOnValidateApply;
            _onValidateApplyScheduled = false;

            if (this == null)
            {
                return;
            }

            ApplyConfiguration();
        }
#endif

        /// <summary>Re-applies dimension layers on this object and all descendants.</summary>
        public void ApplyConfiguration()
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer < 0)
            {
                defaultLayer = 0;
            }

            if (visibleToPlayer == AllowedPlayerTag.Any_Player)
            {
                MultiDimensionLayerUtility.ApplyUniformLayer(transform, defaultLayer);
                return;
            }

            if (!MultiDimensionLayerUtility.TryResolveDimensionLayers(out int dimA, out int dimB))
            {
                if (Application.isPlaying)
                {
                    Debug.LogWarning(
                        $"[{nameof(MultiDimensionRecursive)}] Layers '{MultiDimensionLayerUtility.DimensionALayerName}' / " +
                        $"'{MultiDimensionLayerUtility.DimensionBLayerName}' missing. Cannot apply selection on '{name}'.",
                        this);
                }

                return;
            }

            switch (visibleToPlayer)
            {
                case AllowedPlayerTag.Player_A:
                    MultiDimensionLayerUtility.ApplyPlayerAView(transform, dimA, dimB);
                    break;
                case AllowedPlayerTag.Player_B:
                    MultiDimensionLayerUtility.ApplyPlayerBView(transform, dimA, dimB);
                    break;
            }
        }

        /// <summary>Sets which player can see this subtree and re-applies layers.</summary>
        public void SetVisibleToPlayer(AllowedPlayerTag player)
        {
            visibleToPlayer = player;
            ApplyConfiguration();
        }

        public AllowedPlayerTag VisibleToPlayer => visibleToPlayer;
    }
}
