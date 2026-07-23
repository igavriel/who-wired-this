using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using WhoWiredThis.Core;
using WhoWiredThis.Scenes;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Tutorial
{
    /// <summary>
    /// Per-scene metrics feeder for <see cref="ScoreManager"/>: times and attempt/retry
    /// counts from stage events and <see cref="MultiDimensionPuzzleManager.OnAttemptSubmitted"/>.
    /// Survives role-swap reloads by seeding from any existing <see cref="ScoreManager"/> level record.
    /// </summary>
    public class TutorialMetricsTracker : MonoBehaviour
    {
        private const float Unset = -1f;

        [Header("References")]
        [FormerlySerializedAs("tutorialStageManager")]
        [SerializeField]
        private SceneStageManager sceneStageManager;

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
        private int playerARetries;

        [SerializeField]
        private int playerBRetries;

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
        private int _playerARetries;
        private int _playerBRetries;
        private float _rtAtTutorialStart = Unset;
        private float _rtAtPlayerASolved = Unset;
        private float _rtAtPlayerBOperator = Unset;
        private float _rtAtPlayerBSolved = Unset;
        private float _rtAtTutorialComplete = Unset;
        private string _activePlayerLabel = string.Empty;
        private string _trackedSceneName = string.Empty;
        private bool _finalizedToScoreManager;

        private void OnEnable()
        {
            _trackedSceneName = SceneManager.GetActiveScene().name;
            SeedFromExistingScoreRecord();

            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageStarted += HandleStageStarted;
                sceneStageManager.OnStageChanged += HandleStageChanged;
                sceneStageManager.OnStageCompleted += HandleStageCompleted;
            }
            else
            {
                Debug.LogWarning("[TutorialMetricsTracker] sceneStageManager is not assigned.", this);
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

            if (sceneStageManager == null || playerAPuzzleManager == null || playerBPuzzleManager == null)
            {
                if (ScoreManager.IsGameplayLevel(_trackedSceneName))
                {
                    Debug.LogWarning(
                        $"[TutorialMetricsTracker] Incomplete wiring in gameplay scene '{_trackedSceneName}'.",
                        this);
                }
            }

            if (ScoreManager.IsGameplayLevel(_trackedSceneName))
            {
                TimerManager.Instance?.StartLevelCountdown();
            }

            PushToScoreManager(levelComplete: false);
        }

        private void OnDisable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageStarted -= HandleStageStarted;
                sceneStageManager.OnStageChanged -= HandleStageChanged;
                sceneStageManager.OnStageCompleted -= HandleStageCompleted;
            }

            if (playerAPuzzleManager != null)
            {
                playerAPuzzleManager.OnAttemptSubmitted -= HandlePlayerAAttemptSubmitted;
            }

            if (playerBPuzzleManager != null)
            {
                playerBPuzzleManager.OnAttemptSubmitted -= HandlePlayerBAttemptSubmitted;
            }

            // Capture partial play if the scene unloads before stage completed.
            // Uses cached scene name — GetActiveScene() may already be the next scene.
            if (!_finalizedToScoreManager)
            {
                PushToScoreManager(levelComplete: false);
            }
        }

        private void Update()
        {
            RefreshDebugFields();
            PushToScoreManager(levelComplete: _rtAtTutorialComplete >= 0f);
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

            // Prefer ScoreManager merged scene total so role-swap cutscene gap is not required in local clocks.
            LevelPlayRecord existing = ScoreManager.TryGetLevel(_trackedSceneName);
            if (existing != null)
            {
                float mergedPlay = existing.Blue.PlaySeconds + existing.Red.PlaySeconds;
                if (mergedPlay > totalEl)
                {
                    totalEl = mergedPlay;
                }
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

        private void SeedFromExistingScoreRecord()
        {
            if (!ScoreManager.IsGameplayLevel(_trackedSceneName))
            {
                return;
            }

            LevelPlayRecord existing = ScoreManager.TryGetLevel(_trackedSceneName);
            if (existing == null)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            _playerAAttempts = existing.Blue.Attempts;
            _playerARetries = existing.Blue.Retries;
            _playerBAttempts = existing.Red.Attempts;
            _playerBRetries = existing.Red.Retries;

            if (existing.Blue.PlaySeconds > 0f || existing.Blue.Attempts > 0 || existing.Blue.Solved)
            {
                _rtAtTutorialStart = now - Mathf.Max(0f, existing.Blue.PlaySeconds);
                if (existing.Blue.Solved)
                {
                    _rtAtPlayerASolved = now;
                }
            }

            if (existing.Red.PlaySeconds > 0f || existing.Red.Attempts > 0 || existing.Red.Solved)
            {
                _rtAtPlayerBOperator = now - Mathf.Max(0f, existing.Red.PlaySeconds);
                if (existing.Red.Solved)
                {
                    _rtAtPlayerBSolved = now;
                }
            }

            if (existing.Completed)
            {
                _rtAtTutorialComplete = now;
                _finalizedToScoreManager = true;
            }

            Debug.Log(
                $"[TutorialMetricsTracker] Seeded '{_trackedSceneName}' from ScoreManager: " +
                $"Blue {existing.Blue.Attempts} att/{existing.Blue.Retries} retries, " +
                $"Red {existing.Red.Attempts} att/{existing.Red.Retries} retries.",
                this);
            RefreshDebugFields();
        }

        private void HandleStageStarted()
        {
            if (_rtAtTutorialStart >= 0f)
            {
                return;
            }

            _rtAtTutorialStart = Time.realtimeSinceStartup;
            _activePlayerLabel = "Blue";
            RefreshDebugFields();
            PushToScoreManager(levelComplete: false);
        }

        private void HandleStageChanged(SceneSessionStage stage)
        {
            if (stage == SceneSessionStage.PlayerBOperator && _rtAtPlayerBOperator < 0f)
            {
                _rtAtPlayerBOperator = Time.realtimeSinceStartup;
            }

            _activePlayerLabel = stage == SceneSessionStage.PlayerBOperator ? "Red" : "Blue";
            RefreshDebugFields();
            PushToScoreManager(levelComplete: false);
        }

        private void HandleStageCompleted()
        {
            if (_rtAtTutorialComplete >= 0f)
            {
                return;
            }

            _rtAtTutorialComplete = Time.realtimeSinceStartup;
            RefreshDebugFields();
            PushToScoreManager(levelComplete: true);
            _finalizedToScoreManager = true;
        }

        private void HandlePlayerAAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            _playerAAttempts++;
            if (!result.IsSolved)
            {
                _playerARetries++;
            }

            if (result.IsSolved && _rtAtPlayerASolved < 0f)
            {
                _rtAtPlayerASolved = Time.realtimeSinceStartup;
            }

            RefreshDebugFields();
            PushToScoreManager(levelComplete: false);
        }

        private void HandlePlayerBAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            _playerBAttempts++;
            if (!result.IsSolved)
            {
                _playerBRetries++;
            }

            if (result.IsSolved && _rtAtPlayerBSolved < 0f)
            {
                _rtAtPlayerBSolved = Time.realtimeSinceStartup;
            }

            RefreshDebugFields();
            PushToScoreManager(levelComplete: false);
        }

        private void PushToScoreManager(bool levelComplete)
        {
            string sceneName = string.IsNullOrEmpty(_trackedSceneName)
                ? SceneManager.GetActiveScene().name
                : _trackedSceneName;

            if (!ScoreManager.IsGameplayLevel(sceneName))
            {
                return;
            }

            TutorialMetricsSnapshot snapshot = GetSnapshot();
            ScoreManager.UpdateLiveLevel(
                sceneName,
                snapshot.PlayerAAttempts,
                _playerARetries,
                snapshot.PlayerAElapsedSeconds,
                snapshot.PlayerASolved,
                snapshot.PlayerBAttempts,
                _playerBRetries,
                snapshot.PlayerBElapsedSeconds,
                snapshot.PlayerBSolved,
                snapshot.TotalElapsedSeconds,
                _activePlayerLabel,
                levelComplete || snapshot.TutorialComplete);
        }

        private void RefreshDebugFields()
        {
            float rt = Time.realtimeSinceStartup;
            playerAAttempts = _playerAAttempts;
            playerBAttempts = _playerBAttempts;
            playerARetries = _playerARetries;
            playerBRetries = _playerBRetries;
            totalAttempts = playerAAttempts + playerBAttempts;
            playerASolved = _rtAtPlayerASolved >= 0f;
            playerBSolved = _rtAtPlayerBSolved >= 0f;
            tutorialComplete = _rtAtTutorialComplete >= 0f;

            if (_rtAtTutorialStart < 0f && _rtAtPlayerBOperator < 0f)
            {
                totalElapsedSeconds = 0f;
                playerAElapsedSeconds = 0f;
                playerBElapsedSeconds = 0f;
                return;
            }

            if (_rtAtTutorialStart >= 0f)
            {
                totalElapsedSeconds = tutorialComplete
                    ? (_rtAtTutorialComplete - _rtAtTutorialStart)
                    : (rt - _rtAtTutorialStart);

                playerAElapsedSeconds = playerASolved
                    ? (_rtAtPlayerASolved - _rtAtTutorialStart)
                    : (rt - _rtAtTutorialStart);
            }
            else
            {
                totalElapsedSeconds = 0f;
                playerAElapsedSeconds = 0f;
            }

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
