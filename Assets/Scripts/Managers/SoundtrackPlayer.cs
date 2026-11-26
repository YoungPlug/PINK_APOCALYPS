using System;
using System.Collections;
using System.Collections.Generic;
using PinkApocalypse.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PinkApocalypse.Managers
{
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class SoundtrackPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private AudioClip[] tracks;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private static SoundtrackPlayer instance;
        public static SoundtrackPlayer Instance => instance;

        private readonly List<int> shuffledBag = new List<int>();
        private readonly List<int> playbackHistory = new List<int>();
        private Coroutine advanceRoutine;
        private int bagCursor;
        private int historyCursor = -1;
        private int playbackVersion;
        private bool playbackStarted;
        private bool duplicateInstance;

        public int CurrentTrackIndex { get; private set; } = -1;
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;
        public event Action<string> TrackStarted;
        public string CurrentTrackName => CurrentTrackIndex >= 0 && tracks != null && CurrentTrackIndex < tracks.Length
            ? tracks[CurrentTrackIndex].name
            : string.Empty;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                duplicateInstance = true;
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;
            audioSource.priority = 0;

            BindPlayerInput();
        }

        private void OnEnable()
        {
            if (!duplicateInstance) UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Start()
        {
            if (duplicateInstance || instance != this) return;
            if (tracks == null || tracks.Length == 0)
            {
                Debug.LogWarning("SoundtrackPlayer has no tracks assigned.", this);
                return;
            }

            RebuildShuffleBag(-1);
            PlayNextRandomTrack();
        }

        private void Update()
        {
            if (duplicateInstance || instance != this) return;

            if (input != null)
            {
                if (input.PreviousTrackPressed) PreviousTrack();
                else if (input.NextTrackPressed) NextTrack();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Browsers require a real keyboard or pointer gesture before starting audio.
            if (!playbackStarted && HasBrowserAudioGesture()) PlaySelectedTrack();
#endif
        }

        public void NextTrack() => ChangeTrack(1);

        public void PreviousTrack() => ChangeTrack(-1);

        private void ChangeTrack(int direction)
        {
            if (tracks == null || tracks.Length == 0) return;

            int targetHistoryPosition;
            if (direction < 0)
            {
                targetHistoryPosition = historyCursor - 1;
                if (targetHistoryPosition < 0) return;
            }
            else if (historyCursor < playbackHistory.Count - 1)
            {
                targetHistoryPosition = historyCursor + 1;
            }
            else
            {
                int nextTrack = DrawNextRandomTrack();
                playbackHistory.Add(nextTrack);
                targetHistoryPosition = playbackHistory.Count - 1;
            }

            historyCursor = targetHistoryPosition;
            CurrentTrackIndex = playbackHistory[historyCursor];
            audioSource.clip = tracks[CurrentTrackIndex];
            if (playbackStarted) PlaySelectedTrack();
        }

        private void PlayNextRandomTrack()
        {
            int nextTrack = DrawNextRandomTrack();
            playbackHistory.Add(nextTrack);
            historyCursor = playbackHistory.Count - 1;
            CurrentTrackIndex = nextTrack;
            audioSource.clip = tracks[CurrentTrackIndex];

#if !UNITY_WEBGL || UNITY_EDITOR
            PlaySelectedTrack();
#endif
        }

        private int DrawNextRandomTrack()
        {
            if (bagCursor >= shuffledBag.Count)
                RebuildShuffleBag(CurrentTrackIndex);

            return shuffledBag[bagCursor++];
        }

        private void RebuildShuffleBag(int previousTrack)
        {
            shuffledBag.Clear();
            for (int i = 0; i < tracks.Length; i++) shuffledBag.Add(i);

            for (int i = shuffledBag.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (shuffledBag[i], shuffledBag[swapIndex]) = (shuffledBag[swapIndex], shuffledBag[i]);
            }

            // Avoid repeating the previous song at the boundary between shuffled rounds.
            if (shuffledBag.Count > 1 && shuffledBag[0] == previousTrack)
            {
                int swapIndex = UnityEngine.Random.Range(1, shuffledBag.Count);
                (shuffledBag[0], shuffledBag[swapIndex]) = (shuffledBag[swapIndex], shuffledBag[0]);
            }

            bagCursor = 0;
        }

        private void PlaySelectedTrack()
        {
            if (tracks == null || tracks.Length == 0 || CurrentTrackIndex < 0) return;

            playbackStarted = true;
            playbackVersion++;
            if (advanceRoutine != null) StopCoroutine(advanceRoutine);

            audioSource.Stop();
            audioSource.clip = tracks[CurrentTrackIndex];
            audioSource.time = 0f;
            audioSource.Play();
            TrackStarted?.Invoke(CurrentTrackName);

            int version = playbackVersion;
            advanceRoutine = StartCoroutine(AdvanceAfterTrack(version, audioSource.clip));
        }

        private IEnumerator AdvanceAfterTrack(int version, AudioClip track)
        {
            while (version == playbackVersion && audioSource.clip == track &&
                   !audioSource.isPlaying && audioSource.time <= 0.01f)
                yield return null;

            while (version == playbackVersion && audioSource.clip == track && audioSource.isPlaying)
                yield return null;

            if (version == playbackVersion && audioSource.clip == track && audioSource.time > 0.05f)
                ChangeTrack(1);
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            BindPlayerInput();
        }

        private void BindPlayerInput()
        {
            PlayerMotor player = FindObjectOfType<PlayerMotor>();
            input = player != null ? player.GetComponent<PlayerInputReader>() : null;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static bool HasBrowserAudioGesture()
        {
            bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return keyPressed || mousePressed;
        }
#endif
    }
}