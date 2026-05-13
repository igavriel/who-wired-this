using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Short press-in / release animation for an Activate/Solve control. Inspector-wired only; no runtime UI spawn.
    /// Yield <see cref="PlayPressFeedbackRoutine"/> from <see cref="MultiDimensionPuzzleInteractableBridge"/> before processing.
    /// </summary>
    public class ActivateButtonFeedbackController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField]
        private Transform visualRoot;

        [Tooltip("Optional. When set, interactable is toggled only for the duration of this routine.")]
        [SerializeField]
        private Button uiButton;

        [Tooltip("Optional. Alpha pulse; used before highlightObject if both assigned.")]
        [SerializeField]
        private Graphic highlightGraphic;

        [Tooltip("Optional. Active pulse when highlightGraphic is not assigned.")]
        [SerializeField]
        private GameObject highlightObject;

        [Header("Audio")]
        [SerializeField]
        private AudioSource clickAudio;

        [SerializeField]
        private AudioClip clickClip;

        [Header("Timing")]
        [SerializeField]
        [Range(0.5f, 1f)]
        private float pressedScale = 0.92f;

        [SerializeField]
        [Min(0.01f)]
        private float pressDuration = 0.08f;

        [SerializeField]
        [Min(0.01f)]
        private float releaseDuration = 0.1f;

        [SerializeField]
        [Min(0.01f)]
        private float highlightFlashDuration = 0.15f;

        [SerializeField]
        private bool disableButtonWhileAnimating = true;

        [Header("Optional nudge (RectTransform)")]
        [SerializeField]
        private bool useAnchoredPositionOffset;

        [SerializeField]
        private Vector2 pressedAnchoredOffset;

        private Vector3 originalLocalScale = Vector3.one;
        private Vector2 originalAnchoredPosition;
        private bool cachedRect;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            originalLocalScale = visualRoot.localScale;
            if (visualRoot is RectTransform rect)
            {
                originalAnchoredPosition = rect.anchoredPosition;
                cachedRect = true;
            }
        }

        /// <summary>Press-in, optional flash, release; safe to yield from another behaviour's coroutine.</summary>
        public IEnumerator PlayPressFeedbackRoutine()
        {
            if (visualRoot == null)
            {
                Debug.LogWarning($"[ActivateButtonFeedbackController] '{name}' has no visualRoot; skipping press feedback.", this);
                yield break;
            }

            bool toggledUiButton = false;
            if (disableButtonWhileAnimating && uiButton != null)
            {
                uiButton.interactable = false;
                toggledUiButton = true;
            }

            if (clickAudio != null)
            {
                if (clickClip != null)
                {
                    clickAudio.PlayOneShot(clickClip);
                }
                else if (clickAudio.clip != null)
                {
                    clickAudio.Play();
                }
            }

            Vector3 pressedTargetScale = Vector3.Scale(originalLocalScale, new Vector3(pressedScale, pressedScale, pressedScale));
            yield return LerpLocalScale(pressedTargetScale, pressDuration);

            if (useAnchoredPositionOffset && cachedRect && visualRoot is RectTransform rectPress)
            {
                rectPress.anchoredPosition = originalAnchoredPosition + pressedAnchoredOffset;
            }

            yield return FlashHighlightRoutine();

            if (useAnchoredPositionOffset && cachedRect && visualRoot is RectTransform rectRelease)
            {
                rectRelease.anchoredPosition = originalAnchoredPosition;
            }

            yield return LerpLocalScale(originalLocalScale, releaseDuration);

            if (toggledUiButton && uiButton != null)
            {
                uiButton.interactable = true;
            }
        }

        private IEnumerator LerpLocalScale(Vector3 targetScale, float duration)
        {
            Vector3 start = visualRoot.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t);
                visualRoot.localScale = Vector3.LerpUnclamped(start, targetScale, smooth);
                yield return null;
            }

            visualRoot.localScale = targetScale;
        }

        private IEnumerator FlashHighlightRoutine()
        {
            if (highlightGraphic != null)
            {
                Color start = highlightGraphic.color;
                Color peak = start;
                peak.a = Mathf.Min(1f, peak.a + 0.35f);
                float half = highlightFlashDuration * 0.5f;
                yield return LerpGraphicColor(start, peak, half);
                yield return LerpGraphicColor(peak, start, half);
                highlightGraphic.color = start;
                yield break;
            }

            if (highlightObject != null)
            {
                bool wasActive = highlightObject.activeSelf;
                highlightObject.SetActive(true);
                float elapsed = 0f;
                while (elapsed < highlightFlashDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                highlightObject.SetActive(wasActive);
            }
        }

        private IEnumerator LerpGraphicColor(Color from, Color to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                highlightGraphic.color = Color.LerpUnclamped(from, to, t);
                yield return null;
            }

            highlightGraphic.color = to;
        }
    }
}
