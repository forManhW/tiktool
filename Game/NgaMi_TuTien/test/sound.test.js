const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { createSfxMixer } = require("../src/sound");

class FakeAudio {
  constructor(url) { this.url = url; this.volume = 1; this.currentTime = 0; FakeAudio.all.push(this); }
  play() { return this.url === "blocked" ? Promise.reject(new Error("autoplay")) : Promise.resolve(); }
  pause() { this.paused = true; }
}
FakeAudio.all = [];

test("âm hiệu ứng dùng chung tối đa 4 kênh, hạ và khôi phục âm khi giọng đọc thay đổi", () => {
  FakeAudio.all = [];
  const m = createSfxMixer({ Audio: FakeAudio, now: () => 1000, minGapMs: 0 });
  for (let i = 0; i < 4; i++) assert.equal(m.play("s" + i, "sound", 0.5), true);
  assert.equal(m.play("overflow", "sound", 1), false);
  m.setSpeaking(true);
  assert.deepEqual(FakeAudio.all.map(a => a.volume), [0.2, 0.2, 0.2, 0.2]);
  FakeAudio.all[0].onended();
  assert.equal(m.play("new", "sound", 1), true);
  assert.equal(FakeAudio.all[4].volume, 0.4);
  m.setSpeaking(false);
  assert.equal(FakeAudio.all[4].volume, 1);
  m.stopAll();
  assert.equal(m.stats().active, 0);
  assert.ok(FakeAudio.all.slice(1).every(a => a.paused));
});

test("giới hạn lặp cùng âm và trả kênh khi âm bị chặn hoặc lỗi", async () => {
  FakeAudio.all = [];
  let now = 1000;
  const m = createSfxMixer({ Audio: FakeAudio, now: () => now, minGapMs: 180 });
  assert.equal(m.play("same", "sound", 1), true);
  FakeAudio.all[0].onended();
  now += 179;
  assert.equal(m.play("same", "sound", 1), false);
  now++;
  assert.equal(m.play("same", "sound", 1), true);
  assert.equal(m.play("blocked", "blocked", 1), true);
  await Promise.resolve();
  await Promise.resolve();
  assert.equal(m.stats().active, 1);
  FakeAudio.all[1].onerror();
  assert.equal(m.stats().active, 0);
});

test("âm không báo kết thúc được tự dừng và trả kênh", async () => {
  FakeAudio.all = [];
  const m = createSfxMixer({ Audio: FakeAudio, maxDurationMs: 100, minGapMs: 0 });
  assert.equal(m.play("stuck", "sound", 1), true);
  await new Promise(resolve => setTimeout(resolve, 150));
  assert.equal(m.stats().active, 0);
  assert.equal(FakeAudio.all[0].paused, true);
});

test("hai biến thể âm cho từng nhóm có WAV hợp lệ và bản game khớp nguồn", () => {
  const assets = path.join(__dirname, "..", "src", "assets");
  for (const group of ["kiem", "va_cham", "tu_khi", "ngoc", "lua", "nuoc", "phu_van", "rong", "trung", "ti_thi"]) {
    for (const variant of [1, 2]) {
      const name = `sfx_${group}_${variant}.wav`;
      const raw = fs.readFileSync(path.join(assets, "raw", name));
      const built = fs.readFileSync(path.join(assets, "game", name));
      assert.equal(raw.toString("ascii", 0, 4), "RIFF", name);
      assert.equal(raw.toString("ascii", 8, 12), "WAVE", name);
      assert.deepEqual(built, raw, name);
    }
  }
});

const { createThunderLoop } = require("../src/sound");
test("sấm lặp chồng trong độ kiếp và dừng khi hết kiếp, tắt âm hoặc thoát", () => {
  let now = 0, allow = true;
  const plays = [], stops = [];
  const mixer = {
    play(key, url, volume) { plays.push({ key, url, volume, now }); return allow; },
    stop(key) { stops.push(key); }
  };
  const loop = createThunderLoop(mixer, { now: () => now });
  const strike = { id: 1, phase: "strike" };
  loop.sync(strike, true, 0.8);
  assert.deepEqual(plays[0], { key: "thunder", url: "src/assets/game/thunder.mp3", volume: 0.8 * 2.34, now: 0 });
  now = 3999; loop.sync(strike, true, 0.8);
  assert.equal(plays.length, 1);
  now = 4000; loop.sync(strike, true, 0.8);
  assert.equal(plays.length, 2);
  now = 8000; allow = false; loop.sync(strike, true, 0.8);
  allow = true; now = 8250; loop.sync(strike, true, 0.8);
  assert.equal(plays.length, 4, "thử lại khi mixer đã có chỗ");
  loop.sync({ id: 1, phase: "result" }, true, 0.8);
  assert.deepEqual(stops, ["thunder"]);
  loop.sync(strike, true, 0.8);
  loop.sync(strike, false, 0.8);
  assert.equal(stops.length, 2);
  loop.sync({ id: 2, phase: "strike" }, true, 0.8);
  loop.stop();
  assert.equal(stops.length, 3);
});

test("dừng riêng tiếng sấm đang chồng mà không dừng âm khác", () => {
  FakeAudio.all = [];
  const m = createSfxMixer({ Audio: FakeAudio, minGapMs: 0 });
  m.play("thunder", "thunder", 0.4);
  m.play("thunder", "thunder", 0.4);
  m.play("item", "item", 0.4);
  m.stop("thunder");
  assert.equal(m.stats().active, 1);
  assert.equal(FakeAudio.all[0].paused, true);
  assert.equal(FakeAudio.all[1].paused, true);
  assert.equal(FakeAudio.all[2].paused, undefined);
  m.stopAll();
});

test("âm sấm trong game khớp file nguồn", () => {
  const assets = path.join(__dirname, "../src/assets");
  assert.deepEqual(fs.readFileSync(path.join(assets, "game/thunder.mp3")), fs.readFileSync(path.join(assets, "raw/thunder.mp3")));
});

test("khuếch đại vượt mức audio.volume và vẫn giảm khi có giọng đọc", () => {
  class Node {
    connect(target) { this.target = target; }
    disconnect() { this.disconnected = true; }
  }
  class FakeContext {
    constructor() { this.state = "suspended"; this.destination = {}; FakeContext.instance = this; }
    resume() { this.state = "running"; return Promise.resolve(); }
    createGain() { this.gainNode = new Node(); this.gainNode.gain = { value: 1 }; return this.gainNode; }
    createMediaElementSource() { this.source = new Node(); return this.source; }
  }
  const m = createSfxMixer({ Audio: FakeAudio, AudioContext: FakeContext, minGapMs: 0 });
  m.unlock();
  assert.equal(FakeContext.instance.state, "running");
  assert.equal(m.play("thunder", "sound", 2.34), true);
  const context = FakeContext.instance;
  assert.equal(context.gainNode.gain.value, 2.34);
  m.setSpeaking(true);
  assert.equal(context.gainNode.gain.value, 2.34 * 0.4);
  m.setSpeaking(false);
  assert.equal(context.gainNode.gain.value, 2.34);
  m.stopAll();
  assert.equal(context.gainNode.disconnected, true);
  assert.equal(context.source.disconnected, true);
});

test("không có Web Audio thì âm vẫn phát với giới hạn volume của trình duyệt", () => {
  FakeAudio.all = [];
  const m = createSfxMixer({ Audio: FakeAudio, minGapMs: 0 });
  assert.equal(m.play("item", "sound", 3), true);
  assert.equal(FakeAudio.all[0].volume, 1);
  m.stopAll();
});

test("ưu tiên âm pháp bảo khi tiếng sấm chiếm kênh cuối", () => {
  FakeAudio.all = [];
  const m = createSfxMixer({ Audio: FakeAudio, minGapMs: 0 });
  m.play("thunder", "thunder", 0.5);
  m.play("thunder", "thunder", 0.5);
  m.play("kiem", "kiem", 0.5);
  m.play("va_cham", "va_cham", 0.5);
  assert.equal(m.play("item_linh_thach", "item", 0.5), true);
  assert.equal(FakeAudio.all[0].paused, true);
  assert.equal(FakeAudio.all[1].paused, undefined);
  assert.equal(m.stats().active, 4);
  assert.equal(m.play("trung", "egg", 0.5), true);
  assert.equal(FakeAudio.all[1].paused, true);
  m.stopAll();
});
