using TMPro;
using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    public class PuzzleInputSlotController : MonoBehaviour
    {
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emptyColor = Color.gray;

        private string selectedValueId;

        public string SelectedValueId => selectedValueId;

        private void Awake()
        {
            Clear();
        }

        public void SetValue(PuzzleValueSetSO.PuzzleValueDefinition value)
        {
            selectedValueId = value != null ? value.Id : string.Empty;

            if (valueLabel != null)
            {
                valueLabel.text = value != null ? value.ShortLabel : "-";
                valueLabel.color = value != null ? value.DisplayColor : emptyColor;
            }

            if (targetRenderer != null)
            {
                Color targetColor = value != null ? value.DisplayColor : emptyColor;
                targetRenderer.material.SetColor("_BaseColor", targetColor);
            }
        }

        public void Clear()
        {
            selectedValueId = string.Empty;
            SetValue(null);
        }
    }
}
