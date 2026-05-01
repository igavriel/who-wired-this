using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Tutorial
{
    public class TutorialStationConfigurator : MonoBehaviour
    {
        [SerializeField] private TutorialStationConfig stationConfig;
        [SerializeField] private TutorialModuleState[] modules;
        [SerializeField] private TutorialClueBoardDisplay clueBoard;

        private void Awake()
        {
            ApplyConfiguration();
        }

        public TutorialModuleState[] Modules => modules;

        public void ApplyConfiguration()
        {
            if (stationConfig == null || modules == null)
            {
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                TutorialModuleState module = modules[i];
                if (module == null)
                {
                    continue;
                }

                string moduleId = ResolveModuleId(i);
                int initialState = ResolveInitialState(i);
                module.Configure(moduleId, stationConfig.OwnerSlot, initialState);

                DimensionVisibilityObject visibility = module.GetComponent<DimensionVisibilityObject>();
                if (visibility != null)
                {
                    DimensionVisibilityMode mode = stationConfig.OwnerSlot == TutorialPlayerSlot.PlayerA
                        ? DimensionVisibilityMode.Player_A_Visibility
                        : DimensionVisibilityMode.Player_B_Visibility;

                    // Keep the visibility component enabled and let it auto-apply in Awake.
                    // This local variable usage preserves intent for inspector setup and debugging.
                    _ = mode;
                }
            }

            if (clueBoard != null)
            {
                clueBoard.SetClue(stationConfig.ClueForOtherPlayer);
            }
        }

        private string ResolveModuleId(int index)
        {
            if (stationConfig.ModuleIds == null || index < 0 || index >= stationConfig.ModuleIds.Length)
            {
                string prefix = stationConfig.OwnerSlot == TutorialPlayerSlot.PlayerA ? "A" : "B";
                return $"{prefix}{index + 1}";
            }

            return stationConfig.ModuleIds[index];
        }

        private int ResolveInitialState(int index)
        {
            if (stationConfig.TargetStates == null || index < 0 || index >= stationConfig.TargetStates.Length)
            {
                return 0;
            }

            return stationConfig.TargetStates[index];
        }
    }
}
