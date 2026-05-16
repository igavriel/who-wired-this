using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Core;
using WhoWiredThis.Data;
using WhoWiredThis.UI;

namespace WhoWiredThis.Interactables
{
    public class PuzzleSocket : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        public string socketLabel = "Socket A";

        [Header("Visuals")]
        public Renderer socketIndicator;
        public Material emptyMaterial;
        public Material filledMaterial;

        public ItemData PlacedItem { get; private set; }

        public string GetPromptText()
        {
            if (PlacedItem != null)
            {
                return $"$INTERACT$ Remove {PlacedItem.itemName} from {socketLabel}";
            }

            ItemData sel = InventoryManager.Instance?.SelectedItem;
            return sel != null
                ? $"$INTERACT$ Place {sel.itemName} in {socketLabel}"
                : $"$INTERACT$ {socketLabel} — select an item first (1/2/3)";
        }

        public void Interact(GameObject interactor)
        {
            InventoryManager inv = InventoryManager.Instance;

            if (PlacedItem != null)
            {
                ItemData itemToReturn = PlacedItem;

                if (inv.TryAddItem(itemToReturn))
                {
                    SetPlacedItem(null);
                    PlayerHudPopupRouter.Show(interactor, $"Removed <b>{itemToReturn.itemName}</b> from {socketLabel}.");
                }
                else
                {
                    PlayerHudPopupRouter.Show(interactor, "Bag is full — can't retrieve item.");
                }
            }
            else
            {
                ItemData selected = inv?.SelectedItem;

                if (selected == null)
                {
                    PlayerHudPopupRouter.Show(interactor, "Select an item first (keys 1, 2, or 3).");
                    return;
                }

                inv.TryRemoveItem(selected);
                SetPlacedItem(selected);
                PlayerHudPopupRouter.Show(interactor, $"Placed <b>{selected.itemName}</b> in {socketLabel}.");
            }
        }

        void SetPlacedItem(ItemData item)
        {
            PlacedItem = item;

            if (socketIndicator != null)
            {
                socketIndicator.material = item != null ? filledMaterial : emptyMaterial;
            }
        }
    }
}
