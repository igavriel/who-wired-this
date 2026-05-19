using System;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Randomizes <see cref="MultiDimensionPuzzelManager"/> correctIndex values at runtime for Puzzel Pipes.
    /// Must run before <see cref="Tutorial.TutorialStageManager"/> enables player input (execution order).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RandomPuzzleSolutionAssigner : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        private MultiDimensionPuzzelManager playerAPuzzleManager;

        [SerializeField]
        private MultiDimensionPuzzelManager playerBPuzzleManager;

        [Header("Randomization")]
        [SerializeField]
        private bool enableRandomization = true;

        [SerializeField]
        private bool useSeed;

        [SerializeField]
        private int seed;

        [SerializeField]
        private bool logToConsole;

        [Header("Debug (Inspector only — not player UI)")]
        [SerializeField]
        private string debugBlueSolution = string.Empty;

        [SerializeField]
        private string debugRedSolution = string.Empty;

        private void Awake()
        {
            // Runtime only — do not rewrite scene-authored correctIndex values in Edit Mode.
            if (!Application.isPlaying)
            {
                return;
            }

            ResetManagersForNewSession();
            TryAssignSolutions();
        }

        private void ResetManagersForNewSession()
        {
            playerAPuzzleManager?.ResetSessionForNewRun();
            playerBPuzzleManager?.ResetSessionForNewRun();
        }

        public bool TryAssignSolutions()
        {
            if (!enableRandomization)
            {
                debugBlueSolution = FormatSolutionReadout(playerAPuzzleManager);
                debugRedSolution = FormatSolutionReadout(playerBPuzzleManager);
                return true;
            }

            bool blueOk = TryAssignManager(playerAPuzzleManager, useSeed ? seed : (int?)null, out int[] blueSolution);
            bool redOk = TryAssignManager(
                playerBPuzzleManager,
                useSeed ? seed + 1 : null,
                out int[] redSolution);

            debugBlueSolution = FormatIndices(blueSolution);
            debugRedSolution = FormatIndices(redSolution);

            if (logToConsole)
            {
                string seedNote = useSeed ? $"seed={seed}" : "seed=random";
                Debug.Log(
                    $"[RandomPuzzleSolutionAssigner] Blue: {debugBlueSolution} | Red: {debugRedSolution} ({seedNote})",
                    this);
            }

            return blueOk && redOk;
        }

        private static bool TryAssignManager(
            MultiDimensionPuzzelManager manager,
            int? deterministicSeed,
            out int[] appliedSolution)
        {
            appliedSolution = null;
            if (manager == null)
            {
                return false;
            }

            int count = manager.PuzzleElementCount;
            if (count <= 0)
            {
                return false;
            }

            var maxIndices = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!manager.TryGetElementStateCount(i, out int stateCount) || stateCount <= 0)
                {
                    return false;
                }

                maxIndices[i] = stateCount - 1;
            }

            System.Random random = deterministicSeed.HasValue
                ? new System.Random(deterministicSeed.Value)
                : new System.Random(global::System.Environment.TickCount ^ manager.GetInstanceID());

            if (!PuzzleSolutionGenerator.TryGenerate(count, maxIndices, random, out int[] solution))
            {
                return false;
            }

            if (!manager.TryApplyCorrectIndices(solution))
            {
                return false;
            }

            appliedSolution = solution;
            return true;
        }

        private static string FormatSolutionReadout(MultiDimensionPuzzelManager manager)
        {
            if (manager == null)
            {
                return string.Empty;
            }

            int count = manager.PuzzleElementCount;
            if (count <= 0)
            {
                return string.Empty;
            }

            var indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!manager.TryGetCorrectIndex(i, out indices[i]))
                {
                    indices[i] = -1;
                }
            }

            return FormatIndices(indices);
        }

        private static string FormatIndices(int[] indices)
        {
            if (indices == null || indices.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(",", indices);
        }
    }
}
