const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { createTts } = require("../core/tts");

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
