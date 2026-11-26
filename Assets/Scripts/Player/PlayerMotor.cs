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
        [SerializeField] private Material characterMaterial;

        [Header("Walking")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float runSpeed = 6.5f;
        [SerializeField, Min(0f)] private float groundAcceleration = 24f;

        [Header("Skateboard")]
        [SerializeField, Min(0f)] private float skateMaxSpeed = 11f;
        [SerializeField, Min(0f)] private float skateAcceleration = 8f;
        [SerializeField, Min(0f)] private float skateBraking = 15f;
        [SerializeField, Min(0f)] private float skateCoastingDeceleration = 7.5f;
        [SerializeField, Min(0f)] private float skateJumpHeight = 1.8f;

        [Header("Jump and gravity")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Min(0f)] private float groundedStickForce = 2f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public float CurrentSpeed => horizontalVelocity.magnitude;
        public bool IsMountedOnSkateboard => skateboard != null && skateboard.IsMounted;
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            ApplyCharacterMaterial();
            PinkApocalypse.World.CitySky.Apply();

            GameObject city = GameObject.Find("cartoon_city_free");
            if (city == null) city = GameObject.Find("Cartoon City Map");
            PinkApocalypse.World.CityBoundaryWalls.CreateAround(city);
            PinkApocalypse.World.CityStreetLights.CreateAround(city);
            PinkApocalypse.World.CityTraffic.Configure(city);
        }


        private void ApplyCharacterMaterial()
        {
            if (characterMaterial == null) return;

            Transform characterRoot = transform.Find("Main Character Model");
            if (characterRoot == null) characterRoot = transform.Find("Character Visual");
            if (characterRoot == null) return;

            foreach (Renderer characterRenderer in characterRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = characterRenderer.sharedMaterials;
                if (materials.Length == 0) continue;
                for (int i = 0; i < materials.Length; i++) materials[i] = characterMaterial;
                characterRenderer.sharedMaterials = materials;
            }
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

            bool mountedGrounded = mounted && verticalVelocity <= 0f
                && transform.position.y <= skateboard.MountedRestHeight + 0.02f;
            if (characterController.isGrounded || mountedGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = mounted ? 0f : -groundedStickForce;
                bool performKickflip = mounted && input.KickflipPressed;
                if (performKickflip || input.JumpPressed)
                {
                    float activeJumpHeight = mounted ? skateJumpHeight : jumpHeight;
                    verticalVelocity = Mathf.Sqrt(activeJumpHeight * -2f * gravity);
                    if (performKickflip) skateboard.StartKickflip();
                }
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 frameVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(frameVelocity * Time.deltaTime);

            if (mounted && verticalVelocity < 0f && transform.position.y <= skateboard.MountedRestHeight)
            {
                Vector3 groundedPosition = transform.position;
                groundedPosition.y = skateboard.MountedRestHeight;
                transform.position = groundedPosition;
                verticalVelocity = 0f;
            }
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
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, skateCoastingDeceleration * Time.deltaTime);

            horizontalVelocity = planarVelocity;
            FaceDirection(direction);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
