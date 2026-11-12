# Kế hoạch tổ chức lại TikTool thành nền tảng phát hành game

> Trạng thái: bản kế hoạch, 10/11/2026. Nghiệp vụ tài khoản và mua bán chi tiết nằm ở [NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md](NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md).

## 1. Mục tiêu

TikTool là launcher kiểu Steam dành cho streamer TikTok LIVE:

- **Một người bán duy nhất** là chủ TikTool. Không có chợ cho bên thứ ba và không nhận game từ người khác.
- **Cửa hàng**: hiển thị mọi game, ai cũng xem được.
- **Thư viện**: game đã cài, cập nhật, gỡ.
- **Bản dùng thử** chơi được ngay. **Bản đầy đủ** chỉ tải được khi đăng nhập tài khoản VIP (V2).
- **Người dùng không sửa được code game.** Game được đóng gói, ký số và kiểm tra trước khi chạy.
- **Hub sự kiện LIVE**: quà, tim, comment, follow, share được map vào effect của game (đã làm, xem [GAME_EVENT_API.md](GAME_EVENT_API.md)).

## 2. Lộ trình phiên bản

| Phiên bản | Phạm vi | Không làm |
| --- | --- | --- |
| **V1: Đóng gói và triển khai** | Bộ cài Windows (.exe), auto-update, game đóng gói kèm app, game được ký và kiểm tra, dữ liệu người dùng tách khỏi thư mục cài, adapter TikTok không cần cài Python | Đăng nhập, thanh toán, tải game rời |
| **V1.5: Cửa hàng và tải game** | Catalog game lấy từ server, tải và cập nhật từng game riêng, trang Cửa hàng và Thư viện, giữ bản dùng thử cho mọi game | Thanh toán |
| **V2: Tài khoản VIP** | Đăng ký và đăng nhập, gói VIP, thanh toán VietQR, MoMo hoặc VNPay, license ký số, giới hạn thiết bị, tải bản full theo quyền | Bán lẻ từng game (để V2.5 nếu cần) |
| **V3: Vận hành** | Trang admin, thống kê, mã khuyến mãi, giới thiệu bạn bè, hỗ trợ macOS | |

Làm hết V1 trước khi bắt đầu V2. Đóng gói xong mới biết code game cần tách thế nào, và V2 cần chính cấu trúc đó.

## 3. Cấu trúc repo đề xuất

Hiện tại `server.js`, `desktop.js` và các game nằm lẫn ở gốc repo. Mỗi game có cách build riêng, và TowerGameTrial còn là một repo git lồng bên trong. Cấu trúc đích:

```
TikTool/
├─ apps/
│  ├─ launcher/          # Electron main + preload (desktop.js, preload.js hiện tại)
│  └─ hub/               # server.js tách thành routes/, sessions/, gateway/, store/
├─ core/                 # Logic thuần, có unit test (live-events.js đã có)
├─ sdk/                  # tiktool-game.js cho game (đã có)
├─ adapters/
│  └─ tiktok-live/       # live_adapter.py + script build ra .exe
├─ games/                # Mã nguồn game, mỗi game một thư mục, KHÔNG phát hành dạng thô
│  ├─ tower-game/
│  ├─ clock/
│  └─ ...
├─ tools/
│  ├─ pack-game.mjs      # build → obfuscate → zip → ký → .ttgame
│  └─ keys/              # chỉ chứa public key; private key KHÔNG để trong repo
├─ server/               # (V1.5+) API cửa hàng, tài khoản, license
├─ docs/
└─ test/
```

Việc cần làm:

1. Đổi `Game/TowerGameTrial` từ repo lồng thành thư mục thường (hoặc submodule có `.gitmodules`). Hiện code game đang **không được lưu** vào repo TikTool.
2. Đưa `data/` (mappings, sessions, db, assets) vào `.gitignore` và chuyển sang thư mục dữ liệu người dùng (mục 5.3).
3. Xóa `.assignment3-build/` và `deliverables/` khỏi repo sản phẩm (chuyển sang repo khác hoặc lưu trữ riêng).
4. Thay `pythonCommand()` trỏ tới `../tiktoklive-demo/.venv` bằng adapter đã đóng gói.

## 4. Định dạng gói game `.ttgame`

Một file zip gồm:

```
manifest.json        # id, version, edition (trial|full), minHubVersion, entry, actions, triggers
files/...            # bản build: JS đã bundle và obfuscate, ảnh, âm thanh
SIGNATURE            # Ed25519 ký trên SHA-256 của manifest.json + danh sách hash từng file
```

- **Ký**: `tools/pack-game.mjs` ký bằng private key đặt trên máy build (sau này là CI), không bao giờ nằm trong repo hoặc trong app.
- **Kiểm tra**: launcher chứa public key. Trước mỗi lần chạy, launcher kiểm tra chữ ký và hash từng file. Sai một byte là không chạy và báo "Game bị thay đổi, hãy cài lại".
- **Phục vụ file**: hub đọc file trực tiếp từ gói đã kiểm tra (hoặc thư mục cài đặt chỉ đọc), không phục vụ từ thư mục `Game/` mà người dùng sửa được như hiện nay.
- **Hai bản build cho mỗi game**: `trial` và `full` là **hai gói khác nhau**. Tính năng full được bỏ hẳn khỏi bản trial lúc build, không chỉ ẩn bằng `if`. Đây là lớp bảo vệ thật: không có code thì không mở khóa được.

## 5. V1: Đóng gói và triển khai (làm trước)

### 5.1 Công cụ

- **electron-builder** với target NSIS (bộ cài Windows), `asar: true`.
- **Electron Fuses**: bật `EnableEmbeddedAsarIntegrityValidation` và `OnlyLoadAppFromAsar`; tắt `RunAsNode`, `EnableNodeOptionsEnvironmentVariable` và `EnableNodeCliInspectArguments`. Như vậy người dùng không sửa được `app.asar` và không gắn debugger vào app.
- **electron-updater**: auto-update từ server riêng hoặc Cloudflare R2/S3 (dùng generic provider, không cần repo public).
- **Ký code Windows**: chứng chỉ OV hoặc EV code signing. Không có chứng chỉ thì SmartScreen sẽ cảnh báo "Unknown publisher". Đây là chi phí cần dự trù.

### 5.2 Adapter TikTok

Đề xuất build `live_adapter.py` bằng **PyInstaller** thành `tiktok-adapter.exe` và đặt vào `extraResources`. Hub gọi file exe này thay cho `python`.

Phương án thay thế: chuyển sang `tiktok-live-connector` (Node), vốn đã có trong `package.json`. Cách này bỏ hẳn Python và gọn hơn, nhưng phải kiểm tra lại combo quà, tim và comment. Nên thử ở V1.5.

> **Rủi ro lớn nhất của sản phẩm**: cả hai thư viện đều **không chính thức** (đọc giao thức nội bộ của TikTok). TikTok có thể đổi giao thức và làm hỏng kết nối bất cứ lúc nào. Cần (a) cơ chế cập nhật riêng adapter thật nhanh, (b) điều khoản ghi rõ TikTool không liên kết với TikTok, (c) theo dõi lỗi kết nối từ xa ở V2.

### 5.3 Dữ liệu người dùng

- `server.js` hiện ghi vào `__dirname/data`. Sau khi đóng gói, thư mục này nằm trong asar và **chỉ đọc**, nên sẽ hỏng.
- Đổi sang `process.env.TIKTOOL_DATA_DIR`. Launcher truyền vào `app.getPath("userData")` (`%APPDATA%/TikTool`).
- Game cài ở `%LOCALAPPDATA%/TikTool/games/<id>/<version>/`. Gỡ app vẫn có thể giữ cấu hình mapping.

### 5.4 Bảo mật cần sửa trước khi phát hành V1

| Vấn đề hiện tại | Rủi ro | Cách sửa |
| --- | --- | --- |
| API `127.0.0.1:3090` không có xác thực | Bất kỳ trang web nào đang mở trên máy streamer đều có thể gọi `fetch` để tạo hoặc xóa mapping và bắn lệnh vào game (CSRF, DNS rebinding) | Kiểm tra header `Host` (`127.0.0.1:3090`), chặn `Origin` lạ, thêm token ngẫu nhiên mỗi lần chạy. Launcher truyền token cho dashboard qua preload, dashboard gửi kèm header `X-TikTool-Token` |
| Cửa sổ game Electron không có `sandbox` hay chặn điều hướng | Game (hoặc link trong game) có thể mở trang lạ | `sandbox: true`, `nodeIntegration: false`, `setWindowOpenHandler` → deny, `will-navigate` chỉ cho phép `127.0.0.1:3090` |
| Dashboard mở được bằng trình duyệt thường | Không nguy hiểm, nhưng lộ cách gọi API | Sau khi có token thì chỉ launcher dùng được |
| Phục vụ file game từ thư mục sửa được | Người dùng sửa code game | Dùng gói `.ttgame` đã ký (mục 4) |
| `/api/assets` nhận ảnh base64 tới ~100KB | Ổn | Giữ giới hạn kích thước, chỉ nhận PNG, JPG, WebP (đã có) |
| Token session nằm trong URL game | Lộ nếu streamer chia sẻ URL overlay | Token overlay đã tách riêng. Thêm nút "Tạo lại link OBS" |

### 5.5 Bảo vệ code game: làm được gì và không làm được gì

Game viết bằng JavaScript chạy trên máy người dùng nên **không thể giấu hoàn toàn**. Mục tiêu là làm cho việc sửa hoặc lấy code đủ khó để không đáng công, và làm cho thứ có giá trị (bản full, cập nhật) phụ thuộc vào server:

1. **Bundle và obfuscate** (esbuild + javascript-obfuscator) cho mọi file JS của game khi đóng gói.
2. **Ký và kiểm tra gói** (mục 4). Sửa file là game không chạy.
3. **asar integrity + fuses** cho chính launcher (mục 5.1).
4. **Bản full chỉ tải về khi có license hợp lệ** (V2). Đây là lớp chính.
5. **Watermark license**: bản full nhúng mã người mua (ẩn) để truy ra nguồn nếu bị chia sẻ lậu.
6. Không cố làm DRM nặng (mã hóa file, chống debug). Tốn công, dễ gây lỗi cho người dùng thật, và vẫn bị vượt qua.

### 5.6 Checklist phát hành V1

- [ ] Tách `TIKTOOL_DATA_DIR`, chạy được khi cài vào `Program Files`
- [ ] Adapter `.exe`, không cần Python trên máy
- [ ] Token API + kiểm tra Host/Origin
- [ ] Sandbox cửa sổ game, chặn điều hướng
- [ ] `tools/pack-game.mjs` + kiểm tra chữ ký trong hub
- [ ] electron-builder NSIS + fuses + asar
- [ ] Chứng chỉ ký code + ký bộ cài
- [ ] Auto-update (kênh `stable` và `beta`)
- [ ] Trang điều khoản sử dụng, chính sách riêng tư (bản V1 rút gọn)
- [ ] Báo lỗi: ghi log ra file trong userData, nút "Xuất log gửi hỗ trợ"
- [ ] Test trên máy Windows sạch (không Node, không Python)

## 6. V1.5: Cửa hàng

- Server catalog (tĩnh cũng được: `catalog.json` + file `.ttgame` trên R2) gồm danh sách game, ảnh, mô tả, phiên bản, kích thước, link bản trial.
- Launcher có các tab: **Cửa hàng** (xem tất cả), **Thư viện** (đã cài), **LIVE** (dashboard hiện tại).
- Cài game: tải `.ttgame` → kiểm tra chữ ký → giải nén vào thư mục cài → đăng ký vào thư viện.
- Cập nhật: so `version` trong catalog, tải phần mới, giữ bản cũ đến khi bản mới kiểm tra xong.
- `catalog.json` cũng phải được ký để không bị chèn game giả.

## 7. V2: Tài khoản và VIP (tóm tắt)

Chi tiết ở [NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md](NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md). Tóm tắt kiến trúc:

```
Launcher ──HTTPS──► API server (Node/Fastify hoặc NestJS) ──► PostgreSQL
   │                     │
   │                     ├─ Cổng thanh toán (VietQR qua SePay/Casso, MoMo, VNPay): webhook
   │                     └─ Object storage (R2/S3): file .ttgame bản full, link tải có hạn 5 phút
   └─ lưu license token (Ed25519, hạn 7 ngày) trong Windows Credential Manager (keytar/safeStorage)
```

Launcher kiểm tra license **offline** bằng public key, nên mất mạng vẫn chơi được tối đa 7 ngày. Online thì tự gia hạn token.

## 8. Rủi ro và quyết định còn mở

| # | Vấn đề | Đề xuất |
| --- | --- | --- |
| 1 | Connector TikTok không chính thức | Ghi rõ trong điều khoản, cập nhật adapter tách khỏi app, theo dõi khi TikTok có API chính thức |
| 2 | Chính sách TikTok về game ăn theo quà | Không thiết kế cơ chế giống cờ bạc (quay thưởng ăn tiền). Game chỉ là hiệu ứng tương tác |
| 3 | VIP thuê bao hay bán đứt từng game | Đề xuất chỉ làm VIP thuê bao ở V2 (một gói mở mọi game), bán lẻ để sau. Xem mục 3 của file nghiệp vụ |
| 4 | Pháp lý kinh doanh | Cần đăng ký hộ kinh doanh hoặc công ty để nhận thanh toán qua cổng và xuất hóa đơn. Hỏi kế toán về thuế dịch vụ phần mềm |
| 5 | Mã nguồn bên thứ ba | TowerGameTrial dựa trên `iamkun/tower_game` (license **MIT**, © 2018 BMQB, Inc). Được bán, nhưng **phải giữ thông báo bản quyền MIT** trong gói game (ví dụ màn "Giới thiệu" hoặc file `LICENSES.txt`). Cần kiểm tra thêm ảnh và âm thanh gốc của repo đó. Kiểm tra cả ảnh và âm thanh lấy từ nơi khác (ví dụ asset meme trong `Game/Clock/assets`) |
| 6 | Chi phí cố định | Chứng chỉ ký code (theo năm), server + DB, R2, phí cổng thanh toán (%) |
