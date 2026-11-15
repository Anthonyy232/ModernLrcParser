// node banner.cjs <version> "<highlight>" : writes releases/v<version>.png (1280x640).
// Needs Playwright with Chromium: set NODE_PATH to a node_modules folder that has it.
const { chromium } = require('playwright');
const { execSync } = require('child_process');
const [version, highlight] = process.argv.slice(2);
const logo = execSync('git show origin/main:assets/logo.svg', { cwd: __dirname, encoding: 'utf8' }).replace(/<title>.*?<\/title>/, '');

const html = `<!doctype html><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Instrument+Serif:ital@0;1&display=swap" rel="stylesheet">
<style>
  * { box-sizing: border-box; }
  body { margin: 0; width: 1280px; height: 640px; display: grid; grid-template-columns: 1fr 520px; background: #f5f3ee; color: #1c1b18; font-family: system-ui, sans-serif; }
  .copy { padding-left: 80px; display: flex; flex-direction: column; justify-content: center; }
  .brand { display: flex; align-items: center; gap: 12px; font-size: 26px; font-weight: 700; letter-spacing: -0.02em; }
  .brand svg { width: 38px; height: 38px; }
  h1 { margin: 36px 0 0; font: italic 400 150px/0.95 'Instrument Serif', Georgia, serif; letter-spacing: -0.01em; color: #c2410c; }
  p { margin: 28px 0 0; max-width: 560px; font-size: 26px; line-height: 1.4; color: #615d55; text-wrap: balance; }
  .stage { margin: 40px 40px 40px 0; border-radius: 24px; display: grid; place-items: center;
    background: radial-gradient(60% 80% at 12% 8%, #ffd2b8 0%, transparent 70%), radial-gradient(55% 70% at 92% 20%, #ff9a6e 0%, transparent 70%), radial-gradient(70% 70% at 55% 115%, #ee6337 0%, transparent 65%), #ffdcc8; }
  .stage svg { width: 220px; height: 220px; border-radius: 48px; box-shadow: 0 30px 70px -20px #0009; }
</style>
<div class="copy"><div class="brand">${logo}ModernLrc</div><h1>${version}</h1><p>${highlight}</p></div>
<div class="stage">${logo}</div>`;

(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1280, height: 640 } });
  await p.setContent(html, { waitUntil: 'networkidle' });
  await p.evaluate(() => document.fonts.ready);
  await p.screenshot({ path: `${__dirname}/releases/v${version}.png` });
  await b.close();
})();
