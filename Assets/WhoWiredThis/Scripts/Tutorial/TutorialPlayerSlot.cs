using UnityEngine;

namespace WhoWiredThis.Tutorial
{
    public enum TutorialPlayerSlot
    {
        PlayerA = 0,
        PlayerB = 1
    }

    public class TutorialPlayerSlotComponent : MonoBehaviour
    {
        [SerializeField] private TutorialPlayerSlot slot = TutorialPlayerSlot.PlayerA;

        public TutorialPlayerSlot Slot => slot;

        public void SetSlot(TutorialPlayerSlot value)
        {
            slot = value;
        }
    }
}
