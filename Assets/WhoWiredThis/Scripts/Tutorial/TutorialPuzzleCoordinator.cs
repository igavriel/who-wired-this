using UnityEngine;

namespace WhoWiredThis.Tutorial
{
    public class TutorialPuzzleCoordinator : MonoBehaviour
    {
        [Header("Station Configs")]
        [SerializeField] private TutorialStationConfig playerAConfig;
        [SerializeField] private TutorialStationConfig playerBConfig;
        [SerializeField] private int[] fallbackTargetA = { 0, 1, 2 };
        [SerializeField] private int[] fallbackTargetB = { 0, 1, 2 };

        [Header("Runtime Modules")]
        [SerializeField] private TutorialModuleState[] playerAModules;
        [SerializeField] private TutorialModuleState[] playerBModules;

        [Header("Door")]
        [SerializeField] private TutorialDoorController exitDoor;
        [SerializeField] private bool oneWayUnlock = true;

        private bool solvedOnce;

        private void OnEnable()
        {
            AutoResolveReferencesIfNeeded();
            Bind(playerAModules);
            Bind(playerBModules);
            Reevaluate();
        }

        private void OnDisable()
        {
            Unbind(playerAModules);
            Unbind(playerBModules);
        }

        public void SetModules(
            TutorialModuleState[] aModules,
            TutorialModuleState[] bModules)
        {
            Unbind(playerAModules);
            Unbind(playerBModules);

            playerAModules = aModules;
            playerBModules = bModules;

            Bind(playerAModules);
            Bind(playerBModules);
            Reevaluate();
        }

        private void Bind(TutorialModuleState[] modules)
        {
            if (modules == null)
            {
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null)
                {
                    modules[i].StateChanged += OnModuleStateChanged;
                }
            }
        }

        private void Unbind(TutorialModuleState[] modules)
        {
            if (modules == null)
            {
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null)
                {
                    modules[i].StateChanged -= OnModuleStateChanged;
                }
            }
        }

        private void OnModuleStateChanged(TutorialModuleState module)
        {
            Reevaluate();
        }

        public void Reevaluate()
        {
            bool aMatch = MatchesConfig(playerAModules, playerAConfig, fallbackTargetA);
            bool bMatch = MatchesConfig(playerBModules, playerBConfig, fallbackTargetB);
            bool solved = aMatch && bMatch;

            if (solved && oneWayUnlock)
            {
                solvedOnce = true;
            }

            bool doorUnlocked = oneWayUnlock ? solvedOnce : solved;
            if (exitDoor != null)
            {
                exitDoor.SetUnlocked(doorUnlocked);
            }
        }

        private void AutoResolveReferencesIfNeeded()
        {
            if (exitDoor == null)
            {
                exitDoor = FindFirstObjectByType<TutorialDoorController>();
            }

            if (playerAModules == null || playerAModules.Length == 0)
            {
                playerAModules = FindModulesByPrefix('A');
            }

            if (playerBModules == null || playerBModules.Length == 0)
            {
                playerBModules = FindModulesByPrefix('B');
            }
        }

        private static TutorialModuleState[] FindModulesByPrefix(char prefix)
        {
            TutorialModuleState[] allModules = FindObjectsByType<TutorialModuleState>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            TutorialModuleState[] filtered = new TutorialModuleState[3];
            int count = 0;
            for (int i = 0; i < allModules.Length; i++)
            {
                TutorialModuleState module = allModules[i];
                if (module == null || string.IsNullOrEmpty(module.ModuleId))
                {
                    continue;
                }

                if (module.ModuleId[0] != prefix)
                {
                    continue;
                }

                if (count < filtered.Length)
                {
                    filtered[count] = module;
                    count++;
                }
            }

            return filtered;
        }

        private static bool MatchesConfig(
            TutorialModuleState[] modules,
            TutorialStationConfig config,
            int[] fallbackTargets)
        {
            if (modules == null)
            {
                return false;
            }

            int[] targets = config != null && config.TargetStates != null && config.TargetStates.Length > 0
                ? config.TargetStates
                : fallbackTargets;

            if (targets == null || modules.Length < targets.Length)
            {
                return false;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (modules[i] == null || modules[i].CurrentState != targets[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
