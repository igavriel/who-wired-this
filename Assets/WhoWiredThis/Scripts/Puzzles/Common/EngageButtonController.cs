using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Data.Puzzels;
using WhoWiredThis.UI;
using WhoWiredThis.Util;

namespace WhoWiredThis.Puzzles.Common
{
    public class EngageButtonController : MonoBehaviour, IInteractable
    {
        [Header("Puzzle")]
        [SerializeField] private MonoBehaviour puzzleManager;

        [Header("Message Bank")]
        [SerializeField] private LcdMessageBankSO messageBank;

        [Header("Visuals")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material successMaterial;

        private int failIndex;
        private IPuzzleManager resolvedPuzzleManager;
        private IPuzzleManager PuzzleManager => resolvedPuzzleManager;

        void Awake()
        {
            if (buttonRenderer == null)
                buttonRenderer = GetComponent<Renderer>();

            resolvedPuzzleManager = PuzzleManagerResolver.ResolvePuzzleManagerReference(
                puzzleManager,
                this,
                nameof(EngageButtonController));
        }

        public string GetPromptText()
        {
            IPuzzleManager manager = PuzzleManager;
            if (manager != null && manager.IsSolved)
                return messageBank.promptSolvedMessage;
            return messageBank.promptUnsolvedMessage;
        }

        public void Interact(GameObject interactor)
        {
            IPuzzleManager manager = PuzzleManager;
            if (manager == null || manager.IsSolved) return;

            bool success = manager.TryEngage();

            if (success)
                HandleSuccess();
            else
                HandleFail(manager.Attempts);
        }

        private void HandleSuccess()
        {
            if (buttonRenderer != null && successMaterial != null)
                buttonRenderer.sharedMaterial = successMaterial;

            IPuzzleManager manager = PuzzleManager;
            if (manager == null)
            {
                return;
            }

            string msg = messageBank.successMessage;
            msg += $"\n\n<size=70%>Score recorded: {manager.ComputeCurrentScore()}</size>";
            MessagePanel.Instance?.Show(msg);
        }

        private void HandleFail(int currentAttempts)
        {
            string[] pool = messageBank.engageFailMessages;

            string msg = pool[failIndex % pool.Length];
            failIndex++;

            IPuzzleManager manager = PuzzleManager;
            if (manager != null && currentAttempts >= manager.HintTriggerAttempt)
                msg += $"\n\n<size=70%>Attempts: {currentAttempts} | Hint: check the diagram.</size>";

            MessagePanel.Instance?.Show(msg);
        }

    }
}
