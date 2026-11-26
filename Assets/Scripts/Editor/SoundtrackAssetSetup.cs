using System;
using System.IO;
using System.Linq;
using PinkApocalypse.Managers;
using PinkApocalypse.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PinkApocalypse.EditorTools
{
    public static class SoundtrackAssetSetup
    {
        private const string TrackFolder = "Assets/Audio/Soundtrack";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Pink Apocalypse/Setup Soundtrack")]
        public static void Setup()
        {
            string[] trackPaths = AssetDatabase.FindAssets("t:AudioClip", new[] { TrackFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (trackPaths.Length == 0)
            {
                Debug.LogError($"No audio clips found in {TrackFolder}. Copy the soundtrack files there first.");
                return;
            }

            foreach (string path in trackPaths) ConfigureAudioImport(path);
            AudioClip[] clips = trackPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
                .Where(clip => clip != null)
                .ToArray();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject manager = GameObject.Find("Soundtrack");
            if (manager == null) manager = new GameObject("Soundtrack");

            AudioSource source = manager.GetComponent<AudioSource>();
            if (source == null) source = manager.AddComponent<AudioSource>();
            SoundtrackPlayer player = manager.GetComponent<SoundtrackPlayer>();
            if (player == null) player = manager.AddComponent<SoundtrackPlayer>();

            GameObject playerObject = GameObject.Find("Player Spawn");
            PlayerInputReader input = playerObject != null ? playerObject.GetComponent<PlayerInputReader>() : null;
            SetReference(player, "audioSource", source);
            SetReference(player, "input", input);
            SetAudioClips(player, "tracks", clips);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Soundtrack configured with {clips.Length} tracks. Playlist order is alphabetic by filename.");
        }

        private static void ConfigureAudioImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.65f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAudioClips(UnityEngine.Object target, string propertyName, AudioClip[] clips)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(propertyName);
            list.arraySize = clips.Length;
            for (int index = 0; index < clips.Length; index++)
                list.GetArrayElementAtIndex(index).objectReferenceValue = clips[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
