// Đọc log lệnh game của TikTool hub (SQLite) sau khi chạy test: node Game/DaiChienXanhDo/tools/read-hub-log.js <thư-mục-kết-quả>/data/tiktool.db
const { DatabaseSync } = require("node:sqlite");
const db = new DatabaseSync(process.argv[2]);
const rows = db.prepare("SELECT at, level, message, details FROM event_log WHERE message LIKE 'Game command%' ORDER BY at").all();
for (const r of rows) {
  const d = JSON.parse(r.details || "{}");
  console.log([r.level.padEnd(5), r.message.padEnd(24), String(d.action || "").padEnd(18), d.status || "", d.error || "", d.latencyMs != null ? d.latencyMs + "ms" : "", JSON.stringify(d.params || d.command?.params || "")].join(" "));
}
if (!rows.length) console.log(JSON.stringify(db.prepare("SELECT message, details FROM event_log ORDER BY at DESC LIMIT 3").all(), null, 1));
