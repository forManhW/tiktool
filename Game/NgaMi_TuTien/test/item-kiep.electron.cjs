// Chạy bằng ../../node_modules/electron/dist/electron.exe test/item-kiep.electron.cjs
const { app, BrowserWindow } = require("electron");
const assert = require("node:assert/strict");
const path = require("node:path");
app.whenReady().then(async () => {
  const win = new BrowserWindow({ show: false, webPreferences: { backgroundThrottling: false } });
  await win.loadFile(path.join(__dirname, "../index.html"));
  const result = await win.webContents.executeJavaScript(`(() => {
    const game = window.__ngami.game;
    game.enter();
    game.handleLiveEvent({ type: 'like', user: { id: 'helper', nickname: 'Hộ pháp' }, like: { count: 1 } });
    const state = game.getState();
    state.kiep = { id: 1, targetId: 'target', name: 'Độ kiếp', realmName: 'Nguyên Anh', realm: 3, phase: 'strike', remainingMs: 30000, need: 100, shield: 0, struck: 0, strikes: 6, log: [] };
    state.items = [];
    const sounds = [], stage = document.createElement('div'); document.body.appendChild(stage);
    const view = NgaMi.createRenderer(stage, NgaMi.DEFAULTS, { onSound: name => sounds.push(name) });
    view.render(state);
    state.items = [{ id: 1, item: 'chu_qua', userId: 'target', count: 1, effects: [] }];
    view.render(state);
    state.items.push({ id: 2, item: 'linh_thu_dan', userId: 'target', count: 1, effects: [] });
    view.render(state);
    state.items.push({ id: 3, item: 'linh_thach', userId: 'helper', count: 1, effects: [] });
    view.render(state);
    state.items.push({ id: 4, item: 'ban_dao', userId: 'missing', count: 1, effects: [] });
    view.render(state);
    return sounds;
  })()`);
  assert.deepEqual(result, ["item_chu_qua", "trung", "item_linh_thach"]);
  console.log("Pháp bảo của người độ kiếp và người hộ pháp đều phát đúng âm một lần");
  app.quit();
}).catch(e => { console.error(e); app.exit(1); });
