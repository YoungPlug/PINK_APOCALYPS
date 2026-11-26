using UnityEngine;
using PinkApocalypse.Player;

namespace PinkApocalypse.Skateboard
{
    public sealed class SkateboardController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform boardVisual;
        [SerializeField] private bool mountedOnStart;
        [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField] private string modelResourcePath = "Skateboard/FULL";
        [SerializeField, Range(0f, 1f)] private float pinkTintStrength = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float characterHeightRatio = 0.5f;
        [SerializeField, Min(0.01f)] private float fallbackModelLength = 0.85f;
        [SerializeField, Min(0.1f)] private float kickflipDuration = 0.65f;

        private CharacterController characterController;
        private float kickflipElapsed;
        private float mountVerticalOffset;
        private bool isKickflipping;

        public bool IsMounted { get; private set; }
        public float MountedRestHeight { get; private set; }

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            characterController = GetComponent<CharacterController>();
            ReplaceBoardVisual();
            IsMounted = mountedOnStart;
            if (boardVisual != null)
            {
                boardVisual.gameObject.SetActive(IsMounted);
                if (IsMounted) AlignBoardUnderCharacter();
                else
                {
                    boardVisual.localPosition = mountedOffset;
                    boardVisual.localRotation = Quaternion.identity;
                }
            }
        }

        private void ReplaceBoardVisual()
        {
            if (boardVisual == null || string.IsNullOrWhiteSpace(modelResourcePath)) return;
            GameObject boardModel = Resources.Load<GameObject>(modelResourcePath);
            if (boardModel == null)
            {
                Debug.LogWarning($"Skateboard model not found in Resources: {modelResourcePath}", this);
                return;
            }

            // Remove the simple placeholder parts created by the prototype scene.
            for (int i = boardVisual.childCount - 1; i >= 0; i--)
            {
                GameObject placeholder = boardVisual.GetChild(i).gameObject;
                placeholder.SetActive(false);
                Destroy(placeholder);
            }

            GameObject model = Instantiate(boardModel, boardVisual, false);
            model.name = "FULL Skateboard Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = GetRenderBounds(renderers);
            model.transform.localRotation = GetBoardOrientation(renderers, bounds);

            // Normalize only after laying the board flat so its real deck length controls scale.
            bounds = GetRenderBounds(renderers);
            float horizontalLength = Mathf.Max(bounds.size.x, bounds.size.z);
            float targetLength = GetCharacterHeight() * characterHeightRatio;
            if (targetLength <= 0.01f) targetLength = fallbackModelLength;
            float uniformScale = horizontalLength > 0.001f ? targetLength / horizontalLength : 1f;
            model.transform.localScale = Vector3.one * uniformScale;

            Vector3 centerInModelSpace = model.transform.InverseTransformPoint(bounds.center);
            model.transform.localPosition = -(model.transform.localRotation * (centerInModelSpace * uniformScale));

            ApplyPinkTint(renderers);
        }

        private float GetCharacterHeight()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Bounds characterBounds = default;
            bool hasCharacterBounds = false;
            foreach (Renderer item in renderers)
            {
                if (item.transform == boardVisual || item.transform.IsChildOf(boardVisual)) continue;
                if (!hasCharacterBounds)
                {
                    characterBounds = item.bounds;
                    hasCharacterBounds = true;
                }
                else characterBounds.Encapsulate(item.bounds);
            }
            return hasCharacterBounds ? characterBounds.size.y : 0f;
        }

        private static Quaternion GetBoardOrientation(Renderer[] renderers, Bounds bounds)
        {
            Bounds wheelsBounds = default;
            Bounds deckBounds = default;
            bool hasWheels = false;
            bool hasDeck = false;

            foreach (Renderer item in renderers)
            {
                if (IsWheelRenderer(item))
                {
                    if (!hasWheels) wheelsBounds = item.bounds;
                    else wheelsBounds.Encapsulate(item.bounds);
                    hasWheels = true;
                }
                else
                {
                    if (!hasDeck) deckBounds = item.bounds;
                    else deckBounds.Encapsulate(item.bounds);
                    hasDeck = true;
                }
            }

            Vector3 longitudinalAxis = GetLongestAxis(bounds.size);
            Quaternion orientation = Quaternion.identity;
            if (hasWheels && hasDeck)
            {
                Vector3 wheelDirection = wheelsBounds.center - deckBounds.center;
                if (wheelDirection.sqrMagnitude > 0.0001f)
                    orientation = Quaternion.FromToRotation(wheelDirection.normalized, Vector3.down);
            }
            else
            {
                // Fallback for a single-mesh FBX: put its longest dimension along the ground.
                orientation = Quaternion.FromToRotation(longitudinalAxis, Vector3.forward);
            }

            Vector3 horizontalLengthAxis = Vector3.ProjectOnPlane(orientation * longitudinalAxis, Vector3.up);
            if (horizontalLengthAxis.sqrMagnitude > 0.0001f)
                orientation = Quaternion.FromToRotation(horizontalLengthAxis.normalized, Vector3.forward) * orientation;
            return Quaternion.AngleAxis(180f, Vector3.right) * orientation;
        }

        private static bool IsWheelRenderer(Renderer item)
        {
            if (item.name.IndexOf("wheel", System.StringComparison.OrdinalIgnoreCase) >= 0
                || item.name.IndexOf("roue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (Material material in item.sharedMaterials)
            {
                if (material == null) continue;
                if (material.name.IndexOf("wheel", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || material.name.IndexOf("roue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static Vector3 GetLongestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return Vector3.right;
            return size.y >= size.z ? Vector3.up : Vector3.forward;
        }

        private static Bounds GetRenderBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void ApplyPinkTint(Renderer[] renderers)
        {
            Color pinkTint = Color.Lerp(Color.white, new Color(1f, 0.34f, 0.66f), pinkTintStrength);
            foreach (Renderer item in renderers)
            {
                // Instance materials so the FBX's shared materials remain unchanged.
                foreach (Material material in item.materials)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.GetColor("_BaseColor") * pinkTint);
                    else if (material.HasProperty("_Color")) material.SetColor("_Color", material.GetColor("_Color") * pinkTint);
                }
            }
        }

        private void Update()
        {
            UpdateKickflip();
            if (boardVisual == null) return;

            if (input != null && input.InteractPressed)
            {
                if (IsMounted)
                {
                    IsMounted = false;
                    boardVisual.gameObject.SetActive(false);
                    transform.position -= Vector3.up * mountVerticalOffset;
                    mountVerticalOffset = 0f;
                }
                else
                {
                    IsMounted = true;
                    boardVisual.gameObject.SetActive(true);
                    AlignBoardUnderCharacter();
                }
            }
        }

        public void StartKickflip()
        {
            if (!IsMounted || boardVisual == null || isKickflipping) return;
            kickflipElapsed = 0f;
            isKickflipping = true;
        }

        private void UpdateKickflip()
        {
            if (!isKickflipping || boardVisual == null) return;

            kickflipElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(kickflipElapsed / Mathf.Max(0.1f, kickflipDuration));
            boardVisual.localRotation = Quaternion.AngleAxis(360f * progress, Vector3.forward);

            if (progress >= 1f)
            {
                boardVisual.localRotation = Quaternion.identity;
                isKickflipping = false;
            }
        }

        private void AlignBoardUnderCharacter()
        {
            boardVisual.localRotation = Quaternion.identity;
            boardVisual.localPosition = mountedOffset;

            Bounds characterBounds = default;
            bool hasCharacterBounds = false;
            Renderer[] playerRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer playerRenderer in playerRenderers)
            {
                if (playerRenderer.transform == boardVisual || playerRenderer.transform.IsChildOf(boardVisual)) continue;
                if (!hasCharacterBounds)
                {
                    characterBounds = playerRenderer.bounds;
                    hasCharacterBounds = true;
                }
                else characterBounds.Encapsulate(playerRenderer.bounds);
            }

            if (hasCharacterBounds)
            {
                Vector3 visualCenter = transform.InverseTransformPoint(characterBounds.center);
                Vector3 alignedPosition = boardVisual.localPosition;
                alignedPosition.x = visualCenter.x;
                alignedPosition.z = visualCenter.z;
                boardVisual.localPosition = alignedPosition;
            }

            Bounds boardBounds;
            Renderer[] boardRenderers = boardVisual.GetComponentsInChildren<Renderer>();
            if (boardRenderers.Length == 0) return;
            boardBounds = boardRenderers[0].bounds;
            for (int i = 1; i < boardRenderers.Length; i++)
                boardBounds.Encapsulate(boardRenderers[i].bounds);

            float feetY = characterController != null
                ? transform.TransformPoint(new Vector3(characterController.center.x,
                    characterController.center.y - characterController.height * 0.5f,
                    characterController.center.z)).y
                : transform.position.y;

            // Raise the player by the inverted board's thickness so the board clears the ground.
            mountVerticalOffset = Mathf.Max(0f, boardBounds.size.y);
            transform.position += Vector3.up * mountVerticalOffset;
            MountedRestHeight = transform.position.y;
            // The board is flipped over, so its uppermost wheel surface supports the feet.
            boardVisual.position += Vector3.up * (feetY - boardBounds.max.y);
        }
    }
}
