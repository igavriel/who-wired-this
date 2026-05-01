using UnityEngine;

namespace WhoWiredThis.Tutorial
{
    [CreateAssetMenu(
        fileName = "TutorialStationConfig",
        menuName = "WhoWiredThis/Tutorial/Station Config")]
    public class TutorialStationConfig : ScriptableObject
    {
        [Header("Station Identity")]
        [SerializeField] private TutorialPlayerSlot ownerSlot = TutorialPlayerSlot.PlayerA;
        [SerializeField] private string[] moduleIds = { "A1", "A2", "A3" };

        [Header("Solution Data")]
        [SerializeField] private int[] targetStates = { 0, 1, 2 };

        [Header("Cross Clue")]
        [TextArea(2, 5)]
        [SerializeField] private string clueForOtherPlayer = "Set partner modules to [0,1,2].";

        public TutorialPlayerSlot OwnerSlot => ownerSlot;
        public string[] ModuleIds => moduleIds;
        public int[] TargetStates => targetStates;
        public string ClueForOtherPlayer => clueForOtherPlayer;
    }
}
