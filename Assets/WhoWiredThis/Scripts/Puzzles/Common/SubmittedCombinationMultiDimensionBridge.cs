using System;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    [Serializable]
    public class MultiDimensionDisplaySlotDefinition
    {
        [Tooltip("Optional Inspector label (e.g. VALVE).")]
        public string label;

        [Tooltip("Same MultiDimension as the matching puzzle element. Resolves index in SubmittedIndices.")]
        public MultiDimension sourceInput;

        [Tooltip("Partner ResultVisual MultiDimension readout driven by submitted index.")]
        public MultiDimension display;

        public string Label => label;
    }

    /// <summary>
    /// Passive partner readout of a submitted combination via <see cref="MultiDimension.SetSelection"/>.
    /// Updates on OnAttemptSubmitted only; never reads correctness.
    /// </summary>
    public class SubmittedCombinationMultiDimensionBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;

        [Header("Behavior")]
        [SerializeField] private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        [Header("Slots")]
        [SerializeField] private MultiDimensionDisplaySlotDefinition[] slots;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null || puzzleManager == null || slots == null)
            {
                return;
            }

            ApplySubmittedIndices(result.SubmittedIndices);
        }

        /// <summary>Applies indices without reading correctIndex or solved state.</summary>
        public void ApplySubmittedIndices(int[] submittedIndices)
        {
            if (submittedIndices == null || slots == null)
            {
                return;
            }

            for (int s = 0; s < slots.Length; s++)
            {
                MultiDimensionDisplaySlotDefinition slot = slots[s];
                if (slot == null || slot.sourceInput == null || slot.display == null)
                {
                    continue;
                }

                if (!TryResolveSlotIndex(slot.sourceInput, out int slotIndex))
                {
                    Debug.LogWarning(
                        $"[{nameof(SubmittedCombinationMultiDimensionBridge)}] '{name}' could not resolve slot for '{slot.label}'.",
                        this);
                    continue;
                }

                if (slotIndex < 0 || slotIndex >= submittedIndices.Length)
                {
                    continue;
                }

                ApplyDisplaySelection(slot, submittedIndices[slotIndex]);
            }
        }

        /// <summary>Convenience wrapper for tests and validation.</summary>
        public void ApplySubmittedIndices(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            ApplySubmittedIndices(result.SubmittedIndices);
        }

        private void ApplyDisplaySelection(MultiDimensionDisplaySlotDefinition slot, int stateIndex)
        {
            MultiDimension display = slot.display;
            int subjectCount = display.SubjectCount;
            if (subjectCount <= 0)
            {
                Debug.LogWarning(
                    $"[{nameof(SubmittedCombinationMultiDimensionBridge)}] '{name}' slot '{slot.label}' display '{display.name}' has no subjects.",
                    this);
                return;
            }

            int clamped = Mathf.Clamp(stateIndex, 0, subjectCount - 1);
            if (stateIndex != clamped)
            {
                Debug.LogWarning(
                    $"[{nameof(SubmittedCombinationMultiDimensionBridge)}] '{name}' slot '{slot.label}' index {stateIndex} out of range; clamped to {clamped}.",
                    this);
            }

            display.SetSelection(visibleToPlayer, clamped);
        }

        private bool TryResolveSlotIndex(MultiDimension input, out int slotIndex)
        {
            slotIndex = -1;
            if (puzzleManager == null || input == null)
            {
                return false;
            }

            int count = puzzleManager.PuzzleElementCount;
            for (int i = 0; i < count; i++)
            {
                if (puzzleManager.TryGetPuzzleElement(i, out MultiDimension element, out _) &&
                    element == input)
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slots == null)
            {
                return;
            }

            for (int s = 0; s < slots.Length; s++)
            {
                MultiDimensionDisplaySlotDefinition slot = slots[s];
                if (slot?.sourceInput == null || slot.display == null)
                {
                    continue;
                }

                int sourceCount = slot.sourceInput.SubjectCount;
                int displayCount = slot.display.SubjectCount;
                if (sourceCount > 0 && displayCount > 0 && sourceCount != displayCount)
                {
                    Debug.LogWarning(
                        $"[{nameof(SubmittedCombinationMultiDimensionBridge)}] '{name}' slot '{slot.label}' source has {sourceCount} states but display has {displayCount}.",
                        this);
                }
            }
        }
#endif
    }
}
