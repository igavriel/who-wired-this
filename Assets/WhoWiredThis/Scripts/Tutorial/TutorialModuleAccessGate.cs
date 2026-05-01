using UnityEngine;

namespace WhoWiredThis.Tutorial
{
    public class TutorialModuleAccessGate : MonoBehaviour
    {
        [SerializeField] private TutorialPlayerSlot allowedSlot = TutorialPlayerSlot.PlayerA;

        public TutorialPlayerSlot AllowedSlot => allowedSlot;

        public void SetAllowedSlot(TutorialPlayerSlot slot)
        {
            allowedSlot = slot;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            TutorialPlayerSlotComponent slotComponent =
                interactor.GetComponentInParent<TutorialPlayerSlotComponent>();
            if (slotComponent != null)
            {
                return slotComponent.Slot == allowedSlot;
            }

            string expectedTag = allowedSlot == TutorialPlayerSlot.PlayerA ? "PlayerA" : "PlayerB";
            return interactor.CompareTag(expectedTag) || interactor.transform.root.CompareTag(expectedTag);
        }
    }
}
