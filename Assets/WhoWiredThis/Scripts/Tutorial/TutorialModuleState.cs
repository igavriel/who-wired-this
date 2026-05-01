using System;
using UnityEngine;
using WhoWiredThis.Interfaces;

namespace WhoWiredThis.Tutorial
{
    public class TutorialModuleState : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string moduleId = "A1";

        [Header("State")]
        [SerializeField] private int currentState;
        [SerializeField] private int maxState = 2;

        [Header("Visuals")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material[] stateMaterials = Array.Empty<Material>();
        [SerializeField] private GameObject[] stateObjects = Array.Empty<GameObject>();

        [Header("Access")]
        [SerializeField] private TutorialModuleAccessGate accessGate;

        public event Action<TutorialModuleState> StateChanged;

        public string ModuleId => moduleId;
        public int CurrentState => currentState;

        private void Awake()
        {
            ApplyVisualState();
        }

        public void Configure(string id, TutorialPlayerSlot allowedSlot, int initialState = 0)
        {
            moduleId = id;
            if (accessGate != null)
            {
                accessGate.SetAllowedSlot(allowedSlot);
            }

            currentState = Mathf.Clamp(initialState, 0, maxState);
            ApplyVisualState();
        }

        public string GetPromptText()
        {
            return $"$INTERACT$ Set {moduleId}: [{currentState}]";
        }

        public void Interact(GameObject interactor)
        {
            if (accessGate != null && !accessGate.CanInteract(interactor))
            {
                return;
            }

            currentState = (currentState + 1) % (maxState + 1);
            ApplyVisualState();
            StateChanged?.Invoke(this);
        }

        private void ApplyVisualState()
        {
            if (targetRenderer != null && currentState >= 0 && currentState < stateMaterials.Length)
            {
                Material material = stateMaterials[currentState];
                if (material != null)
                {
                    targetRenderer.sharedMaterial = material;
                }
            }

            for (int i = 0; i < stateObjects.Length; i++)
            {
                if (stateObjects[i] != null)
                {
                    stateObjects[i].SetActive(i == currentState);
                }
            }
        }
    }
}
