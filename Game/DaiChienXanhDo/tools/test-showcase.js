// Kịch bản chụp riêng: triệu hồi Siêu Chiến Binh, thiên thạch, xe tăng/robot phe Xanh, lốc xoáy.
// Chạy (từ thư mục gốc repo, sau khi đã build .exe): node Game/DaiChienXanhDo/tools/test-showcase.js . <thư-mục-kết-quả>
const { spawn } = require("child_process");
const fs = require("fs"), path = require("path");
const [repo, out] = process.argv.slice(2);
const BASE = "http://127.0.0.1:3090";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const api = async (p, b) => { const r = await fetch(BASE + p, b === undefined ? {} : { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(b) }); const t = await r.text(); try { return JSON.parse(t); } catch { return t; } };
fs.rmSync(out, { recursive: true, force: true }); fs.mkdirSync(path.join(out, "data"), { recursive: true });
(async () => {
  const hub = spawn(process.execPath, ["server.js"], { cwd: repo, env: { ...process.env, TIKTOOL_DATA_DIR: path.join(out, "data") }, stdio: "ignore" });
  let game;
  try {
    for (let i = 0; i < 40; i++) { try { await fetch(BASE + "/api/status"); break; } catch { await sleep(250); } }
    const s = await api("/api/sessions", { gameId: "red-blue-brawl", username: "show" });
    const q = new URL(s.gameUrl).searchParams;
    game = spawn(path.join(repo, "Game", "DaiChienXanhDo", "Build", "DaiChienXanhDo.exe"), ["--session", q.get("session"), "--token", q.get("token"), "--test-dir", out], { stdio: "ignore" });
    for (let i = 0; i < 60; i++) { await sleep(500); if (((await api("/api/sessions")) || []).some((x) => x.id === s.id && x.gameOnline)) break; }
    const sid = s.id, ev = (e) => api("/api/events", { sessionId: sid, ...e }), test = (a, p = {}) => api("/api/test", { sessionId: sid, action: a, params: p });
    const shot = async (label) => { await test("debug_screenshot"); console.log("ảnh:", label); await sleep(300); };
    for (const [g, a] of Object.entries({ "9003": "blue_superhero", "9006": "red_meteor", "9001": "blue_spawn", "9002": "red_spawn", "9011": "blue_tank", "9012": "blue_giant_robot" })) await api("/api/mappings", { gameId: "red-blue-brawl", eventType: "gift", giftId: g, action: a, params: { amount: 1 } });
    const names = ["Nguyễn Thảo Uyên", "Trần Minh Quân", "Lê Hoàng Yến", "Phạm Đức Anh", "Vũ Ngọc Lan", "Đặng Gia Huy", "Bùi Khánh Linh", "Đỗ Tuấn Kiệt", "Hồ Bảo Ngọc", "Ngô Thành Đạt", "Dương Mỹ Tâm", "Lý Quốc Bảo"];
    for (let i = 0; i < 12; i++) await ev({ type: "comment", text: i % 2 ? "đỏ" : "xanh", user: { id: "v" + i, nickname: names[i] } });
    await sleep(6000);
    await ev({ type: "gift", giftId: "9003", giftName: "Hero", count: 1, diamondCount: 30, user: { id: "v0", nickname: names[0] } });
    await ev({ type: "gift", giftId: "9001", giftName: "Rose", count: 4, diamondCount: 1, user: { id: "v2", nickname: names[2] } });
    await sleep(1500); await shot("01 triệu hồi Siêu Chiến Binh + lính mang tên");
    await sleep(3000); await shot("02 Siêu Chiến Binh đang đánh");
    await ev({ type: "gift", giftId: "9006", giftName: "Meteor", count: 1, diamondCount: 1000, user: { id: "v1", nickname: names[1] } });
    await sleep(1400); await shot("03 thiên thạch đang rơi");
    await sleep(1200); await shot("04 thiên thạch rơi xuống");
    await ev({ type: "gift", giftId: "9002", giftName: "Rose", count: 15, diamondCount: 1, user: { id: "v3", nickname: names[3] } });
    await sleep(3000);
    await ev({ type: "gift", giftId: "9011", giftName: "Tank", count: 1, diamondCount: 300, user: { id: "v4", nickname: names[4] } });
    await sleep(4500); await shot("05 lính Đỏ vây đánh xe tăng Xanh");
    await ev({ type: "gift", giftId: "9012", giftName: "Robot", count: 1, diamondCount: 5000, user: { id: "v6", nickname: names[6] } });
    await sleep(4200); await shot("06 robot phe Xanh");
    for (let k = 0; k < 3; k++) { await ev({ type: "gift", giftId: "9001", giftName: "Rose", count: 12, diamondCount: 1, user: { id: "v" + (2 * k), nickname: names[2 * k] } }); await ev({ type: "gift", giftId: "9002", giftName: "Rose", count: 12, diamondCount: 1, user: { id: "v" + (2 * k + 1), nickname: names[2 * k + 1] } }); }
    await sleep(2500); await test("debug_tornado");
    await sleep(4400); await shot("07 lốc xoáy (1)");
    await sleep(1400); await shot("08 lốc xoáy (2)");
  } catch (e) { console.log("LỖI", e.message); }
  finally { await sleep(800); try { game && game.kill(); } catch {} try { hub.kill(); } catch {} }
  const log = fs.readFileSync(path.join(process.env.USERPROFILE, "AppData", "LocalLow", "TikTool", "DaiChienXanhDo", "Player.log"), "utf8");
  console.log(log.split(/\r?\n/).filter((l) => /\[Match\]|Exception/.test(l)).join("\n"));
})();
