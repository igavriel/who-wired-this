using System.Text;
using TMPro;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// World-space TextMeshPro diagnostic readout. Render-only; reusable across puzzles.
    /// Receives already-calculated metric values and messages; never reads buttons or computes correctness.
    /// Optional status lamp swaps materials per state when assigned.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class DiagnosticDisplayController : MonoBehaviour
    {
        public enum DisplayState
        {
            Clear = 0,
            Waiting = 1,
            Result = 2,
            Success = 3,
            Error = 4
        }

        [Header("References")]
        [Tooltip("World-space TMP component for the title line. Use TextMeshPro (3D), not TextMeshProUGUI.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("World-space TMP component for the body block. Recommended: monospace SDF font for clean column alignment.")]
        [SerializeField] private TMP_Text bodyText;

        [Header("Machine feedback (optional)")]
        [Tooltip("Prefab-local Activate processing copy; when unset, uses GetComponent on this GameObject.")]
        [SerializeField]
        private MachineFeedbackTextController machineFeedbackText;

        [Tooltip("Optional renderer that swaps materials per state. Leave unset to disable lamp behavior.")]
        [SerializeField] private Renderer statusLampRenderer;

        [Header("Content")]
        [SerializeField] private string title = "DIAGNOSTIC";

        [SerializeField] [TextArea(2, 4)] private string waitingText = "WAITING FOR\nNEXT ATTEMPT...";

        [SerializeField] private string clearText = "NO DATA";

        [Tooltip("Minimum padded width for the metric label column so multiple SetDiagnosticResult calls align on the colon.")]
        [Min(1)]
        [SerializeField] private int metricLabelMinWidth = 10;

        [Header("Optional Lamp Materials")]
        [Tooltip("Used only if statusLampRenderer is assigned. Any null entry is skipped.")]
        [SerializeField] private Material lampWaitingMaterial;

        [SerializeField] private Material lampResultMaterial;
        [SerializeField] private Material lampSuccessMaterial;
        [SerializeField] private Material lampErrorMaterial;
        [SerializeField] private Material lampClearMaterial;

        private DisplayState currentState = DisplayState.Waiting;

        /// <summary>When positive, public display APIs skip updating body text so processing feedback can own it.</summary>
        private int bodyWriteSuppressDepth;

        public DisplayState CurrentState => currentState;

        /// <summary>
        /// Optional <see cref="MachineFeedbackTextController"/> on this diagnostic panel (same prefab instance that receives final results).
        /// </summary>
        public MachineFeedbackTextController GetMachineFeedbackText()
        {
            return machineFeedbackText != null ? machineFeedbackText : GetComponent<MachineFeedbackTextController>();
        }

        /// <summary>Suppresses external body writes from public display APIs until <see cref="EndBodyWriteSuppress"/>.</summary>
        public void BeginBodyWriteSuppress()
        {
            bodyWriteSuppressDepth++;
        }

        public void EndBodyWriteSuppress()
        {
            if (bodyWriteSuppressDepth > 0)
            {
                bodyWriteSuppressDepth--;
            }
        }

        /// <summary>Sets body text even while suppress is active (for processing lines only).</summary>
        public void SetProcessingBodyText(string text)
        {
            if (bodyText == null)
            {
                return;
            }

            bodyText.text = text ?? string.Empty;
            bodyText.ForceMeshUpdate(true);
        }

        private void Awake()
        {
            if (machineFeedbackText == null)
            {
                machineFeedbackText = GetComponent<MachineFeedbackTextController>();
            }

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            SetWaiting();
        }

        public void Clear()
        {
            currentState = DisplayState.Clear;
            WriteBody(clearText);
            ApplyLampMaterial(lampClearMaterial);
        }

        public void SetWaiting()
        {
            currentState = DisplayState.Waiting;
            WriteBody(waitingText);
            ApplyLampMaterial(lampWaitingMaterial);
        }

        public void SetDiagnosticResult(
            string metric1Label, int metric1Value, int metric1Max,
            string metric2Label, int metric2Value, int metric2Max,
            string message,
            string flavorBetweenMetricsAndClue = null)
        {
            currentState = DisplayState.Result;

            int width = ComputeLabelWidth(metric1Label, metric2Label);

            StringBuilder sb = new StringBuilder();
            AppendMetricLine(sb, metric1Label, metric1Value, metric1Max, width);
            sb.AppendLine();
            AppendMetricLine(sb, metric2Label, metric2Value, metric2Max, width);

            if (!string.IsNullOrWhiteSpace(flavorBetweenMetricsAndClue))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(flavorBetweenMetricsAndClue);
            }

            if (!string.IsNullOrEmpty(message))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(message);
            }

            WriteBody(sb.ToString());
            ApplyLampMaterial(lampResultMaterial);
        }

        public void SetSuccess(string message)
        {
            currentState = DisplayState.Success;
            WriteBody(message ?? string.Empty);
            ApplyLampMaterial(lampSuccessMaterial);
        }

        /// <summary>Body-only failed-attempt readout (no metric rows). Uses result lamp styling.</summary>
        public void SetDiagnosticBody(string message)
        {
            currentState = DisplayState.Result;
            WriteBody(message ?? string.Empty);
            ApplyLampMaterial(lampResultMaterial);
        }

        public void SetError(string message)
        {
            currentState = DisplayState.Error;
            WriteBody(message ?? string.Empty);
            ApplyLampMaterial(lampErrorMaterial);
        }

        /// <summary>
        /// Body text only; uses the same <see cref="WriteBody"/> path as other display updates (respects suppress depth).
        /// Does not change lamp or <see cref="currentState"/> — use only when timing is owned outside this component (e.g. tutorial stage boundaries).
        /// </summary>
        public void SetInstructionBody(string body)
        {
            WriteBody(body);
        }

        [ContextMenu("Set Waiting")]
        private void SetWaitingFromInspector()
        {
            EnsureTitle();
            SetWaiting();
        }

        [ContextMenu("Clear")]
        private void ClearFromInspector()
        {
            EnsureTitle();
            Clear();
        }

        private void EnsureTitle()
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }
        }

        private void WriteBody(string contentBlock)
        {
            if (bodyText == null || bodyWriteSuppressDepth > 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(contentBlock ?? string.Empty);
            bodyText.text = sb.ToString();
        }

        private static void AppendMetricLine(StringBuilder sb, string label, int value, int max, int width)
        {
            string padded = PadColonLabel(label, width);
            sb.Append(padded);
            sb.Append(' ');
            sb.Append(value);
            sb.Append(" / ");
            sb.Append(max);
        }

        private static string PadColonLabel(string rawLabel, int width)
        {
            string label = rawLabel ?? string.Empty;
            string withColon = label.EndsWith(":") ? label : label + ":";
            if (withColon.Length >= width)
            {
                return withColon;
            }

            return withColon + new string(' ', width - withColon.Length);
        }

        private int ComputeLabelWidth(string label1, string label2)
        {
            int w = Mathf.Max(1, metricLabelMinWidth);
            w = Mathf.Max(w, (label1 ?? string.Empty).Length + 1);
            w = Mathf.Max(w, (label2 ?? string.Empty).Length + 1);
            return w;
        }

        private void ApplyLampMaterial(Material material)
        {
            if (statusLampRenderer == null || material == null)
            {
                return;
            }

            statusLampRenderer.sharedMaterial = material;
        }
    }
}
