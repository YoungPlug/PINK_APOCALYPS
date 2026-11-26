using System.IO;
using System.Linq;
using System.Collections.Generic;
using PinkApocalypse.CameraSystem;
using PinkApocalypse.Managers;
using PinkApocalypse.Player;
using PinkApocalypse.Skateboard;
using UnityEditor;
using UnityEditor.Animations;
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
        private const string CharacterModelPath = "Assets/Art/Characters/MAYBEBABY_Rigged.fbx";
        private const string CharacterControllerPath = "Assets/Art/Characters/MAYBEBABY_Locomotion.controller";
        private const string EnvironmentModelPath = "Assets/Art/Environment/cartoon_city_free.fbx";
        private const string EnvironmentTextureFolder = "Assets/Art/Environment/Textures";

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

            CreateEnvironment();

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
            GameObject characterModel = CreateCharacterModel(player.transform);
            if (characterModel == null)
            {
                characterModel = new GameObject("Character Visual");
                characterModel.transform.SetParent(player.transform, false);
                CreateCharacterVisual(characterModel.transform, skin, dark, lightPink, pink);
            }
            var visualAnimator = player.AddComponent<CharacterMovementAnimation>();
            SetPrivateReference(visualAnimator, "visualRoot", characterModel.transform);
            SetPrivateReference(visualAnimator, "motor", motor);
            SetPrivateReference(visualAnimator, "animator", characterModel.GetComponent<Animator>());
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
            CreateSoundtrack(input);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(buildScene => buildScene.path == ScenePath))
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"Sprint 01 prototype scene generated: {ScenePath}");
        }

        private static void CreateEnvironment()
        {
            GameObject environmentAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentModelPath);
            if (environmentAsset == null)
            {
                Debug.LogError($"Could not load city map at {EnvironmentModelPath}.");
                return;
            }

            GameObject environment = (GameObject)PrefabUtility.InstantiatePrefab(environmentAsset);
            if (environment == null) environment = Object.Instantiate(environmentAsset);
            environment.name = "Cartoon City Map";
            environment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Dictionary<Material, Material> cityMaterials = CreateCityMaterials();

            foreach (MeshFilter meshFilter in environment.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
                if (renderer != null && cityMaterials.Count > 0)
                {
                    Material[] sourceMaterials = renderer.sharedMaterials;
                    for (int i = 0; i < sourceMaterials.Length; i++)
                        if (sourceMaterials[i] != null && cityMaterials.TryGetValue(sourceMaterials[i], out Material cityMaterial))
                            sourceMaterials[i] = cityMaterial;
                    renderer.sharedMaterials = sourceMaterials;
                }

                if (meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null) continue;
                var meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static Dictionary<Material, Material> CreateCityMaterials()
        {
            const string folder = "Assets/Art/Environment/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art/Environment", "Materials");

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material[] sourceMaterials = AssetDatabase.LoadAllAssetsAtPath(EnvironmentModelPath)
                .OfType<Material>().OrderBy(material => material.name, System.StringComparer.OrdinalIgnoreCase).ToArray();
            var replacements = new Dictionary<Material, Material>();

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                string safeName = string.Join("_", source.name.Split(Path.GetInvalidFileNameChars()));
                string path = $"{folder}/City_{i:000}_{safeName}.mat";
                Material cityMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (cityMaterial == null)
                {
                    cityMaterial = new Material(urpShader) { name = $"City_{i:000}_{safeName}" };
                    AssetDatabase.CreateAsset(cityMaterial, path);
                }

                Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                    : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                if (cityMaterial.HasProperty("_BaseColor")) cityMaterial.SetColor("_BaseColor", color);
                if (cityMaterial.HasProperty("_Color")) cityMaterial.SetColor("_Color", color);

                Texture albedo = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap")
                    : source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                string cityTexturePath = GetCityAlbedoPath(source.name);
                if (!string.IsNullOrEmpty(cityTexturePath))
                    albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(cityTexturePath);
                if (cityMaterial.HasProperty("_BaseMap")) cityMaterial.SetTexture("_BaseMap", albedo);
                if (cityMaterial.HasProperty("_MainTex")) cityMaterial.SetTexture("_MainTex", albedo);

                Texture normal = source.HasProperty("_BumpMap") ? source.GetTexture("_BumpMap") : null;
                if (source.name == "Tile_1")
                {
                    const string normalPath = EnvironmentTextureFolder + "/Tiles_2_Normal.png";
                    var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                    if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
                    {
                        normalImporter.textureType = TextureImporterType.NormalMap;
                        normalImporter.SaveAndReimport();
                    }
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                }
                if (normal != null && cityMaterial.HasProperty("_BumpMap")) cityMaterial.SetTexture("_BumpMap", normal);
                if (source.HasProperty("_Metallic") && cityMaterial.HasProperty("_Metallic"))
                    cityMaterial.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source.HasProperty("_Glossiness") && cityMaterial.HasProperty("_Smoothness"))
                    cityMaterial.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));

                EditorUtility.SetDirty(cityMaterial);
                replacements[source] = cityMaterial;
                Debug.Log($"City material '{source.name}': shader={source.shader?.name}, color={color}, albedo={(albedo != null ? albedo.name : "none")}");
            }

            AssetDatabase.SaveAssets();
            return replacements;
        }

        private static string GetCityAlbedoPath(string materialName)
        {
            string textureName = materialName switch
            {
                "Billboard" => "Banners.png",
                "Car_Color" => "Texture_Atlas_03.png",
                "Color" => "Texture_Atlas_01.png",
                "Color_Glossy" => "Texture_Atlas_02.png",
                "Graffiti" => "Graffiti.png",
                "Road_Signs" => "Road_Signs.png",
                "Roads" => "Roads.png",
                "scrolling text" => "Scrolling_Text.png",
                "Tile_1" => "Tiles_2.png",
                _ => null
            };
            return string.IsNullOrEmpty(textureName) ? null : $"{EnvironmentTextureFolder}/{textureName}";
        }

        private static void CreateSoundtrack(PlayerInputReader input)
        {
            const string soundtrackFolder = "Assets/Audio/Soundtrack";
            string[] trackPaths = AssetDatabase.FindAssets("t:AudioClip", new[] { soundtrackFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => Path.GetFileNameWithoutExtension(path), System.StringComparer.OrdinalIgnoreCase)
                .ToArray();
            AudioClip[] tracks = trackPaths.Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(clip => clip != null).ToArray();
            if (tracks.Length == 0)
            {
                Debug.LogWarning($"No soundtrack clips found in {soundtrackFolder}.");
                return;
            }

            GameObject soundtrack = new GameObject("Soundtrack");
            AudioSource source = soundtrack.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            SoundtrackPlayer player = soundtrack.AddComponent<SoundtrackPlayer>();
            SetPrivateReference(player, "audioSource", source);
            SetPrivateReference(player, "input", input);
            SetPrivateAudioClips(player, "tracks", tracks);
            Debug.Log($"Soundtrack added with {tracks.Length} tracks.");
        }

        private static void SetPrivateAudioClips(Object target, string fieldName, AudioClip[] clips)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(fieldName);
            list.arraySize = clips.Length;
            for (int index = 0; index < clips.Length; index++)
                list.GetArrayElementAtIndex(index).objectReferenceValue = clips[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCharacterModel(Transform playerRoot)
        {
            var modelImporter = AssetImporter.GetAtPath(CharacterModelPath) as ModelImporter;
            if (modelImporter != null)
            {
                bool needsReimport = modelImporter.animationType != ModelImporterAnimationType.Generic || !modelImporter.importAnimation;
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.importAnimation = true;

                ModelImporterClipAnimation[] clips = modelImporter.clipAnimations;
                if (clips.Length == 0) clips = modelImporter.defaultClipAnimations;
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    bool locomotionClip = clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || clip.name.IndexOf("Walk", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || clip.name.IndexOf("Run", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!locomotionClip) continue;
                    if (!clip.loopTime || !clip.loopPose) needsReimport = true;
                    clip.loopTime = true;
                    clip.loopPose = true;
                }

                if (clips.Length > 0) modelImporter.clipAnimations = clips;
                if (needsReimport) modelImporter.SaveAndReimport();
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            if (modelAsset == null) return null;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            if (model == null) model = Object.Instantiate(modelAsset);
            model.name = "Main Character Model";
            model.transform.SetParent(playerRoot, false);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = GetOrCreateCharacterController();
            animator.applyRootMotion = false;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                if (bounds.size.y > 0.001f)
                {
                    float scale = 1.72f / bounds.size.y;
                    model.transform.localScale = Vector3.one * scale;
                    model.transform.localPosition = Vector3.up * (-bounds.min.y * scale + 0.04f);
                }
            }

            SetLayerRecursively(model, 2);
            return model;
        }

        private static RuntimeAnimatorController GetOrCreateCharacterController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
            if (controller != null) return controller;

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(CharacterModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase)).ToArray();
            AnimationClip idleClip = FindClip(clips, "Idle");
            AnimationClip walkClip = FindClip(clips, "Walk");
            AnimationClip runClip = FindClip(clips, "Run");
            if (idleClip == null || walkClip == null || runClip == null)
            {
                Debug.LogError($"Rigged FBX clips missing. Imported clips: {string.Join(", ", clips.Select(c => c.name))}");
                return null;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(CharacterControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            AnimatorState walk = machine.AddState("Walk");
            AnimatorState run = machine.AddState("Run");
            machine.defaultState = idle;
            idle.motion = idleClip;
            walk.motion = walkClip;
            run.motion = runClip;

            AddSpeedTransition(idle, walk, AnimatorConditionMode.Greater, 0.12f);
            AddSpeedTransition(walk, idle, AnimatorConditionMode.Less, 0.08f);
            AddSpeedTransition(walk, run, AnimatorConditionMode.Greater, 4.8f);
            AddSpeedTransition(run, idle, AnimatorConditionMode.Less, 0.08f);
            AddSpeedTransition(run, walk, AnimatorConditionMode.Less, 4.8f);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string name)
        {
            return clips.FirstOrDefault(clip => string.Equals(clip.name, name, System.StringComparison.OrdinalIgnoreCase))
                ?? clips.FirstOrDefault(clip => clip.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AddSpeedTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, "Speed");
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
