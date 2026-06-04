using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Drives SETTINGS (left) and PLACES (middle) result lamps from an opponent puzzle manager snapshot.
    /// Scene-local wiring; uses <see cref="MultiDimension"/> subject indices 0=red, 1=orange, 2=green.
    /// </summary>
    public class SplitMetricResultLightsController : MonoBehaviour
    {
        private const int ColorRed = 0;
        private const int ColorOrange = 1;
        private const int ColorGreen = 2;

        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;
        [SerializeField] private MultiDimension settings;
        [SerializeField] private MultiDimension places;

        [Header("Behavior")]
        [Tooltip("When false, lamps update only on solve attempts (matches commit-only diagnostics).")]
        [SerializeField] private bool updateContinuously;
        [SerializeField] private AllowedPlayerTag visibleToPlayer = AllowedPlayerTag.Any_Player;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }

            if (updateContinuously)
            {
                RefreshLamps(force: true);
                return;
            }

            if (puzzleManager != null && puzzleManager.Solved)
            {
                ApplyBoth(ColorGreen);
            }
            else
            {
                ApplyBoth(ColorRed);
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void Update()
        {
            if (!updateContinuously)
            {
                return;
            }

            RefreshLamps(force: false);
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult _)
        {
            RefreshLamps(force: true);
        }

        private void RefreshLamps(bool force)
        {
            if (puzzleManager == null)
            {
                return;
            }

            if (puzzleManager.Solved)
            {
                ApplyBoth(ColorGreen);
                return;
            }

            if (!puzzleManager.TryGetDiagnosticSnapshot(out int settingsOk, out int placesOk, out int total))
            {
                ApplyBoth(ColorRed);
                return;
            }

            ResolveColors(settingsOk, placesOk, total, out int settingsColor, out int placesColor);
            ApplyColor(settings, settingsColor);
            ApplyColor(places, placesColor);
        }

        private static void ResolveColors(int settingsOk, int placesOk, int total, out int settingsColor, out int placesColor)
        {
            if (total > 0 && placesOk == total)
            {
                settingsColor = ColorGreen;
                placesColor = ColorGreen;
                return;
            }

            if (total > 0 && settingsOk == total && placesOk == 0)
            {
                settingsColor = ColorOrange;
                placesColor = ColorOrange;
                return;
            }

            if (settingsOk == 1 && placesOk == 1)
            {
                settingsColor = ColorRed;
                placesColor = ColorGreen;
                return;
            }

            if (settingsOk == 1 && placesOk == 0)
            {
                settingsColor = ColorOrange;
                placesColor = ColorRed;
                return;
            }

            settingsColor = ColorRed;
            placesColor = ColorRed;
        }

        private void ApplyBoth(int colorIndex)
        {
            ApplyColor(settings, colorIndex);
            ApplyColor(places, colorIndex);
        }

        private void ApplyColor(MultiDimension lamp, int colorIndex)
        {
            if (lamp == null)
            {
                Debug.LogWarning($"[{nameof(SplitMetricResultLightsController)}] Missing MultiDimension reference on '{name}'.", this);
                return;
            }

            int subjectCount = lamp.SubjectCount;
            if (subjectCount <= 0)
            {
                Debug.LogWarning($"[{nameof(SplitMetricResultLightsController)}] '{lamp.name}' has no subjects.", this);
                return;
            }

            int clamped = Mathf.Clamp(colorIndex, 0, subjectCount - 1);
            lamp.SetSelection(visibleToPlayer, clamped);
        }
    }
}
