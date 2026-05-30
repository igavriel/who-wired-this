using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    [CreateAssetMenu(
        fileName = "SharedHistory",
        menuName = "Who Wired This/Puzzles/Shared History",
        order = 1000)]
    public class SharedHistorySO : ScriptableObject
    {
        private static readonly HashSet<SharedHistorySO> LoadedInstances = new HashSet<SharedHistorySO>();

        [Tooltip("The list of history entries.")]
        [SerializeField] private List<HistoryEntry> entries = new List<HistoryEntry>();

        [Tooltip("The next attempt number to use for new entries.")]
        [SerializeField] private int nextAttemptNumber = 1;

        [Tooltip("Maximum entries kept in one puzzle; oldest rows are removed without renumbering.")]
        [Min(1)]
        [SerializeField] private int maxRetainedEntries = 20;

        public event Action OnChanged;

        public IReadOnlyList<HistoryEntry> Entries => entries;

        private void OnEnable()
        {
            LoadedInstances.Add(this);
        }

        private void OnDisable()
        {
            LoadedInstances.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAllAtPlayStart()
        {
            ClearAllLoadedWithoutNotify();
        }

        /// <summary>Clears all loaded history sources and resets attempt counters (e.g. new scene or abort to menu).</summary>
        public static void ClearAllLoaded()
        {
            foreach (SharedHistorySO instance in LoadedInstances)
            {
                if (instance != null)
                {
                    instance.Clear();
                }
            }
        }

        private static void ClearAllLoadedWithoutNotify()
        {
            foreach (SharedHistorySO instance in LoadedInstances)
            {
                if (instance != null)
                {
                    instance.ResetWithoutNotify();
                }
            }
        }

        public int AddEntry(string actor, string inputText, string publicStatus)
        {
            var entry = new HistoryEntry
            {
                attemptNumber = 0,
                actor = actor ?? string.Empty,
                inputText = inputText ?? string.Empty,
                publicStatus = publicStatus ?? string.Empty
            };

            return AddEntryInternal(entry);
        }

        public int AddEntry(HistoryEntry entry)
        {
            if (entry == null)
            {
                return 0;
            }

            return AddEntryInternal(entry);
        }

        private int AddEntryInternal(HistoryEntry entry)
        {
            if (entry.attemptNumber <= 0)
            {
                entry.attemptNumber = nextAttemptNumber++;
            }
            else
            {
                nextAttemptNumber = Mathf.Max(nextAttemptNumber, entry.attemptNumber + 1);
            }

            entries.Add(entry);
            TrimToMaxRetainedEntries();
            OnChanged?.Invoke();
            return entry.attemptNumber;
        }

        public void Clear()
        {
            ResetWithoutNotify();
            OnChanged?.Invoke();
        }

        private void ResetWithoutNotify()
        {
            entries.Clear();
            nextAttemptNumber = 1;
        }

        private void TrimToMaxRetainedEntries()
        {
            while (entries.Count > maxRetainedEntries)
            {
                entries.RemoveAt(0);
            }
        }
    }
}
