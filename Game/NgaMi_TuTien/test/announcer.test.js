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
