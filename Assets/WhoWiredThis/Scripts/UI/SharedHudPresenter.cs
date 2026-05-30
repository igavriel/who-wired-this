using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.Enums;

namespace WhoWiredThis.UI
{
    public class SharedHudPresenter : MonoBehaviour
    {
        [Header("Player HUD views")]
        [SerializeField] private PlayerHudView playerHudViewA;
        [SerializeField] private PlayerHudView playerHudViewB;

        public PlayerHudView PlayerHudViewA => playerHudViewA;
        public PlayerHudView PlayerHudViewB => playerHudViewB;

        private string currentRoomNameA = string.Empty;
        private string currentRoomNameB = string.Empty;
        private string currentScoreLine = string.Empty;
        private string currentTimeLine = "00:00";
        private bool subscribed;

        void Start()
        {
            EnsureSubscribedAndPush();
        }

        void OnEnable()
        {
            EnsureSubscribedAndPush();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        private void EnsureSubscribedAndPush()
        {
            if (!TrySubscribe())
            {
                return;
            }

            PushAllFromManagers();
        }

        private bool TrySubscribe()
        {
            if (subscribed)
            {
                return true;
            }

            if (ScoreManager.Instance == null || TimerManager.Instance == null)
            {
                return false;
            }

            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            TimerManager.Instance.OnTimerUpdated += HandleTimerUpdated;

            if (PlayerZoneTracker.Instance != null)
            {
                PlayerZoneTracker.Instance.OnPlayerAZoneChanged += HandlePlayerAZoneChanged;
                PlayerZoneTracker.Instance.OnPlayerBZoneChanged += HandlePlayerBZoneChanged;
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZoneChanged += HandleSharedZoneChanged;
            }

            subscribed = true;
            return true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            }

            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnTimerUpdated -= HandleTimerUpdated;
            }

            if (PlayerZoneTracker.Instance != null)
            {
                PlayerZoneTracker.Instance.OnPlayerAZoneChanged -= HandlePlayerAZoneChanged;
                PlayerZoneTracker.Instance.OnPlayerBZoneChanged -= HandlePlayerBZoneChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZoneChanged -= HandleSharedZoneChanged;
            }

            subscribed = false;
        }

        private void PushAllFromManagers()
        {
            if (PlayerZoneTracker.Instance != null)
            {
                HandlePlayerAZoneChanged(PlayerZoneTracker.Instance.GetZone(AllowedPlayerTag.Player_A));
                HandlePlayerBZoneChanged(PlayerZoneTracker.Instance.GetZone(AllowedPlayerTag.Player_B));
            }
            else if (GameManager.Instance != null)
            {
                HandleSharedZoneChanged(GameManager.Instance.currentZoneName);
            }

            if (ScoreManager.Instance != null)
            {
                HandleScoreChanged(ScoreManager.Instance.CurrentScore);
            }

            if (TimerManager.Instance != null)
            {
                HandleTimerUpdated(TimerManager.Instance.ElapsedSeconds);
            }
        }

        private void HandleScoreChanged(int score)
        {
            currentScoreLine = $"Score: {score}/{ScoreManager.MaxScore}";
            PushToViews();
        }

        private void HandleTimerUpdated(float seconds)
        {
            int minutes = (int)seconds / 60;
            int secs = (int)seconds % 60;
            currentTimeLine = $"{minutes:00}:{secs:00}";
            PushToViews();
        }

        private void HandlePlayerAZoneChanged(string zoneName)
        {
            currentRoomNameA = zoneName;
            PushToViews();
        }

        private void HandlePlayerBZoneChanged(string zoneName)
        {
            currentRoomNameB = zoneName;
            PushToViews();
        }

        private void HandleSharedZoneChanged(string zoneName)
        {
            currentRoomNameA = zoneName;
            currentRoomNameB = zoneName;
            PushToViews();
        }

        private void PushToViews()
        {
            if (playerHudViewA == null)
            {
                Debug.LogWarning("[SharedHudPresenter] playerHudViewA is not assigned.", this);
            }
            else
            {
                playerHudViewA.ApplySharedHudState(currentRoomNameA, currentScoreLine, currentTimeLine);
            }

            if (playerHudViewB == null)
            {
                Debug.LogWarning("[SharedHudPresenter] playerHudViewB is not assigned.", this);
            }
            else
            {
                playerHudViewB.ApplySharedHudState(currentRoomNameB, currentScoreLine, currentTimeLine);
            }
        }
    }
}
