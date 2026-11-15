# Kế hoạch lời thoại Nga Mi — bản MVP

Bản rút gọn của `KE_HOACH_MO_RONG_LOI_THOAI_NGA_MI.md`. Mục tiêu: lời đọc **đa dạng hơn, đúng trạng thái, cảm ơn cả quà chưa có mapping**, làm xong trong 3 bước nhỏ, không đổi luật game.

Bản đầy đủ vẫn giữ làm tài liệu tham khảo cho các vòng sau (mục "Để sau" cuối file).

## 0. Hiện trạng (đã đối chiếu mã)

| Chỗ | Hiện có | Vấn đề |
|---|---|---|
| `src/announcer.js` | Hàng chờ 4 câu, ưu tiên 0–3, cùng `key` thì thay, câu chờ quá 10–12 giây bị bỏ, tạo trước tiếng Piper | Tốt, **giữ nguyên**. Chỉ thiếu móc báo "vừa đọc xong" |
| `src/game.js` ~40 chỗ `announcer.say()` | Mỗi sự kiện 1 câu cố định | Nghe lặp sau vài chục phút |
| `Game.showInfo` | Chữ luật hiện mỗi 3 giây, giọng đọc tối đa 1 lần/30 giây (`infoVoiceEverySec`) | Chỉ có 2–3 câu luật xoay vòng, không có câu không khí |
| `useItem` | Đã đọc "{tên} vừa sử dụng pháp bảo X, cộng N tu vi" với N là `gained` thật | Đúng, **giữ nguyên** |
| `handleLiveEvent` (quà thô) | Chỉ vào/giữ chỗ, không nói gì | Quà **chưa có mapping** không được cảm ơn |
| `server.js processEvent` | Gửi quà thô cho game **trước** khi `engine.evaluate` | Game không biết quà có mapping hay không |
| `boss_start` | "Sử dụng pháp bảo để tấn công" | Thực tế mọi tu vi nhận được (tu luyện hoặc pháp bảo) đều đánh rồng → câu nói hẹp hơn luật |
| `speakableName` / `daoHuu` | Đã lọc emoji, ký tự lạ; tên rỗng → "đạo hữu" | Đủ dùng, không cần làm thêm |

## 1. Phạm vi MVP

**Làm:**
1. Kho câu + bộ chọn câu chống lặp, áp vào các sự kiện đang có.
2. Lời nền khi rảnh (thay cho cơ chế đọc luật 30 giây).
3. Cảm ơn quà chưa có mapping, nhờ hub gửi kèm cờ `mapped`.

**Không làm:** đổi thang ưu tiên, đổi kích thước hàng chờ, ACK mới, chống trùng `commandId`, công bằng giữa người tặng, đổi chủ đề theo giờ, mô phỏng 24 giờ, log chỉ số, ngắt câu đang đọc, chỉ số "linh thạch". Không đổi luật tu vi, tỉ thí, rồng, tẩu hỏa, thiên kiếp, mapping.

## 2. Bước 1 — Kho câu và bộ chọn câu

### 2.1 Tệp mới `src/lines.js`

Cùng kiểu UMD như các module khác (`root.NgaMi.LINES`), nạp bằng `<script>` trong `index.html`, `require` được trong test. Không dùng JSON để khỏi phải `fetch`.

```js
LINES = {
  // Câu sự kiện: chỉ biến thể LỜI ĐỌC. Chữ trên màn hình giữ như hiện tại (ngắn, ổn định).
  boss_start: [
    "Quái rồng {boss} xuất hiện với {mau} máu! Mọi tu vi đồng môn nhận được đều đánh vào nó.",
    "{boss} đã giáng xuống sân Nga Mi, {mau} máu. Đồng môn tu luyện để góp sức hạ rồng!",
    ...
  ],
  // Câu nền: cả chữ và lời đọc dùng chung một câu.
  idle_khong_khi: [ { t: "Sân Nga Mi hôm nay gió nhẹ, linh khí dồi dào.", when: "any" }, ... ],
  ...
}
```

- Biến viết `{ten}`, `{boss}`, `{mau}`… Thiếu biến nào thì **bỏ câu đó**, chọn câu khác; hết câu dùng được thì quay về câu cố định hiện tại trong `game.js` (giữ làm dự phòng).
- `{ten}` khi đọc dùng `daoHuu(name)` sẵn có (đã ra "đạo hữu X" hoặc "đạo hữu").
- `when` của câu nền: `any`, `tu_luyen` (ngoài tỉ thí), `duel`, `boss`, `tau_hoa`, `kiep`, `trieu_dang`. Câu không khớp trạng thái hiện tại thì không được chọn.

### 2.2 Bộ chọn câu (`pick(group, vars, state)`, khoảng 40 dòng, đặt trong `lines.js`)

- **Túi xáo trộn** cho mỗi nhóm: xáo cả nhóm, rút lần lượt, hết túi thì xáo lại. Khi xáo lại, câu vừa đọc cuối túi cũ không được đứng đầu túi mới.
- Chỉ nhớ trong bộ nhớ, khởi động lại thì xáo mới. Không lưu lịch sử.
- Với nhóm n câu, một câu không lặp lại trong ít nhất khoảng n−1 lượt rút của nhóm đó. Không hứa "không lặp trong 60 phút".
- Seed ngẫu nhiên truyền vào được để test chạy ổn định.

### 2.3 Áp vào `game.js`

Mỗi chỗ `announcer.say({... speech: "..." })` của các sự kiện trong bảng 4.1 đổi thành `speech: L.pick("boss_start", {...}) || speechCũ`. Không đổi `kind`, `priority`, `key`, `text`, thời điểm gọi.

Sửa luôn chữ `boss_start` trên màn hình cho đúng luật: "Quái rồng X xuất hiện! Mọi tu vi đều đánh vào rồng (N máu)".

## 3. Bước 2 — Lời nền khi rảnh

### 3.1 Cơ chế

- Announcer thêm một móc `hooks.idle()` gọi khi đọc xong và hàng chờ rỗng (cuối `pump`). Không đổi gì khác trong announcer.
- Game ghi `idleSince` khi được báo rảnh, và đặt lại khi có câu mới vào hàng chờ. Trong `tick`: nếu giọng bật, game đang chạy, đã rảnh ≥ **khoảng nghỉ nền** thì chọn 1 câu nền, `say({ kind: "idle", priority: 0, key: "idle" })`.
- `key: "idle"` + ưu tiên 0 bảo đảm: chỉ có tối đa 1 câu nền chờ, và câu sự kiện thật luôn đẩy nó ra.
- Khoảng nghỉ nền **mặc định 30 giây**, cài đặt được 15–120 giây; **0 = không đọc lời nền** (giữ ý nghĩa của ô `sInfo` hiện tại: 0 = chỉ hiện chữ). Dùng lại ô cài đặt `sInfo` / `infoVoiceEverySec`, chỉ đổi nhãn thành "Lời dẫn khi rảnh (giây)". Không cần di trú cài đặt.
- Chữ luật trên màn hình vẫn hiện mỗi 3 giây như cũ; chỉ phần **đọc** chuyển sang cơ chế này.

### 3.2 Vòng xoay nhóm câu nền

Mỗi lượt nền lấy nhóm tiếp theo trong vòng: `luat → khong_khi → nhap_mon → khong_khi → luat → loi_moi`

- `luat`: chính câu luật đang có trong `showInfo` (tỉ thí còn bao lâu, điểm hai phe, thưởng phe thắng), vì nó mang số liệu thật. Thêm 2–3 cách nói cho mỗi trạng thái.
- `nhap_mon`: luôn nói bước vào môn trước (tap tim hoặc tặng quà khi còn chỗ), rồi mới tới bình luận 1/2 **chỉ khi tỉ thí đang mở**. Tông môn đầy thì dùng câu "chờ có chỗ trống".
- `loi_moi`: xen kẽ lời mời miễn phí và lời mời pháp bảo; lời mời pháp bảo **tối đa 1 lần / 3 phút**, chưa đủ thì dùng câu miễn phí.
- Nhóm nào không có câu hợp trạng thái thì nhảy sang nhóm sau; cả vòng không có thì im tới lượt sau.

Với khoảng nghỉ 30 giây và câu dài khoảng 4 giây: khoảng 105 câu nền/giờ, trong đó ~35 câu luật, ~35 không khí, ~17 nhập môn, ~17 lời mời (≤ 20 lời mời pháp bảo/giờ theo trần 3 phút). Với kho ở mục 5, mỗi câu không khí lặp khoảng 1 lần/40 phút, chấp nhận được cho MVP.

### 3.3 Khi nào lời nền phải im

Màn cài đặt hoặc game dừng (`!running`), giọng tắt, mất kết nối hub (nếu game biết), đang ở phase `result` của tỉ thí. Câu nền không nêu tên, điểm, quà hay người chơi cụ thể.

## 4. Bước 3 — Cảm ơn quà chưa có mapping

### 4.1 Hub (`server.js processEvent`)

Đổi thứ tự: chạy `engine.evaluate` **trước**, rồi mới `forwardLiveEvent`, gửi kèm `mapped: commands.length > 0`.
- Chế độ an toàn (`safeMode`): không có action nào → `mapped: false`.
- Không đổi gì khác: lệnh vẫn `enqueue` như cũ, log như cũ.
- Hub cũ không gửi `mapped` → game coi là không biết và **không cảm ơn thêm** (giữ hành vi hiện tại).

### 4.2 Game (`handleLiveEvent`)

- `gift` có `mapped === false` và người đó có chỗ (`enterSeat` trả true) → đưa vào lô cảm ơn.
- `mapped === true` → không nói gì ở đây; `useItem` sẽ đọc câu "đã dùng pháp bảo, cộng N tu vi" như hiện tại.
- Lô cảm ơn gom **5 giây** giống cách `join` đang gom: nêu tối đa 2 tên, còn lại "cùng các đạo hữu khác"; `priority: 1`, `key: "gift_thanks"`.
- Câu cảm ơn không nêu tu vi, không nêu số quy đổi: "Cảm tạ {ten} đã gửi {qua} đến Nga Mi!"; tên quà không đọc được thì "đã gửi quà ủng hộ môn phái".
- Người không có chỗ vì tông môn đầy: giữ câu "Tông môn đã đủ chỗ" như hiện tại, không cảm ơn thêm.

**Cần kiểm khi làm:** nếu có quà map vào `tu_luyen` (không phải pháp bảo) thì hiện chưa có câu đọc. Xem `tu_luyen` có đọc không; nếu không, thêm câu "Cảm tạ {ten}, cộng {n} tu vi" ngay tại chỗ tính `gained` của `tu_luyen` khi nguồn là quà.

## 5. Kho câu vòng đầu (~120 câu)

| Nhóm | Số câu | Ghi chú |
|---|---:|---|
| Biến thể lời đọc cho sự kiện | 3 mỗi loại × ~20 loại ≈ 60 | `duel_soon`, `duel_start`, `duel_end`, `boss_start`, `boss_fire`, `boss_milestone`, `boss_killed`, `possess_warn`, `possess_start`, `possess_milestone`, `possess_freed`, `kiep_start`, `kiep_end` (thành/bại), `surge_start`, `hatch`, `wheel_result`, `seat`, `stage_up`, `join`, `full` |
| Luật (biến thể của câu `showInfo`) | 6 | Đủ ba ý thưởng tỉ thí: phe thắng +{thuong}, công đầu x{mvp}, bốc thăm quay thưởng — số lấy từ cấu hình |
| Không khí | 25 | Chia theo `when`; không bịa người, sự kiện |
| Nhập môn | 12 | Đúng thứ tự: vào môn trước, chọn phe sau |
| Lời mời miễn phí | 8 | Tap tim, bình luận, vào phe |
| Lời mời pháp bảo | 8 | Tự nguyện; không "phải", "chắc chắn thắng", "nhận tiền" |
| Cảm ơn quà chưa mapping | 6 | Không nêu tu vi |

Ai viết: nháp bằng AI, anh duyệt hết trước khi đưa vào `lines.js`. Nghe thử bằng Piper trước khi chốt, bỏ câu đọc vấp hoặc dài quá ~6 giây.

## 6. Kiểm thử (`test/`)

1. `pick`: thiếu biến thì bỏ câu; seed cố định cho kết quả cố định; hết túi không lặp câu vừa đọc; `when` lọc đúng trạng thái.
2. Lời nền: không đọc khi giọng tắt, `!running`, hàng chờ còn câu, chưa đủ khoảng nghỉ; `0` = không đọc; lời mời pháp bảo ≤ 1 lần/3 phút; câu sự kiện đẩy được câu nền đang chờ.
3. Nhập môn: chưa mở tỉ thí thì không có câu "bình luận 1/2"; tông môn đầy thì dùng câu chờ chỗ.
4. Quà: `mapped:false` → cảm ơn gom; `mapped:true` → không cảm ơn ở luồng quà thô; thiếu `mapped` → không nói thêm; 10 quà trong 5 giây → 1 câu.
5. Hub: `processEvent` gửi `mapped` đúng cho quà có/không mapping và ở chế độ an toàn.
6. Toàn bộ test hiện có vẫn đạt; tu vi, tỉ thí, phần thưởng không đổi.

Sau test: chạy thử 30–60 phút có người nghe, ghi lại câu nghe lặp hoặc sai rồi sửa kho.

## 7. Cần anh chốt

1. **Giấy phép giọng:** giọng Piper vais1000 chỉ dùng cá nhân. LIVE có nhận quà là dùng thương mại → cần giọng khác trước khi chạy LIVE thật. MVP vẫn làm và thử được với giọng hiện tại.
2. Khoảng nghỉ nền mặc định **30 giây** — được không, hay muốn dày hơn (khi đó cần thêm câu không khí)?
3. Cho phép sửa `server.js` (đổi thứ tự evaluate/forward, thêm `mapped`) — đây là thay đổi duy nhất ngoài thư mục game.
4. Câu `boss_start` sửa thành "mọi tu vi đều đánh vào rồng" — đúng ý anh về luật rồng không?

## Để sau (khi MVP chạy ổn)

Mở rộng kho lên ~300 câu; ghép câu từ mảnh; cảm ơn theo trạng thái cooldown/lỗi; log số câu đọc/bỏ/gom; mô phỏng chạy dài; nhãn "Game tự động · Giọng tổng hợp" trên màn hình; tài liệu vận hành và chính sách LIVE tách riêng.
