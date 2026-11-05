const { app, BrowserWindow, ipcMain, dialog } = require("electron");
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");

let hub;
const windows = new Map();

function createMainWindow() {
  const win = new BrowserWindow({ width: 1320, height: 860, minWidth: 1040, minHeight: 680, backgroundColor: "#f7f5ef", autoHideMenuBar: true, webPreferences: { contextIsolation: true, preload: path.join(__dirname, "preload.js") } });
  win.loadURL("http://127.0.0.1:3090");
}
function waitForHub(tries = 0) { return fetch("http://127.0.0.1:3090/api/status").then(() => createMainWindow()).catch(() => tries < 30 ? new Promise((resolve) => setTimeout(resolve, 250)).then(() => waitForHub(tries + 1)) : createMainWindow()); }

app.whenReady().then(async () => { try { await fetch("http://127.0.0.1:3090/api/status"); } catch { hub = spawn(process.execPath, [path.join(__dirname, "server.js")], { cwd: __dirname, stdio: "ignore", windowsHide: true }); } waitForHub(); });
app.on("window-all-closed", () => { if (process.platform !== "darwin") app.quit(); });
app.on("before-quit", () => hub?.kill());
ipcMain.handle("launch-game", (_, { gameId, url }) => { if (windows.has(gameId)) { windows.get(gameId).focus(); return; } const game = new BrowserWindow({ width: 1280, height: 800, minWidth: 900, minHeight: 600, title: "TikTool Game", autoHideMenuBar: true }); windows.set(gameId, game); game.on("closed", () => windows.delete(gameId)); game.loadURL(url); });
ipcMain.handle("pick-image", async () => { const result = await dialog.showOpenDialog({ title: "Chọn ảnh", properties: ["openFile"], filters: [{ name: "Images", extensions: ["png", "jpg", "jpeg", "webp"] }] }); if (result.canceled) return null; const source = result.filePaths[0], ext = path.extname(source).toLowerCase(), name = `${Date.now()}-${Math.random().toString(16).slice(2)}${ext}`, target = path.join(__dirname, "data", "assets", name); fs.mkdirSync(path.dirname(target), { recursive: true }); fs.copyFileSync(source, target); return { url: `/assets/${name}`, name: path.basename(source) }; });
