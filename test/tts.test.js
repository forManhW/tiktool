const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { createTts, asciiTtsDir } = require("../core/tts");

test("thiếu piper.exe hoặc model giọng thì báo không có giọng, speak báo lỗi rõ ràng", async () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "tts-empty-"));
  const tts = createTts({ dir });
  assert.equal(tts.available, false);
  await assert.rejects(tts.speak("xin chào"), /Chưa có giọng đọc/);
});

const VENDOR = path.join(__dirname, "..", "vendor", "tts");
test("Piper thật (chỉ chạy khi có vendor/tts): đọc câu tiếng Việt ra WAV, câu lặp lại lấy từ bộ đệm", { skip: !fs.existsSync(path.join(VENDOR, "piper", "piper.exe")) }, async () => {
  const tts = createTts({ dir: VENDOR, cacheDir: fs.mkdtempSync(path.join(os.tmpdir(), "tts-cache-")) });
  try {
    assert.equal(tts.available, true);
    const wav = await tts.speak("Chúc mừng đạo hữu đạt cảnh giới Trúc Cơ");
    assert.equal(wav.subarray(0, 4).toString(), "RIFF");
    assert.ok(wav.length > 20000);
    const t = Date.now();
    const again = await tts.speak("Chúc mừng đạo hữu đạt cảnh giới Trúc Cơ");
    assert.ok(Date.now() - t < 100, "câu lặp lại phải lấy từ bộ đệm");
    assert.equal(again.length, wav.length);
  } finally { tts.stop(); }
});

test("thư mục giọng đọc có dấu tiếng Việt: chép sang thư mục không dấu (1 lần), thư mục không dấu thì dùng luôn", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "tts-viet-"));
  const src = path.join(root, "Tải về Hưng", "tts");
  fs.mkdirSync(path.join(src, "piper", "espeak-ng-data"), { recursive: true });
  fs.mkdirSync(path.join(src, "voices"), { recursive: true });
  fs.writeFileSync(path.join(src, "piper", "piper.exe"), "exe");
  fs.writeFileSync(path.join(src, "piper", "espeak-ng-data", "vi_dict"), "dict");
  fs.writeFileSync(path.join(src, "voices", "v.onnx"), "model");
  const target = path.join(root, "ascii", "tts"), logs = [];
  const out = asciiTtsDir(src, { candidates: [path.join(root, "Có dấu"), target], log: (l, m) => logs.push(m) });
  assert.equal(out, target, "bỏ qua ứng viên có dấu, chọn thư mục không dấu");
  assert.equal(fs.readFileSync(path.join(target, "piper", "espeak-ng-data", "vi_dict"), "utf8"), "dict");
  assert.equal(fs.readFileSync(path.join(target, "voices", "v.onnx"), "utf8"), "model");
  assert.equal(logs.length, 1);
  asciiTtsDir(src, { candidates: [target], log: (l, m) => logs.push(m) });
  assert.equal(logs.length, 1, "bản chép còn đúng thì không chép lại");
  const ascii = path.join(root, "ascii", "tts");
  assert.equal(asciiTtsDir(ascii, { candidates: [path.join(root, "x")] }), ascii, "đường dẫn không dấu dùng luôn");
});
