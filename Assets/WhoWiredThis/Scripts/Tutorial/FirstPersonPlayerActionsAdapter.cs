using UnityEngine;
using WhoWiredThis.Player;

namespace WhoWiredThis.Tutorial
{
    [DisallowMultipleComponent]
    public class FirstPersonPlayerActionsAdapter : MonoBehaviour
    {
        [SerializeField] private TutorialPlayerSlot fallbackSlot = TutorialPlayerSlot.PlayerA;

        private void Awake()
        {
            EnsureSlotComponent();
            EnsureInputBridge();
            EnsurePlayerActions();
        }

        private void EnsureSlotComponent()
        {
            TutorialPlayerSlotComponent slotComponent = GetComponent<TutorialPlayerSlotComponent>();
            if (slotComponent == null)
            {
                slotComponent = gameObject.AddComponent<TutorialPlayerSlotComponent>();
            }

            if (CompareTag("PlayerA"))
            {
                slotComponent.SetSlot(TutorialPlayerSlot.PlayerA);
            }
            else if (CompareTag("PlayerB"))
            {
                slotComponent.SetSlot(TutorialPlayerSlot.PlayerB);
            }
            else
            {
                slotComponent.SetSlot(fallbackSlot);
            }
        }

        private void EnsureInputBridge()
        {
            if (GetComponent<PlayerInputBridge>() == null)
            {
                gameObject.AddComponent<PlayerInputBridge>();
            }
        }

        private void EnsurePlayerActions()
        {
            if (GetComponent<PlayerActions>() == null)
            {
                gameObject.AddComponent<PlayerActions>();
            }
        }
    }
}
