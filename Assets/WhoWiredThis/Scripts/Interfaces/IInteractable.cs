using UnityEngine;

namespace WhoWiredThis.Interfaces
{
    public interface IInteractable
    {
        string GetPromptText();
        void Interact(GameObject interactor);
    }
}
