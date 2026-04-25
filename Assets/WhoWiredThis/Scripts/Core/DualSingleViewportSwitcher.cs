using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Core
{
    public class DualSingleViewportSwitcher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraViewportPresetApplier firstCameraApplier;
        [SerializeField] private CameraViewportPresetApplier secondCameraApplier;

        [Header("Dual Layout")]
        [SerializeField]
        private ViewportPreset firstDualPreset =
            ViewportPreset.Left_Half_Display1;
        [SerializeField]
        private ViewportPreset secondDualPreset =
            ViewportPreset.Right_Half_Display1;

        [Header("Single Layout")]
        [SerializeField]
        private ViewportPreset firstSinglePreset =
            ViewportPreset.Full_Display1;
        [SerializeField]
        private ViewportPreset secondSinglePreset =
            ViewportPreset.Full_Display2;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.P;
        [SerializeField] private bool startInDualMode = true;

        private bool isDualMode;

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
            ApplyCurrentLayout();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleLayout();
            }
        }

        [ContextMenu("Toggle Layout")]
        public void ToggleLayout()
        {
            isDualMode = !isDualMode;
            ApplyCurrentLayout();
        }

        private void ApplyCurrentLayout()
        {
            if (isDualMode)
            {
                if (firstCameraApplier != null)
                {
                    firstCameraApplier.SetPreset(firstDualPreset);
                }

                if (secondCameraApplier != null)
                {
                    secondCameraApplier.SetPreset(secondDualPreset);
                }
            }
            else
            {
                if (firstCameraApplier != null)
                {
                    firstCameraApplier.SetPreset(firstSinglePreset);
                }

                if (secondCameraApplier != null)
                {
                    secondCameraApplier.SetPreset(secondSinglePreset);
                }
            }
        }
    }
}
