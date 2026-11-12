// In bảng ngưỡng cảnh giới (Markdown) từ src/config.js, kèm quy đổi theo mapping THAM CHIẾU.
// node tools/realm-table.js [tuViMoiTim=1] [tuViMoiXu=50]
// Game không tự quy đổi (Q4): hai số dưới đây chỉ để đọc bảng; số thật do streamer cài trong TikTool.
const cfg = require("../src/config");
const perLike = Number(process.argv[2]) || 1, perXu = Number(process.argv[3]) || 50;
const fmt = (n) => Math.ceil(n).toLocaleString("vi-VN");
const rows = [];
cfg.realms.forEach((realm) => realm.stages.forEach((min, i) => rows.push({ label: (realm.name + " " + (realm.stageNames[i] || "")).trim(), min })));
console.log(`| Cảnh giới | Tu vi tích lũy | Cần thêm từ bậc trước | ≈ Tim (1 tim = ${perLike}) | ≈ Xu quà (1 xu = ${perXu}) |`);
console.log("|---|---:|---:|---:|---:|");
rows.forEach((r, i) => console.log(`| ${r.label} | ${fmt(r.min)} | ${i ? "+" + fmt(r.min - rows[i - 1].min) : "-"} | ${fmt(r.min / perLike)} | ${fmt(r.min / perXu)} |`));
