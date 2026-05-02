using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    [CreateAssetMenu(
        fileName = "TutorialPuzzleSequence",
        menuName = "WhoWiredThis/Tutorial2/Puzzle Sequence")]
    public class TutorialPuzzleSequenceSO : ScriptableObject
    {
        [SerializeField] private string sequenceTitle = "Cooperative Calibration Tutorial";
        [SerializeField] private PuzzlePhaseSO[] phases;
        [SerializeField] private string finalSuccessMessage = "CORE STABILIZED";

        public string SequenceTitle => sequenceTitle;
        public PuzzlePhaseSO[] Phases => phases;
        public string FinalSuccessMessage => finalSuccessMessage;
    }
}
