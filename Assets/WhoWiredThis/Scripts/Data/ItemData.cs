using UnityEngine;

namespace WhoWiredThis.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "WhoWiredThis/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        [TextArea] public string description;
        // Stable ID used by puzzle validation — must match TestButton's correctItemA / correctItemB
        public string itemId;
    }
}
