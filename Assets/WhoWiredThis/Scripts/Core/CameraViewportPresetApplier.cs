using UnityEngine;
using UnityEngine.Assertions;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Core
{
    public class CameraViewportPresetApplier : MonoBehaviour
    {

        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Configuration")]
        [SerializeField] private ViewportPreset preset = ViewportPreset.Full_Display1;
        private ViewportPreset lastAppliedPreset;
        private bool hasAppliedPreset;

        private void Awake()
        {
            Assert.IsNotNull(targetCamera, "[CameraViewportPresetApplier] requires a target Camera.");
        }

        private void OnEnable()
        {
            ApplyPreset(force: true);
        }

        private void OnValidate()
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[CameraViewportPresetApplier] Target camera is null, skipping preset application");
                return;
            }

            ApplyPreset(force: true);  // Force application to ensure proper initialization
        }

        [ContextMenu("Apply Preset")]
        public void ApplyPreset(bool force)
        {
            if (!force && hasAppliedPreset && lastAppliedPreset == preset)
            {
                return;
            }

            switch (preset)
            {
                case ViewportPreset.Left_Half_Display1:
                    targetCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.Right_Half_Display1:
                    targetCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.Full_Display1:
                    targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.Full_Display2:
                    targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    targetCamera.targetDisplay = 1; // Display 2
                    break;
            }

            lastAppliedPreset = preset;
            hasAppliedPreset = true;
            Debug.Log($"[CameraViewportPresetApplier] Applied preset {preset}");
        }

        public void SetPreset(ViewportPreset nextPreset)
        {
            if (preset == nextPreset)
            {
                return;
            }

            Debug.Log($"[CameraViewportPresetApplier] Setting preset to {nextPreset} from {preset}");
            preset = nextPreset;
            ApplyPreset(force: false);  // Don't force application to avoid infinite recursion
        }
    }
}
