using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nailoong
{
    /// <summary>
    /// 移动端触控：虚拟摇杆（左下）+ 跳跃/冲刺按钮（右下）。
    /// 仅在支持触摸的设备上由 UIManager.EnsureTouchControls() 构建；
    /// 桌面键鼠不受影响。全部运行时 uGUI，零资源依赖。
    /// </summary>
    public static class TouchControls
    {
        public static bool TouchDevice =>
            Application.isMobilePlatform || (Input.touchSupported && SystemInfo.deviceType != DeviceType.Desktop);

        public static void Ensure(Transform canvasRoot, PlayerController pc, PlayerCombat combat)
        {
            if (!TouchDevice || canvasRoot == null || pc == null) return;
            if (canvasRoot.Find("TouchControls") != null) return;

            EnsureEventSystem();

            var rootGo = new GameObject("TouchControls");
            rootGo.transform.SetParent(canvasRoot, false);
            var root = rootGo.AddComponent<RectTransform>();
            Stretch(root);

            // ---------- 虚拟摇杆（左下） ----------
            var joyBase = NewCircle("Joystick", rootGo.transform, new Color(1f, 1f, 1f, 0.16f), 260f);
            AnchorBottom(joyBase, new Vector2(40f, 40f));
            var knob = NewCircle("Knob", joyBase.transform, new Color(1f, 1f, 1f, 0.42f), 110f);
            // knob 不拦截射线，否则按在 knob 上时拖拽事件到不了摇杆基座
            knob.GetComponent<Image>().raycastTarget = false;
            var stick = joyBase.gameObject.AddComponent<TouchJoystick>();
            stick.knob = knob;
            stick.radius = 65f;
            stick.output = pc;

            // ---------- 跳跃按钮（右下） ----------
            var jumpBtn = NewButton("BtnJump", rootGo.transform, new Color(0.35f, 0.75f, 1f, 0.35f), 150f, "跳");
            AnchorBottom(jumpBtn.transform, new Vector2(-70f, 60f));
            jumpBtn.onClick.AddListener(pc.QueueTouchJump);

            // ---------- 冲刺按钮（跳跃上方） ----------
            var dashBtn = NewButton("BtnDash", rootGo.transform, new Color(0.5f, 0.9f, 0.6f, 0.35f), 120f, "滚");
            AnchorBottom(dashBtn.transform, new Vector2(-210f, 40f));
            dashBtn.onClick.AddListener(pc.TouchDash);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(es);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AnchorBottom(Transform t, Vector2 pos)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
        }

        static RectTransform NewCircle(string name, Transform parent, Color color, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        static Button NewButton(string name, Transform parent, Color color, float size, string label)
        {
            var rt = NewCircle(name, parent, color, size);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(rt, false);
            var t = txtGo.AddComponent<Text>();
            // 与 UIManager 相同的字体来源（工程内 simhei → 内置回退），WebGL 下 OS 字体不可用
            var font = Resources.Load<Font>("Fonts/simhei");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = font;
            t.text = label;
            t.fontSize = 34;
            t.color = new Color(1f, 1f, 1f, 0.9f);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            return btn;
        }
    }

    /// <summary>虚拟摇杆拖拽逻辑：输出 -1~1 方向到 PlayerController。</summary>
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public float radius = 65f;
        public PlayerController output;

        Vector2 dir;

        public void OnPointerDown(PointerEventData e) => Drag(e);
        public void OnDrag(PointerEventData e) => Drag(e);
        public void OnPointerUp(PointerEventData e)
        {
            dir = Vector2.zero;
            if (output != null) output.SetTouchMove(Vector2.zero);
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        void Drag(PointerEventData e)
        {
            if (knob == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, e.position, e.pressEventCamera, out local);
            dir = local.sqrMagnitude > 0.001f ? local.normalized * Mathf.Clamp01(local.magnitude / radius) : Vector2.zero;
            knob.anchoredPosition = dir * radius;
            if (output != null) output.SetTouchMove(dir);
        }
    }
}
