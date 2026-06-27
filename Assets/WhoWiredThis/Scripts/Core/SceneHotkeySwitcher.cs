using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;

namespace WhoWiredThis.Core
{
    public class SceneHotkeySwitcher : MonoBehaviour
    {
        [Serializable]
        private struct SceneHotkeyBinding
        {
            [SerializeField] private PlaytestSceneId sceneId;
            [SerializeField] private KeyCode shortcut;

            public PlaytestSceneId SceneId => sceneId;
            public KeyCode Shortcut => shortcut;
        }

        [Header("Flow")]
        [SerializeField] private PlaytestSceneFlowConfigSO flowConfig;

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

            if (flowConfig == null)
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

                if (binding.SceneId == PlaytestSceneId.None)
                {
                    Debug.LogWarning($"[SceneHotkeySwitcher] Binding {i} has no scene id.");
                    continue;
                }

                if (!flowConfig.TryGetSceneName(binding.SceneId, out string sceneName))
                {
                    Debug.LogWarning($"[SceneHotkeySwitcher] Binding {i} scene id '{binding.SceneId}' is not configured.");
                    continue;
                }

                string activeSceneName = SceneManager.GetActiveScene().name;
                if (ignoreWhenAlreadyInTargetScene &&
                    string.Equals(activeSceneName, sceneName, StringComparison.Ordinal))
                {
                    Debug.Log($"[SceneHotkeySwitcher] Already in scene '{sceneName}'.");
                    continue;
                }

                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogWarning($"[SceneHotkeySwitcher] Scene '{sceneName}' is not in Build Settings.");
                    continue;
                }

                Debug.Log($"[SceneHotkeySwitcher] Loading scene '{sceneName}' from shortcut {binding.Shortcut}.");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                return;
            }
        }
    }
}
