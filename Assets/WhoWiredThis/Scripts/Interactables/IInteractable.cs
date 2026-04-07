using UnityEngine;

namespace WhoWiredThis.Interactables
{
    public interface IInteractable
    {
        string GetPromptText();
        void Interact(GameObject interactor);
    }
}
