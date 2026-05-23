using System;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Tutorial
{
    /// <summary>
    /// Tutorial-only metrics: times and attempt counts from stage events and
    /// <see cref="MultiDimensionPuzzleManager.OnAttemptSubmitted"/>. No scoring or UI.
    /// </summary>
    public class TutorialMetricsTracker : MonoBehaviour
    {
        private const float Unset = -1f;

        [Header("References")]
        [SerializeField]
        private TutorialStageManager tutorialStageManager;

        [SerializeField]
        private MultiDimensionPuzzleManager playerAPuzzleManager;

        [SerializeField]
        private MultiDimensionPuzzleManager playerBPuzzleManager;

        [Header("Runtime (debug, Play Mode)")]
        [SerializeField]
        private int totalAttempts;

        [SerializeField]
        private int playerAAttempts;

        [SerializeField]
        private int playerBAttempts;

        [SerializeField]
        private float totalElapsedSeconds;

        [SerializeField]
        private float playerAElapsedSeconds;

        [SerializeField]
        private float playerBElapsedSeconds;

        [SerializeField]
        private bool playerASolved;

        [SerializeField]
        private bool playerBSolved;

        [SerializeField]
        private bool tutorialComplete;

        private int _playerAAttempts;
        private int _playerBAttempts;
        private float _rtAtTutorialStart = Unset;
        private float _rtAtPlayerASolved = Unset;
        private float _rtAtPlayerBOperator = Unset;
        private float _rtAtPlayerBSolved = Unset;
        private float _rtAtTutorialComplete = Unset;

        private void OnEnable()
        {
            if (tutorialStageManager != null)
            {
                tutorialStageManager.OnTutorialStarted += HandleTutorialStarted;
                tutorialStageManager.OnStageChanged += HandleStageChanged;
                tutorialStageManager.OnTutorialCompleted += HandleTutorialCompleted;
            }
            else
            {
                Debug.LogWarning("[TutorialMetricsTracker] tutorialStageManager is not assigned.", this);
            }

            if (playerAPuzzleManager != null)
            {
                playerAPuzzleManager.OnAttemptSubmitted += HandlePlayerAAttemptSubmitted;
            }
            else
            {
                Debug.LogWarning("[TutorialMetricsTracker] playerAPuzzleManager is not assigned.", this);
            }

            if (playerBPuzzleManager != null)
            {
                playerBPuzzleManager.OnAttemptSubmitted += HandlePlayerBAttemptSubmitted;
            }
            else
            {
                Debug.LogWarning("[TutorialMetricsTracker] playerBPuzzleManager is not assigned.", this);
            }
        }

        private void OnDisable()
        {
            if (tutorialStageManager != null)
            {
                tutorialStageManager.OnTutorialStarted -= HandleTutorialStarted;
                tutorialStageManager.OnStageChanged -= HandleStageChanged;
                tutorialStageManager.OnTutorialCompleted -= HandleTutorialCompleted;
            }

            if (playerAPuzzleManager != null)
            {
                playerAPuzzleManager.OnAttemptSubmitted -= HandlePlayerAAttemptSubmitted;
            }

            if (playerBPuzzleManager != null)
            {
                playerBPuzzleManager.OnAttemptSubmitted -= HandlePlayerBAttemptSubmitted;
            }
        }

        private void Update()
        {
            RefreshDebugFields();
        }

        public TutorialMetricsSnapshot GetSnapshot()
        {
            float rt = Time.realtimeSinceStartup;
            bool aSolved = _rtAtPlayerASolved >= 0f;
            bool bSolved = _rtAtPlayerBSolved >= 0f;
            bool complete = _rtAtTutorialComplete >= 0f;
            int ta = _playerAAttempts;
            int tb = _playerBAttempts;

            float totalEl = 0f;
            if (_rtAtTutorialStart >= 0f)
            {
                totalEl = complete ? (_rtAtTutorialComplete - _rtAtTutorialStart) : (rt - _rtAtTutorialStart);
            }

            float aEl = 0f;
            if (_rtAtTutorialStart >= 0f)
            {
                aEl = aSolved ? (_rtAtPlayerASolved - _rtAtTutorialStart) : (rt - _rtAtTutorialStart);
            }

            float bEl = 0f;
            if (_rtAtPlayerBOperator >= 0f)
            {
                bEl = bSolved ? (_rtAtPlayerBSolved - _rtAtPlayerBOperator) : (rt - _rtAtPlayerBOperator);
            }

            return new TutorialMetricsSnapshot(
                ta + tb,
                ta,
                tb,
                totalEl,
                aEl,
                bEl,
                aSolved,
                bSolved,
                complete);
        }

        private void HandleTutorialStarted()
        {
            if (_rtAtTutorialStart >= 0f)
            {
                return;
            }

            _rtAtTutorialStart = Time.realtimeSinceStartup;
            RefreshDebugFields();
        }

        private void HandleStageChanged(TutorialSessionStage stage)
        {
            if (stage == TutorialSessionStage.PlayerBOperator && _rtAtPlayerBOperator < 0f)
            {
                _rtAtPlayerBOperator = Time.realtimeSinceStartup;
            }

            RefreshDebugFields();
        }

        private void HandleTutorialCompleted()
        {
            if (_rtAtTutorialComplete >= 0f)
            {
                return;
            }

            _rtAtTutorialComplete = Time.realtimeSinceStartup;
            RefreshDebugFields();
        }

        private void HandlePlayerAAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            _playerAAttempts++;
            if (result.IsSolved && _rtAtPlayerASolved < 0f)
            {
                _rtAtPlayerASolved = Time.realtimeSinceStartup;
            }

            RefreshDebugFields();
        }

        private void HandlePlayerBAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            _playerBAttempts++;
            if (result.IsSolved && _rtAtPlayerBSolved < 0f)
            {
                _rtAtPlayerBSolved = Time.realtimeSinceStartup;
            }

            RefreshDebugFields();
        }

        private void RefreshDebugFields()
        {
            float rt = Time.realtimeSinceStartup;
            playerAAttempts = _playerAAttempts;
            playerBAttempts = _playerBAttempts;
            totalAttempts = playerAAttempts + playerBAttempts;
            playerASolved = _rtAtPlayerASolved >= 0f;
            playerBSolved = _rtAtPlayerBSolved >= 0f;
            tutorialComplete = _rtAtTutorialComplete >= 0f;

            if (_rtAtTutorialStart < 0f)
            {
                totalElapsedSeconds = 0f;
                playerAElapsedSeconds = 0f;
                playerBElapsedSeconds = 0f;
                return;
            }

            totalElapsedSeconds = tutorialComplete
                ? (_rtAtTutorialComplete - _rtAtTutorialStart)
                : (rt - _rtAtTutorialStart);

            playerAElapsedSeconds = playerASolved
                ? (_rtAtPlayerASolved - _rtAtTutorialStart)
                : (rt - _rtAtTutorialStart);

            if (_rtAtPlayerBOperator < 0f)
            {
                playerBElapsedSeconds = 0f;
            }
            else
            {
                playerBElapsedSeconds = playerBSolved
                    ? (_rtAtPlayerBSolved - _rtAtPlayerBOperator)
                    : (rt - _rtAtPlayerBOperator);
            }
        }
    }
}
