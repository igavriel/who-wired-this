using System;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    public class PuzzleScoreSession
    {
        private readonly int startScore;
        private readonly int penaltyFreeAttempts;
        private readonly int penaltyPerAttempt;
        private readonly int minScore;

        public PuzzleScoreSession(int startScore, int penaltyFreeAttempts, int penaltyPerAttempt, int minScore, int hintTriggerAttempt)
        {
            this.startScore = startScore;
            this.penaltyFreeAttempts = penaltyFreeAttempts;
            this.penaltyPerAttempt = penaltyPerAttempt;
            this.minScore = minScore;
            HintTriggerAttempt = hintTriggerAttempt;
        }

        public bool IsSolved { get; private set; }
        public int Attempts { get; private set; }
        public int HintTriggerAttempt { get; }

        public int ComputeCurrentScore()
        {
            int penaltySteps = Mathf.Max(0, Attempts - penaltyFreeAttempts);
            return Mathf.Max(minScore, startScore - penaltySteps * penaltyPerAttempt);
        }

        public bool TryEngage(Func<bool> checkSolution, Action<int> onSuccess, Action<int> onFailure)
        {
            if (IsSolved)
            {
                return false;
            }

            if (checkSolution())
            {
                IsSolved = true;
                onSuccess?.Invoke(ComputeCurrentScore());
                return true;
            }

            Attempts++;
            onFailure?.Invoke(Attempts);
            return false;
        }
    }
}
