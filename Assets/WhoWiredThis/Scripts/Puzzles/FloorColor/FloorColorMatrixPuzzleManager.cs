using System;
using System.Collections.Generic;
using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Core;
using WhoWiredThis.Data.Puzzles;
using WhoWiredThis.Puzzles.Common;

namespace WhoWiredThis.Puzzles.FloorColor
{
    public class FloorColorMatrixPuzzleManager : MonoBehaviour, IPuzzleManager
    {
        [Serializable]
        private class SwitchRow
        {
            public PolaritySwitchController[] switches = Array.Empty<PolaritySwitchController>();
        }

        [Header("Config")]
        [SerializeField] private Matrix_PuzzleConfigSO config;

        [Header("Switch Matrix")]
        [SerializeField] private Transform matrixRoot;
        [SerializeField] private bool autoCollectSwitchRows = true;
        [SerializeField] private SwitchRow[] switchRows = Array.Empty<SwitchRow>();

        [Header("State (read-only)")]
        [SerializeField, Tooltip("Increments on each failed ENGAGE press.")]
        private int attempts;

        private PuzzleScoreSession scoreSession;

        public bool IsSolved => scoreSession != null && scoreSession.IsSolved;
        public int Attempts => scoreSession != null ? scoreSession.Attempts : attempts;
        public int HintTriggerAttempt => scoreSession != null ? scoreSession.HintTriggerAttempt : 0;

        public event Action OnSuccess;
        public event Action<int> OnFailure;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[FloorColorMatrixPuzzleManager] Missing puzzle config.", this);
                return;
            }

            if (autoCollectSwitchRows)
            {
                AutoCollectRowsFromHierarchy();
            }

            scoreSession = new PuzzleScoreSession(
                config.startScore,
                config.penaltyFreeAttempts,
                config.penaltyPerAttempt,
                config.minScore,
                config.hintTriggerAttempt);
        }

        private void AutoCollectRowsFromHierarchy()
        {
            Transform root = matrixRoot != null ? matrixRoot : transform;
            if (root.childCount == 0)
            {
                return;
            }

            List<Transform> rowRoots = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponentInChildren<PolaritySwitchController>(true) != null)
                {
                    rowRoots.Add(child);
                }
            }

            if (rowRoots.Count == 0)
            {
                return;
            }

            rowRoots.Sort((a, b) => a.position.z.CompareTo(b.position.z));
            switchRows = new SwitchRow[rowRoots.Count];

            for (int rowIndex = 0; rowIndex < rowRoots.Count; rowIndex++)
            {
                PolaritySwitchController[] switchesInRow = rowRoots[rowIndex].GetComponentsInChildren<PolaritySwitchController>(true);
                Array.Sort(switchesInRow, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                switchRows[rowIndex] = new SwitchRow { switches = switchesInRow };
            }
        }

        public int ComputeCurrentScore()
        {
            return scoreSession != null ? scoreSession.ComputeCurrentScore() : 0;
        }

        public bool TryEngage()
        {
            if (scoreSession == null)
            {
                Debug.LogWarning("[FloorColorMatrixPuzzleManager] TryEngage called without an initialized score session.", this);
                return false;
            }

            bool solvedNow = scoreSession.TryEngage(
                CheckSolution,
                HandleSuccess,
                HandleFailure);

            attempts = scoreSession.Attempts;
            return solvedNow;
        }

        private void HandleSuccess(int score)
        {
            ScoreManager.Instance?.SetScore(score);
            GameManager.Instance?.SolvePuzzle();
            OnSuccess?.Invoke();
        }

        private void HandleFailure(int failedAttempts)
        {
            attempts = failedAttempts;
            OnFailure?.Invoke(failedAttempts);
        }

        private bool CheckSolution()
        {
            if (config == null || config.solutionRows == null)
            {
                Debug.LogWarning("[FloorColorMatrixPuzzleManager] Missing solution rows in config.", this);
                return false;
            }

            if (switchRows == null || switchRows.Length != config.solutionRows.Length)
            {
                Debug.LogWarning("[FloorColorMatrixPuzzleManager] Matrix row count mismatch between scene and config.", this);
                return false;
            }

            for (int rowIndex = 0; rowIndex < switchRows.Length; rowIndex++)
            {
                SwitchRow row = switchRows[rowIndex];
                Matrix_PuzzleConfigSO.PolaritySolutionRow solutionRow = config.solutionRows[rowIndex];

                if (row == null || row.switches == null || solutionRow == null || solutionRow.values == null)
                {
                    Debug.LogWarning($"[FloorColorMatrixPuzzleManager] Row {rowIndex} has null scene/config references.", this);
                    return false;
                }

                if (row.switches.Length != solutionRow.values.Length)
                {
                    Debug.LogWarning($"[FloorColorMatrixPuzzleManager] Row {rowIndex} column count mismatch between scene and config.", this);
                    return false;
                }

                for (int columnIndex = 0; columnIndex < row.switches.Length; columnIndex++)
                {
                    PolaritySwitchController currentSwitch = row.switches[columnIndex];
                    if (currentSwitch == null)
                    {
                        Debug.LogWarning($"[FloorColorMatrixPuzzleManager] Null switch at row {rowIndex}, column {columnIndex}.", this);
                        return false;
                    }

                    if (currentSwitch.CurrentState != solutionRow.values[columnIndex])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
