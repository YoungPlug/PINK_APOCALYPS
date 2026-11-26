using UnityEngine;
using PinkApocalypse.Player;

namespace PinkApocalypse.Skateboard
{
    public sealed class SkateboardController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform boardVisual;
        [SerializeField] private bool mountedOnStart = true;
        [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField] private Vector3 parkedOffset = new Vector3(0f, 0.1f, 0.85f);
        [SerializeField, Min(0f)] private float transitionSpeed = 8f;

        public bool IsMounted { get; private set; }

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            IsMounted = mountedOnStart;
            if (boardVisual != null)
            {
                boardVisual.localPosition = IsMounted ? mountedOffset : parkedOffset;
                boardVisual.localRotation = IsMounted ? Quaternion.identity : Quaternion.Euler(0f, 15f, 0f);
            }
        }

        private void Update()
        {
            if (input != null && input.InteractPressed) IsMounted = !IsMounted;
            if (boardVisual == null) return;

            Vector3 destination = IsMounted ? mountedOffset : parkedOffset;
            boardVisual.localPosition = Vector3.Lerp(boardVisual.localPosition, destination, transitionSpeed * Time.deltaTime);
            boardVisual.localRotation = Quaternion.Slerp(boardVisual.localRotation,
                IsMounted ? Quaternion.identity : Quaternion.Euler(0f, 15f, 0f), transitionSpeed * Time.deltaTime);
        }
    }
}
