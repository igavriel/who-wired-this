using UnityEngine;
using TMPro;
using WhoWiredThis.Data.A17;

namespace WhoWiredThis.Puzzles.A17
{
    public class LCDDisplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private A17PuzzleManager puzzleManager;
        [SerializeField] private TextMeshPro displayText;

        [Header("Message Bank")]
        [SerializeField] private LcdMessageBankSO messageBank;

        void Awake()
        {
            if (displayText == null)
                displayText = GetComponentInChildren<TextMeshPro>();
        }

        void Start()
        {
            ShowMessage(messageBank.idleMessage);
            puzzleManager.OnSuccess += HandleSuccess;
            puzzleManager.OnFailure += HandleFailure;
        }

        void OnDestroy()
        {
            puzzleManager.OnSuccess -= HandleSuccess;
            puzzleManager.OnFailure -= HandleFailure;
        }

        private void HandleSuccess() => ShowMessage(messageBank.successMessage);

        private void HandleFailure(int attempts)
        {
            string msg = messageBank.failureMessage;
            if (attempts >= puzzleManager.HintTriggerAttempt)
                msg += $"\n\n<size=70%>Attempts: {attempts} | Hint: check the diagram.</size>";
            ShowMessage(msg);
        }

        private void ShowMessage(string message)
        {
            if (displayText != null)
                displayText.text = message;
        }
    }
}
