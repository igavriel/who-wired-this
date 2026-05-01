using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.UI;

namespace WhoWiredThis.Tutorial
{
    public class TutorialClueBoardDisplay : MonoBehaviour, IInteractable
    {
        [TextArea(2, 6)]
        [SerializeField] private string clueText = "Partner clue is not configured.";

        public void SetClue(string value)
        {
            clueText = value;
        }

        public string GetPromptText()
        {
            return "$INTERACT$ Read clue board";
        }

        public void Interact(GameObject interactor)
        {
            MessagePanel.Instance?.Show(clueText);
        }
    }
}
