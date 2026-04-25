using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Core
{
    public class SceneHotkeySwitcher : MonoBehaviour
    {
        [Serializable]
        private struct SceneHotkeyBinding
        {
            [SerializeField] private string sceneName;
            [SerializeField] private KeyCode shortcut;

            public string SceneName => sceneName;
            public KeyCode Shortcut => shortcut;
        }

        [Header("Bindings")]
        [SerializeField] private SceneHotkeyBinding[] bindings = Array.Empty<SceneHotkeyBinding>();
        [SerializeField] private bool ignoreWhenAlreadyInTargetScene = true;

        [Header("Boss Key")]
        [SerializeField] private KeyCode bossKey = KeyCode.F12;
        [SerializeField] private bool hotkeysEnabled = true;

        private void Update()
        {
            if (Input.GetKeyDown(bossKey))
            {
                hotkeysEnabled = !hotkeysEnabled;
                Debug.Log($"[SceneHotkeySwitcher] Hotkeys {(hotkeysEnabled ? "enabled" : "disabled")}.");
            }

            if (!hotkeysEnabled || bindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                SceneHotkeyBinding binding = bindings[i];
                if (!Input.GetKeyDown(binding.Shortcut))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.SceneName))
                {
                    Debug.LogWarning($"[SceneHotkeySwitcher] Binding {i} has an empty scene name.");
                    continue;
                }

                string activeSceneName = SceneManager.GetActiveScene().name;
                if (ignoreWhenAlreadyInTargetScene && string.Equals(activeSceneName, binding.SceneName, StringComparison.Ordinal))
                {
                    Debug.Log($"[SceneHotkeySwitcher] Already in scene '{binding.SceneName}'.");
                    continue;
                }

                if (!Application.CanStreamedLevelBeLoaded(binding.SceneName))
                {
                    Debug.LogWarning($"[SceneHotkeySwitcher] Scene '{binding.SceneName}' is not in Build Settings.");
                    continue;
                }

                Debug.Log($"[SceneHotkeySwitcher] Loading scene '{binding.SceneName}' from shortcut {binding.Shortcut}.");
                SceneManager.LoadScene(binding.SceneName, LoadSceneMode.Single);
                return;
            }
        }
    }
}
