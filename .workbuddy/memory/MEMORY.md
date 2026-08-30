# 奶龙冒险 Nailoong Adventure — 项目长期记忆

## 身份与路径
- Unity 6.3 LTS 零资源 Demo（角色/敌人/地形/音频/特效/UI 全程序化生成）。
- 工程：`D:\Project\NailoongAdventure`；GitHub：`sun-zihang/NailoongAdventure`。
- 线上：Cloudflare Pages `nailoongadventure.pages.dev`（双平台，手动 `wrangler pages deploy dist` 为唯一游戏发布来源）。

## 构建与部署流程（已验证）
- 全量重建（清 Library 重生成所有资产）：schtasks `NailoongBuildAll`。
- WebGL 网页版构建：schtasks `NailoongWebGLRebuild2`（输出 `Builds/WebGL`）。
- 打包+部署：`rm -rf WebGLBuild && cp -r Builds/WebGL WebGLBuild` → `node build-site.js` → `wrangler pages deploy dist --project-name nailoongadventure`。
- **关键：`WebGLBuild/` 已纳入 git 跟踪**，故 Cloudflare 绑定 GitHub 的自动部署（`npm run build`→`build-site.js`）也能带上游戏本体，**不会覆盖掉 `/game/`**。无需禁用自动部署。
- Unity 在本机通过 schtasks 构建**可正常编译**（多次 0 错误成功；与早期"Unity 损坏"的旧记忆不符，以本工程实测为准）。

## 本机环境限制（durable）
- **无法跑无头浏览器验证 WebGL 运行**：Playwright chromium 报沙箱权限 `拒绝访问 (0x5)`；无系统 Chrome；`agent-browser` 二进制可运行但 `open --executable-path` 用 Playwright/Edge 均静默失败。
  → 验证游戏改动的手段限于：静态代码审查 + 构建产物体积/响应头校验 + 用户真机反馈。不要再耗本机无头浏览器。

## 已解决的 WebGL 坑（勿回退）
- SkinnedMeshRenderer 保存 prefab 时 `m_AABB` 为零 → 运行时被视锥剔除，身体/场景消失。修复：`DragonFactory`/`GameBuilder.PersistMeshes` 显式设 `localBounds` + `updateWhenOffscreen`。
- `/game/*` 的 COOP/COEP 导致 wasm 被跨源隔离拦截（`both async and sync fetching of wasm failed`）。已移除 COEP/COOP。
- 压缩与缓存：`GameBuilder.BuildWebGL` 已设 `Brotli` + `decompressionFallback`；`_headers` 中 `.unityweb` 缓存 `max-age=86400, no-transform`。
- wasm 瘦身：`ManagedStrippingLevel.High` + `Assets/NailoongAdventure/link.xml` 保住 `Assembly-CSharp`（Nailoong 命名空间）。本游戏无反射，剥离安全。

## 移动端触控（已审查正确）
- `TouchControls` 静态类：虚拟摇杆（左下）+ 跳/滚按钮（右下），仅触摸设备由 `UIManager.TickHud` 首次建好（`touchReady` 防重复）。
- 输入融合：`PlayerController.ReadInput` 把键盘轴与 `touchMove` 相加；跳跃 `QueueTouchJump`、冲刺 `TouchDash`。技能槽在 `UIManager` 里可点击 → `PlayerCombat.TryCastById`。
