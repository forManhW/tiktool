# Nga Mi Tu Tiên: kế hoạch chi tiết

> Bản 10/11/2026, đã cập nhật theo Q1–Q9. Game tương tác TikTok LIVE chạy trên TikTool. Phần logic (tu vi, tỉ thí, thông báo, cài đặt, kết nối TikTool) **đã có code khung và test**. Phần đồ họa **chưa làm**, chờ anh duyệt từng hạng mục ở mục 6.

Ký hiệu: ✅ đã làm · 🟡 đề xuất, chờ anh quyết · 👤 bắt buộc người làm · 🤖 AI làm được

---

## 0. Quyết định đã chốt (10/11/2026)

| # | Câu hỏi | Anh chốt |
|---|---|---|
| Q1 | Chu kỳ | **Đổi ngày 10/11:** 7' tu luyện + 3' tỉ thí, lặp 3 lần (tỉ thí ở phút 7, 17, 27). **Phút 30 quái rồng xuất hiện**, đánh tới khi rồng chết thì chu kỳ tính lại từ đầu (mục 4.9) |
| Q2 | Lưu tu vi | **Lưu theo tuần** (đổi ngày 10/11): tu vi giữ qua mọi buổi live trong tuần, **reset 0h thứ Hai** (giờ máy), để làm top treo thưởng theo tuần. Có nút "Xóa tu vi tuần này" |
| Q3 | Vị trí trung tâm | **Người tu vi cao nhất làm Chưởng môn**. Top 2/3 = Tả/Hữu hộ pháp |
| Q4 | Quy đổi tim/quà/comment | **Game không tự quy đổi.** Game chỉ có function (action), streamer cài trong TikTool: quà gì, bao nhiêu tim, comment gì → bao nhiêu tu vi (mục 3.3) |
| Q5 | Phe thua | Không phạt. Phe thắng +300/người. **Công đầu phe thắng: x2 tu vi đã góp trong trận + 1 lượt quay** (đổi 11/11, thay cho +1000). Hòa +100/người |
| Q6 | Tên phe | **Thanh Liên** (gõ 1) / **Hồng Liên** (gõ 2) |
| Q7 | Đọc tiểu cảnh | Đọc mọi tiểu cảnh (có công tắc tắt trong Cài đặt) |
| Q8 | Phần mềm phát | **TikTok LIVE Studio** |
| Q9 | Khung hình | **Dọc 1080×1920**. Đã sửa TikTool để mở cửa sổ game theo `manifest.window` (mục 9) |

## 1. Hiện trạng TikTool (đã đọc code, không suy đoán)

- Game là trang web trong `Game/<Thư mục>/`, hub phục vụ tại `http://127.0.0.1:3090/game/NgaMi_TuTien/`. TikTool tự đọc `manifest.json`.
- Game nhận sự kiện qua SDK `/sdk/tiktool-game.js` (WebSocket `ws://127.0.0.1:8765`):
  - `onLiveEvent(event)`: sự kiện thô nếu khai báo trong `manifest.liveEvents`. Có `user.id`, `user.nickname`, `user.avatarUrl`; quà có `gift.count` và `gift.diamonds` (giá 1 quà, tính bằng xu); tim có `like.count`; comment có `comment.text`.
  - `onAction(action, params, source)`: effect do streamer map trong dashboard.
- Nguồn sự kiện: `live_adapter.py` dùng thư viện **TikTokLive (Python, không chính thức)**. TikTok đổi giao thức thì có thể hỏng tới khi thư viện cập nhật.
- Có **Giả lập sự kiện LIVE** trong dashboard để test không cần lên live.
- Quy định game của TikTool: không `eval`, **không tải script từ Internet**, không đọc file ngoài thư mục game. Mọi thư viện phải tải về để trong thư mục game.

Theo Q4, game **chỉ nhận action** qua mapping của TikTool (`liveEvents` để trống). Mọi con số quy đổi do streamer cài trong dashboard. Lệnh nào cũng kèm `source.user`, nên game vẫn biết ai được cộng tu vi.

---

## 2. Công cụ và môi trường

| Hạng mục | Chọn | Ghi chú |
|---|---|---|
| Ngôn ngữ | JavaScript thuần (ES2020), không cần build | Giống các game có sẵn. Module dạng UMD: chạy trong trình duyệt và test bằng Node |
| Runtime | Electron 39 (đã có trong TikTool), Node 24 (máy anh đang có) | |
| Vẽ | ✅ **DOM + CSS**, không thư viện ngoài | Không có hào quang thường trực nên không cần WebGL. Đo thử: 200 người chơi, mỗi lần vẽ lại khoảng 0,4 ms |
| Giọng nói | Web Speech API (giai đoạn 1), TTS chạy local (giai đoạn 2) | Xem mục 5 |
| Lưu dữ liệu | **SQLite của hub TikTool** (`data/tiktool.db`, bảng `game_player_week`), đổi ngày 11/11. localStorage chỉ còn là bản sao dự phòng | Tu vi theo tuần, mỗi tuần một bộ riêng nên tuần cũ vẫn còn để xuất top. Nút **Top tuần này / Top tuần trước** trên thẻ game ở TikTool tải file CSV (mở bằng Excel) |
| Test | `node --test Game/NgaMi_TuTien/test/game.test.js` · **Giả lập sự kiện LIVE** trong TikTool | 20 test đang chạy được |
| Font | 🟡 Font có đủ dấu tiếng Việt, giấy phép OFL, để trong `assets/fonts/` | Ứng viên: Noto Serif, Playfair Display (cho tiêu đề). Anh duyệt |
| Công cụ đồ họa (người dùng) | Photoshop/Krita/Figma, công cụ AI tạo ảnh nếu chọn | Xem mục 6.3 |

Cấu trúc thư mục:

```
Game/NgaMi_TuTien/
├─ manifest.json           ✅
├─ index.html              ✅ khung tạm: Cài đặt, Thoát, bảng xếp hạng dạng chữ
├─ KE_HOACH.md             ✅ file này
├─ src/
│  ├─ config.js            ✅ cảnh giới, tỉ lệ, tỉ thí, giọng (mọi con số cân bằng)
│  ├─ cultivation.js       ✅ đệ tử, tu vi, cảnh giới, xếp chỗ ngồi
│  ├─ duel.js              ✅ tỉ thí 2 phe theo thời gian
│  ├─ announcer.js         ✅ hàng đợi thông báo, lọc tên để đọc, bộ đọc giọng
│  ├─ game.js              ✅ nối TikTool → tu vi → tỉ thí → thông báo, cài đặt, lưu
│  ├─ render.js            ✅ vẽ khung dọc từ getState() (dùng được cho overlay)
│  ├─ style.css            ✅
│  └─ assets/raw | game    ✅ tranh gốc | bản thu nhỏ cho game
├─ tools/                  ✅ build-assets.js, realm-table.js, png.js
└─ test/game.test.js       ✅
```

---

## 3. Kết nối TikTool: các function đã viết sẵn

### 3.1 Luồng

```
TikTok LIVE → live_adapter.py → TikTool hub → mapping streamer cài → SDK onAction → game.handleAction(action, params, source)
game.getState() → live.emit() → overlay (mirror)
```

### 3.2 Function sự kiện (khai báo trong `manifest.json`, code ở `src/game.js` → `ACTIONS`)

Game **không tự thêm nhân vật nào**. Chỉ người xem thật, hoặc sự kiện bắn từ **Giả lập sự kiện LIVE** của TikTool, mới xuất hiện.

| Action | Tham số | Làm gì | Lỗi trả về TikTool |
|---|---|---|---|
| `binh_luan` | | **Comment tự do**: hiện bóng thoại trên đầu người đang ngồi (lọc ký tự lạ, tối đa 40 ký tự, 4 giây) | Chưa tham gia (comment không làm người mới tham gia) |
| `tu_luyen` | `amount` = **số tu vi anh đặt cho mapping đó** (TikTool nhân theo combo/số mốc tim) | **Thả tim / tặng quà = tham gia**: chưa có chỗ thì vào chỗ luôn (hết chỗ thì báo "Tông môn đã đủ chỗ" và không cộng). Cộng tu vi. Tim và từng món quà đều dùng action này, mỗi mapping một số riêng. Đang tỉ thí và đã vào phe thì cộng cùng số đó vào điểm phe | Chưa tham gia |
| `vao_phe` | `side` 1 / 2 | Vào phe. **Chỉ có hiệu lực khi đang tỉ thí**, mỗi người 1 lần/trận | Chưa tới giờ tỉ thí; đã ở trong phe; tỉ thí tắt; chưa tham gia |
| `start_duel` / `end_duel` | | Mở / kết thúc trận ngay | |

Sự kiện thô (`liveEvents`: like, gift) **không cộng tu vi**, chỉ giữ chỗ cho người đang thả tim/tặng quà. Lý do: mapping tim có thể đặt "mỗi N tim", nên một mình mapping không đủ để biết ai còn đang tap. Vì vậy safe mode của TikTool không chặn việc giữ chỗ, nhưng vẫn chặn tu vi và bóng thoại.

### 3.3 Mapping trong TikTool ✅ (tự tạo sẵn)

`manifest.json` có `defaultMappings`. Khi hub khởi động, nếu game **chưa có mapping nào**, TikTool tự tạo bộ dưới đây. Anh sửa, thêm, xóa thoải mái trong dashboard. Lưu ý: xóa hết mapping của game thì lần khởi động hub sau sẽ tạo lại bộ mặc định.

| # | Sự kiện | Điều kiện | Action | Tham số | Priority |
|---|---|---|---|---|---|
| 1 | comment | khớp đúng "1" | `vao_phe` | side 1 | 20 |
| 2 | comment | khớp đúng "2" | `vao_phe` | side 2 | 20 |
| 3 | comment | **mọi comment** (comment tự do) | `binh_luan` | | 0 (thấp nhất) |
| 4 | like | theo từng người, **mỗi 100 tim** | `tu_luyen` | 100 tu vi / mỗi 100 tim | 0 |
| 5 | gift | **Hoa hồng (5655)**, ví dụ mẫu | `tu_luyen` | 50 tu vi/quà | 0 |

Tim đặt **mỗi 100 tim = +100 tu vi** (anh chốt 10/11) để TikTool không phải gửi lệnh cho game sau mỗi lượt tim. Tim lẻ chưa đủ 100 được giữ lại cộng dồn cho lần sau. Một lượt nhiều tim (vd. 250) vẫn chỉ là 1 lệnh (+200). Thả tim chưa đủ 100 vẫn **tham gia và giữ chỗ** ngay, vì việc này dùng sự kiện tim thô, không chờ mapping.

Comment chỉ chạy **1 mapping có priority cao nhất**. Thứ tự trên đảm bảo "1"/"2" thắng comment tự do. **Quà làm giống các game khác:** mỗi món quà một mapping. Anh chọn quà trong dashboard, chọn action `tu_luyen`, nhập số tu vi cho món đó. Combo x5 thì TikTool tự nhân 5. Quà nào chưa có mapping thì không cộng tu vi (chỉ giữ chỗ).

Giới hạn của TikTool hiện tại (đã đọc `core/live-events.js`), anh cần biết khi cài:

1. **Quà và comment chỉ chạy 1 mapping có priority cao nhất.** Vì vậy 1 quà chỉ gọi được 1 action. Đây là lý do `tu_luyen` tự cộng cả tu vi lẫn điểm phe.
2. Mapping "mọi quà" (giftId trống) cộng cùng 1 số cho mọi quà. Muốn quà đắt cộng nhiều hơn thì tạo mapping riêng cho món đó.
3. Combo quà nhân `amount` theo số lượng, tối đa 100 lần/sự kiện (`maxPerEvent`).
4. Tim theo từng người cần `likeScope = user`. Để `room` thì tu vi cộng cho người thả tim **vượt mốc** của cả phòng.
5. Ngưỡng cảnh giới (mục 4.2) tính bằng tu vi. Số anh cài ở đây quyết định người chơi lên cấp nhanh hay chậm, nên hai bên phải cân cùng nhau.

## 4. Thiết kế gameplay

### 4.1 Bối cảnh Nga Mi

- Nga Mi Sơn (峨眉山, Tứ Xuyên) là núi có thật, nổi tiếng biển mây. Phái Nga Mi trong võ hiệp thường có đệ tử nữ, áo xanh/trắng, kiếm pháp.
- ✅ Anh chốt: **tránh phong cách chùa/Phật**. Nga Mi trong game là **kiếm phái tu tiên trên núi**. Danh sách yếu tố cấm và yếu tố thay thế ở [SPEC_TRANH.md](SPEC_TRANH.md) mục 1.
- 🟡 Chất liệu hình ảnh: đỉnh núi, biển mây, đại điện kiếm phái, bậc thềm đá làm "bậc thang tu vi", kiếm bay, lụa bay, hạc.
- ⚠️ Bản quyền: tên "Nga Mi", núi, tên cảnh giới (Luyện Khí, Trúc Cơ...) là chất liệu chung của thể loại, dùng được. **Không dùng** nhân vật/tình tiết của Kim Dung (Diệt Tuyệt sư thái, Chu Chỉ Nhược, Ỷ Thiên kiếm...) và **không sao chép** hình ảnh/giao diện của "Đỉnh Linh Sơn" hay "Tu chân giới online" trong 2 ảnh mẫu. 2 ảnh chỉ dùng để tham khảo **bố cục**.

### 4.2 Cảnh giới và ngưỡng tu vi (src/config.js → `realms`)

46 bậc, 10 đại cảnh giới. Ngưỡng là tu vi **tích lũy trong tuần** (reset 0h thứ Hai, Q2).

**Cách thiết kế:**
- **Luyện Khí** dồn 9 tầng vào 750 tu vi, mỗi tầng chỉ cần thêm 30–200. Người mới thả vài chục tim là lên tầng 2, có ngay câu chúc mừng. Đây là "móc câu" đầu live.
- Từ **Trúc Cơ** trở đi, mỗi đại cảnh giới bắt đầu ở mức gấp khoảng **2,2–4 lần** đại cảnh giới trước (1.000 → 4.000 → 13.000 → 36.000 → 90.000 → 200.000 → 460.000 → 1.000.000 → 3.000.000). Bậc càng cao càng hiếm, để Chưởng môn và người mặc bộ Chân Tiên có giá trị.
- Mỗi đại cảnh giới có 4 tiểu cảnh với bước tăng dần, để người chơi bậc cao vẫn thấy tiến độ.

**Bảng đầy đủ.** Hai cột cuối chỉ để tham khảo khi anh đặt số tu vi cho từng món quà: giả sử 1 tim = 1 tu vi và mỗi xu giá quà tương đương 50 tu vi. Game **không** tự quy đổi (Q4). Anh cài số khác trong TikTool thì in lại bảng bằng `node tools/realm-table.js <tu vi/tim> <tu vi/xu>`.

| Cảnh giới | Tu vi tích lũy | Cần thêm từ bậc trước | ≈ Tim (1 tim = 1) | ≈ Xu quà (1 xu = 50) |
|---|---:|---:|---:|---:|
| Luyện Khí tầng 1 | 0 | - | 0 | 0 |
| Luyện Khí tầng 2 | 30 | +30 | 30 | 1 |
| Luyện Khí tầng 3 | 70 | +40 | 70 | 2 |
| Luyện Khí tầng 4 | 120 | +50 | 120 | 3 |
| Luyện Khí tầng 5 | 190 | +70 | 190 | 4 |
| Luyện Khí tầng 6 | 280 | +90 | 280 | 6 |
| Luyện Khí tầng 7 | 400 | +120 | 400 | 8 |
| Luyện Khí tầng 8 | 550 | +150 | 550 | 11 |
| Luyện Khí tầng 9 | 750 | +200 | 750 | 15 |
| Trúc Cơ Sơ kỳ | 1.000 | +250 | 1.000 | 20 |
| Trúc Cơ Trung kỳ | 1.500 | +500 | 1.500 | 30 |
| Trúc Cơ Hậu kỳ | 2.200 | +700 | 2.200 | 44 |
| Trúc Cơ Đại viên mãn | 3.000 | +800 | 3.000 | 60 |
| Kết Đan Sơ kỳ | 4.000 | +1.000 | 4.000 | 80 |
| Kết Đan Trung kỳ | 5.500 | +1.500 | 5.500 | 110 |
| Kết Đan Hậu kỳ | 7.500 | +2.000 | 7.500 | 150 |
| Kết Đan Đại viên mãn | 10.000 | +2.500 | 10.000 | 200 |
| Nguyên Anh Sơ kỳ | 13.000 | +3.000 | 13.000 | 260 |
| Nguyên Anh Trung kỳ | 17.000 | +4.000 | 17.000 | 340 |
| Nguyên Anh Hậu kỳ | 22.000 | +5.000 | 22.000 | 440 |
| Nguyên Anh Đại viên mãn | 28.000 | +6.000 | 28.000 | 560 |
| Hóa Thần Sơ kỳ | 36.000 | +8.000 | 36.000 | 720 |
| Hóa Thần Trung kỳ | 46.000 | +10.000 | 46.000 | 920 |
| Hóa Thần Hậu kỳ | 58.000 | +12.000 | 58.000 | 1.160 |
| Hóa Thần Đại viên mãn | 72.000 | +14.000 | 72.000 | 1.440 |
| Luyện Hư Sơ kỳ | 90.000 | +18.000 | 90.000 | 1.800 |
| Luyện Hư Trung kỳ | 110.000 | +20.000 | 110.000 | 2.200 |
| Luyện Hư Hậu kỳ | 135.000 | +25.000 | 135.000 | 2.700 |
| Luyện Hư Đại viên mãn | 165.000 | +30.000 | 165.000 | 3.300 |
| Hợp Thể Sơ kỳ | 200.000 | +35.000 | 200.000 | 4.000 |
| Hợp Thể Trung kỳ | 250.000 | +50.000 | 250.000 | 5.000 |
| Hợp Thể Hậu kỳ | 310.000 | +60.000 | 310.000 | 6.200 |
| Hợp Thể Đại viên mãn | 380.000 | +70.000 | 380.000 | 7.600 |
| Đại Thừa Sơ kỳ | 460.000 | +80.000 | 460.000 | 9.200 |
| Đại Thừa Trung kỳ | 560.000 | +100.000 | 560.000 | 11.200 |
| Đại Thừa Hậu kỳ | 680.000 | +120.000 | 680.000 | 13.600 |
| Đại Thừa Đại viên mãn | 820.000 | +140.000 | 820.000 | 16.400 |
| Độ Kiếp Sơ kỳ | 1.000.000 | +180.000 | 1.000.000 | 20.000 |
| Độ Kiếp Trung kỳ | 1.300.000 | +300.000 | 1.300.000 | 26.000 |
| Độ Kiếp Hậu kỳ | 1.700.000 | +400.000 | 1.700.000 | 34.000 |
| Độ Kiếp Đại viên mãn | 2.200.000 | +500.000 | 2.200.000 | 44.000 |
| Chân Tiên | 3.000.000 | +800.000 | 3.000.000 | 60.000 |

**Đọc bảng theo kiểu người chơi** (với mapping tham chiếu):

| Kiểu người chơi trong 1 buổi | Tu vi | Tới đâu |
|---|---|---|
| Ghé xem, thả khoảng 100 tim | 100 | Luyện Khí tầng 3 |
| Thả tim đều tay, khoảng 1.000 tim | 1.000 | Trúc Cơ (đổi trang phục lần đầu) |
| Tặng khoảng 100 xu | 5.000 | Kết Đan Sơ kỳ |
| Tặng khoảng 1.000 xu | 50.000 | Hóa Thần Trung kỳ |
| Tặng khoảng 10.000 xu | 500.000 | Đại Thừa Sơ kỳ |
| Tặng khoảng 60.000 xu | 3.000.000 | Chân Tiên (đỉnh) |

👤 Đây là thiết kế trên giấy. Sau 1–2 buổi live thử, anh xem log rồi chỉnh số trong mapping TikTool hoặc chỉnh `stages` trong `config.js`. Test sẽ báo lỗi nếu ngưỡng không tăng dần.

### 4.3 Nguồn tu vi

- Tim, quà, comment, follow, share: **do mapping TikTool quyết định** (mục 3.3).
- Game chỉ tự cộng phần thưởng tỉ thí: thắng +300/người, công đầu phe thắng x2 tu vi đã góp trong trận (thay cho +300) + 1 lượt quay, hòa +100/người (sửa trong `src/config.js` → `duel`).

### 4.4 Cân bằng

Tu vi tích lũy cả tuần (Q2), nên ngưỡng ở 4.2 tính cho **1 tuần nhiều buổi live**. 👤 Sau buổi live thử, anh xem ai lên tới đâu rồi chỉnh **một trong hai**: số trong mapping TikTool, hoặc ngưỡng trong `config.js`. Tôi chưa có số liệu live thật nên không đưa con số cam kết.

### 4.5 Chỗ ngồi ✅ (anh chốt 10/11)

1. **Thả tim hoặc tặng quà là tham gia** (anh chốt 10/11, bỏ lệnh comment "tham gia"). Comment không làm người mới tham gia, chỉ hiện bóng thoại cho người đang ngồi.
2. **Tối đa 53 người cùng lúc**: 1 Chưởng môn, 2 Hộ pháp, 5 hàng × 10 cột. **Ai đang tham gia cũng hiện trên màn hình.** Hết chỗ thì báo "Tông môn đã đủ chỗ, hẹn đạo hữu … lần tuyển sinh tiếp theo" (mỗi người tối đa 1 lần / 30 giây).
3. Thứ hạng theo tu vi. **Top 1 = Chưởng môn, top 2/3 = Tả/Hữu hộ pháp.** Ai vượt tu vi thì lên ghế ngay, người cũ bị đẩy xuống. Từ #4 điền kín hàng 1 từ trái sang phải, rồi xuống hàng 2…
4. **Im lặng 2 phút** (không tim / quà / comment; mỗi lượt tim đều tính, không cần đủ 100) thì mờ dần tới 50% trong 3 giây, rồi rời chỗ để nhường người khác. **Rời chỗ chỉ là rời màn hình, tu vi giữ nguyên tới hết tuần** (đã bỏ luật "dưới Kết Đan mất tu vi" ngày 10/11). Thả tim / tặng quà lại là quay về. Đang live lúc qua 0h thứ Hai thì tu vi mọi người về 0 (người đang ngồi vẫn ngồi), có thông báo "Tuần mới bắt đầu".
5. Dưới tên mỗi người có **thanh tiến độ siêu mảnh**: % tu vi tới tiểu cảnh tiếp theo.
6. **Lọc tên**: chỉ giữ chữ Latin (gồm tiếng Việt có dấu), số và . _ -. Tên toàn ký tự lạ (emoji, chữ Hán…) thì hiện "Đạo hữu", thông báo chỉ gọi "đạo hữu", ví dụ "Chúc mừng đạo hữu đột phá thành công, bước vào cảnh giới Hóa Thần Sơ kỳ".
7. Có người vào/rời chỗ hoặc đổi người ở 3 ghế đài thì xếp lại ngay. Còn lại, chỗ ngồi xếp lại tối đa mỗi 1,5 giây để không trượt qua lại liên tục.
8. **Im 3 giây không có thông báo nào** thì banner hiện luật chơi (chỉ chữ, không đọc). Luân phiên 2 câu: "Tỉ thí sẽ bắt đầu sau X phút Y giây, phe thắng mỗi người +300 tu vi" và "Thả tim hoặc tặng quà để nhập môn Nga Mi". Trong trận thì nhắc "Bình luận 1 / 2". Tắt tỉ thí thì chỉ nhắc câu thả tim / tặng quà.

### 4.6 Tỉ thí 2 phe (src/duel.js)
- **Xếp chỗ khi tỉ thí** (anh chốt 11/11, 🟡 chờ duyệt chi tiết): phe 1 nửa trái, phe 2 nửa phải (4 người / hàng, canh giữa theo chiều dọc), ai không chọn phe ngồi dải dưới xem và tu luyện. Bệ 3 ghế đài ẩn trong lúc tỉ thí. Mỗi lần góp tu vi = 1 đòn: người đánh phóng to 1,5 lần trong 0,5 giây, luồng sáng màu phe bay sang 1 người ngẫu nhiên phe kia, người trúng rung nhẹ và **mất 50% tu vi của đòn** (`duel.hitLossRatio`, không trừ điểm phe). Người góp nhiều **to dần tại chỗ** theo tu vi đã góp trong trận: 1.000 → 1,2 lần, 5.000 → 1,5, 10.000 → 1,8, 20.000 → 2,2, tối đa 2,6 (`duel.grow`); người to đè lên người nhỏ, hết tỉ thí thì về cỡ thường. Hết tỉ thí (sau màn kết quả) mọi người về chỗ cũ.

```
[Tu luyện 7'] → còn 60s: báo "sắp tỉ thí" → [TỈ THÍ 3'] → [Kết quả 12s] → lặp lại (phút 7, 17, 27) → phút 30: QUÁI RỒNG (mục 4.9)
```

- Đang tỉ thí: comment **"1"** hoặc **"2"** (mapping → `vao_phe`) để vào phe. Mỗi người chọn **1 lần/trận**, không đổi phe. Vào giữa trận vẫn được.
- **Điểm phe = tu vi thành viên kiếm được từ lúc vào phe** (anh chốt 10/11), không tính tu vi có sẵn trước đó. Trong trận mọi người vẫn thả tim / tặng quà như bình thường. Người chưa chọn phe không được tính. Phần đã góp vẫn tính kể cả khi người đó rời chỗ sau đó. Công đầu (MVP) = người góp nhiều điểm nhất phe thắng, được thêm đúng số tu vi đã góp trong trận (x2) và 1 lượt quay; những người khác phe thắng +300.
- Hết giờ: phe nhiều điểm hơn thắng, bằng điểm là hòa. Thưởng theo Q5. MVP = người góp nhiều điểm nhất phe thắng.
- Tắt "Tỉ thí" trong Cài đặt thì không có trận nào; comment 1/2 bị bỏ qua.
- Streamer bấm Thoát giữa trận thì **trận bị hủy, không thưởng**.

### 4.9 Quái rồng (src/boss.js), anh chốt 10/11

- **Lịch:** rồng xuất hiện ở phút 30 của chu kỳ, sau trận tỉ thí thứ 3. Trong lúc đánh quái **không có tỉ thí**. Rồng chết, hiện kết quả 12 giây, rồi chu kỳ tính lại từ đầu (tỉ thí tiếp theo sau 7 phút). Không giới hạn thời gian: đánh tới khi rồng chết.
- **Máu rồng:** mặc định 30.000, chỉnh trong Cài đặt ("Máu rồng"). Cài đặt còn có công tắc "Đánh quái rồng" và "Rồng xuất hiện sau (phút)".
- **Sát thương** = tu vi người chơi kiếm được trong lúc đánh (tim, quà theo mapping, action `tu_luyen`). Người chơi vẫn được cộng đủ tu vi đó.
- **Rồng đánh trả** (anh chốt 11/11): mỗi 5 giây 1 đòn thường, trúng **1 người ngẫu nhiên đang ngồi, -50 tu vi** (luồng lửa từ rồng xuống người đó, chữ thông báo không đọc). Cứ **đòn thứ 5 là phun lửa**: sóng lửa quét xuống, **mọi người đang ngồi -50 tu vi**, có giọng đọc. Tu vi không xuống dưới 0. Người trúng đòn rung nhẹ tại chỗ, số đỏ -50 nảy lên. Chỉnh trong `boss.attackEveryMs / attackDamage / fireEvery / fireDamage`.
- **Thưởng:** người gây sát thương nhiều nhất được **+2.000 tu vi** (`boss.mvpBonus`) và **1 lượt vòng quay**. Công đầu phe thắng tỉ thí cũng được 1 lượt quay (`duel.mvpWheel`).
- **Vòng quay:** ra tu vi ngẫu nhiên, mặc định +200 (30%), +500 (25%), +1.000 (20%), +2.000 (13%), +5.000 (9%), +10.000 (3%). Sửa ở `config.js` → `wheel.prizes`. Quay 5 giây, hiện kết quả 3 giây. Nhiều lượt thì xếp hàng quay lần lượt. 🟡 Giải thưởng ngoài game (quà thật) chưa có, chờ anh quyết.
- **Giao diện** (🟡 chờ anh duyệt): rồng ở vùng logo (y khoảng 90–730), thanh máu và top 3 sát thương ở y 690. Tông môn lùi xuống 360 px (hàng cuối lấn vào vùng comment TikTok, anh đã chấp nhận). Mỗi cú đánh: rồng rung và lóe sáng, số sát thương bay lên. Rồng chết thì sáng rồi rơi xuống mờ dần. Trong lúc đánh quái **camera không thu vào người thăng cấp** để rồng luôn trong khung.
- **Thông báo có giọng:** rồng xuất hiện, còn 50/25/10% máu, rồng bị tiêu diệt kèm công đầu, quay thưởng và kết quả quay. Câu nhắc luật trong lúc đánh đọc lượng máu còn lại.
- **Ảnh rồng:** đang dùng `dragonBoss.png` (rồng xanh, anh thêm 10/11), tên tạm "Thanh Long". Ảnh trúng đòn / gục ngã là tùy chọn (`boss_dragon_hurt.png`, `boss_dragon_defeated.png`). Thêm hoặc đổi ảnh rồi chạy `node tools/build-assets.js`.

### 4.10 Tẩu hỏa nhập ma (src/possession.js), anh chốt 10/11

- **Phát hiện:** một người nhận ≥ **5.000 tu vi trong 60 giây** (chỉ tính tim / quà qua `tu_luyen`, không tính thưởng) thì bị tẩu hỏa. Cả hai số chỉnh trong Cài đặt. Mỗi lúc chỉ 1 người bị.
- **Ưu tiên:** không có tỉ thí / đánh quái thì bắt đầu ngay. Đang có thì báo "khí tức hỗn loạn, sắp tẩu hỏa nhập ma!", **chờ sự kiện đó xong (kể cả màn kết quả) rồi bắt đầu ngay**. Đang tẩu hỏa thì không có tỉ thí, không ra rồng. Hết tẩu hỏa: **chu kỳ tính lại từ đầu** (7 phút sau có tỉ thí).
- **Người bị tẩu hỏa** đứng ở chỗ quái (giữa phía trên, ảnh `tauHoaNhapMa.png`, hào quang tím), không ngồi trong hàng, không bị rời chỗ vì im lặng. Mỗi lần họ tặng quà / thả tim được X tu vi: **đánh 1 người ngẫu nhiên** đang ngồi, người đó **mất X tu vi** (không âm, không đọc tụt cấp). Họ ngừng tặng thì ngừng đánh. Bản thân họ vẫn được cộng tu vi.
- **Giải cứu:** tu vi người khác kiếm được trong lúc này trừ vào "máu giải cứu". Máu này **dùng chung ô "Máu (rồng & người tẩu hỏa)"** trong Cài đặt. **Không giới hạn thời gian.**
- **Cứu thành công:** mỗi người góp sức được thưởng thêm đúng số tu vi đã góp (**x2**). Người được cứu về lại hàng.
- **Giao diện** (🟡 chờ anh duyệt): thanh "Giải cứu <tên>" màu tím, top 3 người góp sức. **Luồng sáng** (quả cầu kèm vệt sáng): tím đỏ từ người tẩu hỏa bay xuống người trúng đòn, vàng từ người giải cứu bay lên. **Người ra đòn phóng to 1,5 lần trong 0,5 giây.** Tới nơi thì nảy số to: "-X" vàng trên người tẩu hỏa, "-X" đỏ trên người bị đánh, người bị đánh lóe đỏ. Đánh rồng cũng có luồng sáng, phóng to và số "-X" nảy ra trên rồng. Trong lúc sự kiện, dòng thông báo dời xuống y khoảng 600 để không che số. Tông môn lùi xuống như lúc đánh quái, camera không thu vào người thăng cấp.
- **Giọng đọc:** sắp tẩu hỏa, bắt đầu tẩu hỏa, còn 50/25/10%, được giải cứu. Đòn đánh chỉ hiện chữ (không đọc, vì quá nhiều).

### 4.7 Thông báo (chữ + giọng)

| Sự kiện | Câu đọc (ví dụ) | Ưu tiên |
|---|---|---|
| Vào chỗ (gom 10 giây) | Người chơi lần đầu: "Hoan nghênh đạo hữu An, Bình gia nhập Nga Mi". Đã có trong dữ liệu tuần này: "Chào mừng đạo hữu Chi quay trở lại" (đổi 11/11) | thấp |
| Lên tiểu cảnh | "Chúc mừng đạo hữu ABC đạt cảnh giới Luyện Khí tầng 7" | vừa |
| Lên đại cảnh giới | "Chúc mừng đạo hữu ABC đột phá thành công, bước vào cảnh giới Trúc Cơ Sơ kỳ" | cao |
| Lên ghế top (từ 1.000 tu vi) | "Đạo hữu ABC trở thành Chưởng môn phái Nga Mi" | cao |
| Sắp tỉ thí / Bắt đầu / Kết quả | "Phe Thanh Liên chiến thắng. Công đầu thuộc về đạo hữu ABC" | cao |

Chống ồn khi live đông:

- Hàng đợi giọng tối đa 8 câu. Đầy thì bỏ câu ưu tiên thấp nhất.
- Cùng 1 người lên nhiều bậc liên tiếp thì chỉ đọc **bậc mới nhất**.
- Tặng quà lớn nhảy 5 bậc thì chỉ đọc 1 câu.
- **Chữ luôn hiện**, kể cả khi tắt giọng.

Tên để đọc được lọc: bỏ emoji, ký tự trang trí, chữ Hán/Thái... Không còn gì để đọc thì đọc "một vị đạo hữu". Ví dụ `不是鸿恩` → "một vị đạo hữu", `ngo.hay.cui` → "ngo hay cui". 👤 Tên kiểu teencode/viết tắt TTS sẽ đọc sai, không sửa được hết.

### 4.8 🟡 Đề xuất thêm (chưa code, chờ anh duyệt)

- **Độ kiếp có lôi kiếp** (giống ảnh 2): khi chạm Đại viên mãn thì kẹt bình cảnh, cả phòng có 30 giây để thả đủ N tim giúp vượt kiếp. ⚠️ Từ khóa "hộ pháp" **không được là "1"** vì trùng với chọn phe tỉ thí.
- Bảng "Cách chơi" cố định bên trái (giống ảnh 1).
- Thanh mục tiêu tim của cả phòng (giống "16.682 / 17.000" ở ảnh 1).

---

## 5. Giọng nói AI

### 5.0 Đã làm: giọng AI chạy trên máy (cách B, anh chốt 10/11)

- Hub TikTool có `/api/tts` dùng **Piper** + giọng `vi_VN-vais1000-medium`. Không cần mạng, không tốn phí. Chi tiết và giấy phép: `docs/GIONG_DOC_AI.md`.
- Game ưu tiên giọng AI của hub. Hub không có thì dùng giọng tiếng Việt của Windows. Màn Cài đặt ghi rõ đang dùng giọng nào.
- Đo ngày 10/11: câu đầu khoảng 1,5 giây (nạp model), sau đó 0,4–1,7 giây/câu, câu lặp lại 6ms. Game tạo trước câu kế tiếp trong lúc đang đọc câu trước.
- ⚠ **Giấy phép giọng vais1000 không an toàn cho app thương mại** (dựa trên giọng lessac cấm thương mại). Phải thay giọng trước khi phát hoặc bán bộ cài.

### 5.1 Kết quả kiểm tra máy anh

Tôi đã đọc danh sách giọng Windows trên máy này: chỉ có **David, Mark, Zira (tiếng Anh)**. **Chưa có giọng tiếng Việt**, nên giọng đọc trong game hiện sẽ **im lặng**. Màn Cài đặt sẽ cảnh báo "Máy chưa có giọng tiếng Việt".

### 5.2 Các phương án

| Phương án | Chất lượng | Chi phí / điều kiện | Ai làm |
|---|---|---|---|
| A. Web Speech + giọng Windows "Microsoft An" (vi-VN) | Giọng máy, nghe robot nhưng rõ | Miễn phí. Mỗi máy streamer phải cài: Settings → Time & language → Speech → Add voices → Tiếng Việt | 🤖 code ✅ đã có · 👤 cài trên máy và nghe thử |
| B. TTS chạy local (vd. Piper, có model tiếng Việt) chạy qua TikTool hub | Tự nhiên hơn A, tùy model | Miễn phí, không cần mạng. Phải thêm endpoint `/api/tts` vào **server.js** (ngoài thư mục game) và đóng gói thêm model khoảng 60 MB | 🤖 code · 👤 nghe thử chọn giọng, kiểm tra giấy phép model |
| C. TTS đám mây trả phí (FPT.AI, Viettel AI, Google, Azure...) | Tốt nhất | Có phí theo ký tự, cần mạng và API key, độ trễ | 👤 đăng ký tài khoản, trả phí, giữ key · 🤖 code |

Đề xuất: làm A trước để chạy ngay. Nếu anh nghe thấy không đạt thì làm B. `announcer.js` đã có sẵn `httpSpeaker(url)` cho B/C.
⚠️ Tôi **chưa nghe thử** giọng nào, nên không khẳng định giọng nào hay. 👤 Anh phải nghe và chọn.

### 5.3 Bật/tắt

Công tắc "Giọng nói AI" trong Cài đặt. Tắt thì chỉ hiện chữ. Có nút "Nghe thử" và thanh âm lượng.

### 5.4 Tiếng ra live

Giọng phát từ **cửa sổ game** trên máy streamer. Bản OBS overlay (mirror) không phát tiếng, để tránh đọc 2 lần.
👤 Anh dùng TikTok LIVE Studio (Q8): thêm nguồn quay **cửa sổ game** và bật thu **âm thanh hệ thống** để giọng đọc lên live. Tôi không xem được giao diện LIVE Studio trên máy anh, nên anh cần live thử riêng tư 1 lần để chắc có hình và có tiếng.

---

## 6. Đồ họa (mọi quyết định do anh duyệt)

### 6.1 Bố cục đã dựng (dọc 1080×1920, căn giữa x = 540)

Vùng TikTok che (anh đo 10/11): **trên khoảng 1/20** (y < 96), **dưới khoảng 1/4** (y > 1440). Hai bên không bị che.

```
  y 100–247    Logo "Nga Mi" (rộng 440)
  y 252–372    Tỉ thí: đồng hồ, thanh kéo co, điểm 2 phe  |  ngoài giờ: "Đại hội tỉ thí sau 8:21"
  y 372–450    Banner thông báo / luật chơi (tối đa 2 dòng)
  y 460–835    CHƯỞNG MÔN giữa, Tả/Hữu hộ pháp hai bên (x 190 / 890)
  y 850–1426   5 hàng × 10 cột, x 40 → 1040
  y 1440–1920  để trống cho comment TikTok
```

### 6.2 Danh sách hạng mục cần anh duyệt

| Mã | Hạng mục | Lựa chọn tôi đề xuất | Ai làm |
|---|---|---|---|
| G1 | Khung hình | ✅ Dọc 1080×1920 (đã chốt), game tự co giãn theo cửa sổ | 🤖 |
| G2 | Bố cục | 🟡 Đã dựng như 6.1, chờ anh xem trong game | 🤖 |
| G3 | Bảng màu | 🟡 Đã dựng: chữ vàng kim trên nền tối trong suốt; tên đổi màu theo bộ trang phục; Thanh Liên xanh ngọc, Hồng Liên hồng | 🤖 |
| G4 | Font | 🟡 Đang dùng font hệ thống Segoe UI (đủ dấu Việt, không cần tải). Muốn font riêng thì anh chọn file font có giấy phép | 👤 chọn · 🤖 gắn |
| G5 | Hình đệ tử | ✅ **Đã chốt:** nhân vật 2D, **trang phục đổi theo cấp bậc tu vi**, tên hiện trên đầu nhân vật. **Không hiện avatar TikTok** (tránh nặng) | 👤 tạo tranh · 🤖 nhận xét kỹ thuật + ghép vào game |
| G6 | Nền | ✅ Đỉnh núi, biển mây, đại điện kiếm phái; không chùa/Phật. Spec: SPEC_TRANH.md mục 3 | 👤 (ảnh vẽ) · 🤖 làm được mây và hạt động bằng code |
| G7 | Hiệu ứng | ✅ Thăng cấp: **camera thu toàn cảnh vào người đó** (người đó ra giữa màn hình, phóng 1,8 lần, lên đại cảnh giới 2,1 lần), giữ 1,6 giây rồi lùi ra. Nhiều người thăng cấp liền nhau thì lần lượt, chỉ giữ 1 người chờ. Thanh tỉ thí và banner đứng yên. Kèm lóe sáng như cũ. Không có hào quang thường trực | 🤖 |
| G8 | UI tỉ thí | 🟡 Đã dựng: thanh kéo co, đồng hồ, điểm 2 phe, viền tên theo màu phe, màn kết quả + công đầu | 🤖 |
| G9 | Banner thông báo | 🟡 Đã dựng: dải tối trượt xuống dưới thanh tỉ thí; câu quan trọng chữ vàng, câu nhập môn chữ trắng | 🤖 |
| G10 | Nhạc nền, âm hiệu ứng | Cổ phong nhẹ | 👤 mua/thuê, có giấy phép |
| G11 | Logo / tên game | ✅ Anh đã tạo `title_NgaMi.png`. ⚠️ Nét chấm chữ "i" vẽ xiên, nhìn giống "Mí" | 👤 |

### 6.3 Việc AI không làm được về đồ họa

- ❌ **Tôi (Claude) không tạo được ảnh bitmap**: tranh nhân vật, tranh núi, sprite anime. Tôi chỉ làm được giao diện bằng HTML/CSS/SVG, hiệu ứng bằng code và hình vector đơn giản.
- Có 3 cách lấy tranh, 👤 anh chọn:
  1. **Thuê họa sĩ**: đồng bộ nhất, tốn tiền và thời gian.
  2. **Anh tự dùng công cụ AI tạo ảnh**: người phải viết prompt, chọn, sửa tay cho đồng bộ (khó giữ cùng 1 nhân vật qua nhiều tư thế), và đọc điều khoản thương mại của công cụ đó.
  3. **Mua asset pack**: kiểm tra giấy phép cho phép dùng trong live thương mại.
- Ảnh avatar TikTok tải từ CDN của TikTok, có thể hết hạn hoặc lỗi. Game phải có ảnh dự phòng (🤖).

### 6.4 Tranh đã nhận (10/11)

| File | Trạng thái |
|---|---|
| Đủ 10 bộ nhân vật (thêm hoaThan, luyenHu ngày 10/11) | ✅ Nền trong suốt thật, cùng khung 1122×1402, cùng mặt và tư thế, thu nhỏ 95 px vẫn phân biệt được bậc |
| bg_main.png | ✅ 941×1672 (đúng tỉ lệ 9:16 nhưng nhỏ hơn khung 1080×1920 khoảng 13%, trình duyệt phóng lên hơi mềm) |
| seat_chuongMon, seat_hoPhap, title_NgaMi | ✅ |

Tranh gốc để ở `src/assets/raw/`. Game dùng bản đã thu nhỏ ở `src/assets/game/` (nhân vật 384×480, khoảng 160–300 KB). Thêm hoặc sửa tranh gốc thì chạy lại:

```powershell
node Game/NgaMi_TuTien/tools/build-assets.js
```

---

## 7. Cài đặt và Thoát ✅

- Mở game → **màn Cài đặt** hiện trước: Giọng nói AI (bật/tắt), âm lượng, đọc tiểu cảnh, **Tỉ thí (bật/tắt)**, chu kỳ, thời lượng trận, Nghe thử, Xóa dữ liệu, nút **Vào game**.
- Trong game: nút **Thoát** (mờ góc phải, rõ khi rê chuột) hoặc phím **Esc** → lưu dữ liệu, hủy trận dở, quay lại màn Cài đặt.
- Cài đặt được nhớ cho lần sau. Ở màn Cài đặt, game **từ chối** mọi action (TikTool ghi log "failed").
- Tu vi giữ nguyên khi Thoát rồi vào lại **trong cùng buổi live**. Buổi live mới (session TikTool mới) bắt đầu từ 0. Nút "Xóa tu vi buổi live này" để xóa tay.
- 🟡 Nút Thoát đang nằm trong khung hình. Nếu anh quay cả cửa sổ game, người xem sẽ thấy nút này. Có thể ẩn hẳn, chỉ dùng Esc. Anh quyết.

---

## 8. Chia việc Người / AI

| Giai đoạn | Việc | 🤖 AI | 👤 Người |
|---|---|---|---|
| 0. Quyết định | Trả lời Q1–Q9 | | ✅ xong |
| 1. Logic | Tu vi, tỉ thí, thông báo, cài đặt, lưu, action TikTool | ✅ xong, 20 test | 👤 cài mapping (mục 3.3), bấm thử bằng Giả lập sự kiện |
| 2. Giọng | Cài giọng vi-VN trên máy, nghe thử, chọn A/B/C | 🤖 code B/C nếu chọn | 👤 cài, nghe, trả phí nếu chọn C |
| 3. Mock đồ họa | Mock HTML các phương án | 🤖 | 👤 duyệt |
| 4. Tranh | Nhân vật, nền, logo, nhạc | ❌ | 👤 vẽ/mua/tạo AI, kiểm tra giấy phép |
| 5. Renderer | Bố cục, chuyển động, hiệu ứng, overlay OBS vẽ từ `getState()` | 🤖 | 👤 duyệt |
| 6. Tải nặng | Script bắn 200 sự kiện/giây, đo FPS | 🤖 | 👤 chạy trên máy live thật |
| 7. Live thử | Live riêng tư hoặc ít người, xem log, chỉnh cân bằng | 🤖 đề xuất số mới từ log | 👤 lên live, quan sát người xem |
| 8. Chính sách | Đọc quy định TikTok LIVE về game tương tác đổi quà | | 👤 bắt buộc người (tôi không khẳng định nội dung chính sách hiện hành) |

**Việc chắc chắn cần người**: cài mapping quy đổi trong TikTool, duyệt đồ họa, có tranh/nhạc có giấy phép, cài và nghe giọng tiếng Việt, cấu hình LIVE Studio, live thử thật, kiểm tra chính sách TikTok, trả phí dịch vụ (nếu chọn TTS đám mây hoặc thuê họa sĩ).

---

## 9. Rủi ro và giới hạn đã biết

1. **Danh tính người chơi** là `user.id` = username TikTok (`unique_id`). Người xem đổi username giữa buổi thì thành người mới.
2. ✅ **Safe mode**: game không nhận `liveEvents`, mọi tu vi đi qua mapping, nên safe mode của TikTool **chặn được** game này. (TikTool vẫn còn chỗ gửi sự kiện thô trước bước kiểm tra safe mode, nhưng không ảnh hưởng game này.)
3. ✅ **Cửa sổ dọc**: đã sửa `desktop.js` và `public/app.js` của TikTool. Cửa sổ game mở theo `manifest.window` (1080×1920), tự thu nhỏ giữ tỉ lệ cho vừa màn hình. Game khác không khai báo `window` vẫn mở 1280×800 như cũ. ⚠️ Trên màn hình 1080p, cửa sổ chỉ cao khoảng 1000px, nên LIVE Studio quay được độ phân giải bằng cửa sổ. 👤 Anh xem chất lượng thực tế khi live thử.
4. **Adapter TikTok không chính thức**: có thể hỏng khi TikTok đổi giao thức.
5. Giới hạn mapping của TikTool (mục 3.3): 1 quà chỉ 1 mapping, combo tối đa 100.
6. Live đông (hàng trăm người lên cấp liên tục): giọng sẽ bỏ bớt câu (có chủ đích, mục 4.7).

## 10. Lộ trình

| Mốc | Nội dung | Điều kiện xong |
|---|---|---|
| M0 ✅ | Kế hoạch + logic + test + khung Cài đặt/Thoát | `node --test` chạy qua; mở game trong TikTool, Giả lập sự kiện thấy tu vi tăng |
| M1 | ✅ Q1–Q9 đã chốt. 👤 Cài giọng vi-VN, cài mapping | Nghe được "Nghe thử"; Test Lab bắn `tu_luyen` thấy tu vi tăng |
| M2 ✅ | Tranh thật (8/10 bộ) + renderer khung dọc + giả lập 100 người | Chạy mượt 200 người giả lập |
| M3 | 👤 Anh duyệt G2–G9 trong game, gửi 2 bộ còn thiếu | |
| M4 | Nhạc nền, chỉnh theo góp ý | |
| M5 | Live thử, chỉnh cân bằng | Số liệu sau 2 buổi |

---

## 11. Chạy thử ngay

```powershell
node --test Game/NgaMi_TuTien/test/game.test.js
npm run desktop
```

**Cách test (chỉ qua TikTool):**
1. `npm run desktop` → chọn **Nga Mi Tu Tiên** → kiểm tra 6 mapping mặc định (mục 3.3) → **Start Game** → **Vào game**.
2. Ở bảng mapping: gõ tên vào ô **"Người xem test"** ở đầu bảng, bấm **Test** ở dòng Tim hoặc Quà (tự tham gia + cộng tu vi). Dòng "Mọi comment" có ô nhập ngay trên dòng. Thử tỉ thí: Test Lab → `start_duel`, rồi Test dòng "1"/"2". Kết quả (game chạy hay báo lỗi kèm lý do) hiện ở thông báo.
3. Mở trực tiếp `/game/NgaMi_TuTien/` (không qua TikTool) thì game không nhận sự kiện nào.

Đã thử ngày 10/11 qua hub TikTool (session thật, sự kiện giả lập như nút Giả lập):
- (Bản cũ, trước khi bỏ lệnh comment "tham gia") tim trước khi tham gia bị từ chối.
- 40 tim thành 40 tu vi.
- Quà thành tu vi đúng số đặt trong mapping, người cao nhất lên Chưởng môn.
- Tên `不是鸿恩` hiện "Đạo hữu".
- Comment "1" ngoài giờ tỉ thí bị từ chối "Chưa tới giờ tỉ thí".

Trong TikTool: chọn **Nga Mi Tu Tiên** → cài mapping (mục 3.3) → Start Game → màn Cài đặt → Vào game → dùng **Giả lập sự kiện LIVE** bắn tim, quà, comment "1", "2". Test Lab có action `start_duel` để mở trận ngay, không phải đợi 10 phút.
