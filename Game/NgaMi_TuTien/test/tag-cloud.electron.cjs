// Run with: ../../node_modules/.bin/electron test/tag-cloud.electron.cjs
// Offline renderer regression and screenshots; uses an isolated temporary profile.
const { app, BrowserWindow } = require("electron");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { pathToFileURL } = require("node:url");

const root = path.resolve(__dirname, "..");
const output = process.env.NGAMI_TAG_QA_DIR || path.join(os.tmpdir(), "ngami-tag-cloud-qa");
fs.mkdirSync(output, { recursive: true });
app.setPath("userData", path.join(os.tmpdir(), "ngami-tag-cloud-profile-" + process.pid));
const url = (p) => pathToFileURL(path.join(root, p)).href;
const scripts = ["config", "cultivation", "duel", "boss", "possession", "announcer", "lines", "game", "tag-layout", "tag-layout-v3", "tag-layout-cloud", "render"];
const html = path.join(output, "tag-cloud-qa.html");
fs.writeFileSync(html, `<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="${url("src/style.css")}"><div id="stage"></div>${scripts.map((n) => `<script src="${url("src/" + n + ".js")}"></script>`).join("")}`);

app.whenReady().then(async () => {
  // Keep the offscreen window smaller than the desktop so Windows does not clip captures.
  const win = new BrowserWindow({ show: false, width: 500, height: 700, webPreferences: { offscreen: true, backgroundThrottling: false, webSecurity: false } });
  await win.loadFile(html);
  const report = await win.webContents.executeJavaScript(`(async () => {
    const c = NgaMi.DEFAULTS;
    c.layout.tagStyle = 'v3';
    const g = new NgaMi.Game(c, { now: () => Date.now(), scope: 'demo' });
    g.applySettings({ voice: false, sfx: false, duel: true, boss: false, tauHoa: false, kiep: false, linhKhi: false });
    g.enter();
    for (let i = 0; i < 53; i++) g.handleLiveEvent({ type: 'like', user: { id: 'u' + i, nickname: 'Đạo hữu ' + i }, like: { count: 1 } });
    const state = g.getState();
    const base = ${JSON.stringify(url("src/assets/game/"))} + '/';
    const view = NgaMi.createRenderer(document.getElementById('stage'), c, { assetBase: base });
    const starts = []; let cursor = 0;
    for (const realm of c.realms) { starts.push(cursor); cursor += realm.stages.length; }
    const failures = [];
    // Exercise all configured levels on the same DOM node, including each transition to Đại viên mãn.
    for (let ri = 0; ri < c.realms.length; ri++) {
      const realm = c.realms[ri];
      for (let st = 0; st < realm.stages.length; st++) {
        state.seats = [Object.assign({}, state.seats[0], { realm: ri, level: starts[ri] + st })];
        view.render(state);
        const node = document.querySelector('.nm-disciple');
        const img = node.querySelector('.nm-plate').style.backgroundImage;
        const gems = [...node.querySelectorAll('.nm-gems i')];
        const slots = ri === 0 ? 9 : ri === c.realms.length - 1 ? 0 : 4;
        const lit = slots ? st + 1 : 0;
        if (!img.includes('/tag_cloud/tag_' + realm.sprite + '.webp') || img.includes('_dvm') || gems.length !== slots || gems.filter(x => x.classList.contains('on')).length !== lit) failures.push({ ri, st, img, slots: gems.length, lit: gems.filter(x => x.classList.contains('on')).length });
      }
    }
    // Restore a full, varied stage using all realms and levels.
    const full = g.getState();
    for (let i = 0; i < full.seats.length; i++) {
      const ri = [6, 3, 9, 0, 1, 2, 4, 5, 7, 8][i % 10];
      full.seats[i].realm = ri;
      full.seats[i].level = starts[ri] + (i % c.realms[ri].stages.length);
    }
    // Use a fresh renderer so the level-transition camera from the 42-step test
    // does not zoom a podium while taking the full-stage screenshots.
    const fullView = NgaMi.createRenderer(document.getElementById('stage'), c, { assetBase: base });
    fullView.render(full);
    const images = await Promise.all(c.realms.map(r => new Promise(resolve => { const im = new Image(); im.onload = () => resolve(true); im.onerror = () => resolve(false); im.src = base + 'tag_cloud/tag_' + r.sprite + '.webp'; })));
    const tags = [...document.querySelectorAll('.nm-tag')];
    const whiteBands = tags.filter(x => getComputedStyle(x, '::before').content !== 'none' || getComputedStyle(x, '::after').content !== 'none').length;
    window.qa = { view: fullView, state: full };
    return { failures, levels: cursor, tags: tags.length, imagesLoaded: images.filter(Boolean).length, whiteBands };
  })()`);
  assert.deepEqual(report.failures, []);
  assert.equal(report.levels, 42);
  assert.equal(report.tags, 53);
  assert.equal(report.imagesLoaded, 10);
  assert.equal(report.whiteBands, 0);
  await new Promise((resolve) => setTimeout(resolve, 1800));
  fs.writeFileSync(path.join(output, "tag-cloud-san.png"), (await win.webContents.capturePage()).toPNG());
  await win.webContents.executeJavaScript(`(() => { const s = qa.state; s.duel = Object.assign({}, s.duel, { phase: 'duel', scores: { 1: 0, 2: 0 }, remainingMs: 180000 }); s.seats.forEach((x, i) => x.side = i % 2 ? 2 : 1); qa.view.render(s); })()`);
  await new Promise((resolve) => setTimeout(resolve, 1200));
  fs.writeFileSync(path.join(output, "tag-cloud-tithi.png"), (await win.webContents.capturePage()).toPNG());
  const legacy = await win.webContents.executeJavaScript(`(() => {
    const c = NgaMi.DEFAULTS; c.layout.tagStyle = 'v2';
    const state = qa.state; state.duel.phase = 'idle';
    state.seats = [Object.assign({}, state.seats[0], { realm: 1, level: c.realms[0].stages.length + 3 })];
    const view = NgaMi.createRenderer(document.getElementById('stage'), c, { assetBase: ${JSON.stringify(url("src/assets/game/"))} + '/' });
    view.render(state);
    return { image: document.querySelector('.nm-plate').style.backgroundImage, gems: document.querySelectorAll('.nm-gems i.on').length };
  })()`);
  assert.match(legacy.image, /ban3\/tag\/tag_trucCo_dvm\.webp/);
  assert.equal(legacy.gems, 4);
  // Also open the actual entry point: this checks that its script order loads the layout.
  await win.loadFile(path.join(root, "index.html"));
  const entry = await win.webContents.executeJavaScript(`(() => {
    document.getElementById('sVoice').checked = false;
    document.getElementById('sSfx').checked = false;
    document.getElementById('enterBtn').click();
    const { game, view } = window.__ngami;
    game.handleLiveEvent({ type: 'like', user: { id: 'qa-entry', nickname: 'Đạo hữu' }, like: { count: 1 } });
    view.render(game.getState());
    return { layout: Object.keys(NgaMi.TAG_LAYOUT_CLOUD || {}).length, image: document.querySelector('.nm-plate').style.backgroundImage, gems: document.querySelectorAll('.nm-gems i').length };
  })()`);
  assert.equal(entry.layout, 10);
  assert.match(entry.image, /tag_cloud\/tag_luyenKhi\.webp/);
  assert.equal(entry.gems, 9);
  console.log(JSON.stringify(report));
  app.quit();
}).catch((error) => { console.error(error); app.exit(1); });
