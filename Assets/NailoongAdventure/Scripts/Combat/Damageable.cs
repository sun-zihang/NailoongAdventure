using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    public enum Faction { Player, Enemy, Neutral }

    /// <summary>
    /// 通用生命体：血量、受伤闪白、无敌帧、击退、死亡广播。玩家与敌人共用。
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        [Header("生命")]
        public float maxHealth = 100f;
        public float health = 100f;
        public Faction faction = Faction.Enemy;

        [Header("受击")]
        public float invulnerableTime = 0.35f;
        public float knockbackResist = 0f;      // 0=完全击退 1=完全免疫
        public bool showDamageText = true;

        [Header("闪白")]
        public float flashDuration = 0.18f;

        public float Health01 => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => invulnTimer > 0f;

        public event Action<Damageable, float, Vector3> Damaged;
        public event Action<Damageable> Died;

        float invulnTimer;
        float flashTimer;
        readonly List<Material> mats = new List<Material>();
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            CollectMaterials();
        }

        void CollectMaterials()
        {
            mats.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var m = r.material;            // 实例化副本，避免污染共享材质
                if (m != null && !mats.Contains(m)) mats.Add(m);
            }
        }

        void Update()
        {
            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float v = Mathf.Clamp01(flashTimer / flashDuration);
                foreach (var m in mats)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_HitFlash")) m.SetFloat("_HitFlash", v);
                    else if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.white * v * 0.6f);
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint, GameObject source = null, bool critical = false, float knockback = 3f)
        {
            if (IsDead || amount <= 0f) return;
            if (invulnTimer > 0f) return;

            health -= amount;
            invulnTimer = invulnerableTime;
            flashTimer = flashDuration;

            Damaged?.Invoke(this, amount, hitPoint);
            GameEvents.Damage(gameObject, amount, hitPoint);

            if (showDamageText && VFXManager.Instance != null)
                VFXManager.Instance.DamageText(hitPoint + Vector3.up * 0.6f, Mathf.RoundToInt(amount).ToString(),
                    critical ? new Color(1f, 0.85f, 0.25f) : Color.white, critical);

            if (rb != null && knockback > 0f)
            {
                Vector3 dir = (transform.position - (source != null ? source.transform.position : hitPoint)).normalized;
                dir.y = 0f;
                rb.AddForce(dir * knockback * (1f - knockbackResist) * (critical ? 1.6f : 1f), ForceMode.Impulse);
            }

            if (health <= 0f) Die(source);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            health = Mathf.Min(maxHealth, health + amount);
            if (faction == Faction.Player) GameEvents.PlayerHealth(Health01);
        }

        public void SetInvulnerable(float time) => invulnTimer = Mathf.Max(invulnTimer, time);

        public void Die(GameObject source = null)
        {
            if (IsDead) return;
            IsDead = true;
            health = 0f;
            Died?.Invoke(this);
            GameEvents.Killed(gameObject);

            if (faction == Faction.Player) GameEvents.PlayerDead();
        }

        public void ResetHealth()
        {
            IsDead = false;
            health = maxHealth;
            flashTimer = 0f;
            invulnTimer = 0f;
        }
    }
}
