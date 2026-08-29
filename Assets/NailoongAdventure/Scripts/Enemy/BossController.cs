using System.Collections;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 最终 Boss「暴暴龙」：三阶段战斗。
    /// P1 冲撞 + 爪击；P2 召唤小怪 + 尾扫 + 加速冲撞；P3 狂暴旋风 + 追踪火球。
    /// 每次血量跨阶段都会进入硬直，给玩家反击窗口。
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class BossController : MonoBehaviour
    {
        public static BossController Instance { get; private set; }

        [Header("数值")]
        public float chargeSpeed = 16f;
        public float chargeWindup = 0.9f;
        public float chargeDuration = 1.1f;
        public float clawDamage = 22f;
        public float tailDamage = 26f;
        public float chargeDamage = 28f;
        public float fireballDamage = 16f;

        [Header("距离")]
        public float meleeRange = 3.6f;
        public float chargeRange = 16f;

        [Header("阶段")]
        public float phase2At = 0.66f;
        public float phase3At = 0.33f;
        public string minionPrefab = "Prefabs/Enemy_Pudding";

        [Header("挂点")]
        public Transform mouthPoint;
        public Transform tailPoint;

        public float Health01 => dmg != null ? dmg.Health01 : 0f;
        public int Phase => phase;
        public string BossName => "暴暴龙";

        Damageable dmg;
        Transform player;
        Transform bodyRoot;
        Rigidbody rb;

        enum State { Intro, Idle, Chase, ChargeWindup, Charge, Claw, Tail, Summon, Tornado, Fireball, Stagger, Dead }
        State state = State.Intro;

        int phase = 1;
        float stateTimer, actionCooldown, walkPhase, flapPhase;
        Vector3 chargeDir;
        bool summonedP2;

        void Awake()
        {
            Instance = this;
            dmg = GetComponent<Damageable>();
            rb = GetComponent<Rigidbody>();
            bodyRoot = transform.Find("Body") ?? transform;
            if (mouthPoint == null) mouthPoint = transform.Find("MouthPoint");
        }

        void Start()
        {
            var p = PlayerController.Instance;
            if (p != null) player = p.transform;
            dmg.Damaged += OnDamaged;
            dmg.Died += OnDied;
            stateTimer = 2.2f;

            if (CameraRig.Instance != null) CameraRig.Instance.LockBoss(transform);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("sfx_boss_roar", 1f);
                AudioManager.Instance.PlayMusic("bgm_boss", 1.5f);
            }
            GameEvents.Toast("暴暴龙出现了！");
        }

        void Update()
        {
            if (state == State.Dead) return;
            if (player == null)
            {
                var p = PlayerController.Instance;
                if (p != null) player = p.transform;
            }

            if (actionCooldown > 0f) actionCooldown -= Time.deltaTime;
            CheckPhase();

            switch (state)
            {
                case State.Intro: TickIntro(); break;
                case State.Idle: TickIdle(); break;
                case State.Chase: TickChase(); break;
                case State.ChargeWindup: TickChargeWindup(); break;
                case State.Charge: TickCharge(); break;
                case State.Claw: TickMelee(State.Claw, clawDamage, 0.45f, 0.75f); break;
                case State.Tail: TickMelee(State.Tail, tailDamage, 0.55f, 0.9f); break;
                case State.Summon: TickSummon(); break;
                case State.Tornado: TickTornado(); break;
                case State.Fireball: TickFireball(); break;
                case State.Stagger: TickStagger(); break;
            }

            Animate();
        }

        void CheckPhase()
        {
            float h = Health01;
            if (phase == 1 && h <= phase2At) EnterPhase(2);
            else if (phase == 2 && h <= phase3At) EnterPhase(3);
        }

        void EnterPhase(int p)
        {
            phase = p;
            state = State.Stagger;
            stateTimer = 1.6f;
            summonedP2 = false;
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_boss_roar", 0.9f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_explode", transform.position + Vector3.up * 2f, Quaternion.identity, 2.2f);
                VFXManager.Instance.Shake(0.8f, 0.6f);
            }
            GameEvents.Toast(p == 2 ? "暴暴龙怒了！召唤帮手！" : "暴暴龙进入狂暴状态！");
        }

        // ---------- 状态 ----------
        void TickIntro()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) state = State.Chase;
        }

        void TickIdle()
        {
            stateTimer -= Time.deltaTime;
            if (player != null) Face(player.position, 3f);
            if (stateTimer <= 0f) ChooseAction();
        }

        void TickChase()
        {
            if (player == null) return;
            float d = FlatDist(player.position);
            Face(player.position, 4f);

            if (d <= meleeRange)
            {
                if (actionCooldown <= 0f) { state = Random.value < 0.5f ? State.Claw : State.Tail; stateTimer = 0.75f; return; }
                state = State.Idle; stateTimer = 0.5f; return;
            }

            if (d <= chargeRange && actionCooldown <= 0f && Random.value < 0.02f)
            {
                state = State.ChargeWindup;
                stateTimer = chargeWindup;
                return;
            }

            Vector3 dir = FlatDir(player.position);
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = dir.x * 3.4f;
                vel.z = dir.z * 3.4f;
                rb.linearVelocity = vel;
            }
            else transform.position += dir * 3.4f * Time.deltaTime;

            walkPhase += Time.deltaTime * 6f;
        }

        void ChooseAction()
        {
            if (player == null) return;
            float d = FlatDist(player.position);

            if (phase >= 2 && !summonedP2) { state = State.Summon; stateTimer = 1.2f; return; }
            if (phase >= 3 && d > 6f && Random.value < 0.5f) { state = State.Fireball; stateTimer = 1.5f; fireballLeft = 6; return; }
            if (phase >= 3 && d > 3f && Random.value < 0.35f) { state = State.Tornado; stateTimer = 3.2f; return; }
            if (d > meleeRange * 1.6f) { state = State.ChargeWindup; stateTimer = chargeWindup; return; }
            state = State.Chase;
        }

        void TickChargeWindup()
        {
            stateTimer -= Time.deltaTime;
            if (player != null) Face(player.position, 8f);
            if (stateTimer <= 0f)
            {
                chargeDir = player != null ? FlatDir(player.position) : transform.forward;
                state = State.Charge;
                stateTimer = chargeDuration;
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_dash", 0.9f, 0.7f);
            }
        }

        void TickCharge()
        {
            stateTimer -= Time.deltaTime;
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = chargeDir.x * chargeSpeed;
                vel.z = chargeDir.z * chargeSpeed;
                rb.linearVelocity = vel;
            }
            else transform.position += chargeDir * chargeSpeed * Time.deltaTime;

            // 冲撞伤害判定
            if (player != null && FlatDist(player.position) < 2.6f)
            {
                var target = player.GetComponent<Damageable>();
                if (target != null && !target.IsInvulnerable)
                {
                    target.TakeDamage(chargeDamage, player.position + Vector3.up * 0.8f, gameObject, false, 10f);
                    if (VFXManager.Instance != null) VFXManager.Instance.Shake(0.6f, 0.35f);
                }
            }

            if (VFXManager.Instance != null && Random.value < 0.5f)
                VFXManager.Instance.Play("vfx_dash", transform.position + Vector3.up * 0.5f, Quaternion.identity, 1.6f);

            if (stateTimer <= 0f)
            {
                actionCooldown = phase >= 3 ? 1.2f : 2.2f;
                state = State.Idle;
                stateTimer = 0.4f;
                if (VFXManager.Instance != null) VFXManager.Instance.Shake(0.3f, 0.3f);
            }
        }

        bool meleeDone;
        void TickMelee(State self, float damage, float windup, float total)
        {
            stateTimer -= Time.deltaTime;
            if (player != null) Face(player.position, 6f);

            if (!meleeDone && stateTimer <= total - windup)
            {
                meleeDone = true;
                if (player != null && FlatDist(player.position) < meleeRange + 1.1f)
                {
                    var target = player.GetComponent<Damageable>();
                    if (target != null) target.TakeDamage(damage, player.position + Vector3.up * 0.8f, gameObject, false, 8f);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_swing", 0.9f, 0.75f);
                if (VFXManager.Instance != null)
                {
                    Vector3 center = self == State.Tail && tailPoint != null ? tailPoint.position : transform.position + transform.forward * 2f;
                    VFXManager.Instance.Play("vfx_slam", center, Quaternion.identity, self == State.Tail ? 1.3f : 1f);
                    VFXManager.Instance.Shake(0.28f, 0.25f);
                }
            }

            if (stateTimer <= 0f)
            {
                meleeDone = false;
                actionCooldown = phase >= 3 ? 0.8f : 1.5f;
                state = State.Idle;
                stateTimer = 0.5f;
            }
        }

        void TickSummon()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                summonedP2 = true;
                var prefab = Resources.Load<GameObject>(minionPrefab);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = transform.position + new Vector3(Mathf.Cos(i * 2.1f), 0f, Mathf.Sin(i * 2.1f)) * 5f + Vector3.up * 1.2f;
                    if (prefab != null) Instantiate(prefab, pos, Quaternion.identity);
                    if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_shift", pos, Quaternion.identity, 1f);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_boss_roar", 0.8f, 1.1f);
                GameEvents.Toast("暴暴龙召唤了布丁小弟！");
                actionCooldown = 2f;
                state = State.Idle;
                stateTimer = 0.6f;
            }
        }

        void TickTornado()
        {
            stateTimer -= Time.deltaTime;
            transform.Rotate(Vector3.up, 620f * Time.deltaTime);

            Vector3 dir = player != null ? FlatDir(player.position) : transform.forward;
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = dir.x * 7f;
                vel.z = dir.z * 7f;
                rb.linearVelocity = vel;
            }

            if (player != null && FlatDist(player.position) < 4.2f)
            {
                var target = player.GetComponent<Damageable>();
                if (target != null && !target.IsInvulnerable)
                    target.TakeDamage(tailDamage * 0.5f, player.position + Vector3.up * 0.8f, gameObject, false, 8f);
            }

            if (VFXManager.Instance != null && Random.value < 0.8f)
                VFXManager.Instance.Play("vfx_dash", transform.position + Vector3.up * 1f, Quaternion.identity, 2f);

            if (stateTimer <= 0f)
            {
                actionCooldown = 2.5f;
                state = State.Idle;
                stateTimer = 0.6f;
            }
        }

        int fireballLeft;
        float fireballTick;
        void TickFireball()
        {
            stateTimer -= Time.deltaTime;
            fireballTick -= Time.deltaTime;
            if (player != null) Face(player.position, 5f);

            if (fireballTick <= 0f && fireballLeft > 0)
            {
                fireballTick = 0.22f;
                fireballLeft--;
                SpawnFireball();
            }
            if (stateTimer <= 0f)
            {
                actionCooldown = 2.2f;
                state = State.Idle;
                stateTimer = 0.5f;
            }
        }

        void SpawnFireball()
        {
            if (player == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fireball";
            go.transform.localScale = Vector3.one * 0.5f;
            Vector3 origin = mouthPoint != null ? mouthPoint.position : transform.position + transform.forward * 2f + Vector3.up * 1.5f;
            go.transform.position = origin;

            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Nailoong/VertexLit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(1f, 0.45f, 0.25f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f)); }
                rend.material = mat;
            }
            var sphere = go.GetComponent<Collider>();
            if (sphere != null) Destroy(sphere);

            var proj = go.AddComponent<Projectile>();
            var prb = go.AddComponent<Rigidbody>();
            prb.useGravity = false;
            proj.damage = fireballDamage;
            proj.homing = phase >= 3;
            proj.Launch((player.position + Vector3.up * 0.9f - origin).normalized, 18f, fireballDamage, gameObject);
            proj.hitVfx = "vfx_explode";

            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_breath", 0.5f, 1.4f);
        }

        void TickStagger()
        {
            stateTimer -= Time.deltaTime;
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                vel.x *= 0.85f; vel.z *= 0.85f;
                rb.linearVelocity = vel;
            }
            if (stateTimer <= 0f) { state = State.Chase; actionCooldown = 0.3f; }
        }

        // ---------- 事件 ----------
        void OnDamaged(Damageable self, float amount, Vector3 point)
        {
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_hit", point, Quaternion.identity, 1.1f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_hit", 0.6f, Random.Range(0.8f, 1.1f));
        }

        void OnDied(Damageable self)
        {
            state = State.Dead;
            if (CameraRig.Instance != null) CameraRig.Instance.LockBoss(null);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("sfx_boss_roar", 1f, 0.7f);
                AudioManager.Instance.Play("sfx_levelclear", 1f);
                AudioManager.Instance.PlayMusic("bgm_victory", 1f);
            }
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_explode", transform.position + Vector3.up * 2f, Quaternion.identity, 3f);
                VFXManager.Instance.Shake(1f, 1.2f);
                VFXManager.Instance.ScreenFlash(Color.white, 0.5f);
            }
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            float t = 0f;
            while (t < 2.4f)
            {
                t += Time.deltaTime;
                transform.Rotate(Vector3.up, 180f * Time.deltaTime);
                if (t % 0.25f < Time.deltaTime && VFXManager.Instance != null)
                    VFXManager.Instance.Play("vfx_explode", transform.position + Random.insideUnitSphere * 2.5f, Quaternion.identity, 1.2f);
                transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.4f, 0.05f, 1.4f), Mathf.Clamp01((t - 1.4f) / 1f));
                yield return null;
            }
            GameEvents.Toast("暴暴龙被打败了！零食夺回成功！");
            Destroy(gameObject);
        }

        // ---------- 工具与动画 ----------
        void Face(Vector3 pos, float speed)
        {
            Vector3 dir = FlatDir(pos);
            if (dir.magnitude < 0.01f) return;
            Quaternion want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * speed);
        }

        Vector3 FlatDir(Vector3 pos)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            return d.normalized;
        }

        float FlatDist(Vector3 pos)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = pos; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void Animate()
        {
            if (bodyRoot == null) return;
            flapPhase += Time.deltaTime * (state == State.Charge ? 14f : 5f);
            float flap = Mathf.Sin(flapPhase) * 26f;
            var wl = transform.Find("Wing_L");
            var wr = transform.Find("Wing_R");
            if (wl != null) wl.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (wr != null) wr.localRotation = Quaternion.Euler(0f, 0f, -flap);

            float lean = 0f;
            if (state == State.ChargeWindup) lean = -18f;
            else if (state == State.Charge) lean = 22f;
            else if (state == State.Claw || state == State.Tail) lean = 14f;
            else if (state == State.Stagger) lean = -26f;

            float breathe = Mathf.Sin(Time.time * 1.8f) * 2f;
            bodyRoot.localRotation = Quaternion.Slerp(bodyRoot.localRotation, Quaternion.Euler(lean + breathe, 0f, 0f), Time.deltaTime * 7f);

            var tail = transform.Find("Tail1");
            if (tail != null)
            {
                float sway = Mathf.Sin(Time.time * 3.2f) * 12f;
                tail.localRotation = Quaternion.Euler(0f, sway, 0f);
            }
        }
    }
}
