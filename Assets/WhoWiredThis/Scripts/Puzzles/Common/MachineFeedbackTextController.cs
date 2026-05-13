using System.Collections;
using TMPro;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Prefab-local temporary copy in <c>Body_TMP</c> during Activate processing.
    /// Does not own the final diagnostic write; <see cref="DiagnosticDisplayController"/> does after processing ends.
    /// </summary>
    public class MachineFeedbackTextController : MonoBehaviour
    {
        private static readonly string[] DefaultProcessingSteps =
        {
            "READING SIGNAL...",
            "CHECKING SETTINGS...",
            "UPDATING HISTORY..."
        };

        private static readonly string[] DefaultFlavorMessages =
        {
            "THE MACHINE HUMS CONFIDENTLY.",
            "SOMETHING CLICKS INSIDE.",
            "A RELAY COUGHS.",
            "THE PANEL PRETENDS THIS IS NORMAL.",
            "SIGNAL ACCEPTED. PROBABLY.",
            "A TINY LIGHT BLINKS FOR NO REASON.",
            "THE MACHINE CONSIDERS YOUR LIFE CHOICES.",
            "INTERNAL GEARS MAKE AN UNHELPFUL NOISE."
        };

        [Header("Body")]
        [Tooltip("Same world-space TMP as DiagnosticDisplayController.bodyText (e.g. Body_TMP).")]
        [SerializeField]
        private TMP_Text bodyText;

        [Header("Copy")]
        [SerializeField]
        private string processingStatusPrefix = "STATUS: ";

        [SerializeField]
        private string[] processingSteps;

        [Tooltip("Random one-liner appended after unsuccessful solve attempts (not shown during processing).")]
        [SerializeField]
        private string[] flavorMessages;

        [Min(0.05f)]
        [SerializeField]
        private float stepDuration = 0.35f;

        /// <summary>True when <see cref="PlayBodyProcessingFeedback"/> can run (has body + at least one step).</summary>
        public bool CanPlayBodyProcessingFeedback()
        {
            return bodyText != null && GetResolvedSteps().Length > 0;
        }

        /// <summary>
        /// Writes processing STATUS lines only into Body_TMP (no flavor). Caller should keep <see cref="DiagnosticDisplayController.BeginBodyWriteSuppress"/> active.
        /// </summary>
        public IEnumerator PlayBodyProcessingFeedback()
        {
            if (bodyText == null)
            {
                Debug.LogWarning(
                    $"[MachineFeedbackTextController] '{name}' has no bodyText assigned; skipping machine feedback.",
                    this);
                yield break;
            }

            string[] steps = GetResolvedSteps();
            if (steps.Length == 0)
            {
                Debug.LogWarning(
                    $"[MachineFeedbackTextController] '{name}' has no processing steps; skipping machine feedback.",
                    this);
                yield break;
            }

            string prefix = processingStatusPrefix ?? string.Empty;
            float wait = Mathf.Max(0.05f, stepDuration);

            for (int i = 0; i < steps.Length; i++)
            {
                string step = steps[i] ?? string.Empty;
                bodyText.text = $"{prefix}{step}";
                bodyText.ForceMeshUpdate(true);
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        /// <summary>One random flavor line for unsolved post-attempt diagnostic footer; null if no pool configured.</summary>
        public string GetRandomFlavorLine()
        {
            string[] flavors = GetResolvedFlavors();
            if (flavors == null || flavors.Length == 0)
            {
                return null;
            }

            return flavors[Random.Range(0, flavors.Length)] ?? string.Empty;
        }

        private string[] GetResolvedSteps()
        {
            if (processingSteps != null && processingSteps.Length > 0)
            {
                return processingSteps;
            }

            return DefaultProcessingSteps;
        }

        private string[] GetResolvedFlavors()
        {
            if (flavorMessages != null && flavorMessages.Length > 0)
            {
                return flavorMessages;
            }

            return DefaultFlavorMessages;
        }
    }
}
