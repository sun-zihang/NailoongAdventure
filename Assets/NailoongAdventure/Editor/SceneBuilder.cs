using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 场景搭建：主菜单 + 三个关卡（奶黄海滩 / 奶油森林 / 焦糖火山）。
    /// 每个关卡包含地形、光照、天空、植被、敌人、道具、任务链与关卡流程。
    /// </summary>
    public static class SceneBuilder
    {
        public static void BuildAllScenes()
        {
            // 每个场景独立容错，某一关失败不会连累其余关卡
            GameBuilder.Safe("场景 MainMenu", BuildMainMenu);
            GameBuilder.Safe("场景 Level1_Beach", BuildLevel1_Beach);
            GameBuilder.Safe("场景 Level2_Forest", BuildLevel2_Forest);
            GameBuilder.Safe("场景 Level3_Volcano", BuildLevel3_Volcano);
            GameBuilder.Safe("场景 Ending", BuildEnding);
            AssetDatabase.SaveAssets();
        }

        // ================= 通用构件 =================
        static UnityEngine.SceneManagement.Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            string path = GameBuilder.SCENES + "/" + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[场景] 已保存 {path}");
        }

        /// <summary>全局单例：游戏管理 + 音频 + 特效。</summary>
        static void AddBoot()
        {
            var boot = new GameObject("Boot");
            boot.AddComponent<GameManager>();
            boot.AddComponent<AudioManager>();
            boot.AddComponent<VFXManager>();
        }

        static void AddUI() => new GameObject("UI").AddComponent<UIManager>();

        static Camera AddCamera(Vector3 pos)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 800f;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraRig>();
            go.transform.position = pos;
            return cam;
        }

        static GameObject AddPlayer(Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Player_Nailoong.prefab");
            if (prefab == null) { Debug.LogError("玩家预制体缺失"); return null; }
            var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.identity;
            inst.name = "Player_Nailoong";
            return inst;
        }

        static GameObject AddPrefab(string prefabName, Vector3 pos, Vector3? euler = null, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/" + prefabName + ".prefab");
            if (prefab == null) { Debug.LogWarning($"预制体缺失：{prefabName}"); return null; }
            var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(euler ?? Vector3.zero);
            inst.transform.localScale = Vector3.one * scale;
            return inst;
        }

        static GameObject AddTerrain(string name, float size, int segments, Func<float, float, float> height,
            List<EnvironmentFactory.Band> bands, Color32 cliff, float waterLevel)
        {
            var cfg = new EnvironmentFactory.TerrainConfig
            {
                name = name,
                size = size,
                segments = segments,
                height = height,
                bands = bands,
                cliffColor = cliff,
                waterLevel = waterLevel
            };
            var go = EnvironmentFactory.BuildTerrain(cfg, GameBuilder.TerrainMaterial);
            GameBuilder.PersistMeshes(go, GameBuilder.MESHES);
            return go;
        }

        static GameObject AddSpawn(Vector3 pos)
        {
            var go = new GameObject("SpawnPoint");
            go.transform.position = pos;
            return go;
        }

        static Light AddSun(Vector3 euler, Color color, float intensity)
            => EnvironmentFactory.BuildSun(euler, color, intensity);

        // ================= 主菜单 =================
        static void BuildMainMenu()
        {
            var scene = NewScene("MainMenu");
            AddBoot();
            AddUI();

            // 展示台
            var island = AddPrefab("Rock", new Vector3(0f, -0.4f, 0f), null, 3.2f);
            if (island != null) island.name = "ShowIsland";

            var show = AddPrefab("Player_Nailoong", new Vector3(0f, 1.1f, 0f));
            if (show != null)
            {
                show.name = "Nailoong_Show";
                // 展示体只需要外形：移除控制与碰撞相关组件
                var toRemove = new List<Component>();
                foreach (var c in show.GetComponents<Component>())
                {
                    if (c is Transform || c is SkinnedMeshRenderer || c is MeshRenderer || c is MeshFilter || c is DragonAnimator) continue;
                    toRemove.Add(c);
                }
                foreach (var c in toRemove) UnityEngine.Object.DestroyImmediate(c);
            }

            var menu = new GameObject("MenuController");
            var mc = menu.AddComponent<MenuController>();
            mc.showModel = show != null ? show.transform : null;
            mc.orbitRadius = 5.5f;
            mc.orbitSpeed = 8f;

            EnvironmentFactory.BuildSky(
                new Color(0.28f, 0.58f, 0.95f), new Color(1f, 0.88f, 0.72f), new Color(0.5f, 0.45f, 0.42f),
                new Color(1f, 0.95f, 0.7f), new Color(0.85f, 0.9f, 1f), 0.006f, 0.55f);
            AddSun(new Vector3(48f, -30f, 0f), new Color(1f, 0.96f, 0.85f), 1.25f);

            var cam = AddCamera(new Vector3(0f, 1.6f, -5.5f));
            cam.transform.LookAt(new Vector3(0f, 1.4f, 0f));

            // 装饰云
            var clouds = new GameObject("Clouds");
            var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Cloud.prefab");
            EnvironmentFactory.ScatterClouds(clouds, cloudPrefab, 10, 120f, 18f, 7);

            SaveScene(scene, "MainMenu");
        }

        // ================= 大结局 =================
        static void BuildEnding()
        {
            var scene = NewScene("Ending");
            AddBoot();

            // 展示台 + 庆祝的奶龙
            var island = AddPrefab("Rock", new Vector3(0f, -0.4f, 0f), null, 3.6f);
            if (island != null) island.name = "ShowIsland";

            var show = AddPrefab("Player_Nailoong", new Vector3(0f, 1.1f, 0f));
            if (show != null)
            {
                show.name = "Nailoong_Show";
                var toRemove = new List<Component>();
                foreach (var c in show.GetComponents<Component>())
                {
                    if (c is Transform || c is SkinnedMeshRenderer || c is MeshRenderer || c is MeshFilter || c is DragonAnimator) continue;
                    toRemove.Add(c);
                }
                foreach (var c in toRemove) UnityEngine.Object.DestroyImmediate(c);
            }

            // 零食小山（庆祝道具）
            var rand = new System.Random(20260829);
            for (int i = 0; i < 14; i++)
            {
                float a = rand.Next(0, 360) * Mathf.Deg2Rad;
                float r = 1.2f + (float)rand.NextDouble() * 1.6f;
                AddPrefab("Pickup_Snack", new Vector3(Mathf.Cos(a) * r, 0.6f + (float)rand.NextDouble() * 0.8f, Mathf.Sin(a) * r));
            }

            // 金色黄昏天空
            EnvironmentFactory.BuildSky(
                new Color(1f, 0.72f, 0.45f), new Color(1f, 0.9f, 0.65f), new Color(0.55f, 0.42f, 0.4f),
                new Color(1f, 0.88f, 0.6f), new Color(1f, 0.85f, 0.7f), 0.005f, 0.5f);
            AddSun(new Vector3(20f, -12f, 0f), new Color(1f, 0.9f, 0.75f), 1.35f);

            var cam = AddCamera(new Vector3(0f, 1.7f, -5.2f));
            cam.transform.LookAt(new Vector3(0f, 1.4f, 0f));

            // 大结局控制（UI 运行时构建 + 返回主菜单）
            new GameObject("EndingController").AddComponent<EndingController>();

            SaveScene(scene, "Ending");
        }

        // ================= 关卡 1：奶黄海滩 =================
        static void BuildLevel1_Beach()
        {
            var scene = NewScene("Level1_Beach");
            AddBoot();
            AddUI();

            Func<float, float, float> height = (x, z) =>
            {
                float r = new Vector2(x, z).magnitude;
                float sand = 0.9f + Mathf.PerlinNoise(x * 0.035f, z * 0.035f) * 1.4f
                                 + Mathf.PerlinNoise(x * 0.12f, z * 0.12f) * 0.35f;
                // 外围沉入海水
                float sea = -3.2f - Mathf.PerlinNoise(x * 0.02f + 40f, z * 0.02f + 40f) * 1.5f;
                float t = Mathf.SmoothStep(48f, 78f, r);
                float h = Mathf.Lerp(sand, sea, t);
                // 更外圈升起环形山丘作为天然边界
                float wall = Mathf.SmoothStep(92f, 128f, r) * 16f;
                return h + wall;
            };

            var bands = new List<EnvironmentFactory.Band>
            {
                new EnvironmentFactory.Band { height = -99f, color = new Color32(92, 168, 210, 255) },   // 水下
                new EnvironmentFactory.Band { height = -0.2f, color = new Color32(214, 196, 138, 255) },  // 湿沙
                new EnvironmentFactory.Band { height = 1.0f, color = new Color32(242, 224, 160, 255) },   // 干沙
                new EnvironmentFactory.Band { height = 3.0f, color = new Color32(180, 204, 132, 255) },   // 草
                new EnvironmentFactory.Band { height = 9.0f, color = new Color32(148, 142, 128, 255) }    // 岩
            };

            AddTerrain("Terrain_Beach", 260f, 120, height, bands, new Color32(126, 118, 104, 255), 0f);
            var water = EnvironmentFactory.BuildWater(260f, 0f, GameBuilder.WaterMaterial);

            EnvironmentFactory.BuildSky(
                new Color(0.22f, 0.56f, 0.96f), new Color(0.98f, 0.9f, 0.78f), new Color(0.6f, 0.6f, 0.55f),
                new Color(1f, 0.95f, 0.72f), new Color(0.86f, 0.93f, 1f), 0.0035f, 0.62f);
            AddSun(new Vector3(50f, -40f, 20f), new Color(1f, 0.97f, 0.86f), 1.35f);

            // 装饰
            var props = new GameObject("Props");
            var palm = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Tree_Mushroom.prefab");
            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Rock.prefab");
            EnvironmentFactory.Scatter(props, palm, 14, 240f, height, 2.0f, 12f, 0.5f, 0.7f, 1.5f, 101);
            EnvironmentFactory.Scatter(props, rock, 22, 240f, height, -2f, 14f, 0.6f, 0.8f, 2.2f, 202);

            var clouds = new GameObject("Clouds");
            EnvironmentFactory.ScatterClouds(clouds, AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Cloud.prefab"), 12, 200f, 26f, 3);

            // 出生点 + 玩家 + 相机
            AddSpawn(new Vector3(0f, 3f, -30f));
            AddPlayer(new Vector3(0f, 2.4f, -30f));
            AddCamera(new Vector3(0f, 5f, -38f));

            // NPC 小七
            AddPrefab("NPC_Seven", new Vector3(4f, 2.2f, -26f), new Vector3(0f, -160f, 0f));

            // 零食（任务目标）
            var rand = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                double a = rand.NextDouble() * Math.PI * 2;
                double rr = 6 + rand.NextDouble() * 34;
                float x = (float)(Math.Cos(a) * rr);
                float z = (float)(Math.Sin(a) * rr) - 6f;
                float y = height(x, z) + 1.0f;
                string prefab = i % 4 == 3 ? "Pickup_Heal" : ("Pickup_Snack" + (i % 3));
                AddPrefab(prefab, new Vector3(x, y, z));
            }
            AddPrefab("Pickup_Rage", new Vector3(-12f, height(-12f, -14f) + 1f, -14f));

            // 布丁怪
            var enemySpots = new[]
            {
                new Vector3(14f, 0f, -12f), new Vector3(-18f, 0f, -4f), new Vector3(22f, 0f, 8f),
                new Vector3(-6f, 0f, 18f), new Vector3(30f, 0f, -22f), new Vector3(-28f, 0f, -20f)
            };
            for (int i = 0; i < enemySpots.Length; i++)
            {
                var p = enemySpots[i];
                p.y = height(p.x, p.z) + 0.5f;
                AddPrefab("Enemy_Pudding" + (i % 3), p, new Vector3(0f, rand.Next(0, 360), 0f));
            }

            // 关卡流程与任务
            var flow = new GameObject("LevelFlow");
            var lf = flow.AddComponent<LevelFlow>();
            lf.levelIndex = 0;
            lf.levelName = "第一关 · 奶黄海滩";
            lf.levelGoal = "听小七说完，找回 5 个零食，再赶走 4 只布丁怪";
            lf.bgm = "bgm_level1";
            lf.clearOnAllQuests = true;
            lf.clearMessage = "海滩清理完毕！向奶油森林进发！";
            lf.parTime = 100f;

            var qs = flow.AddComponent<QuestSystem>();
            qs.quests = new List<Quest>
            {
                new Quest { id = "q1", title = "听听小七怎么说", description = "走到小七身边按 E 交谈", type = QuestType.Talk, targetId = "小七", required = 3 },
                new Quest { id = "q2", title = "找回散落的零食", description = "收集海滩上的零食", type = QuestType.Collect, targetId = "snack", required = 5 },
                new Quest { id = "q3", title = "赶走捣乱的布丁怪", description = "击败布丁怪", type = QuestType.Kill, targetId = "Enemy_Pudding", required = 4, skillReward = "slam" }
            };

            SaveScene(scene, "Level1_Beach");
        }

        // ================= 关卡 2：奶油森林 =================
        static void BuildLevel2_Forest()
        {
            var scene = NewScene("Level2_Forest");
            AddBoot();
            AddUI();

            Func<float, float, float> height = (x, z) =>
            {
                float r = new Vector2(x, z).magnitude;
                float baseH = 3.2f
                    + Mathf.PerlinNoise(x * 0.022f + 7f, z * 0.022f + 7f) * 7.5f
                    + Mathf.PerlinNoise(x * 0.09f + 3f, z * 0.09f + 3f) * 1.6f;
                // 中心营地压平
                float flat = 1f - Mathf.SmoothStep(10f, 34f, r);
                baseH = Mathf.Lerp(baseH, 3.0f, flat * 0.9f);
                // 外圈高山
                float wall = Mathf.SmoothStep(100f, 138f, r) * 26f;
                return baseH + wall;
            };

            var bands = new List<EnvironmentFactory.Band>
            {
                new EnvironmentFactory.Band { height = -99f, color = new Color32(122, 168, 108, 255) },
                new EnvironmentFactory.Band { height = 2.5f, color = new Color32(136, 196, 116, 255) },
                new EnvironmentFactory.Band { height = 6.0f, color = new Color32(104, 172, 96, 255) },
                new EnvironmentFactory.Band { height = 12.0f, color = new Color32(92, 132, 86, 255) },
                new EnvironmentFactory.Band { height = 20.0f, color = new Color32(140, 138, 126, 255) }
            };

            AddTerrain("Terrain_Forest", 280f, 126, height, bands, new Color32(104, 96, 84, 255), -999f);

            EnvironmentFactory.BuildSky(
                new Color(0.30f, 0.62f, 0.88f), new Color(0.92f, 0.95f, 0.85f), new Color(0.45f, 0.5f, 0.42f),
                new Color(1f, 0.98f, 0.82f), new Color(0.78f, 0.88f, 0.82f), 0.0075f, 0.5f);
            AddSun(new Vector3(46f, -35f, -30f), new Color(1f, 0.95f, 0.8f), 1.15f);

            var props = new GameObject("Props");
            var pine = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Tree_Pine.prefab");
            var lolli = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Tree_Lollipop.prefab");
            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Rock.prefab");
            EnvironmentFactory.Scatter(props, pine, 46, 250f, height, 3f, 22f, 0.55f, 0.8f, 1.9f, 303);
            EnvironmentFactory.Scatter(props, lolli, 26, 250f, height, 3f, 18f, 0.5f, 0.7f, 1.6f, 404);
            EnvironmentFactory.Scatter(props, rock, 30, 250f, height, 2f, 26f, 0.65f, 0.7f, 2.4f, 505);

            var clouds = new GameObject("Clouds");
            EnvironmentFactory.ScatterClouds(clouds, AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Cloud.prefab"), 14, 220f, 34f, 8);

            AddSpawn(new Vector3(0f, 4f, -26f));
            AddPlayer(new Vector3(0f, 3.4f, -26f));
            AddCamera(new Vector3(0f, 6f, -34f));

            // 三个笼子（内含小鸡）
            var cageSpots = new[]
            {
                new Vector3(-26f, 0f, -6f), new Vector3(24f, 0f, -14f), new Vector3(6f, 0f, 26f)
            };
            foreach (var s in cageSpots)
            {
                var p = s; p.y = height(p.x, p.z);
                AddPrefab("Cage", p);
            }

            // 炸鸡鸟
            var rand = new System.Random(22);
            for (int i = 0; i < 5; i++)
            {
                double a = rand.NextDouble() * Math.PI * 2;
                double rr = 18 + rand.NextDouble() * 26;
                float x = (float)(Math.Cos(a) * rr);
                float z = (float)(Math.Sin(a) * rr);
                AddPrefab("Enemy_Bird", new Vector3(x, height(x, z) + 5.2f, z));
            }

            // 精英布丁王守着最后一个笼子
            AddPrefab("Enemy_Elite", new Vector3(6f, height(6f, 26f) + 1f, 26f));

            // 森林里的蜂蜜零食
            for (int i = 0; i < 10; i++)
            {
                double a = rand.NextDouble() * Math.PI * 2;
                double rr = 8 + rand.NextDouble() * 36;
                float x = (float)(Math.Cos(a) * rr);
                float z = (float)(Math.Sin(a) * rr);
                string prefab = i % 5 == 4 ? "Pickup_Rage" : ("Pickup_Snack" + (i % 3));
                AddPrefab(prefab, new Vector3(x, height(x, z) + 1.0f, z));
            }

            // 传送门（任务全部完成后开启）
            var portalPos = new Vector3(0f, height(0f, 48f), 48f);
            var portal = AddPrefab("Portal", portalPos);
            if (portal != null) portal.SetActive(false);

            var flow = new GameObject("LevelFlow");
            var lf = flow.AddComponent<LevelFlow>();
            lf.levelIndex = 1;
            lf.levelName = "第二关 · 奶油森林";
            lf.levelGoal = "打破 3 个笼子救出小鸡，赶走炸鸡鸟，再收集 6 份森林点心";
            lf.bgm = "bgm_level2";
            lf.clearOnAllQuests = false;
            lf.portalToActivate = portal;
            lf.clearMessage = "森林任务完成！传送门已开启，前往焦糖火山！";
            lf.parTime = 140f;

            var qs = flow.AddComponent<QuestSystem>();
            qs.quests = new List<Quest>
            {
                new Quest { id = "q1", title = "救出被关起来的小鸡", description = "攻击笼子把它打碎", type = QuestType.Free, targetId = "cage", required = 3 },
                new Quest { id = "q2", title = "赶走炸鸡鸟", description = "击败空中的炸鸡鸟", type = QuestType.Kill, targetId = "Enemy_Bird", required = 4 },
                new Quest { id = "q3", title = "收集森林点心", description = "收集散落的零食", type = QuestType.Collect, targetId = "snack", required = 6, skillReward = "breath" }
            };

            SaveScene(scene, "Level2_Forest");
        }

        // ================= 关卡 3：焦糖火山 =================
        static void BuildLevel3_Volcano()
        {
            var scene = NewScene("Level3_Volcano");
            AddBoot();
            AddUI();

            Func<float, float, float> height = (x, z) =>
            {
                float r = new Vector2(x, z).magnitude;
                // 中心竞技场：平坦的焦糖平台
                float arena = 8.5f;
                float h = Mathf.Lerp(arena, arena + Mathf.SmoothStep(26f, 60f, r) * 14f, Mathf.Clamp01((r - 22f) / 6f));
                if (r < 22f) h = arena + Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 0.25f;
                // 火山口边缘隆起
                float rim = Mathf.SmoothStep(58f, 74f, r) * 8f - Mathf.SmoothStep(74f, 96f, r) * 6f;
                // 外围峭壁
                float wall = Mathf.SmoothStep(96f, 132f, r) * 30f;
                float detail = Mathf.PerlinNoise(x * 0.06f, z * 0.06f) * 1.2f;
                return h + rim + wall + detail;
            };

            var bands = new List<EnvironmentFactory.Band>
            {
                new EnvironmentFactory.Band { height = -99f, color = new Color32(196, 108, 62, 255) },   // 熔岩
                new EnvironmentFactory.Band { height = 8.0f, color = new Color32(168, 96, 58, 255) },    // 焦糖地
                new EnvironmentFactory.Band { height = 11.0f, color = new Color32(126, 74, 52, 255) },   // 焦岩
                new EnvironmentFactory.Band { height = 18.0f, color = new Color32(88, 62, 56, 255) },    // 暗岩
                new EnvironmentFactory.Band { height = 30.0f, color = new Color32(70, 58, 62, 255) }     // 山顶
            };

            AddTerrain("Terrain_Volcano", 280f, 128, height, bands, new Color32(78, 58, 52, 255), -999f);

            // 熔岩池（竞技场外围的低洼处）
            var lava = EnvironmentFactory.BuildWater(200f, 6.2f, GameBuilder.WaterMaterial);
            string lavaPath = GameBuilder.MAT + "/M_Lava.mat";
            var lavaMat = AssetDatabase.LoadAssetAtPath<Material>(lavaPath);
            if (lavaMat == null)
            {
                lavaMat = new Material(GameBuilder.WaterMaterial);
                AssetDatabase.CreateAsset(lavaMat, lavaPath);
            }
            lavaMat.SetColor("_ShallowColor", new Color(1f, 0.55f, 0.15f, 0.95f));
            lavaMat.SetColor("_DeepColor", new Color(0.75f, 0.16f, 0.06f, 0.98f));
            lavaMat.SetFloat("_WaveSpeed", 0.35f);
            lavaMat.SetFloat("_WaveHeight", 0.05f);
            EditorUtility.SetDirty(lavaMat);
            lava.GetComponent<Renderer>().sharedMaterial = lavaMat;

            EnvironmentFactory.BuildSky(
                new Color(0.28f, 0.16f, 0.24f), new Color(0.95f, 0.55f, 0.32f), new Color(0.30f, 0.14f, 0.12f),
                new Color(1f, 0.72f, 0.42f), new Color(0.55f, 0.28f, 0.22f), 0.010f, 0.45f);
            AddSun(new Vector3(38f, -25f, 40f), new Color(1f, 0.72f, 0.5f), 1.1f);

            var props = new GameObject("Props");
            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(GameBuilder.PREFABS + "/Rock.prefab");
            EnvironmentFactory.Scatter(props, rock, 40, 240f, height, 8f, 40f, 0.8f, 1.0f, 3.0f, 606);

            AddSpawn(new Vector3(0f, 10f, -34f));
            AddPlayer(new Vector3(0f, 9.4f, -34f));
            AddCamera(new Vector3(0f, 12f, -42f));

            // Boss 与小怪
            AddPrefab("Boss_Baobaolong", new Vector3(0f, height(0f, 6f), 6f), new Vector3(0f, 180f, 0f));
            AddPrefab("Pickup_Heal", new Vector3(-14f, height(-14f, 0f) + 1f, 0f));
            AddPrefab("Pickup_Heal", new Vector3(14f, height(14f, 0f) + 1f, 0f));
            AddPrefab("Pickup_Rage", new Vector3(0f, height(0f, -18f) + 1f, -18f));

            var flow = new GameObject("LevelFlow");
            var lf = flow.AddComponent<LevelFlow>();
            lf.levelIndex = 2;
            lf.levelName = "最终关 · 焦糖火山";
            lf.levelGoal = "打败暴暴龙，夺回所有零食！";
            lf.bgm = "bgm_level3";
            lf.clearOnAllQuests = true;
            lf.maxRevives = 2;
            lf.clearMessage = "暴暴龙被击败，零食全部夺回！";
            lf.parTime = 180f;

            var qs = flow.AddComponent<QuestSystem>();
            qs.quests = new List<Quest>
            {
                new Quest { id = "boss", title = "击败暴暴龙", description = "在火山竞技场战胜暴暴龙", type = QuestType.Boss, targetId = "boss", required = 1 }
            };

            SaveScene(scene, "Level3_Volcano");
        }
    }
}
