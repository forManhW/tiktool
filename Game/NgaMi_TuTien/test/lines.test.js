const test = require("node:test");
const assert = require("node:assert/strict");
const { createLinePicker, LINES } = require("../src/lines");

function random(seed) { return () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 0x100000000); }

test("xáo theo túi ổn định với seed, hết túi không lặp câu cuối", () => {
  const catalog = { go: ["a", "b", "c", "d"] };
  const a = createLinePicker(catalog, random(12)), b = createLinePicker(catalog, random(12));
  const first = Array.from({ length: 12 }, () => a("go"));
  assert.deepEqual(first, Array.from({ length: 12 }, () => b("go")));
  assert.equal(new Set(first.slice(0, 4)).size, 4);
  assert.equal(new Set(first.slice(4, 8)).size, 4);
  assert.notEqual(first[3], first[4]);
  assert.notEqual(first[7], first[8]);
});

test("kho câu đã duyệt đủ biến thể và không dùng cách gọi bị yêu cầu bỏ", () => {
  const rows = Object.values(LINES).flat().map((entry) => typeof entry === "string" ? entry : entry.t);
  assert.ok(rows.length >= 120);
  for (const text of rows) assert.doesNotMatch(text, /thả\s+tim|tap\s+tim|tặng\s+quà|gửi\s+quà/i);
  assert.equal(LINES.gift_thanks.length, 6);
  assert.ok(LINES.gift_thanks.every((text) => text.includes("{so_linh_thach}")));
});

test("bỏ mẫu thiếu biến và lọc when theo trạng thái", () => {
  const pick = createLinePicker({ q: ["Chào {ten}", { t: "Trận đang mở", when: "duel" }, { t: "Yên", when: "any" }] }, random(3));
  assert.equal(pick("q", {}, "tu_luyen"), "Yên");
  assert.equal(pick("q", {}, "duel"), "Trận đang mở");
  const named = createLinePicker({ q: ["{ten} được {n}"] }, random(1));
  assert.equal(named("q", { ten: "An" }), null);
  assert.equal(named("q", { ten: "An", n: 0 }), "An được 0");
  assert.equal(named("missing"), null);
});
