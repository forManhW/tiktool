// Sinh Game/DaiChienXanhDo/manifest.json: mỗi hiệu ứng một hàm cho Xanh và một hàm cho Đỏ.
// Chạy: node Game/DaiChienXanhDo/tools/gen-manifest.js Game/DaiChienXanhDo/manifest.json
const fs = require("fs");
const out = process.argv[2];
const effects = [
  ["join", "Vào phe", null],
  ["spawn", "Triệu hồi lính mang tên mình", { min: 1, max: 20, label: "Số lính" }],
  ["like_power", "Tim tiếp sức", { min: 1, max: 100, label: "Mức tiếp sức" }],
  ["superhero", "Triệu hồi Siêu Chiến Binh mang tên mình", { min: 1, max: 5, label: "Số siêu nhân" }],
  ["lightning", "Sấm sét (bậc 4)", { min: 1, max: 10, label: "Số đợt sấm" }],
  ["tank", "Xe tăng (bậc 5)", { min: 1, max: 3, label: "Số xe" }],
  ["meteor", "Mưa thiên thạch (bậc 6)", { min: 1, max: 3, label: "Số đợt" }],
  ["giant_robot", "Robot khổng lồ (bậc 7)", { min: 1, max: 1, label: "Số robot", scale: false }],
];
const param = (p) => (p ? { amount: { type: "number", minimum: p.min, maximum: p.max, step: 1, label: p.label, ...(p.scale === false ? { scale: false } : {}) } } : {});
const actions = [];
for (const [team, name] of [["blue", "XANH"], ["red", "ĐỎ"]])
  for (const [id, label, p] of effects) actions.push({ id: `${team}_${id}`, name: `${name}: ${label}`, parameters: param(p) });
actions.push(
  { id: "ally_like_power", name: "PHE ĐÃ VÀO: Tim tiếp sức", parameters: param(effects[2][2]) },
  { id: "ally_spawn", name: "PHE ĐÃ VÀO: Thêm lính", parameters: param(effects[1][2]) },
  { id: "event_x2", name: "SỰ KIỆN: X2 sức mạnh ngay", parameters: {} },
  { id: "event_disaster", name: "SỰ KIỆN: Thảm họa ngay", parameters: {} },
  { id: "admin_reset_round", name: "QUẢN TRỊ: Reset ván (không tính điểm)", parameters: {} },
  { id: "admin_reset_score", name: "QUẢN TRỊ: Reset tỉ số hôm nay", parameters: {} },
);
const manifest = {
  id: "red-blue-brawl",
  name: "Đại Chiến Xanh Đỏ",
  version: "0.1.0",
  launch: { type: "native", exe: "Build/DaiChienXanhDo.exe" },
  connection: { transport: "websocket", port: 8765 },
  window: { width: 1080, height: 1080 },
  triggers: ["gift", "like", "comment", "follow", "share"],
  liveEvents: [],
  actions,
  defaultMappings: [
    { eventType: "comment", matchMode: "exact", keywords: ["xanh", "blue", "1"], action: "blue_join", params: {}, priority: 20, userCooldownMs: 5000 },
    { eventType: "comment", matchMode: "exact", keywords: ["đỏ", "do", "red", "2"], action: "red_join", params: {}, priority: 20, userCooldownMs: 5000 },
  ],
};
fs.writeFileSync(out, JSON.stringify(manifest, null, 2) + "\n");
console.log(`${actions.length} actions -> ${out}`);
