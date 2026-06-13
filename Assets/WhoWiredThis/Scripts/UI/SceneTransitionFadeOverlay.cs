using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// Full-screen fade overlay on a per-player HUD canvas. Starts transparent; used before scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneTransitionFadeOverlay : MonoBehaviour
    {
        private const string OverlayObjectName = "SceneTransitionFade";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fadeImage;

        private void Awake()
        {
            EnsureOverlay();
            SetAlpha(0f);
            SetBlocksRaycasts(false);
        }

        public void SetAlpha(float alpha)
        {
            EnsureOverlay();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        public void SetBlocksRaycasts(bool blocks)
        {
            EnsureOverlay();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = blocks;
                canvasGroup.interactable = blocks;
            }
        }

        public IEnumerator FadeOutRoutine(float durationSeconds)
        {
            EnsureOverlay();
            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                SetAlpha(1f);
                yield break;
            }

            SetBlocksRaycasts(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(elapsed / duration);
                yield return null;
            }

            SetAlpha(1f);
        }

        private void EnsureOverlay()
        {
            if (canvasGroup != null)
            {
                return;
            }

            RectTransform host = transform as RectTransform;
            if (host == null)
            {
                Debug.LogWarning($"[{nameof(SceneTransitionFadeOverlay)}] '{name}' requires a RectTransform.", this);
                return;
            }

            Transform existing = host.Find(OverlayObjectName);
            GameObject overlayObject;
            if (existing != null)
            {
                overlayObject = existing.gameObject;
            }
            else
            {
                overlayObject = new GameObject(OverlayObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                overlayObject.transform.SetParent(host, false);
                overlayObject.layer = host.gameObject.layer;

                RectTransform rect = overlayObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();

                Image image = overlayObject.GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;
                fadeImage = image;
            }

            canvasGroup = overlayObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayObject.AddComponent<CanvasGroup>();
            }

            if (fadeImage == null)
            {
                fadeImage = overlayObject.GetComponent<Image>();
            }
        }
    }
}
