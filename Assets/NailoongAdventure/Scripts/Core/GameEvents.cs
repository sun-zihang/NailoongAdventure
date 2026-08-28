using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 轻量事件总线：战斗、任务、关卡各系统通过它解耦通信。
    /// </summary>
    public static class GameEvents
    {
        // 战斗
        public static event Action<GameObject, float, Vector3> OnDamage;          // 受害者, 伤害, 命中点
        public static event Action<GameObject> OnKilled;                          // 死亡单位
        public static event Action<float> OnPlayerHealthChanged;
        public static event Action<float> OnRageChanged;
        public static event Action<string> OnSkillCast;                           // 技能名
        public static event Action OnPlayerDead;

        // 世界
        public static event Action<string, int> OnItemCollected;                  // 物品类型, 当前数量
        public static event Action<string> OnCheckpointReached;
        public static event Action<GameObject> OnInteractFocus;
        public static event Action<string, string> OnDialogue;                    // 说话者, 内容

        // 流程
        public static event Action<int> OnLevelStart;
        public static event Action<int, float> OnLevelClear;                      // 关卡序号, 用时
        public static event Action OnGameOver;
        public static event Action<string> OnToast;                               // 屏幕提示

        public static void Damage(GameObject victim, float amount, Vector3 point)
            => OnDamage?.Invoke(victim, amount, point);
        public static void Killed(GameObject unit) => OnKilled?.Invoke(unit);
        public static void PlayerHealth(float v) => OnPlayerHealthChanged?.Invoke(v);
        public static void Rage(float v) => OnRageChanged?.Invoke(v);
        public static void SkillCast(string name) => OnSkillCast?.Invoke(name);
        public static void PlayerDead() => OnPlayerDead?.Invoke();
        public static void ItemCollected(string type, int count) => OnItemCollected?.Invoke(type, count);
        public static void Checkpoint(string id) => OnCheckpointReached?.Invoke(id);
        public static void InteractFocus(GameObject go) => OnInteractFocus?.Invoke(go);
        public static void Dialogue(string speaker, string content) => OnDialogue?.Invoke(speaker, content);
        public static void LevelStart(int index) => OnLevelStart?.Invoke(index);
        public static void LevelClear(int index, float time) => OnLevelClear?.Invoke(index, time);
        public static void GameOver() => OnGameOver?.Invoke();
        public static void Toast(string msg) => OnToast?.Invoke(msg);

        /// <summary>切场景时清空所有监听，避免跨场景悬挂引用。</summary>
        public static void ResetAll()
        {
            OnDamage = null; OnKilled = null; OnPlayerHealthChanged = null; OnRageChanged = null;
            OnSkillCast = null; OnPlayerDead = null; OnItemCollected = null; OnCheckpointReached = null;
            OnInteractFocus = null; OnDialogue = null; OnLevelStart = null; OnLevelClear = null;
            OnGameOver = null; OnToast = null;
        }
    }
}
