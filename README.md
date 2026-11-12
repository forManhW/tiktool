# TikTool

> **Dùng hằng ngày:** cài `release\TikTool-Setup-0.3.2.exe` rồi mở bằng biểu tượng TikTool, không cần terminal. Hướng dẫn: [docs/HUONG_DAN_SU_DUNG.md](docs/HUONG_DAN_SU_DUNG.md).

MVP local cho hệ thống TikTok LIVE Interactive Games.

## Chạy TikTool Desktop

```powershell
npm run desktop
```

TikTool tự chạy Hub local, sau đó mở ứng dụng desktop. Chọn game trong app, sang trang Hiệu ứng rồi nhấn **Start Game** để tự mở game chính. Game Gateway lắng nghe tại `ws://127.0.0.1:8765`.

## Đóng gói bộ cài Windows

```powershell
npm run build:adapter
npm run dist
```

Ra file `release\TikTool-Setup-<version>.exe` để gửi cho người khác cài. Máy người dùng không cần Node hay Python. Chi tiết: [docs/DONG_GOI_VA_CAI_DAT.md](docs/DONG_GOI_VA_CAI_DAT.md).

## Chạy Clock demo

Mở terminal thứ hai:

```powershell
npm run clock
```

## Chạy Tháp PK demo

Mở terminal thứ hai:

```powershell
npm run tower
```

Chọn **Tháp PK: Xây vs Phá** trong dashboard, tạo Live Session rồi mở Game URL. Map `build_block` cho phe Xây, `sabotage` cho phe Phá, và `shield` cho quà hiếm của phe Xây. Phe Xây thắng khi tháp đạt 12 khối; phe Phá giữ tháp ở mặt đất.

Sau đó mở URL Clock mà static server hiển thị. Khi Clock hiện `TOOL CONNECTED`, chọn Clock Overlay trong dashboard rồi dùng Test Lab để gửi `add_time` hoặc `subtract_time`.

## Phạm vi hiện có

- Tự đọc `Game/*/manifest.json`.
- Game WebSocket handshake và command ACK.
- Game Library, Event Mapping, Test Lab, Event Injector và System Log realtime (SSE).
- Mapping có CRUD theo `id`; thao tác với một game không còn ghi đè mapping của game khác.
- Rule engine (`core/live-events.js`) xử lý `gift`, `like` (cộng dồn theo mốc tim), `comment` (từ khóa, không phân biệt dấu), `follow` (1 lần/người/phiên), `share`. Có cooldown chung và theo từng người, nhân tham số theo combo. Chi tiết: [docs/GAME_EVENT_API.md](docs/GAME_EVENT_API.md).
- SDK cho game: `/sdk/tiktool-game.js`.
- Dashboard có mục **Giả lập sự kiện LIVE** để test không cần lên live.
- Một Live Session duy nhất chạy một game authoritative; OBS overlay chỉ mirror `game_state`.
- Mapping lưu local trong `data/mappings.json`.

## Fake LIVE event

Dashboard có Event Injector để demo toàn pipeline. Hoặc gọi API trực tiếp:

```powershell
Invoke-RestMethod http://127.0.0.1:3090/api/events -Method Post -ContentType 'application/json' -Body '{"type":"gift","giftId":"5655","giftName":"Rose","user":{"username":"demo"}}'
```

TikTok Listener A (`tiktok-live-connector`) đã được nối qua nút **Kết nối LIVE**. Đây là connector unofficial; TikTool chỉ dùng local trên `127.0.0.1`, không được expose ra LAN hoặc Internet.

## P1: Live Session và authoritative game

Tạo một Live Session từ dashboard, mở **Game URL** đúng một lần, rồi dùng **OBS URL** làm Browser Source. Game URL là nguồn state duy nhất; OBS URL chỉ là mirror read-only. Cả hai URL có session token ngắn hạn riêng.

Gateway chỉ chấp nhận một client `role: "game"` cho mỗi session; các client `role: "overlay"` chỉ nhận `game_state`. Command được xếp hàng một-at-a-time, timeout ACK sau 5 giây và có audit log latency.

Gift combo được tính theo phần tăng thêm (`x1`, `x2`, `x3` đều tạo effect job) thay vì đợi combo kết thúc. Với quà và comment, rule engine chỉ chọn mapping active có priority cao nhất; với tim, follow, share thì mọi mapping khớp đều chạy.

## Tài liệu

- [docs/GAME_EVENT_API.md](docs/GAME_EVENT_API.md): cách game nhận sự kiện quà, tim, comment, follow, share.
- [docs/KE_HOACH_NEN_TANG.md](docs/KE_HOACH_NEN_TANG.md): kế hoạch đóng gói, phát hành và bảo vệ game (V1 → V3).
- [docs/NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md](docs/NGHIEP_VU_TAI_KHOAN_VA_MUA_BAN.md): tài khoản, VIP, thanh toán, license (V2).

## Test

```powershell
npm test
```
