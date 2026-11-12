# Nga Mi Tu Tiên · Kế hoạch bản 2

> Bản 12/11/2026. **Đã làm xong toàn bộ 6 việc (12/11), 15 ảnh anh tạo đã đưa vào game.** Anh chốt: Long Châu không chia cho tông môn; sao linh thú cách A; linh thú đủ 3 sao mà nở trùng thì được vòng quay; linh thú reset theo tuần; tỉ lệ nở 45/40/15; làm cả Thiên Kiếp (từ Nguyên Anh) và Linh Khí Triều Dâng.
> Anh yêu cầu 6 việc: kiếm bay, hào quang, pháp bảo + linh thú, sáng và nét hơn, sự kiện mới, bảng tu vi.
> ✅ = anh đã chốt · 🟡 = tôi đề xuất, chờ anh chốt · 👤 = anh phải tự làm (tạo ảnh).
> Ảnh cần tạo và prompt: [PROMPT_ANH.md](PROMPT_ANH.md). Bảng tu vi: [README.md](README.md).

## Thứ tự làm đề xuất

| Bước | Việc | Cần ảnh của anh? | Ghi chú |
|---|---|---|---|
| 1 | **Sáng và nét hơn** (mục 4) | Nền mới (có thể làm sau) | Việc gấp nhất vì kênh đang bị nhắc. Phần sửa code và cách quay OBS làm được ngay |
| 2 | Hào quang tu luyện (mục 2) | Không | Code thuần |
| 3 | Kiếm bay thay luồng sáng (mục 1) | 1 ảnh kiếm | Chưa có ảnh thì dùng kiếm vẽ tạm bằng code |
| 4 | Pháp bảo (mục 3) | 10 ảnh pháp bảo | Dựng logic + hiệu ứng bằng hình tạm trước, có ảnh thì thay file |
| 5 | Trứng + linh thú (mục 3) | Trứng + 3 linh thú | |
| 6 | Thiên kiếp (mục 5) | Không bắt buộc | Sét, mây vẽ bằng code |

---

## 1. Đòn đánh: kiếm bay có vệt sáng ✅

- Mọi đòn **của đệ tử** dùng kiếm bay, thay cho luồng sáng hiện tại: tỉ thí, đánh rồng, giải cứu người tẩu hỏa. Đòn của rồng (lửa) và của người tẩu hỏa (tím) giữ nguyên.
- 👤 Anh tạo **1 ảnh kiếm** `fx_sword.png`, nằm ngang, mũi kiếm chỉ sang phải (prompt K1).
- Code làm phần còn lại:
  - Xoay kiếm theo hướng bay.
  - Bay theo đường cong nhẹ.
  - **Vệt sáng đuôi** vẽ bằng code, mờ dần sau khoảng 0,3 giây.
  - Vệt sáng và quầng sáng **tô theo màu**: phe 1 xanh ngọc, phe 2 hồng, đánh rồng vàng kim.

  Nhờ vậy chỉ cần 1 ảnh kiếm cho mọi trường hợp.
- Người ra đòn vẫn phóng to 1,5 lần trong 0,5 giây. Người trúng rung nhẹ. Số trừ tu vi nảy lên như hiện tại.
- Nhẹ máy: tối đa khoảng 30 thanh kiếm bay cùng lúc. Nhiều hơn thì bỏ bớt hiệu ứng (tu vi vẫn tính đủ).

## 2. Hào quang tu luyện ✅

- Ai **vừa được cộng tu vi** (tim, quà, pháp bảo) thì có **hào quang sau lưng**:
  - quầng sáng mềm theo màu chủ đạo của cảnh giới (bảng màu bên dưới);
  - phồng nhẹ và nhấp nháy chậm khoảng 2,5 giây một nhịp.
- **1 phút 30 giây** không được cộng tu vi thì hào quang tắt dần.
- **2 phút** không tương tác thì mờ đi rồi rời chỗ (giữ luật hiện tại, tu vi vẫn còn).
  - 🟡 "Tương tác" ở đây tôi giữ như hiện tại: tim, quà hoặc comment. Anh muốn chỉ tính tu luyện (comment không giữ chỗ) thì báo tôi.
- Không cần ảnh. Code vẽ bằng một lớp gradient, chỉ đổi độ sáng và kích thước, nên 53 người cùng có hào quang vẫn nhẹ.

| Cảnh giới | Màu hào quang (theo màu trang phục) |
|---|---|
| Luyện Khí | trắng ngà |
| Trúc Cơ | xanh lá nhạt |
| Kết Đan | xanh ngọc |
| Nguyên Anh | lam |
| Hóa Thần | tím nhạt |
| Luyện Hư | trắng bạc ánh xanh |
| Hợp Thể | vàng nhạt |
| Đại Thừa | đỏ son + vàng kim |
| Độ Kiếp | tím điện |
| Chân Tiên | vàng kim rực, to hơn hẳn |

Lưu ý: bản 1 đã chốt "không có hào quang chạy liên tục" để máy nhẹ. Giờ anh đổi ý nên tôi làm theo, và vẫn giữ máy nhẹ như trên.

## 3. Pháp bảo (tặng quà = mua pháp bảo = tăng tu vi + hiệu ứng) ✅ 🟡

### 3.1 Nguyên tắc

- Mỗi pháp bảo là **1 hành động riêng** trong TikTool (giống "Tu luyện"), có ô **Tu vi**.
  - Quà nào ra pháp bảo nào, cộng bao nhiêu tu vi: anh đặt bằng mapping trong TikTool, như luật Q4 đã chốt.
  - Game chỉ lo hiệu ứng.
- Hành động "Tu luyện" hiện tại giữ nguyên cho tim và quà thường.
- Pháp bảo **cộng tu vi y như tu luyện**: có tính điểm tỉ thí, sát thương rồng, giải cứu tẩu hỏa, ngưỡng tẩu hỏa.
- Hiệu ứng chạy **tại chỗ người tặng**:
  - Hiện tên pháp bảo trên banner, ví dụ "Hưng Phạm dùng Bàn Đào! +5.000 tu vi".
  - Pháp bảo đắt (từ số 6) có giọng đọc.

### 3.2 Mười pháp bảo 🟡

Tên lấy theo vật phẩm quen thuộc trong truyện tu tiên. Đã tránh đồ của Phật giáo, ví dụ không dùng Tuyết Liên vì là hoa sen. Xếp từ rẻ đến đắt, gợi ý mức quà để anh đặt mapping cho hợp.

| # | Pháp bảo | Mã hành động | Hiệu ứng trên màn | Ảnh | Gợi ý quà |
|---|---|---|---|---|---|
| 1 | **Linh Thạch** (đá linh khí) | `linh_thach` | Vài viên đá xanh rơi xuống tay, vỡ thành đốm sáng bay vào người | P1 | quà 1–5 xu |
| 2 | **Tụ Khí Đan** (đan dược) | `tu_khi_dan` | Viên đan bay lên trước mặt, xoay rồi tan thành vòng sáng bao quanh người | P2 | 5–20 xu |
| 3 | **Chu Quả** (quả đỏ linh) | `chu_qua` | Quả đỏ mọc ra từ cành, rơi xuống, người sáng đỏ ấm | P3 | 20–50 xu |
| 4 | **Bàn Đào** (đào tiên) | `ban_dao` | Quả đào rơi từ trên xuống, chạm người thì bừng sáng hồng, cánh hoa đào bay | P4 | 50–100 xu |
| 5 | **Thiên Niên Linh Chi** | `linh_chi` | Nấm linh chi mọc lên trước người, tỏa khói sáng xanh rồi tan vào | P5 | 100–300 xu |
| 6 | **Ngọc Dịch Quỳnh Tương** (bình ngọc tiên tửu) | `ngoc_dich` | Bình ngọc nghiêng rót dòng nước sáng vào người, gợn sóng sáng | P6 | 300–500 xu |
| 7 | **Bí Tịch Công Pháp** (cuộn bí kíp) | `bi_tich` | Cuộn trục bay tới, mở ra, chữ vàng bay vào người (chữ do code vẽ) | P7 | 500–1.000 xu |
| 8 | **Phi Kiếm Trận** (kiếm tiên) | `phi_kiem` | 8 thanh kiếm bay vòng tròn quanh người rồi hợp lại thành cột sáng (dùng lại ảnh kiếm K1) | P8 | 1.000–3.000 xu |
| 9 | **Long Châu** (ngọc rồng) | `long_chau` | Viên ngọc lớn hạ xuống trên đầu, xoay, tỏa vầng sáng lớn (✅ không chia tu vi cho tông môn) | P9 | 3.000+ xu |
| 10 | **Linh Thú Đản** (trứng linh thú) | `linh_thu_dan` | Trứng rơi xuống cạnh người, rung, nứt rồi **nở ra linh thú** (mục 3.3) | E1, E2 | 🟡 99–299 xu |

- ✅ 10 món chỉ khác nhau ở hiệu ứng (Long Châu không chia tu vi, anh chốt 12/11).
- Chưa có ảnh món nào thì game hiện **hình tạm** (vòng tròn màu + tên món), vẫn chạy đủ logic.

### 3.3 Linh thú (trứng nở ra pet) ✅ 🟡

**Ba linh thú** (tên 🟡):

| Linh thú | Hệ | Tác dụng 1 sao | Tỉ lệ nở 🟡 |
|---|---|---|---|
| **Ngọc Thố** (thỏ ngọc) | Tu luyện | +5% tu vi mỗi lần được cộng | 45% |
| **Hỏa Kỳ Lân** (kỳ lân lửa nhỏ) | Chiến đấu | +5% sát thương | 40% |
| **Kim Phượng** (phượng hoàng vàng nhỏ) | Cả hai | +5% tu vi và +5% sát thương | 15% (hiếm) |

- **"Sát thương"** gồm:
  - sát thương lên rồng;
  - sát thương giải cứu người tẩu hỏa;
  - số tu vi đối thủ mất khi trúng kiếm trong tỉ thí.

  Điểm phe tỉ thí vẫn tính bằng tu vi đã góp, nên Ngọc Thố gián tiếp giúp phe.
- **"+5% tu vi"** áp lên mọi lần được cộng tu vi, kể cả tim, quà và pháp bảo.
- Mỗi người có **tối đa 3 linh thú, mỗi loại 1 con**. Nở trùng loại thì **hợp thể lên sao**.
- Linh thú hiện **nhỏ cạnh chân** nhân vật, kèm số sao. Ai to (hàng trên) thì linh thú cũng to theo.

**Gợi ý cơ chế sao** (anh hỏi 2 con hợp thể nên tăng thế nào):

| Cách | 1★ | 2★ | 3★ | Ưu / nhược |
|---|---|---|---|---|
| **A. Cộng đều (tôi khuyên)** | 5% | 10% | 15% (tối đa) | Dễ hiểu, dễ đọc trên live: "Kỳ Lân 2 sao, thêm 10% sát thương". Mỗi lần nở trùng +1 sao |
| B. Nhân đôi | 5% | 10% | 20% | Sao 3 rất mạnh, kích thích mua trứng, nhưng chênh lệch lớn với người mới |
| C. Giảm dần | 5% | 8% | 10% | Công bằng nhất, nhưng "2 con mới được 8%" người xem dễ thấy thiệt |

Chi tiết cho cách A:
- 2★ cần nở trùng 1 lần (2 con), 3★ cần 3 con.
- ✅ Đủ 3★ mà còn nở trùng thì được **1 lượt vòng quay** cộng tu vi ngẫu nhiên (anh chốt 12/11).
- Mạnh nhất có thể: Ngọc Thố 3★ + Kim Phượng 3★ = **+30% tu vi**; Kỳ Lân 3★ + Kim Phượng 3★ = **+30% sát thương**.

✅ **Linh thú reset theo tuần** cùng tu vi (anh chốt 12/11). Lưu trong SQLite của hub, cột `extra` của bảng `game_player_week`, sao lưu cùng `tiktool.db`.

### 3.4 Đổi tên hành động (function) trong TikTool

Thêm 10 hành động pháp bảo, mỗi cái 1 ô "Tu vi". Các hành động cũ giữ nguyên mã để mapping hiện có không hỏng: `tu_luyen`, `binh_luan`, `vao_phe`, `start_duel`, `end_duel`. Danh sách đầy đủ sẽ ghi vào `docs/GAME_EVENT_API.md`.

## 4. Sáng hơn, nét hơn ✅

Tôi không biết chính xác TikTok nhắc kênh vì lý do gì, vì họ không công bố tiêu chí. Nhưng hình tối và mờ thì nên sửa dù sao đi nữa. Có **3 nguyên nhân làm mờ** tìm được trong code và cách quay:

| Nguyên nhân | Sửa |
|---|---|
| **Cửa sổ game bị thu nhỏ cho vừa màn hình** (màn 1080p chỉ còn khoảng 580×1030), OBS quay cửa sổ nhỏ rồi phóng lên 1080×1920 nên vỡ hình | 👤 Trong OBS / LIVE Studio dùng **Browser Source** với link overlay của TikTool, khổ **1080×1920**. Overlay vẽ đúng độ phân giải gốc, không phụ thuộc màn hình. Tôi sẽ ghi rõ các bước vào hướng dẫn, và để TikTool hiện sẵn link overlay cho dễ copy |
| **Ảnh nền gốc chỉ 941×1672**, bị phóng lên 1080×1920 | 👤 Tạo nền mới (prompt N1), tôi xử lý thành đúng 1080×1920 |
| **Nền là cảnh đêm**, cộng nhiều lớp tối: bảng tỉ thí nền tím than, bóng đen dưới nhân vật, nền mở rộng giảm sáng 25% | Code: nền ban ngày sáng, bảng chữ chuyển sang nền sáng trong mờ chữ đậm, giảm bóng đen, viền tên rõ hơn |

Giọng đọc vẫn phát từ cửa sổ game như cũ, LIVE Studio bắt tiếng máy như bình thường.

## 5. Sự kiện mới: Thiên Kiếp và gợi ý khác 🟡

### 5.1 Thiên Kiếp (lôi kiếp) khi đột phá đại cảnh giới (tôi khuyên làm cái này)

Trong truyện tu tiên, lên cảnh giới lớn phải **vượt lôi kiếp**. Biến nó thành sự kiện tương tác ngắn:

1. **Khi nào:** một người **lên đại cảnh giới từ Nguyên Anh trở lên** (Nguyên Anh, Hóa Thần, Luyện Hư, Hợp Thể, Đại Thừa, Độ Kiếp, Chân Tiên). Lên các bậc nhỏ vẫn chỉ có hiệu ứng thăng cấp như hiện tại.
2. **Diễn biến (khoảng 15 giây):**
   - Camera thu vào người đó. Mây giông tụ trên đầu, chỉ vùng quanh người đó, **không làm tối cả màn hình**.
   - Banner hiện: "Lôi kiếp giáng xuống X! Đồng môn thả tim hộ pháp!".
   - **Số đạo sét theo cảnh giới:**

     | Cảnh giới | Số đạo sét |
     |---|---|
     | Nguyên Anh | 3 |
     | Hóa Thần, Luyện Hư | 5 |
     | Hợp Thể, Đại Thừa | 7 |
     | Độ Kiếp, Chân Tiên | 9 |

     Mỗi đạo sét giáng xuống: nhân vật rung, lóe trắng.
   - Trong 15 giây đó, **tu vi người khác góp được tính là "hộ pháp"**: thanh "kết giới" đầy dần. Mỗi đạo sét đánh vào kết giới trước.
3. **Kết quả:**
   - Kết giới đủ: "Độ kiếp thành công". Người đó được **+5% tu vi** đang có. Hào quang vàng bừng lên, tên đổi màu.
   - Người góp hộ pháp nhiều nhất được +1.000 tu vi.
   - Kết giới không đủ: vẫn lên cảnh giới (**không phạt**, tránh tiêu cực trên live), chỉ không có thưởng.
4. **Va chạm với sự kiện khác:**
   - Đang tỉ thí, đánh rồng hoặc tẩu hỏa thì thiên kiếp **xếp hàng chờ**.
   - Nhiều người cùng đột phá thì làm lần lượt. Hàng chờ tối đa 2 người; ai thêm vào nữa thì chỉ có hiệu ứng thăng cấp thường.
5. **Hình:** sét, mây, kết giới đều vẽ bằng code, **không cần ảnh**. Có thêm 1 ảnh mây giông (prompt T1) thì đẹp hơn, nhưng không bắt buộc.

### 5.2 Gợi ý thêm, làm sau nếu anh thích

| Sự kiện | Cách chơi | Vì sao hợp |
|---|---|---|
| **Linh Khí Triều Dâng** | Ngẫu nhiên khoảng 20 phút 1 lần, **60 giây nhân đôi tu vi** cho mọi người, màn hình có sóng linh khí nhẹ | Tạo cao trào tặng quà ngắn, dễ hiểu |
| **Bí Cảnh Mở Cửa** | Comment "vào" trong 30 giây, hết giờ chọn ngẫu nhiên 3 người nhận rương (tu vi hoặc 1 pháp bảo nhỏ) | Kéo comment, giúp người không tặng quà cũng có phần |
| **Chưởng Môn Truyền Công** | Khi Chưởng môn (top 1) tặng quà lớn, 10% tu vi đó chia đều cho đệ tử đang ngồi | Thưởng cho người top, cả phòng cùng vui |

Tôi khuyên làm **Thiên Kiếp trước**, đúng ý anh "lên cấp cao phải có hiệu ứng đẹp". Rồi đến **Linh Khí Triều Dâng** vì dễ làm và kích thích quà.

## 6. Bảng tu vi lên cấp ✅

Đã viết vào [README.md](README.md) (46 bậc, kèm số tim và số Rose theo mapping gợi ý).

---

## Câu hỏi đã chốt (12/11)

1. Danh sách 10 pháp bảo (3.2): giữ tên như trên hay đổi? Long Châu có chia 1% cho cả tông môn không?
2. Cơ chế sao linh thú: A (5/10/15%), B hay C?
3. Linh thú giữ vĩnh viễn hay reset theo tuần?
4. Tỉ lệ nở 45 / 40 / 15 có ổn không?
5. Thiên Kiếp như 5.1: có làm không? Bắt đầu từ Nguyên Anh có hợp lý không?
