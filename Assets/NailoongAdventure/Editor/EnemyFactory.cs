using System.Collections.Generic;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 敌人建模：布丁怪（果冻质感、可挤压）、炸鸡鸟（扇翅飞行）、暴暴龙（Boss）、小鸡（被救出的伙伴）。
    /// 全部为程序化网格，按"脚底 y=0"建模，Body 子节点由代码做挤压拉伸与扇翅动画。
    /// </summary>
    public static class EnemyFactory
    {
        public static GameObject BuildPudding(Material material, Color32 tint)
        {
            var mb = new MeshBuilder();
            var dark = new Color32(48, 40, 36, 255);
            var white = new Color32(255, 255, 255, 255);
            var highlight = new Color32(255, 255, 255, 90);

            mb.AddBlob(new Vector3(0f, 0.42f, 0f), new Vector3(0.44f, 0.42f, 0.44f), 0, tint, 16, 12, 0.82f);
            mb.AddEllipsoid(new Vector3(0f, 0.80f, 0f), new Vector3(0.13f, 0.16f, 0.13f), 0, tint, 10, 8);       // 顶部小尖
            mb.AddEllipsoid(new Vector3(0f, 0.92f, 0f), new Vector3(0.06f, 0.08f, 0.06f), 0, tint, 8, 6);
            mb.AddEllipsoid(new Vector3(-0.16f, 0.50f, 0.34f), new Vector3(0.10f, 0.12f, 0.06f), 0, white, 10, 8);
            mb.AddEllipsoid(new Vector3(0.16f, 0.50f, 0.34f), new Vector3(0.10f, 0.12f, 0.06f), 0, white, 10, 8);
            mb.AddEllipsoid(new Vector3(-0.16f, 0.50f, 0.39f), new Vector3(0.05f, 0.06f, 0.04f), 0, dark, 8, 6);
            mb.AddEllipsoid(new Vector3(0.16f, 0.50f, 0.39f), new Vector3(0.05f, 0.06f, 0.04f), 0, dark, 8, 6);
            mb.AddEllipsoid(new Vector3(0f, 0.30f, 0.40f), new Vector3(0.09f, 0.05f, 0.04f), 0, dark, 8, 6);      // 嘴
            mb.AddEllipsoid(new Vector3(-0.26f, 0.34f, 0.30f), new Vector3(0.07f, 0.045f, 0.03f), 0, new Color32(255, 150, 160, 255), 8, 6);
            mb.AddEllipsoid(new Vector3(0.26f, 0.34f, 0.30f), new Vector3(0.07f, 0.045f, 0.03f), 0, new Color32(255, 150, 160, 255), 8, 6);
            mb.AddEllipsoid(new Vector3(-0.16f, 0.68f, 0.30f), new Vector3(0.12f, 0.06f, 0.02f), 0, highlight, 8, 6); // 高光

            return Wrap("Enemy_Pudding", mb.ToMesh("PuddingMesh"), material, 0.9f, 0.44f, tint);
        }

        public static GameObject BuildBird(Material material)
        {
            var body = new Color32(232, 163, 61, 255);
            var wing = new Color32(210, 140, 48, 255);
            var dark = new Color32(48, 40, 36, 255);
            var white = new Color32(255, 255, 255, 255);
            var beak = new Color32(255, 150, 60, 255);

            var mb = new MeshBuilder();
            mb.AddEllipsoid(new Vector3(0f, 0.72f, 0f), new Vector3(0.34f, 0.30f, 0.42f), 0, body, 14, 10);
            mb.AddEllipsoid(new Vector3(0f, 1.06f, 0.22f), new Vector3(0.24f, 0.23f, 0.23f), 0, body, 12, 9);
            mb.AddCone(new Vector3(0f, 1.02f, 0.42f), new Vector3(0f, 1.00f, 0.66f), 0.09f, 0, beak, 8);
            mb.AddEllipsoid(new Vector3(-0.12f, 1.12f, 0.38f), new Vector3(0.07f, 0.08f, 0.05f), 0, white, 8, 6);
            mb.AddEllipsoid(new Vector3(0.12f, 1.12f, 0.38f), new Vector3(0.07f, 0.08f, 0.05f), 0, white, 8, 6);
            mb.AddEllipsoid(new Vector3(-0.12f, 1.12f, 0.42f), new Vector3(0.035f, 0.045f, 0.03f), 0, dark, 6, 5);
            mb.AddEllipsoid(new Vector3(0.12f, 1.12f, 0.42f), new Vector3(0.035f, 0.045f, 0.03f), 0, dark, 6, 5);
            mb.AddEllipsoid(new Vector3(0f, 1.28f, 0.16f), new Vector3(0.05f, 0.10f, 0.05f), 0, new Color32(240, 80, 70, 255), 8, 6); // 鸡冠
            mb.AddTube(new List<Vector3> { new Vector3(-0.14f, 0.5f, 0.02f), new Vector3(-0.14f, 0.28f, 0.0f), new Vector3(-0.14f, 0.06f, 0.02f) },
                new List<float> { 0.06f, 0.05f, 0.055f }, new List<int> { 0, 0, 0 }, beak, 6);
            mb.AddTube(new List<Vector3> { new Vector3(0.14f, 0.5f, 0.02f), new Vector3(0.14f, 0.28f, 0.0f), new Vector3(0.14f, 0.06f, 0.02f) },
                new List<float> { 0.06f, 0.05f, 0.055f }, new List<int> { 0, 0, 0 }, beak, 6);
            mb.AddEllipsoid(new Vector3(-0.14f, 0.05f, 0.10f), new Vector3(0.09f, 0.035f, 0.13f), 0, beak, 8, 6);
            mb.AddEllipsoid(new Vector3(0.14f, 0.05f, 0.10f), new Vector3(0.09f, 0.035f, 0.13f), 0, beak, 8, 6);

            var root = Wrap("Enemy_Bird", mb.ToMesh("BirdMesh"), material, 1.35f, 0.36f, body);

            // 翅膀放在根节点下（EnemyController 通过名字查找并扇动）
            var wingMbL = new MeshBuilder();
            wingMbL.AddEllipsoid(new Vector3(0f, 0f, 0f), new Vector3(0.30f, 0.06f, 0.22f), 0, wing, 10, 6);
            wingMbL.AddCone(new Vector3(-0.26f, 0f, 0.06f), new Vector3(-0.46f, 0f, 0.14f), 0.05f, 0, wing, 6);
            var wl = new GameObject("Wing_L");
            wl.transform.SetParent(root.transform, false);
            wl.transform.localPosition = new Vector3(-0.30f, 0.74f, 0f);
            AddMesh(wl, wingMbL.ToMesh("WingLMesh"), material);

            var wingMbR = new MeshBuilder();
            wingMbR.AddEllipsoid(new Vector3(0f, 0f, 0f), new Vector3(0.30f, 0.06f, 0.22f), 0, wing, 10, 6);
            wingMbR.AddCone(new Vector3(0.26f, 0f, 0.06f), new Vector3(0.46f, 0f, 0.14f), 0.05f, 0, wing, 6);
            var wr = new GameObject("Wing_R");
            wr.transform.SetParent(root.transform, false);
            wr.transform.localPosition = new Vector3(0.30f, 0.74f, 0f);
            AddMesh(wr, wingMbR.ToMesh("WingRMesh"), material);

            return root;
        }

        public static GameObject BuildBoss(Material material)
        {
            var skin = new Color32(108, 76, 168, 255);
            var belly = new Color32(245, 197, 66, 255);
            var horn = new Color32(255, 240, 210, 255);
            var dark = new Color32(30, 24, 30, 255);
            var eye = new Color32(255, 90, 60, 255);
            var membrane = new Color32(150, 100, 220, 255);

            var mb = new MeshBuilder();
            // 躯干
            mb.AddEllipsoid(new Vector3(0f, 1.55f, 0f), new Vector3(0.60f, 0.66f, 0.72f), 0, skin, 16, 12);
            mb.AddEllipsoid(new Vector3(0f, 1.35f, 0.42f), new Vector3(0.42f, 0.48f, 0.30f), 0, belly, 14, 10);
            // 脖子与头
            mb.AddTube(new List<Vector3> { new Vector3(0f, 1.95f, 0.10f), new Vector3(0f, 2.20f, 0.34f) },
                new List<float> { 0.30f, 0.26f }, new List<int> { 0, 0 }, skin, 10);
            mb.AddEllipsoid(new Vector3(0f, 2.42f, 0.52f), new Vector3(0.42f, 0.38f, 0.54f), 0, skin, 14, 10);
            mb.AddEllipsoid(new Vector3(0f, 2.30f, 0.86f), new Vector3(0.24f, 0.19f, 0.28f), 0, skin, 12, 8);
            // 獠牙与口腔
            mb.AddEllipsoid(new Vector3(0f, 2.16f, 0.86f), new Vector3(0.20f, 0.08f, 0.26f), 0, dark, 12, 8);
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                mb.AddCone(new Vector3(sx * 0.13f, 2.22f, 0.98f), new Vector3(sx * 0.13f, 2.02f, 0.96f), 0.045f, 0, horn, 6);
            }
            // 眼
            mb.AddEllipsoid(new Vector3(-0.24f, 2.52f, 0.82f), new Vector3(0.11f, 0.10f, 0.06f), 0, eye, 10, 8);
            mb.AddEllipsoid(new Vector3(0.24f, 2.52f, 0.82f), new Vector3(0.11f, 0.10f, 0.06f), 0, eye, 10, 8);
            mb.AddEllipsoid(new Vector3(-0.24f, 2.52f, 0.87f), new Vector3(0.04f, 0.055f, 0.03f), 0, dark, 8, 6);
            mb.AddEllipsoid(new Vector3(0.24f, 2.52f, 0.87f), new Vector3(0.04f, 0.055f, 0.03f), 0, dark, 8, 6);
            // 大角
            mb.AddCone(new Vector3(-0.22f, 2.72f, 0.36f), new Vector3(-0.42f, 3.18f, 0.02f), 0.11f, 0, horn, 8);
            mb.AddCone(new Vector3(0.22f, 2.72f, 0.36f), new Vector3(0.42f, 3.18f, 0.02f), 0.11f, 0, horn, 8);
            // 背刺
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                Vector3 p = new Vector3(0f, 2.05f - t * 0.85f, -0.10f - t * 0.55f);
                mb.AddCone(p, p + new Vector3(0f, 0.30f - t * 0.12f, -0.12f), 0.075f - t * 0.03f, 0, horn, 6);
            }
            // 腿
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                mb.AddTube(new List<Vector3> { new Vector3(sx * 0.34f, 1.15f, 0.05f), new Vector3(sx * 0.40f, 0.60f, 0.02f), new Vector3(sx * 0.38f, 0.18f, 0.10f) },
                    new List<float> { 0.24f, 0.19f, 0.17f }, new List<int> { 0, 0, 0 }, skin, 10);
                mb.AddEllipsoid(new Vector3(sx * 0.38f, 0.10f, 0.22f), new Vector3(0.22f, 0.10f, 0.30f), 0, belly, 10, 8);
                for (int c = 0; c < 3; c++)
                    mb.AddCone(new Vector3(sx * (0.38f + (c - 1) * 0.13f), 0.09f, 0.48f), new Vector3(sx * (0.38f + (c - 1) * 0.13f), 0.06f, 0.64f), 0.05f, 0, horn, 6);
            }
            // 手臂与爪
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                mb.AddTube(new List<Vector3> { new Vector3(sx * 0.56f, 1.85f, 0.10f), new Vector3(sx * 0.72f, 1.45f, 0.26f), new Vector3(sx * 0.74f, 1.10f, 0.44f) },
                    new List<float> { 0.17f, 0.14f, 0.12f }, new List<int> { 0, 0, 0 }, skin, 8);
                mb.AddEllipsoid(new Vector3(sx * 0.74f, 1.04f, 0.52f), new Vector3(0.14f, 0.12f, 0.16f), 0, belly, 8, 6);
                for (int c = 0; c < 3; c++)
                    mb.AddCone(new Vector3(sx * (0.74f + (c - 1) * 0.09f), 1.06f, 0.62f), new Vector3(sx * (0.74f + (c - 1) * 0.09f), 1.02f, 0.78f), 0.04f, 0, horn, 5);
            }

            var root = Wrap("Boss_Baobaolong", mb.ToMesh("BossBodyMesh"), material, 3.1f, 0.62f, skin);

            // 尾巴
            var tail = new GameObject("Tail1");
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(0f, 1.35f, -0.6f);
            var tm = new MeshBuilder();
            tm.AddTube(new List<Vector3>
                {
                    new Vector3(0f, 0f, 0f), new Vector3(0f, -0.05f, -0.55f),
                    new Vector3(0f, -0.12f, -1.05f), new Vector3(0f, -0.22f, -1.55f), new Vector3(0f, -0.30f, -1.95f)
                },
                new List<float> { 0.26f, 0.20f, 0.14f, 0.09f, 0.04f }, new List<int> { 0, 0, 0, 0, 0 }, skin, 10);
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                Vector3 p = new Vector3(0f, -0.05f - t * 0.25f, -0.55f - t * 1.15f);
                tm.AddCone(p, p + new Vector3(0f, 0.18f, -0.06f), 0.06f, 0, horn, 6);
            }
            AddMesh(tail, tm.ToMesh("BossTailMesh"), material);

            // 翅膀
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                var wing = new GameObject(i == 0 ? "Wing_L" : "Wing_R");
                wing.transform.SetParent(root.transform, false);
                wing.transform.localPosition = new Vector3(sx * 0.5f, 1.95f, -0.25f);
                var wm = new MeshBuilder();
                wm.AddQuadTwoSided(new Vector3(0f, 0f, 0.1f), new Vector3(sx * 0.7f, 0.35f, -0.2f),
                    new Vector3(sx * 1.25f, 0.05f, -0.6f), new Vector3(0f, -0.35f, -0.2f), 0, membrane);
                wm.AddCone(new Vector3(0f, 0f, 0.05f), new Vector3(sx * 1.2f, 0.2f, -0.55f), 0.06f, 0, horn, 6);
                AddMesh(wing, wm.ToMesh("BossWingMesh"), material);
            }

            var mouth = new GameObject("MouthPoint");
            mouth.transform.SetParent(root.transform, false);
            mouth.transform.localPosition = new Vector3(0f, 2.28f, 1.05f);

            return root;
        }

        public static GameObject BuildChick(Material material)
        {
            var body = new Color32(255, 226, 92, 255);
            var beak = new Color32(255, 152, 60, 255);
            var dark = new Color32(48, 40, 36, 255);
            var mb = new MeshBuilder();
            mb.AddEllipsoid(new Vector3(0f, 0.28f, 0f), new Vector3(0.26f, 0.26f, 0.26f), 0, body, 12, 9);
            mb.AddEllipsoid(new Vector3(0f, 0.56f, 0.04f), new Vector3(0.20f, 0.19f, 0.19f), 0, body, 12, 9);
            mb.AddCone(new Vector3(0f, 0.54f, 0.20f), new Vector3(0f, 0.52f, 0.32f), 0.055f, 0, beak, 6);
            mb.AddEllipsoid(new Vector3(-0.08f, 0.60f, 0.16f), new Vector3(0.045f, 0.05f, 0.03f), 0, dark, 6, 5);
            mb.AddEllipsoid(new Vector3(0.08f, 0.60f, 0.16f), new Vector3(0.045f, 0.05f, 0.03f), 0, dark, 6, 5);
            mb.AddEllipsoid(new Vector3(0f, 0.72f, 0.02f), new Vector3(0.04f, 0.07f, 0.04f), 0, beak, 6, 5);
            mb.AddEllipsoid(new Vector3(-0.24f, 0.30f, 0.02f), new Vector3(0.06f, 0.10f, 0.03f), 0, body, 6, 5);
            mb.AddEllipsoid(new Vector3(0.24f, 0.30f, 0.02f), new Vector3(0.06f, 0.10f, 0.03f), 0, body, 6, 5);
            return Wrap("Chick", mb.ToMesh("ChickMesh"), material, 0.8f, 0.26f, body);
        }

        // ---------- 工具 ----------
        static GameObject Wrap(string name, Mesh mesh, Material material, float height, float radius, Color32 tint)
        {
            var root = new GameObject(name);
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            AddMesh(body, mesh, material);

            var cap = root.AddComponent<CapsuleCollider>();
            cap.height = height;
            cap.radius = radius;
            cap.center = new Vector3(0f, height * 0.5f, 0f);

            return root;
        }

        static void AddMesh(GameObject go, Mesh mesh, Material material)
        {
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }
    }
}
