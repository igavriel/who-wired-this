using System.Text.RegularExpressions;
using UnityEngine;

namespace WhoWiredThis.UI
{
    [CreateAssetMenu(
        fileName = "YoutubeConfig",
        menuName = "Who Wired This/YouTube Config")]
    public class YoutubeConfigSO : ScriptableObject
    {
        [Tooltip("Full YouTube URL or raw video id.")]
        [SerializeField]
        private string youtubeUrlOrVideoId = "https://www.youtube.com/watch?v=K6HYGICvEaU";

        [SerializeField]
        private bool playOnAwake = true;

        [SerializeField]
        private bool loop = true;

        [Tooltip(
            "Start muted. Prefer false so Display 1 audio is heard; some browsers may still " +
            "block unmuted autoplay.")]
        [SerializeField]
        private bool mute = false;

        [Tooltip("Hide YouTube player chrome where possible.")]
        [SerializeField]
        private bool hideControls = true;

        [Range(0.2f, 1f)]
        [Tooltip("Max fraction of the shorter viewport side used for the 16:9 box.")]
        [SerializeField]
        private float viewportFit = 0.8f;

        [Tooltip(
            "HTTPS origin used as Referer. Must resolve in DNS " +
            "(macOS WKWebView may fetch it). Avoids YouTube Error 153.")]
        [SerializeField]
        private string embedBaseUrl = "https://www.google.com/";

        public string YoutubeUrlOrVideoId => youtubeUrlOrVideoId;
        public bool PlayOnAwake => playOnAwake;
        public bool Loop => loop;
        public bool Mute => mute;
        public bool HideControls => hideControls;
        public float ViewportFit => viewportFit;
        public string EmbedBaseUrl
        {
            get
            {
                string configured = string.IsNullOrWhiteSpace(embedBaseUrl)
                    ? "https://www.google.com/"
                    : embedBaseUrl.Trim();
                if (!configured.EndsWith("/", System.StringComparison.Ordinal))
                {
                    configured += "/";
                }

                return configured;
            }
        }

        public bool TryBuildEmbedUrl(out string embedUrl)
        {
            embedUrl = null;
            if (!TryExtractVideoId(youtubeUrlOrVideoId, out string videoId))
            {
                return false;
            }

            string origin = EmbedBaseUrl.TrimEnd('/');
            var sb = new System.Text.StringBuilder();
            sb.Append("https://www.youtube.com/embed/");
            sb.Append(videoId);
            sb.Append("?autoplay=1&rel=0&modestbranding=1&playsinline=1&enablejsapi=1");
            sb.Append("&origin=");
            sb.Append(UnityEngine.Networking.UnityWebRequest.EscapeURL(origin));
            sb.Append("&widget_referrer=");
            sb.Append(UnityEngine.Networking.UnityWebRequest.EscapeURL(origin));
            if (loop)
            {
                sb.Append("&loop=1&playlist=");
                sb.Append(videoId);
            }

            if (mute)
            {
                sb.Append("&mute=1");
            }

            if (hideControls)
            {
                sb.Append("&controls=0");
            }

            embedUrl = sb.ToString();
            return true;
        }

        public static bool TryExtractVideoId(string urlOrId, out string videoId)
        {
            videoId = null;
            if (string.IsNullOrWhiteSpace(urlOrId))
            {
                return false;
            }

            string trimmed = urlOrId.Trim();
            if (Regex.IsMatch(trimmed, @"^[A-Za-z0-9_-]{6,}$") &&
                !trimmed.Contains('/') &&
                !trimmed.Contains('.'))
            {
                videoId = trimmed;
                return true;
            }

            Match match = Regex.Match(
                trimmed,
                @"(?:youtube\.com\/(?:watch\?.*?v=|embed\/|shorts\/)|youtu\.be\/)([A-Za-z0-9_-]{6,})",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            videoId = match.Groups[1].Value;
            return !string.IsNullOrEmpty(videoId);
        }
    }
}
