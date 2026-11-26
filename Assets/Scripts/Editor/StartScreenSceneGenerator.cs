using PinkApocalypse.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PinkApocalypse.EditorTools
{
    public static class StartScreenSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/TitleScreen.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string LogoPath = "Assets/Art/UI/PinkApocalypse_Logo.png";

        [MenuItem("Pink Apocalypse/Generate Title Screen")]
        public static void Generate()
        {
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo == null)
            {
                Debug.LogError($"Could not load title screen logo at {LogoPath}.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = new GameObject("Title Screen Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = new GameObject("Opaque Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = Color.black;
            backgroundImage.raycastTarget = false;

            GameObject logoObject = new GameObject("Pink Apocalypse Logo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            logoObject.transform.SetParent(canvasObject.transform, false);
            RectTransform logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = Vector2.zero;
            logoRect.anchorMax = Vector2.one;
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            RawImage logoImage = logoObject.GetComponent<RawImage>();
            logoImage.texture = logo;
            logoImage.color = Color.white;
            logoImage.raycastTarget = false;

            AspectRatioFitter aspect = logoObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)logo.width / logo.height;

            GameObject promptObject = new GameObject("Continue Prompt", typeof(RectTransform), typeof(Text), typeof(Outline));
            promptObject.transform.SetParent(canvasObject.transform, false);
            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.05f, 0.045f);
            promptRect.anchorMax = new Vector2(0.95f, 0.15f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;

            Text prompt = promptObject.GetComponent<Text>();
            prompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prompt.text = "НАЖМИТЕ ЛЮБУЮ КНОПКУ";
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.fontSize = 30;
            prompt.resizeTextForBestFit = true;
            prompt.resizeTextMinSize = 16;
            prompt.resizeTextMaxSize = 34;
            prompt.color = new Color(1f, 0.78f, 0.9f, 0.96f);
            prompt.raycastTarget = false;

            Outline outline = promptObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.05f, 0.22f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject inputObject = new GameObject("Start Screen Input", typeof(StartScreenInput));
            inputObject.transform.SetParent(canvasObject.transform, false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
            Debug.Log($"Title screen generated: {ScenePath}. Any keyboard or gamepad button opens the intro cutscene before {GameplayScenePath}.");
        }
    }
}
