using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhoWiredThis.UI
{
    public class MessagePanel : MonoBehaviour
    {
        public static MessagePanel Instance { get; private set; }

        [Header("References")]
        public GameObject panelRoot;
        public TMP_Text messageText;
        public Button closeButton;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            closeButton?.onClick.AddListener(Hide);
            panelRoot?.SetActive(false);
        }

        public void Show(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            panelRoot?.SetActive(true);
        }

        public void Hide() => panelRoot?.SetActive(false);
    }
}
