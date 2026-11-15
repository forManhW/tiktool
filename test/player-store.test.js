"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const { DatabaseSync } = require("node:sqlite");
const { createPlayerStore, weekKey } = require("../core/player-store");

const WED = new Date(2026, 10, 11, 20, 0).getTime();   // thứ Tư 11/11/2026

function store(at = WED) { let t = at; const s = createPlayerStore(new DatabaseSync(":memory:"), { now: () => t }); s.setNow = (v) => { t = v; }; return s; }

test("tuần tính từ thứ Hai", () => {
  assert.equal(weekKey(WED), "2026-11-09");
  assert.equal(weekKey(new Date(2026, 10, 15, 23, 59).getTime()), "2026-11-09");
  assert.equal(weekKey(new Date(2026, 10, 16, 0, 0).getTime()), "2026-11-16");
});

test("lưu thêm / cập nhật theo id, đọc ra xếp theo điểm", () => {
  const s = store();
  s.saveWeek("ngami-tutien", "live", "2026-11-09", [["a", "An", 100, 1], ["b", "Bình", 300, 2]]);
  s.saveWeek("ngami-tutien", "live", "2026-11-09", [["a", "An mới", 500, 1]]);
  const w = s.getWeek("ngami-tutien", "live", "current");
  assert.equal(w.week, "2026-11-09");
  assert.deepEqual(w.members, [["a", "An mới", 500, 1, {}], ["b", "Bình", 300, 2, {}]]);
  assert.deepEqual(s.getWeek("ngami-tutien", "demo").members, [], "demo tách riêng live");
  assert.deepEqual(s.getWeek("game-khac", "live").members, [], "game khác tách riêng");
});

test("replace xóa cả tuần; tuần cũ vẫn giữ để xuất top", () => {
  const s = store();
  s.saveWeek("g", "live", "2026-11-02", [["x", "X", 900, 0]]);
  s.saveWeek("g", "live", "2026-11-09", [["a", "A", 10, 0]]);
  s.saveWeek("g", "live", "2026-11-09", [], { replace: true });
  assert.deepEqual(s.getWeek("g", "live").members, []);
  assert.equal(s.getWeek("g", "live", "previous").members[0][2], 900);
  assert.deepEqual(s.weeks("g", "live").map((w) => w.week), ["2026-11-02"]);
});

test("CSV top tuần có BOM, hạng, tên, điểm", () => {
  const s = store();
  s.saveWeek("g", "live", null, [["a", 'Tên "lạ"', 50, 0], ["b", "B", 70, 0]]);
  const { csv, week } = s.topCsv("g", "live", "current", { scoreLabel: "Tu vi" });
  assert.equal(week, "2026-11-09");
  assert.ok(csv.startsWith("﻿"));
  const lines = csv.slice(1).trim().split("\r\n");
  assert.equal(lines[0], '"Hạng","Tên","ID","Tu vi"');
  assert.equal(lines[1], '"1","B","b","70"');
  assert.equal(lines[2], '"2","Tên ""lạ""","a","50"');
});

test("tuần sai định dạng bị từ chối", () => {
  assert.throws(() => store().getWeek("g", "live", "abc"), /không hợp lệ/);
});

test("cột extra (linh thú) lưu và đọc lại; DB cũ thiếu cột thì tự thêm", () => {
  const db = new DatabaseSync(":memory:");
  db.exec("CREATE TABLE game_player_week (game_id TEXT NOT NULL, scope TEXT NOT NULL, week TEXT NOT NULL, user_id TEXT NOT NULL, nickname TEXT NOT NULL, score INTEGER NOT NULL, joined_at INTEGER NOT NULL, updated_at TEXT NOT NULL, PRIMARY KEY (game_id, scope, week, user_id))");
  db.exec("INSERT INTO game_player_week VALUES ('g','live','2026-11-09','old','Cũ',5,0,'x')");
  const s = createPlayerStore(db, { now: () => WED });
  s.saveWeek("g", "live", null, [["a", "A", 10, 0, { kim_phuong: 2 }]]);
  const m = s.getWeek("g", "live").members;
  assert.deepEqual(m.find((r) => r[0] === "a")[4], { kim_phuong: 2 });
  assert.deepEqual(m.find((r) => r[0] === "old")[4], {});
});

test("kỳ 2 tuần: mã kỳ là thứ Hai đầu kỳ (mốc 09/11/2026); current / previous theo độ dài kỳ của game", () => {
  const { periodKey } = require("../core/player-store");
  const at = (y, m, d, h = 12) => new Date(y, m - 1, d, h).getTime();
  assert.equal(periodKey(at(2026, 11, 12), 2), "2026-11-09");
  assert.equal(periodKey(at(2026, 11, 22, 23), 2), "2026-11-09", "Chủ nhật tuần thứ 2 vẫn kỳ cũ");
  assert.equal(periodKey(at(2026, 11, 23, 0), 2), "2026-11-23", "0h thứ Hai tuần thứ 3 là kỳ mới");
  assert.equal(periodKey(at(2026, 11, 5), 2), "2026-10-26", "trước mốc vẫn chia kỳ đúng");
  assert.equal(periodKey(at(2027, 3, 30), 2), "2027-03-29", "qua năm mới, qua đổi giờ");
  assert.equal(periodKey(at(2026, 11, 18), 1), "2026-11-16", "1 tuần = như weekKey");
  const { DatabaseSync } = require("node:sqlite");
  let now = at(2026, 11, 18);
  const store = createPlayerStore(new DatabaseSync(":memory:"), { now: () => now, periodWeeksFor: (id) => (id === "ngami" ? 2 : 1) });
  assert.equal(store.weekOf("current", "ngami"), "2026-11-09");
  assert.equal(store.weekOf("previous", "ngami"), "2026-10-26");
  assert.equal(store.weekOf("current", "other"), "2026-11-16");
});
