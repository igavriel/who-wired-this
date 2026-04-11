using System;
using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.Data.A17;

namespace WhoWiredThis.Puzzles.A17
{
    public class A17PuzzleManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PuzzleConfigSO config;

        [Header("Switches")]
        [SerializeField] private PolaritySwitchController[] switches;

        [Header("State (read-only)")]
        [SerializeField, Tooltip("Increments on each failed ENGAGE press.")]
        private int attempts;

        public bool IsSolved { get; private set; }
        public int Attempts => attempts;
        public int HintTriggerAttempt => config.hintTriggerAttempt;

        public event Action OnSuccess;
        public event Action<int> OnFailure;

        public int ComputeCurrentScore()
        {
            int penaltySteps = Mathf.Max(0, attempts - config.penaltyFreeAttempts);
            return Mathf.Max(config.minScore, config.startScore - penaltySteps * config.penaltyPerAttempt);
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
            if (switches == null || switches.Length != config.solution.Length) return false;

            for (int i = 0; i < switches.Length; i++)
            {
                if (switches[i] == null || switches[i].CurrentState != config.solution[i])
                    return false;
            }

            return true;
        }
    }
}
