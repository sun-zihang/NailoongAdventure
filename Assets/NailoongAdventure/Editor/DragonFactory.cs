using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 奶龙角色建模：程序化生成骨骼层级 + 蒙皮网格 + 眼睛/嘴部挂点。
    ///
    /// 严格对齐官方设定（第七印象 · 奶龙 Nailoong）：
    ///   · 奶黄色皮肤、圆圆的大脑袋、憨态可掬
    ///   · 标志性 "duang~duang" Q 弹大肚腩 —— 由独立 Belly 骨骼驱动弹性抖动
    ///   · 没有耳朵（去除侧鳍）
    ///   · 没有翅膀、没有背鳍
    ///   · 大而有神的圆眼睛、小爪爪、短尾巴
    /// </summary>
    public static class DragonFactory
    {
        // 骨骼顺序必须与 DragonAnimator.BoneNames 完全一致
        public static readonly string[] BoneNames =
        {
            "Hips","Spine","Chest","Belly","Neck","Head","Jaw",
            "Tail1","Tail2","Tail3",
            "ArmL","ArmR","HandL","HandR",
            "LegL","LegR","FootL","FootR"
        };

        // 调色板（官方奶黄 + 奶白肚皮）
        static readonly Color32 Skin = new Color32(255, 214, 70, 255);      // 奶黄
        static readonly Color32 Belly = new Color32(255, 247, 222, 255);    // 奶白肚皮
        static readonly Color32 Claw = new Color32(255, 251, 238, 255);     // 角与爪（奶油白）
        static readonly Color32 Dark = new Color32(46, 38, 34, 255);        // 眼睛/眉
        static readonly Color32 Blush = new Color32(255, 148, 158, 255);    // 腮红
        static readonly Color32 Mouth = new Color32(122, 58, 62, 255);      // 口腔
        static readonly Color32 Tongue = new Color32(240, 132, 140, 255);   // 舌头

        /// <summary>生成完整的奶龙 GameObject（含 SkinnedMeshRenderer 与骨骼）。</summary>
        public static GameObject Build(string objectName, Material material)
        {
            var root = new GameObject(objectName);

            // ---------- 骨骼 ----------
            var hips = Bone(root.transform, "Hips", new Vector3(0f, 0.58f, 0f));
            var spine = Bone(hips, "Spine", new Vector3(0f, 0.16f, 0f));
            var chest = Bone(spine, "Chest", new Vector3(0f, 0.18f, 0f));
            // 独立肚皮骨：用于 duang~duang 弹性抖动（奶龙灵魂）
            var belly = Bone(spine, "Belly", new Vector3(0f, -0.11f, 0.17f));
            var neck = Bone(chest, "Neck", new Vector3(0f, 0.26f, 0.01f));
            var head = Bone(neck, "Head", new Vector3(0f, 0.13f, 0.02f));
            var jaw = Bone(head, "Jaw", new Vector3(0f, -0.09f, 0.24f));

            // 短尾巴（官方设定：短尾巴，三节足够）
            var tail1 = Bone(hips, "Tail1", new Vector3(0f, -0.02f, -0.34f));
            var tail2 = Bone(tail1, "Tail2", new Vector3(0f, 0f, -0.22f));
            var tail3 = Bone(tail2, "Tail3", new Vector3(0f, 0f, -0.18f));

            // 小短手
            var armL = Bone(chest, "ArmL", new Vector3(-0.34f, 0.04f, 0.02f));
            var armR = Bone(chest, "ArmR", new Vector3(0.34f, 0.04f, 0.02f));
            var handL = Bone(armL, "HandL", new Vector3(-0.04f, -0.19f, 0.02f));
            var handR = Bone(armR, "HandR", new Vector3(0.04f, -0.19f, 0.02f));

            // 小短腿
            var legL = Bone(hips, "LegL", new Vector3(-0.19f, -0.30f, 0.02f));
            var legR = Bone(hips, "LegR", new Vector3(0.19f, -0.30f, 0.02f));
            var footL = Bone(legL, "FootL", new Vector3(0f, -0.18f, 0.03f));
            var footR = Bone(legR, "FootR", new Vector3(0f, -0.18f, 0.03f));

            var bones = new[]
            {
                hips, spine, chest, belly, neck, head, jaw,
                tail1, tail2, tail3,
                armL, armR, handL, handR,
                legL, legR, footL, footR
            };

            var index = new Dictionary<string, int>();
            for (int i = 0; i < BoneNames.Length; i++) index[BoneNames[i]] = i;

            // ---------- 网格 ----------
            var mb = new MeshBuilder();

            // ===== 躯干：胸 + 屁股（蛋形） =====
            mb.AddEllipsoid(chest.TransformPoint(new Vector3(0f, 0.03f, 0.0f)), new Vector3(0.33f, 0.31f, 0.33f), index["Chest"], Skin, 16, 12);
            mb.AddEllipsoid(hips.TransformPoint(new Vector3(0f, -0.03f, -0.05f)), new Vector3(0.35f, 0.32f, 0.32f), index["Hips"], Skin, 16, 12);

            // ===== 灵魂：duang~duang 大肚腩 =====
            // 主要绑定到 Belly 骨，边缘混一点 Spine 防止撕裂
            mb.AddBlendEllipsoid(belly.TransformPoint(new Vector3(0f, -0.01f, 0.03f)),
                new Vector3(0.43f, 0.39f, 0.41f), index["Belly"], index["Spine"], 0.86f, Belly, 18, 14);
            // 肚皮前凸的一层，强化"大肚子"轮廓
            mb.AddBlendEllipsoid(belly.TransformPoint(new Vector3(0f, -0.03f, 0.26f)),
                new Vector3(0.30f, 0.28f, 0.20f), index["Belly"], index["Spine"], 0.9f, Belly, 14, 10);
            // 肚脐
            mb.AddEllipsoid(belly.TransformPoint(new Vector3(0f, -0.02f, 0.44f)), new Vector3(0.042f, 0.042f, 0.03f), index["Belly"], new Color32(238, 186, 84, 255), 8, 6);

            // ===== 脖子（粗短，几乎和头连一起） =====
            mb.AddEllipsoid(neck.TransformPoint(new Vector3(0f, 0.04f, 0.0f)), new Vector3(0.27f, 0.22f, 0.25f), index["Neck"], Skin, 12, 8);

            // ===== 大脑袋（官方：圆圆的大脑袋） =====
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, 0.04f, 0.01f)), new Vector3(0.46f, 0.42f, 0.44f), index["Head"], Skin, 18, 14);
            // 口鼻部（宽扁，带脸颊）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, -0.09f, 0.32f)), new Vector3(0.24f, 0.16f, 0.20f), index["Head"], Skin, 14, 10);
            // 鼻孔
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.085f, -0.03f, 0.48f)), new Vector3(0.032f, 0.026f, 0.02f), index["Head"], Dark, 8, 6);
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.085f, -0.03f, 0.48f)), new Vector3(0.032f, 0.026f, 0.02f), index["Head"], Dark, 8, 6);
            // 口腔（张嘴时可见）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, -0.13f, 0.30f)), new Vector3(0.19f, 0.075f, 0.18f), index["Head"], Mouth, 12, 8);
            // 上排两颗小虎牙
            mb.AddCone(head.TransformPoint(new Vector3(-0.10f, -0.10f, 0.44f)), head.TransformPoint(new Vector3(-0.10f, -0.18f, 0.43f)), 0.030f, index["Head"], Claw, 6);
            mb.AddCone(head.TransformPoint(new Vector3(0.10f, -0.10f, 0.44f)), head.TransformPoint(new Vector3(0.10f, -0.18f, 0.43f)), 0.030f, index["Head"], Claw, 6);

            // 下巴（挂 Jaw 骨，可开合）
            mb.AddEllipsoid(jaw.TransformPoint(new Vector3(0f, -0.02f, 0.09f)), new Vector3(0.21f, 0.10f, 0.20f), index["Jaw"], Belly, 12, 8);
            mb.AddEllipsoid(jaw.TransformPoint(new Vector3(0f, 0.03f, 0.13f)), new Vector3(0.17f, 0.05f, 0.15f), index["Jaw"], Tongue, 12, 6);

            // 腮红
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.33f, -0.08f, 0.24f)), new Vector3(0.10f, 0.065f, 0.03f), index["Head"], Blush, 8, 6);
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.33f, -0.08f, 0.24f)), new Vector3(0.10f, 0.065f, 0.03f), index["Head"], Blush, 8, 6);

            // 眉毛（短短的，表情关键）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.19f, 0.19f, 0.37f)), new Vector3(0.085f, 0.022f, 0.02f), index["Head"], Dark, 8, 6,
                Quaternion.Euler(0f, 0f, -12f));
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.19f, 0.19f, 0.37f)), new Vector3(0.085f, 0.022f, 0.02f), index["Head"], Dark, 8, 6,
                Quaternion.Euler(0f, 0f, 12f));

            // 头顶两个小圆角（官方形象常见的小萌角，圆润不尖）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.18f, 0.36f, 0.0f)), new Vector3(0.065f, 0.10f, 0.065f), index["Head"], Claw, 10, 8,
                Quaternion.Euler(0f, 0f, -16f));
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.18f, 0.36f, 0.0f)), new Vector3(0.065f, 0.10f, 0.065f), index["Head"], Claw, 10, 8,
                Quaternion.Euler(0f, 0f, 16f));

            // 注意：官方奶龙【没有耳朵】【没有翅膀】【没有背鳍】，此处不生成这些部件。

            // 小短手
            AddLimb(mb, armL, handL, index["ArmL"], index["HandL"], 0.115f, 0.10f, Skin, -1);
            AddLimb(mb, armR, handR, index["ArmR"], index["HandR"], 0.115f, 0.10f, Skin, 1);
            AddHand(mb, handL, index["HandL"], -1);
            AddHand(mb, handR, index["HandR"], 1);

            // 小短腿
            AddLimb(mb, legL, footL, index["LegL"], index["FootL"], 0.15f, 0.125f, Skin, -1);
            AddLimb(mb, legR, footR, index["LegR"], index["FootR"], 0.15f, 0.125f, Skin, 1);
            AddFoot(mb, footL, index["FootL"], -1);
            AddFoot(mb, footR, index["FootR"], 1);

            // 短尾巴（三节，末端小肉球）
            var tailPath = new List<Vector3>
            {
                tail1.position, tail2.position, tail3.position,
                tail3.position + tail3.forward * -0.15f
            };
            var tailRadii = new List<float> { 0.175f, 0.125f, 0.075f, 0.032f };
            var tailBones = new List<int> { index["Tail1"], index["Tail2"], index["Tail3"], index["Tail3"] };
            mb.AddTube(tailPath, tailRadii, tailBones, Skin, 10);
            mb.AddEllipsoid(tail3.position + tail3.forward * -0.17f, new Vector3(0.045f, 0.045f, 0.045f), index["Tail3"], Belly, 8, 6);

            var mesh = mb.ToMesh(objectName + "_Mesh");

            // ---------- 蒙皮 ----------
            var bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                bindposes[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            mesh.bindposes = bindposes;

            var smr = root.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.sharedMaterial = material;
            smr.bones = bones;
            smr.rootBone = hips;
            smr.localBounds = mesh.bounds;
            smr.updateWhenOffscreen = true;

            // ---------- 大眼睛（独立网格，用于眨眼与表情缩放） ----------
            CreateEye(head, "Eye_L", new Vector3(-0.18f, 0.07f, 0.31f), material);
            CreateEye(head, "Eye_R", new Vector3(0.18f, 0.07f, 0.31f), material);

            // ---------- 挂点 ----------
            var mouth = new GameObject("MouthPoint");
            mouth.transform.SetParent(head, false);
            mouth.transform.localPosition = new Vector3(0f, -0.06f, 0.52f);

            var slam = new GameObject("SlamPoint");
            slam.transform.SetParent(root.transform, false);
            slam.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            return root;
        }

        // ---------- 构件 ----------
        static Transform Bone(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        static void AddLimb(MeshBuilder mb, Transform start, Transform end, int boneA, int boneB, float r0, float r1, Color32 color, int side)
        {
            Vector3 mid = (start.position + end.position) * 0.5f;
            Vector3 offset = new Vector3(side * 0.015f, 0f, 0.02f);
            mb.AddTube(new List<Vector3> { start.position, mid + offset, end.position },
                new List<float> { r0, (r0 + r1) * 0.5f, r1 },
                new List<int> { boneA, boneA, boneB }, color, 10);
        }

        static void AddHand(MeshBuilder mb, Transform hand, int bone, int side)
        {
            mb.AddEllipsoid(hand.position, new Vector3(0.115f, 0.105f, 0.115f), bone, Belly, 10, 8);
            for (int i = 0; i < 3; i++)
            {
                float a = (i - 1) * 0.55f;
                Vector3 basePos = hand.position + new Vector3(Mathf.Sin(a) * 0.09f, -0.05f, 0.05f + Mathf.Cos(a) * 0.03f);
                Vector3 tip = basePos + new Vector3(side * 0.02f, -0.04f, 0.06f);
                mb.AddCone(basePos, tip, 0.026f, bone, Claw, 6);
            }
        }

        static void AddFoot(MeshBuilder mb, Transform foot, int bone, int side)
        {
            mb.AddEllipsoid(foot.position + foot.forward * 0.05f, new Vector3(0.135f, 0.075f, 0.18f), bone, Belly, 12, 8);
            for (int i = 0; i < 3; i++)
            {
                Vector3 basePos = foot.position + foot.forward * 0.18f + foot.right * (i - 1) * 0.065f;
                mb.AddCone(basePos, basePos + foot.forward * 0.08f, 0.028f, bone, Claw, 6);
            }
        }

        static void CreateEye(Transform head, string name, Vector3 localPos, Material material)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(head, false);
            eye.transform.localPosition = localPos;

            var mb = new MeshBuilder();
            // 眼白：大而有神，略扁椭圆
            mb.AddEllipsoid(new Vector3(0f, 0f, 0.02f), new Vector3(0.145f, 0.155f, 0.105f), 0, new Color32(255, 255, 255, 255), 14, 10);
            // 瞳孔：大圆眼
            mb.AddEllipsoid(new Vector3(0f, -0.005f, 0.11f), new Vector3(0.082f, 0.095f, 0.065f), 0, Dark, 12, 8);
            // 高光
            mb.AddEllipsoid(new Vector3(-0.035f, 0.04f, 0.15f), new Vector3(0.030f, 0.034f, 0.02f), 0, new Color32(255, 255, 255, 255), 8, 6);
            // 次高光
            mb.AddEllipsoid(new Vector3(0.04f, -0.045f, 0.14f), new Vector3(0.018f, 0.020f, 0.02f), 0, new Color32(200, 220, 255, 255), 6, 5);

            var mesh = mb.ToMesh(name + "_Mesh");
            var mf = eye.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = eye.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }
    }
}
