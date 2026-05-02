using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Layer / renderer / collider helpers copied from <see cref="DimensionVisibilityObject"/> for use by
    /// <see cref="MultiDimension"/> only—kept separate so production visibility code stays untouched.
    /// </summary>
    internal static class MultiDimensionLayerUtility
    {
        public const string DimensionALayerName = "DimensionA";
        public const string DimensionBLayerName = "DimensionB";
        private const string PlaceholderToken = "PLACEHOLDER";

        public static bool TryResolveDimensionLayers(out int dimensionA, out int dimensionB)
        {
            dimensionA = LayerMask.NameToLayer(DimensionALayerName);
            dimensionB = LayerMask.NameToLayer(DimensionBLayerName);
            return dimensionA >= 0 && dimensionB >= 0;
        }

        public static void CollectFromRoot(Transform root, out Renderer[] nonPlaceholderRenderers,
            out Collider[] nonPlaceholderColliders, out Renderer[] placeholderRenderers)
        {
            if (root == null)
            {
                nonPlaceholderRenderers = Array.Empty<Renderer>();
                nonPlaceholderColliders = Array.Empty<Collider>();
                placeholderRenderers = Array.Empty<Renderer>();
                return;
            }

            Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);
            Collider[] allColliders = root.GetComponentsInChildren<Collider>(true);
            nonPlaceholderRenderers = FilterRenderersByPlaceholder(allRenderers, includePlaceholders: false);
            nonPlaceholderColliders = FilterCollidersByPlaceholder(allColliders, includePlaceholders: false);
            placeholderRenderers = FilterRenderersByPlaceholder(allRenderers, includePlaceholders: true);
        }

        /// <summary>Non-placeholder → DimensionA, placeholder → DimensionB (same as Player_A visibility).</summary>
        public static void ApplyPlayerAView(Transform root, int dimensionA, int dimensionB)
        {
            CollectFromRoot(root, out Renderer[] r, out Collider[] c, out Renderer[] ph);
            SetTargetsLayer(r, c, dimensionA);
            SetTargetsLayer(ph, Array.Empty<Collider>(), dimensionB);
            ApplyColliderState(c, true);
        }

        /// <summary>Non-placeholder → DimensionB, placeholder → DimensionA (same as Player_B visibility).</summary>
        public static void ApplyPlayerBView(Transform root, int dimensionA, int dimensionB)
        {
            CollectFromRoot(root, out Renderer[] r, out Collider[] c, out Renderer[] ph);
            SetTargetsLayer(r, c, dimensionB);
            SetTargetsLayer(ph, Array.Empty<Collider>(), dimensionA);
            ApplyColliderState(c, true);
        }

        /// <summary>CASE 3 / general object: all renderers and colliders on one layer (typically Default).</summary>
        public static void ApplyUniformLayer(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            SetTargetsLayer(renderers, colliders, layer);
            ApplyColliderState(colliders, true);
        }

        public static void SetTargetsLayer(Renderer[] renderers, Collider[] colliders, int layer)
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].gameObject.layer = layer;
                    }
                }
            }

            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].gameObject.layer = layer;
                    }
                }
            }
        }

        public static void ApplyColliderState(Collider[] colliders, bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
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
    }
}
