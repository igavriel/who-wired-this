using System;
using UnityEngine;

namespace WhoWiredThis.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public const int MaxScore = 100;
        public int CurrentScore { get; private set; }

        public event Action<int> OnScoreChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddScore(int amount)
        {
            CurrentScore = Mathf.Clamp(CurrentScore + amount, 0, MaxScore);
            OnScoreChanged?.Invoke(CurrentScore);
        }

        public void SetScore(int value)
        {
            CurrentScore = Mathf.Clamp(value, 0, MaxScore);
            OnScoreChanged?.Invoke(CurrentScore);
        }

        public void DeductScore(int amount)
        {
            SetScore(CurrentScore - amount);
        }
    }
}
