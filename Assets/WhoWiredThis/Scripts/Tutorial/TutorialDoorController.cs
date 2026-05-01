using UnityEngine;

namespace WhoWiredThis.Tutorial
{
    public class TutorialDoorController : MonoBehaviour
    {
        [SerializeField] private Renderer doorRenderer;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material unlockedMaterial;
        [SerializeField] private GameObject lockedStateObject;
        [SerializeField] private GameObject unlockedStateObject;

        [SerializeField] private bool isUnlocked;

        private void Awake()
        {
            ApplyStateVisuals();
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            ApplyStateVisuals();
        }

        private void ApplyStateVisuals()
        {
            if (doorRenderer != null)
            {
                Material material = isUnlocked ? unlockedMaterial : lockedMaterial;
                if (material != null)
                {
                    doorRenderer.sharedMaterial = material;
                }
            }

            if (blockingCollider != null)
            {
                blockingCollider.enabled = !isUnlocked;
            }

            if (lockedStateObject != null)
            {
                lockedStateObject.SetActive(!isUnlocked);
            }

            if (unlockedStateObject != null)
            {
                unlockedStateObject.SetActive(isUnlocked);
            }
        }
    }
}
