using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Core
{
    public class DualSingleViewportSwitcher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The applier for the first camera")]
        private CameraViewportPresetApplier firstCameraApplier;
        [SerializeField]
        [Tooltip("The applier for the second camera")]
        private CameraViewportPresetApplier secondCameraApplier;

        [Header("Dual Layout")]
        [SerializeField]
        [Tooltip("The preset for the first camera in dual mode")]
        private ViewportPreset firstDualPreset = ViewportPreset.Left_Half_Display1;
        [SerializeField]
        [Tooltip("The preset for the second camera in dual mode")]
        private ViewportPreset secondDualPreset = ViewportPreset.Right_Half_Display1;

        [Header("Single Layout")]
        [SerializeField]
        [Tooltip("The preset for the first camera in single mode")]
        private ViewportPreset firstSinglePreset = ViewportPreset.Full_Display1;
        [SerializeField]
        [Tooltip("The preset for the second camera in single mode")]
        private ViewportPreset secondSinglePreset = ViewportPreset.Full_Display2;

        [Header("Input")]
        [SerializeField]
        [Tooltip("Toggle between dual and single mode")]
        private KeyCode toggleKey = KeyCode.P;
        [SerializeField]
        [Tooltip("Toggle between displaying on Display 1 and Display 2")]
        private KeyCode toggleSwitchKey = KeyCode.O;
        [SerializeField]
        [Tooltip("Start in dual mode")]
        private bool startInDualMode = true;

        private bool isDualMode;
        private bool displaysSwapped;

        private void Awake()
        {
            if (firstCameraApplier == null)
            {
                Debug.LogWarning("[DualSingleViewportSwitcher] First CameraViewportPresetApplier is not assigned.", this);
            }

            if (secondCameraApplier == null)
            {
                Debug.LogWarning("[DualSingleViewportSwitcher] Second CameraViewportPresetApplier is not assigned.", this);
            }
        }

        private void OnEnable()
        {
            isDualMode = startInDualMode;
            displaysSwapped = false;
            ApplyCurrentLayout();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleLayout();
            }

            if (Input.GetKeyDown(toggleSwitchKey))
            {
                ToggleDisplaySwap();
            }
        }

        [ContextMenu("Toggle Layout")]
        public void ToggleLayout()
        {
            isDualMode = !isDualMode;
            ApplyCurrentLayout();
        }

        [ContextMenu("Toggle Display Swap")]
        public void ToggleDisplaySwap()
        {
            displaysSwapped = !displaysSwapped;
            ApplyCurrentLayout();
        }

        private void ApplyCurrentLayout()
        {
            if (isDualMode)
            {
                ViewportPreset firstPreset = displaysSwapped
                    ? ViewportPreset.Left_Half_Display2
                    : firstDualPreset;
                ViewportPreset secondPreset = displaysSwapped
                    ? ViewportPreset.Right_Half_Display1
                    : secondDualPreset;

                if (firstCameraApplier != null)
                {
                    firstCameraApplier.SetPreset(firstPreset);
                }

                if (secondCameraApplier != null)
                {
                    secondCameraApplier.SetPreset(secondPreset);
                }
            }
            else
            {
                ViewportPreset firstPreset = displaysSwapped
                    ? ViewportPreset.Full_Display2
                    : firstSinglePreset;
                ViewportPreset secondPreset = displaysSwapped
                    ? ViewportPreset.Full_Display1
                    : secondSinglePreset;

                if (firstCameraApplier != null)
                {
                    firstCameraApplier.SetPreset(firstPreset);
                }

                if (secondCameraApplier != null)
                {
                    secondCameraApplier.SetPreset(secondPreset);
                }
            }
        }
    }
}
