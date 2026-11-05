import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { Presentation, PresentationFile, FileBlob } from 'file:///C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const workspaceDir = 'D:/DuAnCaNhan/TikTool';
const buildDir = path.join(workspaceDir, '.assignment3-build');
const outDir = path.join(workspaceDir, 'deliverables/assignment3');
const SKILL_DIR = 'C:/Users/Admin/.codex/plugins/cache/openai-primary-runtime/presentations/26.1007.11041/skills/presentations';
const RUNTIME_PYTHON = 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';
process.env.RUNTIME_NODE_MODULES = 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const { finalizePresentation, resolvePresentationFont } = await import(pathToFileURL(path.join(SKILL_DIR, 'container_tools/artifact_tool_utils.mjs')).href);
const font = resolvePresentationFont({ fontFamily: 'Arial' });
const C = { ink: '#17252C', muted: '#56656B', white: '#FAFAF6', teal: '#087F83', dark: '#10272D', light: '#E5F1EE' };
const deck = Presentation.create({ slideSize: { width: 1280, height: 720 } });
const PMI1 = 'https://www.pmi.org/learning/library/leveraging-new-practice-standard-project-estimating-6222';
const PMI2 = 'https://www.pmi.org/learning/library/estimation-event-process-7460';
const localSources = 'Nguồn dự án: README.md, package.json, server.js, live_adapter.py, requirements-live.txt, Game/Clock/manifest.json và Game/TowerPK/manifest.json trong D:/DuAnCaNhan/TikTool. Mã server.js hiện gọi adapter Python TikTokLive. README mô tả connector Node trước đây nên công nghệ kết nối trong bài theo mã nguồn hiện tại.';
const assumptions = 'Giả định học tập: phát triển MVP tương đương phạm vi minh họa từ đầu với hai demo Clock và Tháp PK. Đây không phải dự toán phần việc còn lại, báo giá thị trường hay chi phí thực tế đã phát sinh. Một người có sẵn kỹ năng JavaScript, Python cơ bản và WebSocket, sẵn laptop, Internet và tài khoản có thể dùng để kiểm thử LIVE. Công suất 20 giờ/tuần, đơn giá quy đổi 150.000 đồng/giờ. Tổng effort 80 giờ, lịch cơ sở 4 tuần. Không gồm mua máy mới, bảo trì sau MVP, phát hành thương mại, thuế hoặc lợi nhuận nhà cung cấp.';

function text(slide, value, x, y, w, h, size = 28, color = C.ink, bold = false, align = 'left') {
  const s = slide.shapes.add({ geometry: 'textbox', name: `text-${slide.shapes.items.length + 1}`, position: { left: x, top: y, width: w, height: h }, fill: 'none', line: { fill: 'none', width: 0 } });
  s.text = value;
  s.text.style = { typeface: font, fontSize: size, color, bold, alignment: align, verticalAlignment: 'top', autoFit: 'none', insets: 0, wrap: 'square' };
  return s;
}
function slide(title, number, dark = false) {
  const s = deck.slides.add();
  s.background.fill = dark ? C.dark : C.white;
  if (title) text(s, title, 70, 58, 1140, 100, 43, dark ? '#FFFFFF' : C.ink, true);
  text(s, String(number).padStart(2, '0'), 1140, 653, 70, 28, 18, dark ? '#93B6B8' : C.muted, false, 'right');
  return s;
}
function table(s, values, widths, y, height, fontSize = 25) {
  const t = s.tables.add({ rows: values.length, columns: values[0].length, left: 70, top: y, width: 1140, height, columnWidths: widths, values });
  t.styleOptions = { headerRow: false, bandedRows: false, totalRow: false };
  t.borders.assign({ fill: '#D0DCD8', width: 1, style: 'solid' });
  t.cells.block({ row: 0, column: 0, rowCount: values.length, columnCount: values[0].length }).assign({ fill: C.white, textStyle: { typeface: font, fontSize, color: C.ink, verticalAlignment: 'middle' }, margins: { left: 16, right: 12, top: 10, bottom: 8 }, anchor: 'center' });
  t.cells.block({ row: 0, column: 0, rowCount: 1, columnCount: values[0].length }).assign({ fill: C.dark, textStyle: { typeface: font, fontSize, color: '#FFFFFF', bold: true } });
  return t;
}
function notes(s, sec, script, sources = '') {
  s.speakerNotes.text = `Thời lượng: ${sec} giây.\n\n${script}\n\nCơ sở số liệu\n${assumptions}\n\nNguồn\n${sources}`;
  return script;
}
const scripts = [];

{
  const s = slide('', 1, true);
  text(s, 'TikTool', 70, 118, 1140, 108, 88, '#FFFFFF', true);
  text(s, 'Hoạch định nguồn lực\nvà ước tính chi phí', 70, 265, 1110, 160, 55, '#FFFFFF', true);
  text(s, 'Individual assignment 3', 74, 500, 1000, 48, 27, '#91D1CA');
  text(s, 'Ví dụ MVP ứng dụng game tương tác TikTok LIVE', 74, 557, 1080, 45, 25, '#C0D3D4');
  scripts.push({ title: 'TikTool: Hoạch định nguồn lực và ước tính chi phí', sec: 15, script: notes(s, 15, 'Em xin trình bày việc hoạch định nguồn lực và ước tính chi phí qua dự án TikTool. Bài sử dụng một kịch bản MVP để cho thấy cách chuyển phạm vi công việc thành lịch thực hiện và ngân sách.', localSources) });
}
{
  const s = slide('Phạm vi ví dụ TikTool', 2);
  text(s, 'Ứng dụng desktop local giúp sự kiện LIVE\ntác động đến game và hiển thị lên OBS.', 70, 190, 1120, 105, 34, C.ink, true);
  text(s, 'Sản phẩm trong kịch bản MVP', 70, 342, 1120, 42, 27, C.teal, true);
  text(s, 'Kết nối LIVE và quản lý một phiên game\nÁnh xạ quà tặng sang hành động, có cooldown\nHai demo Clock và Tháp PK, kèm overlay OBS\nTest Lab, giả lập sự kiện và nhật ký xử lý', 70, 404, 1100, 178, 29);
  text(s, 'Kịch bản phát triển MVP từ đầu. Giờ công và chi phí là giả định học tập.', 70, 617, 1060, 35, 22, C.muted);
  scripts.push({ title: 'Phạm vi ví dụ TikTool', sec: 30, script: notes(s, 30, 'TikTool là ứng dụng desktop chạy trên máy người livestream. Hệ thống nhận sự kiện LIVE, áp dụng mapping và gửi lệnh cho game. Ví dụ, quà Rose có thể thêm thời gian vào Clock hoặc thêm khối vào Tháp PK. Phạm vi bài gồm hai demo, overlay OBS và công cụ kiểm thử. Đây là kịch bản xây dựng MVP từ đầu, không phải chi phí đã phát sinh hay phần việc còn lại của dự án.', localSources) });
}
{
  const s = slide('Hoạch định nguồn lực', 3);
  text(s, 'Xác định nguồn lực cần dùng, số lượng\nvà thời điểm sử dụng cho từng công việc.', 70, 180, 1120, 103, 33, C.ink, true);
  text(s, 'Nhân lực', 70, 327, 520, 45, 28, C.teal, true);
  text(s, '1 người kiêm phân tích, thiết kế,\nlập trình và kiểm thử.\nKhả dụng: 20 giờ mỗi tuần.', 70, 390, 535, 160, 28);
  text(s, 'Thiết bị và công cụ', 680, 327, 530, 45, 28, C.teal, true);
  text(s, 'Laptop, Internet, tài khoản LIVE.\nElectron, Node.js, Python,\nWebSocket, SQLite và OBS.', 680, 390, 530, 160, 28);
  text(s, 'Đầu ra: danh sách nguồn lực, người phụ trách và lịch phân bổ theo công việc.', 70, 607, 1080, 55, 24, C.muted);
  scripts.push({ title: 'Hoạch định nguồn lực', sec: 40, script: notes(s, 40, 'Hoạch định nguồn lực là xác định cần ai, cần công cụ gì, bao nhiêu và dùng vào lúc nào. Ta bắt đầu từ phạm vi, chia nhỏ công việc, xác định kỹ năng và kiểm tra lịch khả dụng. Trong ví dụ, một người đảm nhiệm các vai trò nhưng chỉ làm 20 giờ mỗi tuần, nên các vai trò không tương đương nhiều nhân viên. Thiết bị có sẵn gồm laptop và Internet. Công nghệ theo mã nguồn hiện tại gồm Electron, Node.js, adapter Python, WebSocket và SQLite. Đầu ra là danh sách nguồn lực và lịch phân bổ.', `${localSources}\nKhái niệm nguồn lực, kỹ năng và lịch khả dụng tham khảo PMI: ${PMI1}`) });
}
{
  const s = slide('Kế hoạch 80 giờ trong 4 tuần', 4);
  const values = [
    ['Tuần', 'Công việc và phân bổ giờ', 'Giờ'],
    ['1', 'Phân tích phạm vi 8 giờ, thiết kế UI 12 giờ', 20],
    ['2', 'Hub, kết nối LIVE và rule engine 20 giờ', 20],
    ['3', 'Hub 4 giờ, tích hợp hai game và OBS 16 giờ', 20],
    ['4', 'Kiểm thử và sửa lỗi 16 giờ, tài liệu 4 giờ', 20],
    ['Tổng', 'Một người phụ trách toàn bộ các vai trò', 80],
  ];
  const t = table(s, values, [130, 875, 135], 182, 377, 25);
  t.cells.block({ row: 5, column: 0, rowCount: 1, columnCount: 3 }).assign({ fill: C.light, textStyle: { bold: true, color: C.teal, typeface: font, fontSize: 25 } });
  text(s, '80 giờ công ÷ 20 giờ/tuần = 4 tuần lịch', 70, 586, 1120, 50, 32, C.teal, true);
  text(s, 'Lịch cơ sở chưa có thời gian dự phòng. Kiểm tra từng module nằm trong giờ lập trình.', 70, 645, 1060, 42, 20, C.muted);
  scripts.push({ title: 'Kế hoạch 80 giờ trong 4 tuần', sec: 45, script: notes(s, 45, 'Kế hoạch chia MVP thành các gói công việc. Tuần một dành 8 giờ phân tích và 12 giờ giao diện. Tuần hai làm 20 giờ cho Hub, listener và rule engine. Tuần ba hoàn thành 4 giờ Hub còn lại rồi tích hợp game và OBS trong 16 giờ. Tuần bốn dành 16 giờ kiểm thử tích hợp và sửa lỗi, cùng 4 giờ tài liệu. Các kiểm tra nhỏ diễn ra ngay khi lập trình. Tổng effort là 80 giờ. Vì chỉ có 20 giờ khả dụng mỗi tuần, thời gian lịch là 4 tuần. Lịch này chưa có dự phòng tiến độ.', `${localSources}\nWBS và lịch nguồn lực tham khảo PMI: ${PMI1}. Toàn bộ phân bổ giờ do bài tập giả định.`) });
}
{
  const s = slide('Ước tính chi phí', 5);
  text(s, 'Dự báo số tiền cần cho phạm vi công việc,\ndựa trên giờ công, đơn giá và khoản phụ trợ.', 70, 169, 1140, 100, 31, C.ink, true);
  text(s, 'Bottom-up: tính từng gói rồi cộng lại', 70, 305, 1100, 49, 29, C.teal, true);
  text(s, 'Game và OBS: 16 giờ × 150.000 đ/giờ = 2.400.000 đ', 70, 365, 1130, 55, 29);
  text(s, 'Ba điểm PERT cho gói Hub', 70, 451, 1090, 42, 29, C.teal, true);
  text(s, 'Lạc quan O = 16 giờ, thường gặp M = 24 giờ, bi quan P = 32 giờ', 70, 509, 1140, 45, 26);
  text(s, 'E = (O + 4M + P) / 6 = 24 giờ', 70, 565, 1140, 53, 34, C.ink, true);
  text(s, 'Đơn giá và ba điểm đều là giả định. 24 giờ Hub đã nằm trong tổng 80 giờ.', 70, 641, 1050, 36, 21, C.muted);
  scripts.push({ title: 'Ước tính chi phí', sec: 45, script: notes(s, 45, 'Ước tính chi phí dự báo số tiền cần để thực hiện phạm vi công việc. Bài dùng bottom-up, nghĩa là tính chi phí từng gói rồi cộng lại. Ví dụ, game và OBS cần 16 giờ. Nhân với đơn giá giả định 150 nghìn đồng một giờ, ta được 2,4 triệu đồng. Với gói Hub có độ bất định cao hơn, em minh họa PERT bằng ba mức 16, 24 và 32 giờ. Công thức lấy mức thường gặp nhân bốn rồi chia tổng cho sáu, cho kết quả 24 giờ. 24 giờ này đã có trong kế hoạch, không cộng thêm lần nữa.', `Bottom-up tham khảo PMI: ${PMI2}\nPERT tham khảo PMI, phần 3-Point Technique: ${PMI1}\nVí dụ số học và đơn giá là giả định của bài.`) });
}
{
  const s = slide('Chi phí nhân lực theo công việc', 6);
  const values = [
    ['Gói công việc', 'Giờ', 'Chi phí (đồng)'],
    ['Phân tích yêu cầu và phạm vi', 8, '1,200,000'],
    ['Thiết kế và xây dựng giao diện', 12, '1,800,000'],
    ['Hub, listener và rule engine', 24, '3,600,000'],
    ['Tích hợp hai game và overlay OBS', 16, '2,400,000'],
    ['Kiểm thử tích hợp và sửa lỗi', 16, '2,400,000'],
    ['Tài liệu và chuẩn bị bàn giao', 4, '600,000'],
    ['Tổng nhân lực', 80, '12,000,000'],
  ];
  const t = table(s, values, [730, 115, 295], 173, 432, 25);
  t.cells.block({ row: 7, column: 0, rowCount: 1, columnCount: 3 }).assign({ fill: C.light, textStyle: { bold: true, color: C.teal, typeface: font, fontSize: 25 } });
  text(s, 'Đơn giá quy đổi thống nhất: 150.000 đ/giờ. Giờ tự làm vẫn có giá trị kinh tế.', 70, 636, 1050, 38, 23, C.muted);
  scripts.push({ title: 'Chi phí nhân lực theo công việc', sec: 40, script: notes(s, 40, 'Bảng này áp dụng cùng đơn giá giả định cho cả sáu gói công việc. Tổng 80 giờ nhân 150 nghìn đồng bằng 12 triệu đồng. Gói Hub là khoản lớn nhất với 3,6 triệu đồng do phải xử lý kết nối, mapping và điều phối lệnh. Kiểm thử vẫn có ngân sách riêng để kiểm tra combo quà, cooldown và đồng bộ overlay. Một người có thể tự làm mà không trả lương trực tiếp, nhưng 12 triệu vẫn phản ánh giá trị kinh tế của thời gian. Cần phân biệt giá trị này với tiền mặt thực chi.', `${localSources}\nPhương pháp cộng các gói công việc: ${PMI2}. Giờ công và đơn giá là giả định.`) });
}
{
  const s = slide('Ngân sách dự kiến 14.375.000 đồng', 7);
  const values = [
    ['Khoản mục', 'Chi phí (đồng)'],
    ['Nhân lực quy đổi', '12,000,000'],
    ['Phụ trợ: test LIVE 300.000 đ, điện/mạng 200.000 đ', '500,000'],
    ['Chi phí cơ sở', '12,500,000'],
    ['Dự phòng 15% × 12.500.000 đ', '1,875,000'],
    ['Ngân sách gồm dự phòng', '14,375,000'],
  ];
  const t = table(s, values, [845, 295], 179, 366, 25);
  t.cells.block({ row: 5, column: 0, rowCount: 1, columnCount: 2 }).assign({ fill: C.light, textStyle: { bold: true, color: C.teal, typeface: font, fontSize: 25 } });
  text(s, 'Sẵn laptop. Không thuê máy chủ trong kịch bản local.', 70, 578, 1110, 40, 26, C.ink);
  text(s, 'Dự phòng 15% là lựa chọn minh họa. Chưa gồm mua máy mới và bảo trì sau MVP.', 70, 637, 1060, 39, 22, C.muted);
  scripts.push({ title: 'Ngân sách dự kiến 14.375.000 đồng', sec: 40, script: notes(s, 40, 'Ngoài nhân lực, bài giả định 300 nghìn đồng kiểm thử LIVE và 200 nghìn đồng điện, Internet. Laptop có sẵn và ứng dụng chạy local nên không phát sinh mua máy hay thuê máy chủ trong kịch bản này. Chi phí cơ sở là 12,5 triệu đồng. Dự phòng 15% tương đương 1,875 triệu, đưa ngân sách lên 14,375 triệu đồng. Tỷ lệ dự phòng là giả định minh họa, cần điều chỉnh khi có dữ liệu rủi ro thực tế. Ngân sách chưa bao gồm bảo trì sau MVP hoặc phát hành thương mại.', `${localSources}\nTất cả mức tiền phụ trợ và tỷ lệ dự phòng là giả định của bài. Tổng = 80 × 150.000 + 300.000 + 200.000 + (12.500.000 × 15%) = 14.375.000 đồng. Khoản test LIVE là hạn mức tiền giả định, không suy ra số lượng coin hoặc giá quà.`) });
}
{
  const s = slide('Rủi ro và điều chỉnh kế hoạch', 8, true);
  text(s, 'Kết nối LIVE gián đoạn hoặc combo quà sai\ncó thể làm tăng thời gian xử lý Hub.', 70, 183, 1140, 101, 33, '#FFFFFF', true);
  text(s, 'Ví dụ phát sinh thêm 8 giờ', 70, 334, 1100, 42, 29, '#91D1CA', true);
  text(s, 'Chi phí tăng 1.200.000 đ, trong mức dự phòng 1.875.000 đ.\nGiữ 20 giờ/tuần thì lịch tăng từ 4 lên 4,4 tuần.', 70, 393, 1135, 99, 28, '#FFFFFF');
  text(s, 'Ghi giờ thực tế mỗi tuần, kiểm thử bằng Event Injector\nvà ước tính lại trước khi thêm game hoặc tính năng.', 70, 524, 1120, 96, 28, '#C0D3D4');
  text(s, 'Nguồn lực quyết định lịch thực hiện. Ước tính chi phí tạo cơ sở cho ngân sách.', 70, 644, 1060, 39, 21, '#91D1CA');
  scripts.push({ title: 'Rủi ro và điều chỉnh kế hoạch', sec: 45, script: notes(s, 45, 'Rủi ro chính là kết nối LIVE và xử lý combo quà khiến gói Hub cần thêm thời gian. Giả sử phát sinh 8 giờ, chi phí tăng 1,2 triệu đồng, vẫn nằm trong khoản dự phòng 1,875 triệu. Nhưng ngân sách dự phòng không tạo thêm thời gian làm việc: với 20 giờ một tuần, 88 giờ sẽ cần 4,4 tuần. Cách kiểm soát là ghi giờ và chi phí thực tế hàng tuần, dùng Event Injector khi LIVE gián đoạn, rồi ước tính lại nếu mở rộng phạm vi. Kết luận, hoạch định nguồn lực tạo lịch khả thi, còn ước tính chi phí tạo cơ sở cho ngân sách. Ví dụ TikTool có một người, 80 giờ và ngân sách 14,375 triệu đồng.', `${localSources}\nQuản lý và cập nhật ước tính tham khảo PMI: ${PMI1}. Phát sinh 8 giờ là tình huống giả định, không phải lỗi đã đo được. Chi phí cơ sở sau phát sinh 13.700.000 đồng, thấp hơn ngân sách có dự phòng 14.375.000 đồng, còn dư 675.000 đồng nếu không có phát sinh khác.`) });
}

await fs.mkdir(buildDir, { recursive: true });
await fs.mkdir(outDir, { recursive: true });
if (scripts.reduce((sum, item) => sum + item.sec, 0) !== 300) throw new Error('Thời lượng phải là 300 giây');
const hours = [8, 12, 24, 16, 16, 4];
const baseCost = 80 * 150000 + 500000;
if (hours.reduce((a, b) => a + b, 0) !== 80 || baseCost + baseCost * 15 / 100 !== 14375000) throw new Error('Sai tổng giờ/chi phí');
const candidatePath = path.join(buildDir, 'candidate.pptx');
await (await PresentationFile.exportPptx(deck)).save(candidatePath);
console.log('Draft exported');
const finalPath = path.join(outDir, 'TikTool_Assignment3.pptx');
const result = await finalizePresentation({
  workspaceDir, candidatePath, finalPath, pythonExecutable: RUNTIME_PYTHON,
  integrityValidatorPath: path.join(SKILL_DIR, 'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath: path.join(SKILL_DIR, 'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs: ['--expected-slide-size-emu', '12192000,6858000', '--validate-bullet-geometry', '--validate-heading-fit', '--require-native-table-slide', '4', '--require-native-table-slide', '6', '--require-native-table-slide', '7'],
  explicitTotalSlideCount: 8,
  requiredNativeTableOwnerSlides: [4, 6, 7],
  requiredNativeChartOwnerSlides: [],
  tableArithmeticContracts: [
    { slide: 4, table: 1, label_column: 0, total_row: 5, value_columns: [2], component_rows: [1,2,3,4] },
    { slide: 6, table: 1, label_column: 0, total_row: 7, value_columns: [1,2], component_rows: [1,2,3,4,5,6] },
    { slide: 7, table: 1, label_column: 0, total_row: 3, value_columns: [1], component_rows: [1,2] },
    { slide: 7, table: 1, label_column: 0, total_row: 5, value_columns: [1], component_rows: [3,4] },
  ],
  fontPolicy: { basis: 'design', families: [font] },
  verifyArtifactToolImport: true,
  receiptPath: path.join(buildDir, 'validation.json'),
});
console.log('Finalized', JSON.stringify({ finalPath, passed: result.passed }));
// Render the finalized file, not just the authoring object.
const finalDeck = await PresentationFile.importPptx(await FileBlob.load(finalPath));
for (const [i, s] of finalDeck.slides.items.entries()) {
  const png = await finalDeck.export({ slide: s, format: 'png', scale: 1 });
  await fs.writeFile(path.join(buildDir, `slide-${i + 1}.png`), new Uint8Array(await png.arrayBuffer()));
  console.log(`Rendered slide ${i + 1}`);
}
let elapsed = 0;
const timing = scripts.map((item, i) => {
  const fmt = n => `${Math.floor(n / 60)}:${String(n % 60).padStart(2, '0')}`;
  const start = elapsed;
  elapsed += item.sec;
  return `| ${i + 1} | ${item.title} | ${fmt(start)}–${fmt(elapsed)} | ${item.sec} giây |`;
}).join('\n');
const scriptDoc = `# TikTool – Individual assignment 3\n\n## Bài tập\n\nGiải thích hoạch định nguồn lực và ước tính chi phí, kèm ví dụ tương ứng. Trình bày bằng slide trong 5 phút. Đề ghi thời gian chuẩn bị 20 phút.\n\n## Giả định và phạm vi\n\n${assumptions}\n\nPhạm vi ví dụ: ứng dụng desktop chạy local, quản lý một phiên LIVE và game, mapping có cooldown, hai demo Clock và Tháp PK, overlay OBS, Test Lab/Event Injector và nhật ký. Kho mã có thêm các game khác nhưng bài chỉ dùng hai demo này.\n\nChi phí nhân lực là giá trị quy đổi thời gian. Nếu tự làm và không nhận lương, tiền mặt thực chi sẽ khác. Điện/mạng 200.000 đồng là phần phân bổ cho giai đoạn MVP, test LIVE 300.000 đồng là hạn mức giả định. Không sử dụng bảng giá coin, quà tặng hoặc giá dịch vụ thị trường.\n\n## Phân bổ thời gian trình bày\n\n| Slide | Nội dung | Mốc thời gian | Thời lượng |\n| --- | --- | --- | --- |\n${timing}\n\nCác mốc là mục tiêu khi luyện nói, tổng 300 giây. Lời nói bên dưới tương ứng với speaker notes trong PowerPoint.\n\n${scripts.map((item, i) => `## Slide ${i + 1}. ${item.title}\n\n${item.script}\n`).join('\n')}\n## Bảng tính cơ sở\n\nĐơn giá giả định: 150.000 đồng/giờ cho mọi vai trò của cùng một người.\n\n| Gói công việc | Giờ | Chi phí (đồng) |\n| --- | ---: | ---: |\n| Phân tích yêu cầu và phạm vi | 8 | 1.200.000 |\n| Thiết kế và xây dựng giao diện | 12 | 1.800.000 |\n| Hub, listener và rule engine | 24 | 3.600.000 |\n| Tích hợp hai game và OBS | 16 | 2.400.000 |\n| Kiểm thử tích hợp và sửa lỗi | 16 | 2.400.000 |\n| Tài liệu và bàn giao | 4 | 600.000 |\n| **Tổng nhân lực** | **80** | **12.000.000** |\n\nChi phí cơ sở = 12.000.000 + 300.000 + 200.000 = 12.500.000 đồng.\n\nDự phòng = 12.500.000 × 15% = 1.875.000 đồng.\n\n**Ngân sách có dự phòng = 14.375.000 đồng.**\n\nPERT cho Hub: (16 + 4 × 24 + 32) ÷ 6 = 24 giờ. Đây là gói đã có trong 80 giờ. Không cộng lặp.\n\nNếu Hub cần thêm 8 giờ: chi phí tăng 1.200.000 đồng, chi phí cơ sở mới 13.700.000 đồng và khoản dự phòng còn 675.000 đồng, với giả định các khoản khác giữ nguyên. Thời gian lịch = 88 ÷ 20 = 4,4 tuần. Tiền dự phòng không thay thế dự phòng tiến độ.\n\n## Câu hỏi có thể gặp\n\n**Vì sao dùng 150.000 đồng/giờ?** Đây là đơn giá quy đổi giả định để minh họa phép tính. Dự toán thực tế cần đơn giá theo năng lực, hình thức thuê và dữ liệu phù hợp.\n\n**Vì sao chỉ một người mà có nhiều vai trò?** Người làm dự án lần lượt đảm nhiệm các vai trò. Các vai trò không phải năm nhân viên. Lịch tuần không vượt 20 giờ.\n\n**Vì sao ứng dụng miễn phí vẫn có chi phí?** Thời gian của người phát triển và các khoản vận hành có giá trị kinh tế. Không thuê server trong ví dụ local không có nghĩa toàn dự án không tốn chi phí.\n\n**Vì sao dùng dự phòng 15%?** Đây là giả định phục vụ bài tập, không phải tỷ lệ bắt buộc từ PMI. Dự toán thật cần định lượng rủi ro và điều chỉnh theo dữ liệu.\n\n**80 giờ là thời gian đã làm hay cần làm tiếp?** Cả hai đều không. Đó là kịch bản minh họa phát triển một MVP tương đương phạm vi đã chọn từ đầu. Không có timesheet thực tế để kết luận chi phí đã chi hoặc effort còn lại.\n\n## Tài liệu tham khảo\n\n- [PMI – Leveraging the new practice standard for project estimating (Kanabar & Warburton, 2011)](${PMI1}): lịch và kỹ năng nguồn lực, bottom-up, PERT, cơ sở và cập nhật ước tính.\n- [PMI – Estimation is not an event, it's a process! (2005)](${PMI2}): chia chi phí thành các thành phần và tổng hợp bottom-up.\n- [README dự án](D:/DuAnCaNhan/TikTool/README.md), [package.json](D:/DuAnCaNhan/TikTool/package.json), [server.js](D:/DuAnCaNhan/TikTool/server.js), [live_adapter.py](D:/DuAnCaNhan/TikTool/live_adapter.py), [requirements-live.txt](D:/DuAnCaNhan/TikTool/requirements-live.txt), [Clock manifest](D:/DuAnCaNhan/TikTool/Game/Clock/manifest.json), [Tháp PK manifest](D:/DuAnCaNhan/TikTool/Game/TowerPK/manifest.json).\n\nPhạm vi và công nghệ dựa vào tài liệu cùng mã nguồn trong workspace tại ngày 09/10/2026. Mã hiện tại sử dụng adapter Python TikTokLive. README còn nhắc connector Node từ triển khai trước, nên bài theo mã đang chạy. Giờ công, tiền phụ trợ và đơn giá là ví dụ do bài tập đặt ra, không phải số liệu do PMI hay mã nguồn cung cấp.\n`;
await fs.writeFile(path.join(outDir, 'TikTool_LoiThuyetTrinh_5Phut.md'), scriptDoc, 'utf8');
await fs.writeFile(path.join(buildDir, 'content.json'), JSON.stringify({ scripts, hours, budget: 14375000 }, null, 2), 'utf8');
console.log('Completed');

