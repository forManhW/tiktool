const test = require("node:test");
const assert = require("node:assert/strict");
const { Announcer } = require("../src/announcer");

const cfg = { enabled: true, maxQueue: 4, maxWaitMs: 6000, maxWaitImportantMs: 12000 };

test("bộ đọc có prepare(): câu được tạo trước khi vào hàng chờ, lúc đọc dùng lại bản đã tạo", async () => {
  const prepared = [], spoken = [];
  let release;
  const speaker = {
    prepare: (text) => { prepared.push(text); return Promise.resolve("audio:" + text); },
    speak: (text, c, ready) => { spoken.push([text, ready]); return new Promise((r) => { release = r; }); },
    cancel() {}
  };
  const ann = new Announcer(cfg, { speaker });
  ann.say({ text: "câu 1", priority: 2 });
  ann.say({ text: "câu 2", priority: 2 });
  assert.deepEqual(prepared, ["câu 1", "câu 2"], "câu 2 được tạo trước trong lúc câu 1 đang đọc");
  const [, ready1] = spoken[0];
  assert.equal(await ready1, "audio:câu 1");
  release(true);
  await new Promise((r) => setImmediate(r));
  assert.equal(await spoken[1][1], "audio:câu 2");
});

test("tắt giọng: không tạo trước, không đọc", () => {
  const calls = [];
  const ann = new Announcer({ ...cfg, enabled: false }, { speaker: { prepare: (t) => calls.push(t), speak: () => calls.push("speak"), cancel() {} } });
  ann.say({ text: "x" });
  assert.deepEqual(calls, []);
});

test("hàng chờ đầy: bỏ câu ưu tiên thấp nhất, cùng mức thì bỏ câu cũ nhất", () => {
  const spoken = [];
  const ann = new Announcer({ ...cfg, maxQueue: 2 }, { speaker: { speak: (t) => { spoken.push(t); return new Promise(() => {}); }, cancel() {} } });
  ann.say({ text: "đang đọc", priority: 3 });     // vào đọc ngay, không nằm trong hàng chờ
  ann.say({ text: "cũ", priority: 3 });
  ann.say({ text: "thấp", priority: 1 });
  ann.say({ text: "mới", priority: 3 });         // đầy: bỏ "thấp"
  ann.say({ text: "mới hơn", priority: 3 });     // đầy, cùng mức 3: bỏ "cũ"
  assert.deepEqual(ann.queue.map((i) => i.text), ["mới", "mới hơn"]);
});

test("móc idle chỉ báo sau khi đọc xong và hàng chờ đã cạn", async () => {
  const releases = [], idle = [];
  const speaker = { speak: () => new Promise((resolve) => releases.push(resolve)), cancel() {} };
  const ann = new Announcer(cfg, { speaker, idle: () => idle.push(true) });
  ann.say({ text: "một" }); ann.say({ text: "hai" });
  releases.shift()(true); await new Promise((resolve) => setImmediate(resolve));
  assert.equal(idle.length, 0);
  releases.shift()(true); await new Promise((resolve) => setImmediate(resolve));
  assert.equal(idle.length, 1);
});

test("thay cách gọi trong lời đọc, kể cả dữ liệu động, nhưng không sửa chữ hiển thị", () => {
  const spoken = [], shown = [];
  const ann = new Announcer(cfg, { show: (i) => shown.push(i), speaker: { speak: (s) => { spoken.push(s); return Promise.resolve(); }, cancel() {} } });
  ann.say({ text: "An thả tim", speech: "An thả tim và gửi quà, Bình tặng quà" });
  assert.equal(shown[0].text, "An thả tim");
  assert.equal(spoken[0], "An tap tay và sử dụng pháp bảo, Bình sử dụng pháp bảo");
});

test("sự kiện đẩy câu nền ưu tiên 0 ra khỏi hàng chờ khi hàng đầy", () => {
  const ann = new Announcer(cfg, { speaker: { speak: () => new Promise(() => {}), cancel() {} } });
  ann.say({ text: "đang đọc", priority: 3 });
  ann.say({ kind: "idle", key: "idle", text: "lời nền", priority: 0 });
  for (let i = 0; i < 4; i++) ann.say({ text: "sự kiện " + i, priority: 1 });
  assert.equal(ann.queue.length, 4);
  assert.equal(ann.queue.some((item) => item.kind === "idle"), false);
});
