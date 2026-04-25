using System.Collections;
using UnityEngine;

namespace WhoWiredThis.Core
{
    public class ActivateDisplays : MonoBehaviour
    {

        [Header("Configuration")]
        [Tooltip("If enabled, the minimum shared resolution will be applied to all displays.")]
        [SerializeField] private bool setMinimumSharedResolution = false;

        private static int s_LastActivatedDisplayCount = 1;

        private void Start()
        {
            Debug.Log($"[ActivateDisplays] Display.displays.Length: {Display.displays.Length}");

            // Ignore if we've already activated this display range.
            if (Display.displays.Length <= s_LastActivatedDisplayCount)
            {
                Debug.Log($"[ActivateDisplays] Already activated display range {s_LastActivatedDisplayCount} to {Display.displays.Length - 1}");
                return;
            }

            for (int i = s_LastActivatedDisplayCount; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
                Debug.Log($"[ActivateDisplays] Display {i} activated");
            }

            s_LastActivatedDisplayCount = Display.displays.Length;
            if (setMinimumSharedResolution)
            {
                StartCoroutine(ApplyMinimumSharedResolutionNextFrame());
            }
        }

        private static IEnumerator ApplyMinimumSharedResolutionNextFrame()
        {
            Debug.Log("[ActivateDisplays] Applying minimum shared resolution");
            // Wait one frame so rendering sizes are initialized after activation.
            yield return null;

            if (Display.displays.Length == 0)
            {
                Debug.LogWarning("[ActivateDisplays] No displays found");
                yield break;
            }

            int minWidth = int.MaxValue;
            int minHeight = int.MaxValue;

            for (int i = 0; i < Display.displays.Length; i++)
            {
                Display display = Display.displays[i];
                int width = display.systemWidth;
                int height = display.systemHeight;

                Debug.Log(
                    $"[ActivateDisplays] Display {i}: " +
                    $"system={display.systemWidth}x{display.systemHeight}, " +
                    $"rendering={display.renderingWidth}x{display.renderingHeight}");

                if (width > 0 && height > 0)
                {
                    minWidth = Mathf.Min(minWidth, width);
                    minHeight = Mathf.Min(minHeight, height);
                }
            }

            if (minWidth == int.MaxValue || minHeight == int.MaxValue)
            {
                Debug.LogWarning("[ActivateDisplays] Could not determine minimum display resolution.");
                yield break;
            }

            for (int i = 0; i < Display.displays.Length; i++)
            {
                Display.displays[i].SetRenderingResolution(minWidth, minHeight);
                Debug.Log($"[ActivateDisplays] Display {i} SetRenderingResolution({minWidth}, {minHeight})");
            }
        }
    }
}
