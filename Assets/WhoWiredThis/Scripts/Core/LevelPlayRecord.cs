namespace WhoWiredThis.Core
{
    /// <summary>
    /// Per-player stats for one gameplay level (Tutorial / Puzzle Pipes / Puzzle Signal).
    /// Retries = failed submits (attempts that did not solve).
    /// </summary>
    public struct PlayerPlayStats
    {
        public int Attempts;
        public int Retries;
        public float PlaySeconds;
        public bool Solved;
    }

    /// <summary>
    /// Immutable-style snapshot of one level's play for the run scoreboard.
    /// </summary>
    public sealed class LevelPlayRecord
    {
        public string SceneName;
        public PlayerPlayStats Blue;
        public PlayerPlayStats Red;
        public float SceneTotalSeconds;
        public bool Completed;
    }

    /// <summary>
    /// Live status of the active gameplay level for HUD / debug.
    /// </summary>
    public struct CurrentPlayStatus
    {
        public string LevelName;
        public string ActivePlayerLabel;
        public int BlueRetries;
        public int RedRetries;
        public float BlueTimeSeconds;
        public float RedTimeSeconds;
        public float SceneTimeSeconds;
        public bool HasActiveLevel;
    }
}
