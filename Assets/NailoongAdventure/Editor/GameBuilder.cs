using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 一键生成整个 Demo：字体、材质、音效、特效、角色/敌人/道具预制体、四个场景与构建设置。
    /// 菜单：奶龙 / 一键生成 Demo（全量重建）
    /// </summary>
    public static class GameBuilder
    {
        public const string ROOT = "Assets/NailoongAdventure";
        public const string RES = ROOT + "/Resources";
        public const string MAT = ROOT + "/Materials";
        public const string PREFABS = RES + "/Prefabs";
        public const string VFX = RES + "/VFX";
        public const string AUDIO = RES + "/Audio";
        public const string FONTS = RES + "/Fonts";
        public const string MESHES = ROOT + "/Meshes";
        public const string SCENES = ROOT + "/Scenes";

        static Material characterMat, terrainMat, waterMat, propMat;

        [MenuItem("奶龙/一键生成 Demo（全量重建）", false, 10)]
        public static void BuildAll()
        {
            Debug.Log("=== 奶龙冒险 Demo 生成开始 ===");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int failed = 0;

            if (!Safe("目录准备", PrepareFolders)) failed++;
            if (!Safe("字体导入", ImportFont)) failed++;
            if (!Safe("材质创建", CreateMaterials)) failed++;
            if (!Safe("音频合成", () => AudioFactory.GenerateAll(AUDIO))) failed++;
            if (!Safe("特效生成", () => VFXFactory.GenerateAll(VFX))) failed++;
            if (!Safe("预制体生成", BuildPrefabs)) failed++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Safe("场景搭建", SceneBuilder.BuildAllScenes)) failed++;
            if (!Safe("构建设置", ConfigureBuildSettings)) failed++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sw.Stop();
            if (failed == 0)
            {
                EditorPrefs.SetBool(AutoBuild.DoneKey, true);
                Debug.Log($"=== 全部生成完成（{sw.Elapsed.TotalSeconds:F1}s）！打开场景 Scenes/Level1_Beach 即可试玩 ===");
            }
            else
            {
                Debug.LogError($"=== 生成结束，但有 {failed} 个步骤失败，请查看上方红色日志 ===");
            }
        }

        /// <summary>分步容错：单个步骤失败不影响其余步骤，并把堆栈完整输出到控制台。</summary>
        public static bool Safe(string label, System.Action action)
        {
            try
            {
                action();
                Debug.Log($"[奶龙] {label} 完成");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[奶龙] {label} 失败：{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        [MenuItem("奶龙/仅重新生成场景", false, 11)]
        public static void RebuildScenes()
        {
            PrepareFolders();
            CreateMaterials();
            SceneBuilder.BuildAllScenes();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("奶龙/仅重新生成音频", false, 12)]
        public static void RebuildAudio()
        {
            Directory.CreateDirectory(AUDIO);
            AudioFactory.GenerateAll(AUDIO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ================= 构建可执行程序 =================
        /// <summary>
        /// 构建 Windows 64 位可执行程序。可由菜单触发，也可在批处理中
        /// 用 -executeMethod Nailoong.EditorTools.GameBuilder.BuildWindows64 调用。
        /// 若场景尚未生成，会先自动执行一键生成。
        /// </summary>
        [MenuItem("奶龙/构建 Windows 64 可执行程序", false, 20)]
        public static void BuildWindows64()
        {
            // 场景不存在 -> 先一键生成
            if (!File.Exists(SCENES + "/Level1_Beach.unity"))
            {
                Debug.Log("[奶龙] 未检测到关卡场景，先执行一键生成…");
                BuildAll();
            }

            var scenes = new System.Collections.Generic.List<string>();
            foreach (var name in new[] { "MainMenu", "Level1_Beach", "Level2_Forest", "Level3_Volcano" })
            {
                string path = SCENES + "/" + name + ".unity";
                if (File.Exists(path)) scenes.Add(path);
            }

            if (scenes.Count == 0)
            {
                Debug.LogError("[奶龙] 没有可构建的场景，构建中止。请先执行「奶龙/一键生成 Demo（全量重建）」。");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            ConfigureBuildSettings();

            // 强制使用 Mono 后端并关闭托管代码剥离：绕开 IL2CPP 链接器
            // (Unity.Linker.Api.dll) 被本机应用控制策略拦截导致 BuildPlayer 失败的问题。
            UnityEditor.PlayerSettings.SetScriptingBackend(
                UnityEditor.BuildTargetGroup.Standalone,
                UnityEditor.ScriptingImplementation.Mono2x);
            UnityEditor.PlayerSettings.SetManagedStrippingLevel(
                UnityEditor.BuildTargetGroup.Standalone,
                UnityEditor.ManagedStrippingLevel.Disabled);

            string outDir = "Builds/Win64";
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outDir + "/NailoongAdventure.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log("[奶龙] 开始构建 Windows 64 可执行程序，场景数: " + scenes.Count);
            var report = BuildPipeline.BuildPlayer(options);

            string result = report.summary.result.ToString();
            Debug.Log("[奶龙] 构建结果: " + result
                      + " | 错误数: " + report.summary.totalErrors
                      + " | 耗时: " + report.summary.totalTime
                      + " | 输出: " + report.summary.outputPath);

            if (result != "Succeeded")
            {
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == UnityEngine.LogType.Error)
                            Debug.LogError("[奶龙构建错误] " + m.content);
                Debug.LogError("[奶龙] 构建未成功，结果: " + result + "，请查看 Console 中的编译/打包错误。");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // 批处理模式下主动退出（此时不应再加 -quit，否则编辑器会在初始化完成前被提前终止）
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// 构建 WebGL 网页版：切到 WebGL 平台（该平台只支持 IL2CPP）并打包到 Builds/WebGL。
        /// 用 -executeMethod Nailoong.EditorTools.GameBuilder.BuildWebGL 调用。
        /// 若场景尚未生成，会先自动执行一键生成。
        /// </summary>
        [MenuItem("奶龙/构建 WebGL 网页版", false, 21)]
        public static void BuildWebGL()
        {
            // 场景不存在 -> 先一键生成
            if (!File.Exists(SCENES + "/Level1_Beach.unity"))
            {
                Debug.Log("[奶龙] 未检测到关卡场景，先执行一键生成…");
                BuildAll();
            }

            var scenes = new List<string>();
            foreach (var name in new[] { "MainMenu", "Level1_Beach", "Level2_Forest", "Level3_Volcano" })
            {
                string path = SCENES + "/" + name + ".unity";
                if (File.Exists(path)) scenes.Add(path);
            }

            if (scenes.Count == 0)
            {
                Debug.LogError("[奶龙] 没有可构建的场景，构建中止。请先执行「奶龙/一键生成 Demo（全量重建）」。");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            ConfigureBuildSettings();

            // 切到 WebGL 平台（会触发一次全量资产重导入，首次切换较慢）
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[奶龙] 切换活动构建目标到 WebGL，首次切换需重新导入资产，请耐心等待…");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            // WebGL 只支持 IL2CPP 后端
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
            // 关闭托管剥离：避免反射调用/程序化生成依赖的类型被误删
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Disabled);
            // 使用 Brotli 压缩：wasm(~42MB) 压到 ~10MB、data(~18MB) 压到 ~5MB，
            // 全部低于 Cloudflare Pages 单文件 25MB 上限，避免托管时被静默丢弃。
            // Unity 6 的 loader 会在 JS 端自动解压，无需服务端配置 Content-Encoding。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

            string outDir = "Builds/WebGL";
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[奶龙] 开始构建 WebGL 网页版，场景数: " + scenes.Count);
            var report = BuildPipeline.BuildPlayer(options);

            string result = report.summary.result.ToString();
            Debug.Log("[奶龙] WebGL 构建结果: " + result
                      + " | 错误数: " + report.summary.totalErrors
                      + " | 耗时: " + report.summary.totalTime
                      + " | 输出: " + report.summary.outputPath);

            if (result != "Succeeded")
            {
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error)
                            Debug.LogError("[奶龙构建错误] " + m.content);
                Debug.LogError("[奶龙] WebGL 构建未成功，结果: " + result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ================= 目录与基础资产 =================
        static void PrepareFolders()
        {
            Directory.CreateDirectory(ROOT);
            Directory.CreateDirectory(RES);
            Directory.CreateDirectory(MAT);
            Directory.CreateDirectory(PREFABS);
            Directory.CreateDirectory(VFX);
            Directory.CreateDirectory(AUDIO);
            Directory.CreateDirectory(FONTS);
            Directory.CreateDirectory(MESHES);
            Directory.CreateDirectory(SCENES);
        }

        /// <summary>把系统里的中文字体复制进工程，保证 UI 中文不乱码。</summary>
        static void ImportFont()
        {
            string target = FONTS + "/simhei.ttf";
            if (File.Exists(target)) return;

            string[] candidates =
            {
                "C:/Windows/Fonts/simhei.ttf",
                "C:/Windows/Fonts/msyh.ttc",
                "C:/Windows/Fonts/simsun.ttc",
                "C:/Windows/Fonts/deng.ttf",
                "C:/Windows/Fonts/simkai.ttf"
            };

            foreach (var c in candidates)
            {
                if (!File.Exists(c)) continue;
                File.Copy(c, target, true);
                AssetDatabase.ImportAsset(target);
                var font = AssetDatabase.LoadAssetAtPath<Font>(target);
                if (font != null)
                {
                    Debug.Log($"[字体] 已导入 {c}");
                    return;
                }
                File.Delete(target);
            }
            Debug.LogWarning("[字体] 未找到可用中文字体，UI 中文可能显示为方块。");
        }

        static void CreateMaterials()
        {
            characterMat = CreateOrLoadMat(MAT + "/M_Character.mat", "Nailoong/VertexLit", m =>
            {
                m.SetColor("_RimColor", new Color(1f, 0.92f, 0.6f, 1f));
                m.SetFloat("_RimPower", 2.4f);
                m.SetFloat("_RimStrength", 0.45f);
                m.SetFloat("_Steps", 3f);
                m.SetFloat("_Glossiness", 0.22f);
            });

            terrainMat = CreateOrLoadMat(MAT + "/M_Terrain.mat", "Nailoong/VertexLit", m =>
            {
                m.SetColor("_RimColor", new Color(0.7f, 0.85f, 1f, 1f));
                m.SetFloat("_RimPower", 3.5f);
                m.SetFloat("_RimStrength", 0.12f);
                m.SetFloat("_Steps", 2f);
                m.SetFloat("_Glossiness", 0.05f);
            });

            propMat = CreateOrLoadMat(MAT + "/M_Prop.mat", "Nailoong/VertexLit", m =>
            {
                m.SetColor("_RimColor", new Color(1f, 0.95f, 0.8f, 1f));
                m.SetFloat("_RimPower", 2.8f);
                m.SetFloat("_RimStrength", 0.3f);
                m.SetFloat("_Steps", 3f);
                m.SetFloat("_Glossiness", 0.15f);
            });

            waterMat = CreateOrLoadMat(MAT + "/M_Water.mat", "Nailoong/StylizedWater", m =>
            {
                m.SetColor("_ShallowColor", new Color(0.45f, 0.85f, 0.92f, 0.72f));
                m.SetColor("_DeepColor", new Color(0.10f, 0.40f, 0.66f, 0.92f));
                m.SetFloat("_WaveSpeed", 0.9f);
                m.SetFloat("_WaveScale", 1.6f);
                m.SetFloat("_WaveHeight", 0.09f);
            });
        }

        static Material CreateOrLoadMat(string path, string shaderName, System.Action<Material> setup)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(Shader.Find(shaderName));
            else mat.shader = Shader.Find(shaderName);
            setup?.Invoke(mat);
            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material CharacterMaterial => characterMat ??= AssetDatabase.LoadAssetAtPath<Material>(MAT + "/M_Character.mat");
        public static Material TerrainMaterial => terrainMat ??= AssetDatabase.LoadAssetAtPath<Material>(MAT + "/M_Terrain.mat");
        public static Material PropMaterial => propMat ??= AssetDatabase.LoadAssetAtPath<Material>(MAT + "/M_Prop.mat");
        public static Material WaterMaterial => waterMat ??= AssetDatabase.LoadAssetAtPath<Material>(MAT + "/M_Water.mat");

        // ================= 预制体 =================
        static void BuildPrefabs()
        {
            Directory.CreateDirectory(PREFABS);

            // --- 玩家：奶龙 ---
            var player = DragonFactory.Build("Nailoong_Player", CharacterMaterial);
            PersistMeshes(player, MESHES);
            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 8f;
            rb.linearDamping = 0f;
            rb.angularDamping = 2f;
            var cap = player.AddComponent<CapsuleCollider>();
            cap.height = 1.5f;
            cap.radius = 0.5f;
            cap.center = new Vector3(0f, 0.75f, 0f);

            var playerCtl = player.AddComponent<PlayerController>();
            playerCtl.groundMask = LayerMask.GetMask("Default");
            var dmg = player.AddComponent<Damageable>();
            dmg.faction = Faction.Player;
            dmg.maxHealth = 120f;
            dmg.health = 120f;
            dmg.invulnerableTime = 0.6f;
            dmg.knockbackResist = 0.35f;
            var anim = player.AddComponent<DragonAnimator>();
            player.AddComponent<PlayerCombat>();
            SavePrefab(player, PREFABS + "/Player_Nailoong.prefab");
            Object.DestroyImmediate(player);

            // --- 布丁怪（三种颜色） ---
            var tints = new[]
            {
                new Color32(255, 143, 177, 255),   // 草莓
                new Color32(143, 224, 143, 255),   // 抹茶
                new Color32(181, 143, 255, 255)    // 蓝莓
            };
            for (int i = 0; i < tints.Length; i++)
            {
                var enemy = EnemyFactory.BuildPudding(CharacterMaterial, tints[i]);
                PersistMeshes(enemy, MESHES);
                var erb = enemy.AddComponent<Rigidbody>();
                erb.mass = 4f;
                erb.freezeRotation = true;
                var ed = enemy.AddComponent<Damageable>();
                ed.faction = Faction.Enemy;
                ed.maxHealth = 40f;
                ed.health = 40f;
                ed.invulnerableTime = 0.2f;
                var ai = enemy.AddComponent<EnemyController>();
                ai.kind = EnemyKind.Pudding;
                ai.attackDamage = 11f;
                ai.moveSpeed = 2.2f;
                ai.chaseSpeed = 3.8f;
                ai.detectRange = 14f;
                ai.dropPrefab = "Prefabs/Pickup_Snack";
                enemy.name = "Enemy_Pudding";
                SavePrefab(enemy, PREFABS + "/Enemy_Pudding" + i + ".prefab");
                Object.DestroyImmediate(enemy);
            }

            // --- 炸鸡鸟 ---
            var bird = EnemyFactory.BuildBird(CharacterMaterial);
            PersistMeshes(bird, MESHES);
            var brb = bird.AddComponent<Rigidbody>();
            brb.mass = 2f;
            brb.useGravity = false;
            brb.freezeRotation = true;
            var bd = bird.AddComponent<Damageable>();
            bd.faction = Faction.Enemy;
            bd.maxHealth = 32f;
            bd.health = 32f;
            var bai = bird.AddComponent<EnemyController>();
            bai.kind = EnemyKind.Bird;
            bai.attackDamage = 14f;
            bai.moveSpeed = 4.5f;
            bai.chaseSpeed = 7f;
            bai.detectRange = 18f;
            bai.dropPrefab = "Prefabs/Pickup_Heal";
            bai.dropChance = 0.5f;
            bird.name = "Enemy_Bird";
            SavePrefab(bird, PREFABS + "/Enemy_Bird.prefab");
            Object.DestroyImmediate(bird);

            // --- 精英布丁王 ---
            var elite = EnemyFactory.BuildPudding(CharacterMaterial, new Color32(255, 196, 74, 255));
            PersistMeshes(elite, MESHES);
            elite.transform.localScale = Vector3.one * 1.9f;
            var erb2 = elite.AddComponent<Rigidbody>();
            erb2.mass = 12f;
            erb2.freezeRotation = true;
            var ed2 = elite.AddComponent<Damageable>();
            ed2.faction = Faction.Enemy;
            ed2.maxHealth = 140f;
            ed2.health = 140f;
            ed2.knockbackResist = 0.7f;
            var eai = elite.AddComponent<EnemyController>();
            eai.kind = EnemyKind.Elite;
            eai.attackDamage = 20f;
            eai.moveSpeed = 2.6f;
            eai.chaseSpeed = 4.6f;
            eai.detectRange = 16f;
            eai.attackCooldown = 1.2f;
            eai.dropPrefab = "Prefabs/Pickup_Heal";
            eai.dropCount = 2;
            elite.name = "Enemy_Elite";
            SavePrefab(elite, PREFABS + "/Enemy_Elite.prefab");
            Object.DestroyImmediate(elite);

            // --- Boss 暴暴龙 ---
            var boss = EnemyFactory.BuildBoss(CharacterMaterial);
            PersistMeshes(boss, MESHES);
            var bossRb = boss.AddComponent<Rigidbody>();
            bossRb.mass = 40f;
            bossRb.freezeRotation = true;
            var bossDmg = boss.AddComponent<Damageable>();
            bossDmg.faction = Faction.Enemy;
            bossDmg.maxHealth = 620f;
            bossDmg.health = 620f;
            bossDmg.invulnerableTime = 0.12f;
            bossDmg.knockbackResist = 0.95f;
            bossDmg.showDamageText = true;
            var bossAi = boss.AddComponent<BossController>();
            bossAi.mouthPoint = boss.transform.Find("MouthPoint");
            bossAi.tailPoint = boss.transform.Find("Tail1");
            SavePrefab(boss, PREFABS + "/Boss_Baobaolong.prefab");
            Object.DestroyImmediate(boss);

            // --- 道具 ---
            for (int i = 0; i < 3; i++)
            {
                var snack = PropFactory.BuildSnack(PropMaterial, i);
                PersistMeshes(snack, MESHES);
                var c = snack.GetComponent<Collectible>();
                if (c == null) c = snack.AddComponent<Collectible>();
                c.type = PickupType.Snack;
                c.itemId = "snack";
                snack.name = "Pickup_Snack";
                SavePrefab(snack, PREFABS + "/Pickup_Snack" + i + ".prefab");
                Object.DestroyImmediate(snack);
            }

            var heal = PropFactory.BuildSnack(PropMaterial, 0);
            PersistMeshes(heal, MESHES);
            var hc = heal.AddComponent<Collectible>();
            hc.type = PickupType.Heal;
            hc.healAmount = 22f;
            heal.name = "Pickup_Heal";
            SavePrefab(heal, PREFABS + "/Pickup_Heal.prefab");
            Object.DestroyImmediate(heal);

            var rage = PropFactory.BuildSnack(PropMaterial, 2);
            PersistMeshes(rage, MESHES);
            var rc = rage.AddComponent<Collectible>();
            rc.type = PickupType.Rage;
            rc.rageAmount = 20f;
            rage.name = "Pickup_Rage";
            SavePrefab(rage, PREFABS + "/Pickup_Rage.prefab");
            Object.DestroyImmediate(rage);

            var cage = PropFactory.BuildCage(PropMaterial);
            PersistMeshes(cage, MESHES);
            cage.AddComponent<Interactable>().kind = InteractKind.Cage;
            SavePrefab(cage, PREFABS + "/Cage.prefab");
            Object.DestroyImmediate(cage);

            var portal = PropFactory.BuildPortal(PropMaterial);
            PersistMeshes(portal, MESHES);
            portal.AddComponent<Interactable>().kind = InteractKind.Portal;
            SavePrefab(portal, PREFABS + "/Portal.prefab");
            Object.DestroyImmediate(portal);

            var chick = EnemyFactory.BuildChick(CharacterMaterial);
            PersistMeshes(chick, MESHES);
            chick.name = "Chick";
            SavePrefab(chick, PREFABS + "/Chick.prefab");
            Object.DestroyImmediate(chick);

            // --- 小七（NPC） ---
            var seven = BuildSeven();
            PersistMeshes(seven, MESHES);
            var inter = seven.AddComponent<Interactable>();
            inter.kind = InteractKind.Talk;
            inter.speakerName = "小七";
            inter.lines = new[]
            {
                "奶龙！暴暴龙把我们的零食全抢走了，就在前面那片海滩上！",
                "路上有布丁怪拦路，用你的爪子拍它们就行——左键！",
                "吃饱了才有力气打架，记得捡起地上的布丁补充火力值！"
            };
            SavePrefab(seven, PREFABS + "/NPC_Seven.prefab");
            Object.DestroyImmediate(seven);

            // --- 树木 / 岩石 / 云 ---
            var treeA = PropFactory.BuildTree(PropMaterial, new Color32(176, 124, 78, 255), new Color32(126, 206, 122, 255), 1f, 0);
            PersistMeshes(treeA, MESHES); SavePrefab(treeA, PREFABS + "/Tree_Pine.prefab"); Object.DestroyImmediate(treeA);
            var treeB = PropFactory.BuildTree(PropMaterial, new Color32(196, 146, 84, 255), new Color32(255, 170, 200, 255), 1f, 1);
            PersistMeshes(treeB, MESHES); SavePrefab(treeB, PREFABS + "/Tree_Lollipop.prefab"); Object.DestroyImmediate(treeB);
            var treeC = PropFactory.BuildTree(PropMaterial, new Color32(214, 200, 168, 255), new Color32(240, 96, 110, 255), 1f, 2);
            PersistMeshes(treeC, MESHES); SavePrefab(treeC, PREFABS + "/Tree_Mushroom.prefab"); Object.DestroyImmediate(treeC);
            var rock = PropFactory.BuildRock(PropMaterial, new Color32(148, 144, 140, 255), 1f);
            PersistMeshes(rock, MESHES); SavePrefab(rock, PREFABS + "/Rock.prefab"); Object.DestroyImmediate(rock);
            var cloud = PropFactory.BuildCloud(PropMaterial, 1f);
            PersistMeshes(cloud, MESHES); SavePrefab(cloud, PREFABS + "/Cloud.prefab"); Object.DestroyImmediate(cloud);
        }

        /// <summary>Q 版小七：奶龙的地球好伙伴。</summary>
        static GameObject BuildSeven()
        {
            var go = new GameObject("NPC_Seven");
            var mb = new MeshBuilder();
            var skin = new Color32(255, 224, 196, 255);
            var cloth = new Color32(90, 158, 232, 255);
            var pants = new Color32(60, 72, 110, 255);
            var hair = new Color32(58, 44, 40, 255);

            mb.AddTube(new List<Vector3> { new Vector3(0f, 0.42f, 0f), new Vector3(0f, 0.95f, 0f), new Vector3(0f, 1.28f, 0f) },
                new List<float> { 0.20f, 0.19f, 0.17f }, new List<int> { 0, 0, 0 }, cloth, 10);
            mb.AddEllipsoid(new Vector3(0f, 0.30f, 0f), new Vector3(0.19f, 0.28f, 0.17f), 0, pants, 10, 8);
            mb.AddEllipsoid(new Vector3(0f, 1.55f, 0.02f), new Vector3(0.24f, 0.25f, 0.23f), 0, skin, 14, 10);
            mb.AddEllipsoid(new Vector3(0f, 1.72f, 0.0f), new Vector3(0.26f, 0.14f, 0.25f), 0, hair, 12, 8);
            mb.AddEllipsoid(new Vector3(-0.10f, 1.58f, 0.22f), new Vector3(0.045f, 0.055f, 0.03f), 0, new Color32(48, 40, 36, 255), 6, 5);
            mb.AddEllipsoid(new Vector3(0.10f, 1.58f, 0.22f), new Vector3(0.045f, 0.055f, 0.03f), 0, new Color32(48, 40, 36, 255), 6, 5);
            // 手臂
            mb.AddTube(new List<Vector3> { new Vector3(-0.20f, 1.22f, 0f), new Vector3(-0.30f, 0.90f, 0.02f), new Vector3(-0.32f, 0.62f, 0.06f) },
                new List<float> { 0.075f, 0.068f, 0.06f }, new List<int> { 0, 0, 0 }, cloth, 6);
            mb.AddTube(new List<Vector3> { new Vector3(0.20f, 1.22f, 0f), new Vector3(0.30f, 0.90f, 0.02f), new Vector3(0.32f, 0.62f, 0.06f) },
                new List<float> { 0.075f, 0.068f, 0.06f }, new List<int> { 0, 0, 0 }, cloth, 6);
            mb.AddEllipsoid(new Vector3(-0.32f, 0.56f, 0.07f), new Vector3(0.075f, 0.075f, 0.075f), 0, skin, 8, 6);
            mb.AddEllipsoid(new Vector3(0.32f, 0.56f, 0.07f), new Vector3(0.075f, 0.075f, 0.075f), 0, skin, 8, 6);
            // 腿
            mb.AddTube(new List<Vector3> { new Vector3(-0.10f, 0.34f, 0f), new Vector3(-0.11f, 0.16f, 0f) },
                new List<float> { 0.09f, 0.08f }, new List<int> { 0, 0 }, pants, 6);
            mb.AddTube(new List<Vector3> { new Vector3(0.10f, 0.34f, 0f), new Vector3(0.11f, 0.16f, 0f) },
                new List<float> { 0.09f, 0.08f }, new List<int> { 0, 0 }, pants, 6);
            mb.AddEllipsoid(new Vector3(-0.11f, 0.07f, 0.06f), new Vector3(0.09f, 0.06f, 0.14f), 0, new Color32(70, 70, 80, 255), 8, 6);
            mb.AddEllipsoid(new Vector3(0.11f, 0.07f, 0.06f), new Vector3(0.09f, 0.06f, 0.14f), 0, new Color32(70, 70, 80, 255), 8, 6);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("SevenMesh");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CharacterMaterial;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.7f; col.radius = 0.32f; col.center = new Vector3(0f, 0.85f, 0f);
            return go;
        }

        // ================= 工具 =================
        /// <summary>把运行时生成的 Mesh 落盘为资产，避免保存预制体后丢失引用。</summary>
        public static void PersistMeshes(GameObject root, string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var path = folder + "/" + Sanitize(mesh.name) + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null) AssetDatabase.CreateAsset(mesh, path);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved != null) mf.sharedMesh = saved;
            }
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;
                var path = folder + "/" + Sanitize(mesh.name) + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null) AssetDatabase.CreateAsset(mesh, path);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved != null) smr.sharedMesh = saved;
            }
        }

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        public static void SavePrefab(GameObject go, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(SCENES + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SCENES + "/Level1_Beach.unity", true),
                new EditorBuildSettingsScene(SCENES + "/Level2_Forest.unity", true),
                new EditorBuildSettingsScene(SCENES + "/Level3_Volcano.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            PlayerSettings.productName = "奶龙冒险 Nailoong Adventure";
            PlayerSettings.companyName = "Sun Zihang";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
        }
    }
}
