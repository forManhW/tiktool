# Bảng cảnh giới kiểu mới (chỉ có tên cảnh giới, tên người chơi nằm trên đầu):
#   src/assets/raw/tag_mau/tag_mau_*.png -> src/assets/game/tag_v3/tag_<sprite>[_dvm].webp + src/tag-layout-v3.js
#   python tools/build-tag-v3.py
# Cần Pillow, numpy, scipy. Bộ đang dùng (src/assets/game/ban3/tag, tools/build-ban3.py) KHÔNG bị đụng tới.
# Đo hốc ngọc trên từng ảnh (vòng tròn rỗng ở cạnh dưới) để game bật sáng đúng chỗ theo tiểu cảnh.
import json, os
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "..", "src", "assets", "raw", "tag_mau")
OUT = os.path.join(HERE, "..", "src", "assets", "game", "tag_v3")
WIDTH = 420   # bảng to nhất trong game rộng 240 px (ghế Chưởng môn)

# tên file nguồn -> (sprite trong config.realms, số hốc ngọc). dai_vien_man là bản Đại viên mãn của Nguyên Anh (mẫu đợt 1).
MAP = {
    "luyen_khi": ("luyenKhi", 9), "truc_co": ("trucCo", 4), "truc_co_dvm": ("trucCo_dvm", 4),
    "ket_dan": ("ketDan", 4), "ket_dan_dvm": ("ketDan_dvm", 4), "nguyen_anh": ("nguyenAnh", 4), "dai_vien_man": ("nguyenAnh_dvm", 4),
    "hoa_than": ("hoaThan", 4), "hoa_than_dvm": ("hoaThan_dvm", 4), "luyen_hu": ("luyenHu", 4), "luyen_hu_dvm": ("luyenHu_dvm", 4),
    "hop_the": ("hopThe", 4), "hop_the_dvm": ("hopThe_dvm", 4), "dai_thua": ("daiThua", 4), "dai_thua_dvm": ("daiThua_dvm", 4),
    "do_kiep": ("doKiep", 4), "do_kiep_dvm": ("doKiep_dvm", 4), "chan_tien": ("chanTien", 0),
}


def sockets(alpha, count):
    """Tìm `count` hốc ngọc: lỗ rỗng gần tròn, cùng cỡ, xếp một hàng ngang ở nửa dưới, cách đều quanh giữa ảnh."""
    H, W = alpha.shape
    labels, n = ndimage.label(alpha < 110)
    found = []
    for i, sl in enumerate(ndimage.find_objects(labels), 1):
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        w, h = x1 - x0, y1 - y0
        if x0 == 0 or y0 == 0 or x1 == W or y1 == H:
            continue                                   # thông ra ngoài: là nền, không phải hốc
        if not (0.006 * W <= w <= 0.05 * W) or abs(w - h) > 0.4 * max(w, h) or (y0 + y1) / 2 < 0.5 * H:
            continue
        if (labels[sl] == i).sum() < 0.6 * w * h:
            continue
        found.append(((x0 + x1) / 2.0, (y0 + y1) / 2.0, (w + h) / 2.0))
    best = None
    for seed in found:                                 # chọn nhóm cùng hàng, cùng cỡ đông nhất
        row = sorted(f for f in found if abs(f[1] - seed[1]) < 0.04 * H and 0.6 < f[2] / seed[2] < 1.6)
        if best is None or len(row) > len(best):
            best = row
    if not best or len(best) < 2:
        return None
    if len(best) > count:                              # thừa: giữ `count` hốc gần giữa nhất
        best = sorted(sorted(best, key=lambda f: abs(f[0] - W / 2))[:count])
    return best


def main():
    os.makedirs(OUT, exist_ok=True)
    layout, warn = {}, []
    for raw, (key, count) in MAP.items():
        src = os.path.join(RAW, "tag_mau_" + raw + ".png")
        if not os.path.isfile(src):
            warn.append(raw + ": thiếu ảnh nguồn"); continue
        im = Image.open(src).convert("RGBA")
        box = im.getchannel("A").point(lambda a: 255 if a > 24 else 0).getbbox()
        im = im.crop(box)
        entry = {"r": round(im.width / im.height, 3), "gx": [], "gy": 0, "gd": 0}
        if count:
            got = sockets(np.asarray(im.getchannel("A")), count)
            if got and len(got) == count:
                entry["gx"] = [round(100.0 * g[0] / im.width, 1) for g in got]
                entry["gy"] = round(100.0 * float(np.median([g[1] for g in got])) / im.height, 1)
                # Ngọc sáng to hơn lỗ một chút để phủ kín vành hốc.
                entry["gd"] = round(100.0 * float(np.median([g[2] for g in got])) * 1.25 / im.width, 1)
            else:
                warn.append("%s: tìm được %d/%d hốc ngọc" % (raw, len(got or []), count))
        out = im.resize((WIDTH, round(im.height * WIDTH / im.width)), Image.LANCZOS)
        dst = os.path.join(OUT, "tag_" + key + ".webp")
        out.save(dst, "WEBP", quality=88, alpha_quality=92, method=5)
        layout["tag_" + key] = entry
        print("%-16s -> tag_%-14s %dx%d %5.1f KB  ngọc %d" % (raw, key, out.width, out.height, os.path.getsize(dst) / 1024.0, len(entry["gx"])))
    # Ảnh không đo được hốc: mượn khoảng cách / cao độ trung vị của các ảnh đo được, xếp đều quanh giữa.
    ok = [e for e in layout.values() if len(e["gx"]) == 4]
    if ok:
        step = float(np.median([(e["gx"][3] - e["gx"][0]) / 3 for e in ok]))
        gy, gd = float(np.median([e["gy"] for e in ok])), float(np.median([e["gd"] for e in ok]))
        for raw, (key, count) in MAP.items():
            e = layout.get("tag_" + key)
            if e and count == 4 and not e["gx"]:
                e.update({"gx": [round(50 + step * (i - 1.5), 1) for i in range(4)], "gy": round(gy, 1), "gd": round(gd, 1), "guess": True})
    with open(os.path.join(HERE, "..", "src", "tag-layout-v3.js"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("/* Tự sinh bởi tools/build-tag-v3.py (đo bảng cảnh giới kiểu mới). Không sửa tay: thay ảnh thì chạy lại tool. */\n")
        fh.write("(function (root) { root.NgaMi = Object.assign(root.NgaMi || {}, { TAG_LAYOUT_V3: ")
        fh.write(json.dumps(layout, ensure_ascii=False, separators=(",", ":")))
        fh.write(" }); }(typeof self !== \"undefined\" ? self : this));\n")
    print("tag-layout-v3.js:", len(layout), "bảng")
    for w in warn:
        print(" - cần xem lại:", w)


if __name__ == "__main__":
    main()
