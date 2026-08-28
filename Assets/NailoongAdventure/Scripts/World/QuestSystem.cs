using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    public enum QuestType { Collect, Kill, Free, Talk, Reach, Boss }

    [Serializable]
    public class Quest
    {
        public string id = "quest";
        public string title = "任务标题";
        public string description = "任务描述";
        public QuestType type = QuestType.Collect;
        public string targetId = "snack";     // 物品 id / 敌人名 / 目标标签
        public int required = 1;
        public int current;
        public string skillReward = "";       // 完成后解锁的技能
        public bool showPortalHint = true;

        public bool IsComplete => current >= required;
        public string ProgressText => $"{Mathf.Min(current, required)}/{required}";
    }

    /// <summary>
    /// 任务系统：链式推进，监听战斗/收集事件自动计数，完成后解锁技能并通知关卡流程。
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("任务链")]
        public List<Quest> quests = new List<Quest>();

        [Header("提示")]
        public float toastDuration = 3.5f;

        public event Action<Quest> OnQuestComplete;
        public event Action<Quest> OnQuestChanged;
        public event Action OnAllComplete;

        int activeIndex;
        bool allDone;

        public Quest ActiveQuest => activeIndex >= 0 && activeIndex < quests.Count ? quests[activeIndex] : null;
        public bool AllComplete => allDone;
        public int ActiveIndex => activeIndex;

        void Awake() => Instance = this;

        void OnEnable()
        {
            GameEvents.OnItemCollected += HandleItem;
            GameEvents.OnKilled += HandleKilled;
            GameEvents.OnDialogue += HandleDialogue;
        }

        void OnDisable()
        {
            GameEvents.OnItemCollected -= HandleItem;
            GameEvents.OnKilled -= HandleKilled;
            GameEvents.OnDialogue -= HandleDialogue;
        }

        void Start()
        {
            if (quests.Count > 0)
            {
                activeIndex = 0;
                Announce(quests[0]);
            }
        }

        void HandleItem(string itemId, int count)
        {
            var q = ActiveQuest;
            if (q == null) return;
            if ((q.type == QuestType.Collect || q.type == QuestType.Free) && q.targetId == itemId)
                Advance(q, count);
        }

        void HandleKilled(GameObject unit)
        {
            var q = ActiveQuest;
            if (q == null || unit == null) return;

            if (q.type == QuestType.Kill && (unit.name.Contains(q.targetId) || unit.CompareTag(q.targetId)))
                Advance(q, 1);

            if (q.type == QuestType.Boss && unit.GetComponent<BossController>() != null)
            {
                q.current = q.required;
                Complete(q);
            }
        }

        void HandleDialogue(string speaker, string content)
        {
            var q = ActiveQuest;
            if (q == null) return;
            if (q.type == QuestType.Talk && speaker == q.targetId) Advance(q, 1);
        }

        public void Advance(Quest q, int amount)
        {
            if (q == null || q.IsComplete) return;
            q.current = Mathf.Min(q.required, q.current + amount);
            OnQuestChanged?.Invoke(q);
            if (q.IsComplete) Complete(q);
        }

        /// <summary>区域触发型任务（Reach）由触发器调用。</summary>
        public void ReportReach(string zoneId)
        {
            var q = ActiveQuest;
            if (q == null) return;
            if (q.type == QuestType.Reach && q.targetId == zoneId) Advance(q, 1);
        }

        void Complete(Quest q)
        {
            OnQuestComplete?.Invoke(q);

            if (!string.IsNullOrEmpty(q.skillReward) && GameManager.Instance != null)
            {
                GameManager.Instance.UnlockSkill(q.skillReward);
                var combat = FindObjectOfType<PlayerCombat>();
                if (combat != null) combat.UnlockSkill(q.skillReward);
                GameEvents.Toast($"解锁新技能：{SkillDisplayName(q.skillReward)}！");
            }

            if (VFXManager.Instance != null) VFXManager.Instance.ScreenFlash(new Color(1f, 0.9f, 0.6f, 0.28f), 0.3f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_levelclear", 0.7f, 1.2f);

            activeIndex++;
            if (activeIndex >= quests.Count)
            {
                allDone = true;
                OnAllComplete?.Invoke();
                GameEvents.Toast("全部目标完成！");
            }
            else
            {
                Announce(quests[activeIndex]);
            }
        }

        void Announce(Quest q)
        {
            OnQuestChanged?.Invoke(q);
            GameEvents.Toast($"新目标：{q.title}（{q.description}）");
        }

        public static string SkillDisplayName(string id)
        {
            switch (id)
            {
                case "slam": return "泰山压顶";
                case "breath": return "龙耀吐息";
                case "shift": return "奶龙变色";
                case "roll": return "咕噜冲撞";
                default: return id;
            }
        }
    }
}
