using UnityEngine;

namespace Nailoong
{
    public enum InteractKind { Talk, Cage, Portal }

    /// <summary>
    /// 场景互动物：对话 NPC、可击破的笼子、通往下一关的传送门。
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("类型")]
        public InteractKind kind = InteractKind.Talk;

        [Header("对话")]
        public string speakerName = "小七";
        [TextArea(2, 5)]
        public string[] lines = { "奶龙，我们去找回被抢走的零食吧！" };

        [Header("笼子")]
        public GameObject freedCreature;      // 破笼后出现的伙伴
        public string freedCreaturePrefab = "Prefabs/Chick";

        [Header("传送门")]
        public bool requiresUnlock = true;

        [Header("范围")]
        public float range = 3.2f;

        public bool IsDone { get; private set; }

        Transform player;
        int lineIndex;
        bool inRange;
        Damageable dmg;

        void Start()
        {
            var p = PlayerController.Instance;
            if (p != null) player = p.transform;

            if (kind == InteractKind.Cage)
            {
                dmg = GetComponent<Damageable>();
                if (dmg == null) dmg = gameObject.AddComponent<Damageable>();
                dmg.faction = Faction.Enemy;
                dmg.maxHealth = 30f;
                dmg.health = 30f;
                dmg.knockbackResist = 1f;
                dmg.showDamageText = false;
                dmg.Died += OnCageBroken;
            }
        }

        void Update()
        {
            if (IsDone) return;
            if (player == null)
            {
                var p = PlayerController.Instance;
                if (p != null) player = p.transform;
                return;
            }

            bool nowIn = Vector3.Distance(transform.position, player.position) < range;
            if (nowIn != inRange)
            {
                inRange = nowIn;
                GameEvents.InteractFocus(nowIn ? gameObject : null);
            }

            if (inRange && Input.GetKeyDown(KeyCode.E)) Interact();
        }

        public void Interact()
        {
            if (IsDone) return;

            if (kind == InteractKind.Talk)
            {
                if (lines == null || lines.Length == 0) return;
                GameEvents.Dialogue(speakerName, lines[lineIndex % lines.Length]);
                lineIndex++;
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_ui", 0.6f);
                if (lineIndex >= lines.Length) MarkDone();
                return;
            }

            if (kind == InteractKind.Portal)
            {
                if (requiresUnlock && QuestSystem.Instance != null && !QuestSystem.Instance.AllComplete)
                {
                    GameEvents.Toast("任务还没完成，传送门不会开启哦！");
                    return;
                }
                EnterPortal();
            }
        }

        void OnCageBroken(Damageable self)
        {
            if (IsDone) return;
            MarkDone();
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.Play("vfx_explode", transform.position + Vector3.up * 0.6f, Quaternion.identity, 1.2f);
                VFXManager.Instance.Play("vfx_pickup", transform.position + Vector3.up * 0.8f, Quaternion.identity, 1.5f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_levelclear", 0.7f, 1.3f);

            GameObject creature = null;
            if (freedCreature != null) creature = Instantiate(freedCreature, transform.position + Vector3.forward * 0.8f, Quaternion.identity);
            else
            {
                var prefab = Resources.Load<GameObject>(freedCreaturePrefab);
                if (prefab != null) creature = Instantiate(prefab, transform.position + Vector3.forward * 0.8f, Quaternion.identity);
            }
            if (creature != null)
            {
                creature.name = "Freed_Chick";
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_pickup", 0.9f, 1.4f);
            }
            GameEvents.ItemCollected("cage", 1);
            GameEvents.Toast("救出一只小鸡！");
            Destroy(gameObject, 0.35f);
        }

        void EnterPortal()
        {
            MarkDone();
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_levelclear", 1f);
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_shift", transform.position, Quaternion.identity, 2f);
            if (GameManager.Instance != null) GameManager.Instance.NextLevel();
        }

        void MarkDone()
        {
            IsDone = true;
            GameEvents.InteractFocus(null);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
