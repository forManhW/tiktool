const test = require("node:test");
const assert = require("node:assert/strict");
const path = require("path");
const { resolveNativeLaunch } = require("../core/native-launch");

const gameRoot = path.resolve("C:/tiktool/Game");
const url = "http://127.0.0.1:3090/game/DaiChienXanhDo/?session=s1&token=t1";

test("game web thì trả null", () => {
  assert.equal(resolveNativeLaunch({ gameRoot, folder: "Game/Clock", launch: { type: "web" }, url }), null);
});

test("game native: exe trong thư mục game, truyền session/token/ws", () => {
  const r = resolveNativeLaunch({ gameRoot, folder: "Game/DaiChienXanhDo", launch: { type: "native", exe: "Build/DaiChienXanhDo.exe" }, url, gamePort: 8765 });
  assert.equal(r.exe, path.join(gameRoot, "DaiChienXanhDo", "Build", "DaiChienXanhDo.exe"));
  assert.equal(r.cwd, path.join(gameRoot, "DaiChienXanhDo", "Build"));
  assert.deepEqual(r.args, ["--session", "s1", "--token", "t1", "--ws", "ws://127.0.0.1:8765"]);
});

test("chặn exe trỏ ra ngoài thư mục game", () => {
  assert.throws(() => resolveNativeLaunch({ gameRoot, folder: "Game/DaiChienXanhDo", launch: { type: "native", exe: "../Clock/x.exe" }, url }), /thư mục của game/);
  assert.throws(() => resolveNativeLaunch({ gameRoot, folder: "Game/DaiChienXanhDo", launch: { type: "native", exe: "C:/Windows/System32/cmd.exe" }, url }), /thư mục của game/);
});

test("chỉ cho file .exe và cần session/token", () => {
  assert.throws(() => resolveNativeLaunch({ gameRoot, folder: "Game/DaiChienXanhDo", launch: { type: "native", exe: "Build/run.bat" }, url }), /\.exe/);
  assert.throws(() => resolveNativeLaunch({ gameRoot, folder: "Game/DaiChienXanhDo", launch: { type: "native", exe: "Build/a.exe" }, url: "http://127.0.0.1:3090/game/x/" }), /session/);
});

test("manifest Đại Chiến Xanh Đỏ: mỗi hiệu ứng có hàm cho cả hai phe, mapping mặc định hợp lệ", () => {
  const { validateRule } = require("../core/live-events");
  const m = require("../Game/DaiChienXanhDo/manifest.json");
  const ids = new Set(m.actions.map((a) => a.id));
  for (const effect of ["join", "spawn", "like_power", "superhero", "lightning", "tank", "meteor", "giant_robot"]) {
    assert.ok(ids.has(`blue_${effect}`), `thiếu blue_${effect}`);
    assert.ok(ids.has(`red_${effect}`), `thiếu red_${effect}`);
  }
  assert.equal(m.launch.type, "native");
  for (const rule of m.defaultMappings) validateRule({ ...rule, gameId: m.id }, m);
});
