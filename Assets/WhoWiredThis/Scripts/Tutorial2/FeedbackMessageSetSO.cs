using UnityEngine;

namespace WhoWiredThis.Tutorial2
{
    [CreateAssetMenu(
        fileName = "FeedbackMessageSet",
        menuName = "WhoWiredThis/Tutorial2/Feedback Message Set")]
    public class FeedbackMessageSetSO : ScriptableObject
    {
        [SerializeField] private string noMatchMessage = "No usable signal detected.";
        [SerializeField] private string partialMatchMessage = "Partial signal match detected.";
        [SerializeField] private string allValuesWrongPlaceMessage = "Correct values, wrong order.";
        [SerializeField] private string oneLockedMessage = "One value is locked. One value still needs correction.";
        [SerializeField] private string successMessage = "Calibration complete.";

        public string NoMatchMessage => noMatchMessage;
        public string PartialMatchMessage => partialMatchMessage;
        public string AllValuesWrongPlaceMessage => allValuesWrongPlaceMessage;
        public string OneLockedMessage => oneLockedMessage;
        public string SuccessMessage => successMessage;
    }
}
