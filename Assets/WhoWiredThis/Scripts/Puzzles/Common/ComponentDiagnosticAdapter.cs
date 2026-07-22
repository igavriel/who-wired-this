using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    public enum ComponentDiagnosticType
    {
        Ordered,
        Categorical
    }

    public enum ComponentDiagnosticBodyLayout
    {
        LegacyHints,
        LogRows
    }

    [Serializable]
    public class ComponentDiagnosticDefinition
    {
        public MultiDimension input;
        public ComponentDiagnosticType diagnosticType = ComponentDiagnosticType.Ordered;

        [Header("LegacyHints sentences")]
        [TextArea(1, 2)]
        public string correctText = "COMPONENT LOOKS STABLE.";

        [TextArea(1, 2)]
        public string tooLowText = "COMPONENT IS TOO LOW.";

        [TextArea(1, 2)]
        public string tooHighText = "COMPONENT IS TOO HIGH.";

        [TextArea(1, 2)]
        public string mismatchText = "COMPONENT DOES NOT MATCH.";

        [Header("LogRows short status")]
        [Tooltip("Left-side label on the log row (e.g. PRESSURE).")]
        public string rowLabel = "COMPONENT";

        [Tooltip("Status when this slot matches the solution.")]
        public string correctStatus = "OK";

        [Tooltip("Close: submitted index is 1 below correct (TooLow direction).")]
        public string closeTooLowStatus = "A BIT HIGH";

        [Tooltip("Close: submitted index is 1 above correct (TooHigh direction).")]
        public string closeTooHighStatus = "A BIT LOW";

        [Tooltip("Far: submitted index is 2+ below correct (TooLow direction).")]
        public string farTooLowStatus = "TOO HIGH";

        [Tooltip("Far: submitted index is 2+ above correct (TooHigh direction).")]
        public string farTooHighStatus = "TOO LOW";

        [Tooltip("Categorical mismatch status (e.g. NOT BALANCED).")]
        public string mismatchStatus = "NOT BALANCED";

        public bool eligibleForHints = true;
    }

    /// <summary>
    /// Per-component pipe-style diagnostics for MultiDimension puzzles (not Bulls/Cows).
    /// </summary>
    public class ComponentDiagnosticAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;
        [SerializeField] private DiagnosticDisplayController diagnosticDisplay;

        [Header("Layout")]
        [SerializeField] private ComponentDiagnosticBodyLayout bodyLayout = ComponentDiagnosticBodyLayout.LegacyHints;

        [Header("Components")]
        [SerializeField] private ComponentDiagnosticDefinition[] components;

        [Header("System messages (LegacyHints)")]
        [SerializeField] private string solvedMessage = "PIPE LINE CALIBRATED.";
        [SerializeField] private string systemNoneCorrect = "PIPE RESPONSE IS UNSTABLE.";
        [SerializeField] private string systemOneCorrect = "ONE PIPE SECTION RESPONDS.";
        [SerializeField] private string systemTwoCorrect = "PIPE RESPONSE IS CLOSE.";
        [SerializeField] private string partnerLine = "TELL YOUR PARTNER WHAT YOU LEARNED.";

        [Header("LogRows chrome")]
        [SerializeField] private string headerLine1 = "OTHER PLAYER SUBMITS // YOU READ";
        [SerializeField] private string headerLine2 = "### FIND THE PATTERN IN THE LOG ###";
        [SerializeField] private string logTitlePrefix = "DIAGNOSTIC LOG // REVISION";
        [SerializeField] private string statusLabel = "STATUS";
        [SerializeField] private string statusValue = "ANALYZING";
        [SerializeField] private string footerLine = "WAITING FOR PARTNER INPUT";
        [SerializeField] private int lineWidth = ComponentDiagnosticLogFormatter.DefaultWidth;
        [SerializeField] private int totalLines = ComponentDiagnosticLogFormatter.DefaultTotalLines;

        private int attemptCounter;

        private struct SlotEvaluation
        {
            public int SlotIndex;
            public ComponentSlotDiagnosticStatus Status;
            public ComponentDiagnosticDefinition Definition;
        }

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }

            if (diagnosticDisplay == null)
            {
                return;
            }

            if (puzzleManager != null && puzzleManager.Solved)
            {
                diagnosticDisplay.SetSuccess(solvedMessage);
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (puzzleManager == null || diagnosticDisplay == null || result == null)
            {
                return;
            }

            if (result.IsSolved || puzzleManager.Solved)
            {
                diagnosticDisplay.SetSuccess(solvedMessage);
                return;
            }

            attemptCounter++;
            string body = BuildFailedAttemptBody(result);
            diagnosticDisplay.SetDiagnosticBody(body);
        }

        private string BuildFailedAttemptBody(MultiDimensionAttemptResult result)
        {
            List<SlotEvaluation> evaluations = EvaluateSlots(result);
            if (bodyLayout == ComponentDiagnosticBodyLayout.LogRows)
            {
                return BuildLogRowsBody(evaluations);
            }

            return BuildLegacyHintsBody(evaluations);
        }

        private string BuildLogRowsBody(List<SlotEvaluation> evaluations)
        {
            var rows = new List<string>();
            for (int i = 0; i < evaluations.Count; i++)
            {
                SlotEvaluation eval = evaluations[i];
                if (eval.Definition == null)
                {
                    continue;
                }

                string label = string.IsNullOrEmpty(eval.Definition.rowLabel)
                    ? "COMPONENT"
                    : eval.Definition.rowLabel;
                string status = ResolveLogStatus(eval);
                rows.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus(label, status, lineWidth));
            }

            return ComponentDiagnosticLogFormatter.BuildLogBody(
                headerLine1,
                headerLine2,
                logTitlePrefix,
                attemptCounter,
                statusLabel,
                statusValue,
                rows,
                footerLine,
                lineWidth,
                totalLines);
        }

        /// <summary>40×12 pre-submit standby for the partner monitor (LogRows layout only).</summary>
        public string BuildStandbyBody()
        {
            if (bodyLayout != ComponentDiagnosticBodyLayout.LogRows)
            {
                return partnerLine ?? string.Empty;
            }

            var rows = new List<string>();
            if (components != null)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    ComponentDiagnosticDefinition def = components[i];
                    if (def == null)
                    {
                        continue;
                    }

                    string label = string.IsNullOrEmpty(def.rowLabel) ? "COMPONENT" : def.rowLabel;
                    rows.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus(label, "STANDBY", lineWidth));
                }
            }

            return ComponentDiagnosticLogFormatter.BuildLogBody(
                headerLine1,
                headerLine2,
                logTitlePrefix,
                0,
                statusLabel,
                statusValue,
                rows,
                footerLine,
                lineWidth,
                totalLines);
        }

        private string BuildLegacyHintsBody(List<SlotEvaluation> evaluations)
        {
            int correctCount = 0;
            for (int i = 0; i < evaluations.Count; i++)
            {
                if (evaluations[i].Status == ComponentSlotDiagnosticStatus.Correct)
                {
                    correctCount++;
                }
            }

            var hintLines = new List<string>();
            CollectHintLines(evaluations, hintLines);

            var sb = new StringBuilder();
            sb.Append(ResolveSystemMessage(correctCount));

            for (int i = 0; i < hintLines.Count; i++)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(hintLines[i]);
            }

            if (!string.IsNullOrEmpty(partnerLine))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(partnerLine);
            }

            return sb.ToString();
        }

        private List<SlotEvaluation> EvaluateSlots(MultiDimensionAttemptResult result)
        {
            var evaluations = new List<SlotEvaluation>();
            if (components == null || result.SubmittedIndices == null)
            {
                return evaluations;
            }

            // Preserve Inspector array order for LogRows (do not re-sort by slot index).
            for (int c = 0; c < components.Length; c++)
            {
                ComponentDiagnosticDefinition def = components[c];
                if (def == null || def.input == null)
                {
                    continue;
                }

                if (!TryResolveSlotIndex(def.input, out int slotIndex))
                {
                    Debug.LogWarning(
                        $"[ComponentDiagnosticAdapter] '{name}' could not resolve slot for component at index {c}.",
                        this);
                    continue;
                }

                if (slotIndex < 0 || slotIndex >= result.SubmittedIndices.Length)
                {
                    continue;
                }

                if (!puzzleManager.TryGetPuzzleElement(slotIndex, out _, out int correctIndex))
                {
                    continue;
                }

                int submitted = result.SubmittedIndices[slotIndex];
                evaluations.Add(new SlotEvaluation
                {
                    SlotIndex = slotIndex,
                    Status = Classify(def, submitted, correctIndex),
                    Definition = def
                });
            }

            if (bodyLayout == ComponentDiagnosticBodyLayout.LegacyHints)
            {
                evaluations.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
            }

            return evaluations;
        }

        private bool TryResolveSlotIndex(MultiDimension input, out int slotIndex)
        {
            slotIndex = -1;
            int count = puzzleManager.PuzzleElementCount;
            for (int i = 0; i < count; i++)
            {
                if (puzzleManager.TryGetPuzzleElement(i, out MultiDimension element, out _) &&
                    element == input)
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static ComponentSlotDiagnosticStatus Classify(
            ComponentDiagnosticDefinition def,
            int submitted,
            int correctIndex)
        {
            return ComponentDiagnosticClassifier.Classify(def.diagnosticType, submitted, correctIndex);
        }

        private static void CollectHintLines(List<SlotEvaluation> evaluations, List<string> hintLines)
        {
            string firstCorrect = null;
            string firstWrong = null;
            string secondWrong = null;

            for (int i = 0; i < evaluations.Count; i++)
            {
                SlotEvaluation eval = evaluations[i];
                if (eval.Definition == null || !eval.Definition.eligibleForHints)
                {
                    continue;
                }

                if (eval.Status == ComponentSlotDiagnosticStatus.Correct)
                {
                    if (firstCorrect == null)
                    {
                        firstCorrect = eval.Definition.correctText;
                    }

                    continue;
                }

                string wrongLine = GetWrongHintText(eval);
                if (wrongLine == null)
                {
                    continue;
                }

                if (firstWrong == null)
                {
                    firstWrong = wrongLine;
                }
                else if (secondWrong == null)
                {
                    secondWrong = wrongLine;
                }
            }

            if (firstCorrect != null)
            {
                hintLines.Add(firstCorrect);
            }

            if (firstWrong != null)
            {
                hintLines.Add(firstWrong);
            }

            if (firstCorrect == null && secondWrong != null)
            {
                hintLines.Add(secondWrong);
            }
        }

        private static string GetWrongHintText(SlotEvaluation eval)
        {
            ComponentDiagnosticDefinition def = eval.Definition;
            switch (eval.Status)
            {
                case ComponentSlotDiagnosticStatus.TooLow:
                case ComponentSlotDiagnosticStatus.FarTooLow:
                    return FirstNonEmpty(def.farTooLowStatus, def.tooLowText);
                case ComponentSlotDiagnosticStatus.CloseTooLow:
                    return FirstNonEmpty(def.closeTooLowStatus, def.tooLowText);
                case ComponentSlotDiagnosticStatus.TooHigh:
                case ComponentSlotDiagnosticStatus.FarTooHigh:
                    return FirstNonEmpty(def.farTooHighStatus, def.tooHighText);
                case ComponentSlotDiagnosticStatus.CloseTooHigh:
                    return FirstNonEmpty(def.closeTooHighStatus, def.tooHighText);
                case ComponentSlotDiagnosticStatus.Mismatch:
                    return FirstNonEmpty(def.mismatchStatus, def.mismatchText);
                default:
                    return null;
            }
        }

        private static string ResolveLogStatus(SlotEvaluation eval)
        {
            ComponentDiagnosticDefinition def = eval.Definition;
            switch (eval.Status)
            {
                case ComponentSlotDiagnosticStatus.Correct:
                    return FirstNonEmpty(def.correctStatus, "OK");
                case ComponentSlotDiagnosticStatus.CloseTooLow:
                    return FirstNonEmpty(def.closeTooLowStatus, def.tooLowText, "A BIT HIGH");
                case ComponentSlotDiagnosticStatus.CloseTooHigh:
                    return FirstNonEmpty(def.closeTooHighStatus, def.tooHighText, "A BIT LOW");
                case ComponentSlotDiagnosticStatus.FarTooLow:
                case ComponentSlotDiagnosticStatus.TooLow:
                    return FirstNonEmpty(def.farTooLowStatus, def.tooLowText, "TOO HIGH");
                case ComponentSlotDiagnosticStatus.FarTooHigh:
                case ComponentSlotDiagnosticStatus.TooHigh:
                    return FirstNonEmpty(def.farTooHighStatus, def.tooHighText, "TOO LOW");
                case ComponentSlotDiagnosticStatus.Mismatch:
                    return FirstNonEmpty(def.mismatchStatus, def.mismatchText, "NOT BALANCED");
                default:
                    return "???";
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }

        private string ResolveSystemMessage(int correctCount)
        {
            switch (correctCount)
            {
                case 0:
                    return systemNoneCorrect;
                case 1:
                    return systemOneCorrect;
                case 2:
                    return systemTwoCorrect;
                default:
                    return systemTwoCorrect;
            }
        }
    }
}
