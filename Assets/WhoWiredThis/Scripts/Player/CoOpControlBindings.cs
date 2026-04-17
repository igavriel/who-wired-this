using UnityEngine;

namespace WhoWiredThis.Player
{
    [CreateAssetMenu(fileName = "CoOpControlBindings", menuName = "Who Wired This/Player/CoOp Control Bindings")]
    public class CoOpControlBindings : ScriptableObject
    {
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBack = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode sprint = KeyCode.LeftShift;
        [SerializeField] private KeyCode interact = KeyCode.LeftControl;

        public KeyCode MoveForward => moveForward;
        public KeyCode MoveBack => moveBack;
        public KeyCode MoveLeft => moveLeft;
        public KeyCode MoveRight => moveRight;
        public KeyCode Sprint => sprint;
        public KeyCode Interact => interact;
    }
}
