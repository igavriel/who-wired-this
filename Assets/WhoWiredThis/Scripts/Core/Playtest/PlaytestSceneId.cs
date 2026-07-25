namespace WhoWiredThis.Core
{
    /// <summary>
    /// Stable playtest scene identifiers. Scene names are mapped in <see cref="GameConfigSO"/>.
    /// </summary>
    public enum PlaytestSceneId
    {
        None = 0,
        StartScene,
        CutSceneStartTutorial,
        Tutorial,
        CutSceneTutorialPipe,
        PuzzlePipes,
        CutScenePipeSignal,
        PuzzleSignal,
        GameOverScene,

        // Added at the end to preserve serialized int values of existing entries.
        // Tutorial role-swap side-trip: not part of the linear playtest chain order;
        // loaded explicitly by id (Tutorial -> here -> Tutorial).
        CutSceneTutorialSwap,

        // Puzzle Pipes role-swap side-trip: not part of the linear playtest chain order;
        // loaded explicitly by id (Puzzle Pipes -> here -> Puzzle Pipes).
        CutScenePipeSwap,

        // Puzzle Signal role-swap side-trip: not part of the linear playtest chain order;
        // loaded explicitly by id (Puzzle Signal -> here -> Puzzle Signal).
        CutSceneSignalSwap,
    }
}
