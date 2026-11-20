"use strict";
const path = require("path");

/**
 * Game dạng .exe (manifest.launch.type = "native"): tìm file chạy và tham số dòng lệnh.
 * Chỉ cho phép file .exe nằm trong đúng thư mục của game, để manifest không trỏ được ra ngoài.
 *
 * @param {object} o
 * @param {string} o.gameRoot   thư mục Game/ trên đĩa (bản cài: Game/ trong app.asar.unpacked)
 * @param {string} o.folder     "Game/<TênThưMục>" do hub trả về
 * @param {object} o.launch     manifest.launch
 * @param {string} o.url        Game URL của session (chứa session + token)
 * @param {number} [o.gamePort] cổng Game Gateway
 * @returns {null | { exe: string, cwd: string, args: string[] }}  null nếu không phải game native
 */
function resolveNativeLaunch({ gameRoot, folder, launch, url, gamePort = 8765 }) {
  if (!launch || launch.type !== "native") return null;
  if (!launch.exe || typeof launch.exe !== "string") throw new Error("manifest.launch.exe bị thiếu");
  const dir = path.resolve(gameRoot, path.basename(String(folder || "")));
  const exe = path.resolve(dir, launch.exe);
  const rel = path.relative(dir, exe);
  if (!rel || rel.startsWith("..") || path.isAbsolute(rel)) throw new Error("File game phải nằm trong thư mục của game");
  if (path.extname(exe).toLowerCase() !== ".exe") throw new Error("Game native phải là file .exe");
  const query = new URL(url).searchParams;
  const session = query.get("session"), token = query.get("token");
  if (!session || !token) throw new Error("Game URL thiếu session/token");
  return { exe, cwd: path.dirname(exe), args: ["--session", session, "--token", token, "--ws", `ws://127.0.0.1:${gamePort}`] };
}

module.exports = { resolveNativeLaunch };
