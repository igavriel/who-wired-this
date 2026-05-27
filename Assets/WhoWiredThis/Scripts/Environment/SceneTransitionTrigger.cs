using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;
using WhoWiredThis.Player;

namespace WhoWiredThis.Environment
{
    [DisallowMultipleComponent]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("The name of the scene to load when the trigger is activated.")]
        [SerializeField] private string targetSceneName = string.Empty;
        [Tooltip("If the active scene name matches the target scene name, the trigger will not load the target scene.")]
        [SerializeField] private bool ignoreWhenAlreadyInTargetScene = true;

        [Header("Trigger Behavior")]
        [Tooltip("If the trigger has already been activated, it will not be activated again.")]
        [SerializeField] private bool loadOnce = true;
        [Tooltip("The collider that will trigger the scene transition.")]
        [SerializeField] private Collider triggerCollider;
        [Tooltip("If the trigger is not detected by the collider, the trigger will use bounds polling to detect the player.")]
        [SerializeField] private bool useBoundsPollingFallback = true;
        [Tooltip("The interval at which the trigger will check for player overlap.")]
        [SerializeField] private float pollingIntervalSeconds = 0.1f;

        private bool hasTriggered;
        private float nextPollingTime;

        private void OnEnable()
        {
            RegisterConfiguredTriggerColliders();
        }

        private void OnValidate()
        {
            RegisterConfiguredTriggerColliders();

            if (pollingIntervalSeconds < 0.02f)
            {
                pollingIntervalSeconds = 0.02f;
            }
        }

        private void Update()
        {
            // If the bounds polling fallback is not enabled, or the trigger has already been activated, or the trigger collider is not set, return.
            if (!useBoundsPollingFallback || hasTriggered || triggerCollider == null)
            {
                return;
            }

            // If the next polling time is not reached, return.
            if (Time.unscaledTime < nextPollingTime)
            {
                return;
            }

            // Set the next polling time.
            nextPollingTime = Time.unscaledTime + pollingIntervalSeconds;
            TryHandlePlayerOverlap("PlayerA");
            TryHandlePlayerOverlap("PlayerB");
        }

        internal void HandleTriggerEnter(Collider other)
        {
            // If the trigger has already been activated and the load once flag is set, return.
            if (hasTriggered && loadOnce)
            {
                return;
            }

            // If the player is not detected, return.
            if (!PlayerInteractorResolver.TryResolve(other.transform, out _))
            {
                return;
            }

            // Try to load the target scene.
            TryLoadTargetScene();
        }

        internal bool UsesTriggerCollider(Collider candidate)
        {
            return triggerCollider == candidate;
        }

        private void RegisterConfiguredTriggerColliders()
        {
            if (triggerCollider == null)
            {
                Debug.LogWarning($"[SceneTransitionTrigger] '{name}' has no triggerCollider assigned.", this);
                return;
            }

            SceneTransitionTriggerRelay relay = triggerCollider.GetComponent<SceneTransitionTriggerRelay>();
            if (relay == null)
            {
                relay = triggerCollider.gameObject.AddComponent<SceneTransitionTriggerRelay>();
            }

            relay.SetOwner(this);

            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning(
                    $"[SceneTransitionTrigger] Collider '{triggerCollider.name}' should use Is Trigger.",
                    triggerCollider);
            }
        }

        private void TryHandlePlayerOverlap(string playerTag)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                return;
            }

            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                playerCollider = controller;
            }

            if (playerCollider == null)
            {
                return;
            }

            if (!triggerCollider.bounds.Intersects(playerCollider.bounds))
            {
                return;
            }

            HandleTriggerEnter(playerCollider);
        }

        private void TryLoadTargetScene()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("[SceneTransitionTrigger] Target scene name is empty.");
                return;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (ignoreWhenAlreadyInTargetScene &&
                string.Equals(activeSceneName, targetSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogWarning($"[SceneTransitionTrigger] Scene '{targetSceneName}' is not in Build Settings.");
                return;
            }

            if (ShouldCountSceneForPlaytestTotal(activeSceneName))
            {
                PlaytestRunTotal.CompleteCurrentScene(activeSceneName);
            }

            hasTriggered = true;
            Debug.Log($"[SceneTransitionTrigger] Loading scene '{targetSceneName}'.");
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }

        private static bool ShouldCountSceneForPlaytestTotal(string sceneName)
        {
            return string.Equals(sceneName, "Tutorial", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Pipes", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Signal", StringComparison.Ordinal);
        }
    }

    [DisallowMultipleComponent]
    public class SceneTransitionTriggerRelay : MonoBehaviour
    {
        [SerializeField] private SceneTransitionTrigger owner;
        private Collider hostCollider;

        private void Awake()
        {
            hostCollider = GetComponent<Collider>();
        }

        public void SetOwner(SceneTransitionTrigger sceneTransitionTrigger)
        {
            owner = sceneTransitionTrigger;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryResolveOwner())
            {
                owner.HandleTriggerEnter(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (TryResolveOwner())
            {
                owner.HandleTriggerEnter(other);
            }
        }

        private bool TryResolveOwner()
        {
            if (owner != null)
            {
                return true;
            }

            if (hostCollider == null)
            {
                hostCollider = GetComponent<Collider>();
            }

            if (hostCollider == null)
            {
                return false;
            }

            SceneTransitionTrigger[] candidates = FindObjectsByType<SceneTransitionTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                SceneTransitionTrigger candidate = candidates[i];
                if (candidate != null && candidate.UsesTriggerCollider(hostCollider))
                {
                    owner = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
