using UnityEngine;
using WhoWiredThis.Interfaces;

namespace WhoWiredThis.Tutorial2
{
    public enum MachineActionType
    {
        Activate = 0,
        InitializeNextPhase = 1,
        Reset = 2
    }

    public class MachineActionButtonController : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleStationController station;
        [SerializeField] private MachineActionType actionType = MachineActionType.Activate;
        [SerializeField] private bool interactable = true;

        public void Configure(PuzzleStationController ownerStation, MachineActionType type)
        {
            station = ownerStation;
            actionType = type;
        }

        public void SetInteractable(bool canInteract)
        {
            interactable = canInteract;
        }

        public string GetPromptText()
        {
            if (!interactable)
            {
                return "Control locked.";
            }

            return actionType switch
            {
                MachineActionType.Activate => "$INTERACT$ Activate",
                MachineActionType.InitializeNextPhase => "$INTERACT$ Initialize",
                MachineActionType.Reset => "$INTERACT$ Reset tutorial",
                _ => "$INTERACT$ Use"
            };
        }

        public void Interact(GameObject interactor)
        {
            if (!interactable || station == null)
            {
                return;
            }

            switch (actionType)
            {
                case MachineActionType.Activate:
                    station.OnActivatePressed(interactor);
                    break;
                case MachineActionType.InitializeNextPhase:
                    station.OnInitializePressed(interactor);
                    break;
                case MachineActionType.Reset:
                    station.OnResetPressed(interactor);
                    break;
            }
        }
    }
}
