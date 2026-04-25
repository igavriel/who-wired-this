using UnityEngine;
using WhoWiredThis.Interactables;
using WhoWiredThis.UI;

namespace WhoWiredThis.Puzzles.FloorColor
{
    public class FloorColorEngageButtonController : MonoBehaviour, IInteractable
    {
        [Header("Puzzle")]
        [SerializeField] private FloorColorMatrixPuzzleManager puzzleManager;

        [Header("Visuals")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material successMaterial;

        private void Awake()
        {
            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponent<Renderer>();
            }
        }

        public string GetPromptText()
        {
            if (puzzleManager != null && puzzleManager.IsSolved)
            {
                return "FLOOR PATTERN ENGAGED";
            }

            return "$INTERACT$ ENGAGE";
        }

        public void Interact(GameObject interactor)
        {
            if (puzzleManager == null || puzzleManager.IsSolved)
            {
                return;
            }

            bool success = puzzleManager.TryEngage();
            if (success)
            {
                if (buttonRenderer != null && successMaterial != null)
                {
                    buttonRenderer.sharedMaterial = successMaterial;
                }

                MessagePanel.Instance?.Show(
                    "<b>[*] FLOOR PATTERN ENGAGED [*]</b>\n\n" +
                    $"Matrix stabilized.\n" +
                    $"<size=70%>Score recorded: {puzzleManager.ComputeCurrentScore()}</size>");
            }
            else
            {
                MessagePanel.Instance?.Show(
                    $"Pattern mismatch.\n\n<size=70%>Attempts: {puzzleManager.Attempts}</size>");
            }
        }
    }
}
