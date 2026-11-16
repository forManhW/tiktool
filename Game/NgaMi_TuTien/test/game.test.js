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

test("quà thô chỉ cảm ơn khi mapped false, gom 5 giây và không đổi tu vi", () => {
  const { game, shown, advance } = setup();
  for (let i = 0; i < 10; i++) game.handleLiveEvent({ type: "gift", mapped: false, user: { id: "u" + i, nickname: "Tên " + i }, gift: { name: "Rose" } });
  game.handleLiveEvent({ type: "gift", mapped: true, user: { id: "mapped" }, gift: { name: "Rose" } });
  game.handleLiveEvent({ type: "gift", user: { id: "old" }, gift: { name: "Rose" } });
  advance(4999); assert.equal(shown.filter((i) => i.kind === "gift_thanks").length, 0);
  advance(1);
  const thanks = shown.filter((i) => i.kind === "gift_thanks");
  assert.equal(thanks.length, 1); assert.match(thanks[0].speech, /cùng các đạo hữu khác/);
  assert.doesNotMatch(thanks[0].speech, /tu vi/);
  assert.equal(game.sect.members.get("u0").tuvi, 0);
  advance(5000); assert.equal(shown.filter((i) => i.kind === "gift_thanks").length, 1);
});

test("tông môn đầy không cảm ơn quà thô, quà tu luyện đọc số tu vi thực", () => {
  const { game, shown, advance } = setup();
  for (let i = 0; i < game.cfg.capacity; i++) join(game, "u" + i);
  game.handleLiveEvent({ type: "gift", mapped: false, user: { id: "outside" }, gift: { name: "Rose" } });
  game.sect.members.get("u0").pets.ngoc_tho = 1;
  game.handleAction("tu_luyen", { amount: 100 }, src("u0"));
  assert.equal(game.sect.members.get("u0").tuvi, 105);
  assert.match(shown.find((i) => i.kind === "gift_train").speech, /105 tu vi/);
  advance(5000); assert.equal(shown.filter((i) => i.kind === "gift_thanks").length, 0);
});

test("lời cảm ơn linh thạch tính giá đơn vị nhân số lượng và không đoán khi thiếu giá", () => {
  const { game, shown, advance } = setup();
  game.handleLiveEvent({ type: "gift", mapped: false, user: { id: "a", nickname: "An" }, gift: { name: "Rose", count: 3, diamonds: 5 } });
  game.handleLiveEvent({ type: "gift", mapped: false, user: { id: "a", nickname: "An" }, gift: { name: "Rose", count: 2, diamonds: 5 } });
  advance(5000);
  assert.match(shown.filter((i) => i.kind === "gift_thanks").at(-1).speech, /25 linh thạch/);
  game.handleLiveEvent({ type: "gift", mapped: false, user: { id: "b", nickname: "Bình" }, gift: { count: 2, diamonds: 0 } });
  advance(5000);
  const last = shown.filter((i) => i.kind === "gift_thanks").at(-1).speech;
  assert.doesNotMatch(last, /\d+ linh thạch/);
  assert.match(last, /pháp bảo/);
});

test("lời nền hướng dẫn theo trạng thái và im khi mất hub hoặc đang kết quả", () => {
  const { game, clock } = setup();
  const lines = [];
  game.announcer.say = (item) => lines.push(item);
  game.idleTurn = 2; game.idleSince = clock() - 30000; game.tickIdle(clock());
  assert.doesNotMatch(lines.at(-1).speech, /bình luận (một|hai|1|2)/i);
  game.duel.phase = "duel";
  game.idleTurn = 2; game.idleSince = clock() - 30000; game.tickIdle(clock());
  assert.match(lines.at(-1).speech, /môn phái|Nga Mi|vào môn/);
  assert.match(lines.at(-1).speech, /một|hai/);
  for (let i = 0; i < game.cfg.capacity; i++) join(game, "full" + i);
  game.idleTurn = 2; game.idleSince = clock() - 30000; game.tickIdle(clock());
  assert.match(lines.at(-1).speech, /đủ chỗ|hết chỗ/i);
  const n = lines.length;
  game.setHubStatus("reconnecting"); game.tickIdle(clock() + 60000);
  assert.equal(lines.length, n);
  game.setHubStatus("connected"); game.duel.phase = "result"; game.idleSince = clock() - 30000; game.tickIdle(clock());
  assert.equal(lines.length, n);
});

test("lời mời pháp bảo khi rảnh không lặp trước 3 phút", () => {
  const { game, clock } = setup();
  const spoken = [];
  game.announcer.say = (item) => spoken.push(item.speech);
  let now = clock();
  function inviteAfter(ms) { now += ms; game.idleTurn = 5; game.idleSince = now - 30000; game.tickIdle(now); }
  inviteAfter(0); assert.doesNotMatch(spoken.at(-1), /pháp bảo/i);
  inviteAfter(30000); assert.match(spoken.at(-1), /pháp bảo/i);
  inviteAfter(30000); assert.doesNotMatch(spoken.at(-1), /pháp bảo/i);
  inviteAfter(30000); assert.doesNotMatch(spoken.at(-1), /pháp bảo/i);
  inviteAfter(180000); assert.match(spoken.at(-1), /pháp bảo/i);
});

test("Tiên duyên chỉ chạy khi thực sự rảnh và bỏ qua khi không có tên đọc được", () => {
  const { game, clock } = setup();
  const lines = [];
  game.announcer.say = (line) => lines.push(line);
  join(game, "a", "🌸 An");
  game.idleTurn = 3;
  game.idleSince = clock() - 15000;
  game.announcer.speaking = true;
  game.tickIdle(clock());
  assert.equal(lines.length, 0);
  game.announcer.speaking = false;
  game.announcer.queue.push({ priority: 4 });
  game.idleSince = clock() - 15000;
  game.tickIdle(clock());
  assert.equal(lines.length, 0);
  game.announcer.queue = [];
  game.itemBatches.push({});
  game.idleSince = clock() - 15000;
  game.tickIdle(clock());
  assert.equal(lines.length, 0);
  game.itemBatches = [];
  game.idleSince = clock() - 15000;
  game.tickIdle(clock());
  assert.equal(lines.length, 1);
  assert.equal(lines[0].priority, 0);
  assert.match(lines[0].text, /🌸 An/);
  assert.doesNotMatch(lines[0].speech, /🌸/);
  assert.match(lines[0].speech, /An/);
  assert.equal(lines[0].valid(), true);
  game.sect.seated.delete("a");
  assert.equal(lines[0].valid(), false);

  const empty = setup().game;
  join(empty, "emoji", "🌸");
  assert.equal(empty.idleSocialLine(empty.now()), null);
});

test("Tiên duyên dùng đúng cảnh giới bị chặn, ghế hiện tại và linh thú thật", () => {
  const { game, clock } = setup({ kiep: true });
  join(game, "a", "An"); join(game, "b", "Bình");
  const an = game.sect.members.get("a"), binh = game.sect.members.get("b");
  an.tuvi = 13000; // Chưa vượt lôi kiếp: cảnh giới hiển thị vẫn là Kết Đan Đại viên mãn.
  an.pets.ngoc_tho = 1;
  game.socialTurn = 0; game.socialCursor = 0;
  const realm = game.idleSocialLine(clock());
  assert.match(realm.text, /An/);
  assert.match(realm.text, /Kết Đan Đại viên mãn/);
  assert.equal(realm.valid(), true);
  game.sect.passKiep("a", 3);
  assert.equal(realm.valid(), false, "không đọc cảnh giới đã thay đổi");

  game.lastSocial = null; game.socialTurn = 1; game.socialCursor = 0;
  const leader = game.idleSocialLine(clock());
  assert.match(leader.text, /Chưởng môn/);
  assert.match(leader.text, /An/);
  assert.equal(leader.valid(), true);
  binh.tuvi = 20000;
  assert.equal(leader.valid(), false, "không gọi nhầm Chưởng môn sau khi đổi hạng");

  game.lastSocial = null; game.socialTurn = 2; game.socialCursor = 0;
  const pet = game.idleSocialLine(clock());
  assert.match(pet.text, /An/);
  assert.match(pet.text, /Ngọc Thố/);
  assert.equal(pet.valid(), true);
  delete an.pets.ngoc_tho;
  assert.equal(pet.valid(), false, "không kể linh thú không còn trong dữ liệu");
});

test("Tiên duyên đổi người, không lặp người duy nhất trước 3 phút", () => {
  const { game, clock } = setup();
  join(game, "a", "An");
  game.socialTurn = 0; game.socialCursor = 0;
  const first = game.idleSocialLine(clock());
  assert.match(first.text, /An/);
  assert.equal(game.idleSocialLine(clock() + 179999), null);
  join(game, "b", "Bình");
  const second = game.idleSocialLine(clock() + 1000);
  assert.match(second.text, /Bình/);
  assert.notEqual(first.text, second.text);
  game.sect.seated.delete("a");
  assert.equal(game.idleSocialLine(clock() + 180999), null);
  assert.match(game.idleSocialLine(clock() + 181000).text, /Bình/);
});

test("Tiên duyên đi qua hàng giọng P0 và không chặn câu pháp bảo tiếp theo", async () => {
  const { game, clock, spoken } = setup();
  join(game, "a", "🌸 An");
  game.idleTurn = 3;
  game.idleSince = clock() - 15000;
  game.tickIdle(clock());
  assert.equal(spoken.length, 1);
  assert.match(spoken[0], /An/);
  assert.doesNotMatch(spoken[0], /🌸|\{[a-z_]+\}/i);
  game.announcer.say({ kind: "item", priority: 4, text: "Pháp bảo thật" });
  assert.equal(spoken.length, 1, "không cắt ngang câu đang đọc");
  assert.equal(game.announcer.queue[0].priority, 4);
  for (let i = 0; i < 5; i++) await Promise.resolve();
  assert.equal(spoken[1], "Pháp bảo thật");
});

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
  assert.match(a.speech, /đạo hữu ABC/); assert.match(a.speech, /Luyện Khí tầng 7/); assert.doesNotMatch(a.speech, /🐶/u);
  const b = shown.find((i) => i.kind === "realm_up" && i.member.id === "b");
  assert.equal(b.text, "Chúc mừng đạo hữu 不是鸿恩 đột phá thành công, bước vào cảnh giới Hóa Thần Sơ kỳ");
  assert.match(b.speech, /đạo hữu/); assert.match(b.speech, /Hóa Thần Sơ kỳ/); assert.doesNotMatch(b.speech, /不是鸿恩/u);
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
  assert.match(end.speech, /Thanh Liên/); assert.match(end.speech, /500 trên 200/);
  assert.match(end.speech, /Người A/); assert.match(end.speech, /500 tu vi/); assert.match(end.speech, /300 tu vi/);
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
  assert.ok(shown.some((i) => i.kind === "duel_end" && /^Phe Thanh Liên thắng 100 – 50! Công đầu a x2 tu vi đã góp \(\+100\), đồng đội \+300 tu vi( · bốc thăm quay thưởng: \S+)?$/.test(i.text)));
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

test("tỉ thí bốc thăm một người phe thắng: công đầu hai phần, người khác một phần", () => {
  const participants = [
    { id: "a", nickname: "An", side: 1, points: 200 },
    { id: "b", nickname: "Bình", side: 1, points: 100 },
    { id: "c", nickname: "Chi", side: 1, points: 0 },
    { id: "d", nickname: "Dũng", side: 2, points: 50 }
  ];
  for (const [roll, expected] of [[0.01, "a"], [0.26, "b"], [0.51, "c"], [0.76, "a"]]) {
    const { game, shown } = setup();
    participants.forEach((p) => join(game, p.id, p.nickname));
    game.random = () => roll;
    game.onDuelEvent({ type: "duel_end", winner: 1, scores: { 1: 300, 2: 50 }, mvp: { 1: participants[0] }, participants });
    assert.equal(game.wheel.memberId, expected);
    assert.equal(game.wheelQueue.length, 0);
    assert.match(shown.find((i) => i.kind === "duel_end").speech, new RegExp(participants.find((p) => p.id === expected).nickname));
  }
  const { game } = setup();
  game.onDuelEvent({ type: "duel_end", winner: 0, scores: { 1: 0, 2: 0 }, mvp: {}, participants: [] });
  assert.equal(game.wheel, null, "hòa không có vòng quay");
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
  assert.match(info[0].text, /^Tỉ thí sau 6 phút 57 giây · phe thắng mỗi người \+300 tu vi, công đầu x2 tu vi đã góp, bốc thăm 1 người được quay thưởng$/);
  assert.equal(info[0].speech, "Còn 6 phút 57 giây nữa là tới đại hội tỉ thí. Phe thắng mỗi người được cộng 300 tu vi, riêng công đầu được nhân đôi tu vi đã góp, và một người phe thắng được bốc thăm quay thưởng");
  assert.equal(info[0].silent, true, "chờ hết khoảng nhắc sau khi vào game");
});

test("chữ luật vẫn hiện mỗi 3 giây; lời nền đọc khi rảnh và 0 thì tắt lời nền", async () => {
  const { game, shown, spoken, advance } = setup();
  for (let i = 0; i < 20; i++) {
    advance(3000);
    await new Promise((r) => setImmediate(r)); // cho giọng đọc hoàn tất giữa các lần tick
  }
  const info = shown.filter((i) => i.kind === "info");
  assert.ok(info.length >= 15);
  assert.equal(info.filter((i) => !i.silent).length, 0, "nhắc luật trên màn hình không tự đọc");
  assert.equal(shown.filter((i) => i.kind === "idle").length, 4, "60 giây, nghỉ 15 giây mỗi câu: bốn câu nền khi giọng rảnh");
  await new Promise((r) => setImmediate(r));
  assert.ok(spoken.length >= 2);

  game.applySettings({ infoVoiceEverySec: 0 });
  const before = shown.length;
  for (let i = 0; i < 20; i++) advance(3000);
  assert.equal(shown.slice(before).filter((i) => i.kind === "idle").length, 0);
});

test("trong trận: câu nhắc đọc thời gian còn lại và cách chọn phe", () => {
  const { game, shown, advance } = setup();
  game.handleAction("start_duel", {}, { mode: "test" });
  advance(4000); advance(3000);
  const info = shown.filter((i) => i.kind === "info");
  assert.match(info[0].speech, /^Tỉ thí còn 2 phút \d+ giây\. Hiện tại Thanh Liên 0 điểm, Hồng Liên 0 điểm\. Bình luận số một vào phe Thanh Liên, số hai vào phe Hồng Liên\./);
  assert.equal(info[0].silent, true, "không nhắc lại ngay sau thông báo bắt đầu");
});

test("tắt tỉ thí thì không có trận và không vào phe được; luật nhắc thả tim / tặng quà", () => {
  const { game, advance, shown } = setup();
  join(game, "a");
  game.applySettings({ duel: false, boss: false });
  advance(60 * 60000);
  assert.equal(game.duel.phase, "idle");
  assert.throws(() => phe(game, "a", 1), /tắt/);
  advance(3000);
  assert.ok(shown.some((i) => i.kind === "info" && i.text === "Hãy sử dụng pháp bảo để tu luyện nhanh hơn"));
});

test("lời nền đợi giọng sự kiện đọc xong rồi mới bắt đầu tính khoảng nghỉ", async () => {
  let now = WED, finish;
  const shown = [], spoken = [];
  const speaker = { cancel() {}, speak: (text) => { spoken.push(text); return new Promise((resolve) => { finish = resolve; }); } };
  const game = new Game(DEFAULTS, { now: () => now, show: (item) => shown.push(item), speaker, storage: memoryStorage() });
  game.applySettings({ voice: true, duel: false, boss: false, infoVoiceEverySec: 15 });
  game.enter();
  now += 10000;
  game.announcer.say({ kind: "event", text: "Sự kiện bắt đầu", priority: 3 });
  now += 12000;
  game.showInfo(now);
  assert.equal(shown.at(-1).silent, true, "không xếp lời nhắc vào hàng khi đang đọc");
  assert.equal(game.announcer.queue.length, 0);
  finish(true);
  await new Promise((resolve) => setImmediate(resolve));
  now += 14999; game.tick(now);
  assert.equal(spoken.length, 1);
  now += 1; game.tick(now);
  assert.equal(spoken.length, 2);
  assert.equal(spoken[1], "Hãy sử dụng pháp bảo để tu luyện nhanh hơn");

  finish(true);
  await new Promise((resolve) => setImmediate(resolve));
  game.announcer.say({ kind: "event", text: "Sự kiện khác", priority: 3 });
  finish(true);
  await new Promise((resolve) => setImmediate(resolve));
  now += 3000; game.tick(now);
  assert.equal(shown.filter((i) => i.kind === "idle").length, 1, "không nhắc lại ngay sau sự kiện");
});

test("lời mở vòng quay không đọc lặp lại phần thưởng đã được công bố", () => {
  const { game, shown } = setup();
  join(game, "a");
  game.queueWheel({ id: "a", nickname: "a" }, "boss");
  assert.equal(shown.find((item) => item.kind === "wheel_start").silent, true);
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

test("tu vi lưu 2 tuần: buổi live khác trong cùng kỳ vẫn giữ, sang kỳ mới (0h thứ Hai tuần thứ 3) thì reset", () => {
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
  const week2 = new Game(DEFAULTS, { storage, now: () => new Date(2026, 10, 16, 9, 0).getTime() });   // tuần thứ 2 của kỳ
  assert.equal(week2.sect.members.get("a").tuvi, 5000);
  const nextPeriod = new Game(DEFAULTS, { storage, now: () => new Date(2026, 10, 23, 9, 0).getTime() });
  assert.equal(nextPeriod.sect.members.size, 0);
});

test("đang live lúc qua 0h thứ Hai đầu kỳ mới: tu vi về 0, người đang ngồi vẫn ngồi, có thông báo mùa mới", () => {
  const { game, shown, advance } = setup({ start: new Date(2026, 10, 22, 23, 59, 50).getTime() });
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
  assert.match(greet1.speech, /đạo hữu An/); assert.match(greet1.speech, /Nga Mi/);
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

test("pháp bảo: cộng tu vi như tu luyện, có hiệu ứng và banner; mọi món đều có giọng đọc", async () => {
  const { game, shown, spoken, advance } = setup();
  join(game, "a", "An");
  item(game, "a", "ban_dao", 5000);
  item(game, "a", "long_chau", 20000, 2);
  assert.equal(game.sect.members.get("a").tuvi, 25000);
  const fx = game.getState().items;
  assert.deepEqual(fx.map((f) => [f.item, f.userId, f.count]), [["ban_dao", "a", 1], ["long_chau", "a", 2]]);
  const msgs = shown.filter((i) => i.kind === "item");
  assert.equal(msgs.length, 2, "chữ hiện ngay, mỗi lần dùng một dòng");
  assert.deepEqual(msgs.map((i) => [i.priority, i.silent]), [[1, true], [2, true]], "chữ giữ mức cũ; giọng đi qua lô gom");
  assert.match(msgs[0].text, /vừa sử dụng pháp bảo Bàn Đào! \+5\.000 tu vi/);
  assert.match(msgs[1].text, /Long Châu x2/);
  const voiced = () => spoken.filter((t) => /pháp bảo/.test(t));
  assert.equal(voiced().length, 0, "chưa hết 4 giây gom thì chưa đọc");
  advance(4000); await flush(); await flush(); await flush();
  assert.deepEqual(voiced(), ["Đạo hữu a vừa sử dụng pháp bảo Bàn Đào, cộng 5.000 tu vi", "Đạo hữu a vừa sử dụng pháp bảo Long Châu 2 lần, cộng 20.000 tu vi"]);
  assert.equal(shown.filter((i) => i.kind === "item").length, 2, "câu đọc gom không hiện thêm dòng chữ");
});

const flush = () => new Promise((r) => setImmediate(r));
// Bộ đọc giữ câu đang đọc cho tới khi test gọi next(): mô phỏng giọng đang bận.
function slowVoice(settings = {}) {
  let now = WED;
  const spoken = [], shown = [], pending = [];
  // Chỉ giữ các câu test đang theo dõi; câu chào nhập môn / thăng cấp đọc xong ngay và không ghi lại.
  const speaker = { cancel() {}, speak: (t) => {
    if (!/pháp bảo|mức \d|đang đọc/.test(t)) return Promise.resolve(true);
    spoken.push(t); return new Promise((resolve, reject) => pending.push({ resolve, reject }));
  } };
  const game = new Game(DEFAULTS, { now: () => now, show: (i) => shown.push(i), speaker, storage: memoryStorage() });
  game.applySettings({ voice: true, duel: false, boss: false, tauHoa: false, kiep: false, linhKhi: false, infoVoiceEverySec: 0, announceStageUps: false, ...settings });
  game.enter();
  return { game, spoken, shown, advance: (ms) => { now += ms; game.tick(now); },
    next: async (ok = true) => { const p = pending.shift(); if (ok === "error") p.reject(new Error("tts")); else p.resolve(ok); await flush(); await flush(); } };
}

test("gom pháp bảo: cùng người cùng món trong 4 giây thành một câu, cộng số lần và tu vi thực nhận", async () => {
  const { game, spoken, advance, next } = slowVoice();
  join(game, "An"); join(game, "Bình"); advance(0); await flush();
  game.sect.members.get("An").pets.ngoc_tho = 1;         // +5%: câu đọc phải dùng số thực nhận
  item(game, "An", "chu_qua", 100); advance(1000);
  item(game, "An", "chu_qua", 100, 2); advance(1000);
  item(game, "Bình", "chu_qua", 100); item(game, "An", "ban_dao", 200);
  assert.equal(game.itemBatches.length, 3);
  advance(2000);                                         // lô đầu (a + Chu Quả) tròn 4 giây
  assert.deepEqual(spoken, ["Đạo hữu An vừa sử dụng pháp bảo Chu Quả 3 lần, cộng 210 tu vi"]);
  item(game, "An", "chu_qua", 100);                       // lô đã khóa: lần này mở lô mới
  advance(2000);
  assert.equal(game.announcer.queue.length, 2, "người khác / món khác là câu riêng, đúng thứ tự đến");
  await next(); await next();
  assert.deepEqual(spoken.slice(1), ["Đạo hữu Bình vừa sử dụng pháp bảo Chu Quả, cộng 100 tu vi", "Đạo hữu An vừa sử dụng pháp bảo Bàn Đào, cộng 210 tu vi"]);
  advance(2000); await next();
  assert.equal(spoken[3], "Đạo hữu An vừa sử dụng pháp bảo Chu Quả, cộng 105 tu vi");
  assert.equal(game.sect.members.get("An").tuvi, 525, "gom giọng không đổi tu vi");
});

test("gom pháp bảo khi giọng dồn: cộng vào câu đang chờ, ghép tối đa 3 cặp, không sai tên và món", async () => {
  const { game, spoken, advance, next } = slowVoice();
  ["An", "Bình", "Chi", "Dung", "Em", "Phong"].forEach((id) => join(game, id)); advance(0); await flush();
  item(game, "An", "chu_qua", 100); advance(4000);       // đang đọc câu của An
  item(game, "Bình", "chu_qua", 100); advance(4000);     // chờ 1
  item(game, "Chi", "ban_dao", 100); advance(4000);      // chờ 2
  item(game, "Bình", "chu_qua", 100, 2); advance(4000);  // cùng cặp với câu chờ 1: cộng dồn
  item(game, "Dung", "long_chau", 100); item(game, "Em", "chu_qua", 100); item(game, "Phong", "chu_qua", 100); advance(4000);
  const waiting = game.announcer.queue.filter((q) => q.kind === "item").map((q) => q.speech);
  assert.deepEqual(waiting, [
    "Đạo hữu Bình vừa sử dụng pháp bảo Chu Quả 3 lần, cộng 200 tu vi",
    "Đạo hữu Chi vừa sử dụng pháp bảo Bàn Đào, cộng 100 tu vi. Đạo hữu Dung vừa sử dụng pháp bảo Long Châu, cộng 100 tu vi. Đạo hữu Em vừa sử dụng pháp bảo Chu Quả, cộng 100 tu vi",
    "Đạo hữu Phong vừa sử dụng pháp bảo Chu Quả, cộng 100 tu vi"]);
  assert.equal(game.itemVoiceStats.uses, 7);
  await next(); assert.equal(spoken[1], waiting[0]);
});

test("gom pháp bảo: quá 8 lô thì chốt lô cũ nhất sớm; tắt giọng hoặc thoát thì không đọc lại hàng cũ", async () => {
  const { game, spoken, advance } = slowVoice();
  for (let i = 0; i < 9; i++) join(game, "u" + i);
  advance(0); await flush();
  for (let i = 0; i < 9; i++) item(game, "u" + i, "chu_qua", 100);
  assert.equal(game.itemBatches.length, 8);
  assert.equal(spoken.length, 1, "lô cũ nhất được chốt ngay khi vượt giới hạn");
  game.exit(); game.enter(); advance(10000); await flush();
  assert.equal(game.itemBatches.length, 0);
  assert.equal(spoken.length, 1);
});

test("giọng: mọi pháp bảo đang chờ được đọc trước sự kiện khác", async () => {
  const { game, spoken, next } = slowVoice();
  const ann = game.announcer;
  ann.say({ text: "đang đọc", priority: 1 });
  ann.say({ text: "mức 3", priority: 3 });
  for (let i = 1; i <= 4; i++) ann.say({ kind: "item", priority: 4, text: "pháp bảo " + i });
  assert.deepEqual(ann.queue.map(q => q.text), ["pháp bảo 1", "pháp bảo 2", "pháp bảo 3", "pháp bảo 4"], "hàng đầy giữ pháp bảo thay vì dành chỗ cho mức 3");
  ann.say({ text: "mức 3 mới", priority: 3 });
  for (let i = 1; i <= 4; i++) { await next(); assert.equal(spoken.at(-1), "pháp bảo " + i); }
  await next();
  assert.equal(spoken.includes("mức 3 mới"), false, "câu ưu tiên thấp hơn không đẩy pháp bảo ra khỏi hàng");
  ann.say({ text: "mức 3 sau", priority: 3 });
  assert.equal(spoken.at(-1), "mức 3 sau");
  await next();
});

test("một món làm lên cấp: chờ gom rồi đọc pháp bảo trước lời lên cấp", async () => {
  const { game, shown, spoken, advance } = setup();
  join(game, "a");
  await flush(); await flush();
  const start = spoken.length;
  item(game, "a", "chu_qua", 1000);
  const level = shown.find(i => i.kind === "realm_up" || i.kind === "level_up");
  assert.ok(level, "pháp bảo phải tạo sự kiện lên cấp");
  assert.equal(spoken.length, start, "không đọc lên cấp trong lúc lô pháp bảo chưa chốt");
  advance(3999); await flush();
  assert.equal(spoken.length, start);
  advance(1); await flush(); await flush();
  assert.match(spoken[start], /vừa sử dụng pháp bảo Chu Quả/);
  assert.ok(spoken.slice(start + 1).length, "sự kiện lên cấp được đọc sau pháp bảo");
});

test("câu đang đọc kết thúc khi pháp bảo còn gom: sự kiện khác tiếp tục chờ", async () => {
  const { game, spoken, next, advance } = slowVoice();
  join(game, "a"); advance(0); await flush();
  game.announcer.say({ text: "đang đọc", priority: 1 });
  item(game, "a", "chu_qua", 100);
  game.announcer.say({ text: "mức 3", priority: 3 });
  await next();
  assert.deepEqual(spoken, ["đang đọc"]);
  advance(4000);
  assert.match(spoken[1], /vừa sử dụng pháp bảo Chu Quả/);
  await next();
  assert.equal(spoken[2], "mức 3");
  await next();
});

test("cài đặt cũ: lời nền 30 giây chuyển sang 15 đúng một lần, giữ 0 và giá trị khác, không xóa lại chu kỳ tỉ thí", () => {
  const open = (stored) => {
    const storage = memoryStorage();
    if (stored) storage.set("ngami.tutien.settings.v1", stored);
    const game = new Game(DEFAULTS, { now: () => WED, storage });
    return { game, storage };
  };
  assert.equal(open(null).game.settings.infoVoiceEverySec, 15, "chưa có dữ liệu: mặc định 15");
  for (const [before, after] of [[30, 15], ["30", 15], [0, 0], [45, 45], [120, 120], ["abc", 15]]) {
    const { game } = open({ settingsVersion: 2, infoVoiceEverySec: before, duelEveryMin: 20, duelDurationMin: 4, bossHp: 12345 });
    assert.equal(game.settings.infoVoiceEverySec, after, "giá trị cũ " + before);
    assert.equal(game.settings.duelEveryMin, 20, "nâng v2 -> v3 không xóa chu kỳ tỉ thí");
    assert.equal(game.settings.duelDurationMin, 4);
    assert.equal(game.settings.bossHp, 12345);
    assert.equal(game.settings.settingsVersion, 3);
  }
  const v1 = open({ infoVoiceEverySec: 30, duelEveryMin: 15, duelDurationMin: 5 }).game.settings;
  assert.deepEqual([v1.infoVoiceEverySec, v1.duelEveryMin, v1.duelDurationMin], [15, 10, 3], "bản rất cũ: chạy cả hai bước nâng");

  // Sau khi đã chuyển, người dùng tự chọn lại 30 thì giữ 30 ở các lần mở sau.
  const { game, storage } = open({ settingsVersion: 2, infoVoiceEverySec: 30 });
  game.applySettings({ infoVoiceEverySec: 30 });
  assert.equal(new Game(DEFAULTS, { now: () => WED, storage }).settings.infoVoiceEverySec, 30);

  // Lưu không thành công: game vẫn chạy với 15.
  const broken = { get: () => ({ settingsVersion: 2, infoVoiceEverySec: 30 }), set: () => { throw new Error("đầy"); } };
  assert.equal(new Game(DEFAULTS, { now: () => WED, storage: broken }).settings.infoVoiceEverySec, 15);
});

test("liên kết hành động: pháp bảo mang cùng mã act với đúng kết quả game đã tính, không tự thêm đòn", () => {
  const { game, advance } = setup();
  ["a", "b", "x"].forEach((id) => join(game, id));
  // Sân thường: không có mục tiêu, món chỉ thi triển tại chỗ.
  item(game, "a", "linh_thach", 100);
  let fx = game.getState().items.at(-1);
  assert.deepEqual(fx.effects, []);
  assert.ok(fx.act > 0);

  // Tỉ thí: phe kia chưa có ai -> đòn không có mục tiêu, không ai mất tu vi.
  game.handleAction("start_duel", {}, { mode: "test" });
  phe(game, "a", 1);
  item(game, "a", "phi_kiem", 100);
  fx = game.getState().items.at(-1);
  assert.deepEqual(fx.effects, [{ kind: "duel", to: null, lost: 0, side: 1 }]);

  // Có đối thủ: mục tiêu và số tu vi mất trong fx trùng với đòn thật, cùng mã act, chỉ 1 đòn cho 1 lần dùng.
  phe(game, "b", 2); tu(game, "b", 1000);
  const before = game.getState().duel.hits.length, bTuvi = game.sect.members.get("b").tuvi;
  item(game, "a", "long_chau", 400, 3);
  fx = game.getState().items.at(-1);
  const hits = game.getState().duel.hits.slice(before);
  assert.equal(hits.length, 1);
  assert.equal(hits[0].act, fx.act);
  assert.deepEqual(fx.effects, [{ kind: "duel", to: "b", lost: 200, side: 1 }]);
  assert.equal(hits[0].lost, 200);
  assert.equal(game.sect.members.get("b").tuvi, bTuvi - 200);

  // Tu luyện thường (không phải pháp bảo): đòn có mã act riêng, không trùng món nào.
  tu(game, "a", 50);
  const plain = game.getState().duel.hits.at(-1);
  assert.ok(plain.act > fx.act);
  assert.equal(game.getState().items.some((f) => f.act === plain.act), false);

  // Người đứng xem dùng món trong lúc tỉ thí: không tạo đòn.
  item(game, "x", "linh_thach", 100);
  assert.deepEqual(game.getState().items.at(-1).effects, []);
  advance(0);
});

test("liên kết hành động: đánh rồng dùng sát thương thật (có linh thú), kết giới thiên kiếp ghi đúng người nhận", () => {
  const { game } = setup();
  join(game, "a"); join(game, "b");
  game.sect.members.get("a").pets.hoa_ky_lan = 2;         // +10% sát thương
  game.spawnBoss(game.now());
  const hp = game.boss.hp;
  item(game, "a", "chu_qua", 100);
  const fx = game.getState().items.at(-1), hit = game.getState().boss.hits.at(-1);
  assert.deepEqual(fx.effects, [{ kind: "boss", amount: 110 }]);
  assert.equal(hit.act, fx.act);
  assert.equal(game.boss.hp, hp - 110);
  assert.equal(game.sect.members.get("a").tuvi, 100, "tu vi nhận không đổi theo sát thương");
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

test("linh thú chiến đấu: tăng sát thương lên rồng nhưng không tăng tu vi; linh thú reset theo kỳ 2 tuần", () => {
  const { game, advance } = setup();
  join(game, "a");
  game.sect.members.get("a").pets = { hoa_ky_lan: 2, kim_phuong: 1 };   // +15% sát thương, +5% tu vi
  game.spawnBoss(game.now());
  tu(game, "a", 1000);
  assert.equal(game.sect.members.get("a").tuvi, 1050);
  assert.equal(game.boss.maxHp - game.boss.hp, Math.floor(1050 * 1.15));
  advance(7 * 24 * 3600 * 1000);                     // sang tuần sau: cùng kỳ 2 tuần, linh thú còn
  assert.deepEqual(game.sect.members.get("a").pets, { hoa_ky_lan: 2, kim_phuong: 1 });
  advance(7 * 24 * 3600 * 1000);                     // sang kỳ mới
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

test("Thiên kiếp: đủ tu vi Nguyên Anh thì kẹt ở Kết Đan Đại viên mãn tới khi độ kiếp; thành công thì đột phá, hộ pháp được thưởng", () => {
  const { game, advance, shown } = setup({ kiep: true });
  game.applySettings({ duel: false, boss: false });
  join(game, "a", "An"); join(game, "b", "Bình");
  tu(game, "a", 12000);
  assert.equal(game.kiep, null, "Kết Đan: chưa có kiếp");
  tu(game, "a", 1000);                               // 13.000 = đủ Nguyên Anh
  const lvA = () => game.getState().seats.find((s) => s.id === "a");
  assert.equal(lvA().realm, 2, "chưa độ kiếp: vẫn là Kết Đan");
  assert.match(lvA().label, /Kết Đan Đại viên mãn/);
  advance(250);
  const k = game.kiep;
  assert.ok(k && k.targetId === "a" && k.strikes === 6 && k.realmIdx === 3);
  assert.equal(k.need, 650, "5% của 13.000");
  assert.ok(shown.some((i) => i.kind === "kiep_start"));
  tu(game, "b", 700);                                // hộ pháp
  tu(game, "a", 100);                                // tự hộ thân: 50% vào kết giới
  assert.equal(game.kiep.shield, 750);
  assert.equal(game.kiep.selfShield, 50);
  assert.equal(game.getState().kiep.selfHits.length, 1);
  advance(DEFAULTS.tribulation.durationMs);
  assert.equal(game.kiep.phase, "result");
  assert.equal(game.kiep.success, true);
  assert.equal(game.kiep.log.length, 6);
  assert.equal(game.sect.members.get("a").tuvi, 13100 + Math.floor(13100 * 0.05));
  assert.equal(game.sect.members.get("b").tuvi, 700 + 350 + 1000, "hộ pháp +50% đã góp, hộ pháp đầu thêm +1.000");
  advance(DEFAULTS.tribulation.resultMs);
  assert.equal(game.kiep, null);
  assert.equal(lvA().realm, 3, "đã đột phá Nguyên Anh");
  assert.ok(shown.some((i) => i.kind === "realm_up" && /Nguyên Anh/.test(i.text)));
});

test("Thiên kiếp: thất bại thì kẹt ở Đại viên mãn, không mất tu vi, độ kiếp lại sau 3 phút hoặc khi tu vi tăng 10%; chờ khi đang đánh quái", () => {
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
  assert.equal(game.sect.members.get("a").tuvi, tuvi, "không mất tu vi");
  advance(DEFAULTS.tribulation.resultMs);
  const seat = () => game.getState().seats.find((s) => s.id === "a");
  assert.equal(seat().realm, 2, "vẫn Kết Đan Đại viên mãn");
  advance(60000);
  assert.equal(game.kiep, null, "chưa tới lúc thử lại");
  tu(game, "a", Math.ceil(tuvi * 0.1));               // tu vi tăng 10%: được độ kiếp lại ngay
  advance(250);
  assert.ok(game.kiep && game.kiep.targetId === "a", "độ kiếp lại");
});

test("Thiên kiếp: lưu và tải lại giữ đúng cảnh giới đã độ kiếp; dữ liệu cũ không bị tụt cảnh giới", () => {
  const { game } = setup({ kiep: true });
  join(game, "a"); join(game, "b");
  tu(game, "a", 13000);                               // chưa độ kiếp
  tu(game, "b", 13000); game.sect.passKiep("b", 3);   // đã độ kiếp
  const data = game.sect.serialize();
  const { game: g2 } = setup({ kiep: true });
  g2.sect.load(data);
  assert.equal(g2.sect.levelOfMember(g2.sect.members.get("a")).realmIdx, 2);
  assert.equal(g2.sect.levelOfMember(g2.sect.members.get("b")).realmIdx, 3);
  // Bản lưu trước luật mới (không có "_kiep"): giữ cảnh giới theo tu vi.
  g2.sect.load({ v: 3, members: [["c", "C", 13000, 0, {}]] });
  assert.equal(g2.sect.levelOfMember(g2.sect.members.get("c")).realmIdx, 3);
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

test("tu vi + linh thú giữ 2 tuần: sang tuần thứ 2 vẫn còn, sang kỳ mới (0h thứ Hai tuần thứ 3) mới reset", () => {
  let now = new Date(2026, 10, 15, 23, 59).getTime();
  const game = new Game(DEFAULTS, { now: () => now, storage: memoryStorage(), show: () => {} });
  game.applySettings({ duel: false, boss: false });
  game.enter();
  game.handleLiveEvent({ type: "like", user: { id: "a", nickname: "A" } });
  game.handleAction("tu_luyen", { amount: 500 }, { user: { id: "a", nickname: "A" } });
  game.sect.members.get("a").pets = { ngoc_tho: 2 };
  now = new Date(2026, 10, 16, 0, 1).getTime(); game.tick(now);
  assert.equal(game.week, "2026-11-09");
  assert.equal(game.sect.members.get("a").tuvi, 500, "tuần thứ 2 vẫn giữ tu vi");
  assert.equal(game.sect.members.get("a").pets.ngoc_tho, 2, "tuần thứ 2 vẫn giữ linh thú");
  now = new Date(2026, 10, 23, 0, 1).getTime(); game.tick(now);
  assert.equal(game.week, "2026-11-23");
  assert.equal(game.sect.members.get("a").tuvi, 0);
  assert.deepEqual(game.sect.members.get("a").pets, {});
});

test("tỉ thí không ai chọn phe: không báo hòa / không cộng tu vi", () => {
  const { game, advance, shown } = setup();
  game.applySettings({ boss: false });
  join(game, "a");
  advance(game.duel.nextStartAt - game.now() + 1000);
  advance(game.duel.endsAt - game.now() + 1000);
  const end = shown.filter((i) => i.kind === "duel_end");
  assert.equal(end.length, 1);
  assert.match(end[0].text, /chưa có đạo hữu nào chọn phe/);
  assert.equal(game.sect.members.get("a").tuvi, 0);
});
