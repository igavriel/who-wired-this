using TMPro;
using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    public class SharedCoreController : MonoBehaviour
    {
        [SerializeField] private TMP_Text coreStatusText;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Color idleColor = Color.gray;
        [SerializeField] private Color sideCalibratedColor = Color.yellow;
        [SerializeField] private Color stabilizedColor = Color.green;

        private void Awake()
        {
            if (coreStatusText == null)
            {
                coreStatusText = GetComponentInChildren<TMP_Text>(true);
            }

            if (coreRenderer == null)
            {
                coreRenderer = GetComponent<Renderer>();
            }
        }

        public void SetPhaseStatus(string text)
        {
            SetStatus(text, idleColor);
        }

        public void SetSideCalibrated(string text)
        {
            SetStatus(text, sideCalibratedColor);
        }

        public void SetCoreStabilized(string text)
        {
            SetStatus(text, stabilizedColor);
        }

        private void SetStatus(string text, Color color)
        {
            if (coreStatusText != null)
            {
                coreStatusText.text = text;
            }

            if (coreRenderer != null)
            {
                coreRenderer.material.SetColor("_BaseColor", color);
            }
        }
    }
}
