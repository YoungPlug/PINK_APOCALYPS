using UnityEngine;

namespace PinkApocalypse.Player
{
    /// <summary>Feeds locomotion speed into the character's Idle/Walk/Run Animator Controller.</summary>
    public sealed class CharacterMovementAnimation : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speedDampTime = 0.12f;

        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (animator == null && visualRoot != null) animator = visualRoot.GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null || motor == null) return;
            animator.SetFloat(SpeedParameter, motor.CurrentSpeed, speedDampTime, Time.deltaTime);
        }
    }
}
