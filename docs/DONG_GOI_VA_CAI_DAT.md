# Đóng gói và cài đặt TikTool

## Cho người dùng (streamer)

1. Tải file `TikTool-Setup-<phiên bản>.exe`.
2. Mở file. Windows có thể hiện **"Windows protected your PC"** vì bộ cài chưa có chữ ký số: bấm **More info → Run anyway**.
3. Chọn thư mục cài (mặc định trong tài khoản người dùng, không cần quyền Admin) → **Install**.
4. Mở **TikTool** từ Desktop hoặc Start Menu.

Không cần cài Node.js hay Python. Bộ cài đã có sẵn adapter kết nối TikTok LIVE.

Bộ cài đã có sẵn: mọi game cùng ảnh và âm thanh của game, danh mục quà TikTok kèm ảnh quà (lưu sẵn trong máy, không cần tải từ TikTok), bộ mapping hiệu ứng và ảnh hiệu ứng.

Dữ liệu (mapping, live session, ảnh hiệu ứng, log) lưu ở `%APPDATA%\TikTool`. Lần chạy đầu, app chép dữ liệu đóng kèm vào đây. Mapping đóng kèm chỉ được chép khi máy chưa có mapping. Quà và ảnh chỉ thêm phần còn thiếu, nên không ghi đè thứ người dùng đã sửa. Gỡ app vẫn giữ thư mục này, nên cài lại không mất cấu hình. Muốn xóa sạch thì xóa thư mục đó bằng tay.

Khi cần hỗ trợ, gửi file log `%APPDATA%\TikTool\logs\main.log`.

Lưu ý:
- TikTool dùng cổng `3090` (dashboard) và `8765` (game). Nếu chương trình khác đang chiếm cổng, app báo lỗi và thoát.
- Chỉ mở được một cửa sổ TikTool. Mở lần hai sẽ đưa cửa sổ cũ lên trước.
- OBS dùng **OBS URL** trong dashboard như trước (`http://127.0.0.1:3090/...`).

## Cho người build bộ cài

Cần: Windows, Node.js (đã `npm install`), một Python có `TikTokLive` và `PyInstaller`.

```powershell
npm run build:adapter   # live_adapter.py -> vendor\tiktok-adapter\tiktok-adapter.exe
npm run dist            # build:seed rồi -> release\TikTool-Setup-<version>.exe
```

- `npm run dist` tự chạy `build:seed` (`scripts/build-seed.js`). Bước này lấy dữ liệu **hiện tại** trong `data/` của máy build và ghi ra `vendor\seed`:
  - danh mục quà từ `tiktool.db`, có tải ảnh quà về `assets\gifts\` (cần mạng);
  - `mappings.json`;
  - ảnh trong `data\assets`.

  Không đưa vào: `event_log` (lịch sử log) và `sessions.json` (username, token live).
- Muốn bộ cài có mapping hoặc quà mới thì cập nhật trong app dev rồi chạy lại `npm run dist`.

- `build:adapter` mặc định dùng Python trong `..\tiktoklive-demo\.venv`. Dùng Python khác: `$env:TIKTOOL_PYTHON = "C:\...\python.exe"`.
- Chỉ build lại adapter khi `live_adapter.py` hoặc thư viện TikTokLive thay đổi.
- `npm run pack` build bản chưa đóng thành bộ cài (`release\win-unpacked\TikTool.exe`) để thử nhanh.
- Tăng `version` trong `package.json` trước mỗi lần phát hành. Tên file bộ cài lấy theo version này.
- Game được đóng gói là mọi thư mục trong `Game/` (bỏ `node_modules`, `test`, `tools`, `*.md`). Thêm game mới thì build lại bộ cài.
- Icon app: đặt `build\icon.ico` (256×256). Chưa có thì app dùng icon mặc định của Electron.

### Cách bản cài khác bản dev

| | `npm run desktop` (dev) | Bản cài |
| --- | --- | --- |
| Dữ liệu | `./data` | `%APPDATA%\TikTool` |
| Adapter TikTok | `vendor\tiktok-adapter\*.exe` nếu có, không thì `python live_adapter.py` | `resources\tiktok-adapter\tiktok-adapter.exe` |
| Dữ liệu khởi đầu | không dùng | `resources\seed` (quà + ảnh quà, mapping, ảnh hiệu ứng) |
| Hub | chạy trong main process của Electron (hoặc dùng hub `npm start` đang chạy sẵn) | chạy trong main process |
| Code | sửa trực tiếp | nằm trong `app.asar`, có kiểm tra toàn vẹn (Electron fuses) |

### Chưa làm (xem `docs/KE_HOACH_NEN_TANG.md` mục 5.6)

- Ký số bộ cài (cần mua chứng chỉ code signing). Chưa ký thì SmartScreen cảnh báo.
- Auto-update.
- Token API và kiểm tra Host/Origin cho `127.0.0.1:3090`.
- Đóng gói và ký từng game (`.ttgame`), obfuscate code game.

## Mang tu vi từ máy chạy bản cài về máy dev

Tu vi và điểm tuần của người chơi nằm trong bảng `game_player_week` của `tiktool.db`.

1. Trên máy chạy bản cài, **tắt hẳn TikTool**. Copy file `%APPDATA%\TikTool\tiktool.db` (USB, Drive...). Nếu cạnh nó có `tiktool.db-wal` / `tiktool.db-shm` thì copy kèm.
2. Trên máy dev, tắt hub (`npm start` / `npm run desktop`) và cửa sổ game.
3. Gộp vào DB dev:

   ```powershell
   npm run import:players -- "D:\duong-dan\tiktool.db"
   ```

   Script tự sao lưu `data\tiktool.db` thành `tiktool.db.bak-<thời gian>` trước khi gộp. Mỗi người chơi giữ tu vi cao hơn giữa hai máy; người chỉ có ở máy kia thì được thêm vào. Danh mục quà, log và mapping của máy dev giữ nguyên.
4. Mở lại TikTool dev. Game tải tu vi tuần hiện tại từ hub.

Chiều ngược lại (dev → máy cài): chạy script với DB đích là file trên máy kia (khi TikTool ở đó đang tắt):
`npm run import:players -- data\tiktool.db "\may-kia\...\tiktool.db"`.
