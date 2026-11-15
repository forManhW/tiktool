/*
 * Renderer DOM cho khung dọc 1080x1920. Vẽ hoàn toàn từ game.getState(), nên dùng được cho cả cửa sổ game và overlay mirror.
 *
 *   const view = NgaMi.createRenderer(document.getElementById("stage"), NgaMi.DEFAULTS, { assetBase: "src/assets/game/" });
 *   view.render(game.getState());   // gọi mỗi khi state đổi (rẻ: chỉ cập nhật phần thay đổi)
 *
 * Mỗi đệ tử: nhân vật theo cảnh giới, tên + thanh tiến độ siêu mảnh ngay dưới tên, bóng thoại khi comment.
 * Không có hào quang chạy liên tục (đã chốt). Hiệu ứng chỉ chạy 1 lần khi có sự kiện.
 */
(function (root, factory) {
  root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  var W = 1080, H = 1920, SPRITE_RATIO = 384 / 480;
  // Màu chữ tên theo đại cảnh giới, lấy theo màu chủ đạo của bộ trang phục (chờ anh duyệt).
  var REALM_COLORS = ["#e9e4d8", "#b6d98f", "#79d6c6", "#8fb3f0", "#cfa9f5", "#e3ecff", "#f4d57e", "#ff8f7f", "#c792ff", "#fff1c2"];

  function el(tag, cls, parent) { var e = document.createElement(tag); if (cls) e.className = cls; if (parent) parent.appendChild(e); return e; }
  function mmss(ms) { var s = Math.max(0, Math.ceil(ms / 1000)); return Math.floor(s / 60) + ":" + String(s % 60).padStart(2, "0"); }
  function shortName(name, max) {
    var t = String(name || "Đạo hữu").trim();
    var a = typeof Intl !== "undefined" && Intl.Segmenter ? Array.from(new Intl.Segmenter("vi", { granularity: "grapheme" }).segment(t), function (x) { return x.segment; }) : Array.from(t);
    if (a.length <= max) return a.join("");
    // Cắt ở GIỮA, giữ cả đầu và đuôi tên: "NguoiChoiThu1" / "NguoiChoiThu2" -> "Nguo…Thu1" / "Nguo…Thu2", không bị giống nhau.
    var tail = Math.max(2, Math.floor((max - 1) / 2)), head = max - 1 - tail;
    return a.slice(0, head).join("") + "…" + a.slice(a.length - tail).join("");
  }

  /** Vị trí từng chỗ ngồi theo thứ hạng: 3 ghế đài, rồi điền kín từng hàng từ trái sang phải. */
  function computeSlots(layout) {
    var slots = layout.podium.map(function (p, i) { return { x: p.x, y: p.y, h: p.h, seat: p.seat, podium: i }; });
    layout.rows.forEach(function (row, r) {
      var step = (layout.rowRight - layout.rowLeft) / row.count;
      for (var j = 0; j < row.count; j++) slots.push({ x: layout.rowLeft + step * (j + 0.5), y: row.y, h: row.h, row: r });
    });
    return slots;
  }

  function createRenderer(stage, config, opts) {
    opts = opts || {};
    var base = opts.assetBase || "src/assets/game/";
    var L = config.layout, slots = computeSlots(L), realms = config.realms;
    var spriteFor = realms.map(function (r) { return r.sprite; });
    var nodes = {}, last = null;

    // Bộ nào chưa có tranh thì dùng bộ gần nhất phía trước đã có.
    var missing = {};
    function resolveSprites() {
      realms.forEach(function (r, i) { var k = i; while (k > 0 && missing[realms[k].sprite]) k--; spriteFor[i] = realms[k].sprite; });
    }
    realms.forEach(function (r) {
      var img = new Image();
      img.onerror = function () {
        console.warn("[NgaMi] thiếu tranh " + r.sprite + ".png, tạm dùng bộ cảnh giới trước");
        missing[r.sprite] = true; resolveSprites();
        Object.keys(nodes).forEach(function (id) { nodes[id].sprite = null; });
        if (last) render(last, true);
      };
      img.src = base + r.sprite + ".png";
    });

    stage.classList.add("nm-stage");
    stage.innerHTML = "";
    // Camera: nền, logo, đài, đệ tử nằm trong camera để có thể "thu màn hình" vào người thăng cấp.
    // Thanh tỉ thí và banner nằm ngoài camera nên luôn đứng yên, đọc được.
    var camera = el("div", "nm-camera", stage);
    // Nền mở rộng (chính ảnh nền, phóng to + làm mờ) phía sau: camera thu vào người ngồi sát mép thì không lộ khoảng trống.
    var bgExt = el("img", "nm-bg-ext", camera); bgExt.src = base + "bg_main_wide.jpg"; bgExt.alt = "";
    // Nền: 1 ảnh rộng duy nhất (rộng hơn khung 2 bên): camera thu vào người ngồi sát mép vẫn thấy cảnh rõ nét.
    // Rộng 1440, lệch trái 179 px để phần giữa nằm đúng khung 1080. Thiếu ảnh thì quay về nền cũ bg_main.png.
    var bg = el("img", "nm-bg nm-bg-wide", camera); bg.alt = "";
    bg.onerror = function () { bg.onerror = null; bg.className = "nm-bg"; bg.src = bgExt.src = base + "bg_main.png"; };
    bg.src = base + "bg_main_wide.jpg";
    var title = el("img", "nm-title", camera); title.src = base + "title_NgaMi.png"; title.alt = "Nga Mi";
    title.style.cssText = "left:" + (W - L.titleWidth) / 2 + "px;top:" + L.titleTop + "px;width:" + L.titleWidth + "px";
    var status = el("div", "nm-status", stage); status.style.top = L.statusTop + "px";
    var bannerBox = el("div", "nm-banner-box", stage); bannerBox.style.top = L.bannerTop + "px";

    // Quái rồng: nằm trong camera, đè lên chỗ logo. Chưa có ảnh thì hiện hình tạm vẽ bằng code.
    // Ảnh rồng ngang 3:2 (900x600). Người tẩu hỏa: khung dọc giữa phía trên.
    var BOSS = Object.assign({ x: 90, y: 80, w: 900, h: 600, crowdShift: 360, panelTop: 598 }, L.boss || {});
    var dragonBox = el("div", "nm-dragon", camera);
    dragonBox.style.cssText = "left:" + BOSS.x + "px;top:" + BOSS.y + "px;width:" + BOSS.w + "px;height:" + BOSS.h + "px";

    // Người tẩu hỏa nhập ma: đứng ở chỗ quái (giữa phía trên), ảnh tauHoaNhapMa.png.
    var possBox = el("div", "nm-possessed", camera);
    possBox.style.cssText = "left:" + (W / 2 - 224) + "px;top:118px;width:448px;height:560px";
    var possBody = el("div", "nm-poss-body", possBox);   // rung nhẹ khi trúng đòn (không phóng to, không tăng sáng)
    el("div", "nm-poss-aura", possBody);
    var possImg = el("img", "nm-poss-img", possBody); possImg.src = base + "tauHoaNhapMa.png"; possImg.alt = "";
    var possTag = el("div", "nm-poss-tag", possBox);
    var possFx = el("div", "nm-dmg-layer", possBox);
    var dragonImg = el("img", "nm-dragon-img", dragonBox); dragonImg.alt = "";
    var dragonHurt = new Image(), hasHurt = false, hasDefeated = false;
    dragonImg.onerror = function () { dragonBox.classList.add("nm-dragon-placeholder"); dragonImg.style.display = "none"; };
    dragonImg.src = base + "boss_dragon.png";
    dragonHurt.onload = function () { hasHurt = true; }; dragonHurt.src = base + "boss_dragon_hurt.png";
    var dragonDead = new Image(); dragonDead.onload = function () { hasDefeated = true; }; dragonDead.src = base + "boss_dragon_defeated.png";
    var placeholder = el("div", "nm-dragon-ph", dragonBox); placeholder.textContent = "QUÁI RỒNG";
    var dmgLayer = el("div", "nm-dmg-layer", dragonBox);

    // Linh Khí Triều Dâng: lớp hạt sáng bay lên phía sau đệ tử + nhãn đếm ngược góc phải.
    el("div", "nm-surge-fx", camera);
    var surgeChip = el("div", "nm-surge-chip", stage);

    // Đệ tử + bệ ngồi gom vào 1 nhóm để cùng lùi xuống khi đánh quái.
    var crowd = el("div", "nm-crowd", camera);

    // Bệ ngồi 3 ghế đài.
    var seatImgs = L.podium.map(function (p, i) {
      var img = el("img", "nm-seat" + (i === 2 ? " nm-flip" : ""), crowd);
      img.src = base + (i === 0 ? "seat_chuongMon.png" : "seat_hoPhap.png"); img.alt = "";
      var w = i === 0 ? p.h * 1.9 : p.h * 1.85, hgt = w * (i === 0 ? 360 / 640 : 276 / 460);
      img.style.cssText = "left:" + (p.x - w / 2) + "px;top:" + (p.y - hgt * 0.62) + "px;width:" + w + "px;height:" + hgt + "px";
      return img;
    });
    var layer = el("div", "nm-layer", crowd);
    // Lớp hiệu ứng (luồng sáng, số sát thương) nằm trên cùng khung cảnh, toạ độ theo khung 1080x1920.
    var fxLayer = el("div", "nm-fx", camera);
    var bossPanel = el("div", "nm-boss-panel", stage); bossPanel.style.top = BOSS.panelTop + "px";
    var wheelBox = el("div", "nm-wheel-box", stage);
    var bossMode = false, possessMode = false, kiepMode = false, duelMode = false;
    // Lôi kiếp: người độ kiếp đứng dưới mây giông phía trên, cả tông môn lùi xuống KIEP.shift.
    var KIEP = Object.assign({ shift: 430, figTop: 400, figH: 340 }, L.kiep || {});
    function crowdShift() { return (bossMode || possessMode) ? BOSS.crowdShift : kiepMode ? KIEP.shift : 0; }
    function setCrowd() { var y = crowdShift(); crowd.style.transform = y ? "translateY(" + y + "px)" : "none"; }

    function nodeFor(seat) {
      var n = nodes[seat.id];
      if (n) return n;
      var rootEl = el("div", "nm-disciple nm-enter", layer);
      // root đặt vị trí (transform translate); body là lớp con để phóng to / rung TẠI CHỖ mà không đẩy lệch vị trí.
      var body = el("div", "nm-body", rootEl);
      // Hào quang sau lưng (bật khi đang tu luyện): quầng sáng + tia sáng xoay + 2 vòng sáng + hạt sáng bay lên.
      // Khi đánh rồng / giải cứu: ai gây sát thương nhiều hơn thì hào quang to và rực hơn (--hs, nm-halo-lv2/3).
      // Bản 3: hào quang = ảnh pháp trận riêng từng đại cảnh giới (ban3/halo) + quầng sáng mờ + hạt sáng bay lên.
      // h-wrap thở (phóng / thu), ảnh bên trong xoay chậm. Mỗi người chỉ 1 vòng hào quang (đánh rồng / giải cứu: to hơn, xoay nhanh hơn).
      var halo = el("div", "nm-halo", body);
      el("i", "h-glow", halo);
      var haloWrap = el("span", "h-wrap", halo);
      var haloImg = el("img", "h-img", haloWrap);
      haloImg.alt = "";
      haloImg.onerror = function () { this.style.visibility = "hidden"; };
      el("i", "h-spark", halo);
      // Pháp trận của pháp bảo: thay chỗ hào quang cảnh giới trong lúc món đang chạy hiệu ứng.
      var itemHalo = el("img", "nm-ihalo", body); itemHalo.alt = "";
      var img = el("img", "nm-sprite", body); img.alt = "";
      var petsEl = el("div", "nm-pets", body);    // linh thú ngồi cạnh chân
      // Trên đầu: chức vị (ghế đài) + bóng thoại. Dưới chân: khung tên bằng ảnh theo cảnh giới (ban3/tag).
      var head = el("div", "nm-head", body);
      var bubble = el("div", "nm-bubble", head);
      var titleEl = el("div", "nm-tag-title", head);
      var tag = el("div", "nm-tag", body);
      var plate = el("div", "nm-plate", tag);
      var realmEl = el("div", "nm-tag-realm", plate);   // chữ trên ruy băng: tên đại cảnh giới (chỉ hiện ở khung lớn)
      var nameEl = el("div", "nm-tag-name", plate);
      var gemsEl = el("div", "nm-gems", plate);         // ngọc ở cạnh dưới: số viên sáng = tiểu cảnh
      n = nodes[seat.id] = { root: rootEl, body: body, grow: 1, img: img, haloImg: haloImg, haloSrc: "", itemHalo: itemHalo, petsEl: petsEl, petSig: "", tag: tag, bubble: bubble, titleEl: titleEl, nameEl: nameEl, realmEl: realmEl, realmIdx: -1,
        plate: plate, gemsEl: gemsEl, tagW: 0, plateSrc: "", gemSig: "", maxChars: 8,
        sprite: null, key: null, pos: null, level: seat.level, name: null, bubbleId: 0 };
      // setTimeout thay vì requestAnimationFrame: cửa sổ bị che / chạy nền thì rAF dừng, nhân vật mới sẽ tàng hình.
      setTimeout(function () { rootEl.classList.remove("nm-enter"); }, 40);
      return n;
    }

    var ladderRealm = [];
    realms.forEach(function (r, i) { r.stages.forEach(function () { ladderRealm.push(i); }); });

    function flash(n, cls) {
      n.root.classList.remove("nm-level-up", "nm-realm-up");
      void n.root.offsetWidth;   // khởi động lại animation
      n.root.classList.add(cls);
    }

    // ---------- Camera: thăng cấp thì thu màn hình vào người đó (người đó ra giữa), giữ một lúc rồi lùi ra ----------
    var CAM = Object.assign({ zoom: 1.8, realmZoom: 2.1, inMs: 600, holdMs: 1600, outMs: 600, centerY: 760, maxWaitMs: 4000 }, L.camera || {});
    var camBusy = false, camNext = null;
    function requestFocus(id, big) {
      // Đang đánh quái: không thu camera vào người (để rồng và thanh máu luôn trong khung), chỉ lóe sáng.
      // Đang tỉ thí: giữ toàn cảnh để thấy rõ hai phe và ai ra đòn.
      if (bossMode || possessMode || kiepMode || duelMode) return;   // đang độ kiếp: giữ toàn cảnh để thấy mây, sét, kết giới
      // Đang thu vào người khác: chỉ giữ 1 người chờ (ưu tiên lên đại cảnh giới), người chờ quá lâu thì bỏ.
      if (camBusy) { if (!camNext || big || !camNext.big) camNext = { id: id, big: big, at: Date.now() }; return; }
      focus(id, big);
    }
    function focus(id, big) {
      var n = nodes[id];
      if (!n || !n.pos) return camDone();
      var s = n.pos, z = big ? CAM.realmZoom : CAM.zoom;
      var px = s.x, py = s.y - s.h * 0.55 + crowdShift();   // giữa thân nhân vật (cộng phần tông môn đã lùi xuống)
      // Người thăng cấp luôn nằm CHÍNH GIỮA: ngang = giữa màn hình, dọc = giữa vùng không bị TikTok che (y ≈ 760).
      // Không chặn theo mép ảnh nền; ngoài ảnh nền là màu trời đêm của khung.
      var tx = W / 2 - px * z, ty = CAM.centerY - py * z;
      camBusy = true;
      camera.style.transition = "transform " + CAM.inMs + "ms cubic-bezier(.25,.8,.25,1)";
      camera.style.transform = "translate(" + tx.toFixed(1) + "px," + ty.toFixed(1) + "px) scale(" + z + ")";
      n.root.classList.add("nm-focus");
      setTimeout(function () {
        camera.style.transition = "transform " + CAM.outMs + "ms ease-in-out";
        camera.style.transform = "none";
        n.root.classList.remove("nm-focus");
        setTimeout(camDone, CAM.outMs);
      }, CAM.inMs + CAM.holdMs);
    }
    function camDone() {
      camBusy = false;
      var next = camNext; camNext = null;
      if (next && Date.now() - next.at <= CAM.maxWaitMs) focus(next.id, next.big);
    }

    // Màu hào quang theo cảnh giới (theo màu trang phục).
    // Màu đậm, bão hoà để hào quang nổi trên nền trời sáng (màu pastel cũ gần như trắng, không thấy).
    var AURA_COLORS = ["#ffc21a", "#5fd61c", "#00cfa8", "#2f7dff", "#a64dff", "#29c4ff", "#ffae00", "#ff4a2a", "#9a2cff", "#ffcc00"];
    // Ảnh pháp trận theo đại cảnh giới (tên theo sprite: halo_luyenKhi.webp ...). Ảnh lỗi thì ẩn, chỉ còn quầng sáng code.
    function haloSrc(realm) { return base + "ban3/halo/halo_" + (realms[realm] || realms[0]).sprite + ".webp"; }
    var PETS = {}; ((config.pets && config.pets.types) || []).forEach(function (t) { PETS[t.id] = t; });
    function petImg(id) { return PETS[id] ? PETS[id].img : "egg"; }
    var ITEMS = {}; (config.items || []).forEach(function (it) { ITEMS[it.id] = it; });
    // Tải sẵn ảnh pháp trận + hào quang: lần đầu dùng món không bị trễ (trước đây pháp trận hiện muộn, lúc đó lẫn với hào quang cảnh giới).
    var preload = [];
    (config.items || []).forEach(function (it) { if (it.fx !== "egg") { var i = new Image(); i.src = base + "ban3/circle/circle_" + it.id + ".webp"; preload.push(i); } });
    realms.forEach(function (r) { var i = new Image(); i.src = base + "ban3/halo/halo_" + r.sprite + ".webp"; preload.push(i); });
    var GROW = (config.duel && config.duel.grow) || [[0, 1]];
    var DUEL_DRAW = (config.duel && config.duel.drawScale) || 1;
    var STRIKE_PEAK = Math.max(1.15, Math.min(1.2, (config.duel && config.duel.strikePeak) || 1.18));
    function growBase(points) {
      if (!(points > 0)) return GROW[0][1];
      for (var i = 1; i < GROW.length; i++) {
        var a = GROW[i - 1], b = GROW[i];
        if (points <= b[0]) return a[1] + (b[1] - a[1]) * (points - a[0]) / (b[0] - a[0]);
      }
      return GROW[GROW.length - 1][1];
    }
    /** Cỡ vẽ khi tỉ thí: nội suy theo đóng góp như cũ rồi nhân hệ số vẽ cuối cùng. Người chưa vào phe giữ nguyên 1. */
    function growFor(points, inDuel) {
      if (!inDuel) return 1;
      return Math.round(growBase(points) * DUEL_DRAW * 1000) / 1000;
    }
    // ---------- Khung tên dưới chân (bản 3) ----------
    // Ảnh khung ngang khoảng 2,42 : 1; vẽ dẹt hơn một chút (2,6 : 1) cho đỡ tốn chỗ giữa các hàng.
    // Vị trí ô tên và hốc ngọc trên từng ảnh khung (src/tag-layout.js, do tools/build-ban3.py đo). Thiếu thì dùng số trung bình.
    var TAG_LAYOUT = (typeof NgaMi !== "undefined" && NgaMi.TAG_LAYOUT) || {};
    var TAG_DEFAULT = { r: 2.7, panel: [6, 47, 94, 79], gx: [36.2, 45.4, 54.5, 63.7], gy: 83, gd: 5.1 };
    var TAG_RATIO = TAG_DEFAULT.r, TAG_MAX_RATIO = 2.97;
    var realmStart = []; (function () { var k = 0; realms.forEach(function (r, i) { realmStart[i] = k; k += r.stages.length; }); })();
    function tagWidth(s) {
      if (s.tagW) return s.tagW;
      if (s.podium === 0) return 240;
      if (s.podium != null) return 200;
      return Math.min(150, (L.rowRight - L.rowLeft) / ((L.rows[s.row] || L.rows[0]).count) + 4);
    }
    /**
     * Khung theo đại cảnh giới (Đại viên mãn dùng khung _dvm riêng), ngọc sáng theo tiểu cảnh:
     * Sơ kỳ 1 / Trung kỳ 2 / Hậu kỳ 3 / Đại viên mãn 4 viên; Luyện Khí có 9 ngọc, tầng N sáng N viên; Chân Tiên sáng đủ 4.
     * Đổi đại cảnh giới hoặc lên Đại viên mãn: khung lật sang ảnh mới.
     */
    function updatePlate(n, seat) {
      var ri = Math.max(0, Math.min(realms.length - 1, seat.realm || 0)), r = realms[ri];
      var st = Math.max(0, Math.min(r.stages.length - 1, (seat.level || 0) - realmStart[ri]));
      var dvm = r.stages.length === 4 && st === 3, nine = r.stages.length === 9;
      var key = "tag_" + r.sprite + (dvm ? "_dvm" : ""), src = base + "ban3/tag/" + key + ".webp";
      var lay = TAG_LAYOUT[key] || TAG_DEFAULT;
      // Kích thước khung theo đúng tỉ lệ ảnh; ô tên đặt đúng ô kem của ảnh; cỡ chữ theo chiều cao ô kem.
      var size = n.tagW + "x" + key;
      if (n.plateSize !== size) {
        n.plateSize = size;
        // Giới hạn tỉ lệ quá dẹt; vị trí ô tên và ngọc tính theo % kích thước khung.
        var th = n.tagW / Math.min(lay.r, TAG_MAX_RATIO), p = lay.panel, ph = th * (p[3] - p[1]) / 100, pw = n.tagW * (p[2] - p[0]) / 100;
        n.tag.style.height = th.toFixed(0) + "px";
        // Tâm ô kem cách đỉnh khung p_mid %: dời khung lên để ô tên nằm ngay dưới chân (chữ cảnh giới đè lên đệm ngồi).
        n.tag.style.top = "calc(100% - " + (th * p[1] / 100 - 4).toFixed(0) + "px)";
        // Giảm nhẹ cỡ chữ và giới hạn theo chiều cao ô kem để dấu tiếng Việt không bị cắt.
        var fpx = Math.max(1, Math.floor(Math.min(ph * 0.95, Math.max(11, Math.min(n.root.classList.contains("nm-podium") ? 27 : 24, n.tagW * 0.13)))));
        n.tag.style.fontSize = fpx + "px";
        var ns = n.nameEl.style;
        ns.left = p[0] + 2 + "%"; ns.right = (100 - p[2] + 2) + "%"; ns.top = p[1] + "%"; ns.height = (p[3] - p[1]) + "%";
        var mc = Math.max(6, Math.min(16, Math.floor(pw * 0.92 / (fpx * 0.6))));
        if (mc !== n.maxChars) { n.maxChars = mc; n.name = shortName(seat.name, mc); n.nameEl.textContent = n.name; }
      }
      if (n.plateSrc !== src) {
        var first = !n.plateSrc;
        n.plateSrc = src;
        n.plate.style.backgroundImage = "url(\"" + src + "\")";
        n.plate.style.setProperty("--tagimg", "url(\"" + src + "\")");
        if (!first) { n.plate.classList.remove("nm-plate-flip"); void n.plate.offsetWidth; n.plate.classList.add("nm-plate-flip"); }
      }
      n.root.classList.toggle("nm-dvm", dvm || ri === realms.length - 1);   // Đại viên mãn, Chân Tiên: vệt sáng chạy ngang khung
      n.root.classList.toggle("nm-houky", r.stages.length === 4 && st === 2);
      var lit = nine ? st + 1 : r.stages.length === 1 ? 4 : st + 1, slots = nine ? 9 : 4;
      var sig = slots + ":" + lit + ":" + key;
      if (n.gemSig !== sig) {
        var grew = n.gemSig && n.gemSig.split(":")[0] == slots && Number(n.gemSig.split(":")[1]) < lit;
        n.gemSig = sig;
        var html = "";
        for (var i = 0; i < slots; i++) html += '<i class="' + (i < lit ? "on" : "") + (grew && i === lit - 1 ? " nm-gem-new" : "") + '" style="left:' +
          (lay.gx[i] != null ? lay.gx[i] : 50).toFixed(1) + "%;top:" + lay.gy + "%;width:" + lay.gd + '%"></i>';
        n.gemsEl.className = "nm-gems " + (nine ? "nm-gems9" : "nm-gems4");
        n.gemsEl.innerHTML = html;
        n.gemsEl.style.setProperty("--gem", AURA_COLORS[ri] || AURA_COLORS[0]);
      }
    }
    // Tỉ thí: hàng sau luôn vẽ đè hàng trước (không xét độ to) để bảng tên hàng sau không bị người to ở hàng trước che.
    function zFor(n, s) { return 10 + Math.round(s ? s.y : 0) + (s && s.duel ? 0 : Math.round(((n.grow || 1) - 1) * 3000)); }
    /** Đặt nhân vật vào chỗ s ({x, y(chân), h, podium?, seat?}); key đổi thì mới dời chỗ. */
    function place(n, seat, s, key) {
      var hgt = s.h, wid = hgt * SPRITE_RATIO;
      if (n.key !== key) {
        n.key = key; n.pos = s;
        n.root.style.transform = "translate(" + (s.x - wid / 2).toFixed(1) + "px," + (s.y - hgt).toFixed(1) + "px)";
        n.root.style.width = wid + "px";
        n.root.style.height = hgt + "px";
        n.root.style.setProperty("--h", hgt.toFixed(0) + "px");
        n.root.style.zIndex = String(zFor(n, s));
        // Khung tên dưới chân: bề ngang theo chỗ (ghế đài 240 / 200, hàng thường vừa 1 chỗ ngồi, tỉ thí theo ô).
        // Chữ tên to theo bề ngang khung; số ký tự tối đa để tên nằm gọn trong ô giữa khung.
        // Chiều cao, cỡ chữ, ô tên đặt trong updatePlate theo ảnh khung của cảnh giới.
        var tw = tagWidth(s);
        n.tag.style.width = tw.toFixed(0) + "px";
        n.tagW = tw; n.plateSize = "";
        n.duelSpot = !!s.duel;
        n.root.classList.toggle("nm-podium", s.podium != null);
        n.titleEl.textContent = s.seat || "";
        n.name = null;
      }
      var sprite = spriteFor[seat.realm] || spriteFor[0];
      if (n.sprite !== sprite) { n.sprite = sprite; n.img.src = base + sprite + ".png"; }
      // Tên dài: cắt giữa, giữ đuôi (Thu1 / Thu2). Tỉ thí 2 cột rộng chỗ nên giữ được dài hơn.
      var label = shortName(seat.name, n.maxChars);
      if (n.name !== label) { n.name = label; n.nameEl.textContent = label; }
      n.tag.style.setProperty("--realm", REALM_COLORS[seat.realm] || REALM_COLORS[0]);
      // Hào quang: màu chủ đạo của cảnh giới, sáng khi vừa được cộng tu vi (game tắt sau 1 phút 30 giây).
      n.body.style.setProperty("--aura", AURA_COLORS[seat.realm] || AURA_COLORS[0]);
      if (n.realmIdx !== seat.realm) {
        var firstHalo = n.realmIdx < 0;
        n.realmIdx = seat.realm; n.realmEl.textContent = (realms[seat.realm] || realms[0]).name;
        var hsrc = haloSrc(seat.realm);
        if (n.haloSrc !== hsrc) {
          n.haloSrc = hsrc; n.haloImg.src = hsrc;
          // Lên đại cảnh giới: pháp trận mới nở ra, xoay nhanh 1 vòng rồi chậm lại.
          if (!firstHalo) { n.root.classList.remove("nm-halo-new"); void n.root.offsetWidth; n.root.classList.add("nm-halo-new"); }
        }
      }
      updatePlate(n, seat);
      // Mức hào quang: 0 tắt; 1 đang tu luyện; đánh rồng / giải cứu: hạng 1 = 3, hạng 2–3 = 2, còn lại 1 (to dần theo % so với hạng 1).
      var f = seat.fight, lv = f ? (f.rank === 1 ? 3 : f.rank <= 3 ? 2 : 1) : seat.aura ? 1 : 0;
      // Hào quang ảnh đã to sẵn (1,4 lần chiều cao người): hạng 1 to 1,45 lần, hạng 2–3 khoảng 1,2–1,3, còn lại 1–1,15.
      var hs = f ? (f.rank === 1 ? 1.45 : f.rank <= 3 ? 1.2 + 0.1 * f.pct : 1 + 0.15 * f.pct) : 1;
      if (seat.realm >= realms.length - 1 && lv) lv = Math.max(lv, 2);   // Chân Tiên: pháp trận xoay nhanh hơn
      if (n.haloLv !== lv || n.haloHs !== hs) {
        n.haloLv = lv; n.haloHs = hs;
        n.root.classList.toggle("nm-aura-on", lv > 0);
        n.root.classList.toggle("nm-halo-lv2", lv >= 2);
        n.root.classList.toggle("nm-halo-lv3", lv >= 3);
        n.body.style.setProperty("--hs", hs.toFixed(2));
      }
      var petSig = (seat.pets || []).map(function (p) { return p.id + p.stars; }).join(",");
      if (petSig !== n.petSig) {
        n.petSig = petSig;
        n.petsEl.innerHTML = (seat.pets || []).map(function (p) {
          return '<span class="nm-pet"><img src="' + base + petImg(p.id) + '.png" alt=""><b>' + "★".repeat(p.stars) + "</b></span>";
        }).join("");
      }
      n.tag.dataset.side = seat.side || 0;
      n.root.style.setProperty("--sidec", seat.side === 1 ? "#4fd1bd" : seat.side === 2 ? "#f06b9a" : "#ffe9a8");
      // Tỉ thí: góp càng nhiều càng to (tại chỗ, neo ở chân), người to đè lên người nhỏ.
      var g = growFor(seat.duelPoints || 0, seat.side === 1 || seat.side === 2);
      if (g !== n.grow) { n.grow = g; n.body.style.setProperty("--grow", g.toFixed(3)); if (!n.root.classList.contains("nm-strike")) n.root.style.zIndex = String(zFor(n, n.pos)); }
      n.root.classList.toggle("nm-fading", !!seat.fading);
      if (seat.bubble && seat.bubble.id !== n.bubbleId) { n.bubbleId = seat.bubble.id; n.bubble.textContent = seat.bubble.text; n.bubble.classList.add("nm-show"); }
      else if (!seat.bubble && n.bubbleId) { n.bubbleId = 0; n.bubble.classList.remove("nm-show"); }
      if (seat.level > n.level) {
        var bigUp = ladderRealm[seat.level] > ladderRealm[n.level];
        flash(n, bigUp ? "nm-realm-up" : "nm-level-up");
        requestFocus(seat.id, bigUp);   // thăng cấp: camera thu vào người này
      }
      n.level = seat.level;
    }

    function renderStatus(d) {
      if (d.phase !== "duel") delete status.dataset.mode;
      var s1 = d.sides ? d.sides[0].name : "", s2 = d.sides ? d.sides[1].name : "";
      if (d.phase === "duel") {
        var total = d.scores[1] + d.scores[2], pct = total ? (100 * d.scores[1] / total) : 50;
        // Dựng khung tỉ thí 1 lần rồi chỉ cập nhật số, để thanh tiến độ trượt mượt và hiệu ứng rung không bị cắt ngang.
        if (status.dataset.mode !== "duel") {
          status.dataset.mode = "duel";
          status.className = "nm-status nm-duel";
          status.innerHTML =
            '<div class="nm-duel-head"><span>TỈ THÍ</span><em>Bình luận <b class="s1">1</b> hoặc <b class="s2">2</b> để chọn phe</em><b class="nm-duel-time"></b></div>' +
            '<div class="nm-tug"><i></i></div>' +
            '<div class="nm-duel-sides"><span class="s1"></span><span class="s2"></span></div>';
        }
        status.querySelector(".nm-duel-time").textContent = mmss(d.remainingMs);
        status.querySelector(".nm-tug i").style.width = pct.toFixed(1) + "%";
        status.querySelector(".nm-duel-sides .s1").textContent = s1 + " · " + d.scores[1].toLocaleString("vi-VN");
        status.querySelector(".nm-duel-sides .s2").textContent = d.scores[2].toLocaleString("vi-VN") + " · " + s2;

      } else if (d.phase === "result" && d.lastResult) {
        var r = d.lastResult;
        status.className = "nm-status nm-result";
        status.innerHTML = '<div class="nm-result-title">' + (r.winner ? "Phe " + (r.winner === 1 ? s1 : s2) + " chiến thắng" : r.empty ? "Chưa có đạo hữu nào chọn phe" : "Hai phe hòa nhau") + "</div>" +
          '<div class="nm-duel-sides"><span class="s1">' + s1 + " · " + r.scores[1].toLocaleString("vi-VN") + '</span><span class="s2">' + r.scores[2].toLocaleString("vi-VN") + " · " + s2 + "</span></div>";
        if (r.mvp) { var m = el("div", "nm-duel-hint", status); m.textContent = "Công đầu: " + r.mvp; }
      } else if (d.enabled || (bossInfo && bossInfo.enabled)) {
        // Chỉ hiện đếm ngược của sự kiện tới gần nhất (tỉ thí HOẶC quái rồng), không hiện cả hai cùng lúc.
        var duelIn = d.enabled && d.nextInMs ? d.nextInMs : null;
        var bossIn = bossInfo && bossInfo.enabled && bossInfo.nextInMs != null ? bossInfo.nextInMs : null;
        var showBoss = bossIn != null && (duelIn == null || bossIn < duelIn);
        status.className = "nm-status nm-next";
        status.innerHTML = showBoss ? "Quái rồng sau <b>" + mmss(bossIn) + "</b>" : duelIn != null ? "Tỉ thí sau <b>" + mmss(duelIn) + "</b>" : "";
      } else { status.className = "nm-status"; status.innerHTML = ""; }
    }
    var bossInfo = null;

    // ---------- Quái rồng ----------
    var lastHitId = 0, lastAttackId = 0, bossPhase = "idle";
    function renderBoss(b) {
      bossInfo = b;
      var active = !!b && b.phase !== "idle";
      if (active !== bossMode) {
        bossMode = active;
        stage.classList.toggle("nm-boss-mode", active);
        setCrowd();
        if (active) { dragonBox.classList.remove("nm-dragon-dead"); dragonImg.src = base + "boss_dragon.png"; lastHitId = (b.hits && b.hits.length) ? b.hits[b.hits.length - 1].id : 0; lastAttackId = (b.attacks && b.attacks.length) ? b.attacks[b.attacks.length - 1].id : 0; }
      }
      if (!active) { if (!possessMode) bossPanel.innerHTML = ""; bossPhase = "idle"; return; }
      if (b.phase === "result" && bossPhase !== "result") {
        dragonBox.classList.add("nm-dragon-dead");
        if (hasDefeated) dragonImg.src = dragonDead.src;
      }
      bossPhase = b.phase;
      var pct = b.maxHp ? Math.max(0, 100 * b.hp / b.maxHp) : 0;
      bossPanel.innerHTML = b.phase === "result"
        ? '<div class="nm-boss-head"><b>' + escapeHtml(b.name) + ' đã bị tiêu diệt!</b></div>' + (b.lastResult ? '<div class="nm-boss-top">Công đầu: <b>' + escapeHtml(b.lastResult.name) + "</b> · " + b.lastResult.damage.toLocaleString("vi-VN") + " sát thương</div>" : "")
        : '<div class="nm-boss-head"><b>' + escapeHtml(b.name) + '</b><span>' + b.hp.toLocaleString("vi-VN") + " / " + b.maxHp.toLocaleString("vi-VN") + "</span></div>" +
          '<div class="nm-hp"><i style="width:' + pct.toFixed(2) + '%"></i></div>' + '<div class="nm-boss-hint">Sử dụng pháp bảo để tấn công</div>';
      (b.hits || []).forEach(function (h) {
        if (h.id <= lastHitId) return;
        lastHitId = h.id;
        var from = discipleCenter(h.userId);
        strike(h.userId);
        if (from) swordFly(from, DRAGON_CENTER, "gold", function () { showHit(h.amount); });
        else showHit(h.amount);
      });
      // Rồng đánh trả: đòn thường = 1 luồng lửa xuống 1 người; phun lửa = sóng lửa quét xuống cả tông môn.
      (b.attacks || []).forEach(function (a) {
        if (a.id <= lastAttackId) return;
        lastAttackId = a.id;
        if (a.kind === "fire") fireBreath(a.hits);
        else a.hits.forEach(function (h) { dragonStrike(h.id, h.amount); });
      });
    }
    function hitByDragon(id, amount) {
      var to = discipleCenter(id);
      hurtDisciple(id, amount);
      if (to && amount > 0) popNumber(to.x, to.y - 40, "-" + amount.toLocaleString("vi-VN"), "nm-pop-red");
    }
    function dragonStrike(id, amount) {
      var to = discipleCenter(id);
      if (to) beam(DRAGON_CENTER, to, "fire", function () { hitByDragon(id, amount); });
      else hurtDisciple(id, amount);
    }
    function fireBreath(hits) {
      while (fxLayer.children.length > 40) fxLayer.firstChild.remove();
      var wave = el("div", "nm-firewave", fxLayer);
      wave.style.top = DRAGON_CENTER.y + "px";
      setTimeout(function () { wave.remove(); }, 1300);
      hits.forEach(function (h) {
        var to = discipleCenter(h.id);
        // người ở hàng trên trúng trước, hàng dưới trúng sau theo sóng lửa
        var delay = to ? Math.max(0, (to.y - DRAGON_CENTER.y) / 1400 * 700) : 300;
        setTimeout(function () { hitByDragon(h.id, h.amount); }, delay);
      });
    }
    var hitTimer = null;
    function showHit(amount) {
      dragonImg.classList.remove("nm-shake-soft"); void dragonImg.offsetWidth; dragonImg.classList.add("nm-shake-soft");
      if (hasHurt && bossPhase === "fight") { dragonImg.src = dragonHurt.src; clearTimeout(hitTimer); hitTimer = setTimeout(function () { if (bossPhase === "fight") dragonImg.src = base + "boss_dragon.png"; }, 350); }
      popNumber(DRAGON_CENTER.x + (Math.random() - 0.5) * 360, DRAGON_CENTER.y + (Math.random() - 0.3) * 200, "-" + amount.toLocaleString("vi-VN"), "nm-pop-gold");
    }
    // ---------- Tẩu hỏa nhập ma ----------
    var lastPossHitId = 0, possPhase = "idle";
    function renderPossession(ps) {
      var active = !!ps && ps.phase !== "idle";
      if (active !== possessMode) {
        possessMode = active;
        stage.classList.toggle("nm-possess-mode", active);
        setCrowd();
        if (active) { possBox.classList.remove("nm-poss-freed"); lastPossHitId = (ps.hits && ps.hits.length) ? ps.hits[ps.hits.length - 1].id : 0; }
      }
      if (!active) { possPhase = "idle"; if (!bossMode) bossPanel.innerHTML = ""; return; }
      if (ps.phase === "result" && possPhase !== "result") possBox.classList.add("nm-poss-freed");
      possPhase = ps.phase;
      possTag.innerHTML = '<div class="nm-poss-title">TẨU HỎA NHẬP MA</div><div class="nm-tag-name">' + escapeHtml(ps.name) + "</div>";
      var pct = ps.maxHp ? Math.max(0, 100 * ps.hp / ps.maxHp) : 0;
      bossPanel.innerHTML = ps.phase === "result"
        ? '<div class="nm-boss-head"><b>' + escapeHtml(ps.name) + ' đã được giải cứu!</b></div>' + (ps.lastResult ? '<div class="nm-boss-top">' + ps.lastResult.rescuers + " người góp sức được x2 tu vi đã góp</div>" : "")
        : '<div class="nm-boss-head"><b>Giải cứu ' + escapeHtml(ps.name) + '</b><span>' + ps.hp.toLocaleString("vi-VN") + " / " + ps.maxHp.toLocaleString("vi-VN") + "</span></div>" +
          '<div class="nm-hp nm-hp-rescue"><i style="width:' + pct.toFixed(2) + '%"></i></div>' + '<div class="nm-boss-hint">Sử dụng pháp bảo để giải cứu</div>';
      (ps.hits || []).forEach(function (h) {
        if (h.id <= lastPossHitId) return;
        lastPossHitId = h.id;
        if (h.kind === "rescue") {
          // Người giải cứu đánh người tẩu hỏa: phóng to, luồng sáng vàng bay lên, số "-X" nảy ra ở người tẩu hỏa.
          var from = discipleCenter(h.userId);
          strike(h.userId);
          var hitPoss = function () {
            possBody.classList.remove("nm-shake-soft"); void possBody.offsetWidth; possBody.classList.add("nm-shake-soft");
            popNumber(POSS_CENTER.x + (Math.random() - 0.5) * 260, POSS_CENTER.y + (Math.random() - 0.4) * 240, "-" + h.amount.toLocaleString("vi-VN"), "nm-pop-gold");
          };
          if (from) swordFly(from, POSS_CENTER, "gold", hitPoss); else hitPoss();
        } else {
          // Người tẩu hỏa đánh 1 đệ tử: luồng sáng tím đỏ bay xuống, người trúng đòn lóe đỏ và mất tu vi.
          var to = discipleCenter(h.targetId), amt = h.amount, tid = h.targetId;
          if (to) beam(POSS_CENTER, to, "dark", function () { hurtDisciple(tid, amt); popNumber(to.x, to.y - 40, "-" + amt.toLocaleString("vi-VN"), "nm-pop-red"); });
          else hurtDisciple(tid, amt);
        }
      });
    }
    function floatNumber(layerEl, text, cls) {
      if (layerEl.children.length >= 8) layerEl.firstChild.remove();
      var n = el("div", cls, layerEl);
      n.textContent = text;
      n.style.left = (20 + Math.random() * 60).toFixed(0) + "%";
      n.style.top = (25 + Math.random() * 40).toFixed(0) + "%";
      setTimeout(function () { n.remove(); }, 1300);
    }
    // Người bị người tẩu hỏa đánh: lóe đỏ, số tu vi bị trừ bay lên trên đầu.
    function hurtDisciple(id, amount) {
      var n = nodes[id];
      if (!n) return;
      n.root.classList.remove("nm-hurt"); void n.root.offsetWidth; n.root.classList.add("nm-hurt");
      // Gỡ sau khi rung xong, để lần sau ra đòn (phóng to) không bị hiệu ứng rung còn treo đè lên.
      clearTimeout(n.hurtTimer);
      n.hurtTimer = setTimeout(function () { n.root.classList.remove("nm-hurt"); }, 400);
    }

    // ---------- Hiệu ứng đòn đánh dùng chung ----------
    // Điểm trúng đòn: phần thân trên (banner lúc sự kiện nằm ở y khoảng 600, không che số).
    var DRAGON_CENTER = { x: BOSS.x + BOSS.w / 2, y: BOSS.y + BOSS.h * 0.36 }, POSS_CENTER = { x: W / 2, y: 118 + 200 };
    /** Giữa thân đệ tử (toạ độ khung, đã cộng phần tông môn lùi xuống). */
    function discipleCenter(id) {
      var n = nodes[id];
      if (!n || !n.pos) return null;
      var sl = n.pos;
      return { x: sl.x, y: sl.y - sl.h * 0.5 + crowdShift() };
    }
    /** Người ra đòn phóng to 1,5 lần trong 0,5 giây. Tỉ thí (side 1/2): lấy đà, lao ngắn về phía đối thủ, đỉnh phóng nhỏ hơn. */
    function strike(id, duelSide) {
      var n = nodes[id];
      if (!n) return;
      n.root.classList.remove("nm-strike", "nm-strike-duel"); void n.root.offsetWidth; n.root.classList.add("nm-strike");
      if (duelSide === 1 || duelSide === 2) {
        n.root.classList.add("nm-strike-duel");
        n.body.style.setProperty("--lunge", duelSide === 1 ? "1" : "-1");   // phe 1 đứng bên trái, lao sang phải
        n.body.style.setProperty("--peak", String(STRIKE_PEAK));
      }
      n.root.style.zIndex = "9000";
      clearTimeout(n.strikeTimer);
      n.strikeTimer = setTimeout(function () { n.root.classList.remove("nm-strike", "nm-strike-duel"); if (n.pos) n.root.style.zIndex = String(zFor(n, n.pos)); }, 520);
    }
    /** Luồng sáng bay từ `from` tới `to` (khoảng 0,4 giây), tới nơi thì gọi onArrive. */
    function beam(from, to, kind, onArrive) {
      while (fxLayer.children.length > 40) fxLayer.firstChild.remove();
      var dx = to.x - from.x, dy = to.y - from.y, len = Math.sqrt(dx * dx + dy * dy), ang = Math.atan2(dy, dx) * 180 / Math.PI;
      var line = el("div", "nm-beam nm-beam-" + kind, fxLayer);
      line.style.cssText = "left:" + from.x + "px;top:" + from.y + "px;width:" + len.toFixed(0) + "px;transform:rotate(" + ang.toFixed(1) + "deg)";
      var orb = el("div", "nm-orb nm-orb-" + kind, fxLayer);
      orb.style.left = from.x + "px"; orb.style.top = from.y + "px";
      setTimeout(function () { orb.style.transform = "translate(" + dx.toFixed(0) + "px," + dy.toFixed(0) + "px)"; }, 20);
      setTimeout(function () { orb.remove(); if (onArrive) onArrive(); }, 420);
      setTimeout(function () { line.remove(); }, 900);
    }
    function trimFx() { while (fxLayer.children.length > 60) fxLayer.firstChild.remove(); }
    /**
     * Vệt sáng bay từ `from` tới `to` khoảng 0,42 giây, theo màu của phe.
     * (gold: đánh rồng / giải cứu, s1 / s2: màu phe tỉ thí). Tới nơi thì gọi onArrive.
     */
    var SWORD_RGB = { gold: "255,206,84", s1: "79,209,189", s2: "240,107,154" };
    function swordFly(from, to, kind, onArrive) {
      trimFx();
      var dx = to.x - from.x, dy = to.y - from.y, len = Math.sqrt(dx * dx + dy * dy), ang = Math.atan2(dy, dx) * 180 / Math.PI;
      var wrap = el("div", "nm-sword", fxLayer);
      wrap.style.cssText = "left:" + from.x.toFixed(0) + "px;top:" + from.y.toFixed(0) + "px;transform:rotate(" + ang.toFixed(1) + "deg)";
      wrap.style.setProperty("--c", SWORD_RGB[kind] || SWORD_RGB.gold);
      var mover = el("div", "nm-sword-move", wrap);
      // Vệt sáng phép thuật: đuôi sáng mờ dần + quả cầu linh lực ở đầu (không dùng hình kiếm).
      el("div", "nm-sword-trail", mover);
      el("div", "nm-sword-head", mover);
      setTimeout(function () { mover.style.transform = "translateX(" + Math.max(0, len - 30).toFixed(0) + "px)"; }, 20);
      setTimeout(function () {
        wrap.classList.add("nm-sword-done"); if (onArrive) onArrive();
        var hit = el("div", "nm-magic-hit", fxLayer); hit.style.cssText = "left:" + to.x.toFixed(0) + "px;top:" + to.y.toFixed(0) + "px";
        hit.style.setProperty("--c", SWORD_RGB[kind] || SWORD_RGB.gold);
        setTimeout(function () { hit.remove(); }, 520);
      }, 440);
      setTimeout(function () { wrap.remove(); }, 760);
    }

    // ---------- Pháp bảo: hiệu ứng tại chỗ người tặng ----------
    var lastItemId = -1, lastHatchId = -1, eggFx = {};
    function renderItems(list) {
      list = list || [];
      if (lastItemId < 0) { lastItemId = list.length ? list[list.length - 1].id : 0; return; }   // lần vẽ đầu: không chạy lại hiệu ứng cũ
      list.forEach(function (fx) { if (fx.id > lastItemId) { lastItemId = fx.id; playItem(fx); } });
    }
    function itemBox(fx) {
      var c = discipleCenter(fx.userId), n = nodes[fx.userId];
      if (!c || !n) return null;
      var h = n.pos.h * (n.grow || 1), size;
      // Mọi lớp của món (ảnh, chữ, hạt sáng) chung 1 tâm với pháp trận sau lưng: ngực nhân vật (40% từ đỉnh, tính cả phóng to).
      c = { x: c.x, y: n.pos.y - h * 0.6 + crowdShift() };
      size = Math.max(80, Math.min(240, h * 0.85));
      trimFx();
      var box = el("div", "nm-itemfx nm-fx-" + fx.fx, fxLayer);
      box.style.cssText = "left:" + c.x.toFixed(0) + "px;top:" + c.y.toFixed(0) + "px";
      box.style.setProperty("--s", size.toFixed(0) + "px");
      box.style.setProperty("--h", h.toFixed(0) + "px");
      box.style.setProperty("--c", fx.color);
      return { box: box, c: c, h: h, size: size, n: n };
    }
    function itemImg(box, src, cls) { var i = el("img", "nm-item-img " + (cls || ""), box); i.src = base + src + ".png"; i.alt = ""; return i; }
    function playItem(raw) {
      var def = ITEMS[raw.item] || { fx: "rise", img: "egg", color: "#ffffff" };
      var fx = { userId: raw.userId, fx: def.fx, color: def.color, id: raw.id };
      var b = itemBox(fx);
      if (!b) {
        // Người đang độ kiếp không ngồi ở hàng dưới: pháp trận của món hiện quanh người đó trên mây giông.
        if (last && last.kiep && last.kiep.targetId === raw.userId && def.fx !== "egg") {
          var kt = TIER_MS[def.tier || 1] || TIER_MS[1];
          kiepIhalo.className = "nm-ihalo nm-kiep-ihalo";
          kiepIhalo.src = base + "ban3/circle/circle_" + raw.item + ".webp";
          kiepIhalo.style.setProperty("--t", kt + "ms");
          kiepIhalo.style.setProperty("--is", "1");
          void kiepIhalo.offsetWidth;
          kiepIhalo.className = "nm-ihalo nm-kiep-ihalo nm-ih-on nm-ih-" + raw.item;
          clearTimeout(kiepIhalo.timer);
          kiepIhalo.timer = setTimeout(function () { kiepIhalo.className = "nm-ihalo nm-kiep-ihalo"; }, kt + 150);
        }
        return;
      }
      // Món càng đắt hiệu ứng càng to và càng lâu (tier trong config.items).
      var tier = def.tier || 1, dur = TIER_MS[tier] || TIER_MS[1];
      b.box.style.setProperty("--t", dur + "ms");
      // Cỡ tối thiểu theo bậc: người ngồi hàng nhỏ tặng món đắt vẫn thấy hiệu ứng to.
      var minSize = { 3: 140, 4: 190 }[tier] || 0, minH = { 3: 150, 4: 210 }[tier] || 0;
      if (b.size < minSize) { b.size = minSize; b.box.style.setProperty("--s", minSize + "px"); }
      if (b.h < minH) { b.h = minH; b.box.style.setProperty("--h", minH + "px"); }
      b.box.classList.add("nm-tier" + tier);
      flash(b.n, "nm-level-up");
      if (raw.count > 1) { var cnt = el("div", "nm-item-count", b.box); cnt.textContent = "x" + raw.count; }
      var life = dur + 300;
      if (tier >= 2) sparks(b.box, tier >= 4 ? 18 : tier >= 3 ? 12 : 8);
      // Pháp trận riêng của món (ban3/circle) bao quanh người dùng, thay hào quang cảnh giới trong lúc món chạy.
      if (def.fx !== "egg") itemCircle(b.n, raw.item, tier, dur, raw.count > 1);
      if (def.fx === "swords") {
        // Phi Kiếm Trận: 8 thanh kiếm to xoay vòng quanh người, rồi hợp lại thành cột sáng.
        var ring = el("div", "nm-sword-ring", b.box);
        for (var k = 0; k < 8; k++) {
          var arm = el("div", "nm-sword-arm", ring); arm.style.transform = "rotate(" + (k * 45) + "deg)";
          itemImg(arm, "fx_sword", "nm-ring-sword");
        }
        el("div", "nm-item-pillar", b.box);
      } else if (def.fx === "scroll") {
        // Bí Tịch: chữ vàng bay vòng quanh nhân vật.
        itemImg(b.box, def.img, "");
        // 8 chữ Hán hiện quanh người, bay vòng theo pháp trận; tới 60% thời gian thì lần lượt lao vào ngực (mỗi chữ lóe 1 cái).
        var gring = el("div", "nm-glyph-ring", b.box);
        ["劍", "氣", "靈", "心", "功", "法", "神", "力"].forEach(function (ch, i) {
          var arm = el("div", "nm-glyph-arm", gring); arm.style.transform = "rotate(" + (i * 45) + "deg)";
          var g = el("div", "nm-scroll-glyph", arm); g.textContent = ch; g.style.setProperty("--a", (i * 45) + "deg");
          setTimeout(function () {
            g.classList.add("nm-glyph-in");
            setTimeout(function () { flash(b.n, "nm-level-up"); }, 420);
          }, dur * 0.58 + i * 110);
        });
      } else if (def.fx === "orb") {
        // Long Châu: tia sáng xoay, 3 vòng sóng, chớp sáng toàn màn, ngọc lớn tỏa hào quang.
        el("div", "nm-orb-rays", b.box);
        el("div", "nm-orb-aura", b.box);
        itemImg(b.box, def.img, "");
        [0, 1, 2].forEach(function (i) { var r = el("div", "nm-orb-wave", b.box); r.style.animationDelay = "calc(var(--t) * " + (0.3 + i * 0.18).toFixed(2) + ")"; });
        [0, 1, 2].forEach(function (i) {
          setTimeout(function () { var f = el("div", "nm-screen-flash", fxLayer); setTimeout(function () { f.remove(); }, 380); }, dur * (0.3 + i * 0.18));
        });
      } else if (def.fx === "egg") {
        // Trứng rơi xuống, rung, nứt (egg_crack.png), lóe sáng; linh thú hiện ra khi game báo nở (renderHatches).
        var egg = itemImg(b.box, "egg", "nm-egg");
        eggFx[raw.id] = { box: b.box, egg: egg };
        setTimeout(function () { egg.src = base + "egg_crack.png"; egg.classList.add("nm-egg-crack"); }, Math.max(600, hatchMs - 1200));
        life = hatchMs + 400 + Math.min(9, (raw.count || 1) - 1) * 700;
      } else {
        itemImg(b.box, def.img, "");
        if (def.fx === "pour") el("div", "nm-pour-stream", b.box);
        if (def.fx === "grow" || def.fx === "sway") el("div", "nm-item-mist", b.box);
      }
      el("div", "nm-item-burst", b.box);
      if (tier >= 3) { var b2 = el("div", "nm-item-burst nm-burst2", b.box); b2.style.animationDelay = "calc(var(--t) * .62)"; }
      setTimeout(function () {
        popNumber(b.c.x, b.c.y - b.h * 0.65, "+" + raw.amount.toLocaleString("vi-VN"), "nm-pop-gold " + (tier >= 4 ? "nm-pop-big" : "nm-pop-item"));
      }, dur * 0.45);
      setTimeout(function () { b.box.remove(); delete eggFx[raw.id]; }, life);
    }
    /**
     * Pháp trận pháp bảo sau lưng người dùng: nở ra, xoay (kiểu chuyển động theo món: nm-ih-<id>), tan khi món tan.
     * Trong lúc đó hào quang cảnh giới ẩn đi (nm-item-on). Tặng liên tiếp thì chạy lại từ đầu với món mới.
     */
    function itemCircle(n, id, tier, dur, combo) {
      var ih = n.itemHalo;
      ih.className = "nm-ihalo";
      ih.onerror = function () { ih.className = "nm-ihalo"; n.root.classList.remove("nm-item-on"); };
      ih.src = base + "ban3/circle/circle_" + id + ".webp";
      ih.style.setProperty("--t", dur + "ms");
      ih.style.setProperty("--is", ({ 1: 1, 2: 1.1, 3: 1.22, 4: 1.4 }[tier] || 1) * (combo ? 1.12 : 1));
      void ih.offsetWidth;
      ih.className = "nm-ihalo nm-ih-on nm-ih-" + id + (combo ? " nm-ih-combo" : "");
      n.root.classList.add("nm-item-on");
      // Người đang dùng món nổi lên trên người ngồi cạnh để pháp trận không bị che,
      // nhưng vẫn DƯỚI lớp hiệu ứng .nm-fx (z-index 4800) để món đồ, chữ, số tu vi luôn nằm trước nhân vật.
      n.root.style.zIndex = "4700";
      clearTimeout(n.itemTimer);
      n.itemTimer = setTimeout(function () {
        ih.className = "nm-ihalo"; n.root.classList.remove("nm-item-on");
        if (n.pos && !n.root.classList.contains("nm-strike")) n.root.style.zIndex = String(zFor(n, n.pos));
      }, dur + 150);
    }
    var hatchMs = (config.pets && config.pets.hatchMs) || 3200;
    var TIER_MS = { 1: 1600, 2: 2200, 3: 3000, 4: 5000 };
    /** Đốm sáng tỏa ra quanh món đồ. */
    function sparks(box, n) {
      for (var i = 0; i < n; i++) {
        var sp = el("div", "nm-spark", box), a = (i / n) * Math.PI * 2, r = 0.9 + Math.random() * 0.8;
        sp.style.setProperty("--sx", (Math.cos(a) * r).toFixed(2));
        sp.style.setProperty("--sy", (Math.sin(a) * r).toFixed(2));
        sp.style.animationDelay = "calc(var(--t) * " + (0.35 + Math.random() * 0.25).toFixed(2) + ")";
      }
    }
    function renderHatches(list) {
      list = list || [];
      if (lastHatchId < 0) { lastHatchId = list.length ? list[list.length - 1].id : 0; return; }
      list.forEach(function (h) {
        if (h.id <= lastHatchId) return;
        lastHatchId = h.id;
        var c = discipleCenter(h.userId), n = nodes[h.userId], t = PETS[h.pet];
        if (!c || !n || !t) return;
        var e = eggFx[h.fxId];
        if (e) e.egg.classList.add("nm-egg-open");
        trimFx();
        var hgt = n.pos.h * (n.grow || 1), box = el("div", "nm-hatch", fxLayer);
        box.style.cssText = "left:" + c.x.toFixed(0) + "px;top:" + c.y.toFixed(0) + "px";
        box.style.setProperty("--s", Math.max(90, Math.min(220, hgt * 0.9)).toFixed(0) + "px");
        el("div", "nm-item-burst", box);
        var img = el("img", "nm-hatch-pet", box); img.src = base + t.img + ".png"; img.alt = "";
        var lab = el("div", "nm-hatch-label", box);
        lab.textContent = h.result === "wheel" ? t.name + " đủ sao · quay thưởng!" : t.name + " " + "★".repeat(h.stars);
        setTimeout(function () { box.remove(); }, 2600);
      });
    }

    // ---------- Thiên kiếp: mây giông phủ kín phía trên, người độ kiếp đứng giữa dưới mây, tông môn lùi xuống ----------
    var kiepBox = el("div", "nm-kiep", camera);
    el("div", "nm-kiep-sky", kiepBox);
    var kiepClouds = [0, 1, 2].map(function (i) { var c = el("img", "nm-kiep-cloud nm-kiep-cloud" + i, kiepBox); c.src = base + "fx_storm_cloud.png"; c.alt = ""; return c; });
    var kiepDome = el("div", "nm-kiep-dome", kiepBox);
    var kiepFig = el("div", "nm-kiep-fig", kiepBox);
    kiepFig.style.cssText = "left:" + (W / 2 - KIEP.figH * SPRITE_RATIO / 2) + "px;top:" + KIEP.figTop + "px;width:" + (KIEP.figH * SPRITE_RATIO) + "px;height:" + KIEP.figH + "px";
    var kiepImg = el("img", "nm-kiep-img", kiepFig); kiepImg.alt = "";
    var kiepTag = el("div", "nm-kiep-tag", kiepFig);
    var kiepBar = el("div", "nm-kiep-bar", kiepBox);
    var kiepBolts = el("div", "nm-kiep-bolts", kiepBox);
    var kiepId = 0, kiepStruck = 0, kiepDone = false, kiepSelfId = 0;
    // Pháp trận pháp bảo của người đang độ kiếp: hiện quanh người đó trên mây giông (người đó không ngồi ở hàng dưới).
    var kiepIhalo = el("img", "nm-ihalo nm-kiep-ihalo", kiepFig); kiepIhalo.alt = "";
    kiepFig.style.setProperty("--h", KIEP.figH + "px");
    var KIEP_BODY = { x: W / 2, y: KIEP.figTop + KIEP.figH * 0.45 };
    function renderKiep(k) {
      var active = !!k;
      if (active !== kiepMode) { kiepMode = active; stage.classList.toggle("nm-kiep-mode", active); setCrowd(); }
      if (!k) { if (kiepId) { kiepBox.classList.remove("nm-kiep-win"); kiepId = 0; } return; }
      if (k.id !== kiepId) { kiepId = k.id; kiepStruck = 0; kiepDone = false; kiepBox.classList.remove("nm-kiep-win"); kiepSelfId = (k.selfHits && k.selfHits.length) ? k.selfHits[k.selfHits.length - 1].id : 0; }
      // Tự hộ thân: người độ kiếp tự tu luyện / dùng pháp bảo -> vòng kết giới vàng lóe quanh người, số "+X kết giới" bay lên.
      (k.selfHits || []).forEach(function (h) {
        if (h.id <= kiepSelfId) return;
        kiepSelfId = h.id;
        kiepFig.classList.remove("nm-kiep-self"); void kiepFig.offsetWidth; kiepFig.classList.add("nm-kiep-self");
        kiepDome.classList.remove("nm-dome-hit"); void kiepDome.offsetWidth; kiepDome.classList.add("nm-dome-hit");
        popNumber(W / 2 + (Math.random() - 0.5) * 120, KIEP.figTop + 40, "+" + h.amount.toLocaleString("vi-VN") + " kết giới", "nm-pop-gold nm-pop-self");
      });
      var sprite = spriteFor[k.realm] || spriteFor[0];
      if (kiepImg.dataset.sprite !== sprite) { kiepImg.dataset.sprite = sprite; kiepImg.src = base + sprite + ".png"; }
      kiepTag.innerHTML = '<div class="nm-kiep-label">ĐỘ KIẾP · ' + escapeHtml(k.realmName).toUpperCase() + '</div><div class="nm-tag-name">' + escapeHtml(k.name) + "</div>";
      var pct = Math.min(100, 100 * k.shield / Math.max(1, k.need));
      kiepDome.style.opacity = (0.15 + 0.75 * pct / 100).toFixed(2);
      kiepBar.innerHTML = '<div class="nm-kiep-title">Lôi kiếp · ' + escapeHtml(k.realmName) + (k.phase === "strike" ? " · " + Math.ceil(k.remainingMs / 1000) + "s · đạo " + k.struck + "/" + k.strikes : "") + "</div>" +
        '<div class="nm-kiep-track"><i style="width:' + pct.toFixed(1) + '%"></i></div><div class="nm-kiep-sub">Kết giới ' +
        Math.min(k.shield, k.need).toLocaleString("vi-VN") + " / " + k.need.toLocaleString("vi-VN") + "</div><div class=\"nm-kiep-sub\">Sử dụng pháp bảo để hộ pháp</div>";
      (k.log || []).forEach(function (st) {
        if (st.n <= kiepStruck) return;
        kiepStruck = st.n;
        strikeSequence(st.blocked, kiepId);
      });
      if (k.phase === "result" && !kiepDone) {
        kiepDone = true;
        if (k.success) { kiepBox.classList.add("nm-kiep-win"); if (k.bonus) popNumber(W / 2, KIEP.figTop - 10, "+" + k.bonus.toLocaleString("vi-VN"), "nm-pop-gold nm-pop-big"); }
      }
    }
    /**
     * 1 đạo lôi kiếp kéo dài khoảng 3 giây (anh chốt 14/11): sấm chớp nối nhau trước sau rồi mới giáng đòn chính.
     *  0–2,2 giây: 6 tia phụ đánh xuống hai bên / trước mặt, lóe sáng nhẹ, mây rung; tia gần cuối càng dày và sát người độ kiếp.
     *  2,4 giây: tia chính + 2 tia kèm giáng thẳng xuống người độ kiếp (kết giới đủ thì chặn ở vòm), chớp toàn màn.
     *  2,7 giây: 1 tia dư chấn nhỏ.
     */
    function strikeSequence(blocked, id) {
      var alive = function () { return kiepId === id; };
      var side = function (near) {
        var left = Math.random() < 0.5, x0 = left ? 100 + Math.random() * 300 : W - 100 - Math.random() * 300;
        var x1 = near ? W / 2 + (left ? -1 : 1) * (90 + Math.random() * 120) : (left ? 40 + Math.random() * 300 : W - 40 - Math.random() * 300);
        lightning(kiepBolts, x0, 240 + Math.random() * 60, x1, near ? KIEP.figTop + 60 + Math.random() * 120 : 600 + Math.random() * 260, false, false);
      };
      var flash = function (cls) { var fl = el("div", "nm-kiep-flash" + (cls ? " " + cls : ""), kiepBox); setTimeout(function () { fl.remove(); }, 450); };
      [0, 380, 700, 1150, 1500, 1850, 2100].forEach(function (t, i) {
        setTimeout(function () {
          if (!alive()) return;
          side(i >= 4);
          if (i % 2 === 1) side(i >= 5);
          if (i >= 3) flash("nm-kiep-flash-soft");
        }, t + Math.random() * 120);
      });
      setTimeout(function () {
        if (!alive()) return;
        var top = blocked ? KIEP.figTop - 30 : KIEP_BODY.y;
        lightning(kiepBolts, W / 2 + (Math.random() - 0.5) * 240, 280, W / 2, top, blocked, true);
        lightning(kiepBolts, W / 2 - 160 - Math.random() * 80, 260, W / 2 - 40, top + 10, blocked, true);
        lightning(kiepBolts, W / 2 + 160 + Math.random() * 80, 260, W / 2 + 40, top + 10, blocked, true);
        flash();
        if (blocked) { kiepDome.classList.remove("nm-dome-hit"); void kiepDome.offsetWidth; kiepDome.classList.add("nm-dome-hit"); }
        else { kiepFig.classList.remove("nm-kiep-struck"); void kiepFig.offsetWidth; kiepFig.classList.add("nm-kiep-struck"); }
      }, 2400);
      setTimeout(function () { if (alive()) { side(true); flash("nm-kiep-flash-soft"); } }, 2750);
    }
    /** Tia sét gấp khúc (SVG) từ (x0,y0) xuống (x1,y1); bị kết giới chặn thì tóe sáng xanh. */
    function lightning(layerEl, x0, y0, x1, y1, blocked, main) {
      while (layerEl.children.length > 40) layerEl.firstChild.remove();
      var pts = [], seg = 9;
      for (var i = 0; i <= seg; i++) {
        var t = i / seg, jitter = (i === 0 || i === seg) ? 0 : (Math.random() - 0.5) * 70;
        pts.push((x0 + (x1 - x0) * t + jitter).toFixed(0) + "," + (y0 + (y1 - y0) * t).toFixed(0));
      }
      var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
      svg.setAttribute("class", "nm-bolt" + (blocked ? " nm-bolt-blocked" : "") + (main ? "" : " nm-bolt-side"));
      svg.setAttribute("width", W); svg.setAttribute("height", H); svg.setAttribute("viewBox", "0 0 " + W + " " + H);
      // Nhánh rẽ: 1–2 nhánh ngắn tách ra từ thân tia, trông giống sấm sét thật hơn.
      var lines = [pts], nb = 1 + (Math.random() < 0.6 ? 1 : 0);
      for (var bI = 0; bI < nb; bI++) {
        var from = pts[3 + Math.floor(Math.random() * 4)].split(",").map(Number), dir = Math.random() < 0.5 ? -1 : 1, bp = [from.join(",")];
        for (var k = 1; k <= 3; k++) bp.push((from[0] + dir * (25 + Math.random() * 25) * k).toFixed(0) + "," + (from[1] + (30 + Math.random() * 30) * k).toFixed(0));
        lines.push(bp);
      }
      ["nm-bolt-glow", "nm-bolt-core"].forEach(function (cls) {
        lines.forEach(function (l, li) {
          var pl = document.createElementNS("http://www.w3.org/2000/svg", "polyline");
          pl.setAttribute("points", l.join(" ")); pl.setAttribute("class", cls + (li ? " nm-bolt-branch" : "")); svg.appendChild(pl);
        });
      });
      layerEl.appendChild(svg);
      var fl = el("div", "nm-bolt-flash", layerEl); fl.style.cssText = "left:" + x1 + "px;top:" + y1 + "px";
      setTimeout(function () { svg.remove(); fl.remove(); }, 560);
    }

    /** Số sát thương to, nảy ra rồi bay lên. */
    function popNumber(x, y, text, cls) {
      while (fxLayer.children.length > 40) fxLayer.firstChild.remove();
      var n = el("div", "nm-pop " + cls, fxLayer);
      n.textContent = text;
      n.style.left = x.toFixed(0) + "px"; n.style.top = y.toFixed(0) + "px";
      setTimeout(function () { n.remove(); }, 1400);
    }

    // ---------- Tỉ thí: xếp chỗ theo phe ----------
    // Phe 1 nửa trái, phe 2 nửa phải (4 người / hàng, chân từ y≈FIGHT.top tới FIGHT.bottom). Người chưa chọn phe ngồi dải dưới.
    var FIGHT = Object.assign({ top: 560, bottom: 1170, left: 40, mid: 540, right: 1040, gap: 30, cols: 4, maxH: 145, bigH: 195, hugeH: 280,
      watchTop: 1210, watchBottom: 1440, watchMaxH: 100 }, L.duelLayout || {});
    function grid(list, x0, x1, top, bottom, cols, maxH, tag) {
      // Ít người thì 3 cột, nhân vật to hơn (tới bigH); đông mới chia 4 cột.
      // Ít người thì ít cột, nhân vật to hơn: <= 4 người 2 cột (to nhất), <= 9 người 3 cột, đông mới chia 4 cột.
      var fontPx = 22;
      if (tag !== "w") {
        if (list.length <= 4) { cols = 2; maxH = FIGHT.hugeH; fontPx = 30; }
        else if (list.length <= 9) { cols = 3; maxH = FIGHT.bigH; fontPx = 23; }
      }
      // Mỗi ô = nhân vật + khung tên dưới chân (khung đè lên đệm ngồi 8px). Khung rộng gần bằng ô, tối đa 240.
      var rows = Math.max(1, Math.ceil(list.length / cols)), step = (x1 - x0) / cols, out = {};
      var tw = Math.min(240, step - 6), th = tw / TAG_RATIO * 0.58, cellH = (bottom - top) / rows;   // phần khung thò xuống dưới chân
      var h = Math.min(maxH, cellH - th), pitch = h + th;
      var y0 = top + ((bottom - top) - pitch * rows) / 2;              // khối người canh giữa theo chiều dọc
      list.forEach(function (id, i) {
        var r = Math.floor(i / cols), c = i % cols, inRow = Math.min(cols, list.length - r * cols);
        var x = x0 + (x1 - x0) / 2 + (c - (inRow - 1) / 2) * step;     // hàng thiếu người thì dồn vào giữa nửa sân
        out[id] = { s: { x: x, y: y0 + pitch * r + h, h: h * 0.96, duel: tag !== "w", fontPx: fontPx, tagW: tw }, key: tag + list.length + ":" + i };
      });
      return out;
    }
    function duelSpots(seats) {
      var g = { 1: [], 2: [], 0: [] };
      seats.forEach(function (s) { g[s.side === 1 || s.side === 2 ? s.side : 0].push(s.id); });
      var watchCols = g[0].length <= 30 ? 10 : Math.ceil(g[0].length / 3);
      var all = Object.assign({},
        grid(g[1], FIGHT.left, FIGHT.mid - FIGHT.gap, FIGHT.top, FIGHT.bottom, FIGHT.cols, FIGHT.maxH, "a"),
        grid(g[2], FIGHT.mid + FIGHT.gap, FIGHT.right, FIGHT.top, FIGHT.bottom, FIGHT.cols, FIGHT.maxH, "b"),
        grid(g[0], FIGHT.left, FIGHT.right, FIGHT.watchTop, FIGHT.watchBottom, watchCols, FIGHT.watchMaxH, "w"));
      return seats.map(function (s) { return all[s.id]; });
    }
    // Đòn đánh giữa hai phe: người tung đòn lấy đà rồi lao ngắn (0,5 giây), luồng sáng màu phe bay sang, người trúng rung nhẹ.
    var lastDuelHitId = 0;
    function renderDuelHits(d) {
      var hits = d.hits || [];
      if (d.phase !== "duel") { if (hits.length) lastDuelHitId = Math.max(lastDuelHitId, hits[hits.length - 1].id); return; }
      hits.forEach(function (h) {
        if (h.id <= lastDuelHitId) return;
        lastDuelHitId = h.id;
        var from = discipleCenter(h.from), to = h.to != null ? discipleCenter(h.to) : null;
        strike(h.from, h.side);
        if (from && to) swordFly(from, to, "s" + h.side, function () {
          shakeDisciple(h.to);
          var p = discipleCenter(h.to);
          if (p && h.lost > 0) popNumber(p.x, p.y - 30, "-" + h.lost.toLocaleString("vi-VN"), "nm-pop-red");
        });
      });
    }
    function shakeDisciple(id) {
      var n = nodes[id];
      if (!n) return;
      n.root.classList.remove("nm-shaken"); void n.root.offsetWidth; n.root.classList.add("nm-shaken");
    }

    function escapeHtml(t) { return String(t).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }

    // ---------- Vòng quay thưởng ----------
    var wheelId = 0, wheelDisc = null, wheelResult = null;
    function renderWheel(w) {
      if (!w) { if (wheelId) { wheelBox.classList.remove("nm-show"); wheelId = 0; } return; }
      // Đang hiện kết quả tỉ thí: vòng quay hạ xuống giữa sân, không đè lên bảng công bố phe thắng.
      wheelBox.classList.toggle("nm-wheel-low", !!(last && last.duel && last.duel.phase === "result"));
      if (w.id !== wheelId) {
        wheelId = w.id;
        var n = w.prizes.length, seg = 360 / n, colors = ["#f6d77c", "#fff4d6", "#e9b949", "#fbe7a8", "#d99a2b", "#fff0c2", "#f1c75b", "#fde9b0"];
        var stops = w.prizes.map(function (_, i) { return colors[i % colors.length] + " " + (i * seg) + "deg " + ((i + 1) * seg) + "deg"; }).join(",");
        wheelBox.innerHTML = '<div class="nm-wheel-title">' + escapeHtml(w.name) + ' quay thưởng</div><div class="nm-wheel"><div class="nm-wheel-pointer"></div><div class="nm-wheel-disc"></div></div><div class="nm-wheel-result"></div>';
        wheelDisc = wheelBox.querySelector(".nm-wheel-disc");
        wheelResult = wheelBox.querySelector(".nm-wheel-result");
        wheelDisc.style.background = "conic-gradient(" + stops + ")";
        w.prizes.forEach(function (p, i) {
          var lab = el("div", "nm-wheel-label", wheelDisc);
          lab.style.transform = "rotate(" + (i * seg + seg / 2) + "deg) translateY(-118px)";
          lab.textContent = "+" + (p >= 1000 ? (p / 1000) + "K" : p);
        });
        // Quay nhiều vòng rồi dừng đúng ô trúng dưới kim (kim ở đỉnh). Đồng bộ theo thời gian đã trôi (overlay vào giữa chừng vẫn đúng).
        var final = 360 * 6 + (360 - (w.index * seg + seg / 2));
        var remain = Math.max(0, w.spinMs - w.elapsed);
        wheelDisc.style.transition = "none"; wheelDisc.style.transform = "rotate(0deg)";
        void wheelDisc.offsetWidth;
        wheelDisc.style.transition = "transform " + remain + "ms cubic-bezier(.12,.65,.1,1)";
        wheelDisc.style.transform = "rotate(" + final + "deg)";
        wheelBox.classList.add("nm-show");
      }
      wheelResult.textContent = w.applied ? "+" + w.prize.toLocaleString("vi-VN") + " tu vi!" : "";
    }

    // Banner: mỗi lần 1 câu. Câu luật chơi (kind "info") chỉ hiện khi không có câu nào khác đang chờ.
    var lastBannerId = 0, bannerQueue = [], bannerTimer = null;
    function pushBanner(b) {
      if (b.kind === "info" && (bannerTimer || bannerQueue.length)) return;
      bannerQueue.push(Object.assign({ at: Date.now() }, b));
      bannerQueue.sort(function (a, c) { return (c.priority || 0) - (a.priority || 0); });
      if (bannerQueue.length > 3) bannerQueue.length = 3;
      if (!bannerTimer) nextBanner();
    }
    function nextBanner() {
      // Câu đã chờ lâu là chuyện cũ: bỏ qua để banner không chạy trễ so với màn hình
      // (câu quan trọng như tỉ thí / lên ghế được chờ 6 giây, câu thường 3 giây).
      var b = bannerQueue.shift();
      while (b && Date.now() - b.at > (b.priority >= 3 ? 6000 : 3000)) b = bannerQueue.shift();
      if (!b) { bannerTimer = null; return; }
      // Xóa ngay câu cũ để 2 câu không chồng chữ lên nhau.
      while (bannerBox.firstChild) bannerBox.removeChild(bannerBox.firstChild);
      var item = el("div", "nm-banner nm-p" + (b.priority || 0), bannerBox);
      item.textContent = b.text;
      bannerTimer = setTimeout(function () {
        item.classList.add("nm-out");
        bannerTimer = setTimeout(function () { item.remove(); nextBanner(); }, 300);
      }, (b.priority >= 3 ? 3200 : 2200) / (bannerQueue.length ? 1.5 : 1));   // còn câu chờ thì hiện nhanh hơn
    }

    // Live đông thì thứ hạng đổi liên tục: chỉ xếp lại chỗ tối đa mỗi REORDER_MS.
    // Có người vào/rời chỗ hoặc đổi người ở 3 ghế đài thì xếp lại NGAY.
    var REORDER_MS = opts.reorderMs || 1500, lastReorder = 0, lastSig = "";
    function render(state, force) {
      last = state;
      var seats = state.seats || [], now = Date.now();
      // Đang tỉ thí: xếp theo phe (phe 1 bên trái, phe 2 bên phải, ai chưa chọn ngồi dưới xem).
      var d = state.duel || {}, duelLayout = d.phase === "duel" || d.phase === "result";
      duelMode = duelLayout;
      var sig = (duelLayout ? "D" : "N") + seats.map(function (s) { return s.id + (duelLayout ? ":" + s.side : ""); }).sort().join("|") + "#" + seats.slice(0, 3).map(function (s) { return s.id; }).join("|");
      // Có người thăng cấp: xếp lại chỗ ngay để camera thu vào đúng chỗ MỚI của người đó.
      var leveled = seats.some(function (s) { return nodes[s.id] && s.level > nodes[s.id].level; });
      if (force || leveled || sig !== lastSig || now - lastReorder >= REORDER_MS) {
        lastReorder = now; lastSig = sig;
        var seen = {};
        var spots = duelLayout ? duelSpots(seats) : null;
        seats.forEach(function (seat, i) { seen[seat.id] = true; var sp = spots ? spots[i] : { s: slots[i], key: "n" + i }; place(nodeFor(seat), seat, sp.s, sp.key); });
        Object.keys(nodes).forEach(function (id) {
          if (seen[id]) return;
          var n = nodes[id]; delete nodes[id];
          n.root.classList.add("nm-leave"); setTimeout(function () { n.root.remove(); }, 600);
        });
      } else {
        seats.forEach(function (seat) { var n = nodes[seat.id]; if (n && n.pos) place(n, seat, n.pos, n.key); });
      }
      seatImgs.forEach(function (img, i) { img.classList.toggle("nm-empty", !seats[i]); });
      stage.classList.toggle("nm-duel-layout", duelLayout);
      stage.classList.toggle("nm-duel-result", d.phase === "result");
      renderDuelHits(d);
      renderBoss(state.boss || null);
      renderPossession(state.possession || null);
      renderStatus(state.duel || { enabled: false, phase: "idle" });
      renderWheel(state.wheel || null);
      renderItems(state.items);
      renderHatches(state.hatches);
      renderKiep(state.kiep || null);
      stage.classList.toggle("nm-surge", !!state.surge);
      surgeChip.innerHTML = state.surge ? "LINH KHÍ TRIỀU DÂNG<br><b>x" + state.surge.multiplier + " tu vi</b> · " + mmss(state.surge.remainingMs) : "";
      (state.banners || []).forEach(function (b) {
        if (b.id <= lastBannerId) return;
        lastBannerId = b.id;
        pushBanner(b);
        if (b.kind === "seat" && b.memberId && nodes[b.memberId]) flash(nodes[b.memberId], "nm-realm-up");
      });
    }

    function fit() {
      var s = Math.min(window.innerWidth / W, window.innerHeight / H);
      stage.style.transform = "translate(" + ((window.innerWidth - W * s) / 2) + "px," + ((window.innerHeight - H * s) / 2) + "px) scale(" + s + ")";
    }
    window.addEventListener("resize", fit);
    fit();

    return { render: render, slots: slots, fit: fit };
  }

  return { createRenderer: createRenderer, computeSlots: computeSlots, REALM_COLORS: REALM_COLORS };
}));
