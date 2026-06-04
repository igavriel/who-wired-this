using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Result-light-only visual: sine-pulses the active <see cref="MultiDimension"/> subject's
    /// child <see cref="Light"/> (default name IndicatorLight). Retargets when selection index changes.
    /// </summary>
    [RequireComponent(typeof(MultiDimension))]
    public class ResultLightIndicatorPulseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimension multiDimension;

        [Header("Indicator")]
        [SerializeField] private string indicatorChildName = "IndicatorLight";

        [Header("Pulse")]
        [SerializeField] private float minIntensity = 0.5f;
        [SerializeField] private float maxIntensity = 3f;
        [SerializeField] private float pulseSpeed = 2f;

        private Light activeIndicatorLight;
        private int lastSubjectIndex = -1;
        private bool warnedMissingIndicator;

        private void Awake()
        {
            if (multiDimension == null)
            {
                multiDimension = GetComponent<MultiDimension>();
            }
        }

        private void OnEnable()
        {
            lastSubjectIndex = -1;
            activeIndicatorLight = null;
            warnedMissingIndicator = false;
            RetargetIfNeeded(force: true);
        }

        private void Update()
        {
            if (multiDimension == null)
            {
                return;
            }

            if (!RetargetIfNeeded(force: false))
            {
                return;
            }

            if (activeIndicatorLight == null || !activeIndicatorLight.gameObject.activeInHierarchy)
            {
                return;
            }

            float normalized = 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed));
            activeIndicatorLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, normalized);
        }

        private bool RetargetIfNeeded(bool force)
        {
            int index = multiDimension.GetCurrentIndexForSolutionCheck();
            if (!force && index == lastSubjectIndex)
            {
                return activeIndicatorLight != null;
            }

            lastSubjectIndex = index;
            activeIndicatorLight = null;

            if (index < 0)
            {
                return false;
            }

            if (!multiDimension.TryGetSubjectRoot(index, out GameObject subjectRoot) || subjectRoot == null)
            {
                LogMissingIndicatorOnce($"subject index {index} has no root on '{name}'.");
                return false;
            }

            Transform indicatorTransform = subjectRoot.transform.Find(indicatorChildName);
            if (indicatorTransform == null)
            {
                LogMissingIndicatorOnce(
                    $"subject '{subjectRoot.name}' is missing child '{indicatorChildName}' on '{name}'.");
                return false;
            }

            activeIndicatorLight = indicatorTransform.GetComponent<Light>();
            if (activeIndicatorLight == null)
            {
                LogMissingIndicatorOnce(
                    $"child '{indicatorChildName}' under '{subjectRoot.name}' has no Light on '{name}'.");
                return false;
            }

            warnedMissingIndicator = false;
            return true;
        }

        private void LogMissingIndicatorOnce(string detail)
        {
            if (warnedMissingIndicator)
            {
                return;
            }

            warnedMissingIndicator = true;
            Debug.LogWarning($"[{nameof(ResultLightIndicatorPulseController)}] {detail}", this);
        }
    }
}
