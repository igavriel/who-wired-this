using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class TestButton : MonoBehaviour, IInteractable
    {
        [Header("Sockets")]
        public PuzzleSocket socketA;
        public PuzzleSocket socketB;

        [Header("Expected item IDs")]
        public string correctItemIdA = "power_coupler";
        public string correctItemIdB = "flow_regulator";

        [Header("Visuals")]
        public Renderer buttonRenderer;
        public Material idleMaterial;
        public Material successMaterial;

        private static readonly string[] FailMessages =
        {
            "The machine emits a sad <i>bloop</i>. More power is not the same as correct flow.",
            "Sparks. Smoke. Disappointment. The machine judges you silently.",
            "A small puff of acrid smoke escapes the vents. The readout blinks: NOPE.",
            "The relay hums, pauses, then shuts itself off in protest.",
            "Error: Incorrect configuration. Try engaging your brain.",
            "Nothing happens — except a very faint sigh from the ventilation.",
        };

        private int failCount;

        public string GetPromptText() =>
            GameManager.Instance != null && GameManager.Instance.PuzzleSolved
                ? "Relay online."
                : "[E] Test relay";

        public void Interact(GameObject interactor)
        {
            if (GameManager.Instance != null && GameManager.Instance.PuzzleSolved)
            {
                return;
            }

            bool aOk = socketA != null && socketA.PlacedItem != null
                       && socketA.PlacedItem.itemId == correctItemIdA;
            bool bOk = socketB != null && socketB.PlacedItem != null
                       && socketB.PlacedItem.itemId == correctItemIdB;

            if (aOk && bOk)
            {
                OnSuccess();
            }
            else
            {
                OnFail();
            }
        }

        void OnSuccess()
        {
            ScoreManager.Instance?.AddScore(2);
            GameManager.Instance?.SolvePuzzle();

            if (buttonRenderer != null && successMaterial != null)
            {
                buttonRenderer.material = successMaterial;
            }

            MessagePanel.Instance?.Show(
                "<b>★ SUCCESS ★</b>\n\nCorrect flow established. The relay hums to life.\n" +
                "The lights stabilize. Something, somewhere, finally works.");
        }

        void OnFail()
        {
            string msg = FailMessages[failCount % FailMessages.Length];
            failCount++;
            MessagePanel.Instance?.Show(msg);
        }
    }
}
