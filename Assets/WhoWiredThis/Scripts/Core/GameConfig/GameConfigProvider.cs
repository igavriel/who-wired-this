using UnityEngine;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Registers the active <see cref="GameConfigSO"/> for scene flow, scoring, and level countdown.
    /// Lives on the Managers prefab with a serialized config asset reference.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameConfigProvider : MonoBehaviour
    {
        private static GameConfigSO active;
        private static GameConfigSO fallback;
        private static GameConfigProvider instance;

        [SerializeField]
        private GameConfigSO config;

        public static GameConfigProvider Instance => instance;

        public static GameConfigSO Active
        {
            get
            {
                if (active != null)
                {
                    return active;
                }

                if (instance != null && instance.config != null)
                {
                    return instance.config;
                }

                return Fallback;
            }
        }

        private static GameConfigSO Fallback
        {
            get
            {
                if (fallback != null)
                {
                    return fallback;
                }

                fallback = ScriptableObject.CreateInstance<GameConfigSO>();
                fallback.hideFlags = HideFlags.HideAndDontSave;
                fallback.SetDefaultsForCurrentChain();
                return fallback;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            active = null;
            instance = null;
            fallback = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[GameConfigProvider] Duplicate instance; keeping first.", this);
                return;
            }

            instance = this;
            active = config;
            if (config == null)
            {
                Debug.LogWarning("[GameConfigProvider] config is not assigned; using runtime defaults.", this);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (active == config)
            {
                active = null;
            }
        }
    }
}
