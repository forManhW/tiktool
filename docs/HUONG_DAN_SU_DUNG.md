# TikTool + Nga Mi Tu Tiên · Hướng dẫn cài đặt và sử dụng

Bản 0.3.2. Dùng cá nhân trên máy Windows 10/11 (64-bit). Không cần cài Node, Python hay mở terminal.

---

## 1. Cài đặt

1. Chạy file `TikTool-Setup-0.3.2.exe`.
   - Windows có thể hiện "Windows protected your PC" vì bộ cài không có chữ ký số: bấm **More info → Run anyway**.
2. Chọn thư mục cài (mặc định trong thư mục người dùng) → **Install**.
3. Xong sẽ có biểu tượng **TikTool** ở Desktop và Start Menu. Từ giờ chỉ cần bấm đúp biểu tượng đó.

Bộ cài đã có sẵn mọi thứ:

| Thành phần | Ở đâu trong máy |
|---|---|
| Ứng dụng TikTool + các game (Nga Mi Tu Tiên, tranh nhân vật, nền, rồng, âm thanh) | Thư mục cài đặt |
| Bộ nghe TikTok LIVE | `resources\tiktok-adapter` trong thư mục cài |
| Giọng đọc AI tiếng Việt (Piper, giọng vais1000) | `resources\tts` trong thư mục cài |
| Dữ liệu của anh: mapping, phiên live, cơ sở dữ liệu SQLite (log, danh sách quà, **tu vi theo tuần**) | `%APPDATA%\TikTool` |

Gỡ cài đặt (Settings → Apps) **không xóa** `%APPDATA%\TikTool`, nên cài lại / nâng cấp vẫn giữ nguyên tu vi và mapping.

> Lưu ý: chỉ mở **một** TikTool một lúc. Nếu báo "Cổng 3090/8765 đang bị chương trình khác dùng" thì tắt TikTool đang mở (hoặc bản chạy bằng terminal cũ) rồi mở lại.

---

## 2. Mỗi lần lên live

1. Mở **TikTool** từ Desktop.
2. Bấm nút tên tài khoản góc trên bên phải (`@streamer`) → nhập **TikTok LIVE ID** (tên tài khoản TikTok, không có @) → **Lưu**. Chỉ cần làm 1 lần.
3. Tab **Game** → thẻ **Nga Mi Tu Tiên** → **Chọn game**.
4. Bấm **Start Game**: cửa sổ game dọc mở ra ở màn **Cài đặt**.
5. Bấm **Kết nối LIVE** (nút sáng lên khi game đã kết nối). Phải đang phát live trên TikTok thì mới kết nối được.
6. Ở cửa sổ game, xem lại cài đặt → **Vào game**.
7. Trong OBS / TikTok LIVE Studio: thêm nguồn **Window Capture** chọn cửa sổ game (khung dọc 1080×1920).

Kết thúc live: bấm **Ngắt LIVE**, rồi đóng TikTool. Tu vi tự lưu mỗi 5 giây và khi đóng cửa sổ.

Lỡ tắt TikTool giữa buổi: mở lại, TikTool tự kết nối lại LIVE đang chạy; bấm **Start Game** để mở lại game. Người xem thả tim / tặng quà là vào lại chỗ, tu vi vẫn còn.

---

## 3. Mapping sự kiện (quà, tim, comment → hành động trong game)

Tab **Hiệu ứng** của Nga Mi có sẵn mapping gợi ý:

| Sự kiện LIVE | Hành động game | Gợi ý |
|---|---|---|
| Comment đúng `1` | Vào phe | Phe 1 (Thanh Liên) |
| Comment đúng `2` | Vào phe | Phe 2 (Hồng Liên) |
| Mọi comment khác | Bình luận | Hiện bóng thoại trên đầu nhân vật |
| Mỗi 100 tim của 1 người | Tu luyện | +100 tu vi |
| Quà Rose (ID 5655) | Tu luyện | +50 tu vi |

- **Pháp bảo** (bản 0.3): 10 hành động "Pháp bảo: …" (Linh Thạch, Tụ Khí Đan, Chu Quả, Bàn Đào, Thiên Niên Linh Chi, Ngọc Dịch Quỳnh Tương, Bí Tịch Công Pháp, Phi Kiếm Trận, Long Châu, Linh Thú Đản). Gắn mỗi quà vào 1 pháp bảo và nhập số tu vi; món càng đắt càng đẹp. **Linh Thú Đản** là trứng: nở ra linh thú.
- **+ Tạo mapping** để thêm từng quà: chọn quà trong danh sách (hoặc **Nhập quà thủ công** bằng Gift ID), chọn hành động **Tu luyện**, nhập số tu vi.
- Thả tim hoặc tặng quà bất kỳ là người xem **vào tông môn** (có chỗ ngồi), không cần mapping riêng.
- Mỗi mapping có nút **Test** dùng tên ở ô "Người xem test" để gửi sự kiện thật vào game, không cần lên live.

---

## 4. Luật chơi Nga Mi Tu Tiên (tóm tắt)

Đầy đủ có ngay ở ô **Luật chơi** trên màn Cài đặt của game; mọi ô số ở đó đều có **gợi ý**, bấm **Điền gợi ý** để đưa về mặc định.

- **Tông môn**: tối đa 53 chỗ (Chưởng môn, Tả / Hữu hộ pháp + 5 hàng × 10). Im lặng 2 phút thì mờ dần rồi rời chỗ, tu vi vẫn giữ.
- **Cảnh giới**: 10 đại cảnh giới, tu vi càng cao trang phục càng đổi, ngồi càng cao. Thăng cấp có âm thanh, camera thu vào người đó và giọng AI chúc mừng.
- **Chu kỳ 30 phút**: 7 phút tu luyện + 3 phút **tỉ thí**, lặp 3 lần, rồi **quái rồng** xuất hiện; giết rồng xong chu kỳ tính lại.
- **Tỉ thí**: comment 1 / 2 để vào phe; hai phe đứng hai bên, ai không chọn ngồi dưới xem. Mỗi lần góp tu vi là 1 đòn sang 1 người ngẫu nhiên phe kia (người trúng mất 50% tu vi của đòn, điểm phe không bị trừ). Góp càng nhiều nhân vật càng to (1.000 → 1,2 lần, 5.000 → 1,5 lần…). Phe thắng +300 tu vi mỗi người, công đầu được x2 tu vi đã góp và 1 lượt vòng quay.
- **Quái rồng**: tu vi góp = sát thương. Rồng mỗi 5 giây đánh 1 người −50 tu vi, đòn thứ 5 phun lửa cả tông môn −50. Người gây sát thương nhiều nhất +2.000 tu vi và 1 lượt vòng quay.
- **Tẩu hỏa nhập ma**: ai nhận quá nhiều tu vi trong thời gian ngắn (mặc định 5.000 trong 60 giây) bị tẩu hỏa, đánh đồng môn; mọi người góp tu vi để giải cứu, người góp được x2.
- **Pháp bảo**: tặng quà đã gắn pháp bảo thì cộng tu vi kèm hiệu ứng riêng (đào rơi, đan bay, kiếm xoay vòng…).
- **Linh thú**: trứng nở ra Ngọc Thố (45%, +5% tu vi), Hỏa Kỳ Lân (40%, +5% sát thương) hoặc Kim Phượng (15%, cả hai). Trùng loại thì lên sao, tối đa 3 sao (15%); đủ 3 sao mà nở trùng thì được 1 lượt vòng quay. Reset theo tuần.
- **Hào quang**: ai vừa được cộng tu vi có hào quang sau lưng theo màu cảnh giới, 1 phút 30 giây không tu luyện thì tắt.
- **Thiên Kiếp**: lên đại cảnh giới từ Nguyên Anh trở lên thì sét giáng 15 giây; đồng môn thả tim, tặng quà hộ pháp. Đủ kết giới: +5% tu vi, hộ pháp nhiều nhất +1.000.
- **Linh Khí Triều Dâng**: ngẫu nhiên 15–25 phút một lần, 60 giây mọi tu vi x2.
- **Lưu theo tuần**: tu vi giữ qua mọi buổi live trong tuần, reset 0h thứ Hai.

---

## 5. Xuất top tuần để trao thưởng

Tab **Game** → thẻ **Nga Mi Tu Tiên** → **Top tuần này** hoặc **Top tuần trước**. File CSV (Hạng, Tên, ID, Tu vi) mở được bằng Excel. Tuần cũ vẫn được giữ trong cơ sở dữ liệu sau khi sang tuần mới.

Xóa tu vi tuần đang chơi: màn Cài đặt của game → **Xóa tu vi tuần này** (không hoàn tác được).

---

## 6. Sao lưu dữ liệu

Toàn bộ dữ liệu nằm trong `%APPDATA%\TikTool` (gõ đường dẫn này vào thanh địa chỉ File Explorer):

| File / thư mục | Nội dung |
|---|---|
| `tiktool.db` | SQLite: tu vi theo tuần (bảng `game_player_week`), danh sách quà, log 7 ngày |
| `mappings.json` | Mapping quà / tim / comment |
| `sessions.json`, `listener.json` | Phiên live, trạng thái tự kết nối lại |
| `assets\` | Ảnh quà, ảnh hiệu ứng |
| `logs\main.log` | Log ứng dụng khi cần tìm lỗi |
| `Local Storage\` | Cài đặt trong màn Cài đặt của game (giọng, máu rồng, chu kỳ…) |

Sao lưu: **tắt TikTool** rồi copy cả thư mục `%APPDATA%\TikTool` sang chỗ khác. Khôi phục: tắt TikTool, chép đè lại.

Xem tu vi trực tiếp trong SQLite (tùy chọn): mở `tiktool.db` bằng DB Browser for SQLite, bảng `game_player_week` có các cột `game_id, scope, week, user_id, nickname, score, joined_at, updated_at`. `scope = live` là dữ liệu live thật, `demo` là lúc mở thử.

---

## 7. Gặp lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Không nghe giọng AI, chỉ có chữ | Màn Cài đặt → **Nghe thử**: đọc dòng báo bên cạnh "Giọng nói AI". Kiểm tra Volume Mixer của Windows xem TikTool có bị tắt tiếng. |
| Bấm Kết nối LIVE báo lỗi | Kiểm tra đang phát live, TikTok LIVE ID đúng, mạng ổn; bấm **Thử kết nối lại**. Xem tab **Log**. |
| Người xem tặng quà nhưng game không cộng tu vi | Quà đó chưa có mapping → tạo mapping **Tu luyện** cho quà đó (tab Hiệu ứng). Tab **Log** có dòng "Live event received" cho mỗi sự kiện. |
| Game hiện bản cũ sau khi cập nhật | Bấm **Start Game** lại (cửa sổ game tự tải lại). |
| Báo cổng 3090 / 8765 đang bận | Đang có TikTool khác chạy (kể cả bản chạy bằng `npm run desktop`). Tắt hết rồi mở lại. |

Cần hỗ trợ thì gửi file `%APPDATA%\TikTool\logs\main.log` và ảnh chụp tab **Log**.
