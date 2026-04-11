using System;
using UnityEngine;
using WhoWiredThis.Core;

namespace WhoWiredThis.Puzzles.A17
{
    public class A17PuzzleManager : MonoBehaviour
    {
        [Header("Switches")]
        [SerializeField] private PolaritySwitchController[] switches;

        [Header("Solution")]
        [Tooltip("Five values: -1 (Negative), 0 (Off), 1 (Positive). Must include at least one of each.")]
        [SerializeField] private int[] solution = { -1, 0, 1, 0, 1 };

        [Header("Scoring")]
        [SerializeField] private int startScore = 100;
        [SerializeField] private int penaltyFreeAttempts = 5;
        [SerializeField] private int penaltyPerAttempt = 10;
        [SerializeField] private int minScore = 50;
        [SerializeField] private int hintTriggerAttempt = 5;

        [Header("State (read-only)")]
        [SerializeField, Tooltip("Increments on each failed ENGAGE press.")]
        private int attempts;

        public bool IsSolved { get; private set; }
        public int Attempts => attempts;
        public int HintTriggerAttempt => hintTriggerAttempt;

        public event Action OnSuccess;
        public event Action<int> OnFailure;

        public int ComputeCurrentScore()
        {
            int penaltySteps = Mathf.Max(0, attempts - penaltyFreeAttempts);
            return Mathf.Max(minScore, startScore - penaltySteps * penaltyPerAttempt);
        }

        public bool TryEngage()
        {
            if (IsSolved) return false;

            if (CheckSolution())
            {
                IsSolved = true;
                ScoreManager.Instance?.SetScore(ComputeCurrentScore());
                GameManager.Instance?.SolvePuzzle();
                OnSuccess?.Invoke();
                return true;
            }

            attempts++;
            OnFailure?.Invoke(attempts);
            return false;
        }

        private bool CheckSolution()
        {
            if (switches == null || switches.Length != solution.Length) return false;

            for (int i = 0; i < switches.Length; i++)
            {
                if (switches[i] == null || switches[i].SwitchValue != solution[i])
                    return false;
            }

            return true;
        }
    }
}
