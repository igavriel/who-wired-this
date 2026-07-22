using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Live, visual-only scope preview: mirrors each source input's current subject index onto its
    /// display <see cref="MultiDimension"/> whenever the player changes a control — no Submit needed.
    /// Never reads correctness and never touches the puzzle manager, attempts, history, or completion.
    /// Submitted-result readouts remain owned by <see cref="SubmittedCombinationMultiDimensionBridge"/>.
    /// </summary>
    public class LiveCombinationPreviewBridge : MonoBehaviour
    {
        [Header("Behavior")]
        [SerializeField] private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        [Header("Slots")]
        [Tooltip("Same slot shape as SubmittedCombinationMultiDimensionBridge: sourceInput control → display readout.")]
        [SerializeField] private MultiDimensionDisplaySlotDefinition[] slots;

        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
            RefreshAllFromCurrentState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || slots == null)
            {
                return;
            }

            for (int s = 0; s < slots.Length; s++)
            {
                MultiDimension source = slots[s]?.sourceInput;
                if (source != null)
                {
                    source.OnActiveIndexChanged += HandleSourceIndexChanged;
                }
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || slots == null)
            {
                subscribed = false;
                return;
            }

            for (int s = 0; s < slots.Length; s++)
            {
                MultiDimension source = slots[s]?.sourceInput;
                if (source != null)
                {
                    source.OnActiveIndexChanged -= HandleSourceIndexChanged;
                }
            }

            subscribed = false;
        }

        private void HandleSourceIndexChanged(int _)
        {
            RefreshAllFromCurrentState();
        }

        /// <summary>Applies each source input's current index to its display. Visual only.</summary>
        public void RefreshAllFromCurrentState()
        {
            if (slots == null)
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

                int currentIndex = slot.sourceInput.GetCurrentIndexForSolutionCheck();
                if (currentIndex < 0)
                {
                    continue;
                }

                ApplyDisplaySelection(slot, currentIndex);
            }
        }

        private void ApplyDisplaySelection(MultiDimensionDisplaySlotDefinition slot, int stateIndex)
        {
            MultiDimension display = slot.display;
            int subjectCount = display.SubjectCount;
            if (subjectCount <= 0)
            {
                Debug.LogWarning(
                    $"[{nameof(LiveCombinationPreviewBridge)}] '{name}' slot '{slot.label}' display '{display.name}' has no subjects.",
                    this);
                return;
            }

            int clamped = Mathf.Clamp(stateIndex, 0, subjectCount - 1);
            display.SetSelection(visibleToPlayer, clamped);
        }
    }
}
