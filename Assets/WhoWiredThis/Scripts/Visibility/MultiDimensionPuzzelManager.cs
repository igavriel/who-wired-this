using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WhoWiredThis.Interfaces;

namespace WhoWiredThis.Visibility
{
    [Serializable]
    public class MultiDimensionPuzzleElement
    {
        [SerializeField] private MultiDimension element;
        [SerializeField] private int correctIndex;

        public MultiDimension Element => element;
        public int CorrectIndex => correctIndex;
    }

    /// <summary>
    /// Checks a set of MultiDimension objects against per-element target indices.
    /// Case 2 and Case 3 are evaluated; Case 1 is intentionally ignored.
    /// When solved, this component no longer responds to interaction and can disable linked interactables.
    /// </summary>
    public class MultiDimensionPuzzelManager : MonoBehaviour, IInteractable
    {
        [Header("Combination")]
        [SerializeField]
        private MultiDimensionPuzzleElement[] puzzleElements;

        [Header("Interaction")]
        [SerializeField]
        private string interactPrompt = "$INTERACT$ Check combination";

        [SerializeField]
        private string solvedPrompt = "Combination solved.";

        [Header("Solve State")]
        [SerializeField]
        private bool solved;

        [Header("Visual Feedback")]
        [SerializeField]
        private Renderer feedbackRenderer;

        [SerializeField]
        private Material failMaterial;

        [SerializeField]
        private Material solvedMaterial;

        [Header("Optional Disable On Solve")]
        [Tooltip("Any interactable scripts here will be disabled once solved.")]
        [SerializeField]
        private MonoBehaviour[] interactionsToDisable;

        [Header("Retry log (history trackers)")]
        [Tooltip("When enabled, each failed check appends a line to RetryStrings and updates LastRetryString.")]
        [SerializeField]
        private bool captureRetryStrings;

        private readonly List<string> retryStrings = new List<string>();
        private int failedCheckCount;

        /// <summary>Invoked after a failed check when <see cref="CaptureRetryStrings"/> is true: (1-based attempt index, full line).</summary>
        public event Action<int, string> OnRetryStringCaptured;

        public bool Solved => solved;

        /// <summary>Inspector option: record lines on each wrong combination check.</summary>
        public bool CaptureRetryStrings
        {
            get => captureRetryStrings;
            set => captureRetryStrings = value;
        }

        /// <summary>Number of failed checks this session (increments only when capture is on and the check fails after validation).</summary>
        public int FailedCheckCount => failedCheckCount;

        /// <summary>Lines built per failed check, in order. Empty when capture is off or there were no failures.</summary>
        public IReadOnlyList<string> RetryStrings => retryStrings;

        /// <summary>The most recent retry line, or null if none.</summary>
        public string LastRetryString => retryStrings.Count > 0 ? retryStrings[retryStrings.Count - 1] : null;

        private void Awake()
        {
            if (feedbackRenderer == null)
            {
                feedbackRenderer = GetComponentInChildren<Renderer>();
            }
        }

        public string GetPromptText()
        {
            return solved ? solvedPrompt : interactPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (solved)
            {
                return;
            }

            TryCheckSolution();
        }

        /// <summary>Clears <see cref="RetryStrings"/> and resets <see cref="FailedCheckCount"/> (e.g. new session).</summary>
        public void ResetRetryHistory()
        {
            retryStrings.Clear();
            failedCheckCount = 0;
        }

        /// <summary>
        /// Human-readable snapshot of current indices vs expected targets for all participating (non–split) dimensions.
        /// Does not increment retry count.
        /// </summary>
        public string BuildCombinationStateSummary()
        {
            if (puzzleElements == null || puzzleElements.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            bool any = false;
            for (int i = 0; i < puzzleElements.Length; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                if (entry?.Element == null)
                {
                    continue;
                }

                MultiDimension target = entry.Element;
                if (target.CurrentMode == MultiDimension.MultiDimensionMode.SplitPlayers)
                {
                    continue;
                }

                if (any)
                {
                    sb.Append("; ");
                }

                any = true;
                int cur = target.GetCurrentIndexForSolutionCheck();
                string curName = cur >= 0 ? target.GetSubjectDisplayName(cur) : "?";
                sb.Append(target.gameObject.name);
                sb.Append(": ");
                sb.Append(curName);
                sb.Append(" (");
                sb.Append(cur);
                sb.Append(") → ");
                sb.Append(entry.CorrectIndex);
            }

            if (!any)
            {
                sb.Append("(no puzzle dimensions participating)");
            }

            return sb.ToString();
        }

        /// <summary>Full retry line for a given 1-based attempt number (same format appended to <see cref="RetryStrings"/> on failure).</summary>
        public string BuildRetryLine(int oneBasedAttempt)
        {
            return $"{oneBasedAttempt}: {BuildCombinationStateSummary()}";
        }

        /// <summary>
        /// Validates current MultiDimension indices against configured entries.
        /// Returns true when solved.
        /// </summary>
        public bool TryCheckSolution()
        {
            if (solved)
            {
                return true;
            }

            if (puzzleElements == null || puzzleElements.Length == 0)
            {
                return false;
            }

            bool foundParticipatingTarget = false;
            for (int i = 0; i < puzzleElements.Length; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                if (entry == null || entry.Element == null)
                {
                    return false;
                }

                MultiDimension target = entry.Element;
                if (target.CurrentMode == MultiDimension.MultiDimensionMode.SplitPlayers)
                {
                    continue;
                }

                foundParticipatingTarget = true;
                int currentIndex = target.GetCurrentIndexForSolutionCheck();
                if (currentIndex < 0 || currentIndex != entry.CorrectIndex)
                {
                    ApplyFeedbackMaterial(failMaterial);
                    RecordFailedCheckIfEnabled();
                    return false;
                }
            }

            if (!foundParticipatingTarget)
            {
                ApplyFeedbackMaterial(failMaterial);
                RecordFailedCheckIfEnabled();
                return false;
            }

            solved = true;
            LockTargetMultiDimensions();
            ApplyFeedbackMaterial(solvedMaterial);
            DisableInteractionsAfterSolve();
            return true;
        }

        private void LockTargetMultiDimensions()
        {
            if (puzzleElements == null)
            {
                return;
            }

            for (int i = 0; i < puzzleElements.Length; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                if (entry?.Element == null)
                {
                    continue;
                }

                entry.Element.SetSolved(true);
            }
        }

        private void DisableInteractionsAfterSolve()
        {
            if (interactionsToDisable == null)
            {
                return;
            }

            for (int i = 0; i < interactionsToDisable.Length; i++)
            {
                MonoBehaviour behaviour = interactionsToDisable[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is IInteractable)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void ApplyFeedbackMaterial(Material material)
        {
            if (feedbackRenderer == null || material == null)
            {
                return;
            }

            feedbackRenderer.sharedMaterial = material;
        }

        private void RecordFailedCheckIfEnabled()
        {
            if (!captureRetryStrings)
            {
                return;
            }

            failedCheckCount++;
            string line = BuildRetryLine(failedCheckCount);
            retryStrings.Add(line);
            OnRetryStringCaptured?.Invoke(failedCheckCount, line);
        }
    }
}
