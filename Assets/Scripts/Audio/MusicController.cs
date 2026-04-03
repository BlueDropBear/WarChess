using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WarChess.Audio
{
    /// <summary>
    /// Music track identifiers for different game screens and states.
    /// </summary>
    public enum MusicTrack
    {
        MainMenu,
        ArmyBuilder,
        CampaignMap,
        BattleCalm,
        BattleIntense,
        Victory,
        Defeat,
        Shop
    }

    /// <summary>
    /// Manages background music playback with crossfade support.
    /// MonoBehaviour — uses two AudioSources for smooth crossfading.
    /// </summary>
    public class MusicController : MonoBehaviour
    {
        private float _musicVolume = 1.0f;
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _aIsActive = true;
        private readonly Dictionary<MusicTrack, AudioClip> _tracks = new Dictionary<MusicTrack, AudioClip>();
        private MusicTrack? _currentTrack;
        private Coroutine _fadeCoroutine;

        /// <summary>Currently playing track, or null.</summary>
        public MusicTrack? CurrentTrack => _currentTrack;

        /// <summary>
        /// Initializes with player settings volume.
        /// </summary>
        public void Initialize(float musicVolume)
        {
            _musicVolume = musicVolume;

            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceA.playOnAwake = false;
            _sourceA.loop = true;
            _sourceA.volume = _musicVolume;

            _sourceB = gameObject.AddComponent<AudioSource>();
            _sourceB.playOnAwake = false;
            _sourceB.loop = true;
            _sourceB.volume = 0f;
        }

        /// <summary>Plays a music track immediately. If already playing, does nothing.</summary>
        public void PlayTrack(MusicTrack track)
        {
            if (_currentTrack == track) return;
            if (!_tracks.TryGetValue(track, out AudioClip clip)) return;
            if (clip == null) return;

            // Stop any ongoing fade
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            var active = _aIsActive ? _sourceA : _sourceB;
            var inactive = _aIsActive ? _sourceB : _sourceA;

            inactive.Stop();
            inactive.volume = 0f;

            active.clip = clip;
            active.volume = _musicVolume;
            active.Play();

            _currentTrack = track;
        }

        /// <summary>Crossfades to a new track over the given duration in seconds.</summary>
        public void CrossFadeTo(MusicTrack track, float durationSeconds = 1.0f)
        {
            if (_currentTrack == track) return;
            if (!_tracks.TryGetValue(track, out AudioClip clip)) return;
            if (clip == null) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(CrossFadeCoroutine(clip, durationSeconds));
            _currentTrack = track;
        }

        /// <summary>Stops music playback.</summary>
        public void Stop()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _sourceA.Stop();
            _sourceB.Stop();
            _currentTrack = null;
        }

        /// <summary>Pauses or resumes music.</summary>
        public void SetPaused(bool paused)
        {
            var active = _aIsActive ? _sourceA : _sourceB;
            if (paused)
                active.Pause();
            else
                active.UnPause();
        }

        /// <summary>Registers an AudioClip for a MusicTrack.</summary>
        public void RegisterTrack(MusicTrack track, AudioClip clip)
        {
            _tracks[track] = clip;
        }

        /// <summary>Updates volume from settings (call after settings change).</summary>
        public void SetVolume(float musicVolume)
        {
            _musicVolume = Mathf.Clamp01(musicVolume);
            var active = _aIsActive ? _sourceA : _sourceB;
            if (active.isPlaying)
                active.volume = _musicVolume;
        }

        private IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
        {
            var fadeOut = _aIsActive ? _sourceA : _sourceB;
            var fadeIn = _aIsActive ? _sourceB : _sourceA;

            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float elapsed = 0f;
            float startVolume = fadeOut.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
                fadeIn.volume = Mathf.Lerp(0f, _musicVolume, t);

                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = 0f;
            fadeIn.volume = _musicVolume;

            _aIsActive = !_aIsActive;
            _fadeCoroutine = null;
        }
    }
}
