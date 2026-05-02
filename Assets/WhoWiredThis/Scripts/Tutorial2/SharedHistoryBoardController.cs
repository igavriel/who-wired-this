using System.Text;
using TMPro;
using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    public class SharedHistoryBoardController : MonoBehaviour
    {
        [SerializeField] private TMP_Text boardText;
        [SerializeField] private int maxRows = 12;

        private readonly StringBuilder builder = new StringBuilder();

        private void Awake()
        {
            if (boardText == null)
            {
                boardText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        public void RenderHistory(PuzzleAttemptRecord[] records)
        {
            if (boardText == null)
            {
                return;
            }

            builder.Clear();
            builder.AppendLine("PHASE | ACTOR | GUESS | FEEDBACK | NOTE");

            int start = Mathf.Max(0, records.Length - maxRows);
            for (int i = start; i < records.Length; i++)
            {
                PuzzleAttemptRecord row = records[i];
                builder.Append(row.PhaseNumber)
                    .Append(" | ")
                    .Append(row.Actor == Tutorial.TutorialPlayerSlot.PlayerA ? "P1" : "P2")
                    .Append(" | ")
                    .Append(row.GuessText)
                    .Append(" | ")
                    .Append(row.FeedbackText)
                    .Append(" | ")
                    .Append(row.Note)
                    .AppendLine();
            }

            boardText.text = builder.ToString();
        }

        public void ShowBanner(string text)
        {
            if (boardText != null)
            {
                boardText.text = text;
            }
        }
    }
}
