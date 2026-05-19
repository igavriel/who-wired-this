using System;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Simple v1 generator for three-slot pipe puzzle solutions (Puzzel Pipes).
    /// </summary>
    public static class PuzzleSolutionGenerator
    {
        private const int MaxAttempts = 32;

        private static readonly int[] FallbackSolution = { 1, 2, 1 };

        /// <summary>
        /// Generates <paramref name="length"/> indices where each value is in [0, maxIndexPerElement[i]].
        /// </summary>
        public static bool TryGenerate(int length, int[] maxIndexPerElement, System.Random random, out int[] solution)
        {
            solution = null;
            if (length <= 0 || random == null || maxIndexPerElement == null || maxIndexPerElement.Length != length)
            {
                return false;
            }

            solution = new int[length];
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                for (int i = 0; i < length; i++)
                {
                    int maxIndex = maxIndexPerElement[i];
                    if (maxIndex < 0)
                    {
                        return false;
                    }

                    solution[i] = random.Next(0, maxIndex + 1);
                }

                if (PassesConstraints(solution, maxIndexPerElement))
                {
                    return true;
                }
            }

            if (TryApplyFallback(solution, maxIndexPerElement))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public static bool PassesConstraints(int[] solution, int[] maxIndexPerElement)
        {
            if (solution == null || maxIndexPerElement == null || solution.Length != maxIndexPerElement.Length)
            {
                return false;
            }

            int length = solution.Length;
            if (length < 2)
            {
                return true;
            }

            bool allSame = true;
            for (int i = 1; i < length; i++)
            {
                if (solution[i] != solution[0])
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
            {
                return false;
            }

            bool allEdges = true;
            bool hasMiddle = false;
            for (int i = 0; i < length; i++)
            {
                int value = solution[i];
                int maxIndex = maxIndexPerElement[i];
                if (value != 0 && value != maxIndex)
                {
                    allEdges = false;
                }

                if (maxIndex >= 2 && value > 0 && value < maxIndex)
                {
                    hasMiddle = true;
                }
            }

            if (allEdges)
            {
                return false;
            }

            if (!hasMiddle)
            {
                return false;
            }

            return true;
        }

        private static bool TryApplyFallback(int[] solution, int[] maxIndexPerElement)
        {
            int length = solution.Length;
            for (int i = 0; i < length; i++)
            {
                int maxIndex = maxIndexPerElement[i];
                solution[i] = FallbackSolution[i % FallbackSolution.Length];
                if (solution[i] > maxIndex)
                {
                    solution[i] = maxIndex;
                }
            }

            return PassesConstraints(solution, maxIndexPerElement);
        }
    }
}
