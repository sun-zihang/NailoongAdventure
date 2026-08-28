using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nailoong
{
    /// <summary>
    /// UIManager 的面板部分：主菜单、暂停、通关、失败、对话、复活提示。
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        GameObject panelMainMenu, panelPause, panelClear, panelOver, panelDialogue, panelRevive;
        Text dialogueSpeaker, dialogueContent, clearStats, overStats, reviveText, menuProgress;
        float dialogueTimer;

        void BuildPanels()
        {
            EnsureEventSystem();

            // 主菜单
            panelMainMenu = NewPanel("Panel_MainMenu", canvas.transform, new Color(0.04f, 0.05f, 0.09f, 0.9f));
            Stretch(panelMainMenu.GetComponent<RectTransform>());
            var menuTitle = NewText("标题", panelMainMenu.transform, "奶龙冒险", 96, accentColor,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600, -220), new Vector2(600, -110));
            menuTitle.fontStyle = FontStyle.Bold;
            NewText("副标题", panelMainMenu.transform, "Nailoong Adventure · 抢回被暴暴龙夺走的零食", 30, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-700, -300), new Vector2(700, -250));
            menuProgress = NewText("进度", panelMainMenu.transform, "", 24, new Color(0.8f, 0.85f, 1f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-500, 60), new Vector2(500, 100));

            var menuButtons = NewRect("MenuButtons", panelMainMenu.transform);
            Anchor(menuButtons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220, -140), new Vector2(220, 140));
            var vlg = menuButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 22f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            MakeButton("开始新的冒险", menuButtons, () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_ui", 0.8f);
                ShowMainMenu(false);
                if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
            });
            MakeButton("继续冒险", menuButtons, () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_ui", 0.8f);
                ShowMainMenu(false);
                if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
            });
            MakeButton("退出游戏", menuButtons, () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_ui", 0.8f);
                if (GameManager.Instance != null) GameManager.Instance.Quit();
            });
            panelMainMenu.SetActive(false);

            // 暂停
            panelPause = NewPanel("Panel_Pause", canvas.transform, new Color(0.03f, 0.04f, 0.07f, 0.82f));
            Stretch(panelPause.GetComponent<RectTransform>());
            NewText("暂停标题", panelPause.transform, "暂停", 72, accentColor,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-400, -260), new Vector2(400, -160));
            NewText("操作说明", panelPause.transform,
                "WASD 移动 · 空格跳跃（可二段跳）· 鼠标左键攻击 · Shift 咕噜冲撞\nQ 泰山压顶 · F 龙耀吐息 · R 奶龙变色 · E 互动 · 鼠标右键拖动视角 · ESC 暂停",
                26, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-650, -20), new Vector2(650, 90));

            var pauseButtons = NewRect("PauseButtons", panelPause.transform);
            Anchor(pauseButtons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -220), new Vector2(200, 40));
            var pvg = pauseButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            pvg.spacing = 18f; pvg.childAlignment = TextAnchor.MiddleCenter;
            pvg.childControlWidth = false; pvg.childControlHeight = false;

            MakeButton("继续游戏", pauseButtons, () => ShowPause(false));
            MakeButton("重新开始本关", pauseButtons, () =>
            {
                ShowPause(false);
                if (GameManager.Instance != null) GameManager.Instance.ReloadLevel();
            });
            MakeButton("返回主菜单", pauseButtons, () =>
            {
                ShowPause(false);
                if (GameManager.Instance != null) GameManager.Instance.ToMenu();
            });
            panelPause.SetActive(false);

            // 通关
            panelClear = NewPanel("Panel_Clear", canvas.transform, new Color(0.05f, 0.06f, 0.08f, 0.9f));
            Stretch(panelClear.GetComponent<RectTransform>());
            NewText("通关标题", panelClear.transform, "关卡完成！", 82, accentColor,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600, -280), new Vector2(600, -160));
            clearStats = NewText("通关数据", panelClear.transform, "", 30, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500, -60), new Vector2(500, 80));

            var clearButtons = NewRect("ClearButtons", panelClear.transform);
            Anchor(clearButtons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -260), new Vector2(260, -40));
            var hlg = clearButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 26f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;

            MakeButton("下一关", clearButtons, () =>
            {
                panelClear.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.NextLevel();
            });
            MakeButton("重玩本关", clearButtons, () =>
            {
                panelClear.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.ReloadLevel();
            });
            MakeButton("主菜单", clearButtons, () =>
            {
                panelClear.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.ToMenu();
            }, new Color(0.35f, 0.38f, 0.5f));
            panelClear.SetActive(false);

            // 失败
            panelOver = NewPanel("Panel_Over", canvas.transform, new Color(0.12f, 0.04f, 0.05f, 0.9f));
            Stretch(panelOver.GetComponent<RectTransform>());
            NewText("失败标题", panelOver.transform, "奶龙被打败了…", 76, new Color(1f, 0.55f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600, -280), new Vector2(600, -170));
            overStats = NewText("失败数据", panelOver.transform, "再试一次！记得吃布丁回血、攒满火力值放大招。", 30, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600, -60), new Vector2(600, 60));

            var overButtons = NewRect("OverButtons", panelOver.transform);
            Anchor(overButtons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -240), new Vector2(300, -60));
            var ovg = overButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
            ovg.spacing = 26f; ovg.childAlignment = TextAnchor.MiddleCenter;
            ovg.childControlWidth = false; ovg.childControlHeight = false;

            MakeButton("重新开始本关", overButtons, () =>
            {
                ShowGameOver(false);
                if (GameManager.Instance != null) GameManager.Instance.ReloadLevel();
            });
            MakeButton("返回主菜单", overButtons, () =>
            {
                ShowGameOver(false);
                if (GameManager.Instance != null) GameManager.Instance.ToMenu();
            }, new Color(0.35f, 0.38f, 0.5f));
            panelOver.SetActive(false);

            // 对话
            panelDialogue = NewPanel("Panel_Dialogue", canvas.transform, new Color(0.05f, 0.06f, 0.1f, 0.88f));
            Anchor(panelDialogue.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-620, 40), new Vector2(620, 230));
            dialogueSpeaker = NewText("说话者", panelDialogue.transform, "", 34, accentColor,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -18), new Vector2(500, -62), TextAnchor.MiddleLeft);
            dialogueContent = NewText("内容", panelDialogue.transform, "", 28, Color.white,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 18), new Vector2(-30, 130), TextAnchor.UpperLeft);
            dialogueContent.horizontalOverflow = HorizontalWrapMode.Wrap;
            panelDialogue.SetActive(false);

            // 复活提示
            panelRevive = NewPanel("Panel_Revive", canvas.transform, new Color(0.05f, 0.06f, 0.1f, 0.0f));
            Stretch(panelRevive.GetComponent<RectTransform>());
            reviveText = NewText("复活文本", panelRevive.transform, "", 64, new Color(1f, 0.85f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600, -60), new Vector2(600, 60));
            panelRevive.SetActive(false);
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        Button MakeButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick, Color? color = null)
        {
            var rt = NewRect("Btn_" + label, parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 300; le.preferredHeight = 62;
            rt.sizeDelta = new Vector2(300, 62);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = color ?? new Color(0.18f, 0.22f, 0.34f, 0.95f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            var textRt = NewRect("Label", rt);
            Stretch(textRt);
            var t = textRt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = 28;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return btn;
        }

        // ================= 面板显隐 =================
        public void ShowMainMenu(bool show)
        {
            if (panelMainMenu == null) return;
            panelMainMenu.SetActive(show);
            if (!show) return;

            if (hudRoot != null) hudRoot.gameObject.SetActive(false);
            if (GameManager.Instance != null && menuProgress != null)
            {
                int cleared = GameManager.Instance.Save != null ? GameManager.Instance.Save.clearedLevels : 0;
                menuProgress.text = $"已通关 {cleared} / 3 关　·　已解锁技能 {GameManager.Instance.Save.unlockedSkills.Count} 个";
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("bgm_menu", 1.5f);
        }

        public void ShowPause(bool show)
        {
            if (panelPause == null) return;
            panelPause.SetActive(show);
            if (hudRoot != null) hudRoot.gameObject.SetActive(!show);
            if (GameManager.Instance == null) return;

            if (show)
            {
                GameManager.Instance.Pause();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                GameManager.Instance.Resume();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ShowLevelClear(int levelIndex, float time)
        {
            if (panelClear == null) return;
            panelClear.SetActive(true);
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            float best = GameManager.Instance != null && GameManager.Instance.Save.bestTime.TryGetValue(levelIndex, out var b) ? b : -1f;
            string bestText = best > 0f ? $"最佳纪录：{Mathf.FloorToInt(best / 60f):00}:{Mathf.FloorToInt(best % 60f):00}" : "首次通关！";
            clearStats.text = $"第 {levelIndex + 1} 关　用时 {minutes:00}:{seconds:00}\n{bestText}";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_levelclear", 1f);
        }

        public void ShowGameOver(bool show)
        {
            if (panelOver == null) return;
            panelOver.SetActive(show);
            if (hudRoot != null) hudRoot.gameObject.SetActive(!show);
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = show;
        }

        public void ShowDialogue(string speaker, string content)
        {
            if (panelDialogue == null) return;
            panelDialogue.SetActive(true);
            dialogueSpeaker.text = speaker;
            dialogueContent.text = content;
            dialogueTimer = 5f;
            CancelInvoke(nameof(HideDialogue));
            Invoke(nameof(HideDialogue), 5f);
        }

        void HideDialogue()
        {
            if (panelDialogue != null) panelDialogue.SetActive(false);
        }

        public void ShowRevive(int used, int max)
        {
            if (panelRevive == null) return;
            panelRevive.SetActive(true);
            reviveText.text = $"奶龙重整旗鼓！\n剩余复活次数 {max - used}";
            CancelInvoke(nameof(HideRevive));
            Invoke(nameof(HideRevive), 1.6f);
        }

        void HideRevive()
        {
            if (panelRevive != null) panelRevive.SetActive(false);
        }
    }
}
