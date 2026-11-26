using System.Collections;
using PinkApocalypse.Managers;
using PinkApocalypse.Player;
using UnityEngine;
using UnityEngine.UI;

namespace PinkApocalypse.UI
{
    /// <summary>Runtime health bar and short song title notification.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHud : MonoBehaviour
    {
        private static PlayerHud instance;

        private Image healthFill;
        private Text healthValue;
        private CanvasGroup songGroup;
        private Text songTitle;
        private PlayerHealth health;
        private SoundtrackPlayer soundtrack;
        private Coroutine songRoutine;

        public static void EnsureCreated(PlayerHealth playerHealth)
        {
            if (instance == null)
            {
                GameObject hudObject = new GameObject("Game HUD");
                hudObject.AddComponent<PlayerHud>();
            }

            instance.BindHealth(playerHealth);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateCanvas();
            CreateHealthPanel();
            CreateSongPanel();
        }

       private void Update()
{
    if (soundtrack == null)
    {
        SoundtrackPlayer foundPlayer = FindFirstObjectByType<SoundtrackPlayer>();
        if (foundPlayer != null)
        {
            BindSoundtrack(foundPlayer);
        }
    }
}

        private void OnDestroy()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }

            if (soundtrack != null)
            {
                soundtrack.TrackStarted -= OnTrackStarted;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private void BindHealth(PlayerHealth playerHealth)
        {
            if (health == playerHealth) return;

            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }

            health = playerHealth;

            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
                OnHealthChanged(health.CurrentHealth, health.MaximumHealth);
            }
        }

        private void BindSoundtrack(SoundtrackPlayer player)
        {
            if (player == null || soundtrack == player) return;

            if (soundtrack != null)
            {
                soundtrack.TrackStarted -= OnTrackStarted;
            }

            soundtrack = player;
            soundtrack.TrackStarted += OnTrackStarted;

            // The player can start before this HUD is created during scene load.
            if (soundtrack.IsPlaying && !string.IsNullOrEmpty(soundtrack.CurrentTrackName))
            {
                OnTrackStarted(soundtrack.CurrentTrackName);
            }
        }

        private void OnHealthChanged(float current, float maximum)
        {
            float normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            if (healthFill != null)
            {
                healthFill.fillAmount = normalized;
                healthFill.color = Color.Lerp(
                    new Color(0.85f, 0.08f, 0.22f),
                    new Color(1f, 0.2f, 0.58f),
                    normalized
                );
            }

            if (healthValue != null)
            {
                healthValue.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            }
        }

        private void OnTrackStarted(string title)
        {
            if (string.IsNullOrWhiteSpace(title) || songGroup == null || songTitle == null) return;

            songTitle.text = title;
            songGroup.alpha = 1f;

            if (songRoutine != null)
            {
                StopCoroutine(songRoutine);
            }

            songRoutine = StartCoroutine(HideSongTitle());
        }

        private IEnumerator HideSongTitle()
        {
            yield return new WaitForSecondsRealtime(4f);

            const float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                songGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            songGroup.alpha = 0f;
            songRoutine = null;
        }

        private void CreateCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void CreateHealthPanel()
        {
            RectTransform panel = CreateRect(
                "Health Panel",
                transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -28f),
                new Vector2(340f, 94f)
            );
            AddImage(panel.gameObject, new Color(0.035f, 0.018f, 0.055f, 0.88f));

            Text label = CreateText(
                "Health Label",
                panel,
                "ЗДОРОВЬЕ",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.56f),
                new Vector2(0.55f, 0.94f),
                new Color(1f, 0.72f, 0.86f)
            );
            label.fontStyle = FontStyle.Bold;

            healthValue = CreateText(
                "Health Value",
                panel,
                "100 / 100",
                18,
                TextAnchor.MiddleRight,
                new Vector2(0.55f, 0.56f),
                new Vector2(0.94f, 0.94f),
                Color.white
            );

            RectTransform bar = CreateRect(
                "Health Bar",
                panel,
                new Vector2(0.06f, 0.2f),
                new Vector2(0.94f, 0.48f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            AddImage(bar.gameObject, new Color(0.16f, 0.12f, 0.18f, 1f));

            RectTransform fill = CreateRect(
                "Health Fill",
                bar,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            healthFill = AddImage(fill.gameObject, new Color(1f, 0.2f, 0.58f, 1f));
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = 1f;
        }

        private void CreateSongPanel()
        {
            RectTransform panel = CreateRect(
                "Song Title Panel",
                transform,
                Vector2.one,
                Vector2.one,
                new Vector2(1f, 0f),
                new Vector2(-28f, 28f),
                new Vector2(440f, 92f)
            );
            AddImage(panel.gameObject, new Color(0.035f, 0.018f, 0.055f, 0.92f));

            songGroup = panel.gameObject.AddComponent<CanvasGroup>();
            songGroup.alpha = 0f;
            songGroup.interactable = false;
            songGroup.blocksRaycasts = false;

            RectTransform accent = CreateRect(
                "Pink Accent",
                panel,
                new Vector2(0f, 0f),
                new Vector2(0.018f, 1f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            AddImage(accent.gameObject, new Color(1f, 0.18f, 0.56f, 1f));

            Text nowPlaying = CreateText(
                "Now Playing",
                panel,
                "СЕЙЧАС ИГРАЕТ",
                15,
                TextAnchor.MiddleLeft,
                new Vector2(0.07f, 0.58f),
                new Vector2(0.94f, 0.9f),
                new Color(1f, 0.62f, 0.82f)
            );
            nowPlaying.fontStyle = FontStyle.Bold;

            songTitle = CreateText(
                "Song Title",
                panel,
                string.Empty,
                21,
                TextAnchor.MiddleLeft,
                new Vector2(0.07f, 0.12f),
                new Vector2(0.94f, 0.62f),
                Color.white
            );
            songTitle.resizeTextForBestFit = true;
            songTitle.resizeTextMinSize = 14;
            songTitle.resizeTextMaxSize = 21;
            songTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static RectTransform CreateRect(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image AddImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            string value,
            int size,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            RectTransform rect = CreateRect(
                objectName,
                parent,
                anchorMin,
                anchorMax,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size - 5);
            text.resizeTextMaxSize = size;
            return text;
        }
    }
}