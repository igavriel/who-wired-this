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

    [Serializable]
    public class ComponentDiagnosticDefinition
    {
        public MultiDimension input;
        public ComponentDiagnosticType diagnosticType = ComponentDiagnosticType.Ordered;

        [TextArea(1, 2)]
        public string correctText = "COMPONENT LOOKS STABLE.";

        [TextArea(1, 2)]
        public string tooLowText = "COMPONENT IS TOO LOW.";

        [TextArea(1, 2)]
        public string tooHighText = "COMPONENT IS TOO HIGH.";

        [TextArea(1, 2)]
        public string mismatchText = "COMPONENT DOES NOT MATCH.";

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

        [Header("Components")]
        [SerializeField] private ComponentDiagnosticDefinition[] components;

        [Header("System messages")]
        [SerializeField] private string solvedMessage = "PIPE LINE CALIBRATED.";
        [SerializeField] private string systemNoneCorrect = "PIPE RESPONSE IS UNSTABLE.";
        [SerializeField] private string systemOneCorrect = "ONE PIPE SECTION RESPONDS.";
        [SerializeField] private string systemTwoCorrect = "PIPE RESPONSE IS CLOSE.";
        [SerializeField] private string partnerLine = "TELL YOUR PARTNER WHAT YOU LEARNED.";

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
            else
            {
                diagnosticDisplay.SetWaiting();
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void Start()
        {
            if (diagnosticDisplay == null || puzzleManager == null || puzzleManager.Solved)
            {
                return;
            }

            diagnosticDisplay.SetWaiting();
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

            string body = BuildFailedAttemptBody(result);
            diagnosticDisplay.SetDiagnosticBody(body);
        }

        private string BuildFailedAttemptBody(MultiDimensionAttemptResult result)
        {
            List<SlotEvaluation> evaluations = EvaluateSlots(result);
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

            evaluations.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
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
                    return def.tooLowText;
                case ComponentSlotDiagnosticStatus.TooHigh:
                    return def.tooHighText;
                case ComponentSlotDiagnosticStatus.Mismatch:
                    return def.mismatchText;
                default:
                    return null;
            }
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
