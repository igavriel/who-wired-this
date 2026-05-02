using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Tutorial2
{
    public class CooperativeTutorialPuzzleManager : MonoBehaviour
    {
        [Header("Sequence")]
        [SerializeField] private TutorialPuzzleSequenceSO sequence;
        [SerializeField] private FeedbackMessageSetSO feedbackMessages;

        [Header("Stations")]
        [SerializeField] private PuzzleStationController sideAStation;
        [SerializeField] private PuzzleStationController sideBStation;

        [Header("Shared Displays")]
        [SerializeField] private SharedCoreController sharedCore;
        [SerializeField] private SharedHistoryBoardController sharedHistoryBoard;

        [Header("Operator Link")]
        [SerializeField] private bool requireOperatorPads;
        [SerializeField] private string linkLostMessage = "OPERATOR LINK LOST - CALIBRATION PAUSED";

        [Header("Debug")]
        [SerializeField] private bool enableDebugShortcuts = true;

        private readonly List<PuzzleAttemptRecord> attemptHistory = new List<PuzzleAttemptRecord>();
        private int currentPhaseIndex;
        private bool waitingForInitialize;
        private bool sequenceComplete;
        private bool playerALinked = true;
        private bool playerBLinked = true;

        private PuzzlePhaseSO CurrentPhase =>
            sequence != null
            && sequence.Phases != null
            && currentPhaseIndex >= 0
            && currentPhaseIndex < sequence.Phases.Length
                ? sequence.Phases[currentPhaseIndex]
                : null;

        private void Start()
        {
            AutoResolveReferences();
            if (sideAStation != null) sideAStation.SetManager(this);
            if (sideBStation != null) sideBStation.SetManager(this);
            ResetTutorial();
        }

        private void Update()
        {
            if (!enableDebugShortcuts)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugSubmit("SideA", new[] { "R", "G" });
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugSubmit("SideA", new[] { "G", "R" });
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                TryInitializeNextPhase("SideB", null);
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugSubmit("SideB", new[] { "-", "+" });
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                DebugSubmit("SideB", new[] { "+", "-" });
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                ResetTutorial();
            }
        }

        public void SubmitGuess(string stationId, GameObject interactor, string[] guess)
        {
            if (sequenceComplete || CurrentPhase == null || waitingForInitialize)
            {
                return;
            }

            if (!CanOperateNow())
            {
                sharedCore?.SetPhaseStatus(linkLostMessage);
                return;
            }

            if (CurrentPhase.InputStationId != stationId)
            {
                Debug.Log("[Tutorial2] Guess rejected: wrong station for this phase.");
                return;
            }

            if (guess == null || guess.Length < CurrentPhase.SlotCount)
            {
                Debug.Log("[Tutorial2] Guess rejected: insufficient slots.");
                return;
            }

            if (!CurrentPhase.AllowDuplicates)
            {
                int distinctCount = guess.Take(CurrentPhase.SlotCount).Distinct().Count();
                if (distinctCount < CurrentPhase.SlotCount)
                {
                    Debug.Log("[Tutorial2] Guess rejected: duplicate values not allowed.");
                    return;
                }
            }

            string[] normalizedGuess = guess.Take(CurrentPhase.SlotCount).ToArray();
            PuzzleFeedback feedback = CalculateFeedback(CurrentPhase, normalizedGuess);
            string guessText = string.Join(" ", normalizedGuess);
            string feedbackText = $"{CurrentPhase.Metric1Label} {feedback.Metric1Total} / {CurrentPhase.Metric2Label} {feedback.Metric2Aligned}";
            string note = feedback.Message;

            attemptHistory.Add(new PuzzleAttemptRecord
            {
                PhaseNumber = currentPhaseIndex + 1,
                Actor = CurrentPhase.InputPlayer,
                GuessText = guessText,
                FeedbackText = feedback.IsSuccess ? "Calibrated" : feedbackText,
                Note = feedback.IsSuccess ? CurrentPhase.SuccessMessage : note
            });

            sharedHistoryBoard?.RenderHistory(attemptHistory.ToArray());
            GetStation(CurrentPhase.DiagnosticStationId)?.ShowDiagnostic(CurrentPhase, feedback);

            Debug.Log($"[Tutorial2] Attempt submitted | phase={CurrentPhase.PhaseId} guess={guessText}");
            Debug.Log($"[Tutorial2] Feedback calculated | metric1={feedback.Metric1Total} metric2={feedback.Metric2Aligned}");

            if (feedback.IsSuccess)
            {
                waitingForInitialize = currentPhaseIndex < sequence.Phases.Length - 1;
                sharedCore?.SetSideCalibrated(CurrentPhase.SuccessMessage);

                PuzzleStationController inputStation = GetStation(CurrentPhase.InputStationId);
                PuzzleStationController observerStation = GetStation(CurrentPhase.DiagnosticStationId);
                inputStation?.ShowCalibrated(CurrentPhase.SuccessMessage);
                observerStation?.ShowObserverInstruction(CurrentPhase.ObserverSuccessInstruction);

                Debug.Log($"[Tutorial2] Phase solved | phase={CurrentPhase.PhaseId}");
                if (waitingForInitialize)
                {
                    Debug.Log("[Tutorial2] Transition waiting for initialize.");
                }
                else
                {
                    CompleteSequence();
                    return;
                }
            }

            ApplyPhaseView();
        }

        public void TryInitializeNextPhase(string stationId, GameObject interactor)
        {
            if (!waitingForInitialize || CurrentPhase == null)
            {
                return;
            }

            if (CurrentPhase.DiagnosticStationId != stationId)
            {
                return;
            }

            currentPhaseIndex++;
            waitingForInitialize = false;
            Debug.Log($"[Tutorial2] Current phase started | phase={CurrentPhase?.PhaseId}");
            ApplyPhaseView();
        }

        public void SetOperatorLink(TutorialPlayerSlot slot, bool linked)
        {
            if (slot == TutorialPlayerSlot.PlayerA)
            {
                playerALinked = linked;
            }
            else
            {
                playerBLinked = linked;
            }

            if (requireOperatorPads && !CanOperateNow())
            {
                sharedCore?.SetPhaseStatus(linkLostMessage);
            }
            else if (!sequenceComplete && CurrentPhase != null)
            {
                sharedCore?.SetPhaseStatus(CurrentPhase.PhaseTitle);
            }
        }

        public void ResetTutorial()
        {
            attemptHistory.Clear();
            sharedHistoryBoard?.RenderHistory(attemptHistory.ToArray());
            currentPhaseIndex = 0;
            waitingForInitialize = false;
            sequenceComplete = false;
            playerALinked = true;
            playerBLinked = true;

            if (CurrentPhase != null)
            {
                Debug.Log($"[Tutorial2] Current phase started | phase={CurrentPhase.PhaseId}");
            }

            ApplyPhaseView();
        }

        private void ApplyPhaseView()
        {
            PuzzlePhaseSO phase = CurrentPhase;
            if (phase == null)
            {
                return;
            }

            sideAStation?.ConfigureForPhase(
                phase,
                phase.InputStationId == "SideA",
                phase.DiagnosticStationId == "SideA",
                waitingForInitialize);

            sideBStation?.ConfigureForPhase(
                phase,
                phase.InputStationId == "SideB",
                phase.DiagnosticStationId == "SideB",
                waitingForInitialize);

            if (!sequenceComplete)
            {
                sharedCore?.SetPhaseStatus(phase.PhaseTitle);
            }
        }

        private void CompleteSequence()
        {
            sequenceComplete = true;
            string finalMessage = sequence != null ? sequence.FinalSuccessMessage : "CORE STABILIZED";
            sharedCore?.SetCoreStabilized(finalMessage);
            sharedHistoryBoard?.ShowBanner(finalMessage);
            Debug.Log("[Tutorial2] Core stabilized.");
        }

        private PuzzleFeedback CalculateFeedback(PuzzlePhaseSO phase, string[] guess)
        {
            string[] solution = phase.FixedSolution;
            int slotCount = Mathf.Min(phase.SlotCount, solution.Length, guess.Length);
            int aligned = 0;

            Dictionary<string, int> remainingSolution = new Dictionary<string, int>();
            Dictionary<string, int> remainingGuess = new Dictionary<string, int>();

            for (int i = 0; i < slotCount; i++)
            {
                if (guess[i] == solution[i])
                {
                    aligned++;
                    continue;
                }

                if (!remainingSolution.ContainsKey(solution[i])) remainingSolution[solution[i]] = 0;
                if (!remainingGuess.ContainsKey(guess[i])) remainingGuess[guess[i]] = 0;
                remainingSolution[solution[i]]++;
                remainingGuess[guess[i]]++;
            }

            int recognized = aligned;
            foreach (KeyValuePair<string, int> pair in remainingGuess)
            {
                if (remainingSolution.TryGetValue(pair.Key, out int count))
                {
                    recognized += Mathf.Min(count, pair.Value);
                }
            }

            bool success = aligned == slotCount;
            return new PuzzleFeedback
            {
                Metric1Total = recognized,
                Metric2Aligned = aligned,
                IsSuccess = success,
                Message = ResolveFeedbackMessage(recognized, aligned, slotCount, success)
            };
        }

        private string ResolveFeedbackMessage(int recognized, int aligned, int slotCount, bool success)
        {
            if (success)
            {
                return feedbackMessages != null ? feedbackMessages.SuccessMessage : "Calibration complete.";
            }

            if (recognized == 0 && aligned == 0)
            {
                return feedbackMessages != null ? feedbackMessages.NoMatchMessage : "No usable signal detected.";
            }

            if (recognized == slotCount && aligned == 0)
            {
                return feedbackMessages != null ? feedbackMessages.AllValuesWrongPlaceMessage : "Correct values, wrong order.";
            }

            if (recognized == 1 && aligned == 1)
            {
                return feedbackMessages != null ? feedbackMessages.OneLockedMessage : "One value is locked. One value still needs correction.";
            }

            return feedbackMessages != null ? feedbackMessages.PartialMatchMessage : "Partial signal match detected.";
        }

        private PuzzleStationController GetStation(string stationId)
        {
            if (sideAStation != null && sideAStation.StationId == stationId) return sideAStation;
            if (sideBStation != null && sideBStation.StationId == stationId) return sideBStation;
            return null;
        }

        private bool CanOperateNow()
        {
            return !requireOperatorPads || (playerALinked && playerBLinked);
        }

        private void DebugSubmit(string stationId, string[] guess)
        {
            SubmitGuess(stationId, null, guess);
        }

        private void AutoResolveReferences()
        {
            if (sideAStation == null || sideBStation == null)
            {
                PuzzleStationController[] stations = GetComponentsInChildren<PuzzleStationController>(true);
                for (int i = 0; i < stations.Length; i++)
                {
                    if (stations[i].StationId == "SideA")
                    {
                        sideAStation = stations[i];
                    }
                    else if (stations[i].StationId == "SideB")
                    {
                        sideBStation = stations[i];
                    }
                }
            }

            if (sharedCore == null)
            {
                sharedCore = GetComponentInChildren<SharedCoreController>(true);
            }

            if (sharedHistoryBoard == null)
            {
                sharedHistoryBoard = GetComponentInChildren<SharedHistoryBoardController>(true);
            }
        }
    }
}
