using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MechChess.UI
{
    /// <summary>
    /// AegisMusicManager: Dedicated in-game Background Music & Jukebox system.
    /// - Scans Assets/Resources/Audio/Music for user-provided .mp3, .wav, .ogg clips.
    /// - Provides procedural ambient & cyber synthwave fallback tracks if folder is empty.
    /// - Supports Next/Prev, Play/Pause, Shuffle, Volume, and auto-advancing playlists.
    /// </summary>
    public class AegisMusicManager : MonoBehaviour
    {
        public static AegisMusicManager Instance { get; private set; }

        public class TrackInfo
        {
            public string id;
            public string title;
            public string artist;
            public AudioClip clip;
            public bool isProcedural;
            public float duration;
        }

        private const string PrefVolume = "aegis.music.volume";
        private const string PrefTrackIndex = "aegis.music.track_index";

        private AudioSource _audioSource;
        private readonly List<TrackInfo> _tracks = new List<TrackInfo>();
        private int _currentIndex = 0;
        private bool _isPlaying = false;
        private float _volume = 0.65f;

        public static event Action OnTrackChanged;
        public static event Action<bool> OnPlayStateChanged;
        public static event Action<float> OnVolumeChanged;

        public IReadOnlyList<TrackInfo> Tracks => _tracks;
        public int CurrentTrackIndex => _currentIndex;
        public bool IsPlaying => _isPlaying && _audioSource != null && _audioSource.isPlaying;
        public float Volume => _volume;

        public TrackInfo CurrentTrack
        {
            get
            {
                if (_tracks.Count == 0) return null;
                if (_currentIndex < 0 || _currentIndex >= _tracks.Count) _currentIndex = 0;
                return _tracks[_currentIndex];
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // Pure 2D stereo
            _audioSource.loop = false;       // Auto-advances to next track

            _volume = PlayerPrefs.GetFloat(PrefVolume, 0.65f);
            _audioSource.volume = _volume;

            ReloadTracks();
        }

        private void Start()
        {
            if (_tracks.Count > 0 && !IsPlaying)
            {
                int savedIdx = PlayerPrefs.GetInt(PrefTrackIndex, 0);
                Play(savedIdx);
            }
        }

        private void Update()
        {
            // Auto-advance to next track when song finishes naturally
            if (_isPlaying && _audioSource != null && !_audioSource.isPlaying && _audioSource.time == 0f && _tracks.Count > 0)
            {
                NextTrack();
            }
        }

        /// <summary>
        /// Scans Resources/Audio/Music and initializes the playlist.
        /// If user has not placed audio files yet, generates in-memory procedural cyber & fantasy tracks.
        /// </summary>
        public void ReloadTracks()
        {
            _tracks.Clear();

            // 1. Scan Resources/Audio/Music
            AudioClip[] userClips = Resources.LoadAll<AudioClip>("Audio/Music");
            if (userClips != null && userClips.Length > 0)
            {
                for (int i = 0; i < userClips.Length; i++)
                {
                    AudioClip c = userClips[i];
                    if (c == null) continue;
                    _tracks.Add(new TrackInfo
                    {
                        id = $"user_track_{i}",
                        title = FormatTrackTitle(c.name),
                        artist = "Custom BGM",
                        clip = c,
                        isProcedural = false,
                        duration = c.length
                    });
                }
            }

            // 2. Procedural Built-in Synth & Fantasy tracks
            // Always available so game never plays in silence
            _tracks.Add(CreateProceduralTrack("Aegis Cyber Horizon", "Aegis Soundworks", 0));
            _tracks.Add(CreateProceduralTrack("Thánh Địa Tinh Linh (Spirit Sanctuary)", "Aegis Soundworks", 1));
            _tracks.Add(CreateProceduralTrack("Chiến Tuyến Mech (Tactical Pulse)", "Aegis Soundworks", 2));
            _tracks.Add(CreateProceduralTrack("Pha Lê Băng Tuyết (Glacial Frost)", "Aegis Soundworks", 3));

            _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _tracks.Count - 1));
            OnTrackChanged?.Invoke();
        }

        private string FormatTrackTitle(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Không tên";
            return rawName.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        public void Play(int index = -1)
        {
            if (_tracks.Count == 0) return;

            if (index >= 0)
            {
                _currentIndex = Mathf.Clamp(index, 0, _tracks.Count - 1);
            }

            TrackInfo t = CurrentTrack;
            if (t == null || t.clip == null) return;

            _audioSource.clip = t.clip;
            _audioSource.volume = _volume;
            _audioSource.Play();
            _isPlaying = true;

            PlayerPrefs.SetInt(PrefTrackIndex, _currentIndex);
            PlayerPrefs.Save();

            OnTrackChanged?.Invoke();
            OnPlayStateChanged?.Invoke(true);
        }

        public void Pause()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Pause();
            }
            _isPlaying = false;
            OnPlayStateChanged?.Invoke(false);
        }

        public void Resume()
        {
            if (_audioSource != null)
            {
                _audioSource.UnPause();
                if (!_audioSource.isPlaying)
                {
                    Play(_currentIndex);
                }
            }
            _isPlaying = true;
            OnPlayStateChanged?.Invoke(true);
        }

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause();
            else Resume();
        }

        public void NextTrack()
        {
            if (_tracks.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _tracks.Count;
            Play(_currentIndex);
        }

        public void PreviousTrack()
        {
            if (_tracks.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _tracks.Count) % _tracks.Count;
            Play(_currentIndex);
        }

        public void SetVolume(float vol)
        {
            _volume = Mathf.Clamp01(vol);
            if (_audioSource != null) _audioSource.volume = _volume;
            PlayerPrefs.SetFloat(PrefVolume, _volume);
            PlayerPrefs.Save();
            OnVolumeChanged?.Invoke(_volume);
        }

        // =========================================================================
        // PROCEDURAL IN-MEMORY AUDIO SYNTHESIS (Clean fallback soundtrack)
        // =========================================================================
        private static readonly Dictionary<int, AudioClip> _synthClipCache = new Dictionary<int, AudioClip>();

        private TrackInfo CreateProceduralTrack(string title, string artist, int style)
        {
            AudioClip clip;
            if (!_synthClipCache.TryGetValue(style, out clip) || clip == null)
            {
                clip = SynthesizeLoFiTrack(style);
                _synthClipCache[style] = clip;
            }

            return new TrackInfo
            {
                id = $"proc_track_{style}",
                title = title,
                artist = artist,
                clip = clip,
                isProcedural = true,
                duration = clip != null ? clip.length : 16f
            };
        }

        private AudioClip SynthesizeLoFiTrack(int style)
        {
            int sampleRate = 22050;
            float duration = 16.0f; // 16 second seamlessly looping chord progression
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            // Harmonic frequencies based on style
            float[][] chordChords = style switch
            {
                0 => new float[][] { // Cyber Horizon (Am - F - C - G)
                    new float[] { 220.0f, 261.63f, 329.63f },
                    new float[] { 174.61f, 220.00f, 261.63f },
                    new float[] { 130.81f, 164.81f, 196.00f },
                    new float[] { 196.00f, 246.94f, 293.66f }
                },
                1 => new float[][] { // Spirit Sanctuary (Pentatonic Lotus)
                    new float[] { 293.66f, 369.99f, 440.00f },
                    new float[] { 246.94f, 329.63f, 392.00f },
                    new float[] { 220.00f, 277.18f, 329.63f },
                    new float[] { 196.00f, 246.94f, 293.66f }
                },
                2 => new float[][] { // Tactical Pulse (Dm - Bb - F - C)
                    new float[] { 146.83f, 220.00f, 293.66f },
                    new float[] { 116.54f, 174.61f, 233.08f },
                    new float[] { 174.61f, 261.63f, 349.23f },
                    new float[] { 130.81f, 196.00f, 261.63f }
                },
                _ => new float[][] { // Glacial Frost
                    new float[] { 329.63f, 392.00f, 493.88f },
                    new float[] { 261.63f, 329.63f, 392.00f },
                    new float[] { 220.00f, 261.63f, 329.63f },
                    new float[] { 246.94f, 311.13f, 369.99f }
                }
            };

            float chordDuration = duration / chordChords.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int chordIdx = Mathf.FloorToInt(t / chordDuration) % chordChords.Length;
                float[] notes = chordChords[chordIdx];
                float chordT = t % chordDuration;

                // Soft attack / decay envelope per chord
                float env = Mathf.Sin(chordT / chordDuration * Mathf.PI);

                float sample = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float freq = notes[n];
                    // Fundamental sine + warm sub-octave + gentle shimmer
                    sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f;
                    sample += Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.25f;
                    sample += Mathf.Sin(2f * Mathf.PI * (freq * 2.0f) * t) * 0.10f;
                }

                // Add subtle warm tape noise & pulse
                float pulse = 0.85f + 0.15f * Mathf.Sin(t * 4.0f * Mathf.PI);
                sample = (sample / notes.Length) * env * pulse * 0.28f;

                // Soft fade at loop boundary
                float loopFade = 1f;
                if (t < 0.2f) loopFade = t / 0.2f;
                else if (t > duration - 0.2f) loopFade = (duration - t) / 0.2f;

                data[i] = Mathf.Clamp(sample * loopFade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create($"AegisBGM_Style_{style}", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void EnsureInitialized()
        {
            if (Instance != null) return;
            GameObject host = GameObject.Find("Aegis_Music_Host");
            if (host == null)
            {
                host = new GameObject("Aegis_Music_Host");
                host.AddComponent<AegisMusicManager>();
            }
        }
    }
}
