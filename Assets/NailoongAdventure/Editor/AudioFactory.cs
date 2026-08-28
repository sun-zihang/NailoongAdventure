using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 程序化音频：用代码合成音效与 BGM，直接写出 16bit PCM WAV。
    /// 全部音效零外部依赖，风格贴合"奶龙"的 Q 弹卡通感。
    /// </summary>
    public static class AudioFactory
    {
        const int RATE = 44100;

        public static void GenerateAll(string folder)
        {
            Directory.CreateDirectory(folder);

            // ---------- 音效 ----------
            Write(folder, "sfx_jump", Tone(0.26f, Ease.Up, Wave.Sine, 300, 780, 0.55f, 0.02f, 0.12f)
                .Add(Noise(0.1f, 0.12f, 1800, true), 0f));

            Write(folder, "sfx_land", Tone(0.22f, Ease.Down, Wave.Sine, 180, 60, 0.5f, 0.005f, 0.16f)
                .Add(Noise(0.16f, 0.3f, 700), 0f));

            Write(folder, "sfx_swing", Noise(0.2f, 0.32f, 3800, true));

            Write(folder, "sfx_hit", Tone(0.16f, Ease.Down, Wave.Square, 260, 90, 0.42f, 0.004f, 0.1f)
                .Add(Noise(0.12f, 0.32f, 2600), 0f));

            Write(folder, "sfx_pickup", Arp(0.34f, new[] { 72, 76, 79, 84 }, 0.055f, Wave.Triangle, 0.32f));

            Write(folder, "sfx_breath", Noise(1.3f, 0.5f, 1500, true, 0.08f)
                .Add(Tone(1.3f, Ease.Flat, Wave.Saw, 140, 90, 0.12f, 0.15f, 0.4f), 0f));

            Write(folder, "sfx_slam", Tone(0.6f, Ease.Down, Wave.Saw, 220, 35, 0.6f, 0.008f, 0.4f)
                .Add(Noise(0.45f, 0.5f, 900), 0f)
                .Add(Tone(0.35f, Ease.Up, Wave.Sine, 90, 200, 0.25f, 0.0f, 0.3f), 0f));

            Write(folder, "sfx_dash", Noise(0.42f, 0.4f, 2400, true, 0.06f)
                .Add(Tone(0.3f, Ease.Up, Wave.Sine, 420, 900, 0.18f, 0.02f, 0.2f), 0f));

            Write(folder, "sfx_hurt", Tone(0.4f, Ease.Down, Wave.Square, 520, 170, 0.42f, 0.006f, 0.28f)
                .Add(Noise(0.2f, 0.25f, 1200), 0f));

            Write(folder, "sfx_enemy_die", Tone(0.45f, Ease.Down, Wave.Saw, 420, 70, 0.45f, 0.005f, 0.32f)
                .Add(Noise(0.35f, 0.3f, 1500), 0f));

            Write(folder, "sfx_ui", Tone(0.09f, Ease.Up, Wave.Sine, 880, 1320, 0.3f, 0.004f, 0.06f));

            Write(folder, "sfx_levelclear", Arp(1.0f, new[] { 72, 76, 79, 84, 88 }, 0.1f, Wave.Triangle, 0.3f)
                .Add(Arp(1.0f, new[] { 60, 64, 67 }, 0.1f, Wave.Sine, 0.18f), 0f));

            Write(folder, "sfx_boss_roar", Roar(1.6f));

            // ---------- BGM ----------
            Write(folder, "bgm_menu", Song.Menu());
            Write(folder, "bgm_level1", Song.Beach());
            Write(folder, "bgm_level2", Song.Forest());
            Write(folder, "bgm_level3", Song.Volcano());
            Write(folder, "bgm_boss", Song.Boss());
            Write(folder, "bgm_victory", Song.Victory());

            AssetDatabase.Refresh();
        }

        // ================= WAV 输出 =================
        static void Write(string folder, string name, Clip clip)
        {
            string path = Path.Combine(folder, name + ".wav");
            using var fs = new FileStream(path, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            int samples = clip.Samples.Length;
            int dataSize = samples * 2;

            void Ascii(string s) => bw.Write(System.Text.Encoding.ASCII.GetBytes(s));

            Ascii("RIFF");
            bw.Write(36 + dataSize);
            Ascii("WAVE");
            Ascii("fmt ");
            bw.Write(16);
            bw.Write((short)1);            // PCM
            bw.Write((short)1);            // 单声道
            bw.Write(RATE);
            bw.Write(RATE * 2);            // byte rate
            bw.Write((short)2);            // block align
            bw.Write((short)16);           // bits
            Ascii("data");
            bw.Write(dataSize);

            for (int i = 0; i < samples; i++)
            {
                float v = Mathf.Clamp(clip.Samples[i], -1f, 1f);
                bw.Write((short)(v * 32767f));
            }
        }

        // ================= 合成基元 =================
        class Clip
        {
            public float[] Samples;
            public Clip(int length) { Samples = new float[length]; }
            public Clip(float seconds) { Samples = new float[Mathf.CeilToInt(seconds * RATE)]; }

            public Clip Add(Clip other, float offsetSeconds)
            {
                int offset = Mathf.CeilToInt(offsetSeconds * RATE);
                for (int i = 0; i < other.Samples.Length; i++)
                {
                    int idx = i + offset;
                    if (idx >= 0 && idx < Samples.Length) Samples[idx] += other.Samples[i];
                }
                return this;
            }

            public Clip Normalize(float peak = 0.92f)
            {
                float max = 0f;
                foreach (var s in Samples) max = Mathf.Max(max, Mathf.Abs(s));
                if (max > 0.0001f)
                {
                    float k = peak / max;
                    for (int i = 0; i < Samples.Length; i++) Samples[i] *= k;
                }
                return this;
            }

            public Clip FadeEdges(float inTime, float outTime)
            {
                int fi = Mathf.CeilToInt(inTime * RATE);
                int fo = Mathf.CeilToInt(outTime * RATE);
                for (int i = 0; i < fi && i < Samples.Length; i++) Samples[i] *= (float)i / fi;
                for (int i = 0; i < fo && i < Samples.Length; i++) Samples[Samples.Length - 1 - i] *= (float)i / fo;
                return this;
            }

            /// <summary>循环无缝：把尾部与头部做极短交叉淡化。</summary>
            public Clip Loopify(float xfade = 0.05f)
            {
                int n = Mathf.CeilToInt(xfade * RATE);
                if (n * 2 >= Samples.Length) return this;
                var copy = (float[])Samples.Clone();
                for (int i = 0; i < n; i++)
                {
                    float k = (float)i / n;
                    Samples[Samples.Length - n + i] = copy[Samples.Length - n + i] * (1f - k) + copy[i] * k;
                }
                return this;
            }
        }

        enum Wave { Sine, Square, Saw, Triangle }

        enum Ease { Flat, Up, Down }

        static float Osc(Wave wave, double phase)
        {
            double p = phase % 1.0;
            return wave switch
            {
                Wave.Square => p < 0.5 ? 1f : -1f,
                Wave.Saw => (float)(p * 2.0 - 1.0),
                Wave.Triangle => (float)(4.0 * Math.Abs(p - 0.5) - 1.0),
                _ => (float)Math.Sin(phase * Math.PI * 2.0)
            };
        }

        /// <summary>单音：可扫频，带 ADSR 简化包络。</summary>
        static Clip Tone(float seconds, Ease ease, Wave wave, float f0, float f1, float amp, float attack, float release)
        {
            var clip = new Clip(seconds);
            int len = clip.Samples.Length;
            int a = Mathf.CeilToInt(attack * RATE);
            int r = Mathf.CeilToInt(release * RATE);
            double phase = 0;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float f = ease switch
                {
                    Ease.Up => Mathf.Lerp(f0, f1, t),
                    Ease.Down => Mathf.Lerp(f0, f1, t * t),
                    _ => f0
                };
                phase += f / RATE;

                float env = 1f;
                if (i < a) env = (float)i / Mathf.Max(a, 1);
                else if (i > len - r) env = Mathf.Clamp01((float)(len - i) / Mathf.Max(r, 1));

                clip.Samples[i] = Osc(wave, phase) * amp * env;
            }
            return clip;
        }

        /// <summary>噪声：一阶低通，可选扫频（用于风声、爆炸、冲刺）。</summary>
        static Clip Noise(float seconds, float amp, float cutoff, bool sweep = false, float attack = 0.005f)
        {
            var clip = new Clip(seconds);
            int len = clip.Samples.Length;
            int a = Mathf.CeilToInt(attack * RATE);
            var rand = new System.Random(20260828);
            float last = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                float k = sweep ? Mathf.Lerp(0.9f, 0.05f, t) : 0.35f;
                last = last + (white - last) * k;
                float env = 1f - t * t;
                if (i < a) env *= (float)i / Mathf.Max(a, 1);
                clip.Samples[i] = last * amp * env;
            }
            cutOffGain(clip, cutoff);
            return clip;
        }

        static void cutOffGain(Clip clip, float cutoff)
        {
            // 简单的音色平衡：高频噪声整体增益补偿
            float g = Mathf.Clamp(cutoff / 2000f, 0.5f, 2.2f);
            for (int i = 0; i < clip.Samples.Length; i++) clip.Samples[i] *= g;
        }

        /// <summary>上行琶音（拾取、通关）。</summary>
        static Clip Arp(float seconds, int[] midi, float step, Wave wave, float amp)
        {
            var clip = new Clip(seconds);
            for (int i = 0; i < midi.Length; i++)
            {
                float f = MidiToFreq(midi[i]);
                var note = Tone(Mathf.Min(step * 6f, seconds - i * step), Ease.Flat, wave, f, f, amp, 0.006f, step * 5f);
                clip.Add(note, i * step);
            }
            return clip.Normalize(0.9f).FadeEdges(0.004f, 0.12f);
        }

        /// <summary>Boss 咆哮：低频锯齿 + 颤音 + 噪声层。</summary>
        static Clip Roar(float seconds)
        {
            var clip = new Clip(seconds);
            int len = clip.Samples.Length;
            double phase = 0;
            var rand = new System.Random(777);
            float lastNoise = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float f = Mathf.Lerp(150f, 58f, Mathf.SmoothStep(0f, 1f, t));
                f *= 1f + Mathf.Sin(t * 46f) * 0.06f;      // 颤音
                phase += f / RATE;

                float growl = Osc(Wave.Saw, phase) * 0.55f + Osc(Wave.Square, phase * 0.5f) * 0.18f;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                lastNoise = lastNoise + (white - lastNoise) * 0.18f;

                float env = Mathf.SmoothStep(0f, 0.12f, t) * (1f - Mathf.SmoothStep(0.65f, 1f, t));
                clip.Samples[i] = (growl + lastNoise * 0.35f) * env * 0.75f;
            }
            return clip.Normalize(0.95f).FadeEdges(0.02f, 0.25f);
        }

        static float MidiToFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        // ================= 音乐 =================
        /// <summary>简易音序器：旋律 + 贝斯 + 打击，输出可循环片段。</summary>
        static class Song
        {
            class Step { public int Midi = -1; public float Beat = 0.5f; public Wave Wave = Wave.Triangle; public float Amp = 0.3f; }

            static Clip Compose(float bpm, float beats, Step[] melody, Step[] bass, int[] drums, Wave leadWave)
            {
                float spb = 60f / bpm;
                var track = new Clip(beats * spb + 1.2f);

                if (melody != null)
                {
                    float cursor = 0f;
                    foreach (var s in melody)
                    {
                        if (s.Midi < 0) { cursor += s.Beat * spb; continue; }
                        float dur = s.Beat * spb * 1.05f;
                        var note = Tone(dur, Ease.Flat, s.Wave != Wave.Sine && s.Wave != Wave.Saw ? s.Wave : leadWave,
                            MidiToFreq(s.Midi), MidiToFreq(s.Midi), s.Amp, 0.012f, dur * 0.6f);
                        // 加一层高八度点缀，让音色更"奶"
                        var shine = Tone(dur * 0.7f, Ease.Flat, Wave.Sine, MidiToFreq(s.Midi + 12), MidiToFreq(s.Midi + 12), s.Amp * 0.28f, 0.01f, dur * 0.5f);
                        track.Add(note, cursor);
                        track.Add(shine, cursor);
                        cursor += s.Beat * spb;
                    }
                }

                if (bass != null)
                {
                    float cursor = 0f;
                    foreach (var s in bass)
                    {
                        if (s.Midi < 0) { cursor += s.Beat * spb; continue; }
                        float dur = s.Beat * spb * 0.95f;
                        var note = Tone(dur, Ease.Flat, Wave.Sine, MidiToFreq(s.Midi), MidiToFreq(s.Midi), 0.4f, 0.015f, dur * 0.5f);
                        track.Add(note, cursor);
                        cursor += s.Beat * spb;
                    }
                }

                if (drums != null)
                {
                    for (int i = 0; i < drums.Length; i++)
                    {
                        if (drums[i] <= 0) continue;
                        float t = i * spb * 0.5f;
                        if (drums[i] == 1) track.Add(Tone(0.12f, Ease.Down, Wave.Sine, 150, 60, 0.32f, 0.004f, 0.1f), t);           // kick
                        else track.Add(Noise(0.07f, 0.14f, 6000, true), t);                                                          // hat
                    }
                }

                return track.Normalize(0.82f).Loopify(0.06f);
            }

            static Step N(int midi, float beat = 0.5f, Wave w = Wave.Triangle, float amp = 0.26f)
                => new Step { Midi = midi, Beat = beat, Wave = w, Amp = amp };
            static Step R(float beat = 0.5f) => new Step { Midi = -1, Beat = beat };

            public static Clip Menu()
            {
                var melody = new[]
                {
                    N(72,0.5f), N(76,0.5f), N(79,0.5f), N(76,0.5f),
                    N(74,0.5f), N(77,0.5f), N(81,0.5f), N(77,0.5f),
                    N(71,0.5f), N(74,0.5f), N(79,0.5f), N(78,0.5f),
                    N(76,1f), R(0.5f), N(72,0.5f)
                };
                var bass = new[]
                {
                    N(48,1f), N(50,1f), N(45,1f), N(47,1f),
                    N(48,1f), N(50,1f), N(43,1f), N(48,1f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 8 == 0 ? 1 : (i % 4 == 2 ? 2 : 0);
                return Compose(104, 16, melody, bass, drums, Wave.Triangle);
            }

            public static Clip Beach()
            {
                var melody = new[]
                {
                    N(72,0.5f), N(72,0.25f), N(76,0.25f), N(79,0.5f), N(77,0.5f),
                    N(76,0.5f), N(74,0.5f), N(72,0.5f), R(0.5f),
                    N(79,0.5f), N(81,0.5f), N(83,1f),
                    N(81,0.5f), N(79,0.5f), N(76,1f), R(0.5f)
                };
                var bass = new[]
                {
                    N(48,1f), N(55,1f), N(50,1f), N(57,1f),
                    N(48,1f), N(55,1f), N(53,1f), N(55,1f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 8 == 0 ? 1 : (i % 2 == 1 ? 2 : 0);
                return Compose(120, 16, melody, bass, drums, Wave.Sine);
            }

            public static Clip Forest()
            {
                var melody = new[]
                {
                    N(69,0.5f), N(72,0.5f), N(76,0.5f), N(74,0.5f),
                    N(72,0.5f), N(69,0.5f), N(67,1f),
                    N(64,0.5f), N(67,0.5f), N(72,0.5f), N(71,0.5f),
                    N(69,1f), R(1f)
                };
                var bass = new[]
                {
                    N(45,1f), N(52,1f), N(47,1f), N(50,1f),
                    N(45,1f), N(52,1f), N(48,1f), N(45,1f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 8 == 0 ? 1 : (i % 4 == 3 ? 2 : 0);
                return Compose(108, 16, melody, bass, drums, Wave.Triangle);
            }

            public static Clip Volcano()
            {
                var melody = new[]
                {
                    N(68,0.5f), N(68,0.25f), N(71,0.25f), N(68,0.5f), R(0.5f),
                    N(75,0.5f), N(74,0.5f), N(71,1f),
                    N(68,0.5f), N(66,0.5f), N(68,0.5f), N(71,0.5f),
                    N(68,1f), R(1f)
                };
                var bass = new[]
                {
                    N(44,0.5f), N(44,0.5f), N(51,1f), N(44,0.5f), N(44,0.5f), N(49,1f),
                    N(44,0.5f), N(44,0.5f), N(51,1f), N(46,1f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 4 == 0 ? 1 : (i % 2 == 0 ? 2 : 0);
                return Compose(126, 16, melody, bass, drums, Wave.Saw);
            }

            public static Clip Boss()
            {
                var melody = new[]
                {
                    N(63,0.5f), N(63,0.25f), N(66,0.25f), N(63,0.5f), N(61,0.5f),
                    N(63,0.5f), N(68,0.5f), N(66,1f),
                    N(63,0.5f), N(61,0.5f), N(58,0.5f), N(61,0.5f),
                    N(63,1f), R(1f)
                };
                var bass = new[]
                {
                    N(39,0.5f), N(39,0.5f), N(39,0.5f), N(39,0.5f),
                    N(38,0.5f), N(38,0.5f), N(38,0.5f), N(38,0.5f),
                    N(39,0.5f), N(39,0.5f), N(39,0.5f), N(39,0.5f),
                    N(34,1f), N(34,1f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 2 == 0 ? 1 : 2;
                return Compose(138, 16, melody, bass, drums, Wave.Saw);
            }

            public static Clip Victory()
            {
                var melody = new[]
                {
                    N(72,0.5f), N(76,0.5f), N(79,0.5f), N(84,0.5f),
                    N(83,0.5f), N(79,0.5f), N(84,1f),
                    N(86,0.5f), N(84,0.5f), N(79,0.5f), N(76,0.5f),
                    N(72,1.5f)
                };
                var bass = new[]
                {
                    N(48,1f), N(55,1f), N(53,1f), N(55,1f),
                    N(48,1f), N(60,1f), N(48,1.5f)
                };
                var drums = new int[32];
                for (int i = 0; i < 32; i++) drums[i] = i % 4 == 0 ? 1 : (i % 2 == 1 ? 2 : 0);
                return Compose(132, 16, melody, bass, drums, Wave.Triangle);
            }
        }
    }
}
