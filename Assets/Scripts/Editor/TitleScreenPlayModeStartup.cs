using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PinkApocalypse.EditorTools
{
    /// <summary>Starts Editor Play Mode at the title screen, avoiding a gameplay-scene flash.</summary>
    [InitializeOnLoad]
    internal static class TitleScreenPlayModeStartup
    {
        private const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";

        static TitleScreenPlayModeStartup()
        {
            EditorApplication.delayCall += SetTitleSceneAsPlayModeStart;
        }

        private static void SetTitleSceneAsPlayModeStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            SceneAsset titleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
            if (titleScene != null)
                EditorSceneManager.playModeStartScene = titleScene;
        }
    }
}
