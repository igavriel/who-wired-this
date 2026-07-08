using WhoWiredThis.Core;

namespace WhoWiredThis.Scenes
{
    /// <summary>
    /// Which player operates the staged puzzle for the current load of a scene.
    /// </summary>
    public enum SceneRolePhase
    {
        /// <summary>Phase 1 (fresh run): Player A operates, Player B reads the diagnostic.</summary>
        PlayerAOperator = 0,

        /// <summary>Phase 2 (after the cut-scene round trip): Player B operates, Player A reads the diagnostic.</summary>
        PlayerBOperator = 1,
    }

    /// <summary>
    /// Static, play-session-scoped operator phase that persists across a role-swap cut-scene
    /// round trip (e.g. Puzzle -> CutScene-*-Swap -> Puzzle). Mirrors the project's existing
    /// cross-scene persistence pattern (see <see cref="PlaytestRunTotal"/>);
    /// there is no DontDestroyOnLoad. Defaults to Phase 1 so loading a scene directly behaves normally.
    /// </summary>
    public static class SceneRoleState
    {
        public static SceneRolePhase Phase { get; private set; } = SceneRolePhase.PlayerAOperator;

        /// <summary>True once the role-swap round trip has handed control to Player B.</summary>
        public static bool HasSwapped => Phase == SceneRolePhase.PlayerBOperator;

        /// <summary>Reset to Phase 1. Call at run start and when returning to the main menu.</summary>
        public static void Reset()
        {
            Phase = SceneRolePhase.PlayerAOperator;
        }

        /// <summary>Flag the next scene load as Phase 2 (Player B operator). Set before loading the swap cut scene.</summary>
        public static void MarkSwapToPlayerBOperator()
        {
            Phase = SceneRolePhase.PlayerBOperator;
        }

        /// <summary>
        /// Resets operator phase when entering a staged scene unless returning from its swap cut scene.
        /// </summary>
        public static void ConfigureForSceneLoad(PlaytestSceneId loadedScene, PlaytestSceneId? previousScene)
        {
            switch (loadedScene)
            {
                case PlaytestSceneId.Tutorial:
                    if (previousScene == PlaytestSceneId.CutSceneTutorialSwap)
                    {
                        return;
                    }

                    Reset();
                    break;

                case PlaytestSceneId.PuzzlePipes:
                    if (previousScene == PlaytestSceneId.CutScenePipeSwap)
                    {
                        return;
                    }

                    Reset();
                    break;
            }
        }
    }
}
