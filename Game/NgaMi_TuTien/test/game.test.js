const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const DEFAULTS = require("../src/config");
const { Game, ACTION_IDS } = require("../src/game");
const { cleanName } = require("../src/cultivation");

function memoryStorage() {
  const store = new Map();
  return { get: (k) => store.get(k) ?? null, set: (k, v) => store.set(k, JSON.parse(JSON.stringify(v))) };
}

const WED = new Date(2026, 10, 11, 10, 0, 0).getTime();   // thứ Tư 11/11/2026 10:00 (giờ máy)
function setup({ storage = memoryStorage(), start = WED, tauHoa = false, kiep = false, linhKhi = false } = {}) {
  let now = start;
  const shown = [], spoken = [];
  const speaker = { available: () => true, cancel() {}, speak: (t) => { spoken.push(t); return Promise.resolve(true); } };
  const game = new Game(DEFAULTS, { now: () => now, show: (i) => shown.push(i), speaker, storage });
  game.applySettings({ voice: true, duel: true, tauHoa, kiep, linhKhi });   // test cũ cộng nhiều tu vi 1 lần: tắt tẩu hỏa trừ khi test cần
  game.enter();
  return { game, shown, spoken, storage, advance: (ms) => { now += ms; game.tick(now); }, clock: () => now };
}
const src = (id, nickname = id, eventType = "gift") => ({ eventType, user: { id, nickname } });
// Thả tim = tham gia (sự kiện thô TikTool gửi kèm mỗi lượt tim).
const join = (game, id, name) => game.handleLiveEvent({ type: "like", user: { id, nickname: name || id }, like: { count: 1 } });
const tu = (game, id, amount, name) => game.handleAction("tu_luyen", { amount }, src(id, name));
const phe = (game, id, side) => game.handleAction("vao_phe", { side }, src(id, id, "comment"));
const comment = (game, id, text) => game.handleAction("binh_luan", {}, { eventType: "comment", user: { id, nickname: id }, comment: { text } });

test("manifest khai báo đúng các action game xử lý", () => {
  const manifest = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "manifest.json"), "utf8"));
  assert.deepEqual(manifest.actions.map((a) => a.id).sort(), [...ACTION_IDS].sort());
  assert.deepEqual(manifest.liveEvents.sort(), ["gift", "like"]);
});

test("manifest.defaultMappings hợp lệ với rule engine TikTool", () => {
  const { validateRule } = require("../../../core/live-events");
  const manifest = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "manifest.json"), "utf8"));
  const rules = manifest.defaultMappings.map((r) => validateRule({ ...r, gameId: manifest.id }, manifest));
  const byAction = (a) => rules.filter((r) => r.action === a);
  assert.equal(byAction("nhap_mon").length, 0, "đã bỏ lệnh comment tham gia");
  assert.equal(byAction("binh_luan")[0].matchMode, "any");
  assert.ok(byAction("vao_phe")[0].priority > byAction("binh_luan")[0].priority, "comment tự do phải có priority thấp nhất");
  // Tim và quà đều dùng tu_luyen với số tu vi đặt riêng cho từng mapping (quà chọn theo giftId, như các game khác).
  assert.deepEqual(byAction("tu_luyen").map((r) => r.eventType).sort(), ["gift", "like"]);
  assert.equal(byAction("tu_luyen").find((r) => r.eventType === "gift").giftId, "5655");
});


test("thả tim hoặc tặng quà là tham gia; comment không làm người mới tham gia", () => {
  const { game } = setup();
  assert.equal(game.handleLiveEvent({ type: "comment", user: { id: "c" }, comment: { text: "x" } }), false);
  assert.equal(game.sect.isSeated("c"), false);
  assert.equal(game.handleLiveEvent({ type: "like", user: { id: "a" }, like: { count: 100 } }), true);
  assert.equal(game.handleLiveEvent({ type: "gift", user: { id: "b" }, gift: { id: "5655", count: 1 } }), true);
  assert.deepEqual(game.getState().seats.map((x) => x.id).sort(), ["a", "b"]);
  assert.equal(game.sect.members.get("a").tuvi, 0, "sự kiện thô không cộng tu vi");
  // Mapping tim/quà tới trước sự kiện thô: tu_luyen tự cho vào chỗ.
  assert.equal(tu(game, "d", 15), true);
  tu(game, "d", 35);
  assert.equal(game.sect.members.get("d").tuvi, 50);
});

test("comment tự do hiện bóng thoại cho người đang ngồi", () => {
  const { game } = setup();
  join(game, "a");
  comment(game, "a", "chào cả nhà 🐶");
  assert.equal(game.getState().seats[0].bubble.text, "chào cả nhà");
  assert.throws(() => comment(game, "nguoi-la", "hello"), /chưa tham gia/);
});

test("quà: cộng đúng số tu vi đặt trong mapping, TikTool đã nhân sẵn theo combo", () => {
  const { game } = setup();
  join(game, "a");
  game.handleAction("tu_luyen", { amount: 150 }, { eventType: "gift", user: { id: "a" }, gift: { id: "5655", count: 3, diamonds: 1 } });
  assert.equal(game.sect.members.get("a").tuvi, 150);
});

test("im 2 phút thì mờ, thêm 3 giây thì rời chỗ; tương tác thì giữ chỗ", () => {
  const { game, advance } = setup();
  assert.equal(DEFAULTS.idleMs, 120000);
  join(game, "a"); join(game, "b");
  advance(DEFAULTS.idleMs - 1000);
  assert.equal(game.getState().seats.some((s) => s.fading), false, "chưa tới 2 phút thì chưa mờ");
  game.handleLiveEvent({ type: "like", user: { id: "b" }, like: { count: 1 } });
  advance(1500);
  const seats = Object.fromEntries(game.getState().seats.map((s) => [s.id, s.fading]));
  assert.deepEqual(seats, { a: true, b: false });
  advance(DEFAULTS.fadeMs);
  assert.deepEqual(game.getState().seats.map((s) => s.id), ["b"]);
  tu(game, "a", 5); // thả tim / tặng quà lại là quay về chỗ
  assert.equal(game.sect.isSeated("a"), true);
});

test("rời chỗ chỉ là rời màn hình: ai cũng giữ nguyên tu vi (bỏ luật mất tu vi)", () => {
  const { game, advance } = setup();
  join(game, "thap"); tu(game, "thap", 3999);
  join(game, "cao"); tu(game, "cao", 4000);
  advance(DEFAULTS.idleMs + DEFAULTS.fadeMs);
  assert.equal(game.getState().seats.length, 0);
  assert.equal(game.sect.members.get("thap").tuvi, 3999);
  assert.equal(game.sect.members.get("cao").tuvi, 4000);
  tu(game, "thap", 1);
  assert.equal(game.getState().seats[0].label, "Kết Đan Sơ kỳ");
});

test("tối đa 53 người; người thứ 54 nhận thông báo hết chỗ, quà không được cộng", () => {
  const { game, shown } = setup();
  for (let i = 0; i < 53; i++) join(game, "u" + i);
  assert.equal(game.getState().seats.length, 53);
  join(game, "late", "Kiều Oanh");
  assert.equal(game.sect.isSeated("late"), false);
  assert.throws(() => tu(game, "late", 50, "Kiều Oanh"), /đủ chỗ/);
  assert.ok(shown.some((i) => i.text === "Tông môn đã đủ chỗ, hẹn đạo hữu Kiều Oanh lần tuyển sinh tiếp theo"));
});

test("tên: HIỆN đủ icon, kí tự đặc biệt, chữ nước ngoài; giọng ĐỌC chỉ phần chữ đọc được, không có thì 'đạo hữu'", () => {
  const { game, shown } = setup();
  join(game, "a", "ABC 🐶"); tu(game, "a", 400, "ABC 🐶");
  join(game, "b", "不是鸿恩"); tu(game, "b", 36000, "不是鸿恩");
  join(game, "c", "★彡 Anh 彡★");
  const a = shown.find((i) => i.kind === "level_up" && i.member.id === "a");
  assert.equal(a.text, "Chúc mừng đạo hữu ABC 🐶 đạt cảnh giới Luyện Khí tầng 7");
  assert.equal(a.speech, "Chúc mừng đạo hữu ABC đạt cảnh giới Luyện Khí tầng 7");
  const b = shown.find((i) => i.kind === "realm_up" && i.member.id === "b");
  assert.equal(b.text, "Chúc mừng đạo hữu 不是鸿恩 đột phá thành công, bước vào cảnh giới Hóa Thần Sơ kỳ");
  assert.equal(b.speech, "Chúc mừng đạo hữu đột phá thành công, bước vào cảnh giới Hóa Thần Sơ kỳ");
  assert.equal(game.getState().seats.find((s) => s.id === "b").name, "不是鸿恩");
  assert.equal(game.getState().seats.find((s) => s.id === "c").name, "★彡 Anh 彡★");
  game.flushJoins(game.now());
  const j = shown.filter((i) => i.kind === "join").pop();
  assert.match(j.text, /★彡 Anh 彡★/);
  assert.match(j.speech, /đạo hữu ABC, 不是鸿恩|đạo hữu ABC, Anh|Anh/);
  assert.doesNotMatch(j.speech, /[★彡🐶不]/u);
});

test("top 1-2-3 lên Chưởng môn / Tả / Hữu hộ pháp, người cũ bị đẩy xuống", () => {
  const { game, shown } = setup();
  ["a", "b", "c", "d"].forEach((id) => join(game, id));
  tu(game, "a", 5000); tu(game, "b", 3000); tu(game, "c", 2000); tu(game, "d", 10);
  assert.deepEqual(game.getState().seats.slice(0, 3).map((s) => [s.id, s.seat]), [["a", "Chưởng môn"], ["b", "Tả hộ pháp"], ["c", "Hữu hộ pháp"]]);
  tu(game, "d", 9000);
  const st = game.getState().seats;
  assert.deepEqual(st.map((s) => s.id), ["d", "a", "b", "c"]);
  assert.equal(st[3].seat, null);
  assert.ok(shown.some((i) => i.kind === "seat" && /d trở thành Chưởng môn/.test(i.text)));
});

test("thanh tiến độ: % tới tiểu cảnh tiếp theo", () => {
  const { game } = setup();
  join(game, "a"); tu(game, "a", 50); // tầng 2 (30) -> tầng 3 (70)
  assert.equal(game.getState().seats[0].progress, 0.5);
});

test("tỉ thí: người vào phe được đọc tên theo phe (gom lại 1 câu), kết quả đọc kèm tỉ số và tu vi thưởng", async () => {
  const { game, shown, spoken, advance } = setup();
  ["a", "b", "c"].forEach((id) => join(game, id, "Người " + id.toUpperCase()));
  game.handleAction("start_duel", {}, { mode: "test" });
  phe(game, "a", 1); phe(game, "b", 1); phe(game, "c", 2);
  advance(DEFAULTS.voice.sideJoinBatchMs);
  const sideMsg = shown.find((i) => i.kind === "side_join");
  assert.equal(sideMsg.speech, "Người A, Người B gia nhập phe Thanh Liên. Người C gia nhập phe Hồng Liên");
  assert.equal(sideMsg.silent, undefined);
  game.random = () => 0.99;   // đòn kiếm của C trúng B (B đang 0 tu vi nên không mất gì), để số tu vi của A cố định
  tu(game, "a", 500); tu(game, "c", 200);
  game.handleAction("end_duel", {}, { mode: "test" });
  const end = shown.find((i) => i.kind === "duel_end");
  assert.equal(end.speech, "Phe Thanh Liên chiến thắng với tỉ số 500 trên 200. Mỗi đạo hữu phe thắng được cộng 300 tu vi. Công đầu thuộc về đạo hữu Người A, được nhân đôi tu vi đã góp, cộng thêm 500 tu vi và một lượt quay thưởng");
  assert.equal(game.sect.members.get("a").tuvi, 500 + 500, "công đầu x2 số đã góp");
  assert.equal(game.sect.members.get("b").tuvi, 300, "người khác phe thắng +300");
  assert.equal(game.sect.members.get("c").tuvi, 200, "phe thua không bị trừ");
});

test("tỉ thí: điểm phe chỉ tính tu vi kiếm được từ lúc vào phe, không tính tu vi có sẵn", () => {
  const { game, advance } = setup();
  ["a", "b", "c", "d"].forEach((id) => join(game, id));
  tu(game, "a", 5000); tu(game, "c", 2000); // tu vi có sẵn trước trận: không tính
  game.handleAction("start_duel", {}, { mode: "test" });
  tu(game, "a", 700);                        // trong trận nhưng CHƯA vào phe: không tính
  phe(game, "a", 1); phe(game, "b", 1); phe(game, "c", 2);
  assert.deepEqual(game.duel.snapshot(0).scores, { 1: 0, 2: 0 });
  tu(game, "a", 100); tu(game, "b", 300); tu(game, "c", 500);
  tu(game, "d", 9999);                       // không chọn phe: không tính
  assert.deepEqual(game.duel.snapshot(0).scores, { 1: 400, 2: 500 });
  advance(DEFAULTS.idleMs + DEFAULTS.fadeMs); // c rời chỗ (mất tu vi) nhưng phần đã góp vẫn tính
  game.handleAction("end_duel", {}, { mode: "test" });
  assert.equal(game.lastDuelResult.winner, 2);
  assert.deepEqual(game.lastDuelResult.scores, { 1: 400, 2: 500 });
  assert.equal(game.lastDuelResult.mvp, "c");
});

test("tỉ thí: vào phe (phải đang ngồi), thưởng phe thắng, phe thua không bị trừ", () => {
  const { game, advance, shown } = setup();
  ["a", "b", "c"].forEach((id) => join(game, id));
  game.handleAction("start_duel", {}, { mode: "test" });
  phe(game, "a", 1);
  assert.throws(() => phe(game, "a", 2), /Đã ở trong phe/);
  phe(game, "b", 2);
  tu(game, "a", 100); tu(game, "b", 50); tu(game, "c", 999);
  assert.deepEqual(game.duel.snapshot(0).scores, { 1: 100, 2: 50 });
  const before = game.sect.members.get("a").tuvi;
  game.handleAction("end_duel", {}, { mode: "test" });
  assert.equal(game.sect.members.get("a").tuvi, before + 100, "công đầu: thêm đúng số đã góp (x2), không cộng +300");
  assert.equal(game.sect.members.get("b").tuvi, 50);
  assert.ok(shown.some((i) => i.kind === "duel_end" && /^Phe Thanh Liên thắng 100 – 50! Mỗi người \+300 tu vi, công đầu a x2 tu vi đã góp \(\+100\)$/.test(i.text)));
});

test("lịch: 7' tu + 3' tỉ thí (phút 7, 17, 27), phút 30 quái rồng; rồng chết mới tính lại chu kỳ", () => {
  const { game, advance } = setup();
  // Đi từng giây như game chạy thật (mỗi tick xử lý 1 bước chuyển trạng thái).
  const at = (min) => { while (game.now() - start < min * 60000) advance(1000); };
  const start = game.now();
  at(6.9); assert.equal(game.duel.phase, "idle");
  at(7.05); assert.equal(game.duel.phase, "duel");
  at(10.5); assert.equal(game.duel.phase, "idle");
  at(17.05); assert.equal(game.duel.phase, "duel");
  at(27.05); assert.equal(game.duel.phase, "duel");
  at(29.9); assert.equal(game.boss.phase, "idle");
  at(30.3); assert.equal(game.boss.phase, "fight", "phút 30 (sau màn kết quả tỉ thí) rồng xuất hiện");
  at(45); assert.equal(game.boss.phase, "fight", "đánh quái không giới hạn thời gian");
  assert.equal(game.duel.phase, "idle", "trong lúc đánh quái không có tỉ thí");
  join(game, "a"); tu(game, "a", DEFAULTS.boss.hp);
  assert.equal(game.boss.phase, "result");
  at(45.3); assert.equal(game.boss.phase, "idle");
  at(52.2); assert.equal(game.duel.phase, "duel", "rồng chết xong, 7 phút sau mới tỉ thí");
});

test("quái rồng: tu vi kiếm được trong lúc đánh = sát thương; công đầu +tu vi + 1 lượt quay", () => {
  const { game, shown, advance } = setup();
  game.applySettings({ bossHp: 1000 });
  ["a", "b"].forEach((id) => join(game, id, "Người " + id));
  game.spawnBoss(game.now());
  assert.ok(shown.some((i) => i.kind === "boss_start"));
  tu(game, "a", 300); tu(game, "b", 100);
  assert.equal(game.getState().boss.hp, 600);
  tu(game, "b", 350);   // còn 250/1000 = 25%
  assert.ok(shown.some((i) => i.kind === "boss_milestone" && /25%/.test(i.text)));
  tu(game, "a", 5000);   // vượt máu còn lại: chỉ tính phần còn lại
  const st = game.getState().boss;
  assert.equal(st.phase, "result");
  assert.deepEqual(st.top.map((t) => [t.id, t.damage]), [["a", 550], ["b", 450]]);
  const before = game.sect.members.get("a").tuvi;
  assert.equal(before, 5300 + DEFAULTS.boss.mvpBonus, "công đầu được cộng thưởng");
  assert.equal(game.getState().wheel.memberId, "a", "công đầu được quay thưởng");
  advance(DEFAULTS.wheel.spinMs + 100);
  const prize = game.getState().wheel.prize;
  assert.equal(game.sect.members.get("a").tuvi, before + prize);
  assert.ok(shown.some((i) => i.kind === "wheel_result" && i.text.endsWith(" quay trúng +" + prize.toLocaleString("vi-VN") + " tu vi!")));
});

test("vòng quay: chọn ô theo tỉ lệ; công đầu phe thắng tỉ thí cũng được quay", () => {
  const seq = [0.0, 0.999];
  let k = 0;
  const { Game } = require("../src/game");
  const g = new Game(DEFAULTS, { storage: memoryStorage(), random: () => seq[k++ % seq.length], now: () => WED });
  assert.equal(g.pickPrize(), 0);
  assert.equal(g.pickPrize(), DEFAULTS.wheel.prizes.length - 1);

  const { game } = setup();
  ["a", "b"].forEach((id) => join(game, id));
  game.handleAction("start_duel", {}, { mode: "test" });
  phe(game, "a", 1); phe(game, "b", 2);
  tu(game, "a", 500); tu(game, "b", 100);
  game.handleAction("end_duel", {}, { mode: "test" });
  assert.equal(game.getState().wheel.memberId, "a");
  assert.equal(game.getState().wheel.reason, "duel");
});

test("tắt đánh quái trong Cài đặt: không có rồng; đang đánh mà tắt thì hủy, tỉ thí chạy lại", () => {
  const { game, advance } = setup();
  game.applySettings({ boss: false });
  advance(40 * 60000);
  assert.equal(game.boss.phase, "idle");
  game.applySettings({ boss: true });
  game.spawnBoss(game.now());
  game.applySettings({ boss: false });
  assert.equal(game.boss.phase, "idle");
  assert.ok(game.duel.nextStartAt > game.now(), "lịch tỉ thí chạy lại");
});

test("im 3 giây không có thông báo: hiện luật tỉ thí kèm số giây và tu vi thưởng", () => {
  const { game, shown, advance } = setup();
  advance(3000);
  const info = shown.filter((i) => i.kind === "info");
  assert.ok(info.length >= 1);
  assert.match(info[0].text, /^Tỉ thí sẽ bắt đầu sau 6 phút 57 giây, phe thắng mỗi người \+300 tu vi$/);
  assert.equal(info[0].speech, "Còn 6 phút 57 giây nữa là tới đại hội tỉ thí. Phe thắng mỗi người được cộng 300 tu vi");
  assert.equal(info[0].silent, false, "câu nhắc đầu tiên có đọc");
});

test("câu nhắc luật: chữ hiện mỗi 3 giây, giọng chỉ đọc 1 lần mỗi 30 giây; đặt 0 thì chỉ hiện chữ", async () => {
  const { game, shown, spoken, advance } = setup();
  for (let i = 0; i < 20; i++) advance(3000);   // 60 giây rảnh
  const info = shown.filter((i) => i.kind === "info");
  assert.ok(info.length >= 15);
  assert.equal(info.filter((i) => !i.silent).length, 2, "60 giây: đọc 2 lần");
  await new Promise((r) => setImmediate(r));
  assert.ok(spoken.some((t) => /đại hội tỉ thí|nhập môn Nga Mi/.test(t)));

  game.applySettings({ infoVoiceEverySec: 0 });
  const before = shown.length;
  for (let i = 0; i < 20; i++) advance(3000);
  assert.equal(shown.slice(before).filter((i) => i.kind === "info" && !i.silent).length, 0);
});

test("trong trận: câu nhắc đọc thời gian còn lại và cách chọn phe", () => {
  const { game, shown, advance } = setup();
  game.handleAction("start_duel", {}, { mode: "test" });
  advance(4000); advance(3000);
  const info = shown.filter((i) => i.kind === "info" && !i.silent);
  assert.match(info[0].speech, /^Tỉ thí còn 2 phút \d+ giây\. Hiện tại Thanh Liên 0 điểm, Hồng Liên 0 điểm\. Bình luận số một vào phe Thanh Liên, số hai vào phe Hồng Liên\./);
});

test("tắt tỉ thí thì không có trận và không vào phe được; luật nhắc thả tim / tặng quà", () => {
  const { game, advance, shown } = setup();
  join(game, "a");
  game.applySettings({ duel: false, boss: false });
  advance(60 * 60000);
  assert.equal(game.duel.phase, "idle");
  assert.throws(() => phe(game, "a", 1), /tắt/);
  advance(3000);
  assert.ok(shown.some((i) => i.kind === "info" && i.text === "Thả tim hoặc tặng quà để nhập môn Nga Mi"));
});

test("mỗi lần thăng cấp đều có sự kiện level_up/realm_up (để phát âm thanh), kể cả khi tắt đọc tiểu cảnh", () => {
  const { game, shown } = setup();
  game.applySettings({ announceStageUps: false });
  join(game, "a");
  tu(game, "a", 30);   // Luyện Khí tầng 2
  tu(game, "a", 970);  // Trúc Cơ
  tu(game, "a", 1);    // không lên cấp
  assert.deepEqual(shown.filter((i) => i.kind === "level_up" || i.kind === "realm_up").map((i) => i.kind), ["level_up", "realm_up"]);
  assert.equal(game.settings.sfx, true);
});

test("tắt giọng nói: vẫn hiện chữ, không đọc", async () => {
  const { game, shown, spoken } = setup();
  game.applySettings({ voice: false });
  join(game, "a"); tu(game, "a", 400);
  await new Promise((r) => setImmediate(r));
  assert.ok(shown.length > 0);
  assert.equal(spoken.length, 0);
});

test("tu vi lưu theo tuần: buổi live khác trong cùng tuần vẫn giữ, sang tuần mới (0h thứ Hai) thì reset", () => {
  const { weekKey } = require("../src/game");
  assert.equal(weekKey(WED), "2026-11-09");
  assert.equal(weekKey(new Date(2026, 10, 15, 23, 59).getTime()), "2026-11-09", "Chủ nhật 23:59 vẫn là tuần cũ");
  assert.equal(weekKey(new Date(2026, 10, 16, 0, 0).getTime()), "2026-11-16", "0h thứ Hai là tuần mới");

  const storage = memoryStorage();
  const { game } = setup({ storage });
  join(game, "a", "An"); tu(game, "a", 5000, "An");
  game.exit();
  const sameWeek = new Game(DEFAULTS, { storage, now: () => new Date(2026, 10, 14, 20, 0).getTime() });   // thứ Bảy, buổi live khác
  assert.equal(sameWeek.sect.members.get("a").tuvi, 5000);
  assert.equal(sameWeek.sect.members.get("a").nickname, "An");
  const nextWeek = new Game(DEFAULTS, { storage, now: () => new Date(2026, 10, 16, 9, 0).getTime() });
  assert.equal(nextWeek.sect.members.size, 0);
});

test("đang live lúc qua 0h thứ Hai: tu vi về 0, người đang ngồi vẫn ngồi, có thông báo tuần mới", () => {
  const { game, shown, advance } = setup({ start: new Date(2026, 10, 15, 23, 59, 50).getTime() });
  join(game, "a"); tu(game, "a", 5000);
  advance(5000); join(game, "a");
  advance(6000);   // qua 0h thứ Hai
  assert.equal(game.sect.members.get("a").tuvi, 0);
  assert.equal(game.sect.isSeated("a"), true);
  assert.ok(shown.some((i) => i.kind === "week"));
});

test("dữ liệu thử (mở không qua TikTool) tách riêng dữ liệu live thật", () => {
  const storage = memoryStorage();
  const live = new Game(DEFAULTS, { storage, now: () => WED });
  live.enter(); join(live, "a"); tu(live, "a", 100); live.save();
  const demo = new Game(DEFAULTS, { storage, now: () => WED, scope: "demo" });
  assert.equal(demo.sect.members.size, 0);
});

test("ngưỡng cảnh giới tăng dần, mỗi đại cảnh giới có 1 bộ trang phục", () => {
  const all = DEFAULTS.realms.flatMap((r) => r.stages);
  for (let i = 1; i < all.length; i++) assert.ok(all[i] > all[i - 1], `ngưỡng thứ ${i} phải lớn hơn ngưỡng trước`);
  const sprites = DEFAULTS.realms.map((r) => r.sprite);
  assert.equal(new Set(sprites).size, 10);
  sprites.forEach((s) => assert.ok(fs.existsSync(path.join(__dirname, "..", "src", "assets", "game", s + ".png")), `thiếu ${s}.png`));
});

test("bố cục: 3 ghế đài + 5 hàng x 10 cột = 53 chỗ, căn giữa", () => {
  const L = DEFAULTS.layout;
  assert.equal(DEFAULTS.capacity, 53);
  assert.equal(L.rows.length, 5);
  L.rows.forEach((r) => assert.equal(r.count, 10));
  assert.equal(L.rowLeft + L.rowRight, 1080);
  assert.equal(L.podium[1].x + L.podium[2].x, 1080);
  L.rows.forEach((r) => assert.ok(r.y <= 1440, "hàng cuối phải nằm trên vùng comment TikTok (1/4 dưới)"));
});

test("lưu tên: giữ icon / kí tự đặc biệt, bỏ kí tự điều khiển ẩn, đảo chiều, dấu chồng chất", () => {
  assert.equal(cleanName("Sống chó đời nhẹ như gió 🐶"), "Sống chó đời nhẹ như gió 🐶");
  assert.equal(cleanName("不是鸿恩"), "不是鸿恩");
  assert.equal(cleanName("★彡"), "★彡");
  assert.equal(cleanName("Mikepham.19"), "Mikepham.19");
  assert.equal(cleanName("An‮h​"), "Anh");
  assert.ok(Array.from(cleanName("Ź̂̃̄̅")).length <= 3, "dấu chồng chất bị cắt bớt");
  assert.equal(cleanName("👨‍👩‍👧 gia đình"), "👨‍👩‍👧 gia đình", "emoji ghép giữ nguyên");
});

test("tẩu hỏa nhập ma: vượt ngưỡng trong cửa sổ thời gian thì bị; dưới ngưỡng thì không", () => {
  const { game, advance } = setup({ tauHoa: true });
  game.applySettings({ tauHoaTuvi: 1000, tauHoaWindowSec: 60 });
  ["a", "b"].forEach((id) => join(game, id));
  tu(game, "a", 600); advance(61000); tu(game, "a", 600);   // 2 lần cách nhau hơn 60 giây: không bị
  assert.equal(game.possession.phase, "idle");
  tu(game, "b", 600); advance(10000); tu(game, "b", 500);   // 1.100 trong 10 giây: bị
  assert.equal(game.possession.phase, "active");
  assert.equal(game.possession.member.id, "b");
  assert.ok(!game.getState().seats.some((s) => s.id === "b"), "người tẩu hỏa không ngồi trong hàng");
  assert.equal(game.getState().possession.name, "b");
});

test("tẩu hỏa: người bị tẩu hỏa tặng quà thì đánh 1 người ngẫu nhiên, người đó mất đúng số tu vi (không âm)", () => {
  const { game } = setup({ tauHoa: true });
  game.applySettings({ tauHoaTuvi: 1000 });
  ["a", "b", "c"].forEach((id) => join(game, id));
  tu(game, "a", 800); tu(game, "b", 300);
  tu(game, "c", 1000);                 // c bị tẩu hỏa
  game.random = () => 0;               // luôn chọn người đầu tiên còn tu vi
  tu(game, "c", 500);
  assert.equal(game.sect.members.get("a").tuvi, 300);
  tu(game, "c", 5000);
  assert.equal(game.sect.members.get("a").tuvi, 0, "không xuống dưới 0");
  tu(game, "c", 200);                  // a đã 0 tu vi: đánh b
  assert.equal(game.sect.members.get("b").tuvi, 100);
});

test("tẩu hỏa: mọi người góp tu vi giải cứu; cứu thành công thì được thêm đúng số đã góp (x2); xong thì 7 phút sau tỉ thí", () => {
  const { game, shown, advance } = setup({ tauHoa: true });
  game.applySettings({ tauHoaTuvi: 1000, bossHp: 2000 });
  ["a", "b", "c"].forEach((id) => join(game, id));
  tu(game, "c", 1000);
  assert.equal(game.getState().possession.maxHp, 2000, "máu dùng chung ô với rồng");
  tu(game, "a", 1500); tu(game, "b", 900);     // b chỉ tính 500 (máu còn lại)
  assert.equal(game.possession.phase, "result");
  assert.equal(game.sect.members.get("a").tuvi, 1500 + 1500);
  assert.equal(game.sect.members.get("b").tuvi, 900 + 500);
  assert.ok(shown.some((i) => i.kind === "possess_freed"));
  advance(DEFAULTS.possession.resultMs + 1000);
  assert.equal(game.possession.phase, "idle");
  assert.ok(game.getState().seats.some((s) => s.id === "c"), "được cứu thì về lại hàng");
  const t0 = game.now();
  while (game.duel.phase !== "duel" && game.now() - t0 < 10 * 60000) advance(1000);
  assert.ok(Math.abs(game.now() - t0 - 7 * 60000) <= 2000, "tỉ thí sau 7 phút");
});

test("tẩu hỏa: đang tỉ thí thì chờ trận xong mới bắt đầu; không giới hạn thời gian", () => {
  const { game, shown, advance } = setup({ tauHoa: true });
  game.applySettings({ tauHoaTuvi: 1000 });
  ["a", "b"].forEach((id) => join(game, id));
  game.handleAction("start_duel", {}, { mode: "test" });
  tu(game, "a", 1200);
  assert.equal(game.possession.phase, "idle");
  assert.ok(shown.some((i) => i.kind === "possess_warn"), "báo sắp tẩu hỏa");
  game.handleAction("end_duel", {}, { mode: "test" });
  for (let i = 0; i < 15; i++) advance(1000);    // hết màn kết quả tỉ thí
  assert.equal(game.possession.phase, "active");
  for (let i = 0; i < 40; i++) advance(60000);   // 40 phút không ai cứu
  assert.equal(game.possession.phase, "active", "không giới hạn thời gian");
  assert.equal(game.boss.phase, "idle", "không ra rồng khi đang tẩu hỏa");
  assert.equal(game.sect.isSeated("a"), true, "người tẩu hỏa không bị rời chỗ vì im lặng");
});

test("lời chào: người chơi lần đầu 'Hoan nghênh … gia nhập', người đã có trong dữ liệu 'Chào mừng … quay trở lại'", () => {
  const storage = memoryStorage();
  const first = setup({ storage });
  join(first.game, "a", "An");
  first.advance(DEFAULTS.voice.joinBatchMs);
  const greet1 = first.shown.find((i) => i.kind === "join");
  assert.equal(greet1.text, "Hoan nghênh An gia nhập Nga Mi");
  assert.equal(greet1.speech, "Hoan nghênh đạo hữu An gia nhập Nga Mi");
  first.game.exit();

  // Buổi live khác trong cùng tuần: An đã có trong dữ liệu, Bình là người mới.
  const again = setup({ storage, start: WED + 3600000 });
  join(again.game, "a", "An"); join(again.game, "b", "Bình");
  again.advance(DEFAULTS.voice.joinBatchMs);
  const greet2 = again.shown.find((i) => i.kind === "join");
  assert.equal(greet2.text, "Hoan nghênh Bình gia nhập Nga Mi · Chào mừng An quay trở lại");
  assert.equal(greet2.speech, "Hoan nghênh đạo hữu Bình gia nhập Nga Mi. Chào mừng đạo hữu An quay trở lại");
});

test("rồng đánh người chơi: mỗi 5 giây -50 tu vi 1 người ngẫu nhiên; đòn thứ 5 phun lửa trúng tất cả -50; không âm", () => {
  const { game, advance, shown } = setup();
  ["a", "b", "c"].forEach((id) => join(game, id));
  tu(game, "a", 1000); tu(game, "b", 1000); tu(game, "c", 30);
  game.random = () => 0;                          // đòn thường luôn trúng người đầu tiên đang ngồi (a)
  game.spawnBoss(game.now());
  for (let i = 0; i < 4; i++) advance(DEFAULTS.boss.attackEveryMs);   // 4 đòn thường
  assert.equal(game.sect.members.get("a").tuvi, 1000 - 4 * 50);
  assert.equal(game.sect.members.get("b").tuvi, 1000);
  advance(DEFAULTS.boss.attackEveryMs);                                 // đòn thứ 5: phun lửa
  assert.equal(game.sect.members.get("a").tuvi, 1000 - 5 * 50);
  assert.equal(game.sect.members.get("b").tuvi, 950);
  assert.equal(game.sect.members.get("c").tuvi, 0, "không xuống dưới 0");
  assert.ok(shown.some((i) => i.kind === "boss_fire"));
  const att = game.getState().boss.attacks;
  assert.equal(att[att.length - 1].kind, "fire");
  assert.equal(att[att.length - 1].hits.length, 3);
});

// ---------- Lưu tu vi tuần qua hub TikTool ----------
const { hubStore } = require("../src/game");
function fakeHub(initial = {}) {
  const weeks = { ...initial }, posts = [];   // "scope|week" -> Map(id -> row)
  let down = false;
  const fetch = async (url, opt = {}) => {
    if (down) throw new Error("hub tắt");
    const u = new URL(url, "http://hub");
    if (!opt.method || opt.method === "GET") {
      const k = u.searchParams.get("scope") + "|" + u.searchParams.get("week");
      return { ok: true, json: async () => ({ week: u.searchParams.get("week"), members: [...(weeks[k] || new Map()).values()] }) };
    }
    const b = JSON.parse(opt.body); posts.push(b);
    const k = b.scope + "|" + b.week;
    if (b.replace || !weeks[k]) weeks[k] = new Map();
    b.members.forEach((r) => weeks[k].set(r[0], r));
    return { ok: true, json: async () => ({}) };
  };
  return { fetch, posts, weeks, setDown: (v) => { down = v; } };
}
const settle = () => new Promise((r) => setTimeout(r, 0));

test("hub: dữ liệu cũ trong máy được đẩy lên hub, lần lưu sau chỉ gửi người đổi tu vi", async () => {
  const hub = fakeHub(), local = memoryStorage();
  local.set("ngami.tutien.week.live", { week: "2026-11-09", sect: { v: 3, members: [["a", "A", 500, 1], ["b", "B", 70, 2]] } });
  const store = hubStore({ gameId: "ngami-tutien", prefix: "ngami.tutien.week", fetch: hub.fetch, local });
  const { game } = setup({ storage: store });
  const loaded = await store.load(game.saveKey(), game.week);
  game.mergeSaved(loaded); game.sect.dirty = true; game.save(); await settle();
  assert.deepEqual([...hub.weeks["live|2026-11-09"].keys()].sort(), ["a", "b"]);
  tu(game, "a", 30); game.save(); await settle();
  const last = hub.posts[hub.posts.length - 1];
  assert.deepEqual(last.members.map((r) => r[0]), ["a"]);
  assert.equal(last.members[0][2], 530);
});

test("hub: dữ liệu hub được gộp vào, mỗi người lấy tu vi lớn hơn; xóa tuần thì hub cũng xóa", async () => {
  const hub = fakeHub({ "live|2026-11-09": new Map([["a", ["a", "A", 900, 1]], ["c", ["c", "C", 40, 3]]]) });
  const store = hubStore({ gameId: "ngami-tutien", prefix: "ngami.tutien.week", fetch: hub.fetch, local: memoryStorage() });
  const { game } = setup({ storage: store });
  join(game, "a"); tu(game, "a", 100);                       // vào trước khi hub trả lời
  game.mergeSaved(await store.load(game.saveKey(), game.week));
  assert.equal(game.sect.members.get("a").tuvi, 900);
  assert.equal(game.sect.members.get("c").tuvi, 40);
  game.resetAllData(); await settle();
  assert.equal(hub.weeks["live|2026-11-09"].size, 0);
});

test("hub: tắt thì vẫn lưu localStorage và tự gửi lại khi hub chạy lại", async (t) => {
  t.mock.timers.enable({ apis: ["setTimeout"] });
  const hub = fakeHub(), local = memoryStorage();
  const store = hubStore({ gameId: "ngami-tutien", prefix: "ngami.tutien.week", fetch: hub.fetch, local });
  hub.setDown(true);
  store.set("ngami.tutien.week.live", { week: "2026-11-09", sect: { v: 3, members: [["a", "A", 10, 1]] } });
  for (let i = 0; i < 5; i++) await Promise.resolve();
  assert.equal(store.status.ok, false);
  assert.ok(local.get("ngami.tutien.week.live"));
  hub.setDown(false);
  t.mock.timers.tick(5000);
  for (let i = 0; i < 5; i++) await Promise.resolve();
  assert.equal(hub.weeks["live|2026-11-09"].get("a")[2], 10);
});

test("tỉ thí: góp tu vi = 1 đòn trúng ngẫu nhiên 1 người phe kia, người trúng mất 50% tu vi của đòn; chưa vào phe thì không đánh", () => {
  const { game } = setup();
  ["a", "b", "c", "x"].forEach((id) => join(game, id));
  tu(game, "c", 1000);
  game.handleAction("start_duel", {}, { mode: "test" });
  phe(game, "a", 1); phe(game, "b", 2); phe(game, "c", 2);
  game.random = () => 0.99;
  tu(game, "a", 100);
  tu(game, "x", 100);                                   // không vào phe: chỉ tu luyện tự thân
  tu(game, "b", 50);
  const hits = game.getState().duel.hits;
  assert.equal(hits.length, 2);
  assert.deepEqual([hits[0].from, hits[0].to, hits[0].side], ["a", "c", 1]);
  assert.deepEqual([hits[1].from, hits[1].to, hits[1].side], ["b", "a", 2]);
  assert.equal(game.sect.members.get("c").tuvi, 950, "trúng đòn 100 tu vi: mất 50");
  assert.equal(hits[0].lost, 50);
  assert.equal(game.sect.members.get("a").tuvi, 75, "a góp 100, trúng đòn 50 của b: mất 25");
  assert.equal(game.duel.scores[1], 100, "điểm phe không bị trừ");
  assert.equal(game.getState().seats.find((s) => s.id === "a").duelPoints, 100);
  const seats = game.getState().seats;
  assert.equal(seats.find((s) => s.id === "x").side, 0);
  assert.equal(seats.find((s) => s.id === "b").side, 2);
});

// ---------- Bản 2: pháp bảo, linh thú, thiên kiếp, linh khí, hào quang ----------
const item = (game, id, itemId, amount, count = 1) => game.handleAction(itemId, { amount, count }, src(id));

test("pháp bảo: cộng tu vi như tu luyện, có hiệu ứng và banner; món đắt có giọng đọc", () => {
  const { game, shown } = setup();
  join(game, "a", "An");
  item(game, "a", "ban_dao", 5000);
  item(game, "a", "long_chau", 20000, 2);
  assert.equal(game.sect.members.get("a").tuvi, 25000);
  const fx = game.getState().items;
  assert.deepEqual(fx.map((f) => [f.item, f.userId, f.count]), [["ban_dao", "a", 1], ["long_chau", "a", 2]]);
  const msgs = shown.filter((i) => i.kind === "item");
  assert.equal(msgs[0].silent, true);
  assert.match(msgs[0].text, /dùng Bàn Đào! \+5\.000 tu vi/);
  assert.notEqual(msgs[1].silent, true);
  assert.match(msgs[1].text, /Long Châu x2/);
});

test("trứng: nở ra linh thú theo tỉ lệ, trùng loại thì lên sao (5/10/15%), đủ 3 sao thì được vòng quay", () => {
  const { game, advance, shown } = setup();
  join(game, "a");
  game.random = () => 0;                         // luôn nở Ngọc Thố
  item(game, "a", "linh_thu_dan", 100, 4);       // 4 trứng
  assert.equal(game.sect.members.get("a").pets.ngoc_tho, undefined, "chưa nở ngay");
  advance(DEFAULTS.pets.hatchMs + 4 * 700);
  assert.equal(game.sect.members.get("a").pets.ngoc_tho, 3, "tối đa 3 sao");
  assert.ok(game.wheel || game.wheelQueue.length, "quả thứ 4 trùng khi đã 3 sao: được vòng quay");
  assert.deepEqual(shown.filter((i) => i.kind === "hatch").length, 4);
  assert.deepEqual(game.getState().seats[0].pets, [{ id: "ngoc_tho", stars: 3 }]);
  // Ngọc Thố 3 sao: +15% tu vi
  const before = game.sect.members.get("a").tuvi;
  tu(game, "a", 1000);
  assert.equal(game.sect.members.get("a").tuvi - before, 1150);
});

test("tỉ lệ nở: Ngọc Thố 45%, Hỏa Kỳ Lân 40%, Kim Phượng 15%", () => {
  const { game } = setup();
  const pick = (r) => { game.random = () => r; return game.pickPet().id; };
  assert.equal(pick(0.44), "ngoc_tho");
  assert.equal(pick(0.46), "hoa_ky_lan");
  assert.equal(pick(0.84), "hoa_ky_lan");
  assert.equal(pick(0.86), "kim_phuong");
});

test("linh thú chiến đấu: tăng sát thương lên rồng nhưng không tăng tu vi; linh thú reset theo tuần", () => {
  const { game, advance } = setup();
  join(game, "a");
  game.sect.members.get("a").pets = { hoa_ky_lan: 2, kim_phuong: 1 };   // +15% sát thương, +5% tu vi
  game.spawnBoss(game.now());
  tu(game, "a", 1000);
  assert.equal(game.sect.members.get("a").tuvi, 1050);
  assert.equal(game.boss.maxHp - game.boss.hp, Math.floor(1050 * 1.15));
  advance(7 * 24 * 3600 * 1000);                     // sang tuần sau
  assert.deepEqual(game.sect.members.get("a").pets, {});
});

test("linh thú được lưu cùng dữ liệu tuần và đọc lại được", () => {
  const storage = memoryStorage();
  const { game } = setup({ storage });
  join(game, "a"); tu(game, "a", 100);
  game.sect.members.get("a").pets = { kim_phuong: 2 };
  game.sect.dirty = true; game.save();
  const { game: g2 } = setup({ storage });
  assert.deepEqual(g2.sect.members.get("a").pets, { kim_phuong: 2 });
});

test("Linh Khí Triều Dâng: 15-25 phút ngẫu nhiên, 60 giây tu vi x2, rồi lên lịch lần sau", () => {
  const { game, advance, shown } = setup({ linhKhi: true });
  game.applySettings({ duel: false, boss: false });
  join(game, "a");
  assert.ok(game.nextSurgeAt - game.now() >= DEFAULTS.surge.minEveryMs && game.nextSurgeAt - game.now() <= DEFAULTS.surge.maxEveryMs);
  advance(game.nextSurgeAt - game.now());
  assert.ok(shown.some((i) => i.kind === "surge_start"));
  tu(game, "a", 100);
  assert.equal(game.sect.members.get("a").tuvi, 200);
  assert.equal(game.getState().surge.multiplier, 2);
  advance(DEFAULTS.surge.durationMs);
  assert.equal(game.getState().surge, null);
  tu(game, "a", 100);
  assert.equal(game.sect.members.get("a").tuvi, 300);
});

test("Thiên kiếp: lên Nguyên Anh thì độ kiếp; đồng môn hộ pháp đủ thì thành công +5% tu vi, hộ pháp đầu +1.000", () => {
  const { game, advance, shown } = setup({ kiep: true });
  game.applySettings({ duel: false, boss: false });
  join(game, "a", "An"); join(game, "b", "Bình");
  tu(game, "a", 12000);
  assert.equal(game.kiep, null, "Kết Đan: chưa có kiếp");
  tu(game, "a", 1000);                               // 13.000 = Nguyên Anh
  advance(250);
  const k = game.kiep;
  assert.ok(k && k.targetId === "a" && k.strikes === 3);
  assert.equal(k.need, 650, "5% của 13.000");
  assert.ok(shown.some((i) => i.kind === "kiep_start"));
  tu(game, "b", 700);                                // hộ pháp
  tu(game, "a", 100);                                // chính người độ kiếp góp: không tính vào kết giới
  assert.equal(game.kiep.shield, 700);
  advance(DEFAULTS.tribulation.durationMs);
  assert.equal(game.kiep.phase, "result");
  assert.equal(game.kiep.success, true);
  assert.equal(game.kiep.log.length, 3);
  assert.equal(game.sect.members.get("a").tuvi, 13100 + Math.floor(13100 * 0.05));
  assert.equal(game.sect.members.get("b").tuvi, 700 + 1000);
  advance(DEFAULTS.tribulation.resultMs);
  assert.equal(game.kiep, null);
});

test("Thiên kiếp: kết giới không đủ thì vẫn lên cảnh giới, không thưởng, không phạt; chờ khi đang đánh quái", () => {
  const { game, advance } = setup({ kiep: true });
  game.applySettings({ duel: false });
  join(game, "a");
  game.spawnBoss(game.now());
  tu(game, "a", 13000);
  advance(250);
  assert.equal(game.kiep, null, "đang đánh quái: xếp hàng chờ");
  assert.equal(game.kiepQueue.length, 1);
  game.boss.cancel(); game.restartCycle(game.now());
  advance(250);
  assert.ok(game.kiep);
  const tuvi = game.sect.members.get("a").tuvi;
  advance(DEFAULTS.tribulation.durationMs);
  assert.equal(game.kiep.success, false);
  assert.equal(game.sect.members.get("a").tuvi, tuvi);
});

test("hào quang: có khi vừa được cộng tu vi, tắt sau 1 phút 30 giây", () => {
  const { game, advance } = setup();
  game.applySettings({ duel: false, boss: false });
  join(game, "a");
  assert.equal(game.getState().seats[0].aura, false);
  tu(game, "a", 100);
  assert.equal(game.getState().seats[0].aura, true);
  advance(89 * 1000); game.sect.touch("a");
  assert.equal(game.getState().seats[0].aura, true);
  advance(2 * 1000);
  assert.equal(game.getState().seats[0].aura, false);
});
