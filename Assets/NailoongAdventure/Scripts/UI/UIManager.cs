using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nailoong
{
    /// <summary>
    /// UI 中枢：运行时用代码构建整套界面（不依赖任何预制体），
    /// 包含 HUD、技能栏、任务追踪、Boss 血条、飘字、闪屏，以及各类面板（见 UIPanels.cs）。
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("配色")]
        public Color healthColor = new Color(1f, 0.35f, 0.42f);
        public Color rageColor = new Color(1f, 0.72f, 0.18f);
        public Color bossColor = new Color(0.72f, 0.35f, 0.95f);
        public Color accentColor = new Color(1f, 0.83f, 0.35f);
        public Color panelColor = new Color(0.08f, 0.09f, 0.14f, 0.88f);

        Font font;
        Canvas canvas;
        RectTransform hudRoot, floatRoot;

        // HUD 元素
        Image healthFill, rageFill, bossFill;
        GameObject bossBar, interactHint, toastObj;
        Text toastText, questTitleText, questProgressText, interactText, centerBigText, centerSubText;
        readonly List<SkillSlot> skillSlots = new List<SkillSlot>();
        float toastTimer, flashTimer, introTimer;

        PlayerCombat combat;
        Damageable playerHealth;
        QuestSystem boundQuest;
        Image flashImage;

        class SkillSlot
        {
            public GameObject root;
            public Text keyText, nameText;
            public Image cooldownMask, icon;
            public PlayerCombat.SkillSetting skill;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            font = Resources.Load<Font>("Fonts/simhei");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildCanvas();
            BuildHud();
            BuildPanels();
            BindEvents();
        }

        void Start()
        {
            RefreshQuestDisplay();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
                ShowMainMenu(true);
        }

        void Update()
        {
            TickHud();
            TickToast();
            TickFlash();
            TickIntro();
        }

        // ================= 画布 =================
        void BuildCanvas()
        {
            var go = new GameObject("UI_Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            hudRoot = NewRect("HUD", go.transform);
            Stretch(hudRoot);
            floatRoot = NewRect("FloatRoot", go.transform);
            Stretch(floatRoot);

            // 闪屏层
            flashImage = NewImage("Flash", go.transform, Color.white, 0f);
            Stretch(flashImage.rectTransform);
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.raycastTarget = false;
        }

        // ================= HUD =================
        void BuildHud()
        {
            // 左上：生命 / 火力
            var stats = NewPanel("Stats", hudRoot, new Color(0.05f, 0.06f, 0.1f, 0.55f));
            Anchor(stats.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(430, -170));

            NewText("名字", stats.transform, "奶龙 Nailoong", 26, accentColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -14), new Vector2(360, -50));

            healthFill = NewBar("HealthBar", stats.transform, healthColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -58), new Vector2(400, -92));
            NewText("血标签", stats.transform, "HP", 20, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -57), new Vector2(90, -93), TextAnchor.MiddleLeft);

            rageFill = NewBar("RageBar", stats.transform, rageColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -100), new Vector2(400, -134));
            NewText("火标签", stats.transform, "火力", 20, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -99), new Vector2(90, -135), TextAnchor.MiddleLeft);

            // 右上：任务追踪
            var questPanel = NewPanel("QuestPanel", hudRoot, new Color(0.05f, 0.06f, 0.1f, 0.55f));
            Anchor(questPanel.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-450, -30), new Vector2(-30, -140));
            NewText("任务标题标签", questPanel.transform, "当前目标", 22, accentColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(300, -46), TextAnchor.MiddleLeft);
            questTitleText = NewText("任务名", questPanel.transform, "-", 26, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -48), new Vector2(400, -84), TextAnchor.MiddleLeft);
            questProgressText = NewText("任务进度", questPanel.transform, "0/0", 22, new Color(0.85f, 0.9f, 1f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -86), new Vector2(400, -118), TextAnchor.MiddleLeft);

            // 左下：技能栏
            var skillBar = NewRect("SkillBar", hudRoot);
            Anchor(skillBar, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(30, 130));
            var layout = skillBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = skillBar.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 中央：Boss 血条
            bossBar = NewPanel("BossBar", hudRoot, new Color(0.05f, 0.04f, 0.08f, 0.6f));
            Anchor(bossBar.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-420, -180), new Vector2(420, -240));
            var bossName = NewText("Boss名", bossBar.transform, "暴暴龙", 26, bossColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -6), new Vector2(400, -40), TextAnchor.MiddleLeft);
            bossFill = NewBar("BossFill", bossBar.transform, bossColor,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 8), new Vector2(-20, 34));
            bossBar.SetActive(false);

            // 中央下：交互提示
            interactHint = NewPanel("InteractHint", hudRoot, new Color(0.05f, 0.06f, 0.1f, 0.7f));
            Anchor(interactHint.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-220, 190), new Vector2(220, 250));
            interactText = NewText("交互文本", interactHint.transform, "按 E 互动", 26, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            interactHint.SetActive(false);

            // 中央：Toast
            toastObj = NewPanel("Toast", hudRoot, new Color(0.05f, 0.06f, 0.1f, 0.75f));
            Anchor(toastObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, 90), new Vector2(460, 150));
            toastText = NewText("Toast文本", toastObj.transform, "", 28, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            toastObj.SetActive(false);

            // 关卡开场大字
            centerBigText = NewText("开场标题", hudRoot, "", 78, accentColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600, 40), new Vector2(600, 140), TextAnchor.MiddleCenter);
            centerSubText = NewText("开场副标题", hudRoot, "", 32, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600, -40), new Vector2(600, 10), TextAnchor.MiddleCenter);
            centerBigText.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
            centerSubText.color = new Color(1f, 1f, 1f, 0f);
        }

        // ================= 事件绑定 =================
        void BindEvents()
        {
            GameEvents.OnPlayerHealthChanged += v => { if (healthFill != null) healthFill.fillAmount = v; };
            GameEvents.OnRageChanged += v => { if (rageFill != null) rageFill.fillAmount = v; };
            GameEvents.OnToast += ShowToast;
            GameEvents.OnDialogue += (speaker, content) => ShowDialogue(speaker, content);
            GameEvents.OnInteractFocus += HandleInteractFocus;
            GameEvents.OnLevelClear += (index, time) => ShowLevelClear(index, time);
            GameEvents.OnGameOver += () => ShowGameOver(true);
        }

        // ================= 每帧刷新 =================
        void TickHud()
        {
            if (combat == null) combat = FindObjectOfType<PlayerCombat>();
            if (playerHealth == null && combat != null) playerHealth = combat.GetComponent<Damageable>();

            // 延迟绑定：场景中的玩家/任务系统可能晚于 UI 创建，这里每帧检测一次
            if (combat != null && skillSlots.Count == 0) BindSkillBar(combat);

            var qs = QuestSystem.Instance;
            if (qs != null && boundQuest != qs)
            {
                boundQuest = qs;
                qs.OnQuestChanged += _ => RefreshQuestDisplay();
                qs.OnQuestComplete += _ => RefreshQuestDisplay();
                qs.OnAllComplete += () => RefreshQuestDisplay();
                RefreshQuestDisplay();
            }

            if (playerHealth != null && healthFill != null)
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, playerHealth.Health01, Time.deltaTime * 12f);
            if (combat != null && rageFill != null)
                rageFill.fillAmount = Mathf.Lerp(rageFill.fillAmount, combat.Rage01, Time.deltaTime * 12f);

            // Boss 血条
            var boss = BossController.Instance;
            if (boss != null && bossBar != null)
            {
                if (!bossBar.activeSelf) bossBar.SetActive(true);
                if (bossFill != null) bossFill.fillAmount = Mathf.Lerp(bossFill.fillAmount, boss.Health01, Time.deltaTime * 10f);
            }
            else if (bossBar != null && bossBar.activeSelf) bossBar.SetActive(false);

            // 技能冷却
            for (int i = 0; i < skillSlots.Count; i++)
            {
                var slot = skillSlots[i];
                if (slot.skill == null) continue;
                float cd = slot.skill.cooldown > 0f && combat != null && combat.Cooldowns.TryGetValue(slot.skill.id, out var left)
                    ? left / slot.skill.cooldown : 0f;
                slot.cooldownMask.fillAmount = cd;
                bool ready = cd <= 0f && (combat == null || combat.rage >= slot.skill.rageCost);
                slot.icon.color = slot.skill.unlocked ? (ready ? Color.white : new Color(0.55f, 0.55f, 0.6f)) : new Color(0.25f, 0.25f, 0.3f);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance != null)
            {
                if (GameManager.Instance.State == GameState.Playing) ShowPause(true);
                else if (GameManager.Instance.State == GameState.Paused) ShowPause(false);
            }
        }

        void TickToast()
        {
            if (toastTimer <= 0f) return;
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f && toastObj != null) toastObj.SetActive(false);
        }

        void TickFlash()
        {
            if (flashImage == null) return;
            if (flashTimer <= 0f) return;
            flashTimer -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(flashTimer / 0.25f);
            var c = flashImage.color;
            flashImage.color = new Color(c.r, c.g, c.b, a);
            if (flashTimer <= 0f) flashImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        void TickIntro()
        {
            if (introTimer <= 0f) return;
            introTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(introTimer / 3.2f);
            float fade = k > 0.75f ? (1f - k) / 0.25f : Mathf.Clamp01(k / 0.35f);
            if (centerBigText != null) centerBigText.color = new Color(accentColor.r, accentColor.g, accentColor.b, fade);
            if (centerSubText != null) centerSubText.color = new Color(1f, 1f, 1f, fade * 0.95f);
        }

        // ================= 对外接口 =================
        public void ShowToast(string msg)
        {
            if (toastObj == null) return;
            toastObj.SetActive(true);
            toastText.text = msg;
            toastTimer = 3.5f;
        }

        public void FlashScreen(Color color, float duration)
        {
            if (flashImage == null) return;
            flashImage.color = color;
            flashTimer = duration;
        }

        public void ShowLevelIntro(string title, string goal)
        {
            if (centerBigText == null) return;
            centerBigText.text = title;
            centerSubText.text = goal;
            introTimer = 3.2f;
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color, bool critical)
        {
            if (floatRoot == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z < 0f) return;

            var go = new GameObject("FloatText");
            go.transform.SetParent(floatRoot, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = critical ? 46 : 32;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(200, 80);
            rt.anchoredPosition = new Vector2(screen.x - Screen.width * 0.5f, screen.y - Screen.height * 0.5f);
            StartCoroutine(FloatTextRoutine(rt, t, critical));
        }

        IEnumerator FloatTextRoutine(RectTransform rt, Text t, bool critical)
        {
            float life = critical ? 1.0f : 0.75f;
            float elapsed = 0f;
            Vector2 start = rt.anchoredPosition + new Vector2(Random.Range(-24f, 24f), 0f);
            float rise = critical ? 120f : 85f;
            while (elapsed < life)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = elapsed / life;
                rt.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 12f, rise * k);
                float scale = critical ? 1f + Mathf.Sin(k * Mathf.PI) * 0.35f : 1f;
                rt.localScale = Vector3.one * scale;
                t.color = new Color(t.color.r, t.color.g, t.color.b, 1f - k * k);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        void HandleInteractFocus(GameObject target)
        {
            if (interactHint == null) return;
            if (target == null) { interactHint.SetActive(false); return; }
            var ia = target.GetComponent<Interactable>();
            string tip = "按 E 互动";
            if (ia != null)
            {
                if (ia.kind == InteractKind.Cage) tip = "按攻击键打破笼子！";
                else if (ia.kind == InteractKind.Portal) tip = "按 E 进入传送门";
                else tip = "按 E 与 " + ia.speakerName + " 交谈";
            }
            interactText.text = tip;
            interactHint.SetActive(true);
        }

        public void RefreshQuestDisplay()
        {
            var qs = QuestSystem.Instance;
            if (qs == null) return;
            var q = qs.ActiveQuest;
            if (questTitleText == null) return;
            if (q == null)
            {
                questTitleText.text = "全部完成！";
                questProgressText.text = "前往传送门";
                return;
            }
            questTitleText.text = q.title;
            questProgressText.text = $"{q.description}  {q.ProgressText}";
        }

        public void BindSkillBar(PlayerCombat c)
        {
            combat = c;
            if (c == null) return;
            var bar = hudRoot.Find("SkillBar");
            if (bar == null) return;
            foreach (Transform child in bar) Destroy(child.gameObject);
            skillSlots.Clear();

            foreach (var s in c.AllSkills)
            {
                if (s == null) continue;
                var slot = new SkillSlot { skill = s };
                var root = NewPanel("Slot_" + s.id, bar, new Color(0.08f, 0.09f, 0.14f, 0.8f));
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 96);
                var le = root.AddComponent<LayoutElement>();
                le.preferredWidth = 96; le.preferredHeight = 96;

                slot.icon = NewImage("Icon", root.transform, KeyColor(s.id), 1f);
                Stretch(slot.icon.rectTransform, new Vector2(6, 6), new Vector2(-6, -6));

                slot.cooldownMask = NewImage("CD", root.transform, new Color(0f, 0f, 0f, 0.62f), 1f);
                Stretch(slot.cooldownMask.rectTransform, new Vector2(6, 6), new Vector2(-6, -6));
                slot.cooldownMask.type = Image.Type.Filled;
                slot.cooldownMask.fillMethod = Image.FillMethod.Radial360;
                slot.cooldownMask.fillAmount = 0f;

                slot.keyText = NewText("Key", root.transform, KeyLabel(s.key), 26, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0, 22), new Vector2(0, 22), TextAnchor.MiddleCenter);
                slot.keyText.fontStyle = FontStyle.Bold;

                slot.nameText = NewText("Name", root.transform, s.displayName, 17, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(0, -8), new Vector2(0, -8), TextAnchor.MiddleCenter);

                slot.root = root;
                skillSlots.Add(slot);
            }
        }

        Color KeyColor(string id)
        {
            switch (id)
            {
                case "slam": return new Color(0.95f, 0.55f, 0.25f);
                case "breath": return new Color(0.95f, 0.3f, 0.35f);
                case "shift": return new Color(0.45f, 0.75f, 1f);
                case "roll": return new Color(0.5f, 0.9f, 0.6f);
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }

        string KeyLabel(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return "左键";
                case KeyCode.LeftShift: return "Shift";
                default: return key.ToString();
            }
        }

        // ================= UI 构建工具 =================
        RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        GameObject NewPanel(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt.gameObject;
        }

        Image NewImage(string name, Transform parent, Color color, float alpha)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, alpha);
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, Transform parent, string content, int size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Anchor(rt, anchorMin, anchorMax, offsetMin, offsetMax);
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        Image NewBar(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var bg = NewImage(name + "_bg", parent, new Color(0.12f, 0.13f, 0.18f, 0.95f), 1f);
            Anchor(bg.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);

            var fillRect = NewRect(name + "_fill", bg.transform);
            Stretch(fillRect, new Vector2(3, 3), new Vector2(-3, -3));
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void Stretch(RectTransform rt, Vector2? marginMin = null, Vector2? marginMax = null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = marginMin ?? Vector2.zero;
            rt.offsetMax = marginMax ?? Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
