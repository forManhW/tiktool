# Clock Overlay

Game demo số 1 cho TikTool. Overlay bắt đầu từ 24 giờ, tự đếm ngược và nhận lệnh qua WebSocket.

## Chạy local

Mở thư mục này bằng một static server. Ví dụ nếu đã có Node.js:

```powershell
npx serve .
```

Mở URL mà `serve` hiển thị. Chế độ overlay cho OBS:

```text
http://localhost:3000/?overlay=1
```

Trong OBS, thêm Browser Source bằng URL trên. Giao diện hiển thị nền tối toàn màn hình với đồng hồ ở giữa; đặt kích thước Browser Source theo khung hình phát của bạn.

## WebSocket contract

Overlay tự kết nối đến `ws://127.0.0.1:8765`. Có thể đổi endpoint:

```text
http://localhost:3000/?ws=ws://127.0.0.1:8765
```

Lệnh tăng 1 giờ:

```json
{
  "id": "cmd-001",
  "type": "game_action",
  "action": "add_time",
  "params": { "hours": 1 }
}
```

Các action hỗ trợ: `add_time`, `subtract_time`, `set_time`, `pause_timer`, `resume_timer`.

Sau mỗi lệnh có `id`, game phản hồi:

```json
{
  "type": "command_result",
  "commandId": "cmd-001",
  "status": "success"
}
```
