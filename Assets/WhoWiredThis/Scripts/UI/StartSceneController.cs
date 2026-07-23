using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Scenes;

namespace WhoWiredThis.UI
{
    public class StartSceneController : MonoBehaviour
    {
        [SerializeField] private TMP_Text introTextLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private PlaytestSceneFlowBootstrap flowBootstrap;
        [SerializeField] private KeyCode playerAActionKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode playerBActionKey = KeyCode.RightControl;

        private bool hasStarted;
        private float inputReadyTime;

        private void OnEnable()
        {
            inputReadyTime = Time.unscaledTime + 0.35f;
        }

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
                startButton.onClick.AddListener(HandleStartClicked);
            }
            else
            {
                Debug.LogWarning("[StartSceneController] startButton is not assigned.", this);
            }
        }

        private void Update()
        {
            if (hasStarted || Time.unscaledTime < inputReadyTime)
            {
                return;
            }

            if (Input.GetKeyDown(playerAActionKey) || Input.GetKeyDown(playerBActionKey))
            {
                Debug.Log("[StartSceneController] Player action key pressed. Starting run.");
                HandleStartClicked();
            }
        }

        private void HandleStartClicked()
        {
            if (hasStarted)
            {
                return;
            }

            hasStarted = true;
            Debug.Log("[StartSceneController] Start button clicked.");

            PlaytestRunSummary.Clear();
            SharedHistorySO.ClearAllLoaded();
            SceneRoleState.Reset();
            ScoreManager.BeginRun();
            Debug.Log("[StartSceneController] Total-time run tracking started.");

            if (flowBootstrap == null)
            {
                flowBootstrap = PlaytestSceneFlowBootstrap.FindBootstrap();
            }

            if (flowBootstrap == null)
            {
                Debug.LogError("[StartSceneController] PlaytestSceneFlowBootstrap not found.", this);
                ScoreManager.ResetRun();
                hasStarted = false;
                return;
            }

            if (!flowBootstrap.TryLoadNextScene(
                    this,
                    fadeOutDurationSeconds: 0f,
                    fadeOverlays: null,
                    ignoreWhenAlreadyInTargetScene: true,
                    preferFadeWhenAvailable: false,
                    out string loadError))
            {
                Debug.LogError($"[StartSceneController] Failed to load next scene: {loadError}");
                ScoreManager.ResetRun();
                hasStarted = false;
            }
        }
    }
}
