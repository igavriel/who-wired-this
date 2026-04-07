using UnityEngine;
using WhoWiredThis.Core;

namespace WhoWiredThis.Environment
{
    // Attach to a trigger volume (Box Collider with Is Trigger = true).
    // Tag the player GameObject "Player".
    public class ZoneTrigger : MonoBehaviour
    {
        public string zoneName;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameManager.Instance?.SetZone(zoneName);
            }
        }
    }
}
