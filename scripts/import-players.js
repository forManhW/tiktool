// Gộp tu vi/điểm người chơi (bảng game_player_week) từ tiktool.db của máy khác vào DB của máy này.
// Mỗi người chơi trong cùng game + scope + tuần giữ ĐIỂM CAO HƠN; người chỉ có ở file nguồn thì được thêm vào.
// Không đụng tới gift_catalog, event_log, mapping.
//
//   node scripts/import-players.js <đường dẫn tiktool.db copy từ máy kia> [DB đích]
//
// DB đích mặc định: ./data/tiktool.db (bản dev). Tắt TikTool (hub + cửa sổ game) trước khi chạy.
const fs = require("fs");
const path = require("path");
const { DatabaseSync } = require("node:sqlite");

const source = process.argv[2], target = path.resolve(process.argv[3] || path.join(__dirname, "..", "data", "tiktool.db"));
if (!source || !fs.existsSync(source)) { console.error("Cách dùng: node scripts/import-players.js <tiktool.db nguồn> [tiktool.db đích]"); process.exit(1); }
if (path.resolve(source) === target) { console.error("File nguồn trùng file đích."); process.exit(1); }

// Sao lưu DB đích trước khi gộp.
if (fs.existsSync(target)) {
  const backup = `${target}.bak-${new Date().toISOString().replace(/[:.]/g, "-")}`;
  fs.copyFileSync(target, backup);
  console.log(`Đã sao lưu: ${backup}`);
}

const db = new DatabaseSync(target);
require("../core/player-store").createPlayerStore(db);   // tạo bảng nếu DB đích chưa có
db.prepare("ATTACH DATABASE ? AS src").run(path.resolve(source));
try {
  const hasTable = db.prepare("SELECT 1 FROM src.sqlite_master WHERE type = 'table' AND name = 'game_player_week'").get();
  if (!hasTable) throw new Error("File nguồn không có bảng game_player_week (chưa có tu vi nào được lưu).");
  const count = (sql) => Number(db.prepare(sql).get().n);
  const before = count("SELECT COUNT(*) AS n FROM main.game_player_week");
  const incoming = count("SELECT COUNT(*) AS n FROM src.game_player_week");
  db.exec("BEGIN");
  db.exec(`INSERT INTO main.game_player_week (game_id, scope, week, user_id, nickname, score, joined_at, updated_at)
    SELECT game_id, scope, week, user_id, nickname, score, joined_at, updated_at FROM src.game_player_week WHERE true
    ON CONFLICT (game_id, scope, week, user_id) DO UPDATE SET
      nickname = CASE WHEN excluded.score >= score THEN excluded.nickname ELSE nickname END,
      score = MAX(score, excluded.score),
      joined_at = MIN(joined_at, excluded.joined_at),
      updated_at = MAX(updated_at, excluded.updated_at)`);
  db.exec("COMMIT");
  const after = count("SELECT COUNT(*) AS n FROM main.game_player_week");
  console.log(`Nguồn có ${incoming} dòng. DB đích: ${before} -> ${after} dòng (thêm ${after - before} người, còn lại giữ điểm cao hơn).`);
  for (const r of db.prepare("SELECT game_id, scope, week, COUNT(*) AS n, MAX(score) AS top FROM main.game_player_week GROUP BY game_id, scope, week ORDER BY week DESC LIMIT 10").all())
    console.log(`  ${r.game_id} [${r.scope}] tuần ${r.week}: ${r.n} người, cao nhất ${r.top}`);
} catch (e) {
  try { db.exec("ROLLBACK"); } catch {}
  console.error(`Lỗi: ${e.message}`); process.exitCode = 1;
} finally {
  try { db.exec("DETACH DATABASE src"); } catch {}
  db.close();
}
