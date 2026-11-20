# Brief cho ChatGPT: Hình ảnh 2D + nội dung chữ

> Cách dùng: dán **mục 1 + mục 2** vào ChatGPT trước (để nó hiểu bối cảnh), sau đó dán từng yêu cầu ở mục 3 và 4.
> Ảnh nhận được: tải về, đổi tên đúng như bảng, chép vào thư mục ghi ở đầu mỗi bảng. Thư mục `Assets/...` nằm trong `C:\MinhAnh\tiktool\Game\DaiChienXanhDo\UnityProject\`. Chép xong thì báo người phụ trách (xem mục 3.6) để gắn vào game.

---

## 1. Bối cảnh (dán cho ChatGPT)

Tôi đang làm game 3D tương tác cho TikTok LIVE tên **"Đại Chiến Xanh Đỏ"**. Hai phe **Xanh** và **Đỏ** đấm nhau liên tục giữa một đấu trường vuông. Người xem tặng quà để vào phe và tạo hiệu ứng cho phe mình. Có 8 bậc hiệu ứng: đấm thường, đấm liên hoàn, biến hình "Siêu Chiến Binh", gọi sấm sét, xe tăng, mưa thiên thạch, robot khổng lồ. Mỗi phe có một **Lõi** (khối trụ phát sáng). Phe bị phá lõi thì thua, phe kia +1 điểm, ván mới bắt đầu ngay.

Màn hình game vuông 1080x1080, người xem chủ yếu nhìn trên điện thoại, nên mọi hình phải **đậm, rõ, đọc được khi nhỏ**.

## 2. Quy tắc phong cách (dán cho ChatGPT)

- Phong cách: **3D low-poly kiểu đồ chơi**, khối đơn giản, viền sáng, bóng đổ mềm, vui nhộn, hơi "game mobile".
- Màu: Xanh `#2D7DFF`, Đỏ `#FF3B3B`, vàng nhấn `#FFD43B`, trắng. Nền tối `#1A1F2E` khi cần nền.
- Icon: **PNG nền trong suốt**, một vật thể ở giữa, chừa lề 8%, **không có chữ**.
- **Cấm**: logo/nhân vật/trang phục có thương hiệu (Superman, DC, Marvel, Pokémon…), logo TikTok, người thật, chữ "S" trên ngực. "Siêu Chiến Binh" là nhân vật tự thiết kế: áo choàng, mặt nạ mắt, biểu tượng **tia chớp trong hình lục giác** trên ngực.
- Ảnh có chữ tiếng Việt thì chữ phải đúng chính tả, có dấu. Nếu không chắc chắn đúng dấu thì làm bản không chữ.

---

## 3. Hình ảnh cần làm

### 3.1 Icon hiệu ứng (512x512, PNG trong suốt) → `Assets/_Game/UI/Icons/`

| Tên file | Nội dung | Màu chủ đạo |
| --- | --- | --- |
| `icon_join.png` | Lá cờ cắm xuống đất, nửa xanh nửa đỏ | Xanh + đỏ |
| `icon_spawn.png` | 3 chiến binh low-poly nhỏ đứng thành nhóm | Trắng + vàng |
| `icon_like_power.png` | Trái tim phát sáng có tia năng lượng | Hồng/vàng |
| `icon_punch.png` | Nắm đấm phát sáng lao về phía trước | Vàng |
| `icon_combo_punch.png` | 3 nắm đấm chồng nhau kiểu chuyển động nhanh | Cam |
| `icon_superhero.png` | Áo choàng bay + biểu tượng tia chớp trong lục giác | Vàng kim |
| `icon_lightning.png` | Tia sét đánh xuống từ đám mây | Trắng xanh |
| `icon_tank.png` | Xe tăng low-poly nhìn nghiêng 3/4, nòng đang khai hỏa | Xám xanh |
| `icon_meteor.png` | Thiên thạch rực lửa có đuôi | Cam đỏ |
| `icon_giant_robot.png` | Robot khổng lồ low-poly, mắt phát sáng | Bạc + vàng |

**Prompt mẫu** (đổi phần in nghiêng cho từng icon):
> Tạo icon game 512x512, nền trong suốt, phong cách 3D low-poly kiểu đồ chơi, ánh sáng mềm, viền sáng nhẹ, nhìn rõ khi thu nhỏ còn 64px. Nội dung: *một nắm đấm phát sáng màu vàng #FFD43B lao về phía trước*. Không có chữ, không logo thương hiệu.

### 3.2 UI (PNG trong suốt) → `Assets/_Game/UI/Sprites/`

| Tên file | Kích thước | Nội dung |
| --- | --- | --- |
| `logo.png` | 1024x1024 | Logo chữ "ĐẠI CHIẾN XANH ĐỎ", nửa xanh nửa đỏ, hai nắm đấm chạm nhau ở giữa. Kiểm tra kỹ dấu tiếng Việt |
| `score_frame.png` | 800x160 | Khung bảng tỉ số ngang, nửa trái viền xanh, nửa phải viền đỏ, giữa có chỗ cho dấu ":", **không có chữ số** |
| `hp_frame.png` | 600x80 | Khung thanh máu bo góc, viền kim loại low-poly, bên trong trống |
| `banner_win_blue.png` | 1000x300 | Dải băng chiến thắng màu xanh, trống ở giữa để game tự viết chữ |
| `banner_win_red.png` | 1000x300 | Như trên, màu đỏ |
| `badge_x2.png` | 400x400 | Huy hiệu "X2" phát sáng vàng (chữ X2 được phép) |
| `banner_warning.png` | 1000x200 | Dải cảnh báo sọc vàng đen kiểu công trường, trống ở giữa |
| `gift_banner.png` | 900x220 | Khung thông báo quà lớn, ô tròn bên trái để đặt icon, trống phần còn lại |

### 3.3 Ảnh concept (chỉ để làm mẫu khi tìm model 3D, không đưa vào game) → `Game/DaiChienXanhDo/concept/`

| Tên file | Nội dung |
| --- | --- |
| `concept_fighters.png` | Một chiến binh phe Xanh và một phe Đỏ đứng cạnh nhau, low-poly, đầu to thân nhỏ, găng tay đấm bốc to |
| `concept_superhero.png` | Chiến binh đã biến hình Siêu Chiến Binh: áo choàng, mặt nạ mắt, biểu tượng tia chớp trong lục giác |
| `concept_arena.png` | Đấu trường vuông nhìn từ trên xuống nghiêng 55°, lõi xanh góc dưới trái, lõi đỏ góc trên phải, vạch chéo giữa sân |
| `concept_core.png` | Lõi: bệ trụ tròn + viên ngọc lớn phát sáng lơ lửng phía trên, một bản xanh một bản đỏ |
| `concept_tank_robot.png` | Xe tăng low-poly và robot khổng lồ cao gấp 7 lần người, cùng phong cách đồ chơi |

### 3.4 Ảnh quảng bá

| Tên file | Kích thước | Nội dung |
| --- | --- | --- |
| `thumbnail_live.png` | 1080x1920 | Ảnh bìa buổi live: hai phe lao vào nhau, robot khổng lồ phía sau, chữ "ĐẠI CHIẾN XANH ĐỎ" + "VÀO PHE NGAY!" |
| `thumbnail_square.png` | 1080x1080 | Bản vuông cùng nội dung |

### 3.5 Texture cho hiệu ứng 3D, mặt sân, bầu trời

Đây là loại ảnh **có tác dụng thật trong cảnh 3D**. Claude gắn chúng vào hệ thống hạt (particle) và vật liệu: một vụ nổ hay một tia sét trong Unity được tạo từ hàng trăm bản sao của một ảnh nhỏ, phóng to/thu nhỏ/xoay/mờ dần.

**Quy tắc riêng cho texture hạt (bảng A), dán kèm khi yêu cầu:**
- **Nền ĐEN tuyền `#000000`**, không dùng nền trong suốt (ảnh "trong suốt" của AI hay bị lẫn ô caro giả). Game cộng sáng nên phần đen tự biến mất.
- Hình **trắng hoặc xám** (trừ khi bảng ghi màu), để game tự tô màu Xanh/Đỏ theo phe.
- Vật thể nằm giữa ảnh, **tắt mờ dần về mép**, không chạm cạnh ảnh.
- **Không chữ, không khung, không bóng đổ, không phối cảnh**: chỉ một hình phẳng nhìn thẳng.
- Kích thước 512x512.

**A. Texture hạt** → `Assets/_Game/VFX/Textures/`

| Tên file | Nội dung | Dùng cho |
| --- | --- | --- |
| `fx_glow.png` | Một chấm sáng tròn mềm, sáng nhất ở tâm, mờ dần ra ngoài | Hào quang Siêu Chiến Binh, tim tiếp sức, lõi phát sáng |
| `fx_spark.png` | Tia lửa hình sao 4 cánh, cánh dài mảnh | Tia lửa khi đấm, khi lính chết |
| `fx_impact_ring.png` | Vòng tròn sóng xung kích, viền sáng, tâm rỗng | Cú đấm mạnh, robot giẫm, thiên thạch chạm đất |
| `fx_smoke.png` | Một cụm khói tròn bồng bềnh, xám nhạt, mép mềm | Khói xe tăng, bụi thiên thạch, thảm họa |
| `fx_fire.png` | Một ngọn lửa đơn hình giọt nước, trắng ở lõi | Đuôi thiên thạch, nòng súng xe tăng (game tô cam) |
| `fx_lightning.png` | Một tia sét dọc từ trên xuống dưới, nhiều nhánh nhỏ, mảnh, trắng xanh nhạt | Sấm sét |
| `fx_debris.png` | 6 mảnh đá vỡ nhỏ góc cạnh rải rác, xám | Mảnh vỡ khi lõi bị đánh, thảm họa |
| `fx_star.png` | Ngôi sao 5 cánh bo tròn kiểu hoạt hình, vàng `#FFD43B` (bảng này có màu) | Bị đánh choáng, ăn mừng thắng ván |
| `fx_confetti.png` | 12 mảnh giấy màu chữ nhật nhỏ rải rác, đủ màu (bảng này có màu) | Pháo giấy khi thắng ván |

**Prompt mẫu** (đổi phần in nghiêng):
> Tạo texture hiệu ứng hạt cho game, 512x512, nền đen tuyền #000000. Nội dung: *một tia lửa hình sao 4 cánh, cánh dài và mảnh, màu trắng, sáng nhất ở tâm*. Hình phẳng nhìn thẳng, mờ dần về mép, không chạm cạnh ảnh, không chữ, không khung, không bóng đổ.

**B. Texture mặt sân và lõi** → `Assets/_Game/Art/Textures/`

Phải **lát liền mạch (seamless/tileable)**: ghép 4 tấm cạnh nhau không thấy đường nối. Nhìn thẳng từ trên xuống, ánh sáng đều, **không bóng đổ** (game tự đổ bóng), không vật thể nổi bật (để lặp lại không lộ).

| Tên file | Kích thước | Nội dung |
| --- | --- | --- |
| `tex_ground_grass.png` | 1024x1024 | Cỏ low-poly kiểu đồ chơi, xanh lá dịu `#5C8A55`, hơi lốm đốm. Không quá rực để lính xanh/đỏ nổi bật |
| `tex_ground_arena.png` | 1024x1024 | Nền đấu trường: gạch đá vuông lớn màu kem xám, mạch gạch mảnh |
| `tex_core_energy.png` | 1024x1024 | Vân năng lượng dạng tổ ong lục giác phát sáng, **trắng xám trên nền đen** (game tô xanh/đỏ) |

**Prompt mẫu:**
> Tạo texture lát liền mạch (seamless tileable) 1024x1024, nhìn thẳng từ trên xuống, ánh sáng đều, không bóng đổ: *nền gạch đá vuông lớn màu kem xám kiểu đấu trường game low-poly, mạch gạch mảnh*. Bốn cạnh phải khớp nhau khi ghép lặp lại.

> Lưu ý: AI hay làm texture **chưa thật sự liền mạch**. Cứ gửi file, Claude sẽ sửa đường nối trong Unity. Không cần làm lại nhiều lần.

**C. Bầu trời / nền xa** → `Assets/_Game/Art/Textures/`

| Tên file | Kích thước | Nội dung |
| --- | --- | --- |
| `sky_panorama.png` | 4096x2048 | Ảnh toàn cảnh 360° (equirectangular): trời hoàng hôn tím cam, mây low-poly, đường chân trời có núi low-poly mờ xa. Hai mép trái phải phải nối khớp nhau |

### 3.6 Ảnh nào dùng vào đâu (tóm tắt)

| Loại | Có trong game? | Ai gắn vào |
| --- | --- | --- |
| Icon hiệu ứng, khung UI, banner, logo (3.1, 3.2) | Có, ở lớp giao diện 2D phủ trên màn hình | Antigravity (code UI) |
| Texture hạt (3.5 A) | Có, thành hiệu ứng 3D: nổ, sét, khói, tia lửa | Claude |
| Texture sân, lõi, bầu trời (3.5 B, C) | Có, phủ lên mặt sân, lõi, nền trời | Claude |
| Ảnh concept (3.3) | **Không**, chỉ để làm mẫu khi tìm model 3D | Antigravity tham khảo |
| Thumbnail (3.4) | **Không**, dùng làm ảnh bìa live, video TikTok | Bạn |

---

## 4. Nội dung chữ cần viết (dán từng yêu cầu)

### 4.1 Dải hướng dẫn
> Viết 12 câu hướng dẫn ngắn (tối đa 38 ký tự mỗi câu, tiếng Việt có dấu, giọng sôi động, có thể dùng 1 emoji) chạy ở đáy màn hình game cho người xem mới. Nội dung: comment "xanh" hoặc "đỏ" để vào phe; tặng quà để tạo hiệu ứng cho phe mình; phá lõi đối thủ để thắng; phe nào hết lính thì lõi bị đánh; sự kiện X2 xuất hiện ngẫu nhiên; sân quá đông sẽ có thảm họa. Không nhắc tên quà cụ thể. Trả về dạng danh sách JSON các chuỗi.

(Kết quả dán vào `settings.json` → `guideLines`, hoặc nhập trong menu F10 của game.)

### 4.2 Câu thông báo
> Viết mỗi loại 6 câu thông báo vui, ngắn (tối đa 45 ký tự), tiếng Việt có dấu, dùng biến `{ten}` cho tên người xem và `{phe}` cho tên phe (XANH/ĐỎ):
> 1. Người mới vào phe.
> 2. Tặng quà gọi sấm sét.
> 3. Tặng quà gọi xe tăng.
> 4. Tặng quà gọi mưa thiên thạch.
> 5. Tặng quà gọi robot khổng lồ.
> 6. Một phe thắng ván.
> 7. Cảnh báo lõi sắp vỡ (kêu gọi người xem vào cứu).
> 8. Thảm họa sắp xảy ra.
> Trả về JSON dạng `{ "join": [...], "lightning": [...], ... }`.

(Gửi kết quả cho Claude để đưa vào game.)

### 4.3 Caption và hashtag
> Viết 10 caption ngắn cho video TikTok quảng bá buổi live game "Đại Chiến Xanh Đỏ" (game đánh nhau 2 phe, người xem tặng quà để chọn phe). Mỗi caption dưới 100 ký tự, kèm 5 hashtag tiếng Việt và tiếng Anh phù hợp. Không hứa thưởng tiền hay quà thật cho người xem.

### 4.4 Kịch bản dẫn live
> Viết 20 câu nói ngắn để streamer dẫn khi chơi "Đại Chiến Xanh Đỏ": mở đầu buổi live, mời vào phe, khi một phe sắp thua, khi có robot khổng lồ, khi có thảm họa, khi kết thúc ván, cảm ơn người tặng quà. Giọng hài hước, thân thiện, không hứa thưởng tiền.

---

## 5. Không nhờ ChatGPT làm

- **Code** cho project (Claude và Antigravity làm, để không lệch kiến trúc).
- Model 3D (Antigravity tìm model CC0, Claude gắn vào game).
- Âm thanh, nhạc (dùng nguồn CC0 để chắc chắn về bản quyền).
