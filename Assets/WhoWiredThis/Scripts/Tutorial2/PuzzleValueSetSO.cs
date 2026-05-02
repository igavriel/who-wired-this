using System;
using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    [CreateAssetMenu(
        fileName = "PuzzleValueSet",
        menuName = "WhoWiredThis/Tutorial2/Puzzle Value Set")]
    public class PuzzleValueSetSO : ScriptableObject
    {
        [Serializable]
        public class PuzzleValueDefinition
        {
            [SerializeField] private string id = "value_id";
            [SerializeField] private string shortLabel = "?";
            [SerializeField] private string displayLabel = "Value";
            [SerializeField] private Color displayColor = Color.white;
            [SerializeField] private Sprite icon;

            public string Id => id;
            public string ShortLabel => shortLabel;
            public string DisplayLabel => displayLabel;
            public Color DisplayColor => displayColor;
            public Sprite Icon => icon;
        }

        [SerializeField] private string id = "value_set";
        [SerializeField] private string displayName = "Value Set";
        [SerializeField] private PuzzleValueDefinition[] values = Array.Empty<PuzzleValueDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public PuzzleValueDefinition[] Values => values;

        public bool TryGetById(string valueId, out PuzzleValueDefinition value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null && values[i].Id == valueId)
                {
                    value = values[i];
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
