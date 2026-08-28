# 奶龙冒险 Nailoong Adventure — Unity Demo

> 一句话目标：在 Unity 中从零打造「奶龙」3D 冒险游戏 Demo，包含环境、战斗、任务、完整关卡流程，以及程序化角色动画与视听效果。

**技术栈**：Unity 6.3 LTS（6000.3.22f1）· Built-in 渲染管线 · 纯 C# · 零外部资源依赖

> **仓库说明（GitHub）**：本仓库只提交**源代码**（脚本 / Shader / 工程设置 / 文档）。预制体、场景、音效、特效、材质等运行时资产由编辑器脚本**首次打开工程时自动生成**（菜单「奶龙 / 一键生成 Demo」或 AutoBuild 自动入口），因此不纳入版本控制。克隆后按「快速开始」第 2–3 步即可跑起来。

---

## 一、快速开始

| 步骤 | 操作 |
|------|------|
| 1 | 用 Unity Hub 打开工程目录 `D:\Project\NailoongAdventure`（Unity 版本 6000.3.22f1） |
| 2 | 等待 Unity 完成首次导入与脚本编译（右下角进度结束，通常几分钟） |
| 3 | 编译通过后工程会**自动生成**全部内容（Console 打印 `[奶龙] … 完成` 与 `=== 全部生成完成 ===`） |
| 4 | 打开场景 `Assets/NailoongAdventure/Scenes/Level1_Beach`，点击 Play 开始游戏 |

> 若第 3 步未自动执行，手动点击菜单 **奶龙 → 一键生成 Demo（全量重建）**。
> 生成脚本按步骤做了容错：任何一步失败都会在 Console 打出红色日志与完整堆栈，其余步骤继续。

> 菜单项说明：
> - **奶龙 / 一键生成 Demo（全量重建）**：字体、材质、音效、特效、预制体、四个场景、构建设置，全部重建。
> - **奶龙 / 仅重新生成场景**：资产不变，只重建四个场景。
> - **奶龙 / 仅重新生成音频**：只重新合成 WAV 音效与 BGM。

**打包 exe**：菜单 `File → Build Settings → Build`，目标平台 Windows x64，输出目录建议 `Build/Win64`。

---

## 二、操作说明

| 输入 | 动作 |
|------|------|
| `W A S D` | 相机相对方向移动 |
| `空格` | 跳跃（可二段跳） |
| `鼠标移动` | 环绕视角 |
| `鼠标左键` | 普攻三连击（拍击 ×2 + 尾扫） |
| `Shift` | 咕噜冲撞（翻滚冲刺，冲刺期间无敌，可穿透伤害） |
| `Q` | 泰山压顶（跳起砸地，范围击飞，耗 25 火力） |
| `F` | 龙耀吐息（持续喷射星火，耗 45 火力） |
| `R` | 奶龙变色（6 秒减伤 50% + 提速，耗 30 火力） |
| `E` | 与 NPC 对话 / 开启传送门 |
| `ESC` | 暂停 |

---

## 三、关卡流程

| 关卡 | 场景 | 目标 | 奖励 |
|------|------|------|------|
| 第一关 · 奶黄海滩 | `Level1_Beach` | ① 与小七对话 ② 收集 5 个零食 ③ 击退 4 只布丁怪 | 解锁 **泰山压顶** |
| 第二关 · 奶油森林 | `Level2_Forest` | ① 打碎 3 个笼子救出小鸡 ② 击退 4 只炸鸡鸟 ③ 收集 6 份森林点心 | 解锁 **龙耀吐息**，开启传送门 |
| 最终关 · 焦糖火山 | `Level3_Volcano` | 击败暴暴龙（三阶段 Boss） | 通关结算 |

关卡之间由 `LevelFlow` 串联：任务全部完成 → 播放通关面板 → 自动载入下一关；死亡后可在出生点复活（火山关限 2 次）。

---

## 四、战斗设计

**火力值（Rage）** 是核心资源，参考奶龙联动设定中的「火冒三丈」：

- 造成伤害 `+2.2`，受到伤害 `+5`，自然衰减 `1.5/秒`，上限 100。
- 释放技能消耗火力值，并按 **已损失生命的 25%** 回复气血 —— 越敢打，越能打。

| 技能 | 消耗 | 冷却 | 效果 |
|------|------|------|------|
| 奶龙拍击（普攻） | — | 0.32s | 三连击，第三段尾扫 1.25 倍伤害 |
| 咕噜冲撞 | — | 0.75s | 冲刺穿透 + 0.32s 无敌帧 |
| 泰山压顶 | 25 火力 | 4s | 跃起砸地，4.2m 范围伤害 + 击飞 |
| 龙耀吐息 | 45 火力 | 6s | 前方扇形持续喷射（每 0.16s 一跳伤害） |
| 奶龙变色 | 30 火力 | 12s | 6 秒内减伤 50%、移动加速 |

打击感由 `VFXManager` 统一调度：命中顿帧（hitstop）、相机创伤抖动、屏幕闪白、伤害飘字（暴击放大加粗）、粒子特效。

---

## 五、程序化资产（全部由代码生成）

| 类别 | 说明 |
|------|------|
| **奶龙模型** | `DragonFactory`：20 根骨骼的 SkinnedMesh，大肚子 / 大脑袋 / 短四肢 / 小翅膀 / 四节尾巴，含独立眼睛网格与嘴部挂点，顶点色着色 |
| **敌人** | `EnemyFactory`：布丁怪（三色果冻）、炸鸡鸟（独立翅膀节点）、精英布丁王、暴暴龙（背刺 + 大角 + 尾锤）、小鸡 |
| **场景装饰** | `PropFactory`：零食（布丁/蛋糕/甜甜圈）、笼子、传送门、三种甜点树、岩石、云朵 |
| **地形** | `EnvironmentFactory`：程序化高度场网格 + 顶点色分层（沙滩/草地/岩石/熔岩）+ MeshCollider |
| **音效与 BGM** | `AudioFactory`：合成 13 个音效与 6 段 BGM，直接写出 16bit PCM WAV |
| **粒子特效** | `VFXFactory`：10 套特效（命中、爆炸、拾取、治疗、落地、起跳、冲刺、冲击波、吐息、变色） |
| **UI** | `UIManager`：运行时代码构建整套界面，中文字体自动从系统导入（simhei） |

### 角色动画
`DragonAnimator` 不使用任何动画文件，而是**实时计算骨骼姿态**：

- 状态机：Locomotion / Jump / Fall / Land / Dash / Claw / Tail / Breath / Slam / Hurt / Eat / Victory / Sleep
- 每根骨骼用 `SmoothDamp` 向目标角度插值，因此任意状态切换都是平滑过渡，不会突跳
- 叠加层：呼吸、尾巴延迟波浪（相位按骨序号偏移）、跑动前倾、随机眨眼、受击抖动、挤压拉伸（squash & stretch）
- 攻击动作由 `PlayerCombat` 用协程驱动时序（前摇 → 判定 → 后摇），与动画时长严格对齐

---

## 六、目录结构

```
Assets/NailoongAdventure/
├── Editor/                 # 编辑器生成脚本（不参与运行时）
│   ├── GameBuilder.cs      # 一键生成入口（菜单：奶龙/一键生成 Demo）
│   ├── SceneBuilder.cs     # 四个场景搭建
│   ├── DragonFactory.cs    # 奶龙建模
│   ├── EnemyFactory.cs     # 敌人与 Boss 建模
│   ├── PropFactory.cs      # 道具与装饰
│   ├── EnvironmentFactory.cs# 地形/天空/光照
│   ├── AudioFactory.cs     # 程序化音频合成
│   ├── VFXFactory.cs       # 粒子特效预制体
│   └── MeshBuilder.cs      # 网格构建图元库
├── Scripts/
│   ├── Core/               # GameManager / AudioManager / VFXManager / CameraRig / GameEvents
│   ├── Player/             # PlayerController / DragonAnimator / PlayerCombat
│   ├── Combat/             # Damageable
│   ├── Enemy/              # EnemyController / BossController / Projectile
│   ├── World/              # QuestSystem / LevelFlow / Collectible / Interactable
│   └── UI/                 # UIManager / UIPanels / MenuController
├── Shaders/                # NailoongVertexLit（卡通）/ GradientSkybox / StylizedWater
├── Resources/              # Prefabs / VFX / Audio / Fonts（运行时加载）
├── Materials/ Meshes/ Scenes/
```

---

## 七、已知限制与扩展方向

- Demo 定位为**可玩原型**：地形为程序化低模，未做 LOD 与遮挡剔除优化。
- Boss 战目前只有一种 Boss；`BossController` 状态机已预留扩展位（新增状态即可）。
- 存档使用 `PlayerPrefs + JSON`，记录已通关数与已解锁技能；最佳用时仅在本次会话内保留。
- 若要替换为美术资源：把 FBX 拖入 `Assets/`，用其 SkinnedMeshRenderer 替换 `Player_Nailoong` 预制体中的对应节点，并保持骨骼命名与 `DragonAnimator.BoneNames` 一致即可无缝接入现有动画系统。

---

*角色形象与技能灵感来自《奶龙》官方设定（异星幼龙、duang~duang 大肚子、变色与变大变小能力）及《灵兽大冒险》联动技能（火力值机制、泰山压顶、龙耀吐息）。本 Demo 仅用于技术学习与非商业演示。*
