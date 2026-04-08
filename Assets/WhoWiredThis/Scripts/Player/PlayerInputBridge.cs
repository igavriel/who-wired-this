using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WhoWiredThis.Player
{
    // Centralized gameplay intents from Input System actions.
    public class PlayerInputBridge : MonoBehaviour
    {
        [Header("Keyboard Fallback")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private KeyCode helpKey = KeyCode.H;
        [SerializeField] private KeyCode menuKey = KeyCode.Escape;
        [SerializeField] private KeyCode slot1Key = KeyCode.Alpha1;
        [SerializeField] private KeyCode slot2Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode slot3Key = KeyCode.Alpha3;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }

        public bool InteractPressedThisFrame { get; private set; }
        public bool InventoryPressedThisFrame { get; private set; }
        public bool HelpPressedThisFrame { get; private set; }
        public bool MenuPressedThisFrame { get; private set; }
        public bool Slot1PressedThisFrame { get; private set; }
        public bool Slot2PressedThisFrame { get; private set; }
        public bool Slot3PressedThisFrame { get; private set; }

        // Used by gameplay to apply mouse-specific UI click guards.
        public bool InteractPressedFromPointerThisFrame { get; private set; }

#if ENABLE_INPUT_SYSTEM
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction interactAction;
        private InputAction inventoryAction;
        private InputAction helpAction;
        private InputAction menuAction;
        private InputAction slot1Action;
        private InputAction slot2Action;
        private InputAction slot3Action;
#endif

        private bool isUsingGamepad;

        public bool IsUsingGamepad
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return isUsingGamepad;
#else
                return false;
#endif
            }
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            InitializeActionsIfNeeded();
            SetActionsEnabled(true);
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            SetActionsEnabled(false);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void InitializeActionsIfNeeded()
        {
            if (moveAction != null)
            {
                return;
            }

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = new InputAction("Look", InputActionType.Value);
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Mouse>/leftButton");
            interactAction.AddBinding("<Gamepad>/buttonSouth");

            inventoryAction = new InputAction("Inventory", InputActionType.Button);
            inventoryAction.AddBinding("<Keyboard>/i");
            inventoryAction.AddBinding("<Gamepad>/buttonWest");

            helpAction = new InputAction("Help", InputActionType.Button);
            helpAction.AddBinding("<Keyboard>/h");
            helpAction.AddBinding("<Gamepad>/buttonNorth");

            menuAction = new InputAction("Menu", InputActionType.Button);
            menuAction.AddBinding("<Keyboard>/escape");
            menuAction.AddBinding("<Gamepad>/start");

            slot1Action = new InputAction("Slot1", InputActionType.Button);
            slot1Action.AddBinding("<Keyboard>/1");
            slot1Action.AddBinding("<Gamepad>/dpad/left");

            slot2Action = new InputAction("Slot2", InputActionType.Button);
            slot2Action.AddBinding("<Keyboard>/2");
            slot2Action.AddBinding("<Gamepad>/dpad/up");

            slot3Action = new InputAction("Slot3", InputActionType.Button);
            slot3Action.AddBinding("<Keyboard>/3");
            slot3Action.AddBinding("<Gamepad>/dpad/right");
        }

        private void SetActionsEnabled(bool enabled)
        {
            if (moveAction == null)
            {
                return;
            }

            if (enabled)
            {
                moveAction.Enable();
                lookAction.Enable();
                interactAction.Enable();
                inventoryAction.Enable();
                helpAction.Enable();
                menuAction.Enable();
                slot1Action.Enable();
                slot2Action.Enable();
                slot3Action.Enable();
            }
            else
            {
                moveAction.Disable();
                lookAction.Disable();
                interactAction.Disable();
                inventoryAction.Disable();
                helpAction.Disable();
                menuAction.Disable();
                slot1Action.Disable();
                slot2Action.Disable();
                slot3Action.Disable();
            }
        }
#endif

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Move = moveAction.ReadValue<Vector2>();
            Look = lookAction.ReadValue<Vector2>();

            InteractPressedThisFrame = interactAction.WasPressedThisFrame();
            InventoryPressedThisFrame = inventoryAction.WasPressedThisFrame();
            HelpPressedThisFrame = helpAction.WasPressedThisFrame();
            MenuPressedThisFrame = menuAction.WasPressedThisFrame();
            Slot1PressedThisFrame = slot1Action.WasPressedThisFrame();
            Slot2PressedThisFrame = slot2Action.WasPressedThisFrame();
            Slot3PressedThisFrame = slot3Action.WasPressedThisFrame();

            InteractPressedFromPointerThisFrame =
                InteractPressedThisFrame && interactAction.activeControl?.device is Mouse;

            isUsingGamepad = false;
            isUsingGamepad |= moveAction.activeControl?.device is Gamepad && Move.sqrMagnitude > 0.0001f;
            isUsingGamepad |= lookAction.activeControl?.device is Gamepad && Look.sqrMagnitude > 0.0001f;
            isUsingGamepad |= interactAction.activeControl?.device is Gamepad && InteractPressedThisFrame;
            isUsingGamepad |= inventoryAction.activeControl?.device is Gamepad && InventoryPressedThisFrame;
            isUsingGamepad |= helpAction.activeControl?.device is Gamepad && HelpPressedThisFrame;
            isUsingGamepad |= menuAction.activeControl?.device is Gamepad && MenuPressedThisFrame;
            isUsingGamepad |= slot1Action.activeControl?.device is Gamepad && Slot1PressedThisFrame;
            isUsingGamepad |= slot2Action.activeControl?.device is Gamepad && Slot2PressedThisFrame;
            isUsingGamepad |= slot3Action.activeControl?.device is Gamepad && Slot3PressedThisFrame;
#else
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }

            float vertical = 0f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            Move = new Vector2(horizontal, vertical);
            Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            InteractPressedThisFrame = Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0);
            InteractPressedFromPointerThisFrame = Input.GetMouseButtonDown(0);

            InventoryPressedThisFrame =
                Input.GetKeyDown(inventoryKey);
            HelpPressedThisFrame =
                Input.GetKeyDown(helpKey);
            MenuPressedThisFrame =
                Input.GetKeyDown(menuKey);
            Slot1PressedThisFrame =
                Input.GetKeyDown(slot1Key);
            Slot2PressedThisFrame =
                Input.GetKeyDown(slot2Key);
            Slot3PressedThisFrame =
                Input.GetKeyDown(slot3Key);
            isUsingGamepad = false;
#endif
        }
    }
}
