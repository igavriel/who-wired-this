#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace WhoWiredThis.Editor
{
    public static class EditorValidationConsoleReporter
    {
        public static void Report(string title, int issueCount, string reportBody, bool showDialog = false)
        {
            reportBody ??= string.Empty;

            string summary = ExtractSummaryLine(reportBody)
                ?? (issueCount == 0
                    ? $"{title}: ALL CHECKS PASSED"
                    : $"{title}: {issueCount} issue(s)");

            foreach (string rawLine in reportBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.TrimEnd();
                if (line.StartsWith("FAIL:", StringComparison.Ordinal))
                {
                    Debug.LogError($"[{title}] {line}");
                }
                else if (line.StartsWith("WARN:", StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[{title}] {line}");
                }
            }

            if (issueCount == 0)
            {
                Debug.Log($"[{title}] {summary}\n{reportBody}");
            }
            else
            {
                Debug.LogError($"[{title}] {summary}\n{reportBody}");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    issueCount == 0 ? $"{title} OK" : $"{title} Issues",
                    reportBody,
                    "OK");
            }
        }

        private static string ExtractSummaryLine(string reportBody)
        {
            foreach (string rawLine in reportBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("===", StringComparison.Ordinal)
                    && line.Contains("validation", StringComparison.OrdinalIgnoreCase))
                {
                    return line;
                }
            }

            return null;
        }
    }
}
#endif
