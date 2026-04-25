using UnityEngine;
using UnityEngine.Assertions;

namespace WhoWiredThis.Core
{
    public class CameraViewportPresetApplier : MonoBehaviour
    {
        public enum ViewportPreset
        {
            LeftHalfDisplay1,
            RightHalfDisplay1,
            FullDisplay1,
            FullDisplay2
        }

        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Configuration")]
        [SerializeField] private ViewportPreset preset = ViewportPreset.FullDisplay1;

        private void Awake()
        {
            Assert.IsNotNull(targetCamera, "CameraViewportPresetApplier requires a target Camera.");
        }

        private void OnEnable()
        {
            ApplyPreset();
        }

        private void OnValidate()
        {
            if (targetCamera == null)
            {
                return;
            }

            ApplyPreset();
        }

        [ContextMenu("Apply Preset")]
        public void ApplyPreset()
        {
            switch (preset)
            {
                case ViewportPreset.LeftHalfDisplay1:
                    targetCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.RightHalfDisplay1:
                    targetCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.FullDisplay1:
                    targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    targetCamera.targetDisplay = 0; // Display 1
                    break;

                case ViewportPreset.FullDisplay2:
                    targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    targetCamera.targetDisplay = 1; // Display 2
                    break;
            }
        }

        public void SetPreset(ViewportPreset nextPreset)
        {
            preset = nextPreset;
            ApplyPreset();
        }
    }
}
