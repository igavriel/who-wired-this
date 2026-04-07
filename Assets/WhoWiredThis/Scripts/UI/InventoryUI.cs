using UnityEngine;
using WhoWiredThis.Inventory;

namespace WhoWiredThis.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Slots (assign in order: 0, 1, 2)")]
        public InventorySlotUI[] slots;

        void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            Refresh();
        }

        void Refresh()
        {
            InventoryManager inv = InventoryManager.Instance;
            for (int i = 0; i < slots.Length; i++)
            {
                bool hasItem = i < inv.Items.Count;
                bool selected = i == inv.SelectedIndex;
                slots[i].SetSlot(hasItem ? inv.Items[i] : null, selected);
            }
        }
    }
}
