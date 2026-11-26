using UnityEngine;

namespace PinkApocalypse.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Skateboard.SkateboardController skateboard;

        [Header("Walking")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float runSpeed = 6.5f;
        [SerializeField, Min(0f)] private float groundAcceleration = 24f;

        [Header("Skateboard")]
        [SerializeField, Min(0f)] private float skateMaxSpeed = 11f;
        [SerializeField, Min(0f)] private float skateAcceleration = 8f;
        [SerializeField, Min(0f)] private float skateBraking = 15f;
        [SerializeField, Min(0f)] private float skateSteeringDegrees = 115f;

        [Header("Jump and gravity")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Min(0f)] private float groundedStickForce = 2f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public float CurrentSpeed => horizontalVelocity.magnitude;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (input == null || characterController == null) return;

            bool mounted = skateboard != null && skateboard.IsMounted;
            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            Transform view = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;

            Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : transform.forward;
            Vector3 right = view != null ? Vector3.ProjectOnPlane(view.right, Vector3.up).normalized : transform.right;
            Vector3 desiredDirection = forward * moveInput.y + right * moveInput.x;
            if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

            if (mounted)
                MoveOnSkateboard(moveInput, desiredDirection);
            else
                MoveOnFoot(moveInput, desiredDirection);

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -groundedStickForce;
                if (input.JumpPressed) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 frameVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(frameVelocity * Time.deltaTime);
        }

        private void MoveOnFoot(Vector2 inputVector, Vector3 direction)
        {
            float speed = input != null && input.SprintHeld ? runSpeed : walkSpeed;
            Vector3 desired = direction * (speed * inputVector.magnitude);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, groundAcceleration * Time.deltaTime);
            FaceDirection(direction);
        }

        private void MoveOnSkateboard(Vector2 inputVector, Vector3 direction)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(horizontalVelocity, Vector3.up);
            float throttle = Mathf.Clamp(inputVector.y, -1f, 1f);
            if (throttle > 0f)
                planarVelocity = Vector3.MoveTowards(planarVelocity, direction * skateMaxSpeed, skateAcceleration * throttle * Time.deltaTime);
            else if (throttle < 0f)
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, skateBraking * -throttle * Time.deltaTime);
            else
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, skateBraking * 0.18f * Time.deltaTime);

            float steer = inputVector.x;
            if (planarVelocity.sqrMagnitude > 0.04f && Mathf.Abs(steer) > 0.01f)
            {
                float directionSign = Vector3.Dot(planarVelocity.normalized, transform.forward) < 0f ? -1f : 1f;
                transform.Rotate(Vector3.up, steer * skateSteeringDegrees * directionSign * Time.deltaTime, Space.World);
                planarVelocity = Vector3.RotateTowards(planarVelocity, transform.forward * planarVelocity.magnitude,
                    Mathf.Abs(steer) * skateSteeringDegrees * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }

            horizontalVelocity = planarVelocity;
            if (planarVelocity.sqrMagnitude < 0.04f) FaceDirection(direction);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
