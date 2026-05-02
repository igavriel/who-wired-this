using TMPro;
using UnityEngine;
using WhoWiredThis.Interfaces;

namespace WhoWiredThis.Tutorial2
{
    public class PuzzleOptionButtonController : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleStationController station;
        [SerializeField] private int optionIndex;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer targetRenderer;

        private PuzzleValueSetSO.PuzzleValueDefinition configuredValue;
        private bool interactable = true;

        public void Configure(
            PuzzleStationController ownerStation,
            PuzzleValueSetSO.PuzzleValueDefinition value,
            int index)
        {
            station = ownerStation;
            configuredValue = value;
            optionIndex = index;

            if (label != null)
            {
                label.text = value != null ? value.ShortLabel : "?";
                label.color = value != null ? value.DisplayColor : Color.white;
            }

            if (targetRenderer != null && value != null)
            {
                targetRenderer.material.SetColor("_BaseColor", value.DisplayColor);
            }
        }

        public void SetInteractable(bool canInteract)
        {
            interactable = canInteract;
        }

        public string GetPromptText()
        {
            if (!interactable || configuredValue == null)
            {
                return "Station input disabled.";
            }

            return $"$INTERACT$ Select {configuredValue.DisplayLabel}";
        }

        public void Interact(GameObject interactor)
        {
            if (!interactable || station == null || configuredValue == null)
            {
                return;
            }

            station.OnOptionSelected(optionIndex, configuredValue.Id, interactor);
        }
    }
}
