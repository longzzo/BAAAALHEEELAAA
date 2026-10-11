using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valhalla
{
    public enum CombatCue
    {
        LightWindup, HeavyWindup, LightHit, HeavyHit, Miss, Dash, Parry, Hurt, Headbutt, Throw,
        PropImpact, GateOpen, GateBreak, Pickup, RunePower, RuneGuard, RuneHeal
    }

    // Synthetic-clip 2D SFX pool. Combat rules never depend on this; it only listens and plays.
    public sealed class CombatAudio : MonoBehaviour
    {
        const int VoiceCount = 8;
        const float PerCueCooldown = .055f;
        const float MaxGain = .3f;
        // Eight voices * max .3 * master .45 * waveform peak .82 stays below full scale.
        const float MasterGain = .45f;

        readonly Dictionary<CombatCue, AudioClip> clips = new Dictionary<CombatCue, AudioClip>();
        readonly Dictionary<CombatCue, float> lastPlayedAt = new Dictionary<CombatCue, float>();
        AudioSource[] voices;
        float[] voiceLastUsed;

        public int CachedClipCount { get; private set; }
        public int PlayedCount { get; private set; }
        public CombatCue LastCue { get; private set; }
        public bool MenuPaused { get; private set; }

        void Awake()
        {
            voices = new AudioSource[VoiceCount];
            voiceLastUsed = new float[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                voices[i] = source;
                voiceLastUsed[i] = float.NegativeInfinity;
            }

            foreach (CombatCue cue in Enum.GetValues(typeof(CombatCue)))
            {
                var clip = SoundPalette.Create(cue);
                if (clip == null) continue;
                clips[cue] = clip;
                lastPlayedAt[cue] = float.NegativeInfinity;
            }
            CachedClipCount = clips.Count;
        }

        void OnEnable() { TattooSignals.Activated += OnTattooActivated; }

        void OnDisable()
        {
            TattooSignals.Activated -= OnTattooActivated;
            if (voices == null) return;
            foreach (var source in voices) if(source)source.Stop();
        }

        void OnDestroy()
        {
            foreach (var clip in clips.Values) Destroy(clip);
            clips.Clear();
        }

        public void Play(CombatCue cue)
        {
            if (!enabled || !gameObject.activeInHierarchy || MenuPaused) return;
            if (!clips.TryGetValue(cue, out var clip) || clip == null) return;

            float now = Time.unscaledTime;
            if (now - lastPlayedAt[cue] < PerCueCooldown) return;
            lastPlayedAt[cue] = now;

            var (pitch, volume) = Profile(cue);
            int index = ChooseVoice();
            var voice = voices[index];
            voice.clip = clip;
            voice.pitch = 1f;
            voice.volume = Mathf.Min(volume, MaxGain) * MasterGain;
            voice.Play();
            voiceLastUsed[index] = now;

            LastCue = cue;
            PlayedCount++;
        }

        public void SetMenuPaused(bool paused)
        {
            MenuPaused = paused;
            if (voices == null) return;
            foreach (var source in voices)
            {
                if(!source)continue;
                if (paused) source.Pause();
                else source.UnPause();
            }
        }

        int ChooseVoice()
        {
            for (int i = 0; i < voices.Length; i++)
                if (!voices[i].isPlaying) return i;

            int oldest = 0;
            for (int i = 1; i < voices.Length; i++)
                if (voiceLastUsed[i] < voiceLastUsed[oldest]) oldest = i;
            return oldest;
        }

        static (float pitch, float volume) Profile(CombatCue cue)
        {
            switch (cue)
            {
                case CombatCue.LightWindup: return (1.05f, .12f);
                case CombatCue.HeavyWindup: return (.85f, .18f);
                case CombatCue.LightHit: return (1.0f, .22f);
                case CombatCue.HeavyHit: return (.8f, .3f);
                case CombatCue.Miss: return (1.15f, .10f);
                case CombatCue.Dash: return (1.1f, .15f);
                case CombatCue.Parry: return (1.2f, .25f);
                case CombatCue.Hurt: return (.9f, .2f);
                case CombatCue.Headbutt: return (.75f, .28f);
                case CombatCue.Throw: return (1.0f, .2f);
                case CombatCue.PropImpact: return (.7f, .25f);
                case CombatCue.GateOpen: return (.9f, .22f);
                case CombatCue.GateBreak: return (.65f, .3f);
                case CombatCue.Pickup: return (1.3f, .12f);
                case CombatCue.RunePower: return (1.1f, .24f);
                case CombatCue.RuneGuard: return (1.0f, .22f);
                case CombatCue.RuneHeal: return (1.2f, .18f);
                default: return (1f, .15f);
            }
        }

        void OnTattooActivated(TattooPower power) => Play(RuneCueFor(power));

        static CombatCue RuneCueFor(TattooPower power)
        {
            switch (power)
            {
                case TattooPower.Blood:
                case TattooPower.Valor:
                    return CombatCue.RuneHeal;
                case TattooPower.Reprisal:
                case TattooPower.Frost:
                case TattooPower.SecondWind:
                case TattooPower.Recall:
                case TattooPower.Countermark:
                case TattooPower.GiantCounter:
                    return CombatCue.RuneGuard;
                default:
                    return CombatCue.RunePower;
            }
        }
    }
}
