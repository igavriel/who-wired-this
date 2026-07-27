using System.Reflection;
using Gree.UnityWebView;
using UnityEngine;
using UnityEngine.UI;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// One WebView on Display 1 (video + audio). Canvas B mirrors the WebView bitmap
    /// so Display 2 sees the same picture without a second player / second audio stream.
    /// </summary>
    [DisallowMultipleComponent]
    public class YoutubeWebViewController : MonoBehaviour
    {
        private const string LogPrefix = "[YoutubeWebView]";
        private const float Aspect = 16f / 9f;
        private const string EndedMessage = "ended";

        private static readonly FieldInfo WebViewTextureField = typeof(WebViewObject).GetField(
            "texture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField]
        private YoutubeConfigSO config;

        [SerializeField]
        private RectTransform canvasAAnchor;

        [SerializeField]
        private RectTransform canvasBAnchor;

        [SerializeField]
        private Camera canvasACamera;

        [Tooltip("RawImage under Canvas B (Display 2). Mirrors Display 1 WebView texture.")]
        [SerializeField]
        private RawImage display2Mirror;

        [Tooltip("Flip Display 2 RawImage UVs (macOS WebView bitmap is often upside-down).")]
        [SerializeField]
        private bool flipDisplay2UvY = true;

        private WebViewObject webViewA;
        private bool started;
        private bool endedSignaled;

        /// <summary>Raised once when the trailer reaches its natural end (not on StopPlayback).</summary>
        public event System.Action PlaybackEnded;

        public bool IsPlaying => started && webViewA != null;

        private void Awake()
        {
            SetVideoSurfacesVisible(false);
        }

        private void OnEnable()
        {
            // When a StartSceneTrailerSequence is present it owns timing; do not auto-play.
            if (config != null &&
                config.PlayOnAwake &&
                GetComponent<StartSceneTrailerSequence>() == null)
            {
                StartCoroutine(StartPlaybackNextFrame());
            }
            else if (!started)
            {
                SetVideoSurfacesVisible(false);
            }
        }

        private System.Collections.IEnumerator StartPlaybackNextFrame()
        {
            yield return null;
            if (!isActiveAndEnabled || config == null || !config.PlayOnAwake)
            {
                yield break;
            }

            StartPlayback();
        }

        private void OnDisable()
        {
            StopAndDestroyWebViews();
        }

        private void OnDestroy()
        {
            StopAndDestroyWebViews();
        }

        private void LateUpdate()
        {
            if (!started)
            {
                return;
            }

            ApplyMargins(webViewA, canvasAAnchor, canvasACamera);
            UpdateDisplay2Mirror();
        }

        [ContextMenu("Start Playback")]
        public void StartPlayback()
        {
            if (config == null)
            {
                Debug.LogWarning($"{LogPrefix} config is not assigned.", this);
                return;
            }

            if (!config.TryBuildEmbedUrl(out string embedUrl))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Could not parse YouTube URL/id: '{config.YoutubeUrlOrVideoId}'.",
                    this);
                return;
            }

            // Single WebView on Display 1 only (audio source). Display 2 gets a texture mirror.
            // Keep WebView under this object (no DontDestroyOnLoad) so leaving StartScene
            // tears down video + audio with the scene.
            string referer = config.EmbedBaseUrl;
            DestroyLeftoverNamedWebViews();
            EnsureWebView(ref webViewA, "YouTubeWebView-A");

            SetVideoSurfacesVisible(true);
            SizeAnchorsToFit();
            ApplyMargins(webViewA, canvasAAnchor, canvasACamera);
            ConfigureDisplay2Mirror();

            endedSignaled = false;
            if (webViewA != null)
            {
                ApplyYoutubeRefererHeaders(webViewA, referer);
                webViewA.LoadURL(embedUrl);
                webViewA.SetVisibility(true);
                StartCoroutine(ArmEndedListenerWhenReady());
            }

            started = true;
            Debug.Log(
                $"{LogPrefix} Loading Display1 embed (Display2=mirror) Referer={referer} url={embedUrl}",
                this);
        }

        [ContextMenu("Stop Playback")]
        public void StopPlayback()
        {
            StopAndDestroyWebViews();
        }

        /// <summary>
        /// Show/hide YoutubeAnchor-16x9 (and Display 2 mirror) when playback is idle.
        /// </summary>
        private void SetVideoSurfacesVisible(bool visible)
        {
            if (canvasAAnchor != null)
            {
                canvasAAnchor.gameObject.SetActive(visible);
            }

            if (canvasBAnchor != null)
            {
                canvasBAnchor.gameObject.SetActive(visible);
            }

            if (display2Mirror != null)
            {
                display2Mirror.gameObject.SetActive(visible);
            }
        }

        private void ConfigureDisplay2Mirror()
        {
            if (display2Mirror == null)
            {
                return;
            }

            display2Mirror.enabled = true;
            display2Mirror.color = Color.white;
            display2Mirror.raycastTarget = false;
            if (flipDisplay2UvY)
            {
                display2Mirror.uvRect = new Rect(0f, 1f, 1f, -1f);
            }
            else
            {
                display2Mirror.uvRect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void UpdateDisplay2Mirror()
        {
            if (display2Mirror == null || webViewA == null || WebViewTextureField == null)
            {
                return;
            }

            var tex = WebViewTextureField.GetValue(webViewA) as Texture;
            if (tex != null && display2Mirror.texture != tex)
            {
                display2Mirror.texture = tex;
            }
        }

        private static void ApplyYoutubeRefererHeaders(WebViewObject webView, string baseUrl)
        {
            if (webView == null || string.IsNullOrWhiteSpace(baseUrl))
            {
                return;
            }

            string origin = baseUrl.TrimEnd('/');
            webView.AddCustomHeader("Referer", origin + "/");
        }

        private void SizeAnchorsToFit()
        {
            float fit = config != null ? Mathf.Clamp(config.ViewportFit, 0.2f, 1f) : 0.8f;
            SizeAnchor(canvasAAnchor, fit);
            SizeAnchor(canvasBAnchor, fit);
        }

        private static void SizeAnchor(RectTransform anchor, float fit)
        {
            if (anchor == null)
            {
                return;
            }

            RectTransform parent = anchor.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Rect parentRect = parent.rect;
            float maxW = parentRect.width * fit;
            float maxH = parentRect.height * fit;
            float width = maxW;
            float height = width / Aspect;
            if (height > maxH)
            {
                height = maxH;
                width = height * Aspect;
            }

            anchor.anchorMin = new Vector2(0.5f, 0.5f);
            anchor.anchorMax = new Vector2(0.5f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.anchoredPosition = Vector2.zero;
            anchor.sizeDelta = new Vector2(width, height);
        }

        private void EnsureWebView(ref WebViewObject webView, string objectName)
        {
            if (webView != null)
            {
                return;
            }

            GameObject go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            webView = go.AddComponent<WebViewObject>();
            webView.Init(
                cb: OnWebViewJsMessage,
                err: msg => Debug.LogWarning($"{LogPrefix} {objectName} error: {msg}", this),
                httpErr: msg => Debug.LogWarning($"{LogPrefix} {objectName} http error: {msg}", this),
                ld: null,
                started: null,
                hooked: null,
                cookies: null,
                transparent: false,
                zoom: false,
                ua: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
                radius: 0,
                androidForceDarkMode: 0,
                enableWKWebView: true);
        }

        private void OnWebViewJsMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (!string.Equals(message, EndedMessage, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (endedSignaled)
            {
                return;
            }

            endedSignaled = true;
            Debug.Log($"{LogPrefix} Trailer ended.", this);
            PlaybackEnded?.Invoke();
        }

        private System.Collections.IEnumerator ArmEndedListenerWhenReady()
        {
            // Video element appears after the embed loads.
            for (int attempt = 0; attempt < 20 && webViewA != null && started; attempt++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (webViewA == null || !started)
                {
                    yield break;
                }

                webViewA.EvaluateJS(
                    "(function(){" +
                    "function arm(){" +
                    "var v=document.querySelector('video');" +
                    "if(!v){return;}" +
                    "if(v._wwtEndedArmed){return;}" +
                    "v._wwtEndedArmed=true;" +
                    "v.addEventListener('ended',function(){try{Unity.call('ended');}catch(e){}});" +
                    "}" +
                    "arm();" +
                    "})();");
            }
        }

        private static void ApplyMargins(WebViewObject webView, RectTransform anchor, Camera canvasCamera)
        {
            if (webView == null || anchor == null)
            {
                return;
            }

            if (!TryGetScreenRect(anchor, canvasCamera, out Rect screenRect))
            {
                return;
            }

            int left = Mathf.RoundToInt(screenRect.xMin);
            int top = Mathf.RoundToInt(Screen.height - screenRect.yMax);
            int right = Mathf.RoundToInt(Screen.width - screenRect.xMax);
            int bottom = Mathf.RoundToInt(screenRect.yMin);

            left = Mathf.Max(0, left);
            top = Mathf.Max(0, top);
            right = Mathf.Max(0, right);
            bottom = Mathf.Max(0, bottom);

            webView.SetMargins(left, top, right, bottom);
        }

        private static bool TryGetScreenRect(RectTransform rectTransform, Camera canvasCamera, out Rect screenRect)
        {
            screenRect = default;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Camera cam = canvasCamera;
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cam = null;
            }
            else if (cam == null && canvas != null)
            {
                cam = canvas.worldCamera;
            }

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return screenRect.width > 1f && screenRect.height > 1f;
        }

        private void StopAndDestroyWebViews()
        {
            if (!started && webViewA == null)
            {
                DestroyLeftoverNamedWebViews();
                SetVideoSurfacesVisible(false);
                return;
            }

            started = false;
            if (display2Mirror != null)
            {
                display2Mirror.texture = null;
            }

            DestroyWebView(ref webViewA);
            SetVideoSurfacesVisible(false);
            DestroyLeftoverNamedWebViews();
        }

        private static void DestroyLeftoverNamedWebViews()
        {
            // Clean any older DontDestroyOnLoad leftovers from previous builds of this feature.
            DestroyByName("YouTubeWebView-A");
            DestroyByName("YouTubeWebView-B");
        }

        private static void DestroyByName(string objectName)
        {
            var leftover = GameObject.Find(objectName);
            if (leftover == null)
            {
                return;
            }

            var webView = leftover.GetComponent<WebViewObject>();
            if (webView != null)
            {
                StopNativePlayback(webView);
            }

            UnityEngine.Object.Destroy(leftover);
        }

        private static void DestroyWebView(ref WebViewObject webView)
        {
            if (webView == null)
            {
                return;
            }

            StopNativePlayback(webView);

            if (webView.gameObject != null)
            {
                UnityEngine.Object.Destroy(webView.gameObject);
            }

            webView = null;
        }

        /// <summary>
        /// Silence and blank the native WebView before Destroy so audio does not
        /// continue after StartScene unloads.
        /// </summary>
        private static void StopNativePlayback(WebViewObject webView)
        {
            if (webView == null)
            {
                return;
            }

            try
            {
                webView.EvaluateJS(
                    "(function(){try{var v=document.querySelector('video');" +
                    "if(v){v.pause();v.muted=true;v.volume=0;}" +
                    "window.postMessage(JSON.stringify({event:'command',func:'pauseVideo',args:[]}),'*');" +
                    "window.postMessage(JSON.stringify({event:'command',func:'mute',args:[]}),'*');" +
                    "}catch(e){}})();");
                webView.Pause();
                webView.SetVisibility(false);
                webView.LoadURL("about:blank");
            }
            catch (System.Exception)
            {
                // Native plugin may already be tearing down during scene unload.
            }
        }
    }
}
