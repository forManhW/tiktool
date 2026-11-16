// Run with: ../../node_modules/.bin/electron test/item-projectile.electron.cjs
// Exercises actual action-to-renderer links, including the 64-element VFX budget.
const { app, BrowserWindow } = require("electron");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");

const root = path.resolve(__dirname, "..");
const output = process.env.NGAMI_VFX_QA_DIR || path.join(os.tmpdir(), "ngami-item-vfx-qa");
fs.mkdirSync(output, { recursive: true });
app.setPath("userData", path.join(os.tmpdir(), "ngami-item-vfx-profile-" + process.pid));

app.whenReady().then(async () => {
  const win = new BrowserWindow({ show: false, width: 500, height: 700, webPreferences: { offscreen: true, backgroundThrottling: false } });
  await win.loadFile(path.join(root, "index.html"));
  const report = await win.webContents.executeJavaScript(`(async () => {
    const assert = (ok, why) => { if (!ok) throw Error(why); };
    const c = NgaMi.DEFAULTS;
    const game = new NgaMi.Game(c, { now: () => Date.now(), random: () => 0, scope: 'demo' });
    game.applySettings({ voice: false, sfx: false, duel: true, boss: false, tauHoa: false, kiep: false, linhKhi: false });
    game.enter();
    const source = id => ({ user: { id, nickname: id }, eventType: 'gift' });
    for (const id of ['a', 'b']) game.handleLiveEvent({ type: 'like', user: source(id).user, like: { count: 1 } });
    game.handleAction('tu_luyen', { amount: 10000 }, source('b'));
    game.handleAction('start_duel', {}, { mode: 'test' });
    game.handleAction('vao_phe', { side: 1 }, source('a'));
    game.handleAction('vao_phe', { side: 2 }, source('b'));
    document.getElementById('settings').classList.add('hidden');
    const stage = document.getElementById('stage'); stage.classList.remove('hidden');
    const sounds = [];
    const view = NgaMi.createRenderer(stage, c, { onSound: x => sounds.push(x) });
    view.render(game.getState());
    const cases = [
      ['linh_thach', 60, 9], ['tu_khi_dan', 210, 3], ['chu_qua', 360, 3],
      ['ban_dao', 72, 9], ['linh_chi', 380, 3], ['ngoc_dich', 380, 3],
      ['bi_tich', 340, 3], ['phi_kiem', 340, 9], ['long_chau', 720, 3]
    ];
    const found = [];
    for (const [item, width, count] of cases) {
      view.resetEffects();
      game.handleAction(item, { amount: 100 }, source('a'));
      const state = game.getState();
      assert(state.items.at(-1).effects.some(x => x.kind === 'duel' && x.to === 'b'), item + ': missing real target');
      view.render(state);
      const flights = [...stage.querySelectorAll('.nm-vfx-fly')];
      assert(flights.length === count, item + ': projectile count ' + flights.length);
      assert(flights.every(x => parseFloat(x.style.width) === width), item + ': incorrect projectile width');
      const glyph = stage.querySelector('.nm-vfx-glyphs');
      assert(Boolean(glyph) === (item === 'bi_tich'), item + ': incorrect glyph attachment');
      if (glyph) {
        assert(glyph.textContent.includes('氣') && getComputedStyle(glyph, '::before').content.includes('劍'), 'missing gold glyphs');
        assert(view.vfxStats().elements >= 2, 'glyph missing from VFX budget');
      }
      found.push(item);
    }
    view.resetEffects();
    game.handleAction('linh_thu_dan', { amount: 100 }, source('a'));
    view.render(game.getState());
    assert(stage.querySelectorAll('.nm-sword-item .nm-sword-move').length === 3, 'egg item fallback must have three doubled projectiles');
    view.resetEffects();
    game.handleAction('tu_luyen', { amount: 100 }, source('a'));
    view.render(game.getState());
    assert(stage.querySelector('.nm-sword:not(.nm-sword-item)'), 'ordinary attack must retain original size');
    view.resetEffects();
    const before = game.sect.members.get('b').tuvi;
    game.handleAction('bi_tich', { amount: 100 }, source('a'));
    const finalState = game.getState(), expectedLost = before - game.sect.members.get('b').tuvi;
    assert(finalState.duel.hits.at(-1).lost === expectedLost, 'game damage differs from recorded hit');
    view.render(finalState);
    window.itemQA = { game, view, stage, source, sounds, expectedLost };
    return { found, initial: view.vfxStats() };
  })()`);
  assert.equal(report.found.length, 9);
  // Bí Tịch tier 3: the three flights begin around 1.5 s, final impact at 2.04 s.
  let flight = null;
  for (let i = 0; i < 100; i++) {
    flight = await win.webContents.executeJavaScript(`(() => {
      const g = document.querySelector('.nm-vfx-glyphs'); if (!g) return null;
      const p = g.previousElementSibling, a = g.getBoundingClientRect(), b = p.getBoundingClientRect();
      return { visible: getComputedStyle(g).opacity, inline: g.style.opacity, transform: g.style.transform, distance: Math.hypot(a.x+a.width/2-b.x-b.width/2, a.y+a.height/2-b.y-b.height/2) };
    })()`);
    if (flight && flight.inline === "1") break;
    await new Promise((resolve) => setTimeout(resolve, 40));
  }
  assert.ok(flight && flight.inline === "1", JSON.stringify(flight));
  assert.ok(flight.distance < 4, "gold glyphs must follow the projectile center");
  let impact;
  for (let i = 0; i < 80; i++) {
    impact = await win.webContents.executeJavaScript(`(() => ({ sounds: itemQA.sounds.filter(x => x === 'va_cham').length, sizes: [...document.querySelectorAll('.nm-vfx-burst')].map(x => x.style.getPropertyValue('--s')), damage: [...document.querySelectorAll('.nm-pop-red')].map(x => x.textContent), expectedLost: itemQA.expectedLost }))()`);
    if (impact.sounds) break;
    await new Promise((resolve) => setTimeout(resolve, 40));
  }
  assert.equal(impact.sounds, 1);
  assert.ok(impact.sizes.includes("240px"), "impact must be twice the previous 120px: " + JSON.stringify(impact));
  assert.deepEqual(impact.damage, ["-" + impact.expectedLost.toLocaleString("vi-VN")]);
  const stress = await win.webContents.executeJavaScript(`(() => {
    const { game, view, source } = itemQA; view.resetEffects(); let maxElements = 0, maxSequences = 0;
    for (let i = 0; i < 51; i++) { const user = source('extra' + i).user; game.handleLiveEvent({ type: 'like', user, like: { count: 1 } }); }
    for (let i = 0; i < 12; i++) { game.handleAction(i % 2 ? 'bi_tich' : 'phi_kiem', { amount: 100 }, source('a')); view.render(game.getState()); const s = view.vfxStats(); maxElements = Math.max(maxElements, s.elements); maxSequences = Math.max(maxSequences, s.sequences); }
    const seats = game.getState().seats.length; view.resetEffects(); return { seats, maxElements, maxSequences, after: view.vfxStats() };
  })()`);
  assert.ok(stress.maxElements <= 64 && stress.maxSequences <= 4);
  assert.equal(stress.seats, 53);
  assert.equal(stress.after.elements, 0);
  assert.equal(stress.after.sequences, 0);
  const edgeCases = await win.webContents.executeJavaScript(`(() => {
    const { game, view, stage, source } = itemQA;
    const foe = game.duel.players.get('b'); game.duel.players.delete('b');
    game.handleAction('bi_tich', { amount: 100 }, source('a'));
    let state = game.getState(); view.render(state);
    const noTarget = { actualTarget: state.items.at(-1).effects.some(e => e.kind === 'duel' && e.to != null), glyphs: stage.querySelectorAll('.nm-vfx-glyphs').length, flights: stage.querySelectorAll('.nm-vfx-fly').length };
    view.resetEffects(); game.duel.players.set('b', foe);
    const cfg = Object.assign({}, NgaMi.DEFAULTS, { vfx: Object.assign({}, NgaMi.DEFAULTS.vfx, { enabled: false }) });
    const disabled = NgaMi.createRenderer(stage, cfg);
    const baseline = game.getState(); baseline.duel.phase = 'idle'; disabled.render(baseline);
    game.handleAction('bi_tich', { amount: 100 }, source('a')); disabled.render(game.getState());
    const fallback = { itemSword: stage.querySelectorAll('.nm-sword-item').length, projectiles: stage.querySelectorAll('.nm-sword-item .nm-sword-move').length, glyphs: stage.querySelectorAll('.nm-vfx-glyphs').length };
    disabled.resetEffects(); return { noTarget, fallback };
  })()`);
  assert.deepEqual(edgeCases.noTarget, { actualTarget: false, glyphs: 0, flights: 0 });
  assert.deepEqual(edgeCases.fallback, { itemSword: 1, projectiles: 3, glyphs: 0 });
  console.log(JSON.stringify({ report, flight, impact, stress, edgeCases }));
  app.quit();
}).catch((error) => { console.error(error); app.exit(1); });
