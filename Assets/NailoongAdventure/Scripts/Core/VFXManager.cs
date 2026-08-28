using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 视听反馈中枢：粒子特效池 + 命中顿帧 + 屏幕闪白 + 伤害飘字 + 相机抖动。
    /// 粒子预制体从 Resources/VFX 加载，缺失时运行时程序化生成等价效果。
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        readonly Dictionary<string, ParticleSystem> prefabs = new Dictionary<string, ParticleSystem>();
        readonly Dictionary<string, Stack<ParticleSystem>> pools = new Dictionary<string, Stack<ParticleSystem>>();
        ParticleSystem fallback;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            fallback = CreateBurst();
            fallback.gameObject.SetActive(false);
        }

        public ParticleSystem Play(string key, Vector3 position, Quaternion rotation, float scale = 1f, Color? tint = null)
        {
            var ps = Spawn(key);
            if (ps == null) return null;
            ps.transform.SetPositionAndRotation(position, rotation);
            ps.transform.localScale = Vector3.one * scale;
            ps.gameObject.SetActive(true);
            if (tint.HasValue) { var main = ps.main; main.startColor = tint.Value; }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
            StartCoroutine(Recycle(key, ps));
            return ps;
        }

        public ParticleSystem Play(string key, Vector3 position) => Play(key, position, Quaternion.identity);

        IEnumerator Recycle(string key, ParticleSystem ps)
        {
            float total = ps.main.duration + ps.main.startLifetime.constantMax + 0.25f;
            yield return new WaitForSeconds(total);
            ps.gameObject.SetActive(false);
            if (!pools.TryGetValue(key, out var stack)) { stack = new Stack<ParticleSystem>(); pools[key] = stack; }
            if (stack.Count < 12) stack.Push(ps);
            else Destroy(ps.gameObject);
        }

        ParticleSystem Spawn(string key)
        {
            if (!prefabs.ContainsKey(key)) prefabs[key] = Resources.Load<ParticleSystem>("VFX/" + key);
            var prefab = prefabs[key];

            if (pools.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var pooled = stack.Pop();
                if (pooled != null) return pooled;
            }
            if (prefab != null)
            {
                var inst = Instantiate(prefab, transform);
                inst.gameObject.SetActive(false);
                return inst;
            }
            var fb = Instantiate(fallback, transform);
            fb.gameObject.SetActive(false);
            return fb;
        }

        ParticleSystem CreateBurst()
        {
            var go = new GameObject("FallbackBurst");
            go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.6f;
            main.startSpeed = 6f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null) rend.material = new Material(shader);
            return ps;
        }

        // ---------- 打击感 ----------
        public void HitStop(float duration = 0.06f) => StartCoroutine(HitStopRoutine(duration));

        IEnumerator HitStopRoutine(float duration)
        {
            float prev = Time.timeScale;
            Time.timeScale = Mathf.Min(prev, 0.05f);
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prev <= 0f ? 1f : prev;
        }

        public void DamageText(Vector3 worldPos, string text, Color color, bool critical = false)
        {
            if (UIManager.Instance != null) UIManager.Instance.SpawnFloatingText(worldPos, text, color, critical);
        }

        public void Shake(float amount, float duration = 0.25f)
        {
            if (CameraRig.Instance != null) CameraRig.Instance.AddTrauma(amount, duration);
        }

        public void ScreenFlash(Color color, float duration = 0.2f)
        {
            if (UIManager.Instance != null) UIManager.Instance.FlashScreen(color, duration);
        }
    }
}
