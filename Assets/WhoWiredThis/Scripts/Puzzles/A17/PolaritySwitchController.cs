using UnityEngine;
using WhoWiredThis.Interactables;

namespace WhoWiredThis.Puzzles.A17
{
    public enum PolarityState
    {
        Negative = -1,
        Off = 0,
        Positive = 1
    }

    public class PolaritySwitchController : MonoBehaviour, IInteractable
    {
        [Header("State")]
        [SerializeField] private PolarityState currentState = PolarityState.Off;

        [Header("Visuals")]
        [SerializeField] private Renderer switchRenderer;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material positiveMaterial;

        public int SwitchValue => (int)currentState;
        public PolarityState CurrentState => currentState;

        void Awake()
        {
            if (switchRenderer == null)
                switchRenderer = GetComponent<Renderer>();
            ApplyMaterial();
        }

        public string GetPromptText()
        {
            string stateLabel = currentState switch
            {
                PolarityState.Negative => "[ - ]",
                PolarityState.Positive => "[ + ]",
                _ => "[ 0 ]"
            };
            return $"[E] Polarity: {stateLabel}";
        }

        public void Interact(GameObject interactor)
        {
            currentState = currentState switch
            {
                PolarityState.Negative => PolarityState.Off,
                PolarityState.Off => PolarityState.Positive,
                PolarityState.Positive => PolarityState.Negative,
                _ => PolarityState.Off
            };
            ApplyMaterial();
        }

        private void ApplyMaterial()
        {
            if (switchRenderer == null) return;

            Material mat = currentState switch
            {
                PolarityState.Negative => negativeMaterial,
                PolarityState.Positive => positiveMaterial,
                _ => offMaterial
            };

            if (mat != null)
            {
                switchRenderer.sharedMaterial = mat;
            }
        }
    }
}
