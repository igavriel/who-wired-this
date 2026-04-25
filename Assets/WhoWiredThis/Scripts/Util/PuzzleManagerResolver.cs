using UnityEngine;
using WhoWiredThis.Interfaces;

namespace WhoWiredThis.Util
{
    public static class PuzzleManagerResolver
    {
        public static IPuzzleManager ResolvePuzzleManagerReference(
            MonoBehaviour puzzleManager,
            Object context,
            string contextName)
        {
            IPuzzleManager resolvedPuzzleManager = puzzleManager as IPuzzleManager;
            if (resolvedPuzzleManager != null)
            {
                return resolvedPuzzleManager;
            }

            if (puzzleManager != null)
            {
                Debug.LogError($"[{contextName}] Assigned puzzleManager does not implement IPuzzleManager.", context);
                return null;
            }

            Debug.LogError(
                $"[{contextName}] Missing puzzleManager reference. Assign a component that implements IPuzzleManager.",
                context);
            return null;
        }
    }
}
