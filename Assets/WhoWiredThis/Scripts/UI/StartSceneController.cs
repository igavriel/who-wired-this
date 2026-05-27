using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WhoWiredThis.Core;

namespace WhoWiredThis.UI
{
    public class StartSceneController : MonoBehaviour
    {
        private const string BestTimeKey = "PlaytestBestTimeSeconds";

        [SerializeField] private TMP_Text introTextLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private string tutorialSceneName = "Tutorial";
        [SerializeField] private KeyCode playerAActionKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode playerBActionKey = KeyCode.RightControl;
        [SerializeField] private KeyCode bossModifierKey = KeyCode.F12;
        [SerializeField] private KeyCode bossResetKey = KeyCode.Alpha1;

        private bool hasStarted;

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
            if (IsBossResetPressed())
            {
                ResetBestTime();
            }

            if (hasStarted)
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
            PlaytestRunTotal.BeginRun();
            Debug.Log("[StartSceneController] Total-time run tracking started.");
            Debug.Log($"[StartSceneController] Loading scene '{tutorialSceneName}'.");
            SceneManager.LoadScene(tutorialSceneName, LoadSceneMode.Single);
        }

        private bool IsBossResetPressed()
        {
            return (Input.GetKey(bossModifierKey) && Input.GetKeyDown(bossResetKey)) ||
                   (Input.GetKey(bossResetKey) && Input.GetKeyDown(bossModifierKey));
        }

        private static void ResetBestTime()
        {
            PlayerPrefs.DeleteKey(BestTimeKey);
            PlayerPrefs.Save();
            Debug.Log("[StartSceneController] Boss key pressed. Best time was reset.");
        }
    }
}
