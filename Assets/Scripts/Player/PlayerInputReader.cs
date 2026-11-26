using UnityEngine;
using UnityEngine.InputSystem;

namespace PinkApocalypse.Player
{
    /// <summary>Small adapter around the project's existing Input System actions.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actionsAsset;

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction previousTrackAction;
        private InputAction nextTrackAction;

        public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 GamepadLook => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        public bool JumpPressed => jumpAction != null && jumpAction.WasPressedThisFrame();
        public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();
        public bool InteractPressed => interactAction != null && interactAction.WasPressedThisFrame();
        public bool PreviousTrackPressed => previousTrackAction != null && previousTrackAction.WasPressedThisFrame();
        public bool NextTrackPressed => nextTrackAction != null && nextTrackAction.WasPressedThisFrame();

        private void Awake()
        {
            if (actionsAsset == null)
            {
                Debug.LogError("PlayerInputReader needs the project's InputActionAsset.", this);
                return;
            }

            playerMap = actionsAsset.FindActionMap("Player", false);
            if (playerMap == null)
            {
                Debug.LogError("The InputActionAsset does not contain a 'Player' action map.", this);
                return;
            }

            moveAction = playerMap.FindAction("Move", false);
            lookAction = playerMap.FindAction("Look", false);
            jumpAction = playerMap.FindAction("Jump", false);
            sprintAction = playerMap.FindAction("Sprint", false);
            interactAction = playerMap.FindAction("Interact", false);
            previousTrackAction = playerMap.FindAction("Previous", false);
            nextTrackAction = playerMap.FindAction("Next", false);
        }

        private void OnEnable()
        {
            if (playerMap != null) playerMap.Enable();
        }

        private void OnDisable()
        {
            if (playerMap != null) playerMap.Disable();
        }
    }
}
