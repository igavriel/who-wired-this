using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class ClueInteractable : MonoBehaviour, IInteractable
    {
        [TextArea(3, 6)]
        public string clueText = "More power is not the same as correct flow.";

        public string GetPromptText() => "$INTERACT$ Examine note";

        public void Interact(GameObject interactor)
        {
            PlayerHudPopupRouter.Show(interactor, $"<i>\"{clueText}\"</i>");
        }
    }
}
