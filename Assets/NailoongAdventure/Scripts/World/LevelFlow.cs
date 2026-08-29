using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 关卡流程编排：开场演出（标题 + 目标 + BGM）、任务完成后的通关处理、死亡重生。
    /// </summary>
    public class LevelFlow : MonoBehaviour
    {
        [Header("关卡信息")]
        public int levelIndex = 0;
        public string levelName = "奶黄海滩";
        [TextArea(2, 4)]
        public string levelGoal = "找回被暴暴龙抢走的零食！";
        public string bgm = "bgm_level1";

        [Header("通关")]
        public bool clearOnAllQuests = true;
        public GameObject portalToActivate;       // 任务全完成后激活的传送门
        public string clearMessage = "关卡完成！";
        [Tooltip("三星目标用时（秒）；0 = 自动按关卡序号估算")]
        public float parTime = 0f;

        public float ParTime => parTime > 0f ? parTime : 75f + levelIndex * 45f;

        [Header("死亡")]
        public Transform respawnPoint;
        public int maxRevives = 3;
        int revivesUsed;

        QuestSystem quests;
        bool cleared;

        void Start()
        {
            quests = GetComponent<QuestSystem>();
            if (quests == null) quests = FindObjectOfType<QuestSystem>();
            if (quests != null) quests.OnAllComplete += HandleAllComplete;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(bgm, 1.2f);
            if (UIManager.Instance != null) UIManager.Instance.ShowLevelIntro(levelName, levelGoal);
            if (respawnPoint == null)
            {
                var spawn = GameObject.Find("SpawnPoint");
                if (spawn != null) respawnPoint = spawn.transform;
            }

            GameEvents.OnPlayerDead += HandlePlayerDead;
        }

        void OnDestroy() => GameEvents.OnPlayerDead -= HandlePlayerDead;

        void HandleAllComplete()
        {
            if (cleared) return;

            if (clearOnAllQuests)
            {
                cleared = true;
                GameEvents.Toast(clearMessage);
                Celebrate();
                Invoke(nameof(DoClear), 1.2f);
            }
            else if (portalToActivate != null)
            {
                portalToActivate.SetActive(true);
                GameEvents.Toast("传送门已开启！");
            }
        }

        /// <summary>通关庆祝：音效 + 全屏星光 + 屏震。</summary>
        void Celebrate()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_levelclear", 1f);
            if (VFXManager.Instance != null)
            {
                var p = PlayerController.Instance;
                Vector3 pos = p != null ? p.transform.position : transform.position;
                for (int i = 0; i < 6; i++)
                {
                    Vector3 off = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(0.5f, 3f), Random.Range(-2.5f, 2.5f));
                    VFXManager.Instance.Play("vfx_pickup", pos + off, Quaternion.identity, 1.6f);
                }
                VFXManager.Instance.Shake(0.25f, 0.3f);
            }
        }

        void DoClear()
        {
            if (GameManager.Instance != null) GameManager.Instance.ClearCurrentLevel();
        }

        void HandlePlayerDead()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            if (revivesUsed < maxRevives)
            {
                revivesUsed++;
                Invoke(nameof(Revive), 1.4f);
                if (UIManager.Instance != null) UIManager.Instance.ShowRevive(revivesUsed, maxRevives);
            }
            else
            {
                if (GameManager.Instance != null) GameManager.Instance.GameOver();
            }
        }

        void Revive()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            var dmg = player.GetComponent<Damageable>();
            if (dmg != null) { dmg.ResetHealth(); dmg.SetInvulnerable(2f); }
            player.CanAct = true;

            if (respawnPoint != null) player.Teleport(respawnPoint.position);
            else player.Teleport(Vector3.up * 3f);

            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_heal", player.transform.position, Quaternion.identity, 1.5f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_pickup", 1f, 1.1f);
        }
    }
}
