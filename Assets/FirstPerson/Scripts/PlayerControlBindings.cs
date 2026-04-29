using UnityEngine;

namespace FirstPerson
{
    [CreateAssetMenu(fileName = "PlayerControlBindings", menuName = "FirstPerson/Player Control Bindings")]
    public class PlayerControlBindings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBack = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;

        [Header("Actions")]
        [SerializeField] private KeyCode interact = KeyCode.E;

        public KeyCode MoveForward => moveForward;
        public KeyCode MoveBack => moveBack;
        public KeyCode MoveLeft => moveLeft;
        public KeyCode MoveRight => moveRight;
        public KeyCode Interact => interact;
    }
}
