using System;
using UnityEngine;

namespace Valhalla
{
    // Original procedural foley: independent deterministic noise never consumes gameplay Random.
    // Each sound has a different transient, body and decay rather than a pitched copy of one beep.
    public static class SoundPalette
    {
        public const int SampleRate = 44100;
        public static AudioClip Create(CombatCue cue)
        {
            float duration = Duration(cue);
            var samples = new float[Mathf.CeilToInt(duration * SampleRate)];
            uint noiseState = 7919u + (uint)cue * 104729u;
            float lowNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate, u = t / duration;
                noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
                float white = (noiseState & 0xffffff) / 8388607.5f - 1;
                lowNoise += .075f * (white - lowNoise);
                float highNoise = white - lowNoise;
                float sample;
                switch (cue)
                {
                    case CombatCue.LightWindup: sample = Swish(u, highNoise, lowNoise, .35f); break;
                    case CombatCue.HeavyWindup: sample = Swish(u, highNoise, lowNoise, .14f) + .22f * Chirp(t, 130, -110) * Mathf.Sin(Mathf.PI * u); break;
                    case CombatCue.Miss: sample = Swish(u, highNoise, lowNoise, .55f); break;
                    case CombatCue.Dash: sample = Swish(u, highNoise, lowNoise, .25f) + .2f * lowNoise * Decay(t, 12); break;
                    case CombatCue.Throw: sample = Swish(u, highNoise, lowNoise, .2f) + .2f * Chirp(t, 170, -240) * Decay(t, 12); break;
                    case CombatCue.LightHit: sample = Impact(t, white, lowNoise, 150, 65, 30); break;
                    case CombatCue.HeavyHit: sample = Impact(t, white, lowNoise, 94, 43, 14) + .25f * Modal(t, 230, 22); break;
                    case CombatCue.Hurt: sample = Impact(t, white, lowNoise, 117, 47, 19); break;
                    case CombatCue.Headbutt: sample = Impact(t, white, lowNoise, 180, 72, 27); break;
                    case CombatCue.PropImpact: sample = Impact(t, white, lowNoise, 128, 52, 15) + .3f * Modal(t, 370, 28); break;
                    case CombatCue.GateBreak: sample = Impact(t, white, lowNoise, 87, 33, 9) + .8f * lowNoise * Decay(t, 8) + .14f * white * Decay(t, 20) * Mathf.Sin(t * 173); break;
                    case CombatCue.GateOpen: sample = .3f * Modal(t, 480, 9) + .6f * lowNoise * Decay(t, 10) * (.5f + .5f * Mathf.Sin(t * 91)); break;
                    case CombatCue.Parry: sample = .62f * Modal(t, 1180, 13) + .13f * white * Decay(t, 160) + .18f * Chirp(t, 240, -150) * Decay(t, 35); break;
                    case CombatCue.Pickup: sample = .45f * Modal(t, 1540, 34) + .08f * white * Decay(t, 190); break;
                    case CombatCue.RunePower: sample = Rune(t, u, 146.83f, .75f) + .6f * lowNoise * Decay(t, 10); break;
                    case CombatCue.RuneGuard: sample = Rune(t, u, 220, .6f) + .18f * Modal(t, 880, 16); break;
                    default: sample = Rune(t, u, 293.66f, .8f); break;
                }
                // Two-ms attack and final fade suppress clicks; soft saturation bounds every sample.
                float fade = Mathf.Min(1, t / .002f) * Mathf.Min(1, (duration - t) / .025f);
                samples[i] = .82f * (float)Math.Tanh(sample * 1.5f) * fade;
            }
            var clip = AudioClip.Create("Valhalla " + cue, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Duration(CombatCue cue)
        {
            switch(cue)
            {
                case CombatCue.HeavyHit: return .48f;
                case CombatCue.HeavyWindup: return .28f;
                case CombatCue.GateBreak: return .7f;
                case CombatCue.GateOpen: return .42f;
                case CombatCue.Parry: return .5f;
                case CombatCue.RunePower: case CombatCue.RuneGuard: case CombatCue.RuneHeal: return .6f;
                case CombatCue.PropImpact: return .4f;
                case CombatCue.Pickup: return .16f;
                default: return .24f;
            }
        }
        static float Decay(float t, float rate) => Mathf.Exp(-t * rate);
        static float Chirp(float t, float start, float slope) => Mathf.Sin(2 * Mathf.PI * (start * t + .5f * slope * t * t));
        static float Modal(float t, float frequency, float decay)
        {
            return (Mathf.Sin(t * frequency * 2 * Mathf.PI) * .55f +
                Mathf.Sin(t * frequency * 2.71f * 2 * Mathf.PI) * .28f +
                Mathf.Sin(t * frequency * 4.13f * 2 * Mathf.PI) * .17f) * Decay(t, decay);
        }
        static float Swish(float u, float high, float low, float brightness)
            => (high * brightness + low * 2) * Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.5f);
        static float Impact(float t, float noise, float low, float start, float end, float decay)
        {
            float phase = 2 * Mathf.PI * (end * t + (start - end) * (1 - Mathf.Exp(-t * 30)) / 30);
            return .68f * Mathf.Sin(phase) * Decay(t, decay) + .4f * noise * Decay(t, 110) + 1.1f * low * Decay(t, decay * 1.5f);
        }
        static float Rune(float t, float u, float fundamental, float strength)
            => strength * (Mathf.Sin(2 * Mathf.PI * fundamental * t) + .3f * Mathf.Sin(2 * Mathf.PI * fundamental * 1.5f * t)) * Mathf.Sin(Mathf.PI * u) * Decay(t, 6);
    }
}
