# Nga Mi Tu Tiên: spec tranh (nhân vật + phong cảnh)

> Bản 10/11/2026. Dùng cho người vẽ hoặc người tạo tranh bằng AI. Phần đã chốt với anh ghi ✅, phần tôi đề xuất ghi 🟡 (anh đổi được).

## 0. Đã chốt

- ✅ Nhân vật **2D, chỉ nữ**. **Không** dùng avatar TikTok.
- ✅ **10 bộ trang phục**, mỗi đại cảnh giới 1 bộ. Phần "sang" của bậc cao **vẽ luôn vào tranh** (lụa bay, ánh sáng tĩnh). Code **không** chạy hào quang riêng cho từng nhân vật, để 100 người chơi vẫn nhẹ.
- ✅ **Tên người chơi do code vẽ** trên đầu nhân vật. Tranh **không chứa chữ**.
- ✅ **Tránh phong cách chùa/Phật.** Nga Mi ở đây là **kiếm phái tu tiên trên núi**, không phải nơi tu hành tôn giáo.

## 1. Cấm dùng (tránh đụng chạm tôn giáo và bản quyền)

| Không dùng | Thay bằng |
|---|---|
| Chùa, tháp, mái chùa cong kiểu điện Phật, tượng Phật/Bồ Tát, voi trắng | Đại điện kiếm phái, cổng sơn môn bằng đá, đình nghỉ, cầu treo |
| Hoa sen, đài sen, chữ 卍, tràng hạt, mõ, chuông chùa, hương khói | Mây, núi, thác nước, tùng bách, hoa mai, hạc |
| Áo cà sa, đầu cạo, khăn trùm ni cô, tay bắt ấn (mudra) | Tóc dài búi/buộc, áo đạo bào/váy cổ trang; tay để trên gối hoặc ôm kiếm |
| Vòng sáng tròn sau đầu (halo) | Lụa bay, kiếm bay lơ lửng, đốm sáng li ti |
| Nhân vật/tình tiết Kim Dung (Diệt Tuyệt, Chu Chỉ Nhược, Ỷ Thiên kiếm…) | Nhân vật tự thiết kế |
| Tranh, nhân vật, UI của game khác (kể cả "Đỉnh Linh Sơn", "Tu chân giới online" trong ảnh mẫu) | Ảnh mẫu chỉ dùng tham khảo **bố cục**. ⚠️ Không đưa ảnh mẫu vào công cụ AI làm ảnh gốc (img2img) |

Chữ trên biển hiệu (nếu có) chỉ ghi **"Nga Mi"** hoặc **"峨眉"**. Không ghi tên chùa.

## 2. Nhân vật đệ tử (10 file)

### 2.1 Quy cách chung (bắt buộc cho cả 10 file)

| Mục | Yêu cầu |
|---|---|
| Kích thước | **512 × 640 px** (tỉ lệ 4:5), mọi file **cùng kích thước** |
| Định dạng | **PNG 32-bit, nền trong suốt**, sRGB, mỗi file ≤ 400 KB |
| Tư thế | **Ngồi tĩnh tọa, nhìn thẳng**, khoanh chân trên đệm vải tròn hoặc tảng đá phẳng; hai tay đặt trên gối **hoặc** ôm kiếm ngang đùi. 10 bậc **cùng một tư thế** |
| Vị trí trong khung | Căn giữa ngang. Đáy đệm/đá chạm cách mép dưới khoảng 20px. **Đỉnh đầu (kể cả trâm, mũ) không vượt quá 80px tính từ mép trên**, để code đặt tên vào khoảng trống đó |
| Tỉ lệ người | 🟡 **Chibi khoảng 1:3** (đầu to). Hàng nhỏ nhất trên màn chỉ cao khoảng 95px, nên người tỉ lệ thật sẽ không nhìn rõ mặt |
| Khuôn mặt | **Cùng 1 gương mặt, kiểu tóc gốc, màu da** cho cả 10 bậc. Chỉ đổi trang phục, phụ kiện tóc, vũ khí |
| Nét vẽ | Viền rõ, mảng màu lớn, ít chi tiết nhỏ. Chi tiết dưới 4px sẽ mất khi thu nhỏ |
| Ánh sáng | Chiếu từ trên xuống, nhẹ. Không đổ bóng ra nền (code tự thêm bóng dưới đệm) |
| Không có | Chữ, khung, nền, viền phát sáng quá 24px ra ngoài thân |

Kích thước hiển thị dự kiến trên khung 1080×1920 (để anh hình dung độ chi tiết):

| Hàng | Số ghế | Cao trên màn |
|---|---|---|
| Chưởng môn (#1) | 1 | khoảng 300 px |
| Tả/Hữu hộ pháp (#2–3) | 2 | khoảng 220 px |
| Trưởng lão (#4–9) | 6 | khoảng 150 px |
| Chân truyền (#10–21) | 12 | khoảng 120 px |
| Nội môn (#22–45) | 24 | khoảng 95 px |

### 2.2 Mười bộ trang phục 🟡

Nguyên tắc: **mỗi bậc khác bậc trước ở cả màu chủ đạo lẫn hình dáng** (tay áo, phụ kiện đầu, vật sau lưng). Thu nhỏ còn 95px vẫn phải nhận ra bậc nhờ màu và hình khối, không cần nhìn chi tiết.

| # | File | Cảnh giới | Màu chủ đạo | Hình dáng / phụ kiện |
|---|---|---|---|---|
| 1 | `char_01_luyen_khi.png` | Luyện Khí | Trắng ngà, xám nhạt | Áo vải đơn giản tay hẹp, tóc buộc đuôi ngựa bằng dây vải. Không phụ kiện |
| 2 | `char_02_truc_co.png` | Trúc Cơ | Xanh lá nhạt | Đai lưng vải, trâm gỗ, túi nhỏ bên hông |
| 3 | `char_03_ket_dan.png` | Kết Đan | Xanh ngọc (màu đặc trưng Nga Mi) | Áo 2 lớp viền bạc, trâm bạc, tay áo rộng hơn |
| 4 | `char_04_nguyen_anh.png` | Nguyên Anh | Lam đậm | Áo choàng mỏng ngoài, **kiếm đeo sau lưng** (chuôi nhô qua vai) |
| 5 | `char_05_hoa_than.png` | Hóa Thần | Tím nhạt | Tay áo rất rộng, búi tóc cao cài trâm ngọc, dải lụa ngắn trên vai |
| 6 | `char_06_luyen_hu.png` | Luyện Hư | Trắng bạc ánh xanh | Váy nhiều lớp mỏng, **dải lụa dài bay** hai bên, vài đốm sáng xanh vẽ sẵn |
| 7 | `char_07_hop_the.png` | Hợp Thể | Vàng nhạt, viền vàng kim | Mão nhỏ trên búi tóc, hoa văn mây vàng trên áo, lụa bay |
| 8 | `char_08_dai_thua.png` | Đại Thừa | Đỏ son + vàng kim | Giáp vai nhẹ bằng kim loại, áo choàng đỏ dài, **2 thanh kiếm nhỏ lơ lửng** sau lưng |
| 9 | `char_09_do_kiep.png` | Độ Kiếp | Đen tím, viền tím điện | Lụa bay dạng tia, vài tia sét nhỏ **vẽ sẵn** quanh vạt áo, tóc xõa một phần |
| 10 | `char_10_chan_tien.png` | Chân Tiên | Trắng tinh + vàng kim | Trang phục lộng lẫy nhất, lụa dài bay cao, **vòng kiếm bay** xếp hình quạt sau lưng (không phải vòng sáng tròn) |

Tham khảo vũ khí: Nga Mi nổi tiếng với **kiếm** và **Nga Mi thích** (cặp dùi ngắn cầm tay). Có thể cho bậc 5 trở lên cầm Nga Mi thích. Đây là vũ khí võ thuật, không mang tính tôn giáo.

### 2.3 Gợi ý nếu tạo bằng AI ảnh

- Làm **bậc 1 trước**, chốt mặt và tư thế, rồi dùng chính ảnh đó làm ảnh tham chiếu cho 9 bậc sau. Đây là cách duy nhất để giữ cùng một khuôn mặt.
- Xuất ảnh lớn rồi **tự tách nền** và **căn lại vào khung 512×640** theo mục 2.1. AI thường không đặt đúng vị trí.
- Đọc điều khoản công cụ AI về **dùng thương mại** (live có nhận quà là có thu nhập).

## 3. Phong cảnh và đạo cụ

| File | Kích thước | Định dạng | Nội dung |
|---|---|---|---|
| `bg_main.webp` (hoặc .jpg) | **1080 × 1920** | WebP/JPG, ≤ 1,5 MB | Đỉnh núi Nga Mi ban đêm/hoàng hôn, biển mây phía dưới, dãy núi xa, đại điện kiếm phái **nhỏ ở phía xa** (không chiếm trung tâm). **Giữa khung để trống, tối và ít chi tiết** vì nhân vật và tên sẽ phủ lên |
| `seat_chuong_mon.png` | 640 × 360 | PNG trong suốt | Bệ ngồi của Chưởng môn: phiến đá ngọc hoặc đài mây nhiều tầng, **không phải đài sen** |
| `seat_ho_phap.png` | 400 × 240 | PNG trong suốt | Bệ ngồi Hộ pháp (1 file, code tự lật cho bên phải) |
| `terrace.png` (🟡 tùy chọn) | 1080 × 1100 | PNG trong suốt | Các bậc thềm đá xếp tầng, chỗ đệ tử ngồi theo hàng |
| `logo_ngami.png` (🟡 tùy chọn) | 800 × 240 | PNG trong suốt | Chữ "Nga Mi" thư pháp/kiếm khí. Dấu tiếng Việt phải đúng |

🟡 Tách lớp (bầu trời / núi xa / núi gần) **không bắt buộc**. Có thì code làm mây trôi và chuyển động nhẹ tự nhiên hơn.

### 3.1 Vùng TikTok đè lên (ước lượng từ 2 ảnh anh gửi, cần anh kiểm tra lại)

Trên khung 1080×1920, các vùng sau **không đặt chi tiết quan trọng**:

| Vùng | Ước lượng | Bị che bởi |
|---|---|---|
| Trên cùng | khoảng 0–260 px | Tên kênh, số người xem, thanh tìm kiếm |
| Mép phải | khoảng 940–1080 px, từ y 950 đến y 1750 | Nút like, comment, share, quà |
| Dưới cùng | khoảng 1450–1920 px | Comment chạy, ô nhập comment |

👤 Đây là số tôi ước từ ảnh chụp điện thoại, **chưa đo trên live thật**. Anh chụp màn hình 1 buổi live thử ở LIVE Studio để chốt.

## 4. Nộp và nhận xét

1. Anh đặt file vào `Game/NgaMi_TuTien/assets/raw/` và báo tôi.
2. Tôi kiểm tra và trả lời từng file theo checklist:
   - [ ] Đúng kích thước, nền trong suốt, dung lượng
   - [ ] Đỉnh đầu không vượt 80px, đáy đệm đúng vị trí, căn giữa
   - [ ] 10 bậc cùng mặt, cùng tư thế
   - [ ] Thu nhỏ còn 95px vẫn phân biệt được 10 bậc (tôi xếp 10 hình cạnh nhau để anh xem)
   - [ ] Không có yếu tố ở mục 1
   - [ ] Nền: giữa khung đủ trống, không có chi tiết quan trọng trong vùng TikTok che
3. **Đẹp hay không, có đúng chất anh muốn không: anh quyết.** Tôi chỉ nhận xét kỹ thuật và mức độ phù hợp.

Trong lúc chờ tranh, tôi có thể dựng phần vẽ game bằng hình tạm cùng kích thước 512×640. Có tranh thật thì thay file, không phải sửa code.

## 5. Quái rồng (sự kiện "Tông môn đánh quái", cứ 30 phút một lần)

### 5.1 Vị trí trên màn hình (đề xuất, khung 1080×1920)

```
  y 0–96       (TikTok che)
  y 100–740    RỒNG (dáng cuộn dọc, khoảng 640 × 640): đè lên chỗ logo "Nga Mi" (logo ẩn trong lúc đánh quái)
  y 740–800    Thanh máu rồng + dòng "Đánh quái: thả tim / tặng quà để gây sát thương, +… tu vi"
  y 820–1500   Cả tông môn LÙI XUỐNG khoảng 250 px so với bình thường để cách rồng một đoạn (hàng cuối lấn vào vùng comment: chờ anh chốt)
  y 1440–1920  (comment TikTok che)
```

### 5.2 File cần tạo

| File | Bắt buộc | Nội dung |
|---|---|---|
| `boss_dragon.png` | ✅ | Rồng ở tư thế chính (đang bay, nhìn xuống tông môn) |
| `boss_dragon_hurt.png` | Tùy chọn | Cùng tư thế, bị trúng đòn (nhăn mặt, vảy lóe sáng). Không có thì code tự làm hiệu ứng rung + lóe trắng |
| `boss_dragon_defeated.png` | Tùy chọn | Rồng gục, mắt nhắm, rơi xuống. Không có thì code tự làm mờ dần + rơi |

Quy cách chung (giống tranh nhân vật):
- **1200 × 1200 px**, vuông (dáng cuộn dọc, anh chốt 10/11). Game sẽ hiện ở khoảng 640 × 640 px.
- **PNG nền trong suốt thật.** ⚠ Nhiều công cụ AI vẽ ô bàn cờ giả. Nếu bị giả thì tạo lại trên **nền xanh lá đặc #00FF00** rồi tách nền (xem 5.3).
- Toàn bộ thân, đuôi, sừng, râu **nằm gọn trong khung**, chừa khoảng 40 px quanh mép. Dáng chính diện: đầu ở **giữa phía trên**, thân cuộn chữ S dọc phía dưới.
- Cùng phong cách với 10 bộ nhân vật: anime chibi mềm, viền rõ, mảng màu lớn, trang trí vàng kim. **Rồng màu trắng và vàng kim** (anh chốt 10/11).
- **Không** dùng: tượng Phật, hoa sen, chữ 卍, hào quang tròn sau đầu (mục 1). **Không** bắt chước rồng hay quái vật của game hoặc phim nào (kể cả Cửu Vĩ Yêu Hồ trong ảnh mẫu).

### 5.3 Prompt tạo ảnh (tiếng Anh cho công cụ AI ảnh), bản 3: dáng chính diện cuộn dọc, trắng vàng kim, nền trong suốt

Anh chốt 10/11:
- Bản 2 (rồng nằm ngang, bò dài) xấu.
- Dáng cần giống ảnh rồng xanh anh gửi: **nhìn thẳng, đầu ở giữa phía trên, thân cuộn chữ S theo chiều dọc ngay trước mặt, đối xứng, gọn**.
- Màu trắng vàng kim, nền trong suốt.

**Quy cách đổi theo dáng mới:** dáng cuộn dọc gần như vuông, nên ảnh làm **1200 × 1200 px (vuông 1:1)**. Trong game, rồng hiện khoảng 640 × 640 px ở vùng y 100–740. Trong lúc đánh quái, tông môn lùi xuống nhiều hơn (xem 5.1).

**Ảnh chính `boss_dragon.png`:**

```
A majestic Eastern (Chinese-style) dragon boss for a cute xianxia cultivation game, white and gold,
front view, perfectly symmetrical composition.
Pose: the head is at the top center facing the viewer directly, fierce but charismatic expression, mouth open
holding a glowing golden pearl inside the mouth, golden antler horns spreading up and outward, a large fluffy mane
of white and pale-gold fur around the head, two very long golden whiskers curling outward to the left and right
in big elegant S-shaped spirals.
Below the head, ONE single continuous serpentine body descends vertically in a compact stacked S-shaped coil right
in front of the viewer, flowing unbroken from the neck to the tail tip, every part of the body visible and connected;
the tail curls at the bottom and ends in a flame-like golden fur tuft that is clearly attached to the body.
Two front legs raised at the left and right of the upper coil with sharp golden claws, two rear legs at the lower
left and right of the coil. Pearl-white scales with shimmering gold edges, cream-gold segmented underbelly,
golden ornamental fins along the back, small golden auspicious swirl clouds at the left and right sides
not covering the body.
Style: soft anime game illustration, clean bold outlines, large flat color areas, gentle cel shading,
gold ornamental accents, slightly cute rounded shapes, matching a chibi anime xianxia game.
Whole dragon including whiskers and tail fully inside the frame with margin on all sides, centered.
Isolated on a fully transparent background, PNG with alpha channel, no background, no shadow.
Square 1:1, high resolution.
```

**Negative prompt:**

```
checkerboard background, fake transparency pattern, white background, solid background, scenery, sky, mountains,
side view, horizontal body, crawling pose, lying down, asymmetrical,
disconnected tail, separated tail, floating tail, broken body, body split into pieces, gap in the body,
body hidden behind clouds, tangled loops, multiple tails, extra heads, extra limbs, cropped whiskers, cut off body,
text, letters, watermark, logo, signature, temple, pagoda, buddha, lotus, swastika, halo, human characters,
blurry, photorealistic, 3d render, gore, blood
```

**Ảnh bị trúng đòn `boss_dragon_hurt.png`** (dùng ảnh chính làm ảnh tham chiếu):

```
Same white and gold dragon, same front-view coiled pose, same framing and colors as the reference image, one
continuous body with the tail attached. The dragon is hit: eyes squeezed shut, head recoiling slightly,
white-gold impact sparks bursting on its chest, scales flashing bright. Isolated on a fully transparent background,
PNG with alpha, square 1:1.
```

**Ảnh gục ngã `boss_dragon_defeated.png`:**

```
Same white and gold dragon, same art style and colors as the reference image, one continuous body with the tail
attached. Defeated: eyes closed with dizzy swirls, head drooping down, coil loosened and sagging, whiskers limp,
golden pearl dimmed and cracked falling from its mouth. Cute, not gory. Isolated on a fully transparent background,
PNG with alpha, square 1:1.
```

**Về nền trong suốt (quan trọng):**
- Có công cụ AI xuất được PNG trong suốt thật, nhưng **nhiều công cụ chỉ vẽ ô bàn cờ giả**, như ảnh Luyện Khí lần trước. Tạo xong cứ gửi tôi, tôi kiểm tra từng pixel và báo ngay là thật hay giả.
- Nếu ra giả: thay dòng `Isolated on a fully transparent background, PNG with alpha channel, no background` bằng `Plain solid pure green background (#00FF00)`, rồi tách nền bằng remove.bg hoặc Photoshop. Nền xanh lá dễ tách hơn hẳn, vì rồng trắng vàng không có màu xanh.
- Đừng tạo trên nền trắng: rồng màu trắng, tách nền sẽ ăn mất lông bờm và râu.

**Về ảnh rồng xanh mẫu:** nếu ảnh đó lấy từ Internet chứ không phải anh tự tạo, **đừng đưa nó vào công cụ AI làm ảnh tham chiếu** (img2img), vì dễ ra bản sao gần giống, vướng bản quyền. Prompt trên đã tả lại dáng bằng chữ, đủ để AI vẽ dáng tương tự.

Mẹo:
- Tạo ảnh chính trước, chọn bản ưng, rồi **dùng ảnh đó làm ảnh tham chiếu** (image reference hoặc img2img) cho 2 ảnh còn lại, để giữ đúng một con rồng.
- Đọc điều khoản dùng thương mại của công cụ AI ảnh.
- Tạo xong thì bỏ file vào `src/assets/raw/` và báo tôi kiểm tra như mục 4.
