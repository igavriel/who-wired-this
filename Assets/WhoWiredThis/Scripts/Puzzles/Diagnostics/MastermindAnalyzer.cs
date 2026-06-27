using System.Collections.Generic;

namespace WhoWiredThis.Puzzles.Diagnostics
{
    /// <summary>
    /// Pure Mastermind classification for one position. Membership-based Present
    /// (multiplicity intentionally simplified for the 2-stick tutorial).
    /// </summary>
    public static class MastermindAnalyzer
    {
        public static SymbolMatch Evaluate(int value, int position, IReadOnlyList<int> solution)
        {
            if (solution == null || position < 0 || position >= solution.Count)
            {
                return SymbolMatch.Absent;
            }

            if (value == solution[position])
            {
                return SymbolMatch.Exact;
            }

            for (int i = 0; i < solution.Count; i++)
            {
                if (solution[i] == value)
                {
                    return SymbolMatch.Present;
                }
            }

            return SymbolMatch.Absent;
        }

        public static SymbolMatch[] EvaluateAll(IReadOnlyList<int> solution, IReadOnlyList<int> attempt)
        {
            int n = solution?.Count ?? 0;
            var result = new SymbolMatch[n];
            for (int i = 0; i < n; i++)
            {
                int v = attempt != null && i < attempt.Count ? attempt[i] : -1;
                result[i] = Evaluate(v, i, solution);
            }

            return result;
        }
    }
}
