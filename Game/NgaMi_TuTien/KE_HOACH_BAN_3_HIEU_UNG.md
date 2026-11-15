# Nga Mi Tu Tiên · Bản 3: hào quang, bảng tên, hiệu ứng pháp bảo

> Bản hoàn chỉnh 13/11/2026, **đã chốt** với anh. Gồm 2 phần:
> - **Phần A: dành cho ChatGPT tạo ảnh.** Đọc từ đầu tới hết phần A là đủ để tạo và lưu toàn bộ 37 ảnh.
> - **Phần B: thiết kế chuyển động.** Claude dùng để làm code sau khi có ảnh. ChatGPT không cần làm gì ở phần B.

**Đã chốt (13/11):**
1. Bảng tên đặt **dưới chân** nhân vật (như ảnh mẫu anh gửi).
2. Khung tên theo **10 đại cảnh giới**, cấp nhỏ phân biệt bằng code, **thêm khung riêng cho Đại viên mãn** (8 cảnh giới có Đại viên mãn).
3. Bí Tịch giữ **8 chữ Hán** bay quanh người như hiện tại (chữ do code vẽ, không nằm trong ảnh).

---

# PHẦN A · HƯỚNG DẪN TẠO ẢNH (cho ChatGPT)

## A.1 Bạn cần làm gì

Tạo **37 ảnh** PNG cho một game tu tiên phong cách chibi anime, theo đúng prompt ở mục A.5 đến A.8, đặt đúng tên file và lưu vào đúng thư mục ở mục A.2.

| Nhóm | Số ảnh | Là gì |
|---|---|---|
| A. Hào quang | 10 | Pháp trận tròn phía sau người đang tu luyện, mỗi đại cảnh giới 1 ảnh |
| B. Khung tên | 10 | Khung tên trống, mỗi đại cảnh giới 1 ảnh |
| D. Khung tên Đại viên mãn | 8 | Bản nâng cấp lộng lẫy hơn của khung B, cho cấp Đại viên mãn |
| C. Pháp trận pháp bảo | 9 | Pháp trận tròn phía sau người vừa dùng pháp bảo |

## A.2 Lưu file ở đâu

Thư mục gốc dự án: `D:\DuAnCaNhan\TikTool\Game\NgaMi_TuTien\`

Lưu **ảnh gốc** (khổ to, chưa cắt) vào các thư mục con sau (tạo thư mục nếu chưa có):

| Nhóm | Thư mục lưu |
|---|---|
| A. Hào quang | `src/assets/raw/ban3/halo/` |
| B. Khung tên | `src/assets/raw/ban3/tag/` |
| D. Khung Đại viên mãn | `src/assets/raw/ban3/tag/` (cùng thư mục với B) |
| C. Pháp trận pháp bảo | `src/assets/raw/ban3/circle/` |

Quy tắc:
- **Đúng tên file** ở cột "File" (chữ thường/hoa đúng như ghi, đuôi `.png`).
- **Không** lưu vào `src/assets/game/`. Thư mục đó là ảnh đã cắt và nén cho game, Claude tự làm.
- Tạo lại ảnh nào thì **ghi đè** file cũ, không thêm hậu tố `_v2`.
- Ảnh phải nền phông khác (xem A.4) thì vẫn giữ đúng tên file, thêm dòng vào file `src/assets/raw/ban3/GHI_CHU.txt`: `<tên file>: nền đen` hoặc `<tên file>: nền xanh`.

## A.3 Khổ ảnh

| Nhóm | Khổ khi tạo |
|---|---|
| A. Hào quang | Vuông 1024×1024 |
| B, D. Khung tên | Ngang 1536×1024 |
| C. Pháp trận | Vuông 1024×1024 |

Claude sẽ tự cắt và thu nhỏ: hào quang về 384×384, khung tên về 480×220, pháp trận về 512×512.

## A.4 Nền trong suốt

Mọi ảnh cần **nền trong suốt thật** (PNG có kênh alpha). Nếu không làm được nền trong suốt thật (ảnh ra nền ô bàn cờ xám trắng giả), tạo lại với nền phông như sau:

| Nhóm | Nền thay thế | Lý do |
|---|---|---|
| A. Hào quang, C. Pháp trận | **Đen tuyền #000000** | Ảnh phát sáng, viền mờ. Nền đen tách được sạch, nền xanh sẽ làm viền bị lem màu |
| B, D. Khung tên | **Xanh lá tuyền #00FF00** | Khung có viền rõ, không phát sáng. Không dùng màu xanh lá trong khung Trúc Cơ (B2, D1): với 2 ảnh này dùng nền **hồng tuyền #FF00FF** |

Câu dùng để tạo lại: *"Regenerate on a pure solid black background (#000000), nothing else changed."* (đổi mã màu cho đúng nhóm).

## A.5 Quy tắc chung cho mọi ảnh

Dán **khối phong cách** sau một lần ở đầu mỗi cuộc chat, rồi mới dán prompt từng ảnh. Mỗi tin nhắn tạo **1 ảnh**.

```
You will create game UI art for a bright, cute chibi anime xianxia (Chinese fantasy cultivation) mobile game.
Shared style: clean bold outlines, soft cel shading, bright saturated but soft colors, gold ornamental accents,
glowing ethereal "immortal energy" feel (soft light, drifting qi wisps), crisp and sharp, no blur, no haze.
Must stay readable when shrunk to 100 pixels: bold simple shapes, few tiny details.
Never include: text, letters, numbers, readable characters, watermark, logo, signature, faces, people, animals
(unless asked), temples, pagodas, Buddha, lotus flowers, swastika symbols, prayer beads, monks,
a flat glowing disc behind a head.
Isolated on a fully transparent background (PNG with real alpha channel), no shadow, no ground,
nothing outside the object.
```

**Tuyệt đối:**
- **Không có chữ** trong ảnh, kể cả trên ruy băng hay ô tên của khung (tên và cấp do game tự ghi).
- **Không hoa sen**, không tượng Phật, không vầng tròn sau đầu kiểu tôn giáo. Hào quang là **pháp trận / luồng linh khí**, giữa để trống.
- Hào quang và pháp trận phải **đối xứng tròn** (game sẽ xoay chậm, ảnh lệch một bên sẽ trông xấu khi xoay).

**Giữ đồng bộ trong từng nhóm:** mỗi nhóm (A, B, D, C) mở 1 cuộc chat riêng. Tạo ảnh đầu tiên của nhóm, chọn bản đẹp nhất, các ảnh sau thêm câu *"Same art style, same shape, same line weight and same proportions as the first image of this set."*

**Thứ tự nên tạo:** A1 → B1 → B4 → D3 → C7 trước (5 ảnh mẫu để anh duyệt phong cách), rồi mới tạo phần còn lại.

## A.6 Nhóm A · Hào quang (10 ảnh, vuông 1024×1024, lưu `src/assets/raw/ban3/halo/`)

Mẫu prompt (thay [MOTIF] và [COLOR] theo bảng):

```
A circular cultivation aura array seen from the front, to be placed BEHIND a seated chibi character's whole body.
Ring shape: the central 45% of the circle must be completely EMPTY and transparent (the character sits there).
The ring itself: [MOTIF], main color [COLOR], with soft outer glow fading to transparent at the edges.
Radially symmetric (looks good while slowly rotating), evenly balanced, no single top point, no crown shape.
It must look like swirling spirit energy / a magic array, NOT a religious halo and NOT a solid disc.
Centered, filling 95% of the canvas, transparent background.
```

| Mã | File | Cảnh giới | [MOTIF] | [COLOR] |
|---|---|---|---|---|
| A1 | `halo_luyenKhi.png` | Luyện Khí | thin wisps of white mist circling, a few tiny floating qi sparks | pale jade white and very light green |
| A2 | `halo_trucCo.png` | Trúc Cơ | bamboo leaves spinning in a circle, light green wind streaks | fresh bamboo green with gold highlights |
| A3 | `halo_ketDan.png` | Kết Đan | swirling qi ribbon with three small glowing golden pills evenly spaced on the ring | teal turquoise with gold |
| A4 | `halo_nguyenAnh.png` | Nguyên Anh | two intertwined silk-like energy ribbons forming a ring | sapphire blue with white light |
| A5 | `halo_hoaThan.png` | Hóa Thần | ring of soft violet spirit flames | violet purple with pink-white cores |
| A6 | `halo_luyenHu.png` | Luyện Hư | ring of tiny stars and nebula dust, faint constellation lines | silver white and sky blue |
| A7 | `halo_hopThe.png` | Hợp Thể | auspicious curling clouds arranged in a symmetric ring | warm gold and cream |
| A8 | `halo_daiThua.png` | Đại Thừa | phoenix tail feathers fanned into a full circle (no bird head, no bird body) | vermilion red and gold |
| A9 | `halo_doKiep.png` | Độ Kiếp | ring of crackling golden lightning over dark storm wisps | deep indigo violet with bright gold lightning |
| A10 | `halo_chanTien.png` | Chân Tiên | radiant immortal ring: fine golden light rays, celestial clouds, faint rainbow shimmer; the most magnificent of the set | brilliant white-gold with subtle rainbow edges |

## A.7 Nhóm B · Khung tên (10 ảnh, ngang 1536×1024, lưu `src/assets/raw/ban3/tag/`)

Hình dáng tham khảo: tấm bảng chữ nhật bo góc, **ruy băng cong nhỏ ở giữa cạnh trên**, ô sáng lớn ở giữa để ghi tên (giống bảng tên dễ thương nhưng phong cách tiên hiệp, sang hơn).

Mẫu prompt (thay [THEME] và [COLOR] theo bảng):

```
An empty name plate frame for a game UI, front view, horizontal; the plate is about 2.2 : 1 (width : height)
and fills about 90% of the canvas width.
Layout: a rounded rectangular plate with a wide plain light panel in the center (for a player name added later by code),
and a small curved ribbon banner on top center (also empty, for a rank title added later by code).
Decoration ONLY at the four corners and around the top ribbon; the long top and bottom edges and both side edges
must stay plain and uniform (the frame will be stretched horizontally by the game).
Four small round EMPTY gem sockets evenly spaced along the bottom edge (the game lights them up).
Theme: elegant immortal / xianxia, [THEME], main color [COLOR].
Inner panel: soft cream-white, flat, no texture, no pattern, no text. Ribbon: plain, no text.
Absolutely no text, letters or numbers anywhere. Centered, transparent background.
```

| Mã | File | Cảnh giới | [THEME] | [COLOR] |
|---|---|---|---|---|
| B1 | `tag_luyenKhi.png` | Luyện Khí | simple jade plaque with small cloud swirls at the corners | pale jade green and white |
| B2 | `tag_trucCo.png` | Trúc Cơ | bamboo frame, bamboo leaves at the corners | bamboo green with light gold trim |
| B3 | `tag_ketDan.png` | Kết Đan | jade frame with a tiny golden pill emblem at the ribbon center | teal with gold trim |
| B4 | `tag_nguyenAnh.png` | Nguyên Anh | sapphire frame, flowing silk ribbon ends at the corners | sapphire blue with silver trim |
| B5 | `tag_hoaThan.png` | Hóa Thần | amethyst frame with small violet spirit flames at the corners | violet with pink-gold trim |
| B6 | `tag_luyenHu.png` | Luyện Hư | silver frame with tiny stars at the corners | silver white and sky blue |
| B7 | `tag_hopThe.png` | Hợp Thể | gold frame with auspicious cloud corners | warm gold and cream |
| B8 | `tag_daiThua.png` | Đại Thừa | lacquer frame with phoenix feather corners | vermilion red and gold |
| B9 | `tag_doKiep.png` | Độ Kiếp | dark frame with golden lightning corners | deep indigo with gold |
| B10 | `tag_chanTien.png` | Chân Tiên | radiant celestial frame with small cloud wings at both sides and fine light rays behind the ribbon; the most luxurious of the set | white-gold with subtle rainbow shimmer |

## A.8 Nhóm D · Khung tên Đại viên mãn (8 ảnh, ngang 1536×1024, lưu `src/assets/raw/ban3/tag/`)

Đại viên mãn là cấp cao nhất trong mỗi cảnh giới. Khung D là **bản nâng cấp của đúng khung B cùng cảnh giới**: giữ nguyên hình dáng, bố cục, màu chính, kích thước ô tên và vị trí 4 hốc ngọc, nhưng lộng lẫy hơn rõ rệt.

Cách làm: mở lại cuộc chat nhóm B, gửi ảnh B tương ứng (hoặc mô tả lại nó) rồi dán mẫu prompt:

```
Create the "Great Perfection" upgraded version of this exact name plate frame ([THEME], main color [COLOR]).
Keep exactly the same overall shape, proportions, layout, inner panel size and the four gem sockets on the bottom edge.
Upgrade it: richer gold filigree on the four corners, a pair of small elegant wings or flame-like flourishes
on both sides, a small radiant jewel at the ribbon center, soft golden aura glow around the whole frame.
The long top and bottom edges and the side edges must still be plain and uniform (it will be stretched horizontally).
Inner panel: soft cream-white, flat, no text. Ribbon: plain, no text. Absolutely no text, letters or numbers.
Centered, transparent background.
```

| Mã | File | Nâng cấp từ | [THEME] | [COLOR] |
|---|---|---|---|---|
| D1 | `tag_trucCo_dvm.png` | B2 | bamboo frame, bamboo leaves at the corners | bamboo green with light gold trim |
| D2 | `tag_ketDan_dvm.png` | B3 | jade frame with a golden pill emblem | teal with gold trim |
| D3 | `tag_nguyenAnh_dvm.png` | B4 | sapphire frame, flowing silk ribbon ends | sapphire blue with silver trim |
| D4 | `tag_hoaThan_dvm.png` | B5 | amethyst frame with violet spirit flames | violet with pink-gold trim |
| D5 | `tag_luyenHu_dvm.png` | B6 | silver frame with tiny stars | silver white and sky blue |
| D6 | `tag_hopThe_dvm.png` | B7 | gold frame with auspicious clouds | warm gold and cream |
| D7 | `tag_daiThua_dvm.png` | B8 | lacquer frame with phoenix feathers | vermilion red and gold |
| D8 | `tag_doKiep_dvm.png` | B9 | dark frame with golden lightning | deep indigo with gold |

Luyện Khí (chia 9 tầng) và Chân Tiên (chỉ 1 cấp) không có Đại viên mãn nên không có khung D.

## A.9 Nhóm C · Pháp trận pháp bảo (9 ảnh, vuông 1024×1024, lưu `src/assets/raw/ban3/circle/`)

Mẫu prompt (thay [MOTIF] và [COLOR] theo bảng):

```
A circular magic array (formation circle) seen from the front, for a xianxia game item effect.
Ring shape with the central 35% EMPTY and transparent. [MOTIF]. Main color [COLOR],
soft outer glow fading to transparent. Radially symmetric so it looks good while rotating.
Centered, filling 95% of the canvas, transparent background.
```

| Mã | File | Pháp bảo | [MOTIF] | [COLOR] |
|---|---|---|---|---|
| C1 | `circle_linh_thach.png` | Linh Thạch | hexagonal ice-crystal shards arranged in a ring | icy cyan blue |
| C2 | `circle_tu_khi_dan.png` | Tụ Khí Đan | ring of alchemy furnace flames with tiny pill orbs | warm orange and gold |
| C3 | `circle_chu_qua.png` | Chu Quả | ring of curling vines with small red fruits | ruby red and leaf green |
| C4 | `circle_ban_dao.png` | Bàn Đào | ring of peach blossom branches and petals (peach blossoms, NOT lotus) | soft pink and gold |
| C5 | `circle_linh_chi.png` | Thiên Niên Linh Chi | ring of curling auspicious clouds with tiny glowing spores | jade green and cream |
| C6 | `circle_ngoc_dich.png` | Ngọc Dịch Quỳnh Tương | concentric water ripples with droplets on the ring | aqua blue and white |
| C7 | `circle_bi_tich.png` | Bí Tịch Công Pháp | magic rune circle: two thin rings with abstract rune-like symbols between them (decorative strokes only, NOT readable letters or real characters) | golden yellow |
| C8 | `circle_phi_kiem.png` | Phi Kiếm Trận | eight small flying swords pointing outward, evenly spaced around a ring | steel blue and white light |
| C9 | `circle_long_chau.png` | Long Châu | a golden eastern dragon curled into a full circle around the edge, head and tail meeting, with cloud swirls | rich gold and amber |

Linh Thú Đản không cần pháp trận ảnh (dùng vầng sáng code + hiệu ứng trứng nở hiện có).

## A.10 Kiểm tra trước khi báo xong

Với mỗi ảnh:
- [ ] Đúng tên file, đúng thư mục, đúng khổ.
- [ ] Không có chữ, số, ký tự đọc được (soi kỹ ruy băng và ô tên của khung).
- [ ] Nền trong suốt thật, hoặc nền phông đúng màu và đã ghi vào `GHI_CHU.txt`.
- [ ] Hào quang / pháp trận: giữa trống, đối xứng tròn, không hoa sen, không giống vầng sáng sau đầu.
- [ ] Khung tên: cạnh trên, dưới, hai bên trơn đều; có 4 hốc ngọc trống ở cạnh dưới; ruy băng ở giữa cạnh trên.
- [ ] Khung D cùng hình dáng với khung B tương ứng, chỉ lộng lẫy hơn.

Xong thì báo anh danh sách file đã lưu. Anh chuyển cho Claude ghép vào game.

---

# PHẦN B · THIẾT KẾ CHUYỂN ĐỘNG (Claude làm code)

## B.1 Độ nặng khi dùng ảnh

Dùng ảnh **nhẹ hơn** cách vẽ bằng code hiện tại:
- Hiện tại mỗi người 5 lớp gradient xoay + mặt nạ tròn; 53 người = 265 lớp vẽ lại liên tục.
- Bản 3 mỗi người 1–2 ảnh xoay bằng card đồ họa (transform), không vẽ lại.
- 37 ảnh sau khi cắt + nén khoảng 40–150 KB mỗi ảnh, game nặng thêm **khoảng 3–4 MB** (game hiện 7,5 MB, bộ cài 260 MB).
- Mỗi ảnh giải mã 1 lần, mọi người cùng cảnh giới dùng chung. Bộ nhớ đồ họa thêm khoảng 15 MB.
- Ảnh nào chưa có thì game dùng tạm hào quang / khung vẽ bằng code đơn giản (không lỗi, không trống).

## B.2 Hào quang sau người tu luyện

Pháp trận tâm ở ngực, đường kính ≈ 1,3 lần chiều cao nhân vật, nằm sau nhân vật. Lớp: ảnh hào quang cảnh giới (A) + quầng sáng mờ phía sau (code) + 4–6 đốm sáng bay lên (code).

| Trạng thái | Chuyển động |
|---|---|
| Không tu luyện (quá 1 phút 30 giây không được cộng tu vi) | Ẩn |
| Bắt đầu tu luyện | Hiện dần 0,8 giây, phóng từ 0,6 lên 1 lần |
| Đang tu luyện | Ảnh xoay rất chậm (1 vòng / 30 giây), nhịp thở 0,97 ↔ 1,03 mỗi 3 giây, quầng sáng sáng / tối theo nhịp |
| Vừa được cộng tu vi | Lóe sáng 0,5 giây (to 1,12 lần, sáng hơn) |
| Lên tiểu cảnh | Lóe sáng + 1 vòng sáng loang ra |
| Lên đại cảnh giới | Ảnh cũ tan, ảnh cảnh giới mới nở ra, xoay nhanh 1 vòng rồi chậm lại |
| Đánh rồng / giải cứu | Theo hạng sát thương: hạng 1 to **1,8 lần** + 1 bản sao xoay ngược + đốm sáng dày; hạng 2–3 to 1,4 lần; còn lại 1–1,25 lần theo % so với hạng 1 |
| Tỉ thí | Như đang tu luyện; người vừa ra đòn lóe sáng màu phe |

## B.3 Bảng tên dưới chân

**Bố cục:** khung ảnh (B hoặc D) đặt ngay dưới chân nhân vật. Ruy băng ghi cấp nhỏ, ô giữa ghi tên. Ba ghế đài có thêm chữ chức vị (Chưởng môn, Tả / Hữu hộ pháp) nhỏ phía trên ruy băng.

**Đổi bố cục để không che nhau** (vì bảng chuyển xuống chân, hàng sau dễ che bảng hàng trước):
- Nhân vật hàng thường thu nhỏ khoảng 15%, giãn khoảng cách giữa các hàng cho đủ chỗ bảng.
- Bảng nằm trên cùng (không bị nhân vật hàng sau che), chỉ hào quang nằm sau.
- Bề ngang bảng hàng thường tối đa bằng khoảng cách 2 chỗ ngồi; tên dài cắt giữa ("Nguo…Thu1").
- Bảng **không phóng to** theo nhân vật khi tỉ thí / đánh rồng, chỉ hào quang phóng to.
- Kiểm tra bằng ảnh chụp ở 4 cảnh: 53 người, 10 người, tỉ thí, đánh rồng.

**Co giãn:** khung ảnh cắt 9 mảnh (4 góc giữ nguyên, cạnh kéo dài) nên tên dài ngắn đều vừa.

**Phân biệt cấp nhỏ (code):**

| Cấp | Khung | Ruy băng | Ngọc ở cạnh dưới | Thêm |
|---|---|---|---|---|
| Sơ kỳ | B | "Sơ kỳ" | 1 viên sáng | |
| Trung kỳ | B | "Trung kỳ" | 2 viên sáng | |
| Hậu kỳ | B | "Hậu kỳ" | 3 viên sáng | viền sáng hơn |
| Đại viên mãn | **D** | "Đại viên mãn" | 4 viên sáng | vệt sáng chạy ngang khung mỗi 4 giây |
| Luyện Khí tầng 1–9 | B1 | "Tầng 1"…"Tầng 9" | ngọc sáng dần theo tầng (tầng 1–2: 1 viên … tầng 9: 4 viên) | |
| Chân Tiên | B10 | "Chân Tiên" | 4 viên | khung luôn lấp lánh |

**Chuyển động bảng tên:**
- Vào chỗ: bật lên 0,6 → 1,05 → 1 lần.
- Lên tiểu cảnh: nảy nhẹ, ngọc mới sáng lên kèm tia lóe.
- Lên Đại viên mãn / lên đại cảnh giới: lật 180° đổi sang khung mới.
- Sắp rời chỗ: mờ cùng nhân vật như hiện tại.
- Tỉ thí: viền khung phát sáng màu phe (xanh Thanh Liên / hồng Hồng Liên).

## B.4 Hiệu ứng pháp bảo

**Nguyên tắc chung:**
- Mọi lớp của 1 món (ảnh món, pháp trận C, hạt sáng, chữ) dùng **chung 1 tâm** là ngực người dùng. Sửa lỗi Bí Tịch lệch (hiện vòng chữ đặt cao hơn tâm vầng sáng 15% chiều cao người).
- Pháp trận C hiện **phía sau** người dùng, nở ra, xoay, tan khi món tan.
- Giữ bậc thời gian hiện tại: 1,6 / 2,2 / 3 / 5 giây; món càng đắt càng to.
- Số "+tu vi" nảy lên trên đầu lúc món nhập vào người.
- Combo (x5, x10): chạy 1 lần to hơn, góc trên hiện "x10", pháp trận xoay nhanh hơn; không chạy lặp.

| # | Món (bậc) | Kịch bản | Pháp trận |
|---|---|---|---|
| 1 | Linh Thạch (1) | Rơi từ trên xuống ngực, khựng lại, vỡ thành 6 mảnh tinh thể xanh bay vào người | C1 xoay nhanh |
| 2 | Tụ Khí Đan (1) | Hiện trước ngực, rung lắc mạnh dần, nứt ánh vàng cam, co lại nhập vào người | C2 đập theo nhịp rung |
| 3 | Chu Quả (1) | Rơi, nảy 2 lần, bừng sáng đỏ, tóe hạt sáng đỏ | C3 |
| 4 | Bàn Đào (2) | Lơ lửng hạ xuống chậm, cánh hoa đào rơi quanh, xoay nửa vòng, sáng hồng rồi nhập vào | C4 |
| 5 | Thiên Niên Linh Chi (2) | Mọc lên từ chân, đung đưa như sóng biển, phun bào tử xanh bay lên | C5 |
| 6 | Ngọc Dịch Quỳnh Tương (3) | Bình nghiêng trên đầu rót dòng ngọc dịch, gợn nước loang quanh chân 3 vòng | C6 |
| 7 | Bí Tịch Công Pháp (3) | Cuộn trục bay tới mở ra trên đầu, 8 chữ Hán vàng 劍 氣 靈 心 功 法 神 力 xoay **đúng quanh tâm người**, rồi lần lượt lao vào ngực | C7 |
| 8 | Phi Kiếm Trận (3) | 8 kiếm hiện thành vòng quanh người, xoay nhanh dần, hợp thành cột sáng từ trời xuống | C8 nằm dưới chân, nghiêng thành elip |
| 9 | Long Châu (4, 5 giây) | Ngọc hạ xuống trên đầu, pháp trận rồng xoay phía sau, 3 đợt sóng sáng + chớp toàn màn, ngọc nhập vào người | C9 lớn |
| 10 | Linh Thú Đản (3) | Trứng rơi cạnh người, rung, nứt, bừng sáng, linh thú nhảy ra (như hiện tại) | vầng sáng code |

## B.5 Thứ tự làm

1. ChatGPT tạo 5 ảnh mẫu (A1, B1, B4, D3, C7) → lưu đúng thư mục → anh báo Claude.
2. Claude làm code bản 3 với 5 ảnh mẫu (ảnh còn thiếu dùng tạm bản code), chụp ở đúng cỡ trên live để anh duyệt phong cách.
3. Duyệt xong, ChatGPT tạo 32 ảnh còn lại.
4. Claude ghép đủ, kiểm tra 4 cảnh (53 người, 10 người, tỉ thí, đánh rồng), rồi đóng gói .exe + zip.
