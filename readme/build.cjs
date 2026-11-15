// node readme/build.cjs <checkout> : README hero and feature panels (light and dark) into <checkout>/.github/readme/,
// and the package icon into <checkout>/assets/icon.png. Code and output come from inputs/ (written by capture/),
// headlines and copy from <checkout>/site/index.html.
const fs = require('fs');
const path = require('path');
const { svg, render } = require('../shared.cjs');

const checkout = process.argv[2];
const input = name => fs.readFileSync(path.join(__dirname, 'inputs', name), 'utf8').replace(/\n$/, '');
const site = fs.readFileSync(path.join(checkout, 'site/index.html'), 'utf8');
const logo = svg(checkout, 'assets/logo.svg');

const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const span = (cls, s) => `<span class="${cls}">${esc(s)}</span>`;

// Just enough C#: strings, keywords, and type names that aren't member accesses.
const keywords = new Set(['var', 'foreach', 'in', 'if', 'is', 'string', 'new']);
const cs = code => code.replace(/("""[\s\S]*?"""|\$?"(?:[^"\\\n]|\\.)*")|(\.?)\b([A-Za-z_]\w*)|([^"$A-Za-z_.]+|.)/g, (m, str, dot, word, other) =>
  str ? span('s', str)
    : word ? esc(dot) + (keywords.has(word) ? span('k', word) : !dot && /^[A-Z]/.test(word) ? span('t', word) : esc(word))
    : esc(other));
const lrc = text => text.split('\n').map(line => {
  const [, stamps, voice = '', rest] = line.match(/^((?:\[[^\]]*\])*)([MFD]:)?(.*)$/);
  return span('tag', stamps) + (voice && span('k', voice)) + rest.split(/(<[^>]*>)/).map((s, i) => i % 2 ? span('tag', s) : esc(s)).join('');
}).join('\n');
const marked = (text, mark) => mark ? text.split(mark).map((s, i) => i % 2 ? `<mark>${esc(s)}</mark>` : esc(s)).join('') : esc(text);

// Headline and first paragraph of each feature section, and the hero, so the panels say what the site says.
const features = Object.fromEntries([...site.matchAll(/<p class="num">(\d+)<\/p>\s*<h2>([\s\S]*?)<\/h2>\s*<p>([\s\S]*?)<\/p>/g)]
  .map(([, num, title, text]) => [num, { title, text }]));
const [, heroTitle] = site.match(/<h1>([\s\S]*?)<\/h1>/);
const [, heroText] = site.match(/<p class="lede">([\s\S]*?)<\/p>/);

const theme = dark => `<style>
  :root { --bg: ${dark ? '#171614' : '#f5f3ee'}; --ink: ${dark ? '#edeae3' : '#1c1b18'}; --muted: ${dark ? '#a09b90' : '#615d55'};
    --accent: ${dark ? '#ff8a5c' : '#c2410c'}; --paper: ${dark ? '#211f1c' : '#fffefb'}; --term: ${dark ? '#0e0d0c' : '#1c1b18'};
    --k: ${dark ? '#ff8a5c' : '#c2410c'}; --t: ${dark ? '#8cc4b8' : '#0f6b5c'}; --s: ${dark ? '#d9c08a' : '#7a5a12'};
    --wash: ${dark
      ? 'radial-gradient(60% 80% at 15% 10%, #b8431f 0%, transparent 70%), radial-gradient(55% 70% at 90% 25%, #7a2a14 0%, transparent 70%), radial-gradient(70% 70% at 60% 110%, #e5532d 0%, transparent 65%), #4a1c10'
      : 'radial-gradient(60% 80% at 12% 8%, #ffd2b8 0%, transparent 70%), radial-gradient(55% 70% at 92% 20%, #ff9a6e 0%, transparent 70%), radial-gradient(70% 70% at 55% 115%, #ee6337 0%, transparent 65%), #ffdcc8'}; }
  * { box-sizing: border-box; }
  body { margin: 0; width: 1280px; color: var(--ink); font-family: system-ui, sans-serif; }
  .card { position: relative; overflow: hidden; border-radius: 28px; background: var(--bg); }
  h1, h2 { margin: 0; font-family: 'Instrument Serif', Georgia, serif; font-weight: 400; letter-spacing: -0.01em; text-wrap: balance; }
  h1 i, h2 i { color: var(--accent); }
  .stage { background: var(--wash); }
  .win { margin: 0; border-radius: 12px; background: var(--paper); box-shadow: 0 30px 70px -30px #0009, 0 0 0 1px #0000001a; }
  .win figcaption { padding: 12px 20px 0; font: 500 12px 'Cascadia Code', monospace; color: var(--muted); }
  .win pre { margin: 0; padding: 10px 20px 18px; overflow: hidden; font: 13px/1.6 'Cascadia Code', monospace; font-variant-ligatures: none; }
  .win.out { background: var(--term); color: #edeae3; }
  .win.out figcaption { color: #a09b90; }
  .k { color: var(--k); } .t { color: var(--t); } .s { color: var(--s); } .tag { color: var(--muted); }
  mark { background: none; color: #ff8a5c; }
</style>`;
const win = (caption, code, cls = '') => `<figure class="win ${cls}">${caption ? `<figcaption>${caption}</figcaption>` : ''}<pre>${code}</pre></figure>`;

const hero = dark => `${theme(dark)}<style>
  .card { height: 760px; text-align: center; }
  .brand { display: inline-flex; align-items: center; gap: 10px; margin-top: 48px; font-size: 22px; font-weight: 700; letter-spacing: -0.02em; }
  .brand svg { width: 30px; height: 30px; }
  h1 { margin-top: 30px; font-size: 116px; line-height: 0.98; }
  p { margin: 22px auto 0; max-width: 560px; text-wrap: balance; font-size: 20px; line-height: 1.45; color: var(--muted); }
  .stage { position: absolute; left: 120px; right: 120px; top: 452px; height: 420px; border-radius: 24px 24px 0 0; text-align: left; }
  .file { position: absolute; left: 56px; top: 40px; width: 560px; }
  .prog { position: absolute; left: 440px; top: 84px; width: 548px; }
  .prog .out { margin: 0 14px 14px; }
  .prog .out pre { padding: 14px 20px; }
</style>
<div class="card">
  <div class="brand">${logo}ModernLrc</div>
  <h1>${heroTitle}</h1>
  <p>${heroText}</p>
  <div class="stage">
    <div class="file">${win('song.lrc', lrc(input('song.lrc.txt')))}</div>
    <div class="prog win"><figcaption>Program.cs</figcaption><pre>${cs(input('hero.cs.txt'))}</pre>${win('', esc(input('hero.out.txt')), 'out')}</div>
  </div>
</div>`;

const feature = (dark, num, name, mark, css = '') => `${theme(dark)}<style>
  .card { display: grid; grid-template-columns: 1fr 700px; align-items: center; }
  .copy { padding: 0 48px 0 72px; }
  .num { margin: 0; font: 500 15px 'Cascadia Code', monospace; color: var(--muted); }
  h2 { margin-top: 14px; font-size: 64px; line-height: 1.02; }
  .copy p:last-child { margin: 20px 0 0; font-size: 19px; line-height: 1.5; color: var(--muted); text-wrap: pretty; }
  .stage { margin: 28px 28px 28px 0; padding: 36px 36px 40px; border-radius: 20px; }
  .out { position: relative; margin: -16px 0 0 150px; }
  ${css}
</style>
<div class="card">
  <div class="copy"><p class="num">${num}</p><h2>${features[num].title}</h2><p>${features[num].text}</p></div>
  <div class="stage">${win('Program.cs', cs(input(`${name}.cs.txt`)))}${win('output', marked(input(`${name}.out.txt`), mark), 'out')}</div>
</div>`;

const panels = [
  ['parse', '01'],
  ['karaoke', '02', /\b(sung)\b/],
  ['duet', '03', /^(\S+)/m],
  // Its long output lines need a wider terminal.
  ['roundtrip', '04', /^(True)$/m, '.out { margin-left: 84px; }'],
];
const out = name => path.join(checkout, '.github/readme', `${name}.png`);
const jobs = [false, true].flatMap(dark => {
  const t = dark ? 'dark' : 'light';
  return [
    { html: hero(dark), out: out(`hero-${t}`) },
    ...panels.map(([name, num, mark, css]) => ({ html: feature(dark, num, name, mark, css), out: out(`feature-${name}-${t}`) })),
  ].map(job => ({ ...job, width: 1280, height: 760, scale: 1.6, selector: '.card' }));
});
jobs.push({ html: `<style>body { margin: 0 } svg { display: block; width: 128px; height: 128px }</style>${logo}`, out: path.join(checkout, 'assets/icon.png'), width: 128, height: 128 });

render(jobs);
