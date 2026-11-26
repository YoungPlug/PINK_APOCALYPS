using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using PinkApocalypse.Player;

namespace PinkApocalypse.Managers
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundtrackPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private AudioClip[] tracks;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private Coroutine advanceRoutine;
        private int playbackVersion;
        private bool playbackStarted;

        public int CurrentTrackIndex { get; private set; } = -1;
        public string CurrentTrackName => CurrentTrackIndex >= 0 && tracks != null && CurrentTrackIndex < tracks.Length
            ? tracks[CurrentTrackIndex].name
            : string.Empty;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (input == null)
            {
                PlayerMotor player = FindObjectOfType<PlayerMotor>();
                if (player != null) input = player.GetComponent<PlayerInputReader>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;
            audioSource.priority = 0;
        }

        private void Start()
        {
            if (tracks == null || tracks.Length == 0)
            {
                Debug.LogWarning("SoundtrackPlayer has no tracks assigned.", this);
                return;
            }

            CurrentTrackIndex = 0;
            audioSource.clip = tracks[CurrentTrackIndex];

#if !UNITY_WEBGL || UNITY_EDITOR
            PlaySelectedTrack();
#endif
        }

        private void Update()
        {
            if (input != null)
            {
                if (input.PreviousTrackPressed) ChangeTrack(-1);
                else if (input.NextTrackPressed) ChangeTrack(1);
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
            CurrentTrackIndex = (CurrentTrackIndex + direction + tracks.Length) % tracks.Length;
            audioSource.clip = tracks[CurrentTrackIndex];

            if (playbackStarted) PlaySelectedTrack();
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
