using System;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Sound effects and background music, fully synthesized at startup
    /// (no audio assets needed). Draw blips rise in pitch with path length.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int SampleRate = 44100;

        AudioSource sfx;
        AudioSource music;
        AudioClip pick, draw, pairComplete, win, click;

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            Instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.volume = 0.8f;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.45f;

            click = Blip("click", 880f, 0.04f);
            pick = Blip("pick", 520f, 0.07f);
            draw = Blip("draw", 660f, 0.05f);
            pairComplete = Sequence("pair", new[] { 523.25f, 783.99f }, 0.13f);
            win = Sequence("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f);
            music.clip = PadLoop();

            ApplyMusicSetting();
        }

        public void PlayClick() => PlaySfx(click, 1f);
        public void PlayPick() => PlaySfx(pick, 1f);
        public void PlayPairComplete() => PlaySfx(pairComplete, 1f);
        public void PlayWin() => PlaySfx(win, 1f);

        public void PlayDraw(int pathLength)
        {
            PlaySfx(draw, 1f + 0.03f * Mathf.Min(pathLength, 20));
        }

        void PlaySfx(AudioClip clip, float pitch)
        {
            if (clip == null || !SaveSystem.SoundOn) return;
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip);
        }

        public void ApplyMusicSetting()
        {
            if (music.clip == null) return;
            if (SaveSystem.MusicOn) { if (!music.isPlaying) music.Play(); }
            else music.Stop();
        }

        // ---- synthesis ----

        static AudioClip Blip(string name, float freq, float duration)
        {
            return Render(name, duration, t =>
                Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 22f) * 0.5f);
        }

        static AudioClip Sequence(string name, float[] freqs, float noteDuration)
        {
            float total = freqs.Length * noteDuration;
            return Render(name, total, t =>
            {
                int i = Mathf.Min((int)(t / noteDuration), freqs.Length - 1);
                float local = t - i * noteDuration;
                return Mathf.Sin(2f * Mathf.PI * freqs[i] * local) * Mathf.Exp(-local * 12f) * 0.45f;
            });
        }

        static AudioClip PadLoop()
        {
            // Four soft sine chords, 2s each, with a smooth window so the loop has no clicks.
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f }, // Am
                new[] { 174.61f, 220.00f, 261.63f }, // F
                new[] { 261.63f, 329.63f, 392.00f }, // C
                new[] { 196.00f, 246.94f, 392.00f }, // G
            };
            const float chordDuration = 2f;
            return Render("music", chords.Length * chordDuration, t =>
            {
                int i = Mathf.Min((int)(t / chordDuration), chords.Length - 1);
                float local = t - i * chordDuration;
                float window = Mathf.Sin(Mathf.PI * local / chordDuration);
                float sample = 0f;
                foreach (var freq in chords[i])
                    sample += Mathf.Sin(2f * Mathf.PI * freq * t);
                return sample / chords[i].Length * window * 0.14f;
            });
        }

        static AudioClip Render(string name, float duration, Func<float, float> sample)
        {
#if UNITY_LUNA
            // Luna playables cannot create AudioClips at runtime — ship silent
            // (ad networks auto-mute playables by default anyway).
            return null;
#else
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = sample(i / (float)SampleRate);
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
#endif
        }
    }
}
