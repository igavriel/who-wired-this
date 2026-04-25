using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Puzzles.FloorColor
{
    public partial class FloorColorResultLightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FloorColorMatrixPuzzleManager puzzleManager;
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

        private void Awake()
        {
            if (lightRenderer == null)
            {
                lightRenderer = GetComponent<Renderer>();
            }
        }

        private void Start()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnSuccess += HandleSuccess;
                puzzleManager.OnFailure += HandleFailure;
            }

            SetState(LightState.Idle);
        }

        private void OnDestroy()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnSuccess -= HandleSuccess;
                puzzleManager.OnFailure -= HandleFailure;
            }
        }

        private void HandleSuccess()
        {
            SetState(LightState.Success);
        }

        private void HandleFailure(int _)
        {
            SetState(LightState.Failure);
        }

        private void SetState(LightState state)
        {
            Material nextMaterial = state switch
            {
                LightState.Success => successMaterial,
                LightState.Failure => failureMaterial,
                _ => idleMaterial
            };

            Color nextColor = state switch
            {
                LightState.Success => successColor,
                LightState.Failure => failureColor,
                _ => idleColor
            };

            if (lightRenderer != null && nextMaterial != null)
            {
                lightRenderer.sharedMaterial = nextMaterial;
            }

            if (indicatorLight != null)
            {
                indicatorLight.color = nextColor;
                indicatorLight.enabled = state != LightState.Idle;
            }
        }
    }
}
