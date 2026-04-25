using UnityEngine;
using TMPro;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Data.Puzzels;
using WhoWiredThis.Util;

namespace WhoWiredThis.Puzzles.Common
{
    public class LCDDisplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonoBehaviour puzzleManager;
        [SerializeField] private TextMeshPro displayText;

        [Header("Message Bank")]
        [SerializeField] private LcdMessageBankSO messageBank;

        private IPuzzleManager resolvedPuzzleManager;
        private IPuzzleManager PuzzleManager => resolvedPuzzleManager;

        void Awake()
        {
            if (displayText == null)
                displayText = GetComponentInChildren<TextMeshPro>();

            resolvedPuzzleManager = PuzzleManagerResolver.ResolvePuzzleManagerReference(
                puzzleManager,
                this,
                nameof(LCDDisplayController));
        }

        void Start()
        {
            ShowMessage(messageBank.idleMessage);
            IPuzzleManager manager = PuzzleManager;
            if (manager != null)
            {
                manager.OnSuccess += HandleSuccess;
                manager.OnFailure += HandleFailure;
            }
        }

        void OnDestroy()
        {
            IPuzzleManager manager = PuzzleManager;
            if (manager != null)
            {
                manager.OnSuccess -= HandleSuccess;
                manager.OnFailure -= HandleFailure;
            }
        }

        private void HandleSuccess() => ShowMessage(messageBank.successMessage);

        private void HandleFailure(int attempts)
        {
            string msg = messageBank.failureMessage;
            IPuzzleManager manager = PuzzleManager;
            if (manager != null && attempts >= manager.HintTriggerAttempt)
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
