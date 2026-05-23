using System;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    [Serializable]
    public class VisualSlotDefinition
    {
        [Tooltip("Optional Inspector label (e.g. VALVE).")]
        public string label;

        [Tooltip("Same MultiDimension as the matching puzzle element. Resolves index in SubmittedIndices.")]
        public MultiDimension sourceInput;

        [Tooltip("One passive visual per state index. Length must match sourceInput.SubjectCount.")]
        public GameObject[] stateVisuals;

        public string Label => label;

    }

    /// <summary>
    /// Passive 3D readout of a submitted combination. Updates on OnAttemptSubmitted only; never reads correctness.
    /// </summary>
    public class SubmittedCombinationVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;

        [SerializeField] private Transform visualRoot;

        [Header("Slots")]
        [SerializeField] private VisualSlotDefinition[] slots;

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
            if (result == null || puzzleManager == null || visualRoot == null || slots == null)
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
                VisualSlotDefinition slot = slots[s];
                if (slot == null || slot.stateVisuals == null || slot.sourceInput == null)
                {
                    continue;
                }

                if (!TryResolveSlotIndex(slot.sourceInput, out int slotIndex))
                {
                    Debug.LogWarning(
                        $"[SubmittedCombinationVisualizer] '{name}' could not resolve slot for '{slot.label}'.",
                        this);
                    continue;
                }

                if (slotIndex < 0 || slotIndex >= submittedIndices.Length)
                {
                    continue;
                }

                int stateIndex = submittedIndices[slotIndex];
                ApplySlotVisual(slot, stateIndex);
            }
        }

        private void ApplySlotVisual(VisualSlotDefinition slot, int stateIndex)
        {
            GameObject[] visuals = slot.stateVisuals;
            int count = visuals.Length;
            if (count == 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(stateIndex, 0, count - 1);
            if (stateIndex != clamped)
            {
                Debug.LogWarning(
                    $"[SubmittedCombinationVisualizer] '{name}' slot '{slot.label}' index {stateIndex} out of range; clamped to {clamped}.",
                    this);
            }

            for (int i = 0; i < count; i++)
            {
                GameObject visual = visuals[i];
                if (visual != null)
                {
                    visual.SetActive(i == clamped);
                }
            }
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
                VisualSlotDefinition slot = slots[s];
                if (slot?.sourceInput == null || slot.stateVisuals == null)
                {
                    continue;
                }

                int expected = slot.sourceInput.SubjectCount;
                if (slot.stateVisuals.Length != expected)
                {
                    Debug.LogWarning(
                        $"[SubmittedCombinationVisualizer] '{name}' slot '{slot.label}' has {slot.stateVisuals.Length} visuals but source has {expected} states.",
                        this);
                }
            }
        }
#endif
    }
}
