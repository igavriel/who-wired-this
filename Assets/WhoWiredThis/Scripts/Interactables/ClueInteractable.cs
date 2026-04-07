using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class ClueInteractable : MonoBehaviour, IInteractable
    {
        [TextArea(3, 6)]
        public string clueText = "More power is not the same as correct flow.";
        public int scoreValue = 1;

        private bool hasBeenRead;

        public string GetPromptText() => "[E] Examine note";

        public void Interact(GameObject interactor)
        {
            MessagePanel.Instance?.Show($"<i>\"{clueText}\"</i>");

            if (!hasBeenRead)
            {
                hasBeenRead = true;
                ScoreManager.Instance?.AddScore(scoreValue);
            }
        }
    }
}
