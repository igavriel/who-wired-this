using UnityEngine;

namespace WhoWiredThis.Puzzles.A17
{
    public class ResultLightController : MonoBehaviour
    {
        private enum LightState { Idle, Failure, Success }

        [Header("References")]
        [SerializeField] private A17PuzzleManager puzzleManager;
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

        void Awake()
        {
            if (lightRenderer == null)
                lightRenderer = GetComponent<Renderer>();
        }

        void Start()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnSuccess += HandleSuccess;
                puzzleManager.OnFailure += HandleFailure;
            }

            SetState(LightState.Idle);
        }

        void OnDestroy()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnSuccess -= HandleSuccess;
                puzzleManager.OnFailure -= HandleFailure;
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
