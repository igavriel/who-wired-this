using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    [Serializable]
    public class HistoryEntry
    {
        public int attemptNumber;
        public string actor;
        public string inputText;
        public string publicStatus;
    }

    /// <summary>
    /// World-space TextMeshPro history board. Render-only; reusable across puzzles.
    /// Owns its own attempt counter and view offset; does not know about validators or solutions.
    /// Auto-scrolls to the latest entry unless the user has scrolled away from the tail.
    /// </summary>
    public class HistoryBoardController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("World-space TMP component for the title line. Use TextMeshPro (3D), not TextMeshProUGUI.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("World-space TMP component for the table body. Recommended: assign a monospace SDF font asset for clean column alignment.")]
        [SerializeField] private TMP_Text bodyText;

        [Header("Content")]
        [SerializeField] private string title = "SHARED HISTORY";

        [Tooltip("Header line above the rows.")]
        [SerializeField] private string headerLine = "# | ACTOR | INPUT | STATUS";

        [SerializeField] private string separatorLine = "----------------------------";

        [Header("Layout")]
        [Tooltip("Maximum visible rows; older rows scroll out of view.")]
        [Min(1)]
        [SerializeField] private int maxVisibleRows = 6;

        [Tooltip("Minimum padded width for the actor column. Set to header label length for clean alignment.")]
        [Min(1)]
        [SerializeField] private int minActorColumnWidth = 5;

        [Tooltip("Minimum padded width for the input column.")]
        [Min(1)]
        [SerializeField] private int minInputColumnWidth = 5;

        [Header("Debug")]
        [Tooltip("If true, polls keyboard for the debug shortcuts: H = sample row, Shift+H = clear, PageUp / PageDown = scroll.")]
        [SerializeField] private bool enableDebugInput;

        [SerializeField] private string debugSampleActor = "P1";
        [SerializeField] private string debugSampleInput = "R G";
        [SerializeField] private string debugSampleStatus = "SIGNAL UNSTABLE";

        private readonly List<HistoryEntry> entries = new List<HistoryEntry>();
        private int nextAttemptNumber = 1;
        // 0 = anchored to latest. Increases as the user scrolls toward older rows.
        private int viewOffset;
        private bool userScrolled;

        public IReadOnlyList<HistoryEntry> Entries => entries;
        public int MaxVisibleRows => maxVisibleRows;

        private void Awake()
        {
            if (bodyText != null)
            {
                // Keep each history entry on a single rendered row.
                bodyText.textWrappingMode = TextWrappingModes.NoWrap;
                bodyText.overflowMode = TextOverflowModes.Overflow;
            }

            Render();
        }

        private void Update()
        {
            if (!enableDebugInput)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    Clear();
                }
                else
                {
                    AddEntry(debugSampleActor, debugSampleInput, debugSampleStatus);
                }
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                ScrollUp();
            }

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                ScrollDown();
            }
        }

        public void Clear()
        {
            entries.Clear();
            nextAttemptNumber = 1;
            viewOffset = 0;
            userScrolled = false;
            Render();
        }

        public int AddEntry(string actor, string inputText, string publicStatus)
        {
            HistoryEntry entry = new HistoryEntry
            {
                attemptNumber = nextAttemptNumber++,
                actor = actor ?? string.Empty,
                inputText = inputText ?? string.Empty,
                publicStatus = publicStatus ?? string.Empty
            };

            entries.Add(entry);
            FinalizeAddedEntry();
            return entry.attemptNumber;
        }

        public int AddEntry(HistoryEntry entry)
        {
            if (entry == null)
            {
                return 0;
            }

            if (entry.attemptNumber <= 0)
            {
                entry.attemptNumber = nextAttemptNumber++;
            }
            else
            {
                nextAttemptNumber = Mathf.Max(nextAttemptNumber, entry.attemptNumber + 1);
            }

            entries.Add(entry);
            FinalizeAddedEntry();
            return entry.attemptNumber;
        }

        public void SetMaxVisibleRows(int count)
        {
            maxVisibleRows = Mathf.Max(1, count);
            ClampViewOffset();
            Render();
        }

        public void ScrollUp()
        {
            int maxOffset = Mathf.Max(0, entries.Count - maxVisibleRows);
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

            int columnNumWidth = ComputeColumnWidth(e => e.attemptNumber.ToString(), 1);
            int columnActorWidth = ComputeColumnWidth(e => e.actor, minActorColumnWidth);
            int columnInputWidth = ComputeColumnWidth(e => e.inputText, minInputColumnWidth);

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(headerLine))
            {
                sb.AppendLine(headerLine);
            }

            if (!string.IsNullOrEmpty(separatorLine))
            {
                sb.AppendLine(separatorLine);
            }

            int total = entries.Count;
            int visible = Mathf.Min(maxVisibleRows, total);
            int startIndex = Mathf.Max(0, total - viewOffset - visible);
            int endIndex = startIndex + visible;

            for (int i = startIndex; i < endIndex; i++)
            {
                HistoryEntry entry = entries[i];
                sb.Append(PadRight(entry.attemptNumber.ToString(), columnNumWidth));
                sb.Append(" | ");
                sb.Append(PadRight(entry.actor ?? string.Empty, columnActorWidth));
                sb.Append(" | ");
                sb.Append(PadRight(entry.inputText ?? string.Empty, columnInputWidth));
                sb.Append(" | ");
                sb.Append(entry.publicStatus ?? string.Empty);
                if (i < endIndex - 1)
                {
                    sb.AppendLine();
                }
            }

            bodyText.text = sb.ToString();
        }

        [ContextMenu("Add Sample Entry")]
        private void AddSampleEntryFromInspector()
        {
            AddEntry(debugSampleActor, debugSampleInput, debugSampleStatus);
        }

        [ContextMenu("Clear History")]
        private void ClearFromInspector()
        {
            Clear();
        }

        private void FinalizeAddedEntry()
        {
            if (userScrolled)
            {
                Render();
            }
            else
            {
                ScrollToLatest();
            }
        }

        private void ClampViewOffset()
        {
            int maxOffset = Mathf.Max(0, entries.Count - maxVisibleRows);
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

        private int ComputeColumnWidth(Func<HistoryEntry, string> selector, int minWidth)
        {
            int width = Mathf.Max(1, minWidth);
            for (int i = 0; i < entries.Count; i++)
            {
                string value = selector(entries[i]) ?? string.Empty;
                if (value.Length > width)
                {
                    width = value.Length;
                }
            }

            return width;
        }

        private static string PadRight(string value, int width)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value.Length >= width)
            {
                return value;
            }

            return value + new string(' ', width - value.Length);
        }
    }
}
