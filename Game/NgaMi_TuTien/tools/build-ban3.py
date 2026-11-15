# Bản 3: cắt + nén ảnh hào quang / pháp trận / khung tên (src/assets/raw/ban3) thành WebP nhẹ cho game (src/assets/game/ban3).
#   python tools/build-ban3.py
# Cần Pillow (pip install pillow). Chạy lại mỗi khi thêm/sửa ảnh trong raw/ban3.
#  - Hào quang, pháp trận: cắt sát phần có hình (bỏ viền trong suốt), đặt vào ô vuông với TÂM VÒNG = tâm ảnh,
#    để game xoay / phóng quanh đúng tâm (ảnh gốc ChatGPT hay lệch, vd. Bàn Đào, Chu Quả khổ 1536x1024 vòng nằm phía trên).
#  - Khung tên: cắt sát, giữ tỉ lệ.
import os, sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "assets")
RAW, OUT = os.path.join(ROOT, "raw", "ban3"), os.path.join(ROOT, "game", "ban3")
SIZES = {"halo": 576, "circle": 576, "tag": 600}   # halo/circle: cạnh vuông; tag: bề ngang


def trim(im, thr=24):
    box = im.getchannel("A").point(lambda a: 255 if a > thr else 0).getbbox()
    return im.crop(box) if box else im


def square(im):
    s = max(im.size)
    out = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    out.paste(im, ((s - im.width) // 2, (s - im.height) // 2))
    return out


total = 0
for group, size in SIZES.items():
    src = os.path.join(RAW, group)
    if not os.path.isdir(src):
        continue
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    for f in sorted(os.listdir(src)):
        if not f.endswith(".png"):
            continue
        im = trim(Image.open(os.path.join(src, f)).convert("RGBA"))
        if group == "tag":
            im = im.resize((size, round(im.height * size / im.width)), Image.LANCZOS)
        else:
            im = square(im).resize((size, size), Image.LANCZOS)
        dst = os.path.join(OUT, group, f[:-4] + ".webp")
        im.save(dst, "WEBP", quality=82, alpha_quality=85, method=4)
        kb = os.path.getsize(dst) / 1024
        total += kb
        print(f"{group}/{f:28s} -> {im.size[0]}x{im.size[1]}  {kb:6.0f} KB")
print(f"Total {total / 1024:.1f} MB")

def blend_seams(img, seams, band=6, fade=180, smooth=41):
    """Phần vẽ nối thêm hơi lệch màu so với ảnh gốc (đường nối thấy rõ khi phóng to).
    Ở mỗi đường nối: đo chênh màu từng dòng giữa 2 bên, làm mượt theo chiều dọc, rồi bù vào phía phần vẽ thêm,
    giảm dần về 0 trong `fade` px (phía ảnh gốc giữ nguyên)."""
    import numpy as np
    a = np.asarray(img).astype(np.float32)
    H, W, _ = a.shape
    k = np.ones(smooth) / smooth
    for x in seams:
        outer_left = x < W / 2                     # đường nối trái: phần vẽ thêm ở bên trái
        inner = a[:, x:x + band] if outer_left else a[:, x - band:x]
        outer = a[:, x - band:x] if outer_left else a[:, x:x + band]
        diff = inner.mean(1) - outer.mean(1)       # H x 3
        diff = np.stack([np.convolve(np.pad(diff[:, c], smooth // 2, mode="edge"), k, mode="valid") for c in range(3)], 1)
        for i in range(fade):
            col = x - 1 - i if outer_left else x + i
            if 0 <= col < W:
                a[:, col] += diff * (1 - i / fade)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


# Nền rộng (bg_main_wide.png: ảnh nền gốc vẽ nối thêm 2 bên, phần giữa trùng khít bg_main.png).
# Bỏ kênh alpha (vài điểm lẻ hơi trong), nén JPEG cho nhẹ. Game đặt nó lệch trái 179 px để phần giữa trùng khung 1080.
wide = os.path.join(ROOT, "raw", "bg_main_wide.png")
if os.path.exists(wide):
    dst = os.path.join(ROOT, "game", "bg_main_wide.jpg")
    img = Image.open(wide).convert("RGB")
    try:
        img = blend_seams(img, [156, 1097])
    except ImportError:
        print("  (thiếu numpy: bỏ qua bước làm mượt đường nối)")
    img.save(dst, "JPEG", quality=90, optimize=True, progressive=True)
    print(f"bg_main_wide.png -> bg_main_wide.jpg  {os.path.getsize(dst) / 1024:.0f} KB")


# ---------- Đo khung tên: vị trí ô tên (ô kem) và các hốc ngọc trên từng ảnh khung, ghi ra src/tag-layout.js ----------
# Mỗi khung ChatGPT vẽ hơi khác nhau (chữ cảnh giới to nhỏ, ô tên cao thấp), nên đo tự động thay vì đặt cứng 1 vị trí.
# Kết quả theo % kích thước ảnh: r = rộng/cao, panel = [trái, trên, phải, dưới] của ô tên, gx = tâm ngọc (ngang), gy = tâm ngọc (dọc), gd = đường kính ngọc (theo bề ngang).
def _runs(v):
    out, st = [], None
    for i, x in enumerate(v):
        if x and st is None: st = i
        if not x and st is not None: out.append((st, i - 1)); st = None
    if st is not None: out.append((st, len(v) - 1))
    return out


def measure_tags():
    import json
    import numpy as np
    folder = os.path.join(OUT, "tag")
    if not os.path.isdir(folder):
        return
    res = {}
    for f in sorted(os.listdir(folder)):
        if not f.endswith(".webp"):
            continue
        im = np.asarray(Image.open(os.path.join(folder, f)).convert("RGBA")).astype(int)
        H, W, _ = im.shape
        r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
        cream = (a > 200) & (r > 228) & (g > 215) & (b > 195) & ((r - b) < 50)
        full = np.isin(np.arange(H), np.where(cream.sum(1) > 0.55 * W)[0])
        t, bm = max(_runs(full), key=lambda q: q[1] - q[0])
        cols = np.where(cream[t:bm + 1].mean(0) > 0.6)[0]
        l, rt = int(cols.min()), int(cols.max())
        name = f[:-5]
        seeds = [21.8 + 7.0 * i for i in range(9)] if name == "tag_luyenKhi" else [36.2, 45.4, 54.5, 63.7]
        gems = []
        for sx in seeds:
            x, best, bx = int(round(sx * W / 100)), None, 0
            for dx in range(-4, 5):
                rs = [q for q in _runs(cream[:, x + dx]) if q[0] > t + 3 and 3 <= q[1] - q[0] <= 0.35 * H]
                if rs and (best is None or rs[-1][1] - rs[-1][0] > best[1] - best[0]): best, bx = rs[-1], x + dx
            if best is None:
                continue
            cy = (best[0] + best[1]) // 2
            hr = [q for q in _runs(cream[cy]) if q[0] <= bx <= q[1]][0]
            gems.append(((hr[0] + hr[1]) / 2, cy, max(hr[1] - hr[0], best[1] - best[0]) + 1))
        gy, gd = float(np.median([q[1] for q in gems])), float(np.median([q[2] for q in gems]))
        res[name] = {"r": round(W / H, 3), "panel": [round(100 * l / W, 1), round(100 * t / H, 1), round(100 * rt / W, 1), round(100 * bm / H, 1)],
                     "gx": [round(100 * q[0] / W, 1) for q in gems], "gy": round(100 * gy / H, 1), "gd": round(100 * gd / W, 1)}
    dst = os.path.join(ROOT, "..", "tag-layout.js")
    with open(dst, "w", encoding="utf-8") as fh:
        fh.write("/* Tự sinh bởi tools/build-ban3.py (đo ảnh khung tên). Không sửa tay: thay ảnh thì chạy lại tool. */\n")
        fh.write("(function (root) { root.NgaMi = Object.assign(root.NgaMi || {}, { TAG_LAYOUT: ")
        fh.write(json.dumps(res, ensure_ascii=False, separators=(",", ":")))
        fh.write(" }); }(typeof self !== \"undefined\" ? self : this));\n")
    print("tag-layout.js:", len(res), "khung")


try:
    measure_tags()
except ImportError:
    print("(thiếu numpy: không đo được khung tên)")
