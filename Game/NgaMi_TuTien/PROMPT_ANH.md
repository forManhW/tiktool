# Nga Mi Tu Tiên · Ảnh cần tạo (bản 2) và prompt ChatGPT

> Bản 12/11/2026. Đi kèm [KE_HOACH_BAN_2.md](KE_HOACH_BAN_2.md). Quy tắc chung (cấm phong cách chùa / Phật, không chữ trong tranh…) vẫn theo [SPEC_TRANH.md](SPEC_TRANH.md) mục 1.

## Cách làm với ChatGPT Plus

1. **Mỗi tin nhắn 1 ảnh.**
   - Mở **cuộc chat mới** cho từng nhóm: nền / kiếm + pháp bảo / trứng + linh thú.
   - Đầu cuộc chat dán **Khối phong cách** bên dưới một lần, rồi dán prompt từng ảnh.
2. **Giữ đồng bộ:** tạo ảnh đầu tiên của nhóm, chọn bản ưng, rồi các ảnh sau nói thêm *"same art style as the previous image"*.
3. **Nền trong suốt:**
   - Prompt đã xin PNG nền trong suốt. Nếu ChatGPT trả ảnh có **ô bàn cờ xám trắng** thì đó là nền giả.
   - Lúc đó gõ: *"Regenerate on a plain solid pure green background (#00FF00), no shadow, nothing else changed"*. Gửi tôi, tôi tách nền xanh bằng code.
   - Riêng **Ngọc Thố (L1)** có viền xanh ngọc nên dùng nền **hồng #FF00FF** thay vì xanh lá.
4. **Lưu file:** tải ảnh về, đổi đúng **tên file** ở cột "File", bỏ vào `Game/NgaMi_TuTien/src/assets/raw/`, rồi báo tôi. Tôi sẽ:
   - kiểm tra nền thật / giả, kích thước, độ nét khi thu nhỏ;
   - cắt, thu nhỏ đưa vào game;
   - xếp các ảnh cạnh nhau ở đúng cỡ trên live để **anh duyệt**.
5. **Kích thước:** ChatGPT chỉ ra 3 khổ là vuông 1024×1024, dọc 1024×1536, ngang 1536×1024. Mỗi ảnh ghi khổ cần chọn; nếu ChatGPT hỏi thì chọn đúng khổ đó. Tôi tự cắt và chỉnh về kích thước game.

### Khối phong cách (dán 1 lần đầu mỗi cuộc chat)

```
You will create game art for a bright, cute chibi anime xianxia (Chinese fantasy cultivation) mobile game.
Shared style for every image: clean bold dark outlines, large flat color areas with soft cel shading,
bright saturated but soft colors, gold ornamental accents, crisp and sharp, high clarity, no blur, no haze.
Every object must stay readable when shrunk to 60 pixels: simple strong silhouette, few tiny details.
Never include: text, letters, watermark, logo, signature, temples, pagodas, Buddha, lotus flowers,
swastika symbols, halos behind heads, prayer beads, monks.
Unless I say otherwise: one single object, centered, filling about 80% of the canvas,
isolated on a fully transparent background (PNG with real alpha channel), no shadow, no ground.
```

---

## Danh sách ảnh

| Mã | File | Dùng cho | Khổ ChatGPT | Bắt buộc |
|---|---|---|---|---|
| N1 | `bg_main.png` | Nền mới, sáng, ban ngày | Dọc 1024×1536 | ✅ (gấp nhất) |
| K1 | `fx_sword.png` | Kiếm bay (mọi đòn đánh) + Phi Kiếm Trận | Ngang 1536×1024 | ✅ |
| P1 | `item_linh_thach.png` | Linh Thạch | Vuông | ✅ |
| P2 | `item_tu_khi_dan.png` | Tụ Khí Đan | Vuông | ✅ |
| P3 | `item_chu_qua.png` | Chu Quả | Vuông | ✅ |
| P4 | `item_ban_dao.png` | Bàn Đào | Vuông | ✅ |
| P5 | `item_linh_chi.png` | Thiên Niên Linh Chi | Vuông | ✅ |
| P6 | `item_ngoc_dich.png` | Ngọc Dịch Quỳnh Tương | Vuông | ✅ |
| P7 | `item_bi_tich.png` | Bí Tịch Công Pháp | Vuông | ✅ |
| P8 | (dùng lại K1) | Phi Kiếm Trận | | |
| P9 | `item_long_chau.png` | Long Châu | Vuông | ✅ |
| E1 | `egg.png` | Linh Thú Đản (trứng nguyên) | Vuông | ✅ |
| E2 | `egg_crack.png` | Trứng đang nứt | Vuông | Tùy chọn (không có thì code tự làm rung + lóe sáng) |
| L1 | `pet_ngoc_tho.png` | Ngọc Thố (tu luyện) | Vuông | ✅ |
| L2 | `pet_hoa_ky_lan.png` | Hỏa Kỳ Lân (chiến đấu) | Vuông | ✅ |
| L3 | `pet_kim_phuong.png` | Kim Phượng (cả hai) | Vuông | ✅ |
| T1 | `fx_storm_cloud.png` | Mây giông Thiên Kiếp | Ngang | Tùy chọn |

Tổng: **15 ảnh bắt buộc**, 2 tùy chọn. Hào quang, vệt sáng kiếm, sét, kết giới, sao linh thú đều **vẽ bằng code**, không cần ảnh.

---

## N1 · Nền mới `bg_main.png` (khổ dọc)

Mục tiêu: **sáng, trong, nét**. Giữa khung là sân đá rộng, ít chi tiết, để 53 nhân vật và tên nổi bật. Không dán Khối phong cách cho ảnh này (ảnh này có nền, không trong suốt).

```
A bright, sharp vertical background for a cute anime xianxia mobile game, tall portrait composition (9:16 feel).
Scene: the summit of a sacred sword-cultivation sect on a mountain in the clouds, bright morning daylight,
clear light-blue sky with a few soft white clouds, warm golden sunlight from the upper left.
Top 15% of the image: open sky with light clouds and distant floating peaks, simple and calm (a title will be placed here).
Middle 45% of the image: a very wide, flat, open white-jade stone plaza seen slightly from above, clean and evenly lit,
with only subtle stone tile lines, no objects in the center (many small characters will sit here in rows).
Around the edges of the plaza: a few green pine trees, pink plum blossom trees, low carved stone railings.
Far behind the plaza: green mountain peaks, a waterfall, a sea of bright clouds, and one small elegant
mountain-gate archway made of stone in the far distance (NOT a temple, NOT a pagoda).
Bottom 20%: stone steps and soft bright clouds, low detail.
Style: soft anime game background painting, clean shapes, bright pastel greens, blues, whites and gold,
high clarity, crisp, no fog, no dark areas, no heavy vignette, not night.
No characters, no people, no animals, no text, no letters, no watermark, no temple, no pagoda, no Buddha, no lotus.
```

Nếu ra quá chói hoặc quá nhiều chi tiết ở giữa, gõ thêm:

```
Keep everything the same but make the central plaza calmer and less detailed, slightly softer contrast in the
middle so small characters with name tags stand out, keep the image bright.
```

## K1 · Kiếm bay `fx_sword.png` (khổ ngang)

Code sẽ tô quầng sáng màu theo phe, nên kiếm để **màu trung tính trắng bạc + vàng kim**.

```
A single flying immortal sword, horizontal, perfectly straight, the tip pointing to the RIGHT, viewed from the side.
Slim elegant straight double-edged blade in bright white-silver with a soft white glow along the edges,
small golden ornate cross-guard, short golden hilt wrapped in white, a short white silk tassel at the end of the hilt.
The sword spans about 85% of the image width, centered vertically, nothing else in the image.
Isolated on a fully transparent background, PNG with real alpha channel, no background, no shadow, no motion trail.
```

## Pháp bảo P1–P9 (khổ vuông)

Mỗi món **1 vật thể**, nhìn hơi chếch từ trên, phát sáng nhẹ. Dán Khối phong cách trước.

**P1 · Linh Thạch `item_linh_thach.png`**
```
A small cluster of three glowing spirit stones: faceted crystal gems in bright cyan-blue with white inner light,
the middle one bigger, a few tiny sparkles around them. Game item icon.
```

**P2 · Tụ Khí Đan `item_tu_khi_dan.png`**
```
One round magic pill (elixir pellet) floating, glossy golden-orange with swirling light patterns on its surface,
a soft warm glow ring around it, two tiny sparkles. Game item icon.
```

**P3 · Chu Quả `item_chu_qua.png`**
```
A short green twig with two small glossy vermilion-red spirit fruits (round, cherry-like but bigger),
glowing softly red-gold, two small green leaves. Game item icon.
```

**P4 · Bàn Đào `item_ban_dao.png`**
```
One big plump immortal peach, pink to cream gradient with a rosy blush, two green leaves on a short stem,
soft pink glow and a few small peach-blossom petals floating around it. Game item icon.
```

**P5 · Thiên Niên Linh Chi `item_linh_chi.png`**
```
A thousand-year lingzhi mushroom: glossy reddish-brown fan-shaped cap with golden concentric rings,
short curved stem, a soft green-gold mist glowing around it. Game item icon.
```

**P6 · Ngọc Dịch Quỳnh Tương `item_ngoc_dich.png`**
```
An elegant small white-jade wine gourd bottle with gold decorations, slightly tilted, the stopper floating
just above it, a ribbon of glowing light-blue liquid curling out of the opening. Game item icon.
```

**P7 · Bí Tịch Công Pháp `item_bi_tich.png`**
```
An ancient cultivation scroll, half unrolled, cream paper with golden rollers and a red silk tie,
glowing golden abstract swirl patterns on the paper (NO letters, NO text, NO characters). Game item icon.
```

**P9 · Long Châu `item_long_chau.png`**
```
One large luminous dragon pearl: a perfect sphere of white-gold light with swirling cloud patterns inside,
held by a small golden cloud-shaped stand underneath, radiant soft glow. Game item icon.
```

## Trứng E1–E2 (khổ vuông)

**E1 · Trứng `egg.png`**
```
One cute magical spirit beast egg, standing upright, oval, pearl-white shell with golden swirl-cloud patterns
and three small colored spots (cyan, red, gold) hinting at different creatures inside, soft rainbow-gold glow.
Game item icon.
```

**E2 · Trứng nứt `egg_crack.png`** (dùng E1 làm ảnh tham chiếu)
```
Exactly the same egg as the previous image, same pose and colors, now with bright golden cracks across the shell
and strong light shining out of the cracks, a few small shell fragments popping off. Game item icon.
```

## Linh thú L1–L3 (khổ vuông)

Linh thú hiện **nhỏ cạnh chân nhân vật** (khoảng 40–80 px), nên phải **dáng rõ, đầu to, màu khác hẳn nhau**. Tư thế **ngồi, nhìn về phía trước, hơi nghiêng 3/4**. Cùng một phong cách với nhau.

**L1 · Ngọc Thố `pet_ngoc_tho.png`** (thú tu luyện)
```
A tiny chibi jade rabbit spirit beast pet, sitting, 3/4 front view, big head and big shiny eyes, round fluffy body,
white fur with soft mint-jade tips on the ears and tail, a small glowing jade-green crescent mark on the forehead,
a tiny floating green leaf of light next to it. Cute and calm. Simple strong silhouette.
```

**L2 · Hỏa Kỳ Lân `pet_hoa_ky_lan.png`** (thú chiến đấu)
```
A tiny chibi fire qilin spirit beast pet, sitting, 3/4 front view, big head and fierce-but-cute eyes,
small deer-like body with red-orange scales, a flaming orange mane and flame-tipped tail, one small golden horn,
golden hooves. Brave and energetic. Simple strong silhouette.
```

**L3 · Kim Phượng `pet_kim_phuong.png`** (thú hiếm, cả hai hệ)
```
A tiny chibi golden phoenix spirit beast pet, standing, 3/4 front view, big head and gentle proud eyes,
round golden body, wings half open, long elegant tail feathers in gold, white and a touch of crimson,
a small crown-like crest. Rare and majestic but cute. Simple strong silhouette.
```

## T1 · Mây giông Thiên Kiếp `fx_storm_cloud.png` (tùy chọn, khổ ngang)

```
A compact swirling thunderstorm cloud seen from below, dark blue-violet cloud with bright white-violet edges,
glowing purple light inside, shaped like a wide flat spiral, no lightning bolts (lightning is drawn separately).
Isolated on a fully transparent background, PNG with alpha, no ground, no sky.
```

---

## Sau khi anh tạo xong

- Gửi dần từng nhóm cũng được. Tôi làm theo thứ tự **N1 → K1 → P1–P9 → E1, L1–L3**.
- Ảnh nào chưa có, game vẫn chạy bằng hình tạm. Ảnh mới chỉ đưa vào game sau khi anh duyệt bản xếp cạnh nhau.
