using System;
using System.Collections.Generic;
using UnityEngine;
using WhoWiredThis.Data;

namespace WhoWiredThis.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public const int MaxSlots = 3;

        private readonly List<ItemData> items = new List<ItemData>();
        public IReadOnlyList<ItemData> Items => items;

        private int selectedIndex = -1;
        public int SelectedIndex => selectedIndex;
        public ItemData SelectedItem =>
            selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;

        public event Action OnInventoryChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public bool TryAddItem(ItemData item)
        {
            if (items.Count >= MaxSlots)
            {
                return false;
            }

            if (HasItem(item))
            {
                return false;
            }

            items.Add(item);

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveItem(ItemData item)
        {
            bool removed = items.Remove(item);

            if (removed)
            {
                selectedIndex = items.Count > 0 ? 0 : -1;
                OnInventoryChanged?.Invoke();
            }

            return removed;
        }

        public bool HasItem(ItemData item) => items.Contains(item);

        public void SelectIndex(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return;
            }

            selectedIndex = index;
            OnInventoryChanged?.Invoke();
        }
    }
}
