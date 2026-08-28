using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 奶龙角色建模：程序化生成骨骼层级 + 蒙皮网格 + 眼睛/嘴部挂点，
    /// 外形特征来自官方设定——奶黄皮肤、duang~duang 大肚子、大脑袋、小翅膀、粗尾巴。
    /// </summary>
    public static class DragonFactory
    {
        // 骨骼顺序必须与 DragonAnimator.BoneNames 完全一致
        public static readonly string[] BoneNames =
        {
            "Hips","Spine","Chest","Neck","Head","Jaw",
            "Tail1","Tail2","Tail3","Tail4",
            "ArmL","ArmR","HandL","HandR",
            "LegL","LegR","FootL","FootR",
            "WingL","WingR"
        };

        // 调色板
        static readonly Color32 Skin = new Color32(255, 216, 77, 255);      // 奶黄
        static readonly Color32 Belly = new Color32(255, 246, 214, 255);    // 奶白肚皮
        static readonly Color32 Horn = new Color32(255, 250, 232, 255);     // 角与爪
        static readonly Color32 Dark = new Color32(48, 40, 36, 255);        // 眼睛/眉
        static readonly Color32 Blush = new Color32(255, 154, 162, 255);    // 腮红
        static readonly Color32 Membrane = new Color32(255, 201, 77, 255);  // 翅膀膜
        static readonly Color32 Mouth = new Color32(120, 62, 66, 255);      // 口腔

        /// <summary>生成完整的奶龙 GameObject（含 SkinnedMeshRenderer 与骨骼）。</summary>
        public static GameObject Build(string objectName, Material material)
        {
            var root = new GameObject(objectName);

            // ---------- 骨骼 ----------
            var hips = Bone(root.transform, "Hips", Vector3.zero, new Vector3(0f, 0.62f, 0f));
            var spine = Bone(hips, "Spine", new Vector3(0f, 0.14f, 0f));
            var chest = Bone(spine, "Chest", new Vector3(0f, 0.16f, 0f));
            var neck = Bone(chest, "Neck", new Vector3(0f, 0.24f, 0.02f));
            var head = Bone(neck, "Head", new Vector3(0f, 0.16f, 0.03f));
            var jaw = Bone(head, "Jaw", new Vector3(0f, -0.10f, 0.26f));

            var tail1 = Bone(hips, "Tail1", new Vector3(0f, -0.02f, -0.40f));
            var tail2 = Bone(tail1, "Tail2", new Vector3(0f, 0f, -0.26f));
            var tail3 = Bone(tail2, "Tail3", new Vector3(0f, 0f, -0.24f));
            var tail4 = Bone(tail3, "Tail4", new Vector3(0f, 0f, -0.20f));

            var armL = Bone(chest, "ArmL", new Vector3(-0.36f, 0.06f, 0.02f));
            var armR = Bone(chest, "ArmR", new Vector3(0.36f, 0.06f, 0.02f));
            var handL = Bone(armL, "HandL", new Vector3(-0.05f, -0.22f, 0.02f));
            var handR = Bone(armR, "HandR", new Vector3(0.05f, -0.22f, 0.02f));

            var legL = Bone(hips, "LegL", new Vector3(-0.20f, -0.32f, 0.02f));
            var legR = Bone(hips, "LegR", new Vector3(0.20f, -0.32f, 0.02f));
            var footL = Bone(legL, "FootL", new Vector3(0f, -0.20f, 0.03f));
            var footR = Bone(legR, "FootR", new Vector3(0f, -0.20f, 0.03f));

            var wingL = Bone(chest, "WingL", new Vector3(-0.26f, 0.18f, -0.14f));
            var wingR = Bone(chest, "WingR", new Vector3(0.26f, 0.18f, -0.14f));

            var bones = new[]
            {
                hips, spine, chest, neck, head, jaw,
                tail1, tail2, tail3, tail4,
                armL, armR, handL, handR,
                legL, legR, footL, footR,
                wingL, wingR
            };

            var index = new Dictionary<string, int>();
            for (int i = 0; i < BoneNames.Length; i++) index[BoneNames[i]] = i;

            // ---------- 网格 ----------
            var mb = new MeshBuilder();

            // 躯干：胸 + 大肚子 + 屁股（大肚子是奶龙的灵魂）
            mb.AddEllipsoid(chest.TransformPoint(new Vector3(0f, 0.02f, 0.02f)), new Vector3(0.40f, 0.36f, 0.38f), index["Chest"], Skin);
            mb.AddBlendEllipsoid(spine.TransformPoint(new Vector3(0f, -0.06f, 0.14f)), new Vector3(0.35f, 0.31f, 0.31f), index["Spine"], index["Hips"], 0.6f, Belly);
            mb.AddEllipsoid(hips.TransformPoint(new Vector3(0f, -0.04f, -0.06f)), new Vector3(0.37f, 0.33f, 0.34f), index["Hips"], Skin);

            // 肚脐（小细节）
            mb.AddEllipsoid(spine.TransformPoint(new Vector3(0f, -0.04f, 0.44f)), new Vector3(0.045f, 0.045f, 0.03f), index["Spine"], new Color32(240, 190, 90, 255), 8, 6);

            // 脖子
            mb.AddEllipsoid(neck.TransformPoint(new Vector3(0f, 0.06f, 0.01f)), new Vector3(0.26f, 0.22f, 0.24f), index["Neck"], Skin, 12, 8);

            // 头（大头）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, 0.03f, 0.02f)), new Vector3(0.42f, 0.39f, 0.40f), index["Head"], Skin, 16, 12);
            // 口鼻
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, -0.10f, 0.30f)), new Vector3(0.21f, 0.16f, 0.20f), index["Head"], Skin, 12, 8);
            // 鼻孔
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.075f, -0.05f, 0.44f)), new Vector3(0.035f, 0.028f, 0.02f), index["Head"], Dark, 8, 6);
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.075f, -0.05f, 0.44f)), new Vector3(0.035f, 0.028f, 0.02f), index["Head"], Dark, 8, 6);
            // 口腔（张嘴时可见）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0f, -0.13f, 0.28f)), new Vector3(0.17f, 0.07f, 0.17f), index["Head"], Mouth, 12, 8);
            // 上排小牙
            mb.AddCone(head.TransformPoint(new Vector3(-0.09f, -0.10f, 0.40f)), head.TransformPoint(new Vector3(-0.09f, -0.17f, 0.40f)), 0.028f, index["Head"], Horn, 6);
            mb.AddCone(head.TransformPoint(new Vector3(0.09f, -0.10f, 0.40f)), head.TransformPoint(new Vector3(0.09f, -0.17f, 0.40f)), 0.028f, index["Head"], Horn, 6);

            // 下巴（挂在 Jaw 骨上，可开合）
            mb.AddEllipsoid(jaw.TransformPoint(new Vector3(0f, -0.02f, 0.10f)), new Vector3(0.20f, 0.10f, 0.20f), index["Jaw"], Belly, 12, 8);
            mb.AddEllipsoid(jaw.TransformPoint(new Vector3(0f, 0.03f, 0.14f)), new Vector3(0.16f, 0.05f, 0.15f), index["Jaw"], new Color32(235, 150, 150, 255), 12, 6); // 舌头

            // 腮红
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.30f, -0.09f, 0.22f)), new Vector3(0.10f, 0.06f, 0.03f), index["Head"], Blush, 8, 6);
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.30f, -0.09f, 0.22f)), new Vector3(0.10f, 0.06f, 0.03f), index["Head"], Blush, 8, 6);

            // 眉毛
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.17f, 0.16f, 0.33f)), new Vector3(0.09f, 0.022f, 0.02f), index["Head"], Dark, 8, 6,
                Quaternion.Euler(0f, 0f, -14f));
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.17f, 0.16f, 0.33f)), new Vector3(0.09f, 0.022f, 0.02f), index["Head"], Dark, 8, 6,
                Quaternion.Euler(0f, 0f, 14f));

            // 头顶两只小角
            mb.AddCone(head.TransformPoint(new Vector3(-0.17f, 0.32f, 0.02f)), head.TransformPoint(new Vector3(-0.23f, 0.58f, -0.02f)), 0.085f, index["Head"], Horn, 8);
            mb.AddCone(head.TransformPoint(new Vector3(0.17f, 0.32f, 0.02f)), head.TransformPoint(new Vector3(0.23f, 0.58f, -0.02f)), 0.085f, index["Head"], Horn, 8);

            // 侧鳍（耳朵）
            mb.AddEllipsoid(head.TransformPoint(new Vector3(-0.40f, 0.08f, -0.02f)), new Vector3(0.05f, 0.13f, 0.09f), index["Head"], Membrane, 8, 6);
            mb.AddEllipsoid(head.TransformPoint(new Vector3(0.40f, 0.08f, -0.02f)), new Vector3(0.05f, 0.13f, 0.09f), index["Head"], Membrane, 8, 6);

            // 背鳍：从脖子一路排到尾巴根
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                Vector3 p = chest.TransformPoint(new Vector3(0f, 0.30f - t * 0.16f, 0.02f - t * 0.34f));
                float h = 0.16f - t * 0.08f;
                mb.AddQuadTwoSided(p + new Vector3(0f, 0f, 0.06f), p + new Vector3(0f, h, -0.02f), p + new Vector3(0f, h, -0.14f), p + new Vector3(0f, 0f, -0.08f), index["Chest"], Membrane);
            }

            // 手臂（短粗）
            AddLimb(mb, armL, handL, index["ArmL"], index["HandL"], 0.135f, 0.115f, Skin, -1);
            AddLimb(mb, armR, handR, index["ArmR"], index["HandR"], 0.135f, 0.115f, Skin, 1);

            // 手掌 + 三只小爪
            AddHand(mb, handL, index["HandL"], -1);
            AddHand(mb, handR, index["HandR"], 1);

            // 腿（短粗）
            AddLimb(mb, legL, footL, index["LegL"], index["FootL"], 0.165f, 0.135f, Skin, -1);
            AddLimb(mb, legR, footR, index["LegR"], index["FootR"], 0.165f, 0.135f, Skin, 1);

            // 脚掌 + 爪
            AddFoot(mb, footL, index["FootL"], -1);
            AddFoot(mb, footR, index["FootR"], 1);

            // 尾巴（四节，逐渐变细，末端带小肉球）
            var tailPath = new List<Vector3>
            {
                tail1.position, tail2.position, tail3.position, tail4.position,
                tail4.position + tail4.forward * -0.18f
            };
            var tailRadii = new List<float> { 0.21f, 0.16f, 0.115f, 0.075f, 0.035f };
            var tailBones = new List<int> { index["Tail1"], index["Tail2"], index["Tail3"], index["Tail4"], index["Tail4"] };
            mb.AddTube(tailPath, tailRadii, tailBones, Skin, 10);
            mb.AddEllipsoid(tail4.position + tail4.forward * -0.20f, new Vector3(0.05f, 0.05f, 0.05f), index["Tail4"], Belly, 8, 6);

            // 小翅膀（骨架 + 双面膜）
            AddWing(mb, wingL, index["WingL"], -1);
            AddWing(mb, wingR, index["WingR"], 1);

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
            smr.updateWhenOffscreen = false;

            // ---------- 眼睛（独立网格，用于眨眼缩放） ----------
            CreateEye(head, "Eye_L", new Vector3(-0.175f, 0.07f, 0.30f), material);
            CreateEye(head, "Eye_R", new Vector3(0.175f, 0.07f, 0.30f), material);

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
            mb.AddEllipsoid(hand.position, new Vector3(0.135f, 0.125f, 0.135f), bone, Belly, 10, 8);
            for (int i = 0; i < 3; i++)
            {
                float a = (i - 1) * 0.5f;
                Vector3 basePos = hand.position + new Vector3(Mathf.Sin(a) * 0.10f, -0.06f, 0.06f + Mathf.Cos(a) * 0.03f);
                Vector3 tip = basePos + new Vector3(side * 0.02f, -0.05f, 0.07f);
                mb.AddCone(basePos, tip, 0.03f, bone, Horn, 6);
            }
        }

        static void AddFoot(MeshBuilder mb, Transform foot, int bone, int side)
        {
            mb.AddEllipsoid(foot.position + foot.forward * 0.06f, new Vector3(0.155f, 0.085f, 0.20f), bone, Belly, 12, 8);
            for (int i = 0; i < 3; i++)
            {
                Vector3 basePos = foot.position + foot.forward * 0.20f + foot.right * (i - 1) * 0.075f;
                mb.AddCone(basePos, basePos + foot.forward * 0.09f, 0.032f, bone, Horn, 6);
            }
        }

        static void AddWing(MeshBuilder mb, Transform wing, int bone, int side)
        {
            // 翼骨
            mb.AddCone(wing.position, wing.position + new Vector3(side * 0.22f, 0.10f, -0.14f), 0.045f, bone, Horn, 6);
            // 翼膜（双面三角）
            Vector3 root0 = wing.position;
            Vector3 root1 = wing.position + new Vector3(side * 0.02f, -0.10f, 0.04f);
            Vector3 mid = wing.position + new Vector3(side * 0.24f, 0.06f, -0.18f);
            Vector3 tip = wing.position + new Vector3(side * 0.40f, -0.06f, -0.36f);
            mb.AddQuadTwoSided(root0, mid, tip, root1, bone, Membrane);
        }

        static void CreateEye(Transform head, string name, Vector3 localPos, Material material)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(head, false);
            eye.transform.localPosition = localPos;

            var mb = new MeshBuilder();
            mb.AddEllipsoid(new Vector3(0f, 0f, 0.02f), new Vector3(0.135f, 0.145f, 0.10f), 0, new Color32(255, 255, 255, 255), 12, 8);
            mb.AddEllipsoid(new Vector3(0f, -0.005f, 0.11f), new Vector3(0.075f, 0.088f, 0.06f), 0, Dark, 10, 8);
            mb.AddEllipsoid(new Vector3(-0.03f, 0.035f, 0.15f), new Vector3(0.028f, 0.032f, 0.02f), 0, new Color32(255, 255, 255, 255), 8, 6);
            mb.AddEllipsoid(new Vector3(0.035f, -0.04f, 0.14f), new Vector3(0.016f, 0.018f, 0.02f), 0, new Color32(200, 220, 255, 255), 6, 5);

            var mesh = mb.ToMesh(name + "_Mesh");
            var mf = eye.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = eye.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }
    }
}
