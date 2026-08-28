using System.Collections.Generic;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 道具与场景装饰：零食、笼子、传送门、甜点树、蘑菇、岩石、云朵。
    /// </summary>
    public static class PropFactory
    {
        // ---------- 零食（收集物） ----------
        public static GameObject BuildSnack(Material material, int variant = 0)
        {
            var mb = new MeshBuilder();
            switch (variant % 3)
            {
                case 0: // 布丁
                    var pudding = new Color32(255, 208, 96, 255);
                    var caramel = new Color32(196, 128, 48, 255);
                    mb.AddEllipsoid(new Vector3(0f, 0.10f, 0f), new Vector3(0.26f, 0.10f, 0.26f), 0, pudding, 12, 8, Quaternion.identity, 0.72f);
                    mb.AddEllipsoid(new Vector3(0f, 0.22f, 0f), new Vector3(0.10f, 0.05f, 0.10f), 0, caramel, 8, 6);
                    break;
                case 1: // 草莓蛋糕
                    var cream = new Color32(255, 246, 226, 255);
                    var sponge = new Color32(236, 190, 120, 255);
                    var berry = new Color32(240, 70, 90, 255);
                    mb.AddEllipsoid(new Vector3(0f, 0.09f, 0f), new Vector3(0.26f, 0.09f, 0.26f), 0, sponge, 12, 8);
                    mb.AddEllipsoid(new Vector3(0f, 0.15f, 0f), new Vector3(0.24f, 0.07f, 0.24f), 0, cream, 12, 8);
                    mb.AddEllipsoid(new Vector3(0f, 0.28f, 0f), new Vector3(0.09f, 0.09f, 0.09f), 0, berry, 10, 8);
                    mb.AddEllipsoid(new Vector3(0f, 0.36f, 0f), new Vector3(0.03f, 0.05f, 0.03f), 0, new Color32(80, 180, 90, 255), 6, 5);
                    break;
                default: // 甜甜圈
                    var dough = new Color32(226, 160, 92, 255);
                    var icing = new Color32(255, 140, 180, 255);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        Vector3 p = new Vector3(Mathf.Cos(a) * 0.18f, 0.16f, Mathf.Sin(a) * 0.18f);
                        mb.AddEllipsoid(p, new Vector3(0.09f, 0.07f, 0.09f), 0, dough, 8, 6);
                        mb.AddEllipsoid(p + Vector3.up * 0.05f, new Vector3(0.085f, 0.045f, 0.085f), 0, icing, 8, 6);
                    }
                    break;
            }

            var go = new GameObject("Pickup_Snack");
            Attach(go, mb.ToMesh("SnackMesh"), material);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.4f;
            col.isTrigger = true;
            return go;
        }

        // ---------- 笼子 ----------
        public static GameObject BuildCage(Material material)
        {
            var metal = new Color32(150, 158, 175, 255);
            var dark = new Color32(90, 96, 112, 255);
            var mb = new MeshBuilder();

            // 底盘与顶盖
            mb.AddEllipsoid(new Vector3(0f, 0.06f, 0f), new Vector3(0.62f, 0.06f, 0.62f), 0, dark, 14, 8);
            mb.AddEllipsoid(new Vector3(0f, 1.24f, 0f), new Vector3(0.60f, 0.05f, 0.60f), 0, dark, 14, 8);
            // 竖栏
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.55f, 0f, Mathf.Sin(a) * 0.55f);
                mb.AddTube(new List<Vector3> { p + Vector3.up * 0.05f, p + Vector3.up * 1.25f },
                    new List<float> { 0.035f, 0.035f }, new List<int> { 0, 0 }, metal, 5);
            }
            // 横向加固
            mb.AddEllipsoid(new Vector3(0f, 0.65f, 0f), new Vector3(0.585f, 0.035f, 0.585f), 0, metal, 14, 6);
            // 顶环
            mb.AddEllipsoid(new Vector3(0f, 1.34f, 0f), new Vector3(0.10f, 0.06f, 0.10f), 0, metal, 10, 6);

            var go = new GameObject("Cage");
            Attach(go, mb.ToMesh("CageMesh"), material);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.35f, 1.2f);
            col.center = new Vector3(0f, 0.68f, 0f);
            return go;
        }

        // ---------- 传送门 ----------
        public static GameObject BuildPortal(Material material)
        {
            var frame = new Color32(255, 214, 90, 255);
            var glow = new Color32(120, 220, 255, 255);
            var mb = new MeshBuilder();

            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 2.2f + 2.2f, 0f);
                mb.AddEllipsoid(p, new Vector3(0.16f, 0.16f, 0.16f), 0, frame, 8, 6);
            }
            // 门内旋涡（用几层半透明感的色环表现）
            mb.AddEllipsoid(new Vector3(0f, 2.2f, -0.05f), new Vector3(1.32f, 1.95f, 0.06f), 0, glow, 20, 12);
            mb.AddEllipsoid(new Vector3(0f, 2.2f, 0.02f), new Vector3(0.92f, 1.36f, 0.05f), 0, new Color32(200, 245, 255, 255), 18, 12);
            mb.AddEllipsoid(new Vector3(0f, 2.2f, 0.06f), new Vector3(0.52f, 0.78f, 0.05f), 0, new Color32(255, 255, 255, 255), 16, 10);

            var go = new GameObject("Portal");
            Attach(go, mb.ToMesh("PortalMesh"), material);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 1.6f;
            col.center = new Vector3(0f, 2.2f, 0f);
            return go;
        }

        // ---------- 甜点树 ----------
        public static GameObject BuildTree(Material material, Color32 trunk, Color32 leaf, float scale = 1f, int style = 0)
        {
            var mb = new MeshBuilder();
            float h = 2.2f * scale;

            mb.AddTube(new List<Vector3>
                {
                    new Vector3(0f, 0f, 0f), new Vector3(0f, h * 0.4f, 0f), new Vector3(0f, h * 0.8f, 0f)
                },
                new List<float> { 0.20f * scale, 0.15f * scale, 0.11f * scale }, new List<int> { 0, 0, 0 }, trunk, 8);

            if (style == 0)
            {
                // 层叠奶油树冠
                for (int i = 0; i < 3; i++)
                {
                    float t = i / 2f;
                    mb.AddEllipsoid(new Vector3(0f, h * (0.85f + t * 0.32f), 0f),
                        new Vector3((0.85f - t * 0.26f) * scale, (0.45f - t * 0.07f) * scale, (0.85f - t * 0.26f) * scale), 0, leaf, 14, 10);
                }
            }
            else if (style == 1)
            {
                // 棒棒糖树
                mb.AddEllipsoid(new Vector3(0f, h * 1.1f, 0f), new Vector3(0.75f * scale, 0.75f * scale, 0.75f * scale), 0, leaf, 14, 10);
                mb.AddEllipsoid(new Vector3(0.25f * scale, h * 1.25f, 0.2f * scale), new Vector3(0.16f * scale, 0.16f * scale, 0.16f * scale), 0,
                    new Color32(255, 255, 255, 255), 8, 6);
            }
            else
            {
                // 蘑菇伞
                mb.AddEllipsoid(new Vector3(0f, h * 1.05f, 0f), new Vector3(0.35f * scale, 0.55f * scale, 0.35f * scale), 0, new Color32(255, 248, 226, 255), 12, 9);
                mb.AddEllipsoid(new Vector3(0f, h * 1.28f, 0f), new Vector3(0.95f * scale, 0.42f * scale, 0.95f * scale), 0, leaf, 16, 10, Quaternion.identity, 0.25f);
            }

            var go = new GameObject("Tree");
            Attach(go, mb.ToMesh("TreeMesh"), material);
            var col = go.AddComponent<CapsuleCollider>();
            col.height = h * 2f;
            col.radius = 0.35f * scale;
            col.center = new Vector3(0f, h, 0f);
            return go;
        }

        // ---------- 岩石 ----------
        public static GameObject BuildRock(Material material, Color32 color, float scale = 1f)
        {
            var mb = new MeshBuilder();
            mb.AddEllipsoid(new Vector3(0f, 0.35f * scale, 0f), new Vector3(0.7f * scale, 0.42f * scale, 0.62f * scale), 0, color, 10, 8);
            mb.AddEllipsoid(new Vector3(0.32f * scale, 0.22f * scale, 0.18f * scale), new Vector3(0.34f * scale, 0.26f * scale, 0.3f * scale), 0, color, 8, 6);
            mb.AddEllipsoid(new Vector3(-0.28f * scale, 0.16f * scale, -0.16f * scale), new Vector3(0.3f * scale, 0.2f * scale, 0.26f * scale), 0, color, 8, 6);

            var go = new GameObject("Rock");
            Attach(go, mb.ToMesh("RockMesh"), material);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f * scale, 0.8f * scale, 1.3f * scale);
            col.center = new Vector3(0f, 0.4f * scale, 0f);
            return go;
        }

        // ---------- 云 ----------
        public static GameObject BuildCloud(Material material, float scale = 1f)
        {
            var white = new Color32(255, 255, 255, 255);
            var shade = new Color32(228, 236, 255, 255);
            var mb = new MeshBuilder();
            mb.AddEllipsoid(new Vector3(0f, 0f, 0f), new Vector3(2.2f * scale, 0.9f * scale, 1.6f * scale), 0, white, 12, 8);
            mb.AddEllipsoid(new Vector3(-1.3f * scale, 0.25f * scale, 0f), new Vector3(1.1f * scale, 0.75f * scale, 1.0f * scale), 0, white, 10, 8);
            mb.AddEllipsoid(new Vector3(1.4f * scale, 0.15f * scale, 0.2f * scale), new Vector3(1.2f * scale, 0.68f * scale, 1.0f * scale), 0, shade, 10, 8);
            mb.AddEllipsoid(new Vector3(0.4f * scale, 0.55f * scale, -0.2f * scale), new Vector3(1.0f * scale, 0.7f * scale, 0.9f * scale), 0, white, 10, 8);

            var go = new GameObject("Cloud");
            Attach(go, mb.ToMesh("CloudMesh"), material);
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ---------- 工具 ----------
        static void Attach(GameObject go, Mesh mesh, Material material)
        {
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }
    }
}
