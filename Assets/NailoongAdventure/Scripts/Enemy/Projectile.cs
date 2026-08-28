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

        public void Launch(Vector3 direction, float speed, float dmg, GameObject ownerGo)
        {
            rb = GetComponent<Rigidbody>();
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
                var p = FindObjectOfType<PlayerController>();
                if (p != null)
                {
                    Vector3 want = (p.transform.position + Vector3.up * 0.8f - transform.position).normalized;
                    Vector3 cur = rb.linearVelocity.normalized;
                    Vector3 next = Vector3.Slerp(cur, want, Time.deltaTime * homingStrength).normalized;
                    rb.linearVelocity = next * rb.linearVelocity.magnitude;
                    transform.forward = next;
                }
            }

            var cols = Physics.OverlapSphere(transform.position, radius, hitMask, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
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
