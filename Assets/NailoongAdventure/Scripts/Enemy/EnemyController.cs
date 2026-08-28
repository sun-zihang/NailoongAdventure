using System.Collections;
using UnityEngine;

namespace Nailoong
{
    public enum EnemyKind { Pudding, Bird, Elite }

    /// <summary>
    /// 小怪 AI：巡逻 → 追击 → 攻击 → 受击 → 死亡。
    /// 动画同样是程序化的：布丁怪靠挤压拉伸与弹跳前进，炸鸡鸟靠翅膀扇动与俯冲。
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class EnemyController : MonoBehaviour
    {
        [Header("类型")]
        public EnemyKind kind = EnemyKind.Pudding;

        [Header("感知")]
        public float detectRange = 12f;
        public float attackRange = 1.9f;
        public float loseRange = 18f;

        [Header("移动")]
        public float moveSpeed = 2.6f;
        public float chaseSpeed = 4.2f;
        public float turnSpeed = 6f;
        public float hopHeight = 0.55f;

        [Header("攻击")]
        public float attackDamage = 12f;
        public float attackWindup = 0.35f;
        public float attackCooldown = 1.6f;

        [Header("掉落")]
        public string dropPrefab = "Prefabs/Pickup_Snack";
        public int dropCount = 1;
        [Range(0, 1)] public float dropChance = 0.75f;

        [Header("巡逻")]
        public float patrolRadius = 6f;
        public float idleTime = 1.6f;

        Damageable dmg;
        Transform player;
        Vector3 homePos, patrolTarget;

        enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
        State state = State.Idle;

        float stateTimer, attackTimer, hopPhase, flapPhase, hurtTimer;
        Vector3 visualBase, bodyBasePos;
        Transform bodyRoot;
        Rigidbody rb;

        void Awake()
        {
            dmg = GetComponent<Damageable>();
            rb = GetComponent<Rigidbody>();
            homePos = transform.position;
            bodyRoot = transform.Find("Body") ?? transform;
            visualBase = bodyRoot.localScale;
            bodyBasePos = bodyRoot.localPosition;
            PickPatrolTarget();
        }

        void Start()
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) player = p.transform;
            dmg.Damaged += OnDamaged;
            dmg.Died += OnDied;
        }

        void Update()
        {
            if (state == State.Dead) return;
            if (player == null)
            {
                var p = FindObjectOfType<PlayerController>();
                if (p != null) player = p.transform;
            }

            if (attackTimer > 0f) attackTimer -= Time.deltaTime;
            if (hurtTimer > 0f) hurtTimer -= Time.deltaTime;

            switch (state)
            {
                case State.Idle: TickIdle(); break;
                case State.Patrol: TickPatrol(); break;
                case State.Chase: TickChase(); break;
                case State.Attack: TickAttack(); break;
                case State.Hurt: TickHurt(); break;
            }

            Animate();
        }

        // ---------- 状态 ----------
        void TickIdle()
        {
            stateTimer -= Time.deltaTime;
            if (player != null && Dist(player.position) < detectRange) { state = State.Chase; return; }
            if (stateTimer <= 0f) { state = State.Patrol; PickPatrolTarget(); }
        }

        void TickPatrol()
        {
            if (player != null && Dist(player.position) < detectRange) { state = State.Chase; return; }
            if (MoveTo(patrolTarget, moveSpeed * 0.6f) || Dist(patrolTarget) < 0.5f)
            {
                state = State.Idle;
                stateTimer = idleTime;
            }
        }

        void TickChase()
        {
            if (player == null) { state = State.Idle; return; }
            float d = Dist(player.position);
            if (d > loseRange) { state = State.Patrol; PickPatrolTarget(); return; }
            if (d <= attackRange && attackTimer <= 0f) { state = State.Attack; stateTimer = attackWindup; return; }
            MoveTo(player.position, chaseSpeed);
        }

        void TickAttack()
        {
            stateTimer -= Time.deltaTime;
            if (player != null)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                dir.y = 0f;
                if (dir.magnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed * 2f);
            }

            if (stateTimer <= 0f)
            {
                DoDamage();
                attackTimer = attackCooldown;
                state = State.Chase;
            }
        }

        void DoDamage()
        {
            if (player == null) return;
            var target = player.GetComponent<Damageable>();
            if (target == null || target.IsDead) return;

            float d = Vector3.Distance(transform.position, player.position);
            if (d <= attackRange + 0.4f)
            {
                Vector3 point = player.position + Vector3.up * 0.8f;
                target.TakeDamage(attackDamage, point, gameObject, false, 5f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_swing", 0.5f, 1.4f);
        }

        void TickHurt()
        {
            if (hurtTimer <= 0f) state = State.Chase;
        }

        // ---------- 事件 ----------
        void OnDamaged(Damageable self, float amount, Vector3 point)
        {
            if (state == State.Dead) return;
            hurtTimer = 0.28f;
            state = State.Hurt;
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_hit", 0.55f, Random.Range(0.85f, 1.2f));
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_hit", point, Quaternion.identity, 0.8f);
        }

        void OnDied(Damageable self)
        {
            state = State.Dead;
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_enemy_die", 0.8f, Random.Range(0.9f, 1.1f));
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_explode", transform.position + Vector3.up * 0.5f, Quaternion.identity, 1f);
                VFXManager.Instance.Shake(0.18f, 0.2f);
            }
            DropLoot();
            StartCoroutine(DeathRoutine());
        }

        void DropLoot()
        {
            if (string.IsNullOrEmpty(dropPrefab)) return;
            var prefab = Resources.Load<GameObject>(dropPrefab);
            for (int i = 0; i < dropCount; i++)
            {
                if (Random.value > dropChance) continue;
                Vector3 pos = transform.position + Vector3.up * 0.6f + Random.insideUnitSphere * 0.4f;
                if (prefab != null) Instantiate(prefab, pos, Quaternion.identity);
                else Collectible.SpawnFallback(pos);
            }
        }

        IEnumerator DeathRoutine()
        {
            // 摊平 → 缩小 → 消失
            float t = 0f;
            Vector3 start = bodyRoot.localScale;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float k = t / 0.55f;
                bodyRoot.localScale = new Vector3(
                    Mathf.Lerp(start.x, start.x * 1.5f, Mathf.SmoothStep(0f, 0.4f, k)),
                    Mathf.Lerp(start.y, start.y * 0.05f, Mathf.SmoothStep(0f, 1f, k)),
                    Mathf.Lerp(start.z, start.z * 1.5f, Mathf.SmoothStep(0f, 0.4f, k)));
                yield return null;
            }
            Destroy(gameObject);
        }

        // ---------- 移动与动画 ----------
        bool MoveTo(Vector3 target, float speed)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            float d = dir.magnitude;
            if (d < 0.001f) return true;
            dir.Normalize();

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);

            if (kind == EnemyKind.Bird)
            {
                // 飞行敌人：高度正弦浮动 + 直接平移
                float hover = 1.6f + Mathf.Sin(Time.time * 2.2f) * 0.35f;
                Vector3 pos = transform.position + dir * speed * Time.deltaTime;
                pos.y = Mathf.Lerp(pos.y, TerrainY(pos) + hover, Time.deltaTime * 3f);
                transform.position = pos;
            }
            else if (rb != null)
            {
                // 地面敌人：用速度驱动，配合弹跳视觉
                Vector3 vel = rb.linearVelocity;
                vel.x = dir.x * speed;
                vel.z = dir.z * speed;
                rb.linearVelocity = vel;
                hopPhase += Time.deltaTime * speed * 2.2f;
            }
            else
            {
                transform.position += dir * speed * Time.deltaTime;
            }
            return false;
        }

        float TerrainY(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out var hit, 120f))
                return hit.point.y;
            return 0f;
        }

        void Animate()
        {
            if (bodyRoot == null) return;

            if (kind == EnemyKind.Bird)
            {
                flapPhase += Time.deltaTime * (state == State.Chase ? 18f : 9f);
                float flap = Mathf.Sin(flapPhase) * 40f;
                var wl = transform.Find("Wing_L");
                var wr = transform.Find("Wing_R");
                if (wl != null) wl.localRotation = Quaternion.Euler(0f, 0f, flap);
                if (wr != null) wr.localRotation = Quaternion.Euler(0f, 0f, -flap);

                float tilt = state == State.Attack ? 35f : 0f;
                bodyRoot.localRotation = Quaternion.Slerp(bodyRoot.localRotation, Quaternion.Euler(tilt, 0f, 0f), Time.deltaTime * 8f);
                return;
            }

            // 果冻怪：呼吸 + 弹跳挤压
            float moving = (state == State.Chase || state == State.Patrol) ? 1f : 0f;
            float bounce = Mathf.Abs(Mathf.Sin(hopPhase)) * moving;
            float breathe = Mathf.Sin(Time.time * 2.6f) * 0.06f;

            float sx = visualBase.x * (1f - bounce * 0.16f + breathe);
            float sy = visualBase.y * (1f + bounce * 0.34f - breathe);
            float sz = visualBase.z * (1f - bounce * 0.16f + breathe);

            if (state == State.Attack)
            {
                float k = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(attackWindup, 0.01f));
                sy *= 1f - k * 0.18f;
                sx *= 1f + k * 0.22f;
                sz *= 1f + k * 0.22f;
            }
            if (hurtTimer > 0f)
            {
                float k = hurtTimer / 0.28f;
                sy *= 1f - k * 0.3f;
                sx *= 1f + k * 0.28f;
                sz *= 1f + k * 0.28f;
            }

            bodyRoot.localScale = new Vector3(sx, sy, sz);
            bodyRoot.localPosition = bodyBasePos + new Vector3(0f, bounce * hopHeight, 0f);
        }

        float Dist(Vector3 p)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = p; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void PickPatrolTarget()
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            patrolTarget = homePos + new Vector3(r.x, 0f, r.y);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
