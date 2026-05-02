using System;
using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Visibility
{
    [Serializable]
    public class MultiDimensionSubject
    {
        [Tooltip("Root GameObject for this subject index (visibility and layers apply here).")]
        [SerializeField]
        private GameObject subject;

        [Tooltip("Player-facing label (HUD, prompts). Falls back to the subject's GameObject name when empty.")]
        [SerializeField]
        private string displayName;

        public GameObject Subject => subject;
        public string DisplayName => displayName;
    }

    /// <summary>
    /// Inspector-driven multi-subject visibility for split dimensions (Who Wired This).
    /// CASE 1: different subject indices for Player A vs Player B (DimensionA / DimensionB routing).
    /// CASE 2: one subject index visible only to Player A or Player B; other subjects inactive (other player sees nothing from this list).
    /// CASE 3 (<see cref="AllowedPlayerTag.Any_Player"/> semantics): one subject index on Default for all players; other subjects inactive.
    /// Optional <see cref="generalObject"/> stays active and on Default for shared interaction (e.g. capsule collider).
    /// Layer rules mirror <see cref="DimensionVisibilityObject"/> via <see cref="MultiDimensionLayerUtility"/> (copied logic, new file only).
    /// </summary>
    public class MultiDimension : MonoBehaviour
    {
        public enum MultiDimensionMode
        {
            SplitPlayers = 0,
            ExclusiveSinglePlayer = 1,
            AllPlayers = 2
        }

        [Header("Subjects (indexed)")]
        [Tooltip("Ordered list; index i selects which object is active for a given case.")]
        [SerializeField]
        private MultiDimensionSubject[] subjects = Array.Empty<MultiDimensionSubject>();

        [Header("General (always on for all players)")]
        [Tooltip("If set, always stays active and on Default layer—not driven by subject indices.")]
        [SerializeField]
        private GameObject generalObject;

        [Header("Mode")]
        [SerializeField]
        private MultiDimensionMode configurationMode = MultiDimensionMode.SplitPlayers;

        [Header("CASE 1 — Split Player A / Player B")]
        [SerializeField]
        private int indexPlayerA;

        [SerializeField]
        private int indexPlayerB;

        [Header("CASE 2 — Exclusive one player")]
        [Tooltip("Only Player_A or Player_B is valid; Any_Player is treated as Default-layer visibility on the exclusive index (same as CASE 3 on one index).")]
        [SerializeField]
        private AllowedPlayerTag exclusivePlayer = AllowedPlayerTag.Player_A;

        [SerializeField]
        private int exclusiveSubjectIndex;

        [Header("CASE 3 — All players (Any_Player semantics)")]
        [SerializeField]
        private int sharedSubjectIndex;

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

        /// <summary>CASE 1 — different subject index per player (split dimensions).</summary>
        public void SetCase1(int playerAIndex, int playerBIndex)
        {
            configurationMode = MultiDimensionMode.SplitPlayers;
            indexPlayerA = playerAIndex;
            indexPlayerB = playerBIndex;
            ApplyConfiguration();
        }

        /// <summary>CASE 2 — one subject index visible only to the given player (or <see cref="AllowedPlayerTag.Any_Player"/> → Default layer for everyone on that subject).</summary>
        public void SetCase2(AllowedPlayerTag player, int subjectIndex)
        {
            configurationMode = MultiDimensionMode.ExclusiveSinglePlayer;
            exclusivePlayer = player;
            exclusiveSubjectIndex = subjectIndex;
            ApplyConfiguration();
        }

        /// <summary>CASE 3 — <see cref="AllowedPlayerTag.Any_Player"/> semantics: one subject on Default for all players.</summary>
        public void SetCase3(int subjectIndex)
        {
            configurationMode = MultiDimensionMode.AllPlayers;
            sharedSubjectIndex = subjectIndex;
            ApplyConfiguration();
        }

        /// <summary>Number of subject slots (length of the subjects array).</summary>
        public int SubjectCount => subjects == null ? 0 : subjects.Length;
        public MultiDimensionMode CurrentMode => configurationMode;
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
        /// Advances the active subject index for the given player according to <see cref="configurationMode"/>.
        /// Split: only <see cref="AllowedPlayerTag.Player_A"/> / <see cref="AllowedPlayerTag.Player_B"/> move their slot;
        /// Exclusive: only the configured <see cref="exclusivePlayer"/> may advance (or either player when that is <see cref="AllowedPlayerTag.Any_Player"/>);
        /// All players: shared index advances for any call.
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

            switch (configurationMode)
            {
                case MultiDimensionMode.SplitPlayers:
                    if (player == AllowedPlayerTag.Any_Player)
                    {
                        return;
                    }

                    if (player == AllowedPlayerTag.Player_A)
                    {
                        indexPlayerA = (indexPlayerA + 1) % n;
                    }
                    else
                    {
                        indexPlayerB = (indexPlayerB + 1) % n;
                    }

                    SetCase1(indexPlayerA, indexPlayerB);
                    break;

                case MultiDimensionMode.ExclusiveSinglePlayer:
                    if (exclusivePlayer == AllowedPlayerTag.Player_A && player != AllowedPlayerTag.Player_A)
                    {
                        return;
                    }

                    if (exclusivePlayer == AllowedPlayerTag.Player_B && player != AllowedPlayerTag.Player_B)
                    {
                        return;
                    }

                    if (exclusivePlayer == AllowedPlayerTag.Any_Player)
                    {
                        if (player == AllowedPlayerTag.Any_Player)
                        {
                            return;
                        }
                    }

                    exclusiveSubjectIndex = (exclusiveSubjectIndex + 1) % n;
                    SetCase2(exclusivePlayer, exclusiveSubjectIndex);
                    break;

                case MultiDimensionMode.AllPlayers:
                    sharedSubjectIndex = (sharedSubjectIndex + 1) % n;
                    SetCase3(sharedSubjectIndex);
                    break;
            }
        }

        /// <summary>
        /// Returns the active index used by solution checks.
        /// CASE 2: <see cref="exclusiveSubjectIndex"/>, CASE 3: <see cref="sharedSubjectIndex"/>,
        /// CASE 1: ignored sentinel (-1) for now.
        /// </summary>
        public int GetCurrentIndexForSolutionCheck()
        {
            if (SubjectCount == 0)
            {
                return -1;
            }

            int max = Mathf.Max(SubjectCount - 1, 0);
            switch (configurationMode)
            {
                case MultiDimensionMode.ExclusiveSinglePlayer:
                    return Mathf.Clamp(exclusiveSubjectIndex, 0, max);
                case MultiDimensionMode.AllPlayers:
                    return Mathf.Clamp(sharedSubjectIndex, 0, max);
                default:
                    return -1;
            }
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
            switch (configurationMode)
            {
                case MultiDimensionMode.SplitPlayers:
                case MultiDimensionMode.ExclusiveSinglePlayer:
                    if (!haveDimensionLayers)
                    {
                        if (Application.isPlaying)
                        {
                            Debug.LogWarning(
                                $"[{nameof(MultiDimension)}] Layers '{MultiDimensionLayerUtility.DimensionALayerName}' / " +
                                $"'{MultiDimensionLayerUtility.DimensionBLayerName}' missing. Cannot apply {configurationMode} on '{name}'.",
                                this);
                        }

                        return;
                    }

                    if (configurationMode == MultiDimensionMode.SplitPlayers)
                    {
                        ApplyCase1(dimA, dimB);
                    }
                    else
                    {
                        ApplyCase2(dimA, dimB, defaultLayer);
                    }

                    break;
                case MultiDimensionMode.AllPlayers:
                    ApplyCase3(defaultLayer);
                    break;
            }
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

        private void ApplyCase1(int dimA, int dimB)
        {
            int max = Mathf.Max(subjects.Length - 1, 0);
            int a = Mathf.Clamp(indexPlayerA, 0, max);
            int b = Mathf.Clamp(indexPlayerB, 0, max);

            for (int i = 0; i < subjects.Length; i++)
            {
                GameObject go = GetSubjectGameObject(i);
                if (go == null || IsGeneral(go))
                {
                    continue;
                }

                bool active = i == a || i == b;
                go.SetActive(active);
                if (!active)
                {
                    continue;
                }

                if (i == a && i == b)
                {
                    MultiDimensionLayerUtility.ApplyPlayerAView(go.transform, dimA, dimB);
                }
                else if (i == a)
                {
                    MultiDimensionLayerUtility.ApplyPlayerAView(go.transform, dimA, dimB);
                }
                else if (i == b)
                {
                    MultiDimensionLayerUtility.ApplyPlayerBView(go.transform, dimA, dimB);
                }
            }
        }

        private void ApplyCase2(int dimA, int dimB, int defaultLayer)
        {
            int max = Mathf.Max(subjects.Length - 1, 0);
            int idx = Mathf.Clamp(exclusiveSubjectIndex, 0, max);

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

                switch (exclusivePlayer)
                {
                    case AllowedPlayerTag.Player_A:
                        MultiDimensionLayerUtility.ApplyPlayerAView(go.transform, dimA, dimB);
                        break;
                    case AllowedPlayerTag.Player_B:
                        MultiDimensionLayerUtility.ApplyPlayerBView(go.transform, dimA, dimB);
                        break;
                    default:
                        // Any_Player: same as CASE 3 — visible to everyone on Default.
                        MultiDimensionLayerUtility.ApplyUniformLayer(go.transform, defaultLayer);
                        break;
                }
            }
        }

        private void ApplyCase3(int defaultLayer)
        {
            int max = Mathf.Max(subjects.Length - 1, 0);
            int idx = Mathf.Clamp(sharedSubjectIndex, 0, max);

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

                MultiDimensionLayerUtility.ApplyUniformLayer(go.transform, defaultLayer);
            }
        }

        private bool IsGeneral(GameObject go)
        {
            return generalObject != null && go == generalObject;
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
