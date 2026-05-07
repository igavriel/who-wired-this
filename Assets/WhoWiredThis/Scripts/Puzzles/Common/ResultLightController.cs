using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Util;

namespace WhoWiredThis.Puzzles.Common
{
    public class ResultLightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference must implement IPuzzleManager.")]
        [RequireInterface(typeof(IPuzzleManager))]
        [SerializeField] private MonoBehaviour puzzleManager;
        [SerializeField] private Renderer lightRenderer;
        [SerializeField] private Light indicatorLight;

        [Header("Materials")]
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material failureMaterial;
        [SerializeField] private Material successMaterial;

        [Header("Light Colors")]
        [SerializeField] private Color idleColor = new Color(1f, 0.75f, 0f);
        [SerializeField] private Color failureColor = Color.red;
        [SerializeField] private Color successColor = Color.green;
        private IPuzzleManager resolvedPuzzleManager;
        private IPuzzleManager PuzzleManager => resolvedPuzzleManager;

        void Awake()
        {
            if (lightRenderer == null)
                lightRenderer = GetComponent<Renderer>();

            resolvedPuzzleManager = PuzzleManagerResolver.ResolvePuzzleManagerReference(
                puzzleManager,
                this,
                nameof(ResultLightController));
        }

        void Start()
        {
            IPuzzleManager manager = PuzzleManager;
            if (manager != null)
            {
                manager.OnSuccess += HandleSuccess;
                manager.OnFailure += HandleFailure;
            }

            SetState(LightState.Idle);
        }

        void OnDestroy()
        {
            IPuzzleManager manager = PuzzleManager;
            if (manager != null)
            {
                manager.OnSuccess -= HandleSuccess;
                manager.OnFailure -= HandleFailure;
            }
        }

        private void HandleSuccess() => SetState(LightState.Success);
        private void HandleFailure(int _) => SetState(LightState.Failure);

        private void SetState(LightState state)
        {
            Material mat = state switch
            {
                LightState.Success => successMaterial,
                LightState.Failure => failureMaterial,
                _ => idleMaterial
            };

            Color lightColor = state switch
            {
                LightState.Success => successColor,
                LightState.Failure => failureColor,
                _ => idleColor
            };

            if (lightRenderer != null && mat != null)
                lightRenderer.sharedMaterial = mat;

            if (indicatorLight != null)
            {
                indicatorLight.color = lightColor;
                indicatorLight.enabled = (state != LightState.Idle);
            }
        }

    }
}
