using UnityEngine;

namespace WhoWiredThis.PanelFocus
{
    /// <summary>
    /// Logical gate for panel action interactions (inputs, Solve/Send). Place on the panel root;
    /// interactables can reference it explicitly or resolve it via <see cref="Resolve"/>.
    /// </summary>
    public class PanelActionLock : MonoBehaviour
    {
        [SerializeField]
        private bool locked;

        public bool IsLocked => locked;

        public void SetLocked(bool value)
        {
            locked = value;
        }

        /// <summary>Returns explicit reference if set; otherwise the nearest parent <see cref="PanelActionLock"/>.</summary>
        public static PanelActionLock Resolve(Component context, PanelActionLock explicitReference)
        {
            if (explicitReference != null)
            {
                return explicitReference;
            }

            return context != null ? context.GetComponentInParent<PanelActionLock>() : null;
        }
    }
}
