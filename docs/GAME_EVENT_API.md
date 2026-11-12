# TikTool Game Event API

Tài liệu cho người làm game chạy trên TikTool. Mọi game mới nên làm theo tài liệu này.

## 1. Luồng sự kiện

```
TikTok LIVE ──► live_adapter.py ──► server.js ──► core/live-events.js ──► Game Gateway (WS 8765) ──► Game
                (gift/like/       (chống trùng,     (mapping, cộng dồn tim,     (hàng đợi, ACK 5s)        (onAction)
                 comment/follow/   session)          cooldown, nhân tham số)
                 share)
```

- **Game không tự đọc TikTok.** Game chỉ nhận `action` mà streamer đã map trong dashboard.
- Streamer quyết định sự kiện nào gây ra hiệu ứng gì. Game chỉ khai báo mình có những effect (action) nào.

## 2. Các loại sự kiện (trigger)

| Trigger | Điều kiện kích hoạt | Số lần kích hoạt (`times`) | Ghi chú |
| --- | --- | --- | --- |
| `gift` | `giftId` (để trống = mọi quà) | số quà trong combo | Chỉ mapping có priority cao nhất được chạy |
| `like` | mỗi `likeEvery` tim, `likeScope` = `room` (cả phòng) hoặc `user` (từng người) | số mốc vượt qua | Tim dư được giữ cho lần sau. Mọi mốc đều chạy (100 tim và 1000 tim) |
| `comment` | `keywords` với `matchMode` = `contains`, `exact`, `startsWith` hoặc `any` | 1 | Không phân biệt hoa thường và dấu tiếng Việt. Chỉ mapping priority cao nhất được chạy |
| `follow` | người xem follow | 1 | Mỗi người chỉ tính 1 lần trong một phiên |
| `share` | người xem share | 1 | Nên đặt `userCooldownMs` |

Tùy chọn chung cho mọi mapping:

- `cooldownMs`: khoảng chờ chung giữa hai lần mapping này chạy.
- `userCooldownMs`: khoảng chờ cho từng người xem. Dùng để chống spam comment hoặc share.
- `repeat`:
  - `multiply` (mặc định): tham số số được nhân với `times`. Ví dụ combo Rose x5 với `amount 2` sẽ gửi `amount 10`.
  - `once`: chỉ gửi đúng tham số đã cấu hình.
- `maxPerEvent`: chặn trên của `times`. Mặc định 100.
- `priority`, `enabled`.

## 3. manifest.json

```json
{
  "id": "tower-game-trial",
  "name": "Tower Game: Xây tháp",
  "version": "0.3.0",
  "launch": { "type": "web", "url": "http://127.0.0.1:3090/game/TowerGameTrial/" },
  "connection": { "transport": "websocket", "port": 8765 },
  "triggers": ["gift", "like", "comment", "follow", "share"],
  "liveEvents": ["follow"],
  "actions": [
    {
      "id": "build_floors",
      "name": "Xây thêm tầng",
      "parameters": {
        "amount": { "type": "number", "required": true, "minimum": 1, "maximum": 500, "step": 1, "label": "Số tầng" }
      }
    },
    {
      "id": "restore_hearts",
      "name": "Hồi tim",
      "parameters": { "amount": { "type": "number", "minimum": 1, "scale": false } }
    }
  ]
}
```

| Trường | Ý nghĩa |
| --- | --- |
| `triggers` | Loại sự kiện game cho phép map. Bỏ trống thì cho phép cả 5 loại |
| `liveEvents` | Sự kiện thô mà game muốn nhận thêm qua `onLiveEvent`, ví dụ để hiện tên người follow. Mặc định là `[]` |
| `defaultMappings` | (Tùy chọn) Bộ mapping khởi đầu, cùng định dạng mapping trong dashboard (không cần `gameId`, `id`). Hub tự thêm khi khởi động **nếu game chưa có mapping nào** |
| `window` | (Tùy chọn) `{ width, height }` kích thước thiết kế. App desktop mở cửa sổ game theo tỉ lệ này, thu nhỏ cho vừa màn hình |
| `parameters.*.scale` | Đặt `false` nếu tham số không được nhân theo combo, ví dụ hồi tim hoặc đặt giờ |
| `parameters.*.maximum` | Giới hạn trên. Khi nhân theo combo, giá trị cũng bị chặn ở mức này |

## 4. SDK phía game

```html
<script src="/sdk/tiktool-game.js"></script>
<script>
  const live = TikTool.connect({
    gameId: "my-game",
    getState: () => ({ score, hp }),
    onAction(action, params, source) {
      // source.eventType: gift | like | comment | follow | share | (test)
      // source.user.nickname: tên người xem tạo ra sự kiện
      if (action === "build") { build(params.amount, source.user?.nickname); live.emit(); return true; }
      throw new Error("Lệnh không hỗ trợ");   // TikTool ghi log là failed
    },
    onLiveEvent(event) { /* { type, user:{id,nickname,avatarUrl}, comment?, like?, gift?, share? } */ },
    onState(state) { /* chỉ chạy ở chế độ overlay OBS */ },
    onStatus(status) { /* demo | connecting | connected | reconnecting | unauthorized | duplicate_game */ }
  });
</script>
```

Quy tắc bắt buộc:

1. `onAction` phải trả `true` khi chạy thành công. Nếu không chạy được, throw lỗi hoặc trả `false`. Không trả gì sẽ bị tính là thất bại.
2. Gọi `live.emit()` sau mỗi lần state thay đổi. SDK tự giới hạn khoảng 5 lần mỗi giây.
3. Game phải chạy được khi mở trực tiếp, không cần session (chế độ `demo`), để test.
4. Không dùng `eval`, không tải script từ Internet và không đọc file ngoài thư mục game. Đây là điều kiện để đóng gói và ký game sau này (xem `KE_HOACH_NEN_TANG.md`).

`Game/TowerGameTrial/gateway.js` là phiên bản cũ của SDK này. Nó đã hỗ trợ `onLiveEvent` và vẫn dùng được.

## 5. Test không cần lên LIVE

Mở dashboard, chọn game, nhấn **Start Game**, rồi mở mục **Giả lập sự kiện LIVE** để bắn quà, tim, comment, follow hoặc share. Cũng có thể gọi API:

```powershell
Invoke-RestMethod http://127.0.0.1:3090/api/events -Method Post -ContentType 'application/json; charset=utf-8' -Body '{"sessionId":"<id>","type":"like","count":150,"user":{"id":"an","nickname":"An"}}'
```

Unit test cho rule engine:

```powershell
node --test test/*.test.js
```


## Lưu điểm người chơi theo tuần (SQLite của hub)

Game nào muốn giữ điểm qua nhiều buổi live thì lưu vào hub thay vì localStorage. Mỗi tuần tính từ 0h thứ Hai (giờ máy).
`scope` là `live` (mặc định) hoặc `demo`. `week` là `YYYY-MM-DD` (thứ Hai), `current` hoặc `previous`; bỏ trống là tuần hiện tại.

| Lệnh | Việc |
|---|---|
| `GET /api/games/<gameId>/players?scope=&week=` | `{ week, members: [[id, nickname, score, joinedAt], ...] }`, xếp điểm giảm dần |
| `POST /api/games/<gameId>/players` `{ scope, week, members, replace }` | Thêm / cập nhật theo id. `replace: true` xóa cả tuần trước khi ghi |
| `GET /api/games/<gameId>/weeks?scope=` | Các tuần đã có dữ liệu, số người, điểm cao nhất |
| `GET /api/games/<gameId>/top.csv?scope=&week=&limit=` | File CSV top tuần (bỏ người 0 điểm) |

Thêm `"weeklyBoard": { "scoreLabel": "Tu vi" }` vào manifest để thẻ game trên TikTool có nút **Top tuần này / Top tuần trước**.
