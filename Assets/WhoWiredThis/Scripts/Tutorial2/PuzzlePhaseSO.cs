using UnityEngine;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Tutorial2
{
    [CreateAssetMenu(
        fileName = "PuzzlePhase",
        menuName = "WhoWiredThis/Tutorial2/Puzzle Phase")]
    public class PuzzlePhaseSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string phaseId = "phase_id";
        [SerializeField] private string phaseTitle = "Calibration Phase";

        [Header("Players and Stations")]
        [SerializeField] private TutorialPlayerSlot inputPlayer = TutorialPlayerSlot.PlayerA;
        [SerializeField] private TutorialPlayerSlot observerPlayer = TutorialPlayerSlot.PlayerB;
        [SerializeField] private string inputStationId = "SideA";
        [SerializeField] private string diagnosticStationId = "SideB";

        [Header("Puzzle")]
        [SerializeField] private PuzzleValueSetSO valueSet;
        [SerializeField] private int slotCount = 2;
        [SerializeField] private string[] fixedSolution = { "G", "R" };
        [SerializeField] private bool allowDuplicates;

        [Header("Feedback Labels")]
        [SerializeField] private string metric1Label = "Recognized";
        [SerializeField] private string metric2Label = "Aligned";

        [Header("Messages")]
        [SerializeField] private string successMessage = "A-SIDE CALIBRATED";
        [SerializeField] private string observerSuccessInstruction = "Initialize next phase.";

        public string PhaseId => phaseId;
        public string PhaseTitle => phaseTitle;
        public TutorialPlayerSlot InputPlayer => inputPlayer;
        public TutorialPlayerSlot ObserverPlayer => observerPlayer;
        public string InputStationId => inputStationId;
        public string DiagnosticStationId => diagnosticStationId;
        public PuzzleValueSetSO ValueSet => valueSet;
        public int SlotCount => slotCount;
        public string[] FixedSolution => fixedSolution;
        public bool AllowDuplicates => allowDuplicates;
        public string Metric1Label => metric1Label;
        public string Metric2Label => metric2Label;
        public string SuccessMessage => successMessage;
        public string ObserverSuccessInstruction => observerSuccessInstruction;
    }
}
