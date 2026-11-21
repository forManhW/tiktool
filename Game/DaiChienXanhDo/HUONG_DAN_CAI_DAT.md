# Hướng dẫn cài đặt và làm việc: Đại Chiến Xanh Đỏ

> Viết cho cả người và AI (Claude Code, Antigravity, Codex…). AI mở repo trên máy mới: **đọc hết file này trước**, rồi đọc [KE_HOACH.md](KE_HOACH.md) để biết luật chơi và phân việc.

---

## 1. Dự án là gì, nằm ở đâu

- Game 3D Unity cho TikTok LIVE: hai phe Xanh/Đỏ đánh nhau liên tục, người xem tặng quà để triệu hồi lính, siêu nhân, xe tăng, robot, sét, thiên thạch. Lâu đài phe nào vỡ thì phe kia +1 điểm.
- Nằm **trong repo TikTool** (`https://github.com/forManhW/tiktool`), không có repo riêng. Bản 1 chạy cùng TikTool: TikTool đọc TikTok LIVE, đổi quà thành lệnh, gửi cho game qua WebSocket `ws://127.0.0.1:8765`.

```
tiktool/
├─ server.js, desktop.js, core/, public/, test/      TikTool (Node.js + Electron)
├─ AGENTS.md, CLAUDE.md                              ghi chú cho AI
└─ Game/DaiChienXanhDo/
   ├─ manifest.json            danh sách hàm game cho TikTool (sinh bằng tools/gen-manifest.js)
   ├─ KE_HOACH.md              luật chơi, kiến trúc, phân việc
   ├─ HUONG_DAN_CAI_DAT.md     file này
   ├─ CREDITS.md               nguồn + giấy phép tài nguyên (chỉ CC0 / OFL)
   ├─ tools/                   sinh manifest, kịch bản test tự động
   ├─ Build/                   file .exe (KHÔNG có trên GitHub, tự build)
   └─ UnityProject/            project Unity 6.3
      └─ Assets/_Game/Scripts/{Core, Fighters, Effects, Camera, Net, Debug, Editor, UI}
```

---

## 2. Cài đặt máy mới (Windows)

| Phần mềm | Phiên bản | Ghi chú |
| --- | --- | --- |
| Git | mới nhất | |
| GitHub CLI (`gh`) | mới nhất | `gh auth login` để push được |
| **Unity Hub** + **Unity 6000.3.29f1** | **đúng bản này** | Khi cài chọn thêm **Windows Build Support (Mono)**. IL2CPP không bắt buộc (build đang dùng Mono) |
| Node.js | **22 LTS** | **Không dùng Node 26**: xem lỗi B ở mục 3 |
| Python | 3.12 hoặc 3.13 | Cho adapter TikTok LIVE |
| VS Code (tùy chọn) | | Extension: Unity, C# Dev Kit |

Các bước:

1. Clone:
   ```bash
   git clone https://github.com/forManhW/tiktool.git
   ```
2. Unity Hub → **Sign in** (license Personal tự cấp khi đăng nhập; xem ở bánh răng → Licenses).
3. Unity Hub → **Installs → Install Editor** → 6000.3.29f1 + Windows Build Support.
4. Unity Hub → **Projects → Add → Add project from disk** → chọn `tiktool/Game/DaiChienXanhDo/UnityProject`.
   Lần mở đầu Unity nhập lại toàn bộ asset (vài phút đến hơn 10 phút). Thư mục `Library/` sẽ chiếm 3–8 GB, nên để repo ở ổ còn trống.
5. TikTool (trong thư mục `tiktool`):
   ```bash
   npm install
   ```
   ```bash
   pip install -r requirements-live.txt
   ```
   ```bash
   npm test
   ```
   Kết quả đúng: 25 pass, 1 skip, 0 fail.

---

## 3. Lỗi đã gặp và cách xử lý

**A. Unity Hub báo "Failed to install … The operation timed out" khi cài Editor.**
Hub kiểm tra checksum file 3,9 GB quá 120 giây (ổ chậm / antivirus). File cài vẫn nằm ở `%APPDATA%\UnityHub\downloads\`. Chạy thẳng (PowerShell, quyền admin):
```bash
"%APPDATA%\UnityHub\downloads\UnitySetup64-6000.3.29f1.exe" /S /D=C:\Program Files\Unity\Hub\Editor\6000.3.29f1
```
Module Windows Build Support cài tương tự với file `UnitySetup-Windows-*-Support-for-Editor-6000.3.29f1.exe` (cùng `/D=`). Hub tự nhận bản Editor sau đó.

**B. `npm run desktop` lỗi vì thiếu Electron (`node_modules/electron/dist` gần như trống).**
Node 26 làm hỏng bước giải nén Electron. Cách tốt nhất: dùng Node 22 LTS rồi `npm install` lại. Nếu buộc dùng Node 26: giải nén tay file `electron-v*-win32-x64.zip` trong `%LOCALAPPDATA%\electron\Cache\` vào `node_modules/electron/dist/`, rồi tạo file `node_modules/electron/path.txt` với nội dung `electron.exe` (không xuống dòng).
npm 11 còn chặn script cài đặt; nếu thấy cảnh báo `install-scripts`:
```bash
npm install-scripts approve electron electron-winstaller
```

**C. Unity chạy dòng lệnh báo lỗi 198 / "No valid Unity Editor license".**
Phiên đăng nhập Unity hết hạn. Mở Unity Hub, Sign out rồi Sign in lại. **AI không tự chạy lệnh `unity license` / `unity auth`**: báo người dùng đăng nhập.

**D. Unity dòng lệnh báo project đang bị mở.**
Một project chỉ mở được bằng một Unity Editor một lúc. Tắt Unity Editor trước khi build bằng dòng lệnh.

---

## 4. Chơi thử trong Unity

Mở scene `Assets/_Game/Scenes/Main.unity`, bấm **Play**.

| Phím | Việc |
| --- | --- |
| F5 / F6 | Một người mới vào phe Xanh / Đỏ |
| F7 / F8 | Quà phe Xanh / Đỏ, lần lượt: lính → siêu nhân → sét → xe tăng → thiên thạch → robot (Shift = robot ngay) |
| F9 | 30 người vào ngẫu nhiên |
| F1 | Đổi chế độ Live tự động ↔ Thường |
| F2 | Ẩn/hiện giao diện |
| F10 | Menu cài đặt |
| M / N | Bật/tắt nhạc nền / âm thanh hiệu ứng |
| Ctrl+R / Ctrl+Shift+R | Reset ván / reset tỉ số hôm nay |
| Chế độ Thường | WASD di chuyển, chuột phải xoay, lăn chuột zoom, click theo một người, Tab, Space, Ctrl+1..4 / 1..4 |

---

## 5. Build file .exe

`Build/` không có trên GitHub, phải tự build:
- Trong Unity: menu **Dai Chien Xanh Do → Build Windows (.exe)**.
- Hoặc dòng lệnh (Unity Editor phải đang tắt):
  ```bash
  "C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe" -batchmode -quit -projectPath Game\DaiChienXanhDo\UnityProject -executeMethod DaiChienXanhDo.EditorTools.BuildScript.BuildWindows -logFile build.log
  ```
  Thành công thì `build.log` có dòng `[Build] Succeeded … 0 lỗi`. Ra file `Game/DaiChienXanhDo/Build/DaiChienXanhDo.exe` (~127 MB).

Các lệnh Editor khác (cùng cú pháp `-executeMethod`):

| Lệnh | Việc | Cẩn thận |
| --- | --- | --- |
| `DaiChienXanhDo.EditorTools.ProjectSetup.Run` | Dựng lại URP, vật liệu, gắn model/âm thanh vào `GameAssets`, **tạo lại scene Main** (sân, lâu đài, tường, trang trí) | Ghi đè `Main.unity`: mọi chỉnh tay trong scene sẽ mất |
| `DaiChienXanhDo.UI.EditorTools.UISetupBuilder.Build` | Dựng lại prefab UI (`Resources/UI/UIRoot.prefab`) và font tiếng Việt | Ghi đè prefab UI |
| `DaiChienXanhDo.EditorTools.ModelInspector.Dump` | Liệt kê model, hoạt ảnh, vật liệu vào log | Chỉ đọc |

---

## 6. Chạy cùng TikTool

```bash
npm run desktop
```
Trong TikTool: chọn game **Đại Chiến Xanh Đỏ** → trang Hiệu ứng → cài quà ↔ hàm → **Start Game** (TikTool tự mở file .exe với `--session/--token`).

- Hàm của game: `blue_*` / `red_*` (`join`, `spawn`, `like_power`, `superhero`, `lightning`, `tank`, `meteor`, `giant_robot`), `ally_like_power`, `ally_spawn`, `event_x2`, `event_disaster`, `admin_reset_round`, `admin_reset_score`. Chi tiết ở KE_HOACH.md mục 2.4.
- Đổi danh sách hàm thì sửa `tools/gen-manifest.js` rồi chạy:
  ```bash
  node Game/DaiChienXanhDo/tools/gen-manifest.js Game/DaiChienXanhDo/manifest.json
  ```
  sau đó `npm test` (có test kiểm tra manifest).
- Lệnh không kèm người gửi (vd. bấm thử trong Test Lab) bị game từ chối: luật "không có nhân vật vô danh".

---

## 7. Test tự động (không cần lên live)

Game có móc test, chỉ bật khi chạy với `--test-dir <thư mục>`. Khi bật, gửi lệnh qua TikTool (`POST /api/test`):

| Lệnh | Việc |
| --- | --- |
| `debug_screenshot` | Chụp khung hình vào `<thư mục>/shot_NN.png` + ghi trạng thái vào Player.log |
| `debug_state` | Chỉ ghi trạng thái (tỉ số, máu lõi, số lính, trạng thái hoạt ảnh, FPS…) |
| `debug_toggle_mode` | Đổi chế độ Live ↔ Thường |
| `debug_quake`, `debug_tornado`, `debug_volcano` | Gọi ngay đúng loại thảm họa |

Kịch bản có sẵn (build .exe trước, chạy từ thư mục gốc repo, TikTool hub không được đang chạy vì cùng cổng 3090/8765):
```bash
node Game/DaiChienXanhDo/tools/test-scenario.js . test-out
```
```bash
node Game/DaiChienXanhDo/tools/test-showcase.js . show-out
```
- `test-scenario.js`: đủ luật chơi: đánh nhỏ → quà → sân đông → thảm họa → thắng ván → lệnh trong lúc ăn mừng → ván mới → chế độ Thường → reset tỉ số. In log `[Match]`/`[Test]` và lưu ảnh.
- `test-showcase.js`: chụp riêng siêu nhân, thiên thạch, xe tăng + robot phe Xanh, lốc xoáy.
- Dữ liệu TikTool của test nằm trong `<thư mục kết quả>/data`, không đụng `data/` thật. Xem kết quả từng lệnh:
  ```bash
  node Game/DaiChienXanhDo/tools/read-hub-log.js test-out/data/tiktool.db
  ```
- Test ghi tỉ số vào file ngày của máy: xóa `%USERPROFILE%\AppData\LocalLow\TikTool\DaiChienXanhDo\data\<YYYY-MM-DD>.json` sau khi test.
- Log game: `%USERPROFILE%\AppData\LocalLow\TikTool\DaiChienXanhDo\Player.log`.

---

## 8. Dữ liệu không có trên GitHub (mỗi máy tự có)

| Dữ liệu | Vị trí |
| --- | --- |
| Cài đặt game (`settings.json`: chế độ, âm thanh, ngưỡng thảm họa, câu hướng dẫn…) | `%USERPROFILE%\AppData\LocalLow\TikTool\DaiChienXanhDo\` |
| Tỉ số + top người tặng theo ngày | cùng chỗ, thư mục `data\` |
| Mapping quà của TikTool, phiên live | `tiktool/data/` (chạy dev) hoặc `%APPDATA%\TikTool` (bản cài) |
| File .exe | `Game/DaiChienXanhDo/Build/` (tự build) |

---

## 9. Quy ước làm việc (bắt buộc với AI)

- **Commit đứng tên `forManhW`**, **không** thêm `Co-Authored-By`, không ghi tên AI/công cụ trong commit, PR, comment.
- Trả lời chủ dự án bằng **tiếng Việt**, ngắn gọn. Kết quả test báo bằng chữ, **không gửi ảnh** trừ khi được hỏi.
- AI phụ (Antigravity…) làm trên **clone riêng + nhánh riêng**, push nhánh, **không push thẳng `main`**. Chủ dự án duyệt rồi mới merge vào `main`.
- Một project Unity chỉ một Editor mở một lúc.
- Chỉ dùng tài nguyên miễn phí giấy phép rõ ràng (CC0, OFL), ghi vào `CREDITS.md`. Không dùng tên/hình nhân vật có thương hiệu (Superman…): gọi là "Siêu Chiến Binh".
- Sửa xong phải build + chạy `test-scenario.js` và `npm test` trước khi báo xong.

**Quyết định thiết kế đã chốt (đừng tự làm ngược lại):**
- Trận vô hạn, lâu đài Xanh góc dưới trái, Đỏ góc trên phải; lõi không hồi máu; màn hình vuông.
- **Không có nhân vật vô danh**: mọi lính/siêu nhân/xe tăng/robot mang tên người triệu hồi.
- Đã bỏ: đấm thường, đấm liên hoàn, biến hình, X2 Phe Yếu, giọng đọc AI, quà hồi máu lõi.
- Rung màn hình chỉ cho sự kiện lớn (robot đáp, thiên thạch, sét đầu, lâu đài sụp, thảm họa).
- Chủ dự án từng yêu cầu xóa: vạch giữa sân, vòng màu dưới chân, vết nứt đất. Bản hiện tại (nhánh visual-polish đã được duyệt vào main) lại có vạch chéo màu, vòng màu dưới xe tăng/quanh lâu đài, vết nứt khi động đất. **Hỏi chủ dự án trước khi thêm/bớt các thứ này.**
