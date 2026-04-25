using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Core;
using WhoWiredThis.Data;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class Collectible : MonoBehaviour, IInteractable
    {
        public ItemData itemData;
        public int scoreValue = 1;

        public string GetPromptText() =>
            itemData != null ? $"$INTERACT$ Pick up {itemData.itemName}" : "$INTERACT$ Pick up";

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
