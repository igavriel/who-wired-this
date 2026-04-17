using UnityEngine;

namespace ThirdPersonMixamo
{
    [CreateAssetMenu(fileName = "PlayerControlBindings", menuName = "ThirdPersonMixamo/Player Control Bindings")]
    public class PlayerControlBindings : ScriptableObject
    {
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBack = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode sprint = KeyCode.LeftShift;
        [SerializeField] private KeyCode interact = KeyCode.LeftControl;
        [SerializeField] private KeyCode jump = KeyCode.Space;

        public KeyCode MoveForward => moveForward;
        public KeyCode MoveBack => moveBack;
        public KeyCode MoveLeft => moveLeft;
        public KeyCode MoveRight => moveRight;
        public KeyCode Sprint => sprint;
        public KeyCode Interact => interact;
        public KeyCode Jump => jump;
    }
}
