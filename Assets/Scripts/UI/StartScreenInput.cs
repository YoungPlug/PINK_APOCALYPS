using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace PinkApocalypse.UI
{
    public sealed class StartScreenInput : MonoBehaviour
    {
        private bool isLoading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ShowTitleScreenWhenPlayingFromGameplayScene()
        {
            if (!Application.isEditor || SceneManager.GetActiveScene().name != "SampleScene") return;
            if (Application.CanStreamedLevelBeLoaded("TitleScreen"))
                SceneManager.LoadScene("TitleScreen", LoadSceneMode.Single);
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
            SceneManager.LoadScene("SampleScene");
        }
    }
}
