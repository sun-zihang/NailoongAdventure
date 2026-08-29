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

            AudioClip clip = null;
#if UNITY_WEBGL
            // WebGL 上浏览器 decodeAudioData 对 Resources 音频很不稳定
            // （报 Unable to decode audio data / Unknown data format）。
            // 改为全部运行时 AudioClip.Create 合成，绕过浏览器解码。
            clip = SynthClip(name) ?? SynthMusic(name) ?? SynthTone(44100, 0.2f, 440, 440, WaveType.Sine);
#else
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) clip = SynthClip(name) ?? SynthMusic(name) ?? SynthTone(44100, 0.2f, 440, 440, WaveType.Sine);
#endif
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

        // ---------- 运行时合成（WebGL 主力，桌面兜底） ----------
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
                case "sfx_boss_roar": clip = SynthRoar(rate, 0.55f); break;
                case "sfx_ui":        clip = SynthTone(rate, 0.10f, 880, 1200, WaveType.Sine); break;
                case "sfx_levelclear":clip = SynthTone(rate, 0.9f, 523, 1046, WaveType.Triangle); break;
            }
            if (clip != null) synthCache[name] = clip;
            return clip;
        }

        AudioClip SynthMusic(string name)
        {
            if (synthCache.TryGetValue(name, out var c)) return c;
            int rate = 44100;
            AudioClip clip = null;
            switch (name)
            {
                case "bgm_menu":     clip = SynthMusicLoop(rate, 104f, 8f, new[] { 60, 64, 67, 72 }, new[] { 72, 76, 79, 84 }, 0.28f, 0.22f); break;
                case "bgm_level1":   clip = SynthMusicLoop(rate, 120f, 8f, new[] { 60, 64, 67, 65 }, new[] { 79, 81, 84, 86 }, 0.26f, 0.24f); break;
                case "bgm_level2":   clip = SynthMusicLoop(rate, 108f, 8f, new[] { 57, 60, 64, 62 }, new[] { 76, 79, 81, 84 }, 0.24f, 0.20f); break;
                case "bgm_level3":   clip = SynthMusicLoop(rate, 126f, 8f, new[] { 56, 60, 63, 61 }, new[] { 75, 78, 80, 83 }, 0.30f, 0.26f); break;
                case "bgm_boss":     clip = SynthMusicLoop(rate, 138f, 8f, new[] { 51, 54, 56, 49 }, new[] { 70, 73, 75, 78 }, 0.32f, 0.28f); break;
                case "bgm_victory":  clip = SynthMusicLoop(rate, 132f, 8f, new[] { 60, 64, 67, 72 }, new[] { 84, 86, 88, 91 }, 0.30f, 0.24f); break;
            }
            if (clip != null) synthCache[name] = clip;
            return clip;
        }

        /// <summary>轻量 BGM 循环：和弦铺底 + 简单旋律 + 节奏。</summary>
        AudioClip SynthMusicLoop(int rate, float bpm, float bars, int[] chords, int[] melody, float chordAmp, float melodyAmp)
        {
            float spb = 60f / bpm;
            float dur = bars * spb;
            int len = Mathf.CeilToInt(dur * rate);
            var data = new float[len];
            var rand = new System.Random(name.GetHashCode());

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float beat = t * bars * 4f;                 // 当前拍数
                int step = Mathf.FloorToInt(beat) % chords.Length;
                float subBeat = beat - Mathf.Floor(beat);

                // 和弦铺底（三角波）
                float fChord = MidiToFreq(chords[step]);
                float chordEnv = 0.06f + 0.94f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.35f);
                data[i] += OscTriangle(fChord * i / rate) * chordEnv * chordAmp * 0.35f;
                data[i] += OscTriangle(fChord * 0.5f * i / rate) * chordEnv * chordAmp * 0.25f; // 低八度

                // 旋律（正弦 + 轻三角）
                if (subBeat < 0.75f)
                {
                    float fMel = MidiToFreq(melody[step]);
                    float melEnv = Mathf.SmoothStep(0f, 0.08f, subBeat) * (1f - Mathf.SmoothStep(0.55f, 0.75f, subBeat));
                    data[i] += (Mathf.Sin(2f * Mathf.PI * fMel * i / rate) * 0.7f + OscTriangle(fMel * i / rate) * 0.3f) * melEnv * melodyAmp;
                }

                // 简单鼓点：每拍 kick，反拍 hat
                if (subBeat < 0.12f)
                {
                    float kickPhase = subBeat / 0.12f;
                    float kickFreq = Mathf.Lerp(120f, 55f, kickPhase);
                    data[i] += Mathf.Sin(2f * Mathf.PI * kickFreq * subBeat) * (1f - kickPhase) * 0.28f;
                }
                if (subBeat > 0.48f && subBeat < 0.60f)
                {
                    float hat = (float)(rand.NextDouble() * 2.0 - 1.0);
                    data[i] += hat * 0.10f;
                }

                // 限制幅值
                data[i] = Mathf.Clamp(data[i], -0.95f, 0.95f);
            }

            var clip = AudioClip.Create("music_" + bpm + "_" + bars, len, 1, rate, true); // loop
            clip.SetData(data, 0);
            return clip;
        }

        static float MidiToFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        static float OscTriangle(float phase)
        {
            float p = phase % 1f;
            return 2f * Mathf.Abs(2f * p - 1f) - 1f;
        }

        AudioClip SynthRoar(int rate, float dur)
        {
            int len = Mathf.CeilToInt(dur * rate);
            var clip = AudioClip.Create("synth_roar", len, 1, rate, false);
            var data = new float[len];
            var rand = new System.Random(777);
            double phase = 0;
            float last = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float f = Mathf.Lerp(150f, 55f, Mathf.SmoothStep(0f, 1f, t));
                f *= 1f + Mathf.Sin(t * 44f) * 0.07f;
                phase += f / rate;

                float saw = (float)((phase % 1.0) * 2.0 - 1.0);
                float sqr = (phase % 1.0) < 0.5 ? 1f : -1f;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                last = last + (white - last) * 0.18f;

                float env = Mathf.SmoothStep(0f, 0.12f, t) * (1f - Mathf.SmoothStep(0.65f, 1f, t));
                data[i] = (saw * 0.5f + sqr * 0.2f + last * 0.3f) * env * 0.75f;
            }
            clip.SetData(data, 0);
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
