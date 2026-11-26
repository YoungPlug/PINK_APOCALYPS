using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PinkApocalypse.UI
{
    public sealed class StartScreenInput : MonoBehaviour
    {
        private bool isLoading;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.transform.Find("Opaque Background") != null) return;

            GameObject background = new GameObject("Opaque Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = background.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            background.transform.SetAsFirstSibling();
        }

        private void Update()
        {
            if (isLoading) return;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && keyboard.anyKey.wasPressedThisFrame)
                {
                    ContinueToGame();
                    return;
                }

                if (device is Gamepad gamepad)
                {
                    foreach (InputControl control in gamepad.allControls)
                    {
                        if (control is ButtonControl button && button.wasPressedThisFrame)
                        {
                            ContinueToGame();
                            return;
                        }
                    }
                }
            }
        }

        private void ContinueToGame()
        {
            isLoading = true;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Text prompt = canvas.GetComponentInChildren<Text>();
                if (prompt != null) prompt.text = "ЗАГРУЗКА...";

                canvas.sortingOrder = short.MaxValue;
                GameObject canvasRoot = canvas.gameObject;
                DontDestroyOnLoad(canvasRoot);
                canvasRoot.AddComponent<StartScreenTransitionOverlay>();
            }

            SceneManager.LoadScene("SampleScene");
        }
    }
}
