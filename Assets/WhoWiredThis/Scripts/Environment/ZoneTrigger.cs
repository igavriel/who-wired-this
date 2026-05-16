using UnityEngine;
using WhoWiredThis.Core;
using WhoWiredThis.Enums;
using WhoWiredThis.Player;

namespace WhoWiredThis.Environment
{
    // Attach to a trigger volume (Box Collider with Is Trigger = true).
    // Tag the player root GameObject "PlayerA" or "PlayerB" (or legacy "Player").
    public class ZoneTrigger : MonoBehaviour
    {
        public string zoneName;

        void OnTriggerEnter(Collider other)
        {
            if (PlayerInteractorResolver.TryResolve(other.transform, out AllowedPlayerTag playerTag))
            {
                if (PlayerZoneTracker.Instance != null)
                {
                    PlayerZoneTracker.Instance.SetZone(playerTag, zoneName);
                    return;
                }
            }

            if (IsLegacyPlayerCollider(other))
            {
                GameManager.Instance?.SetZone(zoneName);
            }
        }

        private static bool IsLegacyPlayerCollider(Collider other)
        {
            Transform current = other.transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
