const { app, BrowserWindow, ipcMain, dialog, screen, shell } = require("electron");
const fs = require("fs");
const path = require("path");

const HUB = `http://127.0.0.1:${Number(process.env.PORT || 3090)}`;
const windows = new Map();
let mainWindow, hub;

// Bản cài đặt: thư mục cài (Program Files, app.asar) chỉ đọc, nên dữ liệu (mapping, session, DB, ảnh) nằm ở %APPDATA%/TikTool.
// Chạy dev (`npm run desktop`) giữ ./data như cũ.
const DATA_DIR = app.isPackaged ? app.getPath("userData") : path.join(__dirname, "data");
const ADAPTER = app.isPackaged ? path.join(process.resourcesPath, "tiktok-adapter", "tiktok-adapter.exe") : path.join(__dirname, "vendor", "tiktok-adapter", "tiktok-adapter.exe");
process.env.TIKTOOL_DATA_DIR = DATA_DIR;
if (fs.existsSync(ADAPTER)) process.env.TIKTOOL_ADAPTER = ADAPTER;
// Dữ liệu khởi đầu (danh sách quà + ảnh quà, mapping, ảnh hiệu ứng) chỉ dùng cho bản cài; dev đã có ./data.
if (app.isPackaged) process.env.TIKTOOL_SEED_DIR = path.join(process.resourcesPath, "seed");
// Giọng đọc AI (Piper + model giọng), nằm ngoài asar vì piper.exe phải chạy như file thật.
process.env.TIKTOOL_TTS_DIR = app.isPackaged ? path.join(process.resourcesPath, "tts") : path.join(__dirname, "vendor", "tts");

// Ghi log ra file để người dùng gửi khi cần hỗ trợ: %APPDATA%/TikTool/logs/main.log
const LOG_FILE = path.join(DATA_DIR, "logs", "main.log");
fs.mkdirSync(path.dirname(LOG_FILE), { recursive: true });
try { if (fs.statSync(LOG_FILE).size > 5 * 1024 * 1024) fs.renameSync(LOG_FILE, `${LOG_FILE}.old`); } catch {}
const writeLog = (level, args) => { try { fs.appendFileSync(LOG_FILE, `${new Date().toISOString()} [${level}] ${args.map((a) => (a instanceof Error ? a.stack : typeof a === "string" ? a : JSON.stringify(a))).join(" ")}\n`); } catch {} };
for (const level of ["log", "info", "warn", "error"]) { const original = console[level].bind(console); console[level] = (...args) => { writeLog(level, args); original(...args); }; }
process.on("uncaughtException", (e) => {
  writeLog("fatal", [e]);
  if (e?.code === "EADDRINUSE") { dialog.showErrorBox("TikTool", `Cổng ${e.port || "3090/8765"} đang bị chương trình khác dùng.\nHãy tắt chương trình đó (hoặc TikTool đang mở) rồi mở lại.`); app.exit(1); }
});

if (!app.requestSingleInstanceLock()) app.quit();
app.on("second-instance", () => { if (mainWindow) { if (mainWindow.isMinimized()) mainWindow.restore(); mainWindow.focus(); } });

// Chỉ cho các cửa sổ ở lại trong hub local; link ngoài mở bằng trình duyệt mặc định.
function lockNavigation(win) {
  win.webContents.setWindowOpenHandler(({ url }) => { if (/^https?:\/\//.test(url) && !url.startsWith(HUB)) shell.openExternal(url); return { action: "deny" }; });
  win.webContents.on("will-navigate", (event, url) => { if (!url.startsWith(HUB)) { event.preventDefault(); if (/^https?:\/\//.test(url)) shell.openExternal(url); } });
}

function createMainWindow() {
  mainWindow = new BrowserWindow({ width: 1320, height: 860, minWidth: 1040, minHeight: 680, title: "TikTool", backgroundColor: "#f7f5ef", autoHideMenuBar: true, webPreferences: { contextIsolation: true, nodeIntegration: false, sandbox: true, preload: path.join(__dirname, "preload.js") } });
  lockNavigation(mainWindow);
  mainWindow.on("closed", () => { mainWindow = null; });
  mainWindow.loadURL(HUB);
}
const hubUp = () => fetch(`${HUB}/api/status`).then((r) => r.ok, () => false);
async function waitForHub(tries = 0) { if (await hubUp() || tries >= 40) return createMainWindow(); await new Promise((r) => setTimeout(r, 250)); return waitForHub(tries + 1); }

app.whenReady().then(async () => {
  writeLog("info", [`TikTool ${app.getVersion()} start, data=${DATA_DIR}, adapter=${process.env.TIKTOOL_ADAPTER || "python"}`]);
  // Hub đã chạy sẵn (vd. `npm start` khi dev) thì dùng luôn; không thì chạy hub ngay trong main process.
  if (!(await hubUp())) hub = require("./server.js");
  waitForHub();
});
app.on("window-all-closed", () => { if (process.platform !== "darwin") app.quit(); });
app.on("before-quit", () => { try { hub?.shutdown(); } catch {} });

// manifest.window = { width, height }: kích thước thiết kế của game (vd. dọc 1080x1920). Thu nhỏ giữ tỉ lệ cho vừa màn hình.
function gameWindowSize(size) {
  const w = Number(size?.width), h = Number(size?.height);
  if (!(w > 0 && h > 0)) return { width: 1280, height: 800, minWidth: 900, minHeight: 600 };
  const area = screen.getPrimaryDisplay().workAreaSize, scale = Math.min(1, (area.width * 0.95) / w, (area.height * 0.95) / h);
  return { width: Math.round(w * scale), height: Math.round(h * scale), minWidth: Math.min(360, Math.round(w * scale)), minHeight: Math.min(360, Math.round(h * scale)), useContentSize: true };
}
ipcMain.handle("launch-game", (_, { gameId, url, window: size }) => {
  if (!String(url).startsWith(HUB)) throw new Error("Game URL phải nằm trên hub local");
  if (windows.has(gameId)) { const open = windows.get(gameId); open.loadURL(url); open.focus(); return; } /* Start Game lại: nạp session mới + code game mới */
  const game = new BrowserWindow({ ...gameWindowSize(size), title: "TikTool Game", autoHideMenuBar: true, webPreferences: { contextIsolation: true, nodeIntegration: false, sandbox: true } });
  lockNavigation(game);
  windows.set(gameId, game); game.on("closed", () => windows.delete(gameId)); game.loadURL(url);
});
ipcMain.handle("pick-image", async () => { const result = await dialog.showOpenDialog({ title: "Chọn ảnh", properties: ["openFile"], filters: [{ name: "Images", extensions: ["png", "jpg", "jpeg", "webp"] }] }); if (result.canceled) return null; const source = result.filePaths[0], ext = path.extname(source).toLowerCase(), name = `${Date.now()}-${Math.random().toString(16).slice(2)}${ext}`, target = path.join(DATA_DIR, "assets", name); fs.mkdirSync(path.dirname(target), { recursive: true }); fs.copyFileSync(source, target); return { url: `/assets/${name}`, name: path.basename(source) }; });
