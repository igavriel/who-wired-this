namespace WhoWiredThis.Tutorial
{
    /// <summary>
    /// Which player operates the tutorial puzzle for the current load of the Tutorial scene.
    /// </summary>
    public enum TutorialRolePhase
    {
        /// <summary>Phase 1 (fresh run): Player A operates, Player B reads the diagnostic.</summary>
        PlayerAOperator = 0,

        /// <summary>Phase 2 (after the cut-scene round trip): Player B operates, Player A reads the diagnostic.</summary>
        PlayerBOperator = 1,
    }

    /// <summary>
    /// Static, play-session-scoped tutorial phase that persists across the role-swap cut-scene
    /// round trip (Tutorial -> CutScene-Tutorial-Swap -> Tutorial). Mirrors the project's existing
    /// cross-scene persistence pattern (see <see cref="WhoWiredThis.Core.PlaytestRunTotal"/>);
    /// there is no DontDestroyOnLoad. Defaults to Phase 1 so loading Tutorial directly behaves normally.
    /// </summary>
    public static class TutorialRoleState
    {
        public static TutorialRolePhase Phase { get; private set; } = TutorialRolePhase.PlayerAOperator;

        /// <summary>True once the role-swap round trip has handed control to Player B.</summary>
        public static bool HasSwapped => Phase == TutorialRolePhase.PlayerBOperator;

        /// <summary>Reset to Phase 1. Call at run start and when returning to the main menu.</summary>
        public static void Reset()
        {
            Phase = TutorialRolePhase.PlayerAOperator;
        }

        /// <summary>Flag the next Tutorial load as Phase 2 (Player B operator). Set before loading the swap cut scene.</summary>
        public static void MarkSwapToPlayerBOperator()
        {
            Phase = TutorialRolePhase.PlayerBOperator;
        }
    }
}
