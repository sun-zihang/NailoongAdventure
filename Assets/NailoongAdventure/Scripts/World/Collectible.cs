using UnityEngine;

namespace Nailoong
{
    public enum PickupType { Snack, Heal, Rage }

    /// <summary>
    /// 可拾取物：零食（任务目标）、布丁蛋糕（回血）、辣椒（火力值）。
    /// 带悬浮旋转动画与磁吸拾取。
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        [Header("属性")]
        public PickupType type = PickupType.Snack;
        public string itemId = "snack";
        public int amount = 1;
        public float healAmount = 18f;
        public float rageAmount = 12f;

        [Header("拾取")]
        public float magnetRange = 2.6f;
        public float magnetSpeed = 9f;
        public float pickRange = 1.1f;

        [Header("动画")]
        public float spinSpeed = 90f;
        public float floatHeight = 0.22f;
        public float floatSpeed = 2.4f;

        Transform player;
        Vector3 basePos;
        bool collected;

        void Start()
        {
            basePos = transform.position;
            var p = PlayerController.Instance;
            if (p != null) player = p.transform;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void Update()
        {
            if (collected) return;
            if (player == null)
            {
                var p = PlayerController.Instance;
                if (p != null) player = p.transform;
            }

            // 悬浮旋转
            float y = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (player == null) return;
            float d = Vector3.Distance(transform.position, player.position);
            if (d < magnetRange)
            {
                Vector3 dir = (player.position + Vector3.up * 0.7f - transform.position).normalized;
                transform.position += dir * magnetSpeed * Time.deltaTime * (1f - d / magnetRange + 0.2f);
                if (d < pickRange) Collect();
            }
        }

        void Collect()
        {
            if (collected) return;
            collected = true;

            var combat = player != null ? player.GetComponent<PlayerCombat>() : null;
            var dmg = player != null ? player.GetComponent<Damageable>() : null;

            switch (type)
            {
                case PickupType.Heal:
                    if (dmg != null) dmg.Heal(healAmount);
                    break;
                case PickupType.Rage:
                    if (combat != null) combat.AddRage(rageAmount);
                    break;
                default:
                    GameEvents.ItemCollected(itemId, amount);
                    break;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_pickup", 0.85f, Random.Range(0.95f, 1.2f));
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_pickup", transform.position, Quaternion.identity, 1f);
            Destroy(gameObject);
        }

        /// <summary>资源缺失时的运行时兜底：生成一个发光小球。</summary>
        public static GameObject SpawnFallback(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Pickup_Snack";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.42f;
            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Nailoong/VertexLit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(1f, 0.82f, 0.35f);
                rend.material = mat;
            }
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            go.AddComponent<Collectible>();
            return go;
        }
    }
}
