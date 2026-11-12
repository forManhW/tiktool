// Tải bộ đọc Piper + giọng tiếng Việt vào vendor/tts (thư mục này không lưu trong git).
//   npm run build:tts
// Nguồn chính thức: github.com/rhasspy/piper (bản 2023.11.14-2), huggingface.co/rhasspy/piper-voices.
// ⚠ Giấy phép giọng: xem docs/GIONG_DOC_AI.md trước khi phát bộ cài.
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const { execFileSync } = require("child_process");

const ROOT = path.join(__dirname, "..");
const OUT = path.join(ROOT, "vendor", "tts"), TMP = path.join(ROOT, "vendor", "tts-download");
const VOICE = "https://huggingface.co/rhasspy/piper-voices/resolve/main/vi/vi_VN/vais1000/medium/";
const FILES = [
  { url: "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip", name: "piper_windows_amd64.zip", size: 22477236 },
  { url: VOICE + "vi_VN-vais1000-medium.onnx", name: "vi_VN-vais1000-medium.onnx", sha256: "ec7c89e2c85f4d1edc24b6120c18aaf1bda614f06b511567eb9c7c0de15e2dab" },
  { url: VOICE + "vi_VN-vais1000-medium.onnx.json", name: "vi_VN-vais1000-medium.onnx.json" },
  { url: VOICE + "MODEL_CARD", name: "MODEL_CARD" }
];

(async () => {
  // Đã đủ file thì bỏ qua: tránh xóa thư mục đang bị piper.exe (hub đang chạy) giữ, dễ làm hỏng bộ giọng.
  const ready = ["piper/piper.exe", "piper/espeak-ng-data/phontab", "voices/vi_VN-vais1000-medium.onnx", "voices/vi_VN-vais1000-medium.onnx.json"].every((f) => fs.existsSync(path.join(OUT, f)));
  if (ready && !process.argv.includes("--force")) return console.log("Giọng đọc đã có sẵn:", OUT);
  fs.mkdirSync(TMP, { recursive: true });
  for (const f of FILES) {
    const file = path.join(TMP, f.name);
    if (!fs.existsSync(file)) {
      console.log("Tải", f.url);
      const res = await fetch(f.url, { redirect: "follow" });
      if (!res.ok) throw new Error(`${f.name}: HTTP ${res.status}`);
      fs.writeFileSync(file, Buffer.from(await res.arrayBuffer()));
    }
    const data = fs.readFileSync(file);
    if (f.size && data.length !== f.size) throw new Error(`${f.name}: sai kích thước (${data.length} ≠ ${f.size})`);
    if (f.sha256) { const h = crypto.createHash("sha256").update(data).digest("hex"); if (h !== f.sha256) throw new Error(`${f.name}: sai mã băm ${h}`); }
  }
  fs.rmSync(OUT, { recursive: true, force: true });
  fs.mkdirSync(path.join(OUT, "voices"), { recursive: true });
  execFileSync("powershell", ["-NoProfile", "-Command", `Expand-Archive -Path '${path.join(TMP, FILES[0].name)}' -DestinationPath '${OUT}' -Force`], { stdio: "inherit" });
  for (const f of FILES.slice(1)) fs.copyFileSync(path.join(TMP, f.name), path.join(OUT, "voices", f.name));
  if (!fs.existsSync(path.join(OUT, "piper", "piper.exe"))) throw new Error("Giải nén xong nhưng không thấy piper/piper.exe");
  console.log("Xong:", OUT);
})().catch((e) => { console.error(e.message); process.exit(1); });
