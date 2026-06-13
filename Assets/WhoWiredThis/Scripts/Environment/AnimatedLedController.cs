using System.Collections;
using UnityEngine;

namespace WhoWiredThis.Environment
{
    /// <summary>
    /// Configures the Animated LED prefab root: optional root transform, indicator light color,
    /// LED mesh material color, and a random delay before the bounce animation starts.
    /// Attach to the prefab root ("Animated LED").
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimatedLedController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Hierarchy")]
        [SerializeField] private Transform ledRoot;
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Renderer ledRenderer;
        [SerializeField] private Animator ledAnimator;

        [Header("Root Placement")]
        [Tooltip("When enabled, applies the local transform below to this prefab root on Awake.")]
        [SerializeField] private bool applyRootTransformOnAwake;
        [SerializeField] private Vector3 rootLocalPosition;
        [SerializeField] private Vector3 rootLocalEulerAngles;
        [SerializeField] private Vector3 rootLocalScale = Vector3.one;

        [Header("Colors")]
        [SerializeField] private Color indicatorLightColor = Color.red;
        [SerializeField] private Color ledMaterialColor = new Color(0.9f, 0.05f, 0.05f, 1f);
        [SerializeField] private Color ledEmissionColor = new Color(0.6f, 0f, 0f, 1f);
        [SerializeField] private bool applyEmissionColor = true;

        [Header("Animation Start")]
        [SerializeField] private float animationStartDelayMin = 0.1f;
        [SerializeField] private float animationStartDelayMax = 1f;

        private Material ledMaterialInstance;

        private void Awake()
        {
            ResolveReferences();
            ApplyRootTransform();
            ApplyColors();
            HoldAnimationUntilStart();
        }

        private void Start()
        {
            StartCoroutine(BeginAnimationAfterRandomDelay());
        }

        private void OnDestroy()
        {
            if (ledMaterialInstance != null)
            {
                Destroy(ledMaterialInstance);
                ledMaterialInstance = null;
            }
        }

        public void ApplyColors()
        {
            if (indicatorLight != null)
            {
                indicatorLight.color = indicatorLightColor;
            }

            if (ledRenderer == null)
            {
                return;
            }

            ledMaterialInstance = ledRenderer.material;
            ledMaterialInstance.SetColor(BaseColorId, ledMaterialColor);
            ledMaterialInstance.SetColor(ColorId, ledMaterialColor);

            if (applyEmissionColor && ledMaterialInstance.HasProperty(EmissionColorId))
            {
                ledMaterialInstance.SetColor(EmissionColorId, ledEmissionColor);
                ledMaterialInstance.EnableKeyword("_EMISSION");
            }
        }

        private void ApplyRootTransform()
        {
            if (!applyRootTransformOnAwake)
            {
                return;
            }

            Transform root = transform;
            root.localPosition = rootLocalPosition;
            root.localRotation = Quaternion.Euler(rootLocalEulerAngles);
            root.localScale = rootLocalScale;
        }

        private void HoldAnimationUntilStart()
        {
            if (ledAnimator != null)
            {
                ledAnimator.enabled = false;
            }
        }

        private IEnumerator BeginAnimationAfterRandomDelay()
        {
            float min = Mathf.Min(animationStartDelayMin, animationStartDelayMax);
            float max = Mathf.Max(animationStartDelayMin, animationStartDelayMax);
            float delay = Random.Range(min, max);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (ledAnimator == null)
            {
                yield break;
            }

            ledAnimator.enabled = true;
            ledAnimator.Rebind();
            ledAnimator.Update(0f);
            ledAnimator.Play(0, 0, 0f);
        }

        private void ResolveReferences()
        {
            if (ledRoot == null)
            {
                ledRoot = transform.Find("LED");
            }

            if (ledRoot == null)
            {
                Debug.LogWarning($"[{nameof(AnimatedLedController)}] Missing child 'LED' on '{name}'.", this);
                return;
            }

            if (indicatorLight == null)
            {
                Transform indicatorTransform = ledRoot.Find("IndicatorLight");
                if (indicatorTransform != null)
                {
                    indicatorLight = indicatorTransform.GetComponent<Light>();
                }
            }

            if (ledRenderer == null)
            {
                ledRenderer = ledRoot.GetComponent<Renderer>();
            }

            if (ledAnimator == null)
            {
                ledAnimator = ledRoot.GetComponent<Animator>();
            }
        }
    }
}
