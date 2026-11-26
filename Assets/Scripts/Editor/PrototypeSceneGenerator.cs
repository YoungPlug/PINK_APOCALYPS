using System.IO;
using PinkApocalypse.CameraSystem;
using PinkApocalypse.Player;
using PinkApocalypse.Skateboard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PinkApocalypse.EditorTools
{
    public static class PrototypeSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Pink Apocalypse/Generate Sprint 01 Prototype")]
        public static void Generate()
        {
            EnsureFolders();
            Material pink = GetOrCreateMaterial("PA_Pink", new Color(0.95f, 0.12f, 0.52f));
            Material lightPink = GetOrCreateMaterial("PA_LightPink", new Color(1f, 0.46f, 0.72f));
            Material dark = GetOrCreateMaterial("PA_Charcoal", new Color(0.12f, 0.1f, 0.15f));
            Material skin = GetOrCreateMaterial("PA_Skin", new Color(0.82f, 0.52f, 0.4f));
            Material white = GetOrCreateMaterial("PA_White", new Color(0.96f, 0.93f, 0.97f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.44f, 0.51f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.22f, 0.17f, 0.24f);
            RenderSettings.fogDensity = 0.012f;

            CreateBlock("Ground", new Vector3(0f, -0.1f, 0f), new Vector3(24f, 0.2f, 24f), dark);
            CreateBlock("Pink test lane", new Vector3(0f, 0.015f, 0f), new Vector3(3.5f, 0.025f, 18f), pink, false);
            CreateBlock("Low box left", new Vector3(-4f, 0.55f, 3f), new Vector3(1.5f, 1.1f, 1.5f), lightPink);
            CreateBlock("Low box right", new Vector3(4f, 0.4f, 6f), new Vector3(1.5f, 0.8f, 1.5f), white);
            CreateBlock("Test barrier", new Vector3(0f, 0.55f, 8f), new Vector3(5f, 1.1f, 0.35f), pink);
            CreateBlock("Ramp", new Vector3(-5f, 0.35f, -2f), new Vector3(3f, 0.7f, 4f), lightPink).transform.rotation = Quaternion.Euler(-10f, 0f, 0f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.83f, 0.91f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject player = new GameObject("Player Spawn");
            player.transform.position = new Vector3(0f, 0.12f, -6f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.93f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;

            var input = player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var board = player.AddComponent<SkateboardController>();
            CreateCharacterVisual(player.transform, skin, dark, lightPink, pink);
            Transform boardVisual = CreateBoardVisual(player.transform, pink, dark, white);
            SetPrivateReference(board, "input", input);
            SetPrivateReference(board, "boardVisual", boardVisual);
            SetPrivateReference(motor, "input", input);
            SetPrivateReference(motor, "skateboard", board);
            SetLayerRecursively(player, 2);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ThirdPersonCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2.5f, -11f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.1f;
            var cameraController = cameraObject.GetComponent<ThirdPersonCamera>();
            SetPrivateReference(cameraController, "target", player.transform);
            SetPrivateReference(cameraController, "input", input);
            SetPrivateInteger(cameraController, "collisionMask", ~(1 << 2));
            SetPrivateReference(motor, "cameraTransform", cameraObject.transform);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) Debug.LogError($"Could not load InputActionAsset at {InputPath}.");
            SetPrivateReference(input, "actionsAsset", actions);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"Sprint 01 prototype scene generated: {ScenePath}");
        }

        private static void EnsureFolders()
        {
            string[] folders = { "Assets/Art", "Assets/Audio", MaterialFolder, "Assets/Prefabs", "Assets/Scripts/Player",
                "Assets/Scripts/Camera", "Assets/Scripts/Skateboard", "Assets/Scripts/Interaction", "Assets/Scripts/Managers" };
            foreach (string folder in folders)
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                    string name = Path.GetFileName(folder);
                    if (parent != null && !AssetDatabase.IsValidFolder(parent))
                    {
                        string grandparent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
                        if (grandparent != null && !AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(grandparent, Path.GetFileName(parent));
                    }
                    if (parent != null && AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(parent, name);
                }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(block.GetComponent<Collider>());
            return block;
        }

        private static void CreateCharacterVisual(Transform root, Material skin, Material hair, Material shirt, Material pants)
        {
            CreatePrimitive("Torso", PrimitiveType.Capsule, root, new Vector3(0f, 1.14f, 0f), new Vector3(0.53f, 0.48f, 0.34f), shirt);
            CreatePrimitive("Head", PrimitiveType.Sphere, root, new Vector3(0f, 1.88f, 0f), new Vector3(0.38f, 0.42f, 0.36f), skin);
            CreatePrimitive("Hair", PrimitiveType.Sphere, root, new Vector3(0f, 2.06f, -0.055f), new Vector3(0.42f, 0.38f, 0.4f), hair);
            CreatePrimitive("Hair fringe", PrimitiveType.Sphere, root, new Vector3(0f, 2.1f, 0.21f), new Vector3(0.34f, 0.2f, 0.18f), hair);
            CreatePrimitive("Left leg", PrimitiveType.Capsule, root, new Vector3(-0.15f, 0.48f, 0f), new Vector3(0.19f, 0.5f, 0.2f), pants);
            CreatePrimitive("Right leg", PrimitiveType.Capsule, root, new Vector3(0.15f, 0.48f, 0f), new Vector3(0.19f, 0.5f, 0.2f), pants);
            CreatePrimitive("Left arm", PrimitiveType.Capsule, root, new Vector3(-0.36f, 1.17f, 0f), new Vector3(0.15f, 0.45f, 0.16f), skin).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            CreatePrimitive("Right arm", PrimitiveType.Capsule, root, new Vector3(0.36f, 1.17f, 0f), new Vector3(0.15f, 0.45f, 0.16f), skin).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        }

        private static Transform CreateBoardVisual(Transform root, Material deck, Material trucks, Material wheels)
        {
            GameObject board = new GameObject("Skateboard Visual");
            board.transform.SetParent(root, false);
            CreatePrimitive("Deck", PrimitiveType.Cube, board.transform, Vector3.zero, new Vector3(0.36f, 0.08f, 1.15f), deck);
            CreatePrimitive("Front truck", PrimitiveType.Cube, board.transform, new Vector3(0f, -0.06f, 0.36f), new Vector3(0.48f, 0.05f, 0.08f), trucks);
            CreatePrimitive("Rear truck", PrimitiveType.Cube, board.transform, new Vector3(0f, -0.06f, -0.36f), new Vector3(0.48f, 0.05f, 0.08f), trucks);
            foreach (float z in new[] { -0.38f, 0.38f })
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                GameObject wheel = CreatePrimitive("Wheel", PrimitiveType.Cylinder, board.transform,
                    new Vector3(x, -0.13f, z), new Vector3(0.11f, 0.06f, 0.11f), wheels);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            return board.transform;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        private static void SetPrivateReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void SetPrivateInteger(Object target, string fieldName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
