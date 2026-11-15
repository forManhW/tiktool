# Nga Mi Tu Tiên · Khung tên bản 2 (dẹt hơn, in sẵn tên cảnh giới)

Thay cho nhóm B và D trong `KE_HOACH_BAN_3_HIEU_UNG.md`. Hào quang (A) và pháp trận (C) **giữ nguyên, không làm lại**.

Lý do làm lại:
- Khung cũ quá cao (tỉ lệ khoảng 2,4 : 1), chiếm nhiều chỗ giữa các hàng người.
- Tên cảnh giới do code ghi lên ruy băng quá nhỏ, không đọc được. Khung mới **in sẵn tên cảnh giới** bằng chữ to, đậm ngay trong ảnh.
- Tên người chơi vẫn do game ghi vào ô giữa. Tiểu cảnh (Sơ kỳ, Trung kỳ...) thể hiện bằng số ngọc sáng do game bật, không ghi chữ.

---

# PHẦN A · HƯỚNG DẪN TẠO ẢNH (cho ChatGPT)

## A.1 Bạn cần làm gì

Tạo **18 ảnh** PNG khung tên cho một game tu tiên phong cách chibi anime:

| Nhóm | Số ảnh | Là gì |
|---|---|---|
| B. Khung tên | 10 | Mỗi đại cảnh giới 1 khung, có in tên cảnh giới |
| D. Khung tên Đại viên mãn | 8 | Bản lộng lẫy hơn của khung B cùng cảnh giới, cùng chữ |

## A.2 Lưu file ở đâu

Thư mục gốc dự án: `D:\DuAnCaNhan\TikTool\Game\NgaMi_TuTien\`

Lưu cả 18 ảnh vào: **`src/assets/raw/ban3/tag/`**

- Đúng tên file ở cột "File" (chữ hoa / thường đúng như ghi, đuôi `.png`).
- **Ghi đè** file cũ cùng tên (khung cũ không dùng nữa). Không thêm hậu tố `_v2`.
- Không lưu vào `src/assets/game/` (thư mục đó Claude tự làm).
- Ảnh phải dùng nền phông (xem A.4) thì ghi thêm 1 dòng vào `src/assets/raw/ban3/GHI_CHU.txt`: `<tên file>: nền xanh` (hoặc `nền hồng`).

## A.3 Khổ ảnh và hình dáng

- Khổ khi tạo: **ngang 1536×1024**. Khung nằm giữa, rộng khoảng **95% bề ngang** ảnh, phần còn lại trong suốt.
- **Khung DẸT**: cả khung (tính cả ruy băng) có tỉ lệ khoảng **3,4 : 1** (rộng 1460 thì cao khoảng 430). Không được cao hơn 1/3 bề ngang.
- Bố cục từ trên xuống:

| Phần | Vị trí | Nội dung |
|---|---|---|
| Ruy băng tên cảnh giới | Giữa cạnh trên, **rộng khoảng 60%** bề ngang khung, cao khoảng **35%** chiều cao khung, đè lên viền trên | **Tên cảnh giới in sẵn**, chữ to, đậm |
| Ô tên người chơi | Giữa khung, rộng khoảng **88%**, cao khoảng **45%** chiều cao khung | Màu kem trơn, **để trống** (game ghi tên) |
| Hốc ngọc | Nằm trên viền dưới, chính giữa | 4 hốc tròn trống, cách đều (Luyện Khí: 9 hốc nhỏ) |

- Trang trí chỉ ở **4 góc** và quanh ruy băng. Cạnh trên, dưới và hai bên để trơn, mảnh.
- Viền khung **mảnh** để ô tên được rộng nhất có thể.

## A.4 Nền trong suốt

Cần **nền trong suốt thật** (PNG có kênh alpha). Nếu ảnh ra nền ô bàn cờ xám trắng giả, tạo lại trên nền phông **xanh lá tuyền #00FF00**. Riêng khung Trúc Cơ (B2, D1) có màu xanh lá nên dùng nền **hồng tuyền #FF00FF**.

Câu dùng để tạo lại: *"Regenerate on a pure solid #00FF00 background, nothing else changed."*

## A.5 Chữ tên cảnh giới (quan trọng nhất)

Mỗi khung có **đúng 1 dòng chữ** trên ruy băng: tên cảnh giới tiếng Việt, **có đủ dấu**, viết đúng y như cột "Chữ in" ở bảng A.7.

Yêu cầu chữ:
- Font **không chân, nét rất đậm, tròn** (kiểu chữ tiêu đề game mobile), dễ đọc khi thu nhỏ.
- Chữ **chiếm gần hết ruy băng**: cao khoảng **60%** chiều cao ruy băng, dài khoảng **80%** bề ngang ruy băng.
- **Màu chữ đậm trên nền ruy băng sáng** (ví dụ chữ nâu sẫm / xanh đen trên ruy băng kem), có viền sáng mảnh quanh chữ. Không dùng chữ vàng nhạt trên nền vàng.
- Dấu tiếng Việt phải **đúng và rõ**: dấu mũ, dấu móc, dấu sắc / huyền / hỏi / ngã / nặng đặt đúng chữ cái. Sai một dấu là phải tạo lại.
- Không thêm chữ nào khác (không "Sơ kỳ", không số, không tên người, không chữ Hán).

## A.6 Khối phong cách (dán 1 lần ở đầu cuộc chat)

```
You will create game UI art for a bright, cute chibi anime xianxia (Chinese fantasy cultivation) mobile game.
Shared style: clean bold outlines, soft cel shading, bright saturated but soft colors, gold ornamental accents,
glowing ethereal immortal feel, crisp and sharp, no blur.
Must stay readable when shrunk to 110 pixels wide: bold simple shapes, thin plain frame edges, few tiny details.
The ONLY text allowed is the exact rank title I give you, printed on the top ribbon, in Vietnamese with correct diacritics.
Never include any other text, numbers, watermark, logo, signature, faces, people, temples, Buddha, lotus flowers.
Isolated on a fully transparent background (PNG with real alpha channel), no shadow, nothing outside the plate.
```

Mỗi tin nhắn tạo **1 ảnh**. Nhóm B một cuộc chat, nhóm D mở lại cuộc chat nhóm B (để giữ đúng hình dáng). Từ ảnh thứ 2 trở đi thêm câu: *"Same art style, same plate shape, same proportions, same ribbon size and same lettering style as the first image of this set."*

## A.7 Nhóm B · Khung tên (10 ảnh)

Mẫu prompt (thay [TITLE], [THEME], [COLOR], [GEMS]):

```
A FLAT, WIDE name plate for a game UI, front view. Overall plate ratio about 3.4 : 1 (very flat), filling 95% of the canvas width.
Top center: a wide curved ribbon banner (about 60% of the plate width) overlapping the top edge,
with the title text "[TITLE]" printed on it: Vietnamese, exact spelling with correct diacritics,
very bold rounded sans-serif letters, dark brown letters with a thin light outline on a light cream ribbon,
letters filling most of the ribbon.
Center: a wide plain soft cream-white panel (about 88% of the plate width), completely EMPTY, no text, no pattern.
Bottom edge: [GEMS] small round EMPTY gem sockets, evenly spaced, centered.
Thin plain frame edges; decoration only at the four corners and around the ribbon.
Theme: elegant immortal / xianxia, [THEME], main color [COLOR].
No other text anywhere. Transparent background.
```

| Mã | File | Chữ in ([TITLE]) | [THEME] | [COLOR] | [GEMS] |
|---|---|---|---|---|---|
| B1 | `tag_luyenKhi.png` | Luyện Khí | simple jade plaque with small cloud swirls at the corners | pale jade green and white | nine |
| B2 | `tag_trucCo.png` | Trúc Cơ | bamboo frame, bamboo leaves at the corners | bamboo green with light gold trim | four |
| B3 | `tag_ketDan.png` | Kết Đan | jade frame with a tiny golden pill emblem on the ribbon ends | teal with gold trim | four |
| B4 | `tag_nguyenAnh.png` | Nguyên Anh | sapphire frame, flowing silk ribbon ends at the corners | sapphire blue with silver trim | four |
| B5 | `tag_hoaThan.png` | Hóa Thần | amethyst frame with small violet spirit flames at the corners | violet with pink-gold trim | four |
| B6 | `tag_luyenHu.png` | Luyện Hư | silver frame with tiny stars at the corners | silver white and sky blue | four |
| B7 | `tag_hopThe.png` | Hợp Thể | gold frame with auspicious cloud corners | warm gold and cream | four |
| B8 | `tag_daiThua.png` | Đại Thừa | lacquer frame with phoenix feather corners | vermilion red and gold | four |
| B9 | `tag_doKiep.png` | Độ Kiếp | dark frame with golden lightning corners | deep indigo with gold | four |
| B10 | `tag_chanTien.png` | Chân Tiên | radiant celestial frame with small cloud wings at both sides, fine light rays behind the ribbon; the most luxurious of the set | white-gold with a deeper gold outline (must stay visible on a light background) | four |

## A.8 Nhóm D · Khung tên Đại viên mãn (8 ảnh)

Khung D là **bản nâng cấp của đúng khung B cùng cảnh giới**: giữ nguyên hình dáng, tỉ lệ dẹt, ruy băng, **chữ in giống hệt**, ô tên và 4 hốc ngọc; chỉ lộng lẫy hơn.

Gửi lại ảnh B tương ứng rồi dán:

```
Create the "Great Perfection" upgraded version of this exact name plate.
Keep exactly the same flat shape and proportions, the same ribbon with the same text "[TITLE]" (same spelling, same diacritics, same lettering),
the same empty cream center panel and the same four empty gem sockets on the bottom edge.
Upgrade it: richer gold filigree on the four corners, small elegant wing or flame flourishes on both sides
(keep them short so the plate stays flat), a small radiant jewel at the ribbon center, a soft golden glow around the plate.
No other text. Transparent background.
```

| Mã | File | Nâng cấp từ | Chữ in ([TITLE]) |
|---|---|---|---|
| D1 | `tag_trucCo_dvm.png` | B2 | Trúc Cơ |
| D2 | `tag_ketDan_dvm.png` | B3 | Kết Đan |
| D3 | `tag_nguyenAnh_dvm.png` | B4 | Nguyên Anh |
| D4 | `tag_hoaThan_dvm.png` | B5 | Hóa Thần |
| D5 | `tag_luyenHu_dvm.png` | B6 | Luyện Hư |
| D6 | `tag_hopThe_dvm.png` | B7 | Hợp Thể |
| D7 | `tag_daiThua_dvm.png` | B8 | Đại Thừa |
| D8 | `tag_doKiep_dvm.png` | B9 | Độ Kiếp |

Luyện Khí (chia 9 tầng) và Chân Tiên (chỉ 1 cấp) không có Đại viên mãn nên không có khung D.

**Thứ tự nên tạo:** B4 và B1 trước (2 ảnh mẫu: 1 khung 4 ngọc, 1 khung 9 ngọc). Anh duyệt xong mới tạo 16 ảnh còn lại.

## A.9 Kiểm tra trước khi báo xong

Với mỗi ảnh:
- [ ] Đúng tên file, đã ghi đè vào `src/assets/raw/ban3/tag/`.
- [ ] Khung **dẹt**: chiều cao cả khung (tính ruy băng) không quá 1/3 bề ngang.
- [ ] Chữ trên ruy băng **đúng từng dấu** như cột "Chữ in". Soi kỹ: ệ, ơ, ế, ê, ó, ầ, ư, ợ, ể, ạ, ừ, ộ, ế, â, ê.
- [ ] Chữ to, đậm, màu tối trên ruy băng sáng; thu ảnh nhỏ còn khoảng 110px bề ngang vẫn nhận ra chữ.
- [ ] Ô tên ở giữa trống trơn, không chữ, không hoa văn.
- [ ] Đủ hốc ngọc trống ở viền dưới (Luyện Khí 9, còn lại 4).
- [ ] Không có chữ nào khác ngoài tên cảnh giới.
- [ ] Nền trong suốt thật, hoặc nền phông đúng màu và đã ghi vào `GHI_CHU.txt`.
- [ ] Khung D cùng hình dáng và cùng chữ với khung B tương ứng, chỉ lộng lẫy hơn.

Xong thì báo anh danh sách file đã lưu. Anh chuyển cho Claude ghép vào game.

---

# PHẦN B · GHI CHÚ CHO CLAUDE (ghép vào game)

- Chạy `python tools/build-ban3.py` để cắt sát, nén WebP vào `src/assets/game/ban3/tag/`.
- Code tự lấy tỉ lệ khung theo ảnh (khung mới dẹt thì bảng tên tự dẹt theo). Khi khung mới vào: **bỏ chữ cảnh giới do code ghi trên ruy băng** (`.nm-tag-realm`), vì ảnh đã in sẵn.
- Đo lại vị trí ô tên và các hốc ngọc trên ảnh mới, chỉnh `.nm-tag-name` và `.nm-gems` cho khớp.
- Khung dẹt hơn thì giãn lại các hàng trong `config.js` (`layout.rows`): có thể trả nhân vật hàng thường về to hơn một chút.
- Kiểm tra 4 cảnh: 53 người, 10 người, tỉ thí, đánh rồng / độ kiếp.
