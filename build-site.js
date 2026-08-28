// 构建脚本：将落地页（index.html）及可选静态资源打包到 dist/，
// 供 Cloudflare Pages 的 `npm run build` 阶段发布。
// 该仓库本体是 Unity 工程，此脚本仅负责 Web 展示页，不触碰游戏源码。

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

console.log('✅ 构建完成 -> dist/index.html');
