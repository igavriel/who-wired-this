using System;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("Controlled Components - runtime debugging - no need to set")]
        [SerializeField] private Renderer[] objectRenderers = Array.Empty<Renderer>();
        [SerializeField] private Collider[] objectColliders = Array.Empty<Collider>();
        [SerializeField] private Renderer[] shadowRenderers = Array.Empty<Renderer>();

        private const string DimensionALayer = "DimensionA";
        private const string DimensionBLayer = "DimensionB";
        private const string ShadowToken = "SHADOW";

        private void Awake()
        {
            ApplyVisibilitySetup();
        }

        private void OnValidate()
        {
            // Avoid layer reassignment during validation. Unity can emit
            // "SendMessage cannot be called during OnValidate" for layer changes.
            // Runtime setup still happens in Awake.
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
            int shadowLayer = mode == DimensionVisibilityMode.PlayerAVisability ? dimensionBLayerIndex : dimensionALayerIndex;

            SetTargetsLayer(objectRenderers, objectColliders, objectLayer);
            SetTargetsLayer(shadowRenderers, Array.Empty<Collider>(), shadowLayer);
            ApplyColliderState(objectColliders, true);
        }

        private void AutoCollectIfNeeded()
        {
            bool needsObjects = objectRenderers == null || objectRenderers.Length == 0;
            bool needsShadows = shadowRenderers == null || shadowRenderers.Length == 0;

            if (!needsObjects && !needsShadows)
            {
                return;
            }

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);

            if (needsObjects)
            {
                List<Renderer> objectRendererList = new List<Renderer>();
                List<Collider> objectColliderList = new List<Collider>();

                for (int i = 0; i < allRenderers.Length; i++)
                {
                    if (!IsShadowTransform(allRenderers[i].transform))
                    {
                        objectRendererList.Add(allRenderers[i]);
                    }
                }

                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (!IsShadowTransform(allColliders[i].transform))
                    {
                        objectColliderList.Add(allColliders[i]);
                    }
                }

                objectRenderers = objectRendererList.ToArray();
                objectColliders = objectColliderList.ToArray();
            }

            if (needsShadows)
            {
                List<Renderer> shadowRendererList = new List<Renderer>();
                List<Collider> shadowColliderList = new List<Collider>();

                for (int i = 0; i < allRenderers.Length; i++)
                {
                    if (IsShadowTransform(allRenderers[i].transform))
                    {
                        shadowRendererList.Add(allRenderers[i]);
                    }
                }

                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (IsShadowTransform(allColliders[i].transform))
                    {
                        shadowColliderList.Add(allColliders[i]);
                    }
                }

                shadowRenderers = shadowRendererList.ToArray();

                if (shadowRenderers.Length == 0)
                {
                    EnsureAutoShadow();
                    shadowRenderers = GetComponentsInChildren<Renderer>(true);

                    List<Renderer> autoShadowRenderers = new List<Renderer>();
                    for (int i = 0; i < shadowRenderers.Length; i++)
                    {
                        if (IsShadowTransform(shadowRenderers[i].transform))
                        {
                            autoShadowRenderers.Add(shadowRenderers[i]);
                        }
                    }

                    shadowRenderers = autoShadowRenderers.ToArray();
                }
            }
        }

        private void EnsureAutoShadow()
        {
            Transform existingShadow = transform.Find("SHADOW_Auto");
            if (existingShadow != null)
            {
                return;
            }

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "SHADOW_Auto";
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Collider shadowCollider = shadow.GetComponent<Collider>();
            if (shadowCollider != null)
            {
                DestroyImmediate(shadowCollider);
            }
        }

        private static bool IsShadowTransform(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name.IndexOf(ShadowToken, StringComparison.OrdinalIgnoreCase) >= 0)
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
