using UnityEngine;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Tutorial2
{
    [RequireComponent(typeof(Collider))]
    public class OperatorPadController : MonoBehaviour
    {
        [SerializeField] private TutorialPlayerSlot assignedPlayer = TutorialPlayerSlot.PlayerA;
        [SerializeField] private CooperativeTutorialPuzzleManager manager;

        public TutorialPlayerSlot AssignedPlayer => assignedPlayer;

        private void OnTriggerEnter(Collider other)
        {
            ResolveManagerIfNeeded();
            if (manager == null || other == null)
            {
                return;
            }

            if (TryResolveSlot(other, out TutorialPlayerSlot slot))
            {
                manager.SetOperatorLink(slot, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            ResolveManagerIfNeeded();
            if (manager == null || other == null)
            {
                return;
            }

            if (TryResolveSlot(other, out TutorialPlayerSlot slot))
            {
                manager.SetOperatorLink(slot, false);
            }
        }

        private bool TryResolveSlot(Collider collider, out TutorialPlayerSlot slot)
        {
            TutorialPlayerSlotComponent slotComponent = collider.GetComponentInParent<TutorialPlayerSlotComponent>();
            if (slotComponent != null)
            {
                slot = slotComponent.Slot;
                return true;
            }

            if (collider.CompareTag("PlayerA") || collider.transform.root.CompareTag("PlayerA"))
            {
                slot = TutorialPlayerSlot.PlayerA;
                return true;
            }

            if (collider.CompareTag("PlayerB") || collider.transform.root.CompareTag("PlayerB"))
            {
                slot = TutorialPlayerSlot.PlayerB;
                return true;
            }

            slot = TutorialPlayerSlot.PlayerA;
            return false;
        }

        private void ResolveManagerIfNeeded()
        {
            if (manager != null)
            {
                return;
            }

            manager = GetComponentInParent<CooperativeTutorialPuzzleManager>();
        }
    }
}
