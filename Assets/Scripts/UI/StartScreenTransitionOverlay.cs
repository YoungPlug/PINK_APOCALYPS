using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PinkApocalypse.UI
{
    /// <summary>Keeps the title screen above the gameplay scene until it is ready.</summary>
    [DisallowMultipleComponent]
    public sealed class StartScreenTransitionOverlay : MonoBehaviour
    {
        private const string GameplayScene = "SampleScene";
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameplayScene) StartCoroutine(RevealGameplay());
        }

        private IEnumerator RevealGameplay()
        {
            // Let scene Start methods establish the camera and opening cutscene first.
            yield return null;
            yield return new WaitForSecondsRealtime(0.25f);

            const float fadeDuration = 0.35f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
