using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Util;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Lives on the physical Solve mesh. Forwards <see cref="IInteractable"/> to a
    /// <see cref="MultiDimensionPuzzleInteractableBridge"/> (or other interactable) on the PuzzleManager so raycasts still hit the button.
    /// </summary>
    public class SolveInteractProxy : MonoBehaviour, IInteractable
    {
        [Tooltip("Typically MultiDimensionPuzzleInteractableBridge on the panel PuzzleManager.")]
        [RequireInterface(typeof(IInteractable))]
        [SerializeField]
        private MonoBehaviour bridgeReference;

        [Tooltip("When locked, Interact is ignored. Leave empty to use a PanelActionLock on a panel ancestor.")]
        [SerializeField]
        private PanelActionLock panelActionLock;

        private IInteractable Bridge => bridgeReference as IInteractable;

        public string GetPromptText()
        {
            return Bridge != null ? Bridge.GetPromptText() : string.Empty;
        }

        public void Interact(GameObject interactor)
        {
            if (Bridge == null)
            {
                Debug.LogWarning($"[SolveInteractProxy] '{name}' has no valid bridgeReference.", this);
                return;
            }

            PanelActionLock gate = PanelActionLock.Resolve(this, panelActionLock);
            if (gate != null && gate.IsLocked)
            {
                return;
            }

            Bridge.Interact(interactor);
        }
    }
}
