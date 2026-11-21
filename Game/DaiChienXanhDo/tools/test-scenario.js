// Kịch bản test đầy đủ: TikTool hub -> game .exe (--test-dir) -> các bước luật chơi -> ảnh + log.
// Chạy (từ thư mục gốc repo, sau khi đã build .exe): node Game/DaiChienXanhDo/tools/test-scenario.js . <thư-mục-kết-quả>
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");

const repo = process.argv[2];
const out = process.argv[3];
const BASE = "http://127.0.0.1:3090";
const PLAYER_LOG = path.join(process.env.USERPROFILE, "AppData", "LocalLow", "TikTool", "DaiChienXanhDo", "Player.log");
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const log = (...a) => console.log(`[${new Date().toISOString().slice(11, 19)}]`, ...a);

fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(path.join(out, "data"), { recursive: true });

async function api(p, body) {
  const r = await fetch(BASE + p, body === undefined ? {} : { method: "POST", headers: { "content-type": "application/json; charset=utf-8" }, body: JSON.stringify(body) });
  const t = await r.text();
  try { return JSON.parse(t); } catch { return t; }
}
const playerLog = () => (fs.existsSync(PLAYER_LOG) ? fs.readFileSync(PLAYER_LOG, "utf8") : "");
async function waitLog(pattern, ms, from = 0) {
  const end = Date.now() + ms;
  while (Date.now() < end) { const m = playerLog().slice(from).match(pattern); if (m) return m[0]; await sleep(300); }
  return null;
}

const NAMES = ["Nguyễn Thảo Uyên", "Trần Minh Quân", "Lê Hoàng Yến", "Phạm Đức Anh", "Vũ Ngọc Lan", "Đặng Gia Huy", "Bùi Khánh Linh", "Đỗ Tuấn Kiệt",
  "Hồ Bảo Ngọc", "Ngô Thành Đạt", "Dương Mỹ Tâm", "Lý Quốc Bảo", "Mai Phương Thảo", "Trương Hải Đăng", "Phan Thu Hà", "Võ Minh Triết"];

(async () => {
  const hub = spawn(process.execPath, ["server.js"], { cwd: repo, env: { ...process.env, TIKTOOL_DATA_DIR: path.join(out, "data") }, stdio: "ignore" });
  let game;
  const results = [];
  try {
    for (let i = 0; i < 40; i++) { try { await fetch(BASE + "/api/status"); break; } catch { await sleep(250); } }
    const s = await api("/api/sessions", { gameId: "red-blue-brawl", username: "test" });
    const q = new URL(s.gameUrl).searchParams;
    const exe = path.join(repo, "Game", "DaiChienXanhDo", "Build", "DaiChienXanhDo.exe");
    game = spawn(exe, ["--session", q.get("session"), "--token", q.get("token"), "--ws", "ws://127.0.0.1:8765", "--test-dir", out], { stdio: "ignore" });
    let online = false;
    for (let i = 0; i < 60 && !online; i++) { await sleep(500); online = ((await api("/api/sessions")) || []).some((x) => x.id === s.id && x.gameOnline); }
    log("game online:", online);
    if (!online) throw new Error("game không kết nối");

    const sid = s.id;
    const ev = (e) => api("/api/events", { sessionId: sid, ...e });
    const test = (action, params = {}) => api("/api/test", { sessionId: sid, action, params });
    const shot = async (label) => { await test("debug_screenshot"); log("ảnh:", label); results.push(label); await sleep(400); };
    const user = (i) => ({ id: "v" + i, nickname: NAMES[i % NAMES.length] + (i >= NAMES.length ? " " + i : "") });

    // mapping quà giả cho test
    const gifts = { "9001": "blue_spawn", "9002": "red_spawn", "9003": "blue_superhero", "9004": "red_superhero", "9005": "blue_lightning", "9006": "red_meteor", "9007": "red_tank", "9008": "red_giant_robot", "9009": "blue_spawn" };
    for (const [giftId, action] of Object.entries(gifts))
      await api("/api/mappings", { gameId: "red-blue-brawl", eventType: "gift", giftId, action, params: { amount: action.endsWith("spawn") ? 3 : 1 } });

    // B1: đánh nhỏ, 8 người mỗi phe vào bằng comment
    for (let i = 0; i < 16; i++) await ev({ type: "comment", text: i % 2 ? "đỏ" : "xanh", user: user(i) });
    await sleep(3500);
    await shot("01 đánh nhỏ 8v8 (Live tự động)");

    // B2: quà các bậc 1–4
    await ev({ type: "gift", giftId: "9001", giftName: "Rose", count: 3, diamondCount: 1, user: user(0) });
    await ev({ type: "gift", giftId: "9003", giftName: "Hero", count: 1, diamondCount: 30, user: user(2) });
    await ev({ type: "gift", giftId: "9004", giftName: "Combo", count: 2, diamondCount: 5, user: user(1) });
    await ev({ type: "gift", giftId: "9005", giftName: "Bolt", count: 1, diamondCount: 150, user: user(4) });
    await sleep(1200);
    await shot("02 quà bậc 1-4: Siêu Chiến Binh + sấm sét");
    await test("debug_state");

    // B3: đông lính -> thảm họa (ngưỡng 120)
    for (let i = 16; i < 56; i++) await ev({ type: "comment", text: i % 2 ? "đỏ" : "xanh", user: user(i) });
    for (let k = 0; k < 4; k++) { await ev({ type: "gift", giftId: "9001", giftName: "Rose", count: 7, diamondCount: 1, user: user(20 + 2 * k) }); await ev({ type: "gift", giftId: "9002", giftName: "Rose", count: 7, diamondCount: 1, user: user(21 + 2 * k) }); }
    await sleep(700);
    await shot("03 sân đông (>120), cảnh báo thảm họa");
    const disaster = await waitLog(/\[Match\] Thảm họa.*/, 8000);
    log("thảm họa:", disaster || "KHÔNG THẤY");
    await sleep(1500);
    await shot("04 đang thảm họa");

    // B4: phe Đỏ dồn quà lớn -> thắng ván
    const before = playerLog().length;
    await ev({ type: "gift", giftId: "9008", giftName: "Robot", count: 1, diamondCount: 5000, user: user(3) });
    await ev({ type: "gift", giftId: "9007", giftName: "Tank", count: 2, diamondCount: 300, user: user(5) });
    await ev({ type: "gift", giftId: "9006", giftName: "Meteor", count: 1, diamondCount: 1000, user: user(7) });
    await sleep(4500);
    await shot("05 robot + xe tăng phe Đỏ");
    await sleep(3500);
    await shot("05b mưa thiên thạch");
    const won = await waitLog(/\[Match\] Phe \w+ thắng ván.*/, 60000, before);
    log("thắng ván:", won || "CHƯA THẮNG sau 60s");
    await sleep(1500);
    await shot("06 ăn mừng thắng ván");

    // B5: lệnh gửi trong lúc ăn mừng phải được giữ lại cho ván mới
    await ev({ type: "comment", text: "xanh", user: { id: "late1", nickname: "Người Đến Muộn" } });
    await ev({ type: "comment", text: "đỏ", user: { id: "late2", nickname: "Khách Vào Sau" } });
    const reset = await waitLog(/\[Match\] Ván mới.*/, 15000, before);
    log("reset:", reset || "KHÔNG RESET");
    await sleep(2500);
    await shot("07 ván mới sau reset");
    await sleep(4000);
    await shot("07b ván mới, camera đã đứng yên");

    // B6: chế độ Thường
    for (let i = 60; i < 66; i++) await ev({ type: "comment", text: i % 2 ? "đỏ" : "xanh", user: user(i) });
    await test("debug_toggle_mode");
    await ev({ type: "gift", giftId: "9005", giftName: "Bolt", count: 1, diamondCount: 150, user: user(60) });
    await sleep(1500);
    await shot("08 chế độ Thường (không thông báo)");
    await test("debug_toggle_mode");

    // B7: reset tỉ số
    await test("admin_reset_score");
    await sleep(800);
    await shot("09 sau reset tỉ số");
  } catch (e) {
    log("LỖI:", e.message);
  } finally {
    await sleep(800);
    try { game && game.kill(); } catch {}
    try { hub.kill(); } catch {}
  }
  const lines = playerLog().split(/\r?\n/).filter((l) => /\[(Test|Match|TikTool)\]|Exception|NullReference|error/i.test(l));
  fs.writeFileSync(path.join(out, "player-summary.txt"), lines.join("\n"));
  console.log("\n--- Player.log (rút gọn) ---\n" + lines.join("\n"));
  console.log("\n--- ảnh ---\n" + results.map((r, i) => `shot_${String(i + 1).padStart(2, "0")}.png  ${r}`).join("\n"));
})();
