# TikTool

MVP local cho hệ thống TikTok LIVE Interactive Games.

## Chạy TikTool Desktop

```powershell
npm run desktop
```

TikTool tự chạy Hub local, sau đó mở ứng dụng desktop. Chọn game trong app, sang trang Hiệu ứng rồi nhấn **Start Game** để tự mở game chính. Game Gateway lắng nghe tại `ws://127.0.0.1:8765`.

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
- Rule engine chuẩn hóa `gift`, `comment`, `like`, `follow`, `share`, match mapping và áp cooldown trước khi phát `game_action`.
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

Gift combo được tính theo phần tăng thêm (`x1`, `x2`, `x3` đều tạo effect job) thay vì đợi combo kết thúc. Rule engine chỉ chọn mapping active có priority cao nhất cho mỗi event.
