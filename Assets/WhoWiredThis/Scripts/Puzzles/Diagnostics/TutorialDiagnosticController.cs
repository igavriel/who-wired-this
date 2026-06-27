using UnityEngine;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Diagnostics
{
    /// <summary>
    /// Tutorial-only diagnostic: listens to one puzzle manager, renders the decode matrix
    /// into a render-only DiagnosticDisplayController.
    /// </summary>
    public class TutorialDiagnosticController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;
        [SerializeField] private DiagnosticDisplayController display;

        [Header("Copy")]
        [SerializeField] private TutorialDiagnosticStrings strings = new TutorialDiagnosticStrings();

        private TutorialDiagnosticReport report;
        private int attemptCounter;

        private void Awake()
        {
            report = new TutorialDiagnosticReport(strings);
        }

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttempt;
            }
            else
            {
                Debug.LogWarning($"[{nameof(TutorialDiagnosticController)}] puzzleManager not assigned on '{name}'.", this);
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttempt;
            }
        }

        private void HandleAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || display == null || puzzleManager == null)
            {
                return;
            }

            int n = puzzleManager.PuzzleElementCount;
            var solution = new int[n];
            for (int i = 0; i < n; i++)
            {
                solution[i] = puzzleManager.TryGetCorrectIndex(i, out int correctIndex) ? correctIndex : -1;
            }

            int seed = unchecked((result.IsSolved ? 0 : ++attemptCounter) * 73856093) ^ HashAttempt(result.SubmittedIndices);
            string body = report.Build(solution, result.SubmittedIndices, seed);

            if (result.IsSolved)
            {
                display.SetSuccess(body);
                return;
            }

            display.SetDiagnosticBody(body);
        }

        private static int HashAttempt(int[] submittedIndices)
        {
            int hash = 17;
            if (submittedIndices == null)
            {
                return hash;
            }

            for (int i = 0; i < submittedIndices.Length; i++)
            {
                hash = (hash * 31) + submittedIndices[i];
            }

            return hash;
        }
    }
}
