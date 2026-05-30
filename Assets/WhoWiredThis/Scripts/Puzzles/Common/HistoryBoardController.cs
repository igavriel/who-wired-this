using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// World-space TextMeshPro history board display. Reusable across puzzles.
    /// Reads entries from a shared history source and keeps only local view state (scroll offset).
    /// Auto-scrolls to the latest entry unless the user has scrolled away from the tail.
    /// </summary>
    public class HistoryBoardController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("World-space TMP component for the title line. Use TextMeshPro (3D), not TextMeshProUGUI.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("World-space TMP component for the table body. Recommended: assign a monospace SDF font asset for clean column alignment.")]
        [SerializeField] private TMP_Text bodyText;

        [Tooltip("Shared history source. Multiple board displays can point to the same source.")]
        [SerializeField] private SharedHistorySO historySource;

        [Header("Content")]
        [SerializeField] private string title = "SHARED HISTORY";
        [Tooltip("Header line above the rows.")]
        [SerializeField] private string headerLine = " # | SIDE | INPUT       | STATUS";
        [SerializeField] private string separatorLine = "===+======+=============+==========";

        [Header("Layout")]
        [Tooltip("Maximum visible rows; older rows scroll out of view.")]
        [Min(1)]
        [SerializeField] private int maxVisibleRows = 10;

        // 0 = anchored to latest. Increases as the user scrolls toward older rows.
        private int viewOffset;
        private bool userScrolled;

        public IReadOnlyList<HistoryEntry> Entries => historySource != null ? historySource.Entries : EmptyEntries;
        public int MaxVisibleRows => maxVisibleRows;
        public SharedHistorySO HistorySource => historySource;

        private static readonly IReadOnlyList<HistoryEntry> EmptyEntries = new List<HistoryEntry>();
        private const int RetryColumnWidth = 2;
        private const int ActorColumnWidth = 4;
        private const int InputTokenWidth = 5;

        private void Awake()
        {
            if (bodyText != null)
            {
                // Keep each history entry on a single rendered row; anchor text to bottom so newest rows stay visible.
                bodyText.textWrappingMode = TextWrappingModes.NoWrap;
                bodyText.overflowMode = TextOverflowModes.Overflow;
                bodyText.verticalAlignment = VerticalAlignmentOptions.Bottom;
            }

            SubscribeToSource();
            Render();
        }

        private void OnEnable()
        {
            SubscribeToSource();
            Render();
        }

        private void OnDisable()
        {
            UnsubscribeFromSource();
        }

        public void Clear()
        {
            historySource?.Clear();
        }

        public int AddEntry(string actor, string inputText, string publicStatus)
        {
            return historySource != null ? historySource.AddEntry(actor, inputText, publicStatus) : 0;
        }

        public int AddEntry(HistoryEntry entry)
        {
            return historySource != null ? historySource.AddEntry(entry) : 0;
        }

        public void SetMaxVisibleRows(int count)
        {
            maxVisibleRows = Mathf.Max(1, count);
            ClampViewOffset();
            Render();
        }

        public void ScrollUp()
        {
            int maxOffset = Mathf.Max(0, Entries.Count - maxVisibleRows);
            if (viewOffset >= maxOffset)
            {
                return;
            }

            viewOffset++;
            userScrolled = viewOffset > 0;
            Render();
        }

        public void ScrollDown()
        {
            if (viewOffset <= 0)
            {
                userScrolled = false;
                return;
            }

            viewOffset--;
            if (viewOffset == 0)
            {
                userScrolled = false;
            }

            Render();
        }

        public void ScrollToLatest()
        {
            viewOffset = 0;
            userScrolled = false;
            Render();
        }

        public void Render()
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (bodyText == null)
            {
                return;
            }

            ClampViewOffset();

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(headerLine))
            {
                sb.AppendLine(headerLine);
            }

            if (!string.IsNullOrEmpty(separatorLine))
            {
                sb.AppendLine(separatorLine);
            }

            IReadOnlyList<HistoryEntry> entries = Entries;
            int total = entries.Count;
            int visible = Mathf.Min(maxVisibleRows, total);
            int startIndex = Mathf.Max(0, total - viewOffset - visible);
            int endIndex = startIndex + visible;

            for (int i = startIndex; i < endIndex; i++)
            {
                HistoryEntry entry = entries[i];
                sb.Append(FormatRetryCell(entry.attemptNumber.ToString()));
                sb.Append(" | ");
                sb.Append(FormatActorCell(entry.actor));
                sb.Append(" | ");
                sb.Append(FormatInputCell(entry.inputText));
                sb.Append(" | ");
                sb.Append(entry.publicStatus ?? string.Empty);
                if (i < endIndex - 1)
                {
                    sb.AppendLine();
                }
            }

            bodyText.text = sb.ToString();
        }

        [ContextMenu("Clear History")]
        private void ClearFromInspector()
        {
            Clear();
        }

        private void SubscribeToSource()
        {
            if (historySource != null)
            {
                historySource.OnChanged -= HandleHistoryChanged;
                historySource.OnChanged += HandleHistoryChanged;
            }
        }

        private void UnsubscribeFromSource()
        {
            if (historySource != null)
            {
                historySource.OnChanged -= HandleHistoryChanged;
            }
        }

        private void HandleHistoryChanged()
        {
            if (userScrolled)
            {
                Render();
                return;
            }

            ScrollToLatest();
        }

        private static string FormatRetryCell(string value)
        {
            string text = value?.Trim() ?? string.Empty;
            if (text.Length > RetryColumnWidth)
            {
                text = text.Substring(text.Length - RetryColumnWidth, RetryColumnWidth);
            }

            return text.PadLeft(RetryColumnWidth);
        }

        private static string FormatActorCell(string value)
        {
            string text = value?.Trim() ?? string.Empty;
            if (text.Length > ActorColumnWidth)
            {
                text = text.Substring(0, ActorColumnWidth);
            }

            return text.PadRight(ActorColumnWidth);
        }

        private static string FormatInputCell(string value)
        {
            string raw = value?.Trim() ?? string.Empty;
            if (raw.Length == 0)
            {
                return new string(' ', InputTokenWidth);
            }

            string[] tokens = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (token.Length > InputTokenWidth)
                {
                    token = token.Substring(0, InputTokenWidth);
                }
                else if (token.Length < InputTokenWidth)
                {
                    token = token.PadRight(InputTokenWidth);
                }

                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(token);
            }

            return sb.ToString();
        }

        private void ClampViewOffset()
        {
            int maxOffset = Mathf.Max(0, Entries.Count - maxVisibleRows);
            if (viewOffset > maxOffset)
            {
                viewOffset = maxOffset;
            }

            if (viewOffset < 0)
            {
                viewOffset = 0;
            }

            if (viewOffset == 0)
            {
                userScrolled = false;
            }
        }

    }
}
