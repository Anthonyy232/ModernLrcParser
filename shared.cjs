// Helpers for readme/build.cjs and releases/banner.cjs.
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const font = (file, style) => `@font-face { font-family: 'Instrument Serif'; font-style: ${style}; font-weight: 400;
  src: url(data:font/woff2;base64,${fs.readFileSync(path.join(__dirname, 'fonts', file)).toString('base64')}) format('woff2'); }`;
const fonts = `<style>${font('InstrumentSerif-Regular.woff2', 'normal')}\n${font('InstrumentSerif-Italic.woff2', 'italic')}</style>`;

// The master SVG from the default-branch checkout, for inlining.
const svg = (checkout, file) => fs.readFileSync(path.join(checkout, file), 'utf8').replace(/<title>.*?<\/title>/, '');

// Renders each job's HTML and screenshots `selector` (or the viewport) to `out`.
async function render(jobs) {
  const browser = await chromium.launch();
  for (const { html, out, width, height, scale = 1, selector } of jobs) {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: scale });
    await page.setContent(`<!doctype html><meta charset="utf-8">${fonts}${html}`);
    await page.evaluate(async () => {
      await document.fonts.load("400 64px 'Instrument Serif'");
      await document.fonts.load("italic 400 64px 'Instrument Serif'");
      await document.fonts.ready;
    });
    const target = selector ? page.locator(selector) : page;
    await target.screenshot({ path: out, omitBackground: true, animations: 'disabled' });
    await page.close();
    console.log(out);
  }
  await browser.close();
}

module.exports = { svg, render };
