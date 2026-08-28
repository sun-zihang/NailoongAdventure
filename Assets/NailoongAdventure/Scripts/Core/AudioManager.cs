using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 音频管理：音效池 + BGM 交叉淡入淡出。
    /// 所有音效均由 Assets/Audio 下的程序化 WAV 提供；若缺失则运行时合成，保证任何情况下都有声。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音量")]
        [Range(0, 1)] public float masterVolume = 0.85f;
        [Range(0, 1)] public float sfxVolume = 1f;
        [Range(0, 1)] public float musicVolume = 0.55f;

        const int POOL_SIZE = 12;
        readonly List<AudioSource> sfxSources = new List<AudioSource>();
        AudioSource musicA, musicB;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> synthCache = new Dictionary<string, AudioClip>();
        Coroutine musicFade;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var go = new GameObject("SFX_" + i);
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = sfxVolume * masterVolume;
                sfxSources.Add(src);
            }

            musicA = CreateMusicSource("MusicA");
            musicB = CreateMusicSource("MusicB");
        }

        AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            return src;
        }

        // ---------- 音效 ----------
        public void Play(string clipName, float volumeScale = 1f, float pitch = 1f, bool loop = false)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;
            var src = GetFreeSource();
            if (src == null) return;
            src.clip = clip;
            src.loop = loop;
            src.pitch = pitch;
            src.volume = sfxVolume * masterVolume * volumeScale;
            src.Play();
        }

        public void PlayAt(string clipName, Vector3 position, float volumeScale = 1f)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, masterVolume * sfxVolume * volumeScale);
        }

        public void StopAllSfx()
        {
            foreach (var s in sfxSources) if (s.isPlaying) s.Stop();
        }

        AudioSource GetFreeSource()
        {
            foreach (var s in sfxSources) if (!s.isPlaying) return s;
            // 全部占用时抢占最早的一个
            var oldest = sfxSources[0];
            foreach (var s in sfxSources) if (s.time > oldest.time) oldest = s;
            return oldest;
        }

        AudioClip GetClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out var hit)) return hit;

            var clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) clip = SynthClip(name);   // 程序化回退
            if (clip != null) cache[name] = clip;
            return clip;
        }

        // ---------- 音乐 ----------
        public void PlayMusic(string clipName, float fadeTime = 1.2f)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            var target = musicA.isPlaying ? musicB : musicA;
            var other = musicA.isPlaying ? musicA : musicB;
            if (target.clip == clip && target.isPlaying) return;

            target.clip = clip;
            target.Play();
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFadeRoutine(target, other, fadeTime));
        }

        IEnumerator CrossFadeRoutine(AudioSource fadeIn, AudioSource fadeOut, float time)
        {
            float t = 0f;
            float startIn = fadeIn.volume;
            float startOut = fadeOut.volume;
            float goal = musicVolume * masterVolume;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / time);
                fadeIn.volume = Mathf.Lerp(startIn, goal, k);
                fadeOut.volume = Mathf.Lerp(startOut, 0f, k);
                yield return null;
            }
            fadeIn.volume = goal;
            fadeOut.volume = 0f;
            if (fadeOut.isPlaying) fadeOut.Stop();
        }

        // ---------- 运行时合成（资源缺失时的兜底） ----------
        AudioClip SynthClip(string name)
        {
            if (synthCache.TryGetValue(name, out var c)) return c;
            AudioClip clip = null;
            int rate = 44100;

            switch (name)
            {
                case "sfx_jump":      clip = SynthTone(rate, 0.22f, 320, 720, WaveType.Sine); break;
                case "sfx_land":      clip = SynthNoise(rate, 0.18f, 0.55f, 900); break;
                case "sfx_swing":     clip = SynthNoise(rate, 0.16f, 0.35f, 4200, true); break;
                case "sfx_hit":       clip = SynthTone(rate, 0.14f, 220, 90, WaveType.Square); break;
                case "sfx_pickup":    clip = SynthTone(rate, 0.30f, 660, 1320, WaveType.Sine); break;
                case "sfx_breath":    clip = SynthNoise(rate, 1.0f, 0.6f, 1600, true); break;
                case "sfx_slam":      clip = SynthTone(rate, 0.55f, 180, 40, WaveType.Saw); break;
                case "sfx_dash":      clip = SynthNoise(rate, 0.35f, 0.4f, 2600, true); break;
                case "sfx_hurt":      clip = SynthTone(rate, 0.35f, 520, 180, WaveType.Square); break;
                case "sfx_enemy_die": clip = SynthTone(rate, 0.4f, 400, 80, WaveType.Saw); break;
                case "sfx_ui":        clip = SynthTone(rate, 0.10f, 880, 1200, WaveType.Sine); break;
                case "sfx_levelclear":clip = SynthTone(rate, 0.9f, 523, 1046, WaveType.Triangle); break;
                default:              clip = SynthTone(rate, 0.2f, 440, 440, WaveType.Sine); break;
            }
            if (clip != null) synthCache[name] = clip;
            return clip;
        }

        enum WaveType { Sine, Square, Saw, Triangle }

        AudioClip SynthTone(int rate, float dur, float f0, float f1, WaveType type)
        {
            int len = Mathf.CeilToInt(dur * rate);
            var clip = AudioClip.Create("synth_" + type + f0, len, 1, rate, false);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float f = Mathf.Lerp(f0, f1, t);
                float phase = 2f * Mathf.PI * f * i / rate;
                float v = type switch
                {
                    WaveType.Square => Mathf.Sign(Mathf.Sin(phase)),
                    WaveType.Saw => 2f * ((f * i / rate) % 1f) - 1f,
                    WaveType.Triangle => 2f * Mathf.Abs(2f * ((f * i / rate) % 1f) - 1f) - 1f,
                    _ => Mathf.Sin(phase)
                };
                float env = Mathf.Sin(Mathf.PI * t);          // 淡入淡出包络
                data[i] = v * env * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip SynthNoise(int rate, float dur, float amp, float cutoff, bool sweep = false)
        {
            int len = Mathf.CeilToInt(dur * rate);
            var clip = AudioClip.Create("synth_noise" + cutoff, len, 1, rate, false);
            var data = new float[len];
            float last = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float white = Random.Range(-1f, 1f);
                float k = sweep ? Mathf.Lerp(1f, 0.05f, t) : 0.5f;
                last = last + (white - last) * k;             // 一阶低通
                float env = 1f - t;
                data[i] = last * env * amp;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
