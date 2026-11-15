"use strict";
// Lưu điểm người chơi THEO TUẦN cho từng game trong SQLite của hub (data/tiktool.db).
// Mỗi tuần (tính từ 0h thứ Hai, giờ máy) là một bộ hàng riêng, nên sang tuần mới dữ liệu tuần cũ vẫn còn để xuất top.
// scope tách dữ liệu live thật ("live") với lúc mở thử ("demo").

const WEEK_RE = /^\d{4}-\d{2}-\d{2}$/;

/** Thứ Hai đầu tuần (giờ máy) của thời điểm ms, dạng YYYY-MM-DD. Khớp với weekKey của game. */
function weekKey(ms) {
  const d = new Date(ms), back = (d.getDay() + 6) % 7, m = new Date(d.getFullYear(), d.getMonth(), d.getDate() - back);
  return `${m.getFullYear()}-${String(m.getMonth() + 1).padStart(2, "0")}-${String(m.getDate()).padStart(2, "0")}`;
}

/**
 * Kỳ lưu điểm dài `weeks` tuần (game khai báo weeklyBoard.periodWeeks trong manifest, mặc định 1).
 * Kỳ tính từ thứ Hai mốc 2026-11-09, mã kỳ = thứ Hai đầu kỳ. Khớp với periodKey của game (vd. Nga Mi giữ 2 tuần).
 */
const PERIOD_ANCHOR = Date.UTC(2026, 10, 9);
function periodKey(ms, weeks = 1) {
  weeks = Math.max(1, Math.floor(weeks || 1));
  const w = weekKey(ms);
  if (weeks === 1) return w;
  const [y, m, d] = w.split("-").map(Number), mon = Date.UTC(y, m - 1, d);
  const idx = Math.floor(Math.round((mon - PERIOD_ANCHOR) / (7 * 86400000)) / weeks);
  return new Date(PERIOD_ANCHOR + idx * weeks * 7 * 86400000).toISOString().slice(0, 10);
}

function createPlayerStore(db, { now = () => Date.now(), periodWeeksFor = () => 1 } = {}) {
  db.exec(`CREATE TABLE IF NOT EXISTS game_player_week (
    game_id TEXT NOT NULL, scope TEXT NOT NULL, week TEXT NOT NULL, user_id TEXT NOT NULL,
    nickname TEXT NOT NULL, score INTEGER NOT NULL, joined_at INTEGER NOT NULL, updated_at TEXT NOT NULL,
    PRIMARY KEY (game_id, scope, week, user_id))`);
  db.exec("CREATE INDEX IF NOT EXISTS game_player_week_rank ON game_player_week (game_id, scope, week, score DESC)");
  // Cột extra (JSON, vd. linh thú của Nga Mi). DB tạo từ bản trước chưa có thì thêm vào.
  if (!db.prepare("PRAGMA table_info(game_player_week)").all().some((c) => c.name === "extra")) db.exec("ALTER TABLE game_player_week ADD COLUMN extra TEXT NOT NULL DEFAULT '{}'");

  const upsert = db.prepare(`INSERT INTO game_player_week (game_id, scope, week, user_id, nickname, score, joined_at, updated_at, extra) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    ON CONFLICT (game_id, scope, week, user_id) DO UPDATE SET nickname = excluded.nickname, score = excluded.score, joined_at = excluded.joined_at, updated_at = excluded.updated_at, extra = excluded.extra`);
  const selectWeek = db.prepare("SELECT user_id, nickname, score, joined_at, extra FROM game_player_week WHERE game_id = ? AND scope = ? AND week = ? ORDER BY score DESC, joined_at ASC");
  const clearWeek = db.prepare("DELETE FROM game_player_week WHERE game_id = ? AND scope = ? AND week = ?");
  const listWeeks = db.prepare("SELECT week, COUNT(*) AS players, MAX(score) AS topScore FROM game_player_week WHERE game_id = ? AND scope = ? GROUP BY week ORDER BY week DESC");

  const scopeOf = (s) => (s === "demo" ? "demo" : "live");
  // "current" / "previous" = kỳ này / kỳ trước theo độ dài kỳ của game.
  function weekOf(w, gameId) {
    const n = Math.max(1, Math.floor(Number(periodWeeksFor(gameId)) || 1));
    if (!w || w === "current") return periodKey(now(), n);
    if (w === "previous") return periodKey(now() - n * 7 * 86400000, n);
    if (!WEEK_RE.test(w)) throw Object.assign(new Error("Tuần không hợp lệ (dạng YYYY-MM-DD)"), { status: 400 });
    return w;
  }

  const parseExtra = (t) => { try { const v = JSON.parse(t || "{}"); return v && typeof v === "object" && !Array.isArray(v) ? v : {}; } catch { return {}; } };
  const extraText = (v) => { const t = JSON.stringify(v && typeof v === "object" && !Array.isArray(v) ? v : {}); return t.length <= 2000 ? t : "{}"; };

  /** Người chơi của 1 tuần, xếp theo điểm giảm dần. members: [id, nickname, score, joinedAt, extra] như game lưu. */
  function getWeek(gameId, scope, week) {
    const w = weekOf(week, gameId);
    return { gameId, scope: scopeOf(scope), week: w, members: selectWeek.all(gameId, scopeOf(scope), w).map((r) => [r.user_id, r.nickname, Number(r.score), Number(r.joined_at), parseExtra(r.extra)]) };
  }

  /** Ghi (thêm / cập nhật) người chơi. replace = true: xóa hết tuần đó trước (dùng khi streamer xóa dữ liệu tuần). */
  function saveWeek(gameId, scope, week, members, { replace = false } = {}) {
    const w = weekOf(week, gameId), sc = scopeOf(scope), at = new Date(now()).toISOString();
    if (!Array.isArray(members)) throw Object.assign(new Error("members phải là mảng"), { status: 400 });
    let saved = 0;
    db.exec("BEGIN");
    try {
      if (replace) clearWeek.run(gameId, sc, w);
      for (const r of members) {
        if (!Array.isArray(r) || r[0] == null || r[0] === "") continue;
        upsert.run(gameId, sc, w, String(r[0]), String(r[1] ?? r[0]).slice(0, 80), Math.max(0, Math.floor(Number(r[2]) || 0)), Math.floor(Number(r[3]) || 0), at, extraText(r[4]));
        saved++;
      }
      db.exec("COMMIT");
    } catch (e) { db.exec("ROLLBACK"); throw e; }
    return { week: w, saved };
  }

  function weeks(gameId, scope) { return listWeeks.all(gameId, scopeOf(scope)).map((r) => ({ week: r.week, players: Number(r.players), topScore: Number(r.topScore) })); }

  /** CSV top tuần (có BOM để Excel đọc đúng tiếng Việt). */
  function topCsv(gameId, scope, week, { limit = 0, scoreLabel = "Điểm" } = {}) {
    const data = getWeek(gameId, scope, week);
    const scored = data.members.filter((r) => r[2] > 0);          // người 0 điểm (vd. vừa sang tuần) không vào top
    const rows = limit > 0 ? scored.slice(0, limit) : scored;
    const q = (v) => `"${String(v).replaceAll('"', '""')}"`;
    const lines = [["Hạng", "Tên", "ID", scoreLabel].map(q).join(",")]
      .concat(rows.map((r, i) => [i + 1, r[1], r[0], r[2]].map(q).join(",")));
    return { week: data.week, csv: "﻿" + lines.join("\r\n") + "\r\n" };
  }

  return { getWeek, saveWeek, weeks, topCsv, weekOf };
}

module.exports = { createPlayerStore, weekKey, periodKey };
