using System.Collections.Generic;
using UnityEngine;

namespace WarChess.Audio
{
    /// <summary>
    /// Sound effect identifiers for all game actions.
    /// </summary>
    public enum SoundEvent
    {
        UnitPlace,
        UnitAttack,
        UnitDeath,
        UnitCharge,
        UnitAbility,
        BattleStart,
        BattleWin,
        BattleLose,
        BattleDraw,
        ButtonClick,
        ButtonBack,
        MenuOpen,
        MenuClose,
        PurchaseSuccess,
        DispatchBoxOpen,
        StarEarned,
        TierPromotion,
        OfficerLevelUp,
        DeployArmy,
        ErrorBuzz
    }

    /// <summary>
    /// Manages sound effect playback. Reads SfxVolume from player settings.
    /// MonoBehaviour — requires AudioSource for playback.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private float _sfxVolume = 1.0f;
        private AudioSource _sfxSource;
        private readonly Dictionary<SoundEvent, AudioClip> _clips = new Dictionary<SoundEvent, AudioClip>();

        /// <summary>
        /// Initializes with player settings volume.
        /// </summary>
        public void Initialize(float sfxVolume)
        {
            _sfxVolume = sfxVolume;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        /// <summary>Plays a sound effect at the current SFX volume.</summary>
        public void Play(SoundEvent evt)
        {
            if (_sfxSource == null) return;
            if (!_clips.TryGetValue(evt, out AudioClip clip)) return;
            if (clip == null) return;

            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        /// <summary>Plays a sound at a specific volume override (0-1).</summary>
        public void PlayAtVolume(SoundEvent evt, float volume)
        {
            if (_sfxSource == null) return;
            if (!_clips.TryGetValue(evt, out AudioClip clip)) return;
            if (clip == null) return;

            _sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>Registers an AudioClip for a SoundEvent.</summary>
        public void RegisterClip(SoundEvent evt, AudioClip clip)
        {
            _clips[evt] = clip;
        }

        /// <summary>Updates volume from settings (call after settings change).</summary>
        public void SetVolume(float sfxVolume)
        {
            _sfxVolume = Mathf.Clamp01(sfxVolume);
        }

        /// <summary>Returns true if a clip is registered for this event.</summary>
        public bool HasClip(SoundEvent evt)
        {
            return _clips.ContainsKey(evt) && _clips[evt] != null;
        }
    }
}
