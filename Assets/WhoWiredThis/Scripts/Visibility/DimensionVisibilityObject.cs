using System;
using System.Collections.Generic;
using UnityEngine;
using WhoWiredThis.Puzzles.A17;

namespace WhoWiredThis.Visibility
{
    public enum DimensionVisibilityMode
    {
        PlayerAVisability = 0,
        PlayerBVisability = 1
    }

    public class DimensionVisibilityObject : MonoBehaviour
    {
        [Header("Dimension Data")]
        [SerializeField] private DimensionVisibilityMode mode = DimensionVisibilityMode.PlayerAVisability;

        [Header("Optional Switch Control")]
        [SerializeField] private PolaritySwitchController controlledPolaritySwitch;

        [Header("Controlled Components - runtime debugging - no need to set")]
        [SerializeField] private Renderer[] objectRenderers = Array.Empty<Renderer>();
        [SerializeField] private Collider[] objectColliders = Array.Empty<Collider>();
        [SerializeField] private Renderer[] placeholderRenderers = Array.Empty<Renderer>();

        private const string DimensionALayer = "DimensionA";
        private const string DimensionBLayer = "DimensionB";
        private const string PlaceholderToken = "PLACEHOLDER";

        private void Awake()
        {
            ApplyVisibilitySetup();
        }

        private void ApplyVisibilitySetup()
        {
            AutoCollectIfNeeded();

            int dimensionALayerIndex = LayerMask.NameToLayer(DimensionALayer);
            int dimensionBLayerIndex = LayerMask.NameToLayer(DimensionBLayer);
            if (dimensionALayerIndex < 0 || dimensionBLayerIndex < 0)
            {
                return;
            }

            int objectLayer = mode == DimensionVisibilityMode.PlayerAVisability ? dimensionALayerIndex : dimensionBLayerIndex;
            int placeholderLayer = mode == DimensionVisibilityMode.PlayerAVisability ? dimensionBLayerIndex : dimensionALayerIndex;

            SetTargetsLayer(objectRenderers, objectColliders, objectLayer);
            SetTargetsLayer(placeholderRenderers, Array.Empty<Collider>(), placeholderLayer);
            ApplyColliderState(objectColliders, true);
            ApplySwitchAllowedPlayerTag();
        }

        private void ApplySwitchAllowedPlayerTag()
        {
            if (controlledPolaritySwitch == null)
            {
                return;
            }

            PolaritySwitchController.AllowedPlayerTag allowedTag =
                mode == DimensionVisibilityMode.PlayerAVisability
                    ? PolaritySwitchController.AllowedPlayerTag.PlayerA
                    : PolaritySwitchController.AllowedPlayerTag.PlayerB;

            controlledPolaritySwitch.SetAllowedPlayerTag(allowedTag);
        }

        private void AutoCollectIfNeeded()
        {
            bool needsObjects = objectRenderers == null || objectRenderers.Length == 0;
            bool needsPlaceholders = placeholderRenderers == null || placeholderRenderers.Length == 0;

            if (!needsObjects && !needsPlaceholders)
            {
                return;
            }

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);

            if (needsObjects)
            {
                objectRenderers = FilterRenderersByPlaceholder(allRenderers, includePlaceholders: false);
                objectColliders = FilterCollidersByPlaceholder(allColliders, includePlaceholders: false);
            }

            if (needsPlaceholders)
            {
                placeholderRenderers = FilterRenderersByPlaceholder(allRenderers, includePlaceholders: true);

                if (placeholderRenderers.Length == 0)
                {
                    EnsureAutoPlaceholder();
                    placeholderRenderers = FilterRenderersByPlaceholder(GetComponentsInChildren<Renderer>(true), includePlaceholders: true);
                }
            }
        }

        private static Renderer[] FilterRenderersByPlaceholder(Renderer[] renderers, bool includePlaceholders)
        {
            List<Renderer> filtered = new List<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                bool isPlaceholder = IsPlaceholderTransform(renderer.transform);
                if (isPlaceholder == includePlaceholders)
                {
                    filtered.Add(renderer);
                }
            }

            return filtered.ToArray();
        }

        private static Collider[] FilterCollidersByPlaceholder(Collider[] colliders, bool includePlaceholders)
        {
            List<Collider> filtered = new List<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                bool isPlaceholder = IsPlaceholderTransform(collider.transform);
                if (isPlaceholder == includePlaceholders)
                {
                    filtered.Add(collider);
                }
            }

            return filtered.ToArray();
        }

        private void EnsureAutoPlaceholder()
        {
            Transform existingPlaceholder = transform.Find("PLACEHOLDER_Auto");
            if (existingPlaceholder != null)
            {
                return;
            }

            GameObject Placeholder = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Placeholder.name = "PLACEHOLDER_Auto";
            Placeholder.transform.SetParent(transform, false);
            Placeholder.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            Placeholder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Placeholder.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Collider PlaceholderCollider = Placeholder.GetComponent<Collider>();
            if (PlaceholderCollider != null)
            {
                DestroyImmediate(PlaceholderCollider);
            }
        }

        private static bool IsPlaceholderTransform(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name.IndexOf(PlaceholderToken, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void SetTargetsLayer(Renderer[] renderers, Collider[] colliders, int layer)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].gameObject.layer = layer;
                }
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].gameObject.layer = layer;
                }
            }
        }

        private static void ApplyColliderState(Collider[] colliders, bool enabled)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }
    }
}
