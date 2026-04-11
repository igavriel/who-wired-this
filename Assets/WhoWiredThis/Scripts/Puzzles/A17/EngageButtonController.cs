using UnityEngine;
using WhoWiredThis.Data.A17;
using WhoWiredThis.Interactables;
using WhoWiredThis.UI;

namespace WhoWiredThis.Puzzles.A17
{
    public class EngageButtonController : MonoBehaviour, IInteractable
    {
        [Header("Puzzle")]
        [SerializeField] private A17PuzzleManager puzzleManager;

        [Header("Message Bank")]
        [SerializeField] private LcdMessageBankSO messageBank;

        [Header("Visuals")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material successMaterial;

        private int failIndex;

        void Awake()
        {
            if (buttonRenderer == null)
                buttonRenderer = GetComponent<Renderer>();
        }

        public string GetPromptText()
        {
            if (puzzleManager != null && puzzleManager.IsSolved)
                return "POLARITY ENGAGED";
            return "[E] ENGAGE";
        }

        public void Interact(GameObject interactor)
        {
            if (puzzleManager == null || puzzleManager.IsSolved) return;

            bool success = puzzleManager.TryEngage();

            if (success)
            {
                HandleSuccess();
            }
            else
            {
                HandleFail(puzzleManager.Attempts);
            }
        }

        private void HandleSuccess()
        {
            if (buttonRenderer != null && successMaterial != null)
                buttonRenderer.sharedMaterial = successMaterial;

            MessagePanel.Instance?.Show(
                "<b>[*] POLARITY ENGAGED [*]</b>\n\n" +
                $"Unit A17 online. Energy matrix stabilized.\n" +
                $"<size=70%>Score recorded: {puzzleManager.ComputeCurrentScore()}</size>");
        }

        private void HandleFail(int currentAttempts)
        {
            string[] pool = messageBank.engageFailMessages;

            string msg = pool[failIndex % pool.Length];
            failIndex++;

            if (currentAttempts >= puzzleManager.HintTriggerAttempt)
            {
                msg += $"\n\n<size=70%>Attempts: {currentAttempts} | Hint: check the diagram.</size>";
            }

            MessagePanel.Instance?.Show(msg);
        }
    }
}
