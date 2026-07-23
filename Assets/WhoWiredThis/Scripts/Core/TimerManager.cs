using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Core
{
    public class TimerManager : MonoBehaviour
    {
        public const float DefaultLevelDurationSeconds = 480f;

        public static TimerManager Instance { get; private set; }

        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds { get; private set; }
        public float LevelDurationSeconds { get; private set; } = DefaultLevelDurationSeconds;
        public bool IsCountdownActive { get; private set; }
        public bool IsRunning { get; private set; } = true;

        public event Action<float> OnTimerUpdated;

        private bool expiredHandled;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (IsCountdownActive)
            {
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
                ElapsedSeconds = Mathf.Max(0f, LevelDurationSeconds - RemainingSeconds);
                OnTimerUpdated?.Invoke(RemainingSeconds);

                if (RemainingSeconds <= 0f)
                {
                    RemainingSeconds = 0f;
                    ElapsedSeconds = LevelDurationSeconds;
                    IsRunning = false;
                    HandleLevelExpired();
                }

                return;
            }

            ElapsedSeconds += Time.deltaTime;
            OnTimerUpdated?.Invoke(ElapsedSeconds);
        }

        public void Stop() => IsRunning = false;

        public void Resume() => IsRunning = true;

        public void StartLevelCountdown(float durationSeconds = DefaultLevelDurationSeconds)
        {
            LevelDurationSeconds = Mathf.Max(1f, durationSeconds);
            RemainingSeconds = LevelDurationSeconds;
            ElapsedSeconds = 0f;
            IsCountdownActive = true;
            IsRunning = true;
            expiredHandled = false;
            OnTimerUpdated?.Invoke(RemainingSeconds);
            Debug.Log($"[TimerManager] Level countdown started: {ScoreManager.FormatTime(RemainingSeconds)}.");
        }

        public void StopLevelCountdown()
        {
            IsCountdownActive = false;
            IsRunning = false;
        }

        public float GetDisplaySeconds()
        {
            return IsCountdownActive ? RemainingSeconds : ElapsedSeconds;
        }

        private void HandleLevelExpired()
        {
            if (expiredHandled)
            {
                return;
            }

            expiredHandled = true;
            Debug.Log("[TimerManager] Level countdown expired.");

            if (!ScoreManager.HasActiveRun)
            {
                return;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (!ScoreManager.IsGameplayLevel(activeSceneName))
            {
                return;
            }

            if (!PlaytestFlowUtility.TryEndRunAndLoadGameOver(abandoned: true, out string error))
            {
                Debug.LogWarning($"[TimerManager] Failed to load Game Over after timeout: {error}");
            }
        }
    }
}
