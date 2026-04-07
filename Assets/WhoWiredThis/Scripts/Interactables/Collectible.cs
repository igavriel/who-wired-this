using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.Data;
using WhoWiredThis.Inventory;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class Collectible : MonoBehaviour, IInteractable
    {
        public ItemData itemData;
        public int scoreValue = 1;

        public string GetPromptText() =>
            itemData != null ? $"[E] Pick up {itemData.itemName}" : "[E] Pick up";

        public void Interact(GameObject interactor)
        {
            if (itemData == null)
            {
                return;
            }

            if (InventoryManager.Instance.TryAddItem(itemData))
            {
                ScoreManager.Instance?.AddScore(scoreValue);
                MessagePanel.Instance?.Show($"Picked up: <b>{itemData.itemName}</b>\n{itemData.description}");
                gameObject.SetActive(false);
            }
            else
            {
                MessagePanel.Instance?.Show("Bag is full. Drop something or use an item first.");
            }
        }
    }
}
