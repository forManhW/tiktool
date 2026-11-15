# Ảnh hiệu ứng mở rộng: src/assets/raw/vfx_morong/*.png -> WebP trong suốt cho game (src/assets/game/vfx).
#   python tools/build-vfx.py                 ghép toàn bộ
#   python tools/build-vfx.py --sheet out.png  thêm bảng soi trên nền tối + nền sáng (để duyệt bằng mắt)
# Cần Pillow, numpy, scipy. Công cụ riêng cho bộ mở rộng; KHÔNG đụng tools/build-ban3.py và bộ ảnh đang dùng.
#
# Loại nền đọc từ GHI_CHU.txt (chỉ lấy dòng "vfx_*.png: nền ..." có file thật; dòng tag_mau_* và ghi chú tự do bỏ qua):
#   nền đen        -> độ sáng (kênh lớn nhất) thành alpha, bù màu để ghép lên nền đen ra đúng ảnh gốc
#   nền trong suốt -> giữ alpha, bỏ quầng mờ và điểm rời ngoài hình
#   nền xanh / hồng -> tách phông theo màu nền
# Ảnh nhiều mảnh (V4, V6, V11): cắt rời từng mảnh, xuất <tên>_1.webp ... theo diện tích giảm dần.
import json, os, re, sys
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "..", "src", "assets", "raw", "vfx_morong")
OUT = os.path.join(HERE, "..", "src", "assets", "game", "vfx")

# size: cạnh dài xuất ra (khoảng 2 lần cỡ dùng trong game). mode: "dir" = có đầu/đuôi, cắt sát; "center" = vòng tròn, giữ tâm ảnh;
# "pieces" = cắt rời từng mảnh (size = cạnh dài mỗi mảnh). view: cỡ soi trong bảng duyệt (cỡ dùng trong game).
SPEC = {
    "vfx_kiem_quang":      {"mode": "dir", "size": 400, "view": 200},
    "vfx_va_cham":         {"mode": "center", "size": 256, "view": 120},
    "vfx_song_linh_khi":   {"mode": "center", "size": 512, "view": 150},
    "vfx_manh_linh_thach": {"mode": "pieces", "size": 72, "view": 30, "expect": 6},
    "vfx_hoa_tien":        {"mode": "dir", "size": 400, "view": 200},
    "vfx_canh_dao":        {"mode": "pieces", "size": 56, "view": 24, "expect": 9},
    "vfx_linh_chi":        {"mode": "dir", "size": 440, "view": 220},
    "vfx_ngoc_dich":       {"mode": "dir", "size": 440, "view": 220},
    "vfx_phu_van":         {"mode": "center", "size": 512, "view": 260},
    "vfx_long_khi":        {"mode": "dir", "size": 840, "view": 420},
    "vfx_trung_no":        {"mode": "pieces", "size": 88, "view": 40, "expect": 10},   # 7 mảnh vỏ + 3 sao nhỏ (3 file cuối)
    "vfx_khoi_bui":        {"mode": "center", "size": 320, "view": 160},
}
BLACK_FLOOR = 10      # nền "đen" lệch 1–2 mức và nhiễu nén: dưới mức này coi là nền
ALPHA_FLOOR = 40      # ảnh alpha: quầng mờ dưới mức này bỏ hẳn
SOLID = 128           # alpha từ mức này trở lên mới tính là thân mảnh khi cắt rời


def read_notes(path):
    """{tên file: 'black' | 'alpha' | 'green' | 'pink'} cho các dòng vfx_*.png có file thật trong thư mục."""
    kinds = {}
    if not os.path.isfile(path):
        return kinds
    for line in open(path, encoding="utf-8"):
        m = re.match(r"\s*(vfx_[a-z0-9_]+\.png)\s*:\s*(.+)", line)
        if not m or not os.path.isfile(os.path.join(RAW, m.group(1))):
            continue
        note = m.group(2).lower()
        kind = "black" if note.startswith("nền đen") else "alpha" if note.startswith("nền trong suốt") else \
            "green" if note.startswith("nền xanh") else "pink" if note.startswith("nền hồng") else None
        if kind:
            kinds[m.group(1)] = kind
    return kinds


def from_black(im):
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    peak = a.max(axis=2)
    alpha = np.clip((peak - BLACK_FLOOR) / (255.0 - BLACK_FLOOR), 0, 1)
    # Bù màu: màu * alpha = ảnh gốc, nên ghép lên nền đen vẫn ra đúng ảnh gốc; vùng đỏ / xanh sẫm giữ sắc độ, chỉ mờ đi.
    rgb = np.where(alpha[..., None] > 0, np.clip(a / np.maximum(alpha[..., None], 1e-3), 0, 255), 0)
    return np.dstack([rgb, alpha * 255]).astype(np.uint8)


def from_key(im, key):
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    dist = np.sqrt(((a - np.array(key, np.float32)) ** 2).sum(axis=2))
    alpha = np.clip((dist - 60) / 90.0, 0, 1)
    spill = 1 if key[1] > key[0] else None            # nền xanh: hạ kênh xanh lá ở viền
    if spill is not None:
        a[..., 1] = np.minimum(a[..., 1], np.maximum(a[..., 0], a[..., 2]) + 10)
    else:                                               # nền hồng: hạ đỏ + lam ở viền
        lim = a[..., 1] + 40
        a[..., 0] = np.minimum(a[..., 0], np.maximum(lim, a[..., 0] * alpha))
        a[..., 2] = np.minimum(a[..., 2], np.maximum(lim, a[..., 2] * alpha))
    return np.dstack([a, alpha * 255]).astype(np.uint8)


def clean_alpha(im):
    """Bỏ quầng mờ và điểm rời: giữ pixel thuộc (hoặc sát) một khối đặc đủ lớn; alpha thấp kéo về 0."""
    a = np.asarray(im.convert("RGBA")).copy()
    al = a[..., 3].astype(np.float32)
    labels, n = ndimage.label(al >= SOLID)
    if n:
        areas = ndimage.sum(np.ones_like(al), labels, range(1, n + 1))
        keep = np.isin(labels, 1 + np.flatnonzero(areas >= max(40, al.size * 0.00002)))
        near = ndimage.binary_dilation(keep, iterations=6)      # giữ viền khử răng cưa quanh khối
        al = np.where(near, al, 0)
    al = np.clip((al - ALPHA_FLOOR) / (255.0 - ALPHA_FLOOR), 0, 1) * 255
    a[..., 3] = al.astype(np.uint8)
    return a


def bbox(alpha, thr=8):
    ys, xs = np.nonzero(alpha > thr)
    return (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1) if len(xs) else None


def fit(rgba, size):
    im = Image.fromarray(rgba, "RGBA")
    k = float(size) / max(im.size)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def split_pieces(rgba, expect):
    """Mỗi khối đặc đủ lớn là 1 mảnh; pixel viền mờ thuộc về khối gần nhất. Điểm rời nhỏ không tính là mảnh."""
    al = rgba[..., 3]
    labels, n = ndimage.label(al >= SOLID)
    if not n:
        return [], 0
    areas = ndimage.sum(np.ones(al.shape), labels, range(1, n + 1))
    big = [i + 1 for i in np.argsort(-areas) if areas[i] >= al.size * 0.0005]   # nhỏ hơn 0,05% ảnh: điểm rời, không phải mảnh
    small = n - len(big)
    seed = np.where(np.isin(labels, big), labels, 0)
    idx = ndimage.distance_transform_edt(seed == 0, return_distances=False, return_indices=True)
    owner = seed[idx[0], idx[1]]
    near = ndimage.binary_dilation(seed > 0, iterations=5)
    pieces = []
    for lab in big:
        part = rgba.copy()
        part[..., 3] = np.where((owner == lab) & near, al, 0)
        b = bbox(part[..., 3])
        if b:
            pieces.append(part[b[1]:b[3], b[0]:b[2]])
    return pieces, small


def margins(alpha, thr=24):
    b = bbox(alpha, thr)
    if not b:
        return None
    h, w = alpha.shape
    return [round(100.0 * float(v), 1) for v in (b[0] / w, b[1] / h, (w - b[2]) / w, (h - b[3]) / h)]


def save(im, name):
    dst = os.path.join(OUT, name + ".webp")
    im.save(dst, "WEBP", quality=86, alpha_quality=92, method=5)
    return os.path.getsize(dst) / 1024.0


def build():
    kinds = read_notes(os.path.join(RAW, "GHI_CHU.txt"))
    os.makedirs(OUT, exist_ok=True)
    manifest, shown, problems = {}, [], []
    for name, spec in SPEC.items():
        f = name + ".png"
        src = os.path.join(RAW, f)
        if not os.path.isfile(src):
            problems.append(f + ": thiếu file nguồn"); continue
        im = Image.open(src)
        has_alpha = im.mode in ("RGBA", "LA") and np.asarray(im.convert("RGBA"))[..., 3].min() < 250
        kind = kinds.get(f)
        if not kind:
            kind = "alpha" if has_alpha else "black"
            problems.append(f + ": GHI_CHU.txt không ghi loại nền, tự nhận là " + kind)
        if kind == "alpha" and not has_alpha:
            problems.append(f + ": ghi nền trong suốt nhưng ảnh không có alpha"); continue
        rgba = from_black(im) if kind == "black" else clean_alpha(im) if kind == "alpha" else \
            from_key(im, (0, 255, 0) if kind == "green" else (255, 0, 255))
        mg = margins(rgba[..., 3])
        entry = {"bg": kind, "source": list(im.size), "marginPct": mg}
        if mg and min(mg) < 6:
            problems.append("%s: cách mép %s%% (trái, trên, phải, dưới), dưới 6%%" % (f, mg))
        if spec["mode"] == "pieces":
            pieces, small = split_pieces(rgba, spec.get("expect"))
            entry["pieces"] = []
            # Giữ tương quan to nhỏ giữa các mảnh: mảnh lớn nhất = size, mảnh nhỏ không dưới nửa size cho còn nhìn ra.
            longest = max([max(p.shape[:2]) for p in pieces] or [1])
            for i, p in enumerate(pieces, 1):
                out = fit(p, max(spec["size"] * 0.5, spec["size"] * max(p.shape[:2]) / longest))
                kb = save(out, "%s_%d" % (name, i))
                entry["pieces"].append({"file": "%s_%d.webp" % (name, i), "w": out.width, "h": out.height})
                shown.append(("%s_%d" % (name, i), out, spec["view"]))
                print("%-28s -> %3dx%-3d %5.1f KB" % ("%s_%d" % (name, i), out.width, out.height, kb))
            if spec.get("expect") and len(pieces) != spec["expect"]:
                problems.append("%s: tách được %d mảnh lớn (kế hoạch %d), bỏ %d điểm rời nhỏ" % (f, len(pieces), spec["expect"], small))
        else:
            h, w = rgba.shape[:2]
            b = bbox(rgba[..., 3], 12) or (0, 0, w, h)
            if spec["mode"] == "center":      # vòng tròn: cắt vuông quanh TÂM ẢNH để game xoay / phóng đúng tâm
                r = max(w / 2 - b[0], b[2] - w / 2, h / 2 - b[1], b[3] - h / 2)
                r = int(min(r + 4, w / 2, h / 2))
                crop = rgba[h // 2 - r:h // 2 + r, w // 2 - r:w // 2 + r]
            else:
                pad = 4
                crop = rgba[max(0, b[1] - pad):min(h, b[3] + pad), max(0, b[0] - pad):min(w, b[2] + pad)]
            out = fit(crop, spec["size"])
            kb = save(out, name)
            entry.update({"file": name + ".webp", "w": out.width, "h": out.height})
            shown.append((name, out, spec["view"]))
            print("%-28s -> %3dx%-3d %5.1f KB  (%s)" % (name, out.width, out.height, kb, kind))
        manifest[name] = entry
    with open(os.path.join(OUT, "vfx.json"), "w", encoding="utf-8") as fh:
        json.dump(manifest, fh, ensure_ascii=False, indent=1)
    if problems:
        print("\nCần xem lại:")
        for p in problems:
            print(" - " + p)
    return shown


def sheet(shown, path):
    """Mỗi ảnh vẽ 4 lần: cỡ trong game và cỡ 120 px (mảnh: x3), trên nền tối và nền sáng của game."""
    cell, cols = 250, 4
    rows = len(shown)
    out = Image.new("RGB", (cell * cols, cell * rows), (0, 0, 0))
    dark, light = (22, 16, 44), (236, 242, 250)
    for r, (name, im, view) in enumerate(shown):
        big = 120 if max(im.size) > 100 else view * 3
        for c, (bg, size) in enumerate([(dark, view), (dark, big), (light, view), (light, big)]):
            tile = Image.new("RGB", (cell, cell), bg)
            k = min(size, cell - 10) / max(im.size)
            small = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
            tile.paste(small, ((cell - small.width) // 2, (cell - small.height) // 2), small)
            out.paste(tile, (c * cell, r * cell))
    out.save(path)
    print("Bảng soi:", path)


if __name__ == "__main__":
    result = build()
    if "--sheet" in sys.argv:
        sheet(result, sys.argv[sys.argv.index("--sheet") + 1])
