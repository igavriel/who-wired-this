using System;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    [Serializable]
    public class ElementResultLightSlot
    {
        [Tooltip("Same MultiDimension as the matching puzzle element.")]
        public MultiDimension sourceElement;

        [Tooltip("Partner ResultLight MultiDimension (Upper / Middle / Lower).")]
        public MultiDimension resultLight;

        public ComponentDiagnosticType diagnosticType = ComponentDiagnosticType.Ordered;
    }

    /// <summary>
    /// Puzzle Pipes: drives three partner result lamps from per-element too-high / too-low / correct classification.
    /// Uses MultiDimension subject indices 0=red, 1=orange, 2=green.
    /// </summary>
    public class SplitResultPipesController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;

        [Header("Slots (Upper = element 1, Middle = element 2, Lower = element 3)")]
        [SerializeField] private ElementResultLightSlot[] elementLights;

        [Header("Behavior")]
        [Tooltip("When false, lamps update only on solve attempts (matches commit-only diagnostics).")]
        [SerializeField] private bool updateContinuously;
        [SerializeField] private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }

            if (updateContinuously)
            {
                RefreshFromCurrentState(force: true);
                return;
            }

            if (puzzleManager != null && puzzleManager.Solved)
            {
                ApplyAll(ComponentDiagnosticClassifier.ColorGreen);
            }
            else
            {
                ApplyAll(ComponentDiagnosticClassifier.ColorRed);
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void Update()
        {
            if (!updateContinuously)
            {
                return;
            }

            RefreshFromCurrentState(force: false);
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            ApplySubmittedIndices(result.SubmittedIndices, result.IsSolved || (puzzleManager != null && puzzleManager.Solved));
        }

        private void RefreshFromCurrentState(bool force)
        {
            if (puzzleManager == null)
            {
                return;
            }

            if (puzzleManager.Solved)
            {
                ApplyAll(ComponentDiagnosticClassifier.ColorGreen);
                return;
            }

            if (!force)
            {
                return;
            }

            ApplyAll(ComponentDiagnosticClassifier.ColorRed);
        }

        /// <summary>Applies lamp colors from submitted indices. Used at runtime and by editor validation.</summary>
        public void ApplySubmittedIndices(int[] submittedIndices, bool solved)
        {
            if (puzzleManager == null || elementLights == null)
            {
                return;
            }

            if (solved || puzzleManager.Solved)
            {
                ApplyAll(ComponentDiagnosticClassifier.ColorGreen);
                return;
            }

            if (submittedIndices == null)
            {
                ApplyAll(ComponentDiagnosticClassifier.ColorRed);
                return;
            }

            for (int s = 0; s < elementLights.Length; s++)
            {
                ElementResultLightSlot slot = elementLights[s];
                if (slot == null || slot.resultLight == null)
                {
                    continue;
                }

                if (slot.sourceElement == null ||
                    !TryResolveSlotIndex(slot.sourceElement, out int slotIndex))
                {
                    ApplyColor(slot.resultLight, ComponentDiagnosticClassifier.ColorRed);
                    continue;
                }

                if (slotIndex < 0 || slotIndex >= submittedIndices.Length)
                {
                    ApplyColor(slot.resultLight, ComponentDiagnosticClassifier.ColorRed);
                    continue;
                }

                if (!puzzleManager.TryGetPuzzleElement(slotIndex, out _, out int correctIndex))
                {
                    ApplyColor(slot.resultLight, ComponentDiagnosticClassifier.ColorRed);
                    continue;
                }

                int submitted = submittedIndices[slotIndex];
                int colorIndex = ComponentDiagnosticClassifier.ResolveColorIndex(
                    slot.diagnosticType,
                    submitted,
                    correctIndex);
                ApplyColor(slot.resultLight, colorIndex);
            }
        }

        private void ApplyAll(int colorIndex)
        {
            if (elementLights == null)
            {
                return;
            }

            for (int i = 0; i < elementLights.Length; i++)
            {
                ElementResultLightSlot slot = elementLights[i];
                if (slot?.resultLight != null)
                {
                    ApplyColor(slot.resultLight, colorIndex);
                }
            }
        }

        private void ApplyColor(MultiDimension lamp, int colorIndex)
        {
            if (lamp == null)
            {
                Debug.LogWarning($"[{nameof(SplitResultPipesController)}] Missing MultiDimension reference on '{name}'.", this);
                return;
            }

            int subjectCount = lamp.SubjectCount;
            if (subjectCount <= 0)
            {
                Debug.LogWarning($"[{nameof(SplitResultPipesController)}] '{lamp.name}' has no subjects.", this);
                return;
            }

            int clamped = Mathf.Clamp(colorIndex, 0, subjectCount - 1);
            lamp.SetSelection(visibleToPlayer, clamped);
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
    }
}
