using PinkApocalypse.CameraSystem;
using PinkApocalypse.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PinkApocalypse.UI
{
    /// <summary>Runs the opening dialogue after the title screen, then restarts into gameplay.</summary>
    public sealed class IntroCutsceneController : MonoBehaviour
    {
        private const string GameplayScene = "SampleScene";
        private const float CameraMoveDuration = 2f;

        private static bool skipIntroOnNextLoad;

        private Transform playerRoot;
        private Camera sceneCamera;
        private ThirdPersonCamera followCamera;
        private PlayerMotor playerMotor;
        private PlayerInputReader playerInput;
        private CharacterMovementAnimation movementAnimation;
        private Vector3 focusPoint;
        private Vector3 cameraStartPosition;
        private Vector3 cameraEndPosition;
        private float startFieldOfView;
        private float elapsed;
        private bool dialogueReady;
        private bool isLoading;
        private GameObject dialoguePanel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneListener()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplayScene) return;
            if (skipIntroOnNextLoad)
            {
                skipIntroOnNextLoad = false;
                return;
            }

            new GameObject("Opening Cutscene").AddComponent<IntroCutsceneController>();
        }

        private void Start()
        {
            GameObject player = GameObject.Find("Player Spawn");
            sceneCamera = Camera.main;
            if (player == null || sceneCamera == null)
            {
                Debug.LogWarning("Opening cutscene could not find the player or main camera.", this);
                return;
            }

            playerRoot = player.transform;
            playerMotor = player.GetComponent<PlayerMotor>();
            playerInput = player.GetComponent<PlayerInputReader>();
            movementAnimation = player.GetComponent<CharacterMovementAnimation>();
            followCamera = sceneCamera.GetComponent<ThirdPersonCamera>();
            if (playerMotor != null) playerMotor.enabled = false;
            if (playerInput != null) playerInput.enabled = false;
            if (movementAnimation != null) movementAnimation.enabled = false;
            if (followCamera != null) followCamera.enabled = false;

            playerRoot.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);
            focusPoint = GetFacePoint(player);
            Vector3 forward = playerRoot.forward;
            cameraStartPosition = focusPoint + Vector3.up * 0.55f + forward * 5.5f;
            cameraEndPosition = focusPoint + Vector3.up * 0.1f + forward * 1.45f;
            startFieldOfView = sceneCamera.fieldOfView;

            sceneCamera.transform.position = cameraStartPosition;
            sceneCamera.transform.rotation = Quaternion.LookRotation(focusPoint - cameraStartPosition, Vector3.up);
            sceneCamera.fieldOfView = 62f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (sceneCamera == null || playerRoot == null) return;

            if (!dialogueReady)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / CameraMoveDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                sceneCamera.transform.position = Vector3.Lerp(cameraStartPosition, cameraEndPosition, easedProgress);
                sceneCamera.transform.rotation = Quaternion.LookRotation(focusPoint - sceneCamera.transform.position, Vector3.up);
                sceneCamera.fieldOfView = Mathf.Lerp(startFieldOfView, 48f, easedProgress);

                if (progress >= 1f)
                {
                    dialogueReady = true;
                    CreateDialoguePanel();
                }
                return;
            }

            if (!isLoading && ContinuePressed())
            {
                isLoading = true;
                skipIntroOnNextLoad = true;
                SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
            }
        }

        private static Vector3 GetFacePoint(GameObject player)
        {
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return player.transform.position + Vector3.up * 1.55f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.86f, bounds.center.z);
        }

        private static bool ContinuePressed()
        {
            foreach (Gamepad gamepad in Gamepad.all)
                if (gamepad.buttonSouth.wasPressedThisFrame) return true;

            return Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        }

        private void CreateDialoguePanel()
        {
            GameObject canvasObject = new GameObject("Opening Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            dialoguePanel = new GameObject("Dialogue Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            dialoguePanel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.06f, 0.045f);
            panelRect.anchorMax = new Vector2(0.94f, 0.29f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = dialoguePanel.GetComponent<Image>();
            panelImage.color = new Color(0.055f, 0.025f, 0.09f, 0.94f);
            Outline outline = dialoguePanel.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.22f, 0.62f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject accent = new GameObject("Pink Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(dialoguePanel.transform, false);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.97f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            accent.GetComponent<Image>().color = new Color(1f, 0.22f, 0.62f, 1f);

            CreateText(dialoguePanel.transform, "Dialogue", "Lil May: вот черт, тут полно ублюдков, придется разобраться с ними",
                36, TextAnchor.MiddleLeft, new Vector2(0.045f, 0.28f), new Vector2(0.96f, 0.94f), Color.white);
            CreateText(dialoguePanel.transform, "Continue Prompt", "SOUTH / X — ПРОДОЛЖИТЬ",
                24, TextAnchor.MiddleRight, new Vector2(0.48f, 0.035f), new Vector2(0.96f, 0.24f), new Color(1f, 0.7f, 0.86f));
        }

        private static void CreateText(Transform parent, string objectName, string value, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.015f, 0.08f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
