using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Tutorial2
{
    public class PuzzleStationController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string stationId = "SideA";

        [Header("References")]
        [SerializeField] private CooperativeTutorialPuzzleManager manager;
        [SerializeField] private TMP_Text stationHeaderText;
        [SerializeField] private TMP_Text diagnosticTitleText;
        [SerializeField] private TMP_Text metricLine1Text;
        [SerializeField] private TMP_Text metricLine2Text;
        [SerializeField] private TMP_Text observerMessageText;
        [SerializeField] private TMP_Text phaseLocalStatusText;

        [Header("Controls")]
        [SerializeField] private PuzzleInputSlotController[] slots;
        [SerializeField] private PuzzleOptionButtonController[] optionButtons;
        [SerializeField] private MachineActionButtonController activateButton;
        [SerializeField] private MachineActionButtonController initializeButton;
        [SerializeField] private MachineActionButtonController resetButton;
        [SerializeField] private GameObject inputAreaRoot;
        [SerializeField] private GameObject diagnosticAreaRoot;

        private PuzzlePhaseSO currentPhase;
        private readonly List<string> currentGuess = new List<string>();
        private int nextSlotIndex;
        private bool isInputStation;
        private bool isDiagnosticStation;
        private bool isWaitingForInitialize;

        public string StationId => stationId;

        private void Awake()
        {
            if (stationId == "SideA" && transform.parent != null && transform.parent.name == "SideB")
            {
                stationId = "SideB";
            }

            AutoResolveReferences();
            ConfigureButtons();
            ClearLocalSelections();
            SetLocalStatus("STANDBY");
        }

        public void SetManager(CooperativeTutorialPuzzleManager puzzleManager)
        {
            manager = puzzleManager;
        }

        public void ConfigureForPhase(
            PuzzlePhaseSO phase,
            bool inputMode,
            bool diagnosticMode,
            bool waitingForInitialize)
        {
            currentPhase = phase;
            isInputStation = inputMode;
            isDiagnosticStation = diagnosticMode;
            isWaitingForInitialize = waitingForInitialize;
            nextSlotIndex = 0;

            if (stationHeaderText != null)
            {
                stationHeaderText.text = phase != null ? phase.PhaseTitle : "Calibration Station";
            }

            if (diagnosticTitleText != null)
            {
                diagnosticTitleText.text = diagnosticMode ? "Diagnostic Feed" : "Local Display";
            }

            bool inputEnabled = isInputStation && !isWaitingForInitialize;
            if (inputAreaRoot != null) inputAreaRoot.SetActive(true);
            if (diagnosticAreaRoot != null) diagnosticAreaRoot.SetActive(true);

            SetOptionButtonsFromValueSet(phase != null ? phase.ValueSet : null, inputEnabled);
            if (activateButton != null) activateButton.SetInteractable(inputEnabled);
            if (initializeButton != null) initializeButton.SetInteractable(isDiagnosticStation && isWaitingForInitialize);
            if (resetButton != null) resetButton.SetInteractable(true);

            if (isInputStation && !isWaitingForInitialize)
            {
                SetLocalStatus("Awaiting input...");
            }
            else if (isDiagnosticStation && isWaitingForInitialize)
            {
                SetLocalStatus("Observer: initialize next phase.");
            }
            else if (isDiagnosticStation)
            {
                SetLocalStatus("Diagnostic relay online.");
            }
            else
            {
                SetLocalStatus("Station idle.");
            }

            ClearDiagnosticText();
            ClearLocalSelections();
        }

        public void OnOptionSelected(int optionIndex, string valueId, GameObject interactor)
        {
            if (!isInputStation || isWaitingForInitialize || currentPhase == null || currentPhase.ValueSet == null)
            {
                return;
            }

            if (!currentPhase.ValueSet.TryGetById(valueId, out PuzzleValueSetSO.PuzzleValueDefinition value))
            {
                return;
            }

            if (!currentPhase.AllowDuplicates && currentGuess.Contains(valueId))
            {
                SetLocalStatus("Duplicate values rejected.");
                return;
            }

            EnsureGuessLength(currentPhase.SlotCount);
            int index = Mathf.Clamp(nextSlotIndex, 0, currentPhase.SlotCount - 1);
            currentGuess[index] = valueId;
            if (index < slots.Length)
            {
                slots[index].SetValue(value);
            }

            nextSlotIndex = (nextSlotIndex + 1) % currentPhase.SlotCount;
            SetLocalStatus("Signal staged.");
        }

        public void OnActivatePressed(GameObject interactor)
        {
            if (!isInputStation || currentPhase == null)
            {
                return;
            }

            EnsureGuessLength(currentPhase.SlotCount);
            manager?.SubmitGuess(stationId, interactor, currentGuess.ToArray());
        }

        public void OnInitializePressed(GameObject interactor)
        {
            if (!isDiagnosticStation || !isWaitingForInitialize)
            {
                return;
            }

            manager?.TryInitializeNextPhase(stationId, interactor);
        }

        public void OnResetPressed(GameObject interactor)
        {
            manager?.ResetTutorial();
        }

        public void ShowDiagnostic(PuzzlePhaseSO phase, PuzzleFeedback feedback)
        {
            if (!isDiagnosticStation || phase == null)
            {
                return;
            }

            if (metricLine1Text != null) metricLine1Text.text = $"{phase.Metric1Label}: {feedback.Metric1Total}";
            if (metricLine2Text != null) metricLine2Text.text = $"{phase.Metric2Label}: {feedback.Metric2Aligned}";
            if (observerMessageText != null) observerMessageText.text = feedback.Message;
        }

        public void ShowObserverInstruction(string instruction)
        {
            if (observerMessageText != null)
            {
                observerMessageText.text = instruction;
            }
        }

        public void ShowCalibrated(string successMessage)
        {
            SetLocalStatus(successMessage);
        }

        private void ConfigureButtons()
        {
            if (activateButton != null)
            {
                activateButton.Configure(this, MachineActionType.Activate);
            }

            if (initializeButton != null)
            {
                initializeButton.Configure(this, MachineActionType.InitializeNextPhase);
            }

            if (resetButton != null)
            {
                resetButton.Configure(this, MachineActionType.Reset);
            }
        }

        private void SetOptionButtonsFromValueSet(PuzzleValueSetSO valueSet, bool canInteract)
        {
            PuzzleValueSetSO.PuzzleValueDefinition[] values = valueSet != null ? valueSet.Values : null;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                PuzzleOptionButtonController button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                PuzzleValueSetSO.PuzzleValueDefinition value = values != null && i < values.Length ? values[i] : null;
                button.Configure(this, value, i);
                button.SetInteractable(canInteract && value != null);
            }
        }

        private void EnsureGuessLength(int slotCount)
        {
            while (currentGuess.Count < slotCount)
            {
                currentGuess.Add(string.Empty);
            }
        }

        private void ClearLocalSelections()
        {
            currentGuess.Clear();
            nextSlotIndex = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Clear();
                }
            }
        }

        private void ClearDiagnosticText()
        {
            if (metricLine1Text != null) metricLine1Text.text = "-";
            if (metricLine2Text != null) metricLine2Text.text = "-";
            if (observerMessageText != null) observerMessageText.text = "-";
        }

        private void SetLocalStatus(string text)
        {
            if (phaseLocalStatusText != null)
            {
                phaseLocalStatusText.text = text;
            }
        }

        private void AutoResolveReferences()
        {
            if (inputAreaRoot == null)
            {
                Transform input = transform.Find("InputArea");
                if (input != null) inputAreaRoot = input.gameObject;
            }

            if (diagnosticAreaRoot == null)
            {
                Transform diagnostic = transform.Find("DiagnosticArea");
                if (diagnostic != null) diagnosticAreaRoot = diagnostic.gameObject;
            }

            if (slots == null || slots.Length == 0)
            {
                slots = GetComponentsInChildren<PuzzleInputSlotController>(true);
            }

            if (optionButtons == null || optionButtons.Length == 0)
            {
                optionButtons = GetComponentsInChildren<PuzzleOptionButtonController>(true);
            }

            if (activateButton == null)
            {
                Transform activate = transform.Find("InputArea/ActivateButton");
                if (activate != null) activateButton = activate.GetComponent<MachineActionButtonController>();
            }

            if (initializeButton == null)
            {
                Transform initialize = transform.Find("InputArea/InitializeButton");
                if (initialize != null) initializeButton = initialize.GetComponent<MachineActionButtonController>();
            }

            if (resetButton == null)
            {
                Transform reset = transform.Find("InputArea/ResetButton");
                if (reset != null) resetButton = reset.GetComponent<MachineActionButtonController>();
            }

            stationHeaderText ??= FindText("StationHeaderText");
            phaseLocalStatusText ??= FindText("PhaseLocalStatusText");
            diagnosticTitleText ??= FindText("DiagnosticArea/DiagnosticTitleText");
            metricLine1Text ??= FindText("DiagnosticArea/MetricLine1Text");
            metricLine2Text ??= FindText("DiagnosticArea/MetricLine2Text");
            observerMessageText ??= FindText("DiagnosticArea/ObserverMessageText");
        }

        private TMP_Text FindText(string relativePath)
        {
            Transform child = transform.Find(relativePath);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
