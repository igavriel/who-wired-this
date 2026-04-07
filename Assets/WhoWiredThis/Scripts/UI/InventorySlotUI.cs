using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhoWiredThis.Data;
using WhoWiredThis.Inventory;

namespace WhoWiredThis.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text itemNameText;
        public Image background;
        public Button slotButton;

        [Header("Settings")]
        public int slotIndex;
        public Color normalColor   = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        public Color selectedColor = new Color(0.2f,  0.55f, 0.2f,  0.95f);
        public Color emptyColor    = new Color(0.08f, 0.08f, 0.08f, 0.6f);

        void Start()
        {
            slotButton?.onClick.AddListener(() => InventoryManager.Instance?.SelectIndex(slotIndex));
        }

        public void SetSlot(ItemData item, bool selected)
        {
            if (itemNameText != null)
            {
                itemNameText.text = item != null
                    ? $"{slotIndex + 1}. {item.itemName}"
                    : $"{slotIndex + 1}. ---";
            }

            if (background != null)
            {
                background.color = item == null ? emptyColor : selected ? selectedColor : normalColor;
            }
        }
    }
}
