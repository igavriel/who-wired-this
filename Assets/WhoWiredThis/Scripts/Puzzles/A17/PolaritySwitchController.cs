using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Interactables;

namespace WhoWiredThis.Puzzles.A17
{
    public partial class PolaritySwitchController : MonoBehaviour, IInteractable
    {

        [Header("State")]
        [SerializeField] private PolarityState currentState = PolarityState.Off;
        [SerializeField] private bool randomizeInitialState = false;

        [Header("Top Trigger")]
        [SerializeField] private bool allowTopTriggerToggle = true;
        [SerializeField] private float topSurfaceOffsetY = 0.2f;
        [SerializeField] private float topDetectionTolerance = 0.05f;
        [SerializeField] private float triggerCooldownSeconds = 0.2f;
        [SerializeField] private AllowedPlayerTag allowedPlayerTag = AllowedPlayerTag.Any_Player;

        [Header("Visuals")]
        [SerializeField] private Renderer switchRenderer;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material positiveMaterial;

        public PolarityState CurrentState => currentState;
        private float _lastTriggerTime = -999f;
        private Collider _ownCollider;
        private bool _wasPlayerAboveTop;

        void Awake()
        {
            if (randomizeInitialState)
            {
                currentState = GetRandomPolarityState();
            }

            if (switchRenderer == null)
                switchRenderer = GetComponent<Renderer>();

            _ownCollider = GetComponent<Collider>();
            if (_ownCollider == null)
            {
                Debug.LogWarning("[PolaritySwitchController] No collider found. Top step detection requires a collider.", this);
            }
            ApplyMaterial();
        }

        private void FixedUpdate()
        {
            if (!allowTopTriggerToggle || _ownCollider == null)
            {
                return;
            }

            bool hasPlayerAboveTop = HasPlayerAboveTopSurface();
            if (hasPlayerAboveTop && !_wasPlayerAboveTop && !IsTriggerCooldownActive())
            {
                ToggleStateWithCooldown();
            }

            _wasPlayerAboveTop = hasPlayerAboveTop;
        }

        public string GetPromptText()
        {
            string stateLabel = currentState switch
            {
                PolarityState.Negative => "[ - ]",
                PolarityState.Positive => "[ + ]",
                _ => "[ 0 ]"
            };
            return $"$INTERACT$ Polarity: {stateLabel}";
        }

        public void Interact(GameObject interactor)
        {
            if (!IsAllowedInteractor(interactor))
            {
                return;
            }

            ToggleState();
        }

        public void SetAllowedPlayerTag(AllowedPlayerTag nextAllowedPlayerTag)
        {
            allowedPlayerTag = nextAllowedPlayerTag;
        }

        private bool HasPlayerAboveTopSurface()
        {
            Bounds bounds = _ownCollider.bounds;
            float halfThicknessY = Mathf.Max(topDetectionTolerance, 0.02f);
            float topY = bounds.max.y + topSurfaceOffsetY;

            Vector3 center = new Vector3(bounds.center.x, topY + halfThicknessY, bounds.center.z);
            Vector3 halfExtents = new Vector3(
                Mathf.Max(bounds.extents.x * 0.95f, 0.05f),
                halfThicknessY,
                Mathf.Max(bounds.extents.z * 0.95f, 0.05f));

            Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                if (hit == _ownCollider || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (IsAllowedPlayerCollider(hit))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAllowedInteractor(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            string requiredTag = GetRequiredTagOrNull();
            return requiredTag == null || HasTagInHierarchy(interactor.transform, requiredTag);
        }

        private bool IsAllowedPlayerCollider(Collider other)
        {
            if (other == null || !HasCharacterControllerInHierarchy(other))
            {
                return false;
            }

            string requiredTag = GetRequiredTagOrNull();
            if (requiredTag == null)
            {
                return true;
            }

            return HasTagInHierarchy(other.transform, requiredTag);
        }

        private static bool HasCharacterControllerInHierarchy(Collider other)
        {
            return other.GetComponent<CharacterController>() != null
                || other.GetComponentInParent<CharacterController>() != null;
        }

        private static bool HasTagInHierarchy(Transform start, string requiredTag)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.CompareTag(requiredTag))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private string GetRequiredTagOrNull()
        {
            return allowedPlayerTag switch
            {
                AllowedPlayerTag.Player_A => "PlayerA",
                AllowedPlayerTag.Player_B => "PlayerB",
                _ => null
            };
        }

        private bool IsTriggerCooldownActive()
        {
            return Time.time - _lastTriggerTime < triggerCooldownSeconds;
        }

        private void ToggleStateWithCooldown()
        {
            _lastTriggerTime = Time.time;
            ToggleState();
        }

        private void ToggleState()
        {
            currentState = currentState switch
            {
                PolarityState.Negative => PolarityState.Off,
                PolarityState.Off => PolarityState.Positive,
                PolarityState.Positive => PolarityState.Negative,
                _ => PolarityState.Off
            };
            ApplyMaterial();
        }

        private static PolarityState GetRandomPolarityState()
        {
            int choice = Random.Range(0, 3);
            return choice switch
            {
                0 => PolarityState.Negative,
                1 => PolarityState.Off,
                _ => PolarityState.Positive
            };
        }

        private void ApplyMaterial()
        {
            if (switchRenderer == null) return;

            Material mat = currentState switch
            {
                PolarityState.Negative => negativeMaterial,
                PolarityState.Positive => positiveMaterial,
                _ => offMaterial
            };

            if (mat != null)
            {
                switchRenderer.sharedMaterial = mat;
            }
        }
    }
}
