using System;
using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Visibility
{
    [Serializable]
    public class MultiDimensionSubject
    {
        [Tooltip("Player-facing label (HUD, prompts). Falls back to the subject's GameObject name when empty.")]
        [SerializeField]
        private string displayName;

        [Tooltip("Root GameObject for this subject index (visibility and layers apply here).")]
        [SerializeField]
        private GameObject subject;

        public string Label => displayName;
        public GameObject Subject => subject;
        public string DisplayName => displayName;
    }

    /// <summary>
    /// Inspector-driven multi-subject visibility for split dimensions (Who Wired This).
    /// Single configuration mode:
    /// one subject index visible to Player A, Player B, or All Players (Any_Player semantics).
    /// Optional <see cref="generalObject"/> stays active and on Default for shared interaction (e.g. capsule collider).
    /// Layer rules mirror <see cref="DimensionVisibilityObject"/> via <see cref="MultiDimensionLayerUtility"/> (copied logic, new file only).
    /// </summary>
    public class MultiDimension : MonoBehaviour
    {
        [Header("Subjects (indexed)")]
        [Tooltip("Ordered list; index i selects which object is active for a given case.")]
        [SerializeField]
        private MultiDimensionSubject[] subjects = Array.Empty<MultiDimensionSubject>();

        [Header("General (always on for all players)")]
        [Tooltip("If set, always stays active and on Default layer—not driven by subject indices.")]
        [SerializeField]
        private GameObject generalObject;

        [Header("Selection (Player_A / Player_B / Any_Player)")]
        [Tooltip("Player_A/Player_B route to dimension-specific layers. Any_Player uses Default (visible to all players).")]
        [SerializeField]
        private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        [SerializeField]
        private int activeSubjectIndex;

        [Header("Runtime Lock")]
        [SerializeField]
        private bool interactionLocked;

        private int[] _defaultLayerPerSubject;
        private int _defaultLayerGeneral = -1;
        private bool _defaultsCaptured;

        private void Awake()
        {
            CaptureDefaultLayers();
            ApplyConfiguration();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Edit mode: refresh captured root layers when references/array size change, then apply.
            // Play mode: re-apply from inspector only — do not re-capture layers (roots may already
            // be on DimensionA/B after ApplyConfiguration, which would corrupt RestoreCapturedRootLayers).
            if (Application.isPlaying)
            {
                ApplyConfiguration();
            }
            else
            {
                CaptureDefaultLayers();
                ApplyConfiguration();
            }
        }
#endif

        /// <summary>
        /// One subject index visible to the given player (or <see cref="AllowedPlayerTag.Any_Player"/> for all players).
        /// </summary>
        public void SetSelection(AllowedPlayerTag player, int subjectIndex)
        {
            visibleToPlayer = player;
            activeSubjectIndex = subjectIndex;
            ApplyConfiguration();
        }

        /// <summary>Number of subject slots (length of the subjects array).</summary>
        public int SubjectCount => subjects == null ? 0 : subjects.Length;
        public bool IsSolved => interactionLocked;

        /// <summary>
        /// Resolved label for prompts/UI: <see cref="MultiDimensionSubject.DisplayName"/> if set, otherwise the subject <see cref="GameObject.name"/>.
        /// </summary>
        public string GetSubjectDisplayName(int index)
        {
            if (subjects == null || index < 0 || index >= subjects.Length)
            {
                return string.Empty;
            }

            MultiDimensionSubject entry = subjects[index];
            if (entry == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            return entry.Subject != null ? entry.Subject.name : string.Empty;
        }

        /// <summary>Display name for the subject at <paramref name="index"/>; equivalent to <see cref="GetSubjectDisplayName"/>.</summary>
        public string this[int index] => GetSubjectDisplayName(index);

        public void SetSolved(bool solved)
        {
            interactionLocked = solved;
        }

        /// <summary>
        /// Advances the active subject index for the given player according to configured visibility.
        /// </summary>
        public void AdvanceIndexForPlayer(AllowedPlayerTag player)
        {
            if (interactionLocked)
            {
                return;
            }

            int n = SubjectCount;
            if (n == 0)
            {
                return;
            }

            if (visibleToPlayer == AllowedPlayerTag.Player_A && player != AllowedPlayerTag.Player_A)
            {
                return;
            }

            if (visibleToPlayer == AllowedPlayerTag.Player_B && player != AllowedPlayerTag.Player_B)
            {
                return;
            }

            if (visibleToPlayer == AllowedPlayerTag.Any_Player && player == AllowedPlayerTag.Any_Player)
            {
                return;
            }

            activeSubjectIndex = (activeSubjectIndex + 1) % n;
            SetSelection(visibleToPlayer, activeSubjectIndex);
        }

        /// <summary>
        /// Returns the active index used by solution checks.
        /// </summary>
        public int GetCurrentIndexForSolutionCheck()
        {
            if (SubjectCount == 0)
            {
                return -1;
            }

            int max = Mathf.Max(SubjectCount - 1, 0);
            return Mathf.Clamp(activeSubjectIndex, 0, max);
        }

        /// <summary>Re-applies inspector configuration (dimensions, activation, general object).</summary>
        public void ApplyConfiguration()
        {
            EnsureDefaultsCaptured();
            ApplyGeneralObject();

            if (subjects == null || subjects.Length == 0)
            {
                return;
            }

            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer < 0)
            {
                defaultLayer = 0;
            }

            bool haveDimensionLayers = MultiDimensionLayerUtility.TryResolveDimensionLayers(out int dimA, out int dimB);
            if (visibleToPlayer != AllowedPlayerTag.Any_Player && !haveDimensionLayers)
            {
                if (Application.isPlaying)
                {
                    Debug.LogWarning(
                        $"[{nameof(MultiDimension)}] Layers '{MultiDimensionLayerUtility.DimensionALayerName}' / " +
                        $"'{MultiDimensionLayerUtility.DimensionBLayerName}' missing. Cannot apply selection mode on '{name}'.",
                        this);
                }

                return;
            }
            ApplySingleSelection(dimA, dimB, defaultLayer);
        }

        /// <summary>Restores each subject and general object root <see cref="GameObject.layer"/> to values captured in <see cref="Awake"/>.</summary>
        public void RestoreCapturedRootLayers()
        {
            EnsureDefaultsCaptured();
            if (subjects != null && _defaultLayerPerSubject != null)
            {
                for (int i = 0; i < subjects.Length && i < _defaultLayerPerSubject.Length; i++)
                {
                    GameObject go = GetSubjectGameObject(i);
                    if (go == null)
                    {
                        continue;
                    }

                    SetRootLayerRecursive(go.transform, _defaultLayerPerSubject[i]);
                }
            }

            if (generalObject != null && _defaultLayerGeneral >= 0)
            {
                SetRootLayerRecursive(generalObject.transform, _defaultLayerGeneral);
            }
        }

        private void CaptureDefaultLayers()
        {
            if (subjects == null)
            {
                _defaultLayerPerSubject = Array.Empty<int>();
                _defaultsCaptured = true;
                return;
            }

            _defaultLayerPerSubject = new int[subjects.Length];
            for (int i = 0; i < subjects.Length; i++)
            {
                GameObject go = GetSubjectGameObject(i);
                _defaultLayerPerSubject[i] = go != null ? go.layer : 0;
            }

            _defaultLayerGeneral = generalObject != null ? generalObject.layer : -1;
            _defaultsCaptured = true;
        }

        private void EnsureDefaultsCaptured()
        {
            if (_defaultsCaptured)
            {
                return;
            }

            CaptureDefaultLayers();
        }

        private void ApplyGeneralObject()
        {
            if (generalObject == null)
            {
                return;
            }

            generalObject.SetActive(true);
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer < 0)
            {
                defaultLayer = 0;
            }

            MultiDimensionLayerUtility.ApplyUniformLayer(generalObject.transform, defaultLayer);
        }

        private void ApplySingleSelection(int dimA, int dimB, int defaultLayer)
        {
            int max = Mathf.Max(subjects.Length - 1, 0);
            int idx = Mathf.Clamp(activeSubjectIndex, 0, max);

            for (int i = 0; i < subjects.Length; i++)
            {
                GameObject go = GetSubjectGameObject(i);
                if (go == null || IsGeneral(go))
                {
                    continue;
                }

                bool active = i == idx;
                go.SetActive(active);
                if (!active)
                {
                    continue;
                }

                switch (visibleToPlayer)
                {
                    case AllowedPlayerTag.Player_A:
                        MultiDimensionLayerUtility.ApplyPlayerAView(go.transform, dimA, dimB);
                        break;
                    case AllowedPlayerTag.Player_B:
                        MultiDimensionLayerUtility.ApplyPlayerBView(go.transform, dimA, dimB);
                        break;
                    default:
                        // Any_Player: visible to everyone on Default.
                        MultiDimensionLayerUtility.ApplyUniformLayer(go.transform, defaultLayer);
                        break;
                }
            }
        }

        private bool IsGeneral(GameObject go)
        {
            return generalObject != null && go == generalObject;
        }

        /// <summary>
        /// Returns the subject root <see cref="GameObject"/> at <paramref name="index"/> (RED/ORNG/GREN slot).
        /// Does not include <see cref="generalObject"/>.
        /// </summary>
        public bool TryGetSubjectRoot(int index, out GameObject subjectRoot)
        {
            subjectRoot = GetSubjectGameObject(index);
            return subjectRoot != null;
        }

        private GameObject GetSubjectGameObject(int index)
        {
            if (subjects == null || index < 0 || index >= subjects.Length)
            {
                return null;
            }

            MultiDimensionSubject entry = subjects[index];
            return entry != null ? entry.Subject : null;
        }

        private static void SetRootLayerRecursive(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int c = 0; c < root.childCount; c++)
            {
                SetRootLayerRecursive(root.GetChild(c), layer);
            }
        }

    }
}
