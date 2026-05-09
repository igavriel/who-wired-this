using System.Text;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Subscribes to <see cref="MultiDimensionPuzzelManager.OnAttemptSubmitted"/> and appends a public row to
    /// <see cref="HistoryBoardController"/>. Holds redundant <see cref="inputOrder"/> (same sequence as puzzle elements)
    /// so display labels use <see cref="MultiDimension.GetSubjectDisplayName"/> without the board knowing the manager.
    /// </summary>
    public class MultiDimensionHistoryAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzelManager puzzleManager;

        [SerializeField] private SharedHistorySO historySource;

        [Tooltip("Legacy fallback during migration. Prefer writing to SharedHistorySO.")]
        [SerializeField] private HistoryBoardController historyBoard;

        [Tooltip("Same order and length as the manager's puzzle elements. Used only for label lookup.")]
        [SerializeField] private MultiDimension[] inputOrder;

        [Header("Display")]
        [SerializeField] private string inputSeparator = " ";

        [Tooltip("If set, replaces result.PublicStatus when the attempt succeeds (e.g. A-SIDE CALIBRATED).")]
        [SerializeField] private string solvedStatus;

        [Tooltip("If set, replaces result.PublicStatus when the attempt fails (e.g. SIGNAL UNSTABLE).")]
        [SerializeField] private string unsolvedStatus;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
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
            if (result == null)
            {
                return;
            }

            string inputText = BuildInputText(result);
            string status = ResolvePublicStatus(result);

            SharedHistorySO targetSource = ResolveHistorySource();
            if (targetSource != null)
            {
                targetSource.AddEntry(result.ActorLabel, inputText, status);
                return;
            }

            if (historyBoard != null)
            {
                historyBoard.AddEntry(result.ActorLabel, inputText, status);
            }
        }

        private SharedHistorySO ResolveHistorySource()
        {
            if (historySource != null)
            {
                return historySource;
            }

            return historyBoard != null ? historyBoard.HistorySource : null;
        }

        private string ResolvePublicStatus(MultiDimensionAttemptResult result)
        {
            if (result.IsSolved && !string.IsNullOrEmpty(solvedStatus))
            {
                return solvedStatus;
            }

            if (!result.IsSolved && !string.IsNullOrEmpty(unsolvedStatus))
            {
                return unsolvedStatus;
            }

            return result.PublicStatus ?? string.Empty;
        }

        private string BuildInputText(MultiDimensionAttemptResult result)
        {
            if (inputOrder == null || result.SubmittedIndices == null)
            {
                return string.Empty;
            }

            int n = Mathf.Min(inputOrder.Length, result.SubmittedIndices.Length);
            StringBuilder sb = new StringBuilder();
            bool any = false;

            for (int i = 0; i < n; i++)
            {
                MultiDimension md = inputOrder[i];
                if (md == null)
                {
                    continue;
                }

                int idx = result.SubmittedIndices[i];
                string label = idx >= 0 ? md.GetSubjectDisplayName(idx) : string.Empty;
                if (string.IsNullOrEmpty(label))
                {
                    label = idx >= 0 ? idx.ToString() : "?";
                }

                if (any)
                {
                    sb.Append(inputSeparator);
                }

                any = true;
                sb.Append(label);
            }

            return sb.ToString();
        }
    }
}
