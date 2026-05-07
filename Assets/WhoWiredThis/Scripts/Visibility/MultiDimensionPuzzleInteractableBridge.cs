using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Util;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Interactable entrypoint that forwards interaction to a MultiDimension puzzle manager.
    /// </summary>
    public class MultiDimensionPuzzleInteractableBridge : MonoBehaviour, IInteractable
    {
        [Header("Target")]
        [Tooltip("Reference must be a MultiDimensionPuzzelManager.")]
        [RequireInterface(typeof(MultiDimensionPuzzelManager))]
        [SerializeField] private MonoBehaviour puzzleTargetReference;

        [Header("Prompt")]
        [SerializeField] private string interactPrompt = "$INTERACT$ Check combination";
        [SerializeField] private string solvedPrompt = "Combination solved.";

        private MultiDimensionPuzzelManager PuzzleTarget => puzzleTargetReference as MultiDimensionPuzzelManager;

        public string GetPromptText()
        {
            MultiDimensionPuzzelManager target = PuzzleTarget;
            return target != null && target.Solved ? solvedPrompt : interactPrompt;
        }

        public void Interact(GameObject interactor)
        {
            MultiDimensionPuzzelManager target = PuzzleTarget;
            if (target == null)
            {
                Debug.LogWarning($"[MultiDimensionPuzzleInteractableBridge] Missing puzzle target on '{name}'.", this);
                return;
            }

            Debug.Log(
                $"[MultiDimensionPuzzleInteractableBridge] '{name}' forwarding Interact to manager '{target.name}'. " +
                $"interactor={(interactor != null ? interactor.name : "null")}.",
                this);
            target.TryCheckSolutionFromInteractor(interactor);
        }
    }
}
