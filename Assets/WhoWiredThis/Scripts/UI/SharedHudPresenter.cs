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
        private string currentTimeLine = "00:00";
        private string currentUrgencyPrompt;
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

            if (TimerManager.Instance == null)
            {
                return false;
            }

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

            if (TimerManager.Instance != null)
            {
                HandleTimerUpdated(TimerManager.Instance.GetDisplaySeconds());
            }
            else
            {
                PushToViews();
            }
        }

        private void HandleTimerUpdated(float seconds)
        {
            currentTimeLine = FormatTimeLine(seconds);
            currentUrgencyPrompt = ResolveUrgencyPrompt(seconds);
            PushToViews();
        }

        private static string FormatTimeLine(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }

        private static string ResolveUrgencyPrompt(float remainingSeconds)
        {
            TimerManager timer = TimerManager.Instance;
            if (timer == null || !timer.IsCountdownActive)
            {
                return null;
            }

            int hurryWindow = GameConfigProvider.Active.HurryUpSeconds;
            if (hurryWindow <= 0)
            {
                return null;
            }

            // Match TopBar floor seconds so HURRY UP! N aligns with MM:SS.
            int remaining = Mathf.Max(0, Mathf.FloorToInt(remainingSeconds));
            if (remaining > hurryWindow)
            {
                return null;
            }

            return $"HURRY UP! {remaining}";
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
                playerHudViewA.ApplySharedHudState(currentRoomNameA, string.Empty, currentTimeLine);
                playerHudViewA.SetUrgencyPrompt(currentUrgencyPrompt);
            }

            if (playerHudViewB == null)
            {
                Debug.LogWarning("[SharedHudPresenter] playerHudViewB is not assigned.", this);
            }
            else
            {
                playerHudViewB.ApplySharedHudState(currentRoomNameB, string.Empty, currentTimeLine);
                playerHudViewB.SetUrgencyPrompt(currentUrgencyPrompt);
            }
        }
    }
}
