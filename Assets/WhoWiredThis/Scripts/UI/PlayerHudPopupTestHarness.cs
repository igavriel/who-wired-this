using UnityEngine;

namespace WhoWiredThis.UI
{
    /// <summary>
    /// Dev-scene helper for Phase 4A popup testing. Attach only in Split Tutorial_UIRefactor.
    /// Play mode: F9 opens Player A popup, F10 opens Player B popup.
    /// </summary>
    public class PlayerHudPopupTestHarness : MonoBehaviour
    {
        [SerializeField] private PlayerHudView playerHudViewA;
        [SerializeField] private PlayerHudView playerHudViewB;

        [Header("Play Mode Test Keys")]
        [SerializeField] private KeyCode showPopupKeyA = KeyCode.F9;
        [SerializeField] private KeyCode showPopupKeyB = KeyCode.F10;

        [TextArea(2, 4)]
        [SerializeField] private string testMessageA = "Player A — test popup (Display 0). Dismiss with your interact key.";
        [TextArea(2, 4)]
        [SerializeField] private string testMessageB = "Player B — test popup (Display 1). Dismiss with your interact key.";

        void Update()
        {
            if (Input.GetKeyDown(showPopupKeyA))
            {
                ShowTestPopupA();
            }

            if (Input.GetKeyDown(showPopupKeyB))
            {
                ShowTestPopupB();
            }
        }

        [ContextMenu("Show Test Popup A")]
        public void ShowTestPopupA()
        {
            if (playerHudViewA == null)
            {
                Debug.LogWarning("[PlayerHudPopupTestHarness] playerHudViewA is not assigned.", this);
                return;
            }

            playerHudViewA.ShowPopup(testMessageA);
        }

        [ContextMenu("Show Test Popup B")]
        public void ShowTestPopupB()
        {
            if (playerHudViewB == null)
            {
                Debug.LogWarning("[PlayerHudPopupTestHarness] playerHudViewB is not assigned.", this);
                return;
            }

            playerHudViewB.ShowPopup(testMessageB);
        }
    }
}
