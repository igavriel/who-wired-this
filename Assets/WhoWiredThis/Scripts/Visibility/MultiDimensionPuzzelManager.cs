using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Player;
using WhoWiredThis.Util;

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
    /// Interaction is expected to be routed via an external IInteractable bridge.
    /// </summary>
    public class MultiDimensionPuzzelManager : MonoBehaviour
    {
        [Header("Combination")]
        [SerializeField]
        private MultiDimensionPuzzleElement[] puzzleElements;

        [Tooltip("Primary solve button interactable (typically SolveInteractProxy on the Solve mesh, forwarding to MultiDimensionPuzzleInteractableBridge on PuzzleManager).")]
        [RequireInterface(typeof(IInteractable))]
        [SerializeField]
        private MonoBehaviour solveButtonInteractable;

        [Header("Solve State")]
        [SerializeField]
        private bool solved;

        [Tooltip("Optional Disable On Solve: Any interactable scripts here will be disabled once solved.")]
        [SerializeField]
        private MonoBehaviour[] interactionsToDisable;

        [Header("Retry log (history trackers)")]
        [Tooltip("When enabled, each failed check appends a line to RetryStrings and updates LastRetryString.")]
        [SerializeField]
        private bool captureRetryStrings;

        [Header("Visual Feedback")]
        [SerializeField]
        private Renderer feedbackRenderer;

        [SerializeField]
        private Material failMaterial;

        [SerializeField]
        private Material solvedMaterial;

        private readonly List<string> retryStrings = new List<string>();
        private int failedCheckCount;

        /// <summary>Invoked after a failed check when <see cref="CaptureRetryStrings"/> is true: (1-based attempt index, full line).</summary>
        public event Action<int, string> OnRetryStringCaptured;

        /// <summary>Invoked after every combination check from <see cref="Interact"/> or <see cref="TryCheckSolution"/> (success or failure).</summary>
        public event Action<MultiDimensionAttemptResult> OnAttemptSubmitted;

        public bool Solved => solved;

        /// <summary>Number of configured puzzle elements (0 when unset).</summary>
        public int PuzzleElementCount => puzzleElements != null ? puzzleElements.Length : 0;

        /// <summary>
        /// Read-only access to a puzzle slot for diagnostics (submitted index comes from attempt result).
        /// </summary>
        public bool TryGetPuzzleElement(int index, out MultiDimension element, out int correctIndex)
        {
            element = null;
            correctIndex = -1;

            if (puzzleElements == null || index < 0 || index >= puzzleElements.Length)
            {
                return false;
            }

            MultiDimensionPuzzleElement entry = puzzleElements[index];
            if (entry?.Element == null)
            {
                return false;
            }

            element = entry.Element;
            correctIndex = entry.CorrectIndex;
            return true;
        }

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

        public bool TryCheckSolutionFromInteractor(GameObject interactor)
        {
            if (solved)
            {
                Debug.Log($"[MultiDimensionPuzzelManager] '{name}' ignored solve request because puzzle is already solved.", this);
                return true;
            }

            AllowedPlayerTag actor = AllowedPlayerTag.Any_Player;
            if (interactor != null)
            {
                if (PlayerInteractorResolver.TryResolve(interactor.transform, out AllowedPlayerTag resolved))
                {
                    actor = resolved;
                }
            }

            Debug.Log(
                $"[MultiDimensionPuzzelManager] '{name}' TryCheckSolutionFromInteractor called. " +
                $"interactor={(interactor != null ? interactor.name : "null")}, actor={actor}.",
                this);
            return TryCheckSolutionWithActor(actor);
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
        /// Returns true when solved. Uses <see cref="AllowedPlayerTag.Any_Player"/> for <see cref="OnAttemptSubmitted"/> actor when not called from <see cref="TryCheckSolutionFromInteractor"/>.
        /// </summary>
        public bool TryCheckSolution()
        {
            return TryCheckSolutionWithActor(AllowedPlayerTag.Any_Player);
        }

        private bool TryCheckSolutionWithActor(AllowedPlayerTag actor)
        {
            if (solved)
            {
                Debug.Log($"[MultiDimensionPuzzelManager] '{name}' TryCheckSolutionWithActor early return: already solved.", this);
                return true;
            }

            if (puzzleElements == null || puzzleElements.Length == 0)
            {
                Debug.LogWarning($"[MultiDimensionPuzzelManager] '{name}' has no puzzleElements configured.", this);
                RaiseAttemptSubmitted(actor, Array.Empty<int>(), false);
                return false;
            }

            int n = puzzleElements.Length;
            int[] submittedIndices = new int[n];

            for (int i = 0; i < n; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                if (entry == null || entry.Element == null)
                {
                    Debug.LogWarning($"[MultiDimensionPuzzelManager] '{name}' has null puzzle element at index {i}.", this);
                    RaiseAttemptSubmitted(actor, submittedIndices, false);
                    return false;
                }

                submittedIndices[i] = entry.Element.GetCurrentIndexForSolutionCheck();
            }

            bool foundParticipatingTarget = false;
            for (int i = 0; i < n; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                MultiDimension target = entry.Element;
                foundParticipatingTarget = true;
                int currentIndex = submittedIndices[i];
                if (currentIndex < 0 || currentIndex != entry.CorrectIndex)
                {
                    Debug.Log(
                        $"[MultiDimensionPuzzelManager] '{name}' solve failed at index {i}. " +
                        $"current={currentIndex}, expected={entry.CorrectIndex}.",
                        this);
                    ApplyFeedbackMaterial(failMaterial);
                    RecordFailedCheckIfEnabled();
                    RaiseAttemptSubmitted(actor, submittedIndices, false);
                    return false;
                }
            }

            if (!foundParticipatingTarget)
            {
                Debug.LogWarning($"[MultiDimensionPuzzelManager] '{name}' has no participating targets (all split-player or invalid).", this);
                ApplyFeedbackMaterial(failMaterial);
                RecordFailedCheckIfEnabled();
                RaiseAttemptSubmitted(actor, submittedIndices, false);
                return false;
            }

            solved = true;
            Debug.Log($"[MultiDimensionPuzzelManager] '{name}' solve success.", this);
            LockTargetMultiDimensions();
            ApplyFeedbackMaterial(solvedMaterial);
            DisableInteractionsAfterSolve();
            RaiseAttemptSubmitted(actor, submittedIndices, true);
            return true;
        }

        private void RaiseAttemptSubmitted(AllowedPlayerTag actor, int[] submittedIndices, bool isSolved)
        {
            int[] copy = submittedIndices == null ? Array.Empty<int>() : (int[])submittedIndices.Clone();
            var result = new MultiDimensionAttemptResult
            {
                Actor = actor,
                ActorLabel = MapActorLabel(actor),
                SubmittedIndices = copy,
                IsSolved = isSolved,
                PublicStatus = isSolved ? "CALIBRATED" : "UNSTABLE",
                PhaseNumber = null,
                PhaseLabel = null
            };

            OnAttemptSubmitted?.Invoke(result);
        }

        private static string MapActorLabel(AllowedPlayerTag actor)
        {
            switch (actor)
            {
                case AllowedPlayerTag.Player_A:
                    return "BLUE";
                case AllowedPlayerTag.Player_B:
                    return "RED";
                default:
                    return "?";
            }
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
            DisableInteractableBehaviour(solveButtonInteractable);

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

                DisableInteractableBehaviour(behaviour);
            }
        }

        private static void DisableInteractableBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            if (behaviour is IInteractable)
            {
                behaviour.enabled = false;
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

        /// <summary>
        /// Builds private diagnostic counts from current selector state.
        /// recognizedCount = right symbols regardless of slot, alignedCount = exact slot+symbol matches.
        /// Returns false when no participating dimensions are available.
        /// </summary>
        public bool TryGetDiagnosticSnapshot(out int recognizedCount, out int alignedCount, out int totalCount)
        {
            recognizedCount = 0;
            alignedCount = 0;
            totalCount = 0;

            if (puzzleElements == null || puzzleElements.Length == 0)
            {
                return false;
            }

            var expectedCounts = new Dictionary<int, int>();
            var currentIndices = new List<int>();

            for (int i = 0; i < puzzleElements.Length; i++)
            {
                MultiDimensionPuzzleElement entry = puzzleElements[i];
                if (entry?.Element == null)
                {
                    continue;
                }

                MultiDimension target = entry.Element;
                totalCount++;
                int currentIndex = target.GetCurrentIndexForSolutionCheck();
                int expectedIndex = entry.CorrectIndex;
                currentIndices.Add(currentIndex);

                if (currentIndex >= 0 && currentIndex == expectedIndex)
                {
                    alignedCount++;
                }

                if (!expectedCounts.TryGetValue(expectedIndex, out int count))
                {
                    count = 0;
                }

                expectedCounts[expectedIndex] = count + 1;
            }

            if (totalCount == 0)
            {
                return false;
            }

            for (int i = 0; i < currentIndices.Count; i++)
            {
                int currentIndex = currentIndices[i];
                if (currentIndex < 0)
                {
                    continue;
                }

                if (!expectedCounts.TryGetValue(currentIndex, out int remaining) || remaining <= 0)
                {
                    continue;
                }

                recognizedCount++;
                expectedCounts[currentIndex] = remaining - 1;
            }

            return true;
        }
    }
}
