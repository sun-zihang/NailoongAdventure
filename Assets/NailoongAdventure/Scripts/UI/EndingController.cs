using UnityEngine;
using UnityEngine.UI;

namespace Nailoong
{
    /// <summary>
    /// 大结局场景：胜利演出 + 甜品图鉴统计 + 任意输入返回主菜单。
    /// 由 SceneBuilder 在 Ending 场景中挂载，UI 运行时构建（零资源依赖）。
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        float timer;
        Text statsText;
        bool leaving;

        void Start()
        {
            timer = 0f;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("bgm_victory", 0.8f);

            // ---------- 运行时 uGUI ----------
            var canvasGo = new GameObject("EndingCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.Load<Font>("Fonts/simhei");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 40);

            Text MakeText(string name, string content, int size, Color color, float y, FontStyle style = FontStyle.Normal)
            {
                var go = new GameObject(name);
                go.transform.SetParent(canvasGo.transform, false);
                var t = go.AddComponent<Text>();
                t.font = font;
                t.text = content;
                t.fontSize = size;
                t.color = color;
                t.alignment = TextAnchor.MiddleCenter;
                t.fontStyle = style;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
                outline.effectDistance = new Vector2(2f, -2f);
                var rt = t.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(40f, y);
                rt.offsetMax = new Vector2(-40f, y + 110f);
                return t;
            }

            MakeText("Title", "🎉 大 结 局 🎉", 72, new Color(1f, 0.85f, 0.3f), 160f, FontStyle.Bold);
            MakeText("Story", "奶龙找回了被暴暴龙抢走的全部零食，\n和小七一起开了一场奶黄派对！", 34, Color.white, 30f);
            statsText = MakeText("Stats", "", 30, new Color(1f, 0.95f, 0.75f), -70f);
            MakeText("Hint", "点击任意处返回主菜单", 26, new Color(1f, 1f, 1f, 0.75f), -180f);

            RefreshStats();
        }

        void RefreshStats()
        {
            if (statsText == null || GameManager.Instance == null) return;
            int total = GameManager.Instance.Save.collectedItems.Count;
            int stars = 0;
            for (int i = 0; i < 3; i++) stars += GameManager.Instance.GetStars(i);
            statsText.text = $"甜品图鉴：{total} 种　　累计星数：{stars} / 9";
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (leaving || timer < 2f) return;

            // 周期性庆祝星光
            if (VFXManager.Instance != null && Random.value < 0.03f)
            {
                var cam = Camera.main;
                Vector3 basePos = cam != null ? cam.transform.position + cam.transform.forward * 6f : Vector3.one * 3f;
                VFXManager.Instance.Play("vfx_pickup",
                    basePos + new Vector3(Random.Range(-4f, 4f), Random.Range(-1f, 3f), Random.Range(-4f, 4f)),
                    Quaternion.identity, 1.4f);
            }

            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                leaving = true;
                if (GameManager.Instance != null) GameManager.Instance.ToMenu();
            }
        }
    }
}
