using UnityEngine;
using UnityEngine.InputSystem;
using PinkApocalypse.Player;

namespace PinkApocalypse.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0f)] private float distance = 5.5f;
        [SerializeField, Min(0f)] private float height = 1.55f;
        [SerializeField, Min(0f)] private float sensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 180f;
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.22f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float yaw;
        private float pitch = 12f;
        private Vector3 positionVelocity;

        private void Start()
        {
            if (input == null && target != null) input = target.GetComponent<PlayerInputReader>();
            yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.None;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (input == null) input = target.GetComponent<PlayerInputReader>();

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            }

            if (input != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = input.Look;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
            }
            else if (input != null && Gamepad.current != null)
            {
                Vector2 look = input.GamepadLook;
                yaw += look.x * gamepadSensitivity * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - look.y * gamepadSensitivity * Time.deltaTime, minPitch, maxPitch);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            Vector3 desiredPosition = focus - orbit * Vector3.forward * distance;
            Vector3 toCamera = desiredPosition - focus;
            float allowedDistance = distance;
            if (Physics.SphereCast(focus, collisionRadius, toCamera.normalized, out RaycastHit hit,
                    distance, collisionMask, QueryTriggerInteraction.Ignore))
                allowedDistance = Mathf.Max(0.5f, hit.distance - collisionRadius);
            desiredPosition = focus + toCamera.normalized * allowedDistance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
