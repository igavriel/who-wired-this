using System;
using UnityEngine;

namespace WhoWiredThis.Core
{
    public class TimerManager : MonoBehaviour
    {
        public static TimerManager Instance { get; private set; }

        public float ElapsedSeconds { get; private set; }
        public bool IsRunning { get; private set; } = true;

        public event Action<float> OnTimerUpdated;

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

            ElapsedSeconds += Time.deltaTime;
            OnTimerUpdated?.Invoke(ElapsedSeconds);
        }

        public void Stop() => IsRunning = false;
        public void Resume() => IsRunning = true;
    }
}
