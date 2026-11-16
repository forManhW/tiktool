// Run with: ../../node_modules/.bin/electron test/tag-stationary.electron.cjs
// Verify item casts and attacks do not move the cultivation label with the body.
const { app, BrowserWindow } = require("electron");
const assert = require("node:assert/strict");
const path = require("node:path");
const os = require("node:os");
app.setPath("userData", path.join(os.tmpdir(), "ngami-stationary-tag-" + process.pid));

app.whenReady().then(async () => {
  const win = new BrowserWindow({ show: false, width: 500, height: 700, webPreferences: { offscreen: true, backgroundThrottling: false } });
  await win.loadFile(path.join(__dirname, "../index.html"));
  const result = await win.webContents.executeJavaScript(`(async () => {
    const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
    const c = NgaMi.DEFAULTS, game = new NgaMi.Game(c, { now: () => Date.now(), random: () => 0, scope: 'demo' });
    game.applySettings({ voice: false, sfx: false, duel: true, boss: false, tauHoa: false, kiep: false, linhKhi: false });
    game.enter();
    const source = id => ({ user: { id, nickname: id }, eventType: 'gift' });
    game.handleLiveEvent({ type: 'like', user: source('a').user, like: { count: 1 } });
    document.getElementById('settings').classList.add('hidden');
    const stage = document.getElementById('stage'); stage.classList.remove('hidden');
    const view = NgaMi.createRenderer(stage, c); view.render(game.getState());
    const measure = root => {
      const tag = root.querySelector('.nm-tag'), plate = tag.querySelector('.nm-plate');
      const a = tag.getBoundingClientRect(), b = root.getBoundingClientRect();
      return { x: a.left - b.left, y: a.top - b.top, width: a.width, height: a.height, animation: getComputedStyle(plate).animationName, parentIsRoot: tag.parentElement === root };
    };
    let root = stage.querySelector('.nm-disciple');
    const idleBefore = measure(root);
    game.handleAction('bi_tich', { amount: 1 }, source('a')); view.render(game.getState());
    await wait(200); const idleDuring = measure(root);
    await wait(2200); const idleGlyphs = measure(root);
    game.handleLiveEvent({ type: 'like', user: source('b').user, like: { count: 1 } });
    game.handleAction('start_duel', {}, { mode: 'test' });
    game.handleAction('vao_phe', { side: 1 }, source('a'));
    game.handleAction('vao_phe', { side: 2 }, source('b'));
    view.render(game.getState()); await wait(900);
    root = stage.querySelector('.nm-tag[data-side="1"]').parentElement;
    const duelBefore = measure(root);
    game.handleAction('bi_tich', { amount: 1 }, source('a')); view.render(game.getState());
    const attackTriggered = root.classList.contains('nm-strike');
    const bodyAnimation = getComputedStyle(root.querySelector('.nm-body')).animationName;
    await wait(200); const duelDuring = measure(root);
    // Real realm changes may replace the image, but must not animate or flip the plate.
    const upgraded = game.getState(), actor = upgraded.seats.find(x => x.id === 'a');
    actor.realm = 1; actor.level = c.realms[0].stages.length;
    view.render(upgraded);
    const afterUpgrade = measure(root);
    const flipped = root.querySelector('.nm-plate').classList.contains('nm-plate-flip');
    view.resetEffects();
    return { idleBefore, idleDuring, idleGlyphs, duelBefore, duelDuring, afterUpgrade, attackTriggered, bodyAnimation, flipped };
  })()`);
  const near = (a, b) => { for (const key of ["x", "y", "width", "height"]) assert.ok(Math.abs(a[key] - b[key]) < 0.5, key + ": " + JSON.stringify({ a, b })); };
  near(result.idleBefore, result.idleDuring);
  near(result.idleBefore, result.idleGlyphs);
  near(result.duelBefore, result.duelDuring);
  assert.ok(result.attackTriggered);
  assert.equal(result.bodyAnimation, "nm-strike-duel");
  for (const key of ["idleBefore", "idleDuring", "idleGlyphs", "duelBefore", "duelDuring", "afterUpgrade"]) {
    assert.equal(result[key].animation, "none");
    assert.equal(result[key].parentIsRoot, true);
  }
  assert.equal(result.flipped, true);
  console.log(JSON.stringify(result));
  app.quit();
}).catch(error => { console.error(error); app.exit(1); });
