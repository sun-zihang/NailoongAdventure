// 构建脚本：将落地页（index.html）与 WebGL 游戏产物打包到 dist/，
// 供 Cloudflare Pages 的 `npm run build` 阶段发布。
// 该仓库本体是 Unity 工程，此脚本只负责展示页与网页版游戏，不触碰游戏源码。

const fs = require('fs');
const path = require('path');

const root = __dirname;
const dist = path.join(root, 'dist');

fs.mkdirSync(dist, { recursive: true });

// 1) 落地页
const indexSrc = path.join(root, 'index.html');
if (!fs.existsSync(indexSrc)) {
  console.error('❌ 未找到 index.html，构建中止。');
  process.exit(1);
}
fs.copyFileSync(indexSrc, path.join(dist, 'index.html'));

// 1.5) 404 回退页：Cloudflare Pages 对未匹配路由会回退到根目录的 404.html，
// 这里直接复用展示页，避免玩家点到不存在的路径时看到裸 404。
fs.copyFileSync(indexSrc, path.join(dist, '404.html'));

// 2) WebGL 游戏产物（Unity 构建后拷入 WebGLBuild/；尚未构建时跳过，不影响发布）
const gameSrc = path.join(root, 'WebGLBuild');
if (fs.existsSync(gameSrc) && fs.statSync(gameSrc).isDirectory()) {
  copyDir(gameSrc, path.join(dist, 'game'));
  console.log('  · 已打包 WebGL 游戏 -> dist/game/');
} else {
  console.log('  · 未检测到 WebGLBuild/，本次仅发布展示页');
}

// 3) Cloudflare Pages 响应头（为 /game/* 声明 COOP/COEP，支持 WebGL 多线程）
const headersSrc = path.join(root, '_headers');
if (fs.existsSync(headersSrc)) {
  fs.copyFileSync(headersSrc, path.join(dist, '_headers'));
  console.log('  · 已写入 _headers（/game/* 跨源隔离）');
}

console.log('✅ 构建完成 -> dist/index.html');

function copyDir(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    const s = path.join(src, entry.name);
    const d = path.join(dest, entry.name);
    if (entry.isDirectory()) copyDir(s, d);
    else fs.copyFileSync(s, d);
  }
}
