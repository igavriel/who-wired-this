using System;
using UnityEngine;

namespace WhoWiredThis.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        public string currentZoneName = "Relay Room";
        public bool PuzzleSolved { get; private set; }

        public event Action<string> OnZoneChanged;
        public event Action OnPuzzleSolved;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetZone(string zoneName)
        {
            currentZoneName = zoneName;
            OnZoneChanged?.Invoke(zoneName);
        }

        public void SolvePuzzle()
        {
            if (PuzzleSolved)
            {
                return;
            }

            PuzzleSolved = true;
            TimerManager.Instance?.Stop();
            OnPuzzleSolved?.Invoke();
        }
    }
}
