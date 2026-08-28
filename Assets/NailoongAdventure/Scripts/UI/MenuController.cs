using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 主菜单场景控制器：展示旋转的奶龙模型 + 环绕镜头，并弹出主菜单面板。
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("展示")]
        public Transform showModel;
        public float rotateSpeed = 22f;
        public float orbitSpeed = 6f;
        public float orbitRadius = 6f;
        public float bobAmplitude = 0.12f;

        Camera cam;
        float angle;
        Vector3 basePos;

        void Start()
        {
            cam = Camera.main;
            if (showModel == null)
            {
                var found = GameObject.Find("Nailoong_Show");
                if (found != null) showModel = found.transform;
            }
            if (showModel != null) basePos = showModel.position;
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (showModel != null)
            {
                showModel.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
                showModel.position = basePos + Vector3.up * Mathf.Sin(Time.time * 1.6f) * bobAmplitude;
            }

            if (cam != null)
            {
                angle += orbitSpeed * Time.deltaTime;
                var target = showModel != null ? showModel.position : Vector3.zero;
                cam.transform.position = target + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0.35f, Mathf.Cos(angle * Mathf.Deg2Rad)) * orbitRadius;
                cam.transform.LookAt(target + Vector3.up * 1.1f);
            }
        }
    }
}
