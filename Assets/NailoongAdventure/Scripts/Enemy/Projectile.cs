using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 通用投射物：暴暴龙火球、星火弹幕等。命中造成伤害后自毁。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public float damage = 14f;
        public float life = 5f;
        public float radius = 0.35f;
        public LayerMask hitMask = ~0;
        public GameObject owner;
        public string hitVfx = "vfx_explode";
        public bool homing;
        public float homingStrength = 2.5f;

        Rigidbody rb;
        float timer;
        TrailRenderer trail;

        // 复用缓冲区：避免每帧 OverlapSphere 分配新数组造成 GC 压力
        const int MaxHits = 16;
        readonly Collider[] hits = new Collider[MaxHits];

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 direction, float speed, float dmg, GameObject ownerGo)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            owner = ownerGo;
            damage = dmg;
            rb.useGravity = false;
            rb.linearVelocity = direction.normalized * speed;
            timer = life;
            transform.forward = direction.normalized;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) { Explode(null); return; }

            if (homing)
            {
                // 用静态单例取玩家，避免每帧 FindObjectOfType 全场景遍历
                var p = PlayerController.Instance;
                if (p != null)
                {
                    Vector3 want = (p.transform.position + Vector3.up * 0.8f - transform.position).normalized;
                    Vector3 cur = rb.linearVelocity.normalized;
                    Vector3 next = Vector3.Slerp(cur, want, Time.deltaTime * homingStrength).normalized;
                    rb.linearVelocity = next * rb.linearVelocity.magnitude;
                    transform.forward = next;
                }
            }

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, hitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                if (owner != null && c.transform.IsChildOf(owner.transform)) continue;
                var target = c.GetComponentInParent<Damageable>();
                if (target != null)
                {
                    target.TakeDamage(damage, c.ClosestPoint(transform.position), owner, false, 4f);
                    Explode(c);
                    return;
                }
                if (!c.isTrigger) { Explode(c); return; }
            }
        }

        void Explode(Collider hit)
        {
            if (VFXManager.Instance != null)
                VFXManager.Instance.Play(hitVfx, transform.position, Quaternion.identity, 1f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt("sfx_hit", transform.position, 0.7f);
            Destroy(gameObject);
        }
    }
}
