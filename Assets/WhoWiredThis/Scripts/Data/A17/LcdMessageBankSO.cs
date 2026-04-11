using UnityEngine;

namespace WhoWiredThis.Data.A17
{
    [CreateAssetMenu(menuName = "WhoWiredThis/A17/LCD Message Bank", fileName = "A17_LcdMessageBank")]
    public class LcdMessageBankSO : ScriptableObject
    {
        [Header("LCD Screen Messages")]
        [TextArea(2, 4)]
        public string idleMessage = "UNIT A17 - POLARITY CONTROL\nSET SWITCH CONFIGURATION\nAWAITING ENGAGE";

        [TextArea(2, 4)]
        public string failureMessage = "POLARITY ERROR\nCONFIGURATION REJECTED\nRECALIBRATE AND RETRY";

        [TextArea(2, 4)]
        public string successMessage = "POLARITY ENGAGED\nENERGY MATRIX STABLE\nUNIT A17 ONLINE";

        [Header("Engage Button Failure Messages (cycled on each fail)")]
        [TextArea(1, 3)]
        public string[] engageFailMessages =
        {
            "POLARITY ERROR: Configuration matrix unstable. Adjust and retry.",
            "SYSTEM ALERT: Incorrect polarity pattern detected. Recalibrate.",
            "UNIT A17: Energy alignment rejected. Check switch settings.",
            "WARNING: Polarity mismatch on one or more channels. Reset and retry.",
            "ERROR 0x17A: Invalid polarity configuration. Review the diagram.",
        };
    }
}
