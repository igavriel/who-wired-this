namespace WhoWiredThis.Core
{
    /// <summary>
    /// Stable playtest scene identifiers. Scene names are mapped in <see cref="PlaytestSceneFlowConfigSO"/>.
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
    }
}
