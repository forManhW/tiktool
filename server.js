const crypto = require("crypto");
const fs = require("fs");
const http = require("http");
const path = require("path");
const { spawn } = require("child_process");
const { WebSocketServer, WebSocket } = require("ws");
const { DatabaseSync } = require("node:sqlite");
const { EVENT_TYPES, normalizeEvent, normalizeRule, validateRule, LiveRuleEngine, publicEvent } = require("./core/live-events");
const { createTts } = require("./core/tts");

const ROOT = __dirname, PORT = Number(process.env.PORT || 3090), GAME_PORT = Number(process.env.GAME_PORT || 8765);
// Bản cài đặt: launcher truyền TIKTOOL_DATA_DIR = %APPDATA%/TikTool (thư mục cài chỉ đọc). Chạy dev: ./data như cũ.
const DATA = process.env.TIKTOOL_DATA_DIR ? path.resolve(process.env.TIKTOOL_DATA_DIR) : path.join(ROOT, "data"), ASSETS = path.join(DATA, "assets"), MAPS = path.join(DATA, "mappings.json"), SESSIONS = path.join(DATA, "sessions.json"), PUBLIC = path.join(ROOT, "public");
const dashboardClients = new Set(), logs = [], seenEvents = new Map(), sockets = new Set(), pending = new Map(), engines = new Map();
// Bản cài đặt có dữ liệu khởi đầu (scripts/build-seed.js): mapping chỉ chép khi máy chưa có, ảnh chỉ chép file còn thiếu; không ghi đè thứ người dùng đã sửa.
const SEED = process.env.TIKTOOL_SEED_DIR && fs.existsSync(process.env.TIKTOOL_SEED_DIR) ? process.env.TIKTOOL_SEED_DIR : null;
fs.mkdirSync(DATA, { recursive: true });
// Chép file còn thiếu (không ghi đè). Không dùng fs.cpSync: Node/Electron sập cả app (0xC0000409) khi đường dẫn có dấu tiếng Việt.
function copyMissing(from, to) { fs.mkdirSync(to, { recursive: true }); for (const e of fs.readdirSync(from, { withFileTypes: true })) { const a = path.join(from, e.name), b = path.join(to, e.name); if (e.isDirectory()) copyMissing(a, b); else if (!fs.existsSync(b)) fs.copyFileSync(a, b); } }
if (SEED) { const seedMaps = path.join(SEED, "mappings.json"), seedAssets = path.join(SEED, "assets"); if (!fs.existsSync(MAPS) && fs.existsSync(seedMaps)) fs.copyFileSync(seedMaps, MAPS); if (fs.existsSync(seedAssets)) copyMissing(seedAssets, ASSETS); }
fs.mkdirSync(ASSETS, { recursive: true }); for (const file of [MAPS, SESSIONS]) if (!fs.existsSync(file)) fs.writeFileSync(file, "[]\n");
const db = new DatabaseSync(path.join(DATA, "tiktool.db"));
// Điểm người chơi theo kỳ cho từng game (kỳ dài weeklyBoard.periodWeeks tuần trong manifest, mặc định 1).
const players = require("./core/player-store").createPlayerStore(db, { periodWeeksFor: (id) => games().find((g) => g.id === id)?.weeklyBoard?.periodWeeks || 1 });
db.exec("CREATE TABLE IF NOT EXISTS event_log (id TEXT PRIMARY KEY, at TEXT NOT NULL, level TEXT NOT NULL, message TEXT NOT NULL, details TEXT NOT NULL)");
db.exec("CREATE TABLE IF NOT EXISTS gift_catalog (gift_id TEXT PRIMARY KEY, name TEXT NOT NULL, diamonds INTEGER, image_url TEXT, last_seen_at TEXT NOT NULL)");
// Danh sách quà đóng kèm: thêm quà còn thiếu, giữ nguyên quà máy này đã thấy khi live.
if (SEED && fs.existsSync(path.join(SEED, "tiktool.db"))) { try { db.prepare("ATTACH DATABASE ? AS seed").run(path.join(SEED, "tiktool.db")); db.exec("INSERT OR IGNORE INTO gift_catalog SELECT gift_id, name, diamonds, image_url, last_seen_at FROM seed.gift_catalog"); } catch (e) { console.warn(`Không nạp được danh sách quà đóng kèm: ${e.message}`); } finally { try { db.exec("DETACH DATABASE seed"); } catch {} } }
const listener = { process: null, sessionId: null, state: "idle", attempts: 0, retryTimer: null, safeMode: false, lastError: null };
function saveGiftCatalog(raw) { const gifts = Array.isArray(raw) ? raw : raw?.gifts || raw?.giftList || raw?.gift_list || []; const insert = db.prepare("INSERT OR REPLACE INTO gift_catalog VALUES (?, ?, ?, ?, ?)"); for (const gift of gifts) { const id = gift.id ?? gift.giftId, name = gift.name ?? gift.giftName; if (id == null || !name) continue; const image = gift.image?.url_list?.[0] ?? gift.image?.urlList?.[0] ?? gift.imageUrl ?? null; insert.run(String(id), String(name), Number(gift.diamondCount ?? gift.diamond_count ?? gift.diamond ?? 0), image, new Date().toISOString()); } return gifts.length; }
const read = (file) => { try { return JSON.parse(fs.readFileSync(file, "utf8")); } catch { return []; } };
const write = (file, data) => fs.writeFileSync(file, `${JSON.stringify(data, null, 2)}\n`);
function duplicateMappingMessage(all, rule, excludeId) { const sameGame = all.filter((x) => x.id !== excludeId && x.gameId === rule.gameId && x.eventType === rule.eventType); if (rule.eventType === "gift" && rule.giftId && sameGame.some((x) => x.giftId === rule.giftId)) return "Gift ID này đã được mapping cho game này"; if (rule.eventType === "like" && sameGame.some((x) => Number(x.likeEvery) === rule.likeEvery && (x.likeScope || "room") === rule.likeScope)) return `Đã có mapping cho mỗi ${rule.likeEvery} tim`; if (rule.eventType === "comment") { const used = new Set(sameGame.flatMap((x) => (x.keywords || []).map((k) => k.toLowerCase()))); const clash = rule.keywords.find((k) => used.has(k.toLowerCase())); if (clash) return `Từ khóa "${clash}" đã được dùng cho game này`; } return null; }
function removeDuplicateMappings() { const kept = [], giftKeys = new Set(); for (const rule of read(MAPS)) { const giftKey = rule.giftId && (rule.eventType || "gift") === "gift" ? `${rule.gameId}:gift:${rule.giftId}` : null; if (giftKey && giftKeys.has(giftKey)) continue; kept.push(rule); if (giftKey) giftKeys.add(giftKey); } if (kept.length !== read(MAPS).length) write(MAPS, kept); }
removeDuplicateMappings();
(function upgradeMappings() { const all = read(MAPS), upgraded = all.map(normalizeRule); if (JSON.stringify(all) !== JSON.stringify(upgraded)) write(MAPS, upgraded); })();
// Game có thể khai báo manifest.defaultMappings: bộ mapping khởi đầu, chỉ thêm khi game CHƯA có mapping nào. Streamer sửa/xóa thoải mái sau đó.
(function seedDefaultMappings() {
  const root = path.join(ROOT, "Game");
  if (!fs.existsSync(root)) return;
  const all = read(MAPS);
  let added = 0;
  for (const dir of fs.readdirSync(root, { withFileTypes: true })) {
    if (!dir.isDirectory()) continue;
    let manifest;
    try { manifest = JSON.parse(fs.readFileSync(path.join(root, dir.name, "manifest.json"))); } catch { continue; }
    if (!manifest?.id || !Array.isArray(manifest.defaultMappings) || all.some((m) => m.gameId === manifest.id)) continue;
    for (const rule of manifest.defaultMappings) {
      try { all.push({ ...validateRule({ ...rule, gameId: manifest.id }, manifest), id: crypto.randomUUID(), updatedAt: new Date().toISOString() }); added++; }
      catch (e) { console.warn(`defaultMappings của ${manifest.id} bị bỏ qua: ${e.message}`); }
    }
  }
  if (added) write(MAPS, all);
})();
// Giọng đọc AI chạy trên máy (Piper). Bản cài: launcher truyền TIKTOOL_TTS_DIR = resources/tts. Chạy dev: vendor/tts.
const tts = createTts({ dir: process.env.TIKTOOL_TTS_DIR || path.join(ROOT, "vendor", "tts"), voice: process.env.TIKTOOL_TTS_VOICE, log: (level, message, details) => log(level, message, details) });
const publish = (type, value) => dashboardClients.forEach((r) => r.write(`event: ${type}\ndata: ${JSON.stringify(value)}\n\n`));
function log(level, message, details = {}) { const entry = { id: crypto.randomUUID(), at: new Date().toISOString(), level, message, details }; logs.unshift(entry); logs.length = Math.min(logs.length, 300); db.prepare("INSERT INTO event_log VALUES (?, ?, ?, ?, ?)").run(entry.id, entry.at, level, message, JSON.stringify(details)); if (Date.now() - (log.prunedAt || 0) > 60000) { log.prunedAt = Date.now(); db.prepare("DELETE FROM event_log WHERE at < ?").run(new Date(Date.now() - 7 * 86400000).toISOString()); } console.log(`${level}: ${message}`); publish("log", entry); return entry; }
function games() { const root = path.join(ROOT, "Game"); return fs.existsSync(root) ? fs.readdirSync(root, { withFileTypes: true }).filter((x) => x.isDirectory()).map((x) => { const manifest = (() => { try { return JSON.parse(fs.readFileSync(path.join(root, x.name, "manifest.json"))); } catch { return null; } })(); const active = sessionList().find((s) => s.status === "live" && s.gameId === manifest?.id); return manifest && { ...manifest, triggers: Array.isArray(manifest.triggers) ? manifest.triggers : EVENT_TYPES, liveEvents: Array.isArray(manifest.liveEvents) ? manifest.liveEvents : [], launch: { ...manifest.launch, url: `http://127.0.0.1:${PORT}/game/${x.name}/` }, folder: `Game/${x.name}`, connected: Boolean(active && gameSocket(active.id)), activeSessionId: active?.id || null }; }).filter(Boolean) : []; }
function sessionList() { return read(SESSIONS); }
function sessionPublic(session) { const active = [...sockets].find((s) => s.sessionId === session.id && s.role === "game"); return { id: session.id, username: session.username, gameId: session.gameId, roomId: session.roomId, status: session.status, createdAt: session.createdAt, gameOnline: Boolean(active), queuePending: session.queue?.length || 0, lastStateAt: session.lastStateAt || null, overlayUrl: session.overlayUrl, gameUrl: session.gameUrl }; }
function getSession(id) { return sessionList().find((x) => x.id === id); }
function saveSession(updated) { const all = sessionList(); const index = all.findIndex((x) => x.id === updated.id); if (index < 0) throw new Error("Session not found"); all[index] = updated; write(SESSIONS, all); publish("session", sessionPublic(updated)); return updated; }
function gameSocket(sessionId) { return [...sockets].find((s) => s.sessionId === sessionId && s.role === "game" && s.readyState === WebSocket.OPEN); }
function overlays(sessionId) { return [...sockets].filter((s) => s.sessionId === sessionId && s.role === "overlay" && s.readyState === WebSocket.OPEN); }
function send(ws, payload) { if (ws?.readyState === WebSocket.OPEN) ws.send(JSON.stringify(payload)); }
// Game gửi state tới 5 lần/giây. Ghi cả sessions.json + báo dashboard mỗi lần làm hub nghẽn và lệnh game chạy trễ.
// Overlay đã nhận state ngay qua broadcastState; file chỉ cần lưu bản mới nhất, tối đa 1 lần / 2 giây, không báo dashboard.
const pendingStates = new Map();
let pendingStateTimer = null;
function persistStateSoon(sessionId, state, at) {
  pendingStates.set(sessionId, { state, at });
  if (pendingStateTimer) return;
  pendingStateTimer = setTimeout(() => {
    pendingStateTimer = null;
    const all = sessionList();
    for (const [id, value] of pendingStates) { const target = all.find((x) => x.id === id); if (target) { target.state = value.state; target.lastStateAt = value.at; } }
    pendingStates.clear();
    write(SESSIONS, all);
  }, 2000);
}
function broadcastState(session) { for (const overlay of overlays(session.id)) send(overlay, { type: "game_state", state: session.state || {}, updatedAt: session.lastStateAt }); }
function dispatchNext(session) { if (session.inFlight || !session.queue?.length) return; const socket = gameSocket(session.id); if (!socket) return; const command = session.queue.shift(); session.inFlight = command; saveSession(session); pending.set(command.id, setTimeout(() => { const fresh = getSession(session.id); if (!fresh?.inFlight || fresh.inFlight.id !== command.id) return; fresh.inFlight = null; saveSession(fresh); log("error", "Game ACK timeout", { sessionId: fresh.id, commandId: command.id, action: command.action }); dispatchNext(fresh); }, 5000)); send(socket, command); for (const overlay of overlays(session.id)) send(overlay, { type: "game_effect", action: command.action, params: command.params }); log("info", "Game command dispatched", { sessionId: session.id, commandId: command.id, action: command.action }); }
function enqueue(session, command) { session = getSession(session.id) || session; if (!gameSocket(session.id)) return false; if ((session.queue?.length || 0) >= 100) { log("error", "Game queue full", { sessionId: session.id }); return false; } session.queue ||= []; session.queue.push(command); saveSession(session); dispatchNext(session); return true; }
function manifestFor(gameId) { return games().find((g) => g.id === gameId); }
function engineFor(sessionId) { if (!engines.has(sessionId)) engines.set(sessionId, new LiveRuleEngine()); return engines.get(sessionId); }
function forwardLiveEvent(session, manifest, event, mapped) { const wanted = manifest?.liveEvents; if (!Array.isArray(wanted) || !wanted.includes(event.type)) return; const raw = publicEvent(event); if (event.type === "gift" && typeof mapped === "boolean") raw.mapped = mapped; const payload = { type: "live_event", event: raw }; send(gameSocket(session.id), payload); for (const overlay of overlays(session.id)) send(overlay, payload); }
let statsTimer = null;
function publishStats(sessionId) { if (statsTimer) return; statsTimer = setTimeout(() => { statsTimer = null; const engine = engines.get(sessionId); if (engine) publish("live_stats", { sessionId, stats: engine.stats }); }, 500); }
function processEvent(input) {
  const event = normalizeEvent(input);
  if (!input.sessionId) throw new Error("Event requires sessionId");
  if (seenEvents.has(event.eventId)) return { event, duplicate: true, matches: [] };
  seenEvents.set(event.eventId, Date.now());
  if (seenEvents.size > 5000) seenEvents.delete(seenEvents.keys().next().value);
  const session = getSession(input.sessionId);
  if (!session || session.status !== "live") throw new Error("Live session not found");
  const manifest = manifestFor(session.gameId), engine = engineFor(session.id);
  if (listener.safeMode && event.source.mode !== "test") {
    forwardLiveEvent(session, manifest, event, false);
    publishStats(session.id);
    return { event, sessionId: session.id, duplicate: false, matches: [{ status: "safe_mode" }] };
  }
  const rules = read(MAPS).filter((r) => r.gameId === session.gameId);
  const { commands, results } = engine.evaluate(event, rules, { actionFor: (rule) => manifest?.actions?.find((a) => a.id === rule.action) });
  // Quà thô đến game trước action để người tặng được vào chỗ, nhưng sau khi đã biết có lệnh hay không.
  forwardLiveEvent(session, manifest, event, commands.length > 0);
  const statusById = new Map(commands.map((command) => [command.id, enqueue(session, command) ? "queued" : "game_offline"]));
  const matches = results.map((r) => r.commandId ? { ...r, status: statusById.get(r.commandId) } : r);
  publishStats(session.id);
  if (commands.length || event.type === "gift" || event.type === "follow") log("info", "Live event received", { sessionId: session.id, eventId: event.eventId, type: event.type, user: event.user.nickname, matches: matches.map((m) => ({ ruleId: m.ruleId, action: m.action, status: m.status, times: m.times })) });
  return { event, sessionId: session.id, duplicate: false, matches };
}
function validMapping(rule) { const manifest = manifestFor(rule?.gameId); if (!manifest) throw new Error("Game not found"); const valid = validateRule(rule, manifest); return { ...valid, id: valid.id || crypto.randomUUID(), updatedAt: new Date().toISOString() }; }
function body(req) { return new Promise((resolve, reject) => { let raw = ""; req.on("data", (c) => { raw += c; if (raw.length > 100000) reject(new Error("Request too large")); }); req.on("end", () => { try { resolve(raw ? JSON.parse(raw) : {}); } catch { reject(new Error("Invalid JSON")); } }); }); }
function json(res, status, value) { res.writeHead(status, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" }); res.end(JSON.stringify(value)); }
function staticFile(res, requestPath) { const gameMatch = requestPath.match(/^\/game\/([^/]+)\/?(.*)$/), assetMatch = requestPath.match(/^\/assets\/(.*)$/), sdkMatch = requestPath.match(/^\/sdk\/(.*)$/); const root = gameMatch ? path.join(ROOT, "Game", gameMatch[1]) : assetMatch ? path.join(DATA, "assets") : sdkMatch ? path.join(ROOT, "sdk") : PUBLIC; const rel = gameMatch ? (gameMatch[2] || "index.html") : assetMatch ? assetMatch[1] : sdkMatch ? sdkMatch[1] : (requestPath === "/" ? "index.html" : requestPath.replace(/^\/+/, "")); const file = path.resolve(root, rel); if (file !== root && !file.startsWith(root + path.sep)) return json(res, 403, { error: "Forbidden" }); fs.readFile(file, (e, data) => { if (e) return json(res, 404, { error: "Not found" }); const ext = path.extname(file); res.writeHead(200, { ...([".html", ".js", ".css", ".json"].includes(ext) ? { "Cache-Control": "no-cache" } : {}), "Content-Type": ({ ".html": "text/html", ".css": "text/css", ".js": "text/javascript", ".json": "application/json", ".svg": "image/svg+xml", ".gif": "image/gif", ".mp3": "audio/mpeg", ".wav": "audio/wav", ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".webp": "image/webp" })[ext] || "application/octet-stream" }); res.end(data); }); }
function listenerStatus() { return { state: listener.state, sessionId: listener.sessionId, attempts: listener.attempts, safeMode: listener.safeMode, lastError: listener.lastError }; }
function scheduleReconnect() { if (!listener.sessionId || listener.attempts >= 3) return; const delay = [3000, 8000, 20000][listener.attempts++]; listener.state = "reconnecting"; listener.retryTimer = setTimeout(() => startListener(listener.sessionId), delay); log("warn", "Listener reconnect scheduled", { delay, attempt: listener.attempts }); }
function pythonCommand() { const bundled = path.join(ROOT, "..", "tiktoklive-demo", ".venv", "Scripts", "python.exe"); return process.env.TIKTOOL_PYTHON || (fs.existsSync(bundled) ? bundled : "python"); }
// Bản cài đặt có sẵn tiktok-adapter.exe (PyInstaller, scripts/build-adapter.ps1) nên máy người dùng không cần Python.
// Adapter TikTok đóng gói (.exe): launcher truyền TIKTOOL_ADAPTER; chạy hub riêng (npm start) thì tự tìm ở vendor/tiktok-adapter.
function adapterCommand(username) { const exe = process.env.TIKTOOL_ADAPTER || path.join(ROOT, "vendor", "tiktok-adapter", "tiktok-adapter.exe"); return exe && fs.existsSync(exe) ? [exe, [username]] : [pythonCommand(), [path.join(ROOT, "live_adapter.py"), username]]; }
function handleAdapterMessage(sessionId, message) { const session = getSession(sessionId); if (!session || listener.sessionId !== sessionId) return; if (message.type === "connected") { listener.state = "connected"; listener.attempts = 0; session.roomId = String(message.roomId || session.roomId); saveSession(session); log("info", "TikTokLive Python adapter connected", { sessionId, roomId: session.roomId }); } else if (message.type === "catalog") { const count = saveGiftCatalog(message.gifts); log("info", "TikTok gift catalog saved", { count }); } else if (message.type === "gift") { if (message.imageUrl) saveGiftCatalog([{ id: message.giftId, name: message.giftName, diamondCount: message.diamondCount, imageUrl: message.imageUrl }]); try { processEvent({ ...message, sessionId, type: "gift", source: { mode: "tiktok" } }); } catch (e) { log("error", "TikTok gift processing failed", { error: e.message }); } } else if (["like", "comment", "follow", "share"].includes(message.type)) { try { processEvent({ ...message, sessionId, source: { mode: "tiktok" } }); } catch (e) { log("error", "TikTok event processing failed", { type: message.type, error: e.message }); } } else if (message.type === "error") { listener.state = "error"; listener.lastError = message.message; log("error", "TikTokLive Python adapter error", { error: message.message }); } else if (message.type === "disconnected" && listener.process) { listener.state = "disconnected"; scheduleReconnect(); } publish("listener", listenerStatus()); }
// Nhớ "đang kết nối LIVE" vào file: hub khởi động lại (cập nhật, lỗi...) thì tự kết nối lại, không mất người xem thật.
const LISTENER_FILE = path.join(DATA, "listener.json");
function rememberListener(sessionId) { try { write(LISTENER_FILE, { sessionId: sessionId || null, at: new Date().toISOString() }); } catch {} }
function resumeListener() {
  let saved; try { saved = JSON.parse(fs.readFileSync(LISTENER_FILE, "utf8")); } catch { return; }
  const session = saved?.sessionId && getSession(saved.sessionId);
  if (!session || session.status !== "live") return rememberListener(null);
  log("info", "Tự kết nối lại TikTok LIVE sau khi hub khởi động lại", { sessionId: session.id, username: session.username });
  startListener(session.id).catch((e) => { listener.state = "error"; listener.lastError = e.message; log("error", "Listener connect failed", { error: e.message }); });
}
async function startListener(sessionId) { const session = getSession(sessionId); if (!session) throw new Error("Session not found"); await stopListener(); clearTimeout(listener.retryTimer); listener.sessionId = sessionId; listener.state = "connecting"; listener.lastError = null; const child = listener.process = spawn(...adapterCommand(session.username.replace(/^@/, "")), { cwd: DATA, windowsHide: true }); let buffer = ""; child.stdout.on("data", (chunk) => { buffer += chunk; const lines = buffer.split(/\r?\n/); buffer = lines.pop(); for (const line of lines) try { handleAdapterMessage(sessionId, JSON.parse(line)); } catch { log("warn", "Invalid Python adapter message"); } }); child.stderr.on("data", (chunk) => log("warn", "TikTokLive adapter stderr", { error: String(chunk).trim() })); child.on("error", (e) => { listener.state = "error"; listener.lastError = e.message; publish("listener", listenerStatus()); }); child.on("exit", () => { if (listener.process !== child) return; listener.process = null; if (listener.state !== "idle") { listener.state = "disconnected"; publish("listener", listenerStatus()); scheduleReconnect(); } }); publish("listener", listenerStatus()); }
async function stopListener() { clearTimeout(listener.retryTimer); const child = listener.process; listener.process = null; listener.sessionId = null; listener.state = "idle"; listener.attempts = 0; if (child && !child.killed) child.kill(); publish("listener", listenerStatus()); }

const app = http.createServer(async (req, res) => { const url = new URL(req.url, `http://${req.headers.host}`); try {
  if (req.method === "GET" && url.pathname === "/api/games") return json(res, 200, games());
  // Điểm người chơi theo tuần (game lưu vào đây thay cho localStorage). /api/games/<gameId>/players|weeks|top.csv
  { const m = url.pathname.match(/^\/api\/games\/([a-z0-9_-]+)\/(players|weeks|top\.csv)$/i); if (m) { const [, gameId, what] = m, scope = url.searchParams.get("scope") || "live", week = url.searchParams.get("week");
    try {
      if (what === "players" && req.method === "GET") return json(res, 200, players.getWeek(gameId, scope, week));
      if (what === "players" && req.method === "POST") { const input = await body(req); return json(res, 200, players.saveWeek(gameId, input.scope || scope, input.week || week, input.members, { replace: !!input.replace })); }
      if (what === "weeks" && req.method === "GET") return json(res, 200, players.weeks(gameId, scope));
      if (what === "top.csv" && req.method === "GET") { const label = games().find((g) => g.id === gameId)?.weeklyBoard?.scoreLabel || "Điểm", out = players.topCsv(gameId, scope, week, { limit: Number(url.searchParams.get("limit")) || 0, scoreLabel: label }); res.writeHead(200, { "Content-Type": "text/csv; charset=utf-8", "Content-Disposition": `attachment; filename=top-${gameId}-${out.week}.csv`, "Cache-Control": "no-cache" }); return res.end(out.csv); }
    } catch (e) { return json(res, e.status || 500, { error: e.message }); } } }
  if (req.method === "GET" && url.pathname === "/api/tts/status") return json(res, 200, { available: tts.available, voice: tts.voice });
  if (req.method === "POST" && url.pathname === "/api/tts") { const input = await body(req); const started = Date.now(); try { const wav = await tts.speak(input.text); log("info", "Giọng AI đã đọc", { text: String(input.text || "").slice(0, 80), ms: Date.now() - started }); res.writeHead(200, { "Content-Type": "audio/wav", "Cache-Control": "no-store" }); return res.end(wav); } catch (e) { log("error", "Giọng AI lỗi", { error: e.message }); return json(res, tts.available ? 500 : 503, { error: e.message }); } }
  if (req.method === "GET" && url.pathname === "/api/status") return json(res, 200, { gameGateway: `ws://127.0.0.1:${GAME_PORT}`, connectedGames: [...sockets].filter((x) => x.role === "game").map((x) => x.sessionId), sessions: sessionList().filter((x) => x.status === "live").map(sessionPublic), listener: listenerStatus() });
  if (req.method === "GET" && url.pathname === "/api/sessions") return json(res, 200, sessionList().map(sessionPublic));
  if (req.method === "POST" && url.pathname === "/api/sessions") { const input = await body(req), game = games().find((x) => x.id === input.gameId); if (sessionList().some((x) => x.status === "live")) return json(res, 409, { error: "Stop the current live session before starting another" }); if (!game) return json(res, 404, { error: "Game not found" }); const id = crypto.randomUUID(), token = crypto.randomBytes(24).toString("base64url"), base = game.launch.url; const session = { id, token, gameId: game.id, username: String(input.username || "local-demo"), roomId: String(input.roomId || "local-demo"), status: "live", createdAt: new Date().toISOString(), queue: [], state: {} }; session.gameUrl = `${base}?session=${id}&token=${token}`; session.overlayUrl = `${base}?overlay=1&session=${id}&token=${token}`; const all = sessionList(); all.push(session); write(SESSIONS, all); log("info", "Live session created", { sessionId: id, gameId: game.id }); return json(res, 201, sessionPublic(session)); }
  if (req.method === "POST" && url.pathname.match(/^\/api\/sessions\/[^/]+\/stop$/)) { const id = url.pathname.split("/")[3], session = getSession(id); if (!session) return json(res, 404, { error: "Session not found" }); session.status = "stopped"; session.queue = []; session.inFlight = null; saveSession(session); engines.delete(id); log("info", "Live session stopped", { sessionId: id }); return json(res, 200, sessionPublic(session)); }
  if (req.method === "PATCH" && url.pathname.match(/^\/api\/sessions\/[^/]+$/)) { const id = url.pathname.split("/")[3], session = getSession(id); if (!session) return json(res, 404, { error: "Session not found" }); const input = await body(req); if (input.username != null) session.username = String(input.username).trim().replace(/^@/, ""); saveSession(session); return json(res, 200, sessionPublic(session)); }
  if (req.method === "GET" && url.pathname.match(/^\/api\/sessions\/[^/]+\/stats$/)) { const id = url.pathname.split("/")[3], session = getSession(id); if (!session) return json(res, 404, { error: "Session not found" }); const engine = engineFor(id), likes = read(MAPS).filter((r) => r.gameId === session.gameId && r.eventType === "like" && r.likeScope !== "user").map((r) => ({ ruleId: r.id, ...engine.likeProgress(normalizeRule(r)) })); return json(res, 200, { sessionId: id, stats: engine.stats, followers: engine.followers.size, likeProgress: likes }); }
  if (req.method === "GET" && url.pathname === "/api/logs") return json(res, 200, db.prepare("SELECT id, at, level, message, details FROM event_log ORDER BY at DESC LIMIT 300").all().map((x) => ({ ...x, details: JSON.parse(x.details) })));
  if (req.method === "GET" && url.pathname === "/api/logs.csv") { const rows = db.prepare("SELECT at, level, message, details FROM event_log ORDER BY at DESC").all(); res.writeHead(200, { "Content-Type": "text/csv; charset=utf-8", "Content-Disposition": "attachment; filename=tiktool-log.csv" }); return res.end("at,level,message,details\n" + rows.map((x) => [x.at,x.level,x.message,x.details].map((v) => `\"${String(v).replaceAll('\"','\"\"')}\"`).join(",")).join("\n")); }
  if (req.method === "POST" && url.pathname.match(/^\/api\/sessions\/[^/]+\/listener\/connect$/)) { const id = url.pathname.split("/")[3]; rememberListener(id); startListener(id).catch((e) => { listener.state = "error"; listener.lastError = e.message; log("error", "Listener connect failed", { error: e.message }); }); return json(res, 202, listenerStatus()); }
  if (req.method === "POST" && url.pathname === "/api/listener/disconnect") { rememberListener(null); await stopListener(); return json(res, 200, listenerStatus()); }
  if (req.method === "POST" && url.pathname === "/api/safe-mode") { listener.safeMode = Boolean((await body(req)).enabled); log("warn", listener.safeMode ? "Safe mode enabled" : "Safe mode disabled"); return json(res, 200, listenerStatus()); }
  if (req.method === "GET" && url.pathname === "/api/stream") { res.writeHead(200, { "Content-Type": "text/event-stream", "Cache-Control": "no-cache", Connection: "keep-alive" }); dashboardClients.add(res); req.on("close", () => dashboardClients.delete(res)); return; }
  if (req.method === "GET" && url.pathname === "/api/mappings") { const gameId = url.searchParams.get("gameId"), prices = new Map(db.prepare("SELECT gift_id AS giftId, diamonds FROM gift_catalog").all().map((gift) => [String(gift.giftId), Number(gift.diamonds) || 0])), mappings = (gameId ? read(MAPS).filter((x) => x.gameId === gameId) : read(MAPS)).map((mapping) => ({ ...mapping, giftDiamonds: mapping.giftDiamonds ?? prices.get(String(mapping.giftId)) ?? null })); return json(res, 200, mappings); }
  if (req.method === "GET" && url.pathname === "/api/gift-library") { const gifts = new Map(); for (const rule of read(MAPS)) { if (rule.eventType !== "gift" || !rule.giftId) continue; if (!gifts.has(rule.giftId)) gifts.set(rule.giftId, { giftId: rule.giftId, giftName: rule.giftName || rule.giftId, giftImage: rule.giftImage || "" }); else if (!gifts.get(rule.giftId).giftImage && rule.giftImage) gifts.get(rule.giftId).giftImage = rule.giftImage; } return json(res, 200, [...gifts.values()].sort((a, b) => a.giftName.localeCompare(b.giftName, "vi"))); }
  if (req.method === "GET" && url.pathname === "/api/gift-catalog") return json(res, 200, db.prepare("SELECT gift_id AS giftId, name, diamonds, image_url AS imageUrl, last_seen_at AS lastSeenAt FROM gift_catalog ORDER BY name").all());
  if (req.method === "POST" && url.pathname === "/api/assets") { const input = await body(req), match = String(input.dataUrl || "").match(/^data:image\/(png|jpeg|webp);base64,([A-Za-z0-9+/=]+)$/); if (!match) return json(res, 400, { error: "Chỉ hỗ trợ ảnh PNG, JPG hoặc WebP" }); const ext = match[1] === "jpeg" ? "jpg" : match[1], name = `${Date.now()}-${crypto.randomUUID().slice(0, 8)}.${ext}`; fs.writeFileSync(path.join(ASSETS, name), Buffer.from(match[2], "base64")); return json(res, 201, { url: `/assets/${name}`, name }); }
  if (req.method === "POST" && url.pathname === "/api/mappings") { const rule = validMapping(await body(req)), all = read(MAPS), duplicate = duplicateMappingMessage(all, rule); if (duplicate) return json(res, 409, { error: duplicate }); all.push(rule); write(MAPS, all); return json(res, 201, rule); }
  const mapMatch = url.pathname.match(/^\/api\/mappings\/([^/]+)$/); if (mapMatch && req.method === "PATCH") { const all = read(MAPS), index = all.findIndex((x) => x.id === mapMatch[1]); if (index < 0) return json(res, 404, { error: "Mapping not found" }); const rule = validMapping({ ...all[index], ...await body(req), id: all[index].id }), duplicate = duplicateMappingMessage(all, rule, rule.id); if (duplicate) return json(res, 409, { error: duplicate }); all[index] = rule; write(MAPS, all); return json(res, 200, all[index]); } if (mapMatch && req.method === "DELETE") { const all = read(MAPS), next = all.filter((x) => x.id !== mapMatch[1]); if (all.length === next.length) return json(res, 404, { error: "Mapping not found" }); write(MAPS, next); return json(res, 204, {}); }
  if (req.method === "POST" && url.pathname === "/api/events") return json(res, 202, processEvent(await body(req)));
  if (req.method === "POST" && url.pathname === "/api/test") { const input = await body(req), session = getSession(input.sessionId) || sessionList().find((x) => x.status === "live" && x.gameId === input.gameId); if (!session) return json(res, 409, { error: "Start a live session for this game first" }); const command = { id: crypto.randomUUID(), type: "game_action", action: input.action, params: input.params || {}, source: { mode: "test" }, createdAt: new Date().toISOString() }; if (!enqueue(session, command)) return json(res, 409, { error: "Authoritative game is offline" }); const fresh = getSession(session.id); return json(res, 202, { commandId: command.id, status: "queued", queuePending: fresh?.queue?.length || 0 }); }
  if (req.method === "GET") return staticFile(res, url.pathname); json(res, 404, { error: "Not found" });
} catch (e) { log("error", "Request failed", { message: e.message }); json(res, 400, { error: e.message }); } });

const gateway = http.createServer(); const wss = new WebSocketServer({ server: gateway });
wss.on("connection", (ws) => { sockets.add(ws); ws.isAlive = true; ws.on("pong", () => { ws.isAlive = true; }); ws.on("message", (raw) => { try { const msg = JSON.parse(raw); if (msg.type === "game_hello") { const session = getSession(msg.sessionId); if (!session || session.token !== msg.token || session.gameId !== msg.gameId || session.status !== "live") return ws.close(4003, "Unauthorized session"); ws.sessionId = session.id; ws.role = msg.role === "overlay" ? "overlay" : "game"; if (ws.role === "game") { const existing = gameSocket(session.id); if (existing && existing !== ws) return ws.close(4009, "Only one authoritative game is allowed"); send(ws, { type: "game_hello_ack", role: "game", sessionId: session.id }); log("info", "Authoritative game connected", { sessionId: session.id }); dispatchNext(session); } else { send(ws, { type: "game_hello_ack", role: "overlay", sessionId: session.id }); send(ws, { type: "game_state", state: session.state || {}, updatedAt: session.lastStateAt || null }); log("info", "Overlay mirror connected", { sessionId: session.id }); } return; } const session = getSession(ws.sessionId); if (!session || ws.role !== "game") return; if (msg.type === "game_state") { session.state = msg.state || {}; session.lastStateAt = new Date().toISOString(); broadcastState(session); persistStateSoon(session.id, session.state, session.lastStateAt); } if (msg.type === "command_result" && session.inFlight?.id === msg.commandId) { clearTimeout(pending.get(msg.commandId)); pending.delete(msg.commandId); const latencyMs = Date.now() - new Date(session.inFlight.createdAt).getTime(); log(msg.status === "success" ? "info" : "error", "Game command result", { sessionId: session.id, commandId: msg.commandId, status: msg.status, latencyMs, error: msg.error }); session.inFlight = null; saveSession(session); dispatchNext(session); } } catch { ws.close(1003, "Invalid message"); } }); ws.on("close", () => { sockets.delete(ws); if (ws.role === "game") log("warn", "Authoritative game disconnected", { sessionId: ws.sessionId }); }); });
setInterval(() => wss.clients.forEach((ws) => { if (!ws.isAlive) return ws.terminate(); ws.isAlive = false; ws.ping(); }), 25000).unref();
gateway.listen(GAME_PORT, "127.0.0.1", () => log("info", "Game gateway ready", { port: GAME_PORT })); app.listen(PORT, "127.0.0.1", () => { setTimeout(resumeListener, 500); return log("info", "TikTool dashboard ready", { url: `http://127.0.0.1:${PORT}` }); });

// Launcher (desktop.js) chạy hub ngay trong main process: gọi shutdown() khi thoát để tắt adapter TikTok.
module.exports = { shutdown: () => { stopListener(); tts.stop(); } };
