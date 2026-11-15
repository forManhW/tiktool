/*
 * Giọng đọc AI chạy trên máy (Piper). Hub dùng qua POST /api/tts { text } -> audio/wav.
 *
 * Thư mục TTS (TIKTOOL_TTS_DIR, mặc định vendor/tts):
 *   piper/piper.exe (+ dll, espeak-ng-data)   bộ đọc Piper
 *   voices/<tên>.onnx + <tên>.onnx.json         giọng đọc; lấy file .onnx đầu tiên theo tên (hoặc TIKTOOL_TTS_VOICE)
 *
 * Piper chạy 1 tiến trình liên tục (--json-input), nạp model 1 lần (khoảng 1–2 giây), sau đó mỗi câu khoảng 0,5–1,3 giây.
 * Câu đã đọc được lưu đệm theo nội dung, đọc lại câu giống hệt (vd. câu tỉ thí) là có ngay.
 */
const fs = require("fs");
const os = require("os");
const path = require("path");
const crypto = require("crypto");
const { spawn } = require("child_process");

const isAscii = (p) => /^[\x20-\x7e]*$/.test(p);
// Không dùng fs.cpSync: bản cài sẵn của Node (C++) làm sập cả tiến trình (0xC0000409) khi đường dẫn nguồn có dấu tiếng Việt.
function copyTree(from, to) {
  fs.mkdirSync(to, { recursive: true });
  for (const e of fs.readdirSync(from, { withFileTypes: true })) {
    const a = path.join(from, e.name), b = path.join(to, e.name);
    if (e.isDirectory()) copyTree(a, b); else fs.copyFileSync(a, b);
  }
}

/**
 * Piper (piper.exe + onnxruntime) crash ngay khi thư mục chứa nó có ký tự ngoài ASCII (vd. giải nén vào "D:\Tải về\..."
 * hoặc tài khoản Windows tên có dấu): thoát mã 0xC0000409, không ra tiếng. Thư mục có dấu cách thì vẫn chạy.
 * Khi đó chép bộ đọc (piper + voices, khoảng 85 MB, chỉ chép lần đầu hoặc khi bản đóng gói đổi) sang thư mục không dấu
 * và chạy từ đó. Trả về thư mục dùng được, hoặc thư mục gốc nếu không chép được.
 */
function asciiTtsDir(dir, { candidates, log: rawLog = () => {} } = {}) {
  // Hàm này chạy lúc hub đang khởi động (log của hub có thể chưa sẵn sàng): lỗi ghi log không được làm hỏng việc chép.
  const log = (...a) => { try { rawLog(...a); } catch {} };
  if (!dir || isAscii(path.resolve(dir))) return dir;
  const list = (candidates || [
    process.env.ProgramData && path.join(process.env.ProgramData, "TikTool", "tts"),
    process.env.PUBLIC && path.join(process.env.PUBLIC, "TikTool", "tts"),
    path.join(os.tmpdir(), "tiktool-tts-bin"),
    path.join(path.parse(path.resolve(dir)).root, "TikTool-tts")
  ]).filter((p) => p && isAscii(path.resolve(p)));
  // Dấu để biết bản đã chép còn đúng với bản đóng gói: kích thước + thời gian sửa của mọi file .exe/.dll/.onnx.
  const stamp = (root) => {
    const parts = [];
    const walk = (d) => { for (const e of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, e.name); if (e.isDirectory()) { if (e.name !== "espeak-ng-data") walk(p); } else if (/\.(exe|dll|onnx|json|ort)$/i.test(e.name)) { const st = fs.statSync(p); parts.push(`${path.relative(root, p)}:${st.size}:${Math.floor(st.mtimeMs)}`); } } };
    walk(root);
    return parts.sort().join("|");
  };
  let want;
  try { want = stamp(dir); } catch { return dir; }
  for (const target of list) {
    try {
      const mark = path.join(target, ".tiktool-copy");
      if (!(fs.existsSync(mark) && fs.readFileSync(mark, "utf8") === want)) {
        fs.rmSync(target, { recursive: true, force: true });
        fs.mkdirSync(target, { recursive: true });
        copyTree(dir, target);
        fs.writeFileSync(mark, want);
        log("info", "Đã chép giọng đọc sang thư mục không dấu", { from: dir, to: target });
      }
      return target;
    } catch (e) { log("warn", "Không chép được giọng đọc sang thư mục không dấu", { to: target, error: e.message }); }
  }
  return dir;
}

function createTts({ dir, voice, cacheDir, maxCache = 300, log = () => {} } = {}) {
  dir = asciiTtsDir(dir, { log });
  const exe = path.join(dir || "", "piper", "piper.exe");
  const voicesDir = path.join(dir || "", "voices");
  const model = (() => {
    try {
      const files = fs.readdirSync(voicesDir).filter((f) => f.endsWith(".onnx")).sort();
      const pick = voice ? files.find((f) => f === voice || f === `${voice}.onnx`) : files[0];
      return pick ? path.join(voicesDir, pick) : null;
    } catch { return null; }
  })();
  const available = fs.existsSync(exe) && !!model && fs.existsSync(`${model}.json`);
  const cache = new Map();   // hash -> file (Map giữ thứ tự chèn, dùng làm LRU)
  cacheDir = cacheDir || path.join(os.tmpdir(), "tiktool-tts");
  // Dọn file âm thanh cũ. Thư mục đang bị tiến trình khác giữ (vd. một TikTool khác, Piper cũ chưa tắt) thì bỏ qua,
  // không để hub dừng lúc khởi động: file trong cache đặt tên theo mã băm nên ghi đè được.
  if (available) { try { fs.rmSync(cacheDir, { recursive: true, force: true }); } catch {} fs.mkdirSync(cacheDir, { recursive: true }); }

  let proc = null, buffer = "", waiting = [];
  const queue = [];   // tránh gửi 2 câu cùng lúc: Piper trả kết quả theo thứ tự nhưng ta chờ từng câu cho chắc

  function start() {
    proc = spawn(exe, ["-m", model, "--json-input"], { cwd: path.dirname(exe), windowsHide: true });
    buffer = "";
    proc.stdout.on("data", (chunk) => {
      buffer += chunk;
      let i;
      while ((i = buffer.indexOf("\n")) >= 0) {
        const line = buffer.slice(0, i).trim(); buffer = buffer.slice(i + 1);
        const w = waiting.shift(); if (w) w.resolve(line);
      }
    });
    proc.stderr.on("data", () => {});
    const fail = (why) => { const ws = waiting; waiting = []; ws.forEach((w) => w.reject(new Error(why))); proc = null; };
    proc.on("error", (e) => { log("error", "Piper không chạy được", { error: e.message }); fail(e.message); });
    proc.on("exit", (code) => { if (proc) { log("warn", "Piper đã dừng", { code }); fail(`Piper exited ${code}`); } });
    log("info", "Piper TTS started", { voice: path.basename(model) });
  }

  function synth(text, file) {
    if (!proc) start();
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error("Piper quá 15 giây không trả lời")), 15000);
      waiting.push({ resolve: (v) => { clearTimeout(timer); resolve(v); }, reject: (e) => { clearTimeout(timer); reject(e); } });
      proc.stdin.write(`${JSON.stringify({ text, output_file: file })}\n`);
    });
  }

  let running = Promise.resolve();
  /** Trả về Buffer WAV của câu `text`. */
  async function speak(text) {
    if (!available) throw new Error("Chưa có giọng đọc (thiếu piper.exe hoặc model giọng)");
    text = String(text || "").replace(/\s+/g, " ").trim().slice(0, 300);
    if (!text) throw new Error("Câu rỗng");
    const key = crypto.createHash("sha1").update(text).digest("hex");
    if (cache.has(key)) { const f = cache.get(key); cache.delete(key); cache.set(key, f); return fs.promises.readFile(f); }
    const file = path.join(cacheDir, `${key}.wav`);
    const job = running.then(() => synth(text, file));
    running = job.catch(() => {});
    await job;
    cache.set(key, file);
    while (cache.size > maxCache) { const [oldKey, oldFile] = cache.entries().next().value; cache.delete(oldKey); fs.rm(oldFile, { force: true }, () => {}); }
    return fs.promises.readFile(file);
  }

  function stop() { if (proc) { const p = proc; proc = null; p.kill(); } }

  return { available, voice: model ? path.basename(model, ".onnx") : null, speak, stop };
}

module.exports = { createTts, asciiTtsDir };
