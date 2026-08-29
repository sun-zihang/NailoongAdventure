using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 奶龙战斗系统：三连击普攻 + 泰山压顶 + 咕噜冲撞 + 龙耀吐息 + 奶龙变色。
    /// 核心资源为「火力值」：造成伤害/受到伤害都会累积，消耗火力值时按已损失生命回复（火冒三丈被动）。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [System.Serializable]
        public class SkillSetting
        {
            public string id = "";
            public string displayName = "";
            public string hint = "";
            public KeyCode key = KeyCode.None;
            public float damage = 10f;
            public float rageCost = 0f;
            public float cooldown = 0.5f;
            public float range = 2.2f;
            public float angle = 100f;
            public float windup = 0.12f;
            public float duration = 0.35f;
            public float knockback = 4f;
            public bool unlocked = true;
        }

        [Header("普攻")]
        public SkillSetting claw = new SkillSetting
        { id = "claw", displayName = "奶龙拍击", key = KeyCode.Mouse0, damage = 12f, cooldown = 0.32f, range = 2.3f, angle = 110f, windup = 0.1f, duration = 0.3f, knockback = 3.5f };

        [Header("技能")]
        public SkillSetting slam = new SkillSetting
        { id = "slam", displayName = "泰山压顶", hint = "跃起砸地，范围击飞", key = KeyCode.Q, damage = 38f, rageCost = 25f, cooldown = 4f, range = 4.2f, angle = 360f, windup = 0.22f, duration = 0.85f, knockback = 9f };

        public SkillSetting breath = new SkillSetting
        { id = "breath", displayName = "龙耀吐息", hint = "持续喷射星火", key = KeyCode.F, damage = 9f, rageCost = 45f, cooldown = 6f, range = 6.5f, angle = 32f, windup = 0.18f, duration = 1.4f, knockback = 1.2f };

        public SkillSetting colorShift = new SkillSetting
        { id = "shift", displayName = "奶龙变色", hint = "Q弹状态：减伤50%、加速", key = KeyCode.R, damage = 0f, rageCost = 30f, cooldown = 12f, range = 0f, angle = 0f, windup = 0.05f, duration = 6f, knockback = 0f };

        public SkillSetting roll = new SkillSetting
        { id = "roll", displayName = "咕噜冲撞", hint = "翻滚穿透，冲刺无敌", key = KeyCode.LeftShift, damage = 16f, rageCost = 0f, cooldown = 0.75f, range = 1.4f, angle = 360f, windup = 0.02f, duration = 0.38f, knockback = 6f };

        [Header("火力值")]
        public float maxRage = 100f;
        public float rage = 0f;
        public float ragePerHit = 2.2f;
        public float ragePerTaken = 5f;
        public float rageDecayPerSecond = 1.5f;

        [Header("属性")]
        public float criticalChance = 0.15f;
        public float criticalMultiplier = 1.8f;
        public float healOnRageSpend = 0.25f;     // 消耗火力时回复"已损失生命"的比例
        public LayerMask enemyMask = ~0;

        [Header("特效挂点")]
        public Transform mouthPoint;
        public Transform slamPoint;

        public float Rage01 => Mathf.Clamp01(rage / maxRage);
        public bool IsShifting => shiftTimer > 0f;
        public bool IsBreathing => breathTimer > 0f;
        public Dictionary<string, float> Cooldowns { get; } = new Dictionary<string, float>();

        PlayerController player;
        DragonAnimator anim;
        Damageable dmg;
        SkillSetting[] all;

        float shiftTimer, breathTimer, breathTick, comboWindow;
        int comboIndex;
        bool comboQueued;
        readonly HashSet<Damageable> hitOnce = new HashSet<Damageable>();

        // 复用缓冲：避免每帧 / 每次攻击产生 GC 分配（WebGL 移动端尤其敏感）
        const int MAX_HITS = 64;
        readonly Collider[] hitBuffer = new Collider[MAX_HITS];
        readonly List<string> cooldownKeys = new List<string>();

        void Awake()
        {
            player = GetComponent<PlayerController>();
            anim = GetComponent<DragonAnimator>();
            dmg = GetComponent<Damageable>();
            all = new[] { claw, slam, breath, colorShift, roll };
        }

        void Start()
        {
            if (mouthPoint == null && anim != null) mouthPoint = anim.mouthPoint;
            if (slamPoint == null) slamPoint = transform;

            if (dmg != null)
            {
                dmg.Damaged += OnPlayerDamaged;
                GameEvents.PlayerHealth(dmg.Health01);
            }
            GameEvents.Rage(Rage01);

            if (player != null) player.OnDashStart += () => StartCoroutine(RollDamage());
        }

        void Update()
        {
            TickTimers();
            ReadInput();
        }

        void TickTimers()
        {
            cooldownKeys.Clear();
            cooldownKeys.AddRange(Cooldowns.Keys);
            foreach (var k in cooldownKeys)
            {
                float v = Cooldowns[k] - Time.deltaTime;
                if (v <= 0f) Cooldowns.Remove(k);
                else Cooldowns[k] = v;
            }

            if (comboWindow > 0f)
            {
                comboWindow -= Time.deltaTime;
                if (comboWindow <= 0f) comboIndex = 0;
            }

            if (shiftTimer > 0f)
            {
                shiftTimer -= Time.deltaTime;
                if (shiftTimer <= 0f) GameEvents.Toast("变色状态结束");
            }

            if (rage > 0f && !IsBreathing)
                rage = Mathf.Max(0f, rage - rageDecayPerSecond * Time.deltaTime);

            if (breathTimer > 0f) UpdateBreath();
        }

        void ReadInput()
        {
            if (player == null || !player.CanAct) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            if (Input.GetKeyDown(KeyCode.Mouse0)) TryClaw();
            if (Input.GetKeyDown(slam.key)) TrySkill(slam, anim != null ? DragonAnimator.State.Slam : DragonAnimator.State.Claw);
            if (Input.GetKeyDown(breath.key)) TryBreath();
            if (Input.GetKeyDown(colorShift.key)) TryColorShift();
        }

        // ---------- 普攻三连 ----------
        void TryClaw()
        {
            if (Cooldowns.ContainsKey("claw") || player == null) return;
            if (comboWindow > 0f) comboQueued = true;

            comboIndex = comboIndex % 3;
            DragonAnimator.State st = comboIndex == 2 ? DragonAnimator.State.Tail : DragonAnimator.State.Claw;
            float dur = claw.duration;
            Cooldowns["claw"] = claw.cooldown;
            comboWindow = claw.cooldown + 0.35f;

            player.CanAct = false;
            if (anim != null) anim.Play(st, dur);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_swing", 0.7f, 0.9f + comboIndex * 0.12f);

            StartCoroutine(AttackRoutine(claw, st, claw.windup, dur));
            comboIndex++;
        }

        IEnumerator AttackRoutine(SkillSetting s, DragonAnimator.State st, float windup, float total)
        {
            yield return new WaitForSeconds(windup);

            hitOnce.Clear();
            float dmgMul = st == DragonAnimator.State.Tail ? 1.25f : 1f;
            ConeHit(s, s.damage * dmgMul, s.range, s.angle);

            // 连击时前冲一小步，增强手感
            if (player != null) player.AddImpulse(transform.forward * 2.6f);

            yield return new WaitForSeconds(Mathf.Max(0.02f, total - windup - 0.08f));
            if (player != null) player.CanAct = true;

            if (comboQueued && Cooldowns.ContainsKey("claw") == false)
            {
                comboQueued = false;
                TryClaw();
            }
        }

        // ---------- 技能 ----------
        void TrySkill(SkillSetting s, DragonAnimator.State st)
        {
            if (s == null || !s.unlocked || Cooldowns.ContainsKey(s.id)) return;
            if (rage < s.rageCost) { GameEvents.Toast("火力值不足！吃点东西或打几下攒攒！"); return; }

            SpendRage(s.rageCost);
            Cooldowns[s.id] = s.cooldown;
            if (player != null) player.CanAct = false;
            if (anim != null) anim.Play(st, s.duration);

            if (s.id == "slam") StartCoroutine(SlamRoutine(s));
        }

        IEnumerator SlamRoutine(SkillSetting s)
        {
            // 起跳
            if (player != null) player.AddImpulse(Vector3.up * 9.5f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_jump", 0.9f, 0.8f);
            yield return new WaitForSeconds(0.32f);

            // 砸落
            if (player != null) player.AddImpulse(Vector3.down * 22f);
            yield return new WaitForSeconds(0.16f);

            Vector3 center = slamPoint != null ? slamPoint.position : transform.position;
            hitOnce.Clear();
            ConeHit(s, s.damage, s.range, 360f, center);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_slam", 1f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_slam", center, Quaternion.identity, 1.4f);
                VFXManager.Instance.Shake(0.55f, 0.4f);
                VFXManager.Instance.HitStop(0.07f);
            }

            yield return new WaitForSeconds(0.25f);
            if (player != null) player.CanAct = true;
        }

        void TryBreath()
        {
            if (!breath.unlocked || Cooldowns.ContainsKey(breath.id)) return;
            if (rage < breath.rageCost) { GameEvents.Toast("火力值不足！"); return; }

            SpendRage(breath.rageCost);
            Cooldowns[breath.id] = breath.cooldown;
            breathTimer = breath.duration;
            breathTick = 0f;
            hitOnce.Clear();
            if (anim != null) anim.Play(DragonAnimator.State.Breath, breath.duration);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_breath", 0.85f);
        }

        void UpdateBreath()
        {
            breathTimer -= Time.deltaTime;
            breathTick -= Time.deltaTime;

            Transform mp = mouthPoint != null ? mouthPoint : transform;
            if (VFXManager.Instance != null)
                VFXManager.Instance.Play("vfx_breath", mp.position, mp.rotation, 1f);

            if (breathTick <= 0f)
            {
                breathTick = 0.16f;
                ConeHit(breath, breath.damage, breath.range, breath.angle, transform.position, true);
            }
            if (breathTimer <= 0f && AudioManager.Instance != null) AudioManager.Instance.StopAllSfx();
        }

        IEnumerator RollDamage()
        {
            yield return new WaitForSeconds(0.02f);
            hitOnce.Clear();
            float t = 0f;
            while (player != null && player.IsDashing && t < 1f)
            {
                t += Time.deltaTime;
                ConeHit(roll, roll.damage * Time.deltaTime * 6f, roll.range, 360f, transform.position, true, true);
                yield return null;
            }
        }

        void TryColorShift()
        {
            if (!colorShift.unlocked || Cooldowns.ContainsKey(colorShift.id)) return;
            if (rage < colorShift.rageCost) { GameEvents.Toast("火力值不足！"); return; }

            SpendRage(colorShift.rageCost);
            Cooldowns[colorShift.id] = colorShift.cooldown;
            shiftTimer = colorShift.duration;
            if (dmg != null) dmg.SetInvulnerable(0.4f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_pickup", 0.9f, 1.3f);
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_shift", transform.position, Quaternion.identity, 1.2f);
            GameEvents.Toast("奶龙变色！减伤 50%，速度提升！");
        }

        // ---------- 伤害判定 ----------
        void ConeHit(SkillSetting s, float damage, float range, float angle, Vector3? origin = null, bool continuous = false, bool silent = false)
        {
            Vector3 center = origin ?? transform.position;
            // 复用缓冲区，避免每次判定分配新数组（冲撞时每帧、吐息时高频调用）
            int count = Physics.OverlapSphereNonAlloc(center, range, hitBuffer, enemyMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var c = hitBuffer[i];
                if (c == null || c.transform == transform) continue;
                var target = c.GetComponentInParent<Damageable>();
                if (target == null || target.faction != Faction.Enemy) continue;
                if (!continuous && hitOnce.Contains(target)) continue;

                Vector3 toTarget = target.transform.position - center;
                toTarget.y = 0f;
                if (toTarget.magnitude < 0.001f) toTarget = transform.forward;
                if (angle < 360f && Vector3.Angle(transform.forward, toTarget.normalized) > angle * 0.5f) continue;

                if (!continuous) hitOnce.Add(target);

                bool crit = Random.value < criticalChance;
                float finalDamage = damage * (crit ? criticalMultiplier : 1f) * (IsShifting ? 0.9f : 1f);
                Vector3 point = c.ClosestPoint(center);

                target.TakeDamage(finalDamage, point, gameObject, crit, s.knockback);
                AddRage(ragePerHit);

                if (!silent)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_hit", 0.65f, Random.Range(0.9f, 1.15f));
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.Play("vfx_hit", point, Quaternion.LookRotation(toTarget.normalized), crit ? 1.5f : 1f);
                        VFXManager.Instance.HitStop(crit ? 0.085f : 0.05f);
                        VFXManager.Instance.Shake(crit ? 0.32f : 0.16f, 0.22f);
                    }
                }
            }

            // 命中反馈已由 VFXManager 的 HitStop / Shake 承担，
            // 原 KnockbackFeel 空协程只产生分配开销，已移除。
        }

        // ---------- 火力值 ----------
        public void AddRage(float amount)
        {
            rage = Mathf.Clamp(rage + amount, 0f, maxRage);
            GameEvents.Rage(Rage01);
        }

        void SpendRage(float cost)
        {
            rage = Mathf.Max(0f, rage - cost);
            GameEvents.Rage(Rage01);

            if (dmg != null && healOnRageSpend > 0f)
            {
                float lost = dmg.maxHealth - dmg.health;
                if (lost > 0.5f)
                {
                    dmg.Heal(lost * healOnRageSpend);
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.Play("vfx_heal", transform.position + Vector3.up * 1f, Quaternion.identity, 1f);
                }
            }
        }

        void OnPlayerDamaged(Damageable self, float amount, Vector3 point)
        {
            float final = IsShifting ? amount * 0.5f : amount;
            if (IsShifting) self.Heal(amount * 0.5f);

            AddRage(ragePerTaken);
            GameEvents.PlayerHealth(self.Health01);

            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_hurt", 0.85f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_hurt", point, Quaternion.identity, 1f);
                VFXManager.Instance.Shake(0.35f, 0.3f);
                VFXManager.Instance.ScreenFlash(new Color(1f, 0.35f, 0.3f, 0.35f), 0.22f);
            }
        }

        public void UnlockSkill(string id)
        {
            foreach (var s in all)
                if (s != null && s.id == id) s.unlocked = true;
        }

        public SkillSetting[] AllSkills => all;
    }
}
