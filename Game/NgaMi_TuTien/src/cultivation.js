/*
 * Hệ thống tu vi: đệ tử, cảnh giới, chỗ ngồi. Logic thuần, không đụng DOM, test được bằng node.
 *
 * Luật chỗ ngồi (anh chốt 10/11):
 *   - Thả tim hoặc tặng quà là tham gia (anh chốt 10/11). Tối đa `capacity` (53) người cùng lúc.
 *   - Mọi người đang ngồi đều hiện trên màn hình.
 *   - Im lặng (không tim / quà / comment) `idleMs` (2 phút) thì mờ dần 50% trong `fadeMs` (3s) rồi rời chỗ.
 *   - Rời chỗ chỉ là rời màn hình: tu vi giữ nguyên tới hết TUẦN (reset 0h thứ Hai). Thả tim / tặng quà lại là quay về.
 *
 * Sự kiện phát ra qua onEvent:
 *   { type: "join", member, returning }        vào chỗ ngồi (returning = đã từng ngồi trong buổi này)
 *   { type: "full", member }                    hết chỗ, không vào được
 *   { type: "leave", member }                   im lặng quá lâu, rời chỗ (vẫn giữ tu vi)
 *   { type: "level_up", member, from, to, bigRealm }
 *   { type: "seat_change", member, seat, previousHolderId }   lên ghế Chưởng môn / Tả / Hữu hộ pháp
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  var TOP_SEATS = ["Chưởng môn", "Tả hộ pháp", "Hữu hộ pháp"];

  /**
   * Lọc tên: chỉ giữ chữ Latin (gồm tiếng Việt có dấu), số, khoảng trắng và . _ -
   * Bỏ emoji, ký tự trang trí, chữ Hán/Thái... Không còn gì thì trả "" (khi đó gọi là "đạo hữu", không kèm tên).
   */
  /** Tách theo "kí tự nhìn thấy" (emoji ghép nhiều mã vẫn là 1), để cắt tên không làm vỡ icon. */
  function graphemes(s) {
    if (typeof Intl !== "undefined" && Intl.Segmenter) return Array.from(new Intl.Segmenter("vi", { granularity: "grapheme" }).segment(s), function (x) { return x.segment; });
    return Array.from(s);
  }
  /**
   * Tên người chơi để LƯU và HIỆN (anh chốt 12/11): giữ đủ icon, kí tự đặc biệt, chữ nước ngoài cho dễ tìm.
   * Chỉ bỏ kí tự điều khiển ẩn, kí tự đảo chiều chữ, khoảng trắng vô hình và dấu chồng chất kiểu "zalgo" (làm vỡ khung tên).
   * Giọng AI đọc bản lọc riêng (announcer.speakableName).
   */
  function cleanName(name) {
    var s = String(name || "").normalize("NFC")
      .replace(/[\u0000-\u001f\u007f-\u009f\u200b\u200e\u200f\u202a-\u202e\u2066-\u2069\ufeff]/g, "")
      .replace(/(\p{M}{2})\p{M}+/gu, "$1")
      .replace(/\s+/g, " ").trim();
    return graphemes(s).slice(0, 30).join("").trim();
  }

  /** Bảng phẳng mọi tiểu cảnh theo thứ tự tăng dần, dùng để tra cảnh giới từ tu vi. */
  function buildLadder(realms) {
    var ladder = [];
    realms.forEach(function (realm, realmIdx) {
      realm.stages.forEach(function (min, stageIdx) {
        var stage = realm.stageNames[stageIdx] || "";
        ladder.push({ realmIdx: realmIdx, stageIdx: stageIdx, min: min, realm: realm.name, stage: stage,
          label: stage ? realm.name + " " + stage : realm.name });
      });
    });
    return ladder;
  }

  function levelOf(ladder, tuvi) {
    var lo = 0, hi = ladder.length - 1;
    while (lo < hi) { var mid = (lo + hi + 1) >> 1; if (ladder[mid].min <= tuvi) lo = mid; else hi = mid - 1; }
    var cur = ladder[lo], next = ladder[lo + 1] || null;
    return { index: lo, realmIdx: cur.realmIdx, stageIdx: cur.stageIdx, realm: cur.realm, stage: cur.stage, label: cur.label,
      next: next ? next.min : null, progress: next ? (tuvi - cur.min) / (next.min - cur.min) : 1 };
  }

  function Sect(config, hooks) {
    this.config = config;
    this.hooks = hooks || {};
    this.now = this.hooks.now || function () { return Date.now(); };
    this.ladder = buildLadder(config.realms);
    this.capacity = config.capacity;
    this.idleMs = config.idleMs;
    this.fadeMs = config.fadeMs;
    this.members = new Map();   // id -> member (mọi người đã tham gia trong buổi live này, kể cả đã rời chỗ)
    this.seated = new Set();    // id đang ngồi trên màn hình
    this.topIds = [];
    this.dirty = false;
    // Lôi kiếp chặn đột phá: từ đại cảnh giới gateFrom trở lên phải độ kiếp thành công mới lên được (null = không chặn).
    // Chưa độ kiếp thì dừng ở Đại viên mãn của cảnh giới trước, tu vi vẫn tích lũy bình thường.
    this.gateFrom = null;
    this.realmFirst = [];          // realmIdx -> vị trí tiểu cảnh đầu tiên trong ladder
    var self = this;
    this.ladder.forEach(function (l, i) { if (self.realmFirst[l.realmIdx] == null) self.realmFirst[l.realmIdx] = i; });
  }

  Sect.prototype.emit = function (event) { if (typeof this.hooks.onEvent === "function") this.hooks.onEvent(event); };
  Sect.prototype.levelOf = function (tuvi) { return levelOf(this.ladder, tuvi); };

  /** Đại cảnh giới đầu tiên người này CHƯA độ kiếp (-1 nếu không bị chặn). m.passed = cảnh giới cao nhất đã độ kiếp qua. */
  Sect.prototype.lockedRealm = function (m) {
    if (this.gateFrom == null || !m) return -1;
    var r = Math.max(this.gateFrom, (m.passed == null ? -1 : m.passed) + 1);
    return r < this.realmFirst.length ? r : -1;
  };
  /** Cảnh giới hiển thị: theo tu vi nhưng không vượt Đại viên mãn của cảnh giới trước cảnh giới chưa độ kiếp. */
  Sect.prototype.levelOfMember = function (m) {
    var lv = levelOf(this.ladder, m ? m.tuvi : 0), locked = this.lockedRealm(m);
    if (locked < 0 || lv.realmIdx < locked) return lv;
    var cap = this.ladder[this.realmFirst[locked] - 1];
    return { index: this.realmFirst[locked] - 1, realmIdx: cap.realmIdx, stageIdx: cap.stageIdx, realm: cap.realm, stage: cap.stage, label: cap.label,
      next: null, progress: 1, blocked: locked };
  };
  /** Đủ tu vi lên cảnh giới mới nhưng chưa độ kiếp: trả cảnh giới cần độ kiếp, không thì -1. */
  Sect.prototype.needsKiep = function (m) {
    var lv = this.levelOfMember(m);
    return lv.blocked != null ? lv.blocked : -1;
  };
  /** Độ kiếp thành công cảnh giới realmIdx: mở khóa và báo lên cảnh giới (reason "kiep"). */
  Sect.prototype.passKiep = function (id, realmIdx) {
    var m = this.members.get(String(id));
    if (!m) return null;
    var before = this.levelOfMember(m);
    m.passed = Math.max(m.passed == null ? -1 : m.passed, realmIdx);
    var after = this.levelOfMember(m);
    m.level = after.index;
    this.dirty = true;
    if (after.index > before.index && this.seated.has(m.id)) this.emit({ type: "level_up", member: m, from: before, to: after, bigRealm: after.realmIdx > before.realmIdx, reason: "kiep" });
    return after;
  };
  Sect.prototype.isSeated = function (id) { return this.seated.has(String(id)); };

  /** Tạo/cập nhật hồ sơ (chưa cho ngồi). */
  Sect.prototype.profile = function (user) {
    var id = String((user && user.id) || "").trim();
    if (!id) return null;
    var m = this.members.get(id);
    if (!m) { m = { id: id, nickname: "", tuvi: 0, level: 0, joinedAt: this.now(), lastActive: 0, everSeated: false, pets: {}, lastGainAt: 0, passed: -1 }; this.members.set(id, m); this.dirty = true; }
    if (user.nickname != null) m.nickname = cleanName(user.nickname);
    return m;
  };

  /** Tham gia (thả tim / tặng quà). Trả "joined" | "already" | "full". */
  Sect.prototype.join = function (user) {
    var m = this.profile(user);
    if (!m) return "invalid";
    if (this.seated.has(m.id)) { m.lastActive = this.now(); return "already"; }
    if (this.seated.size >= this.capacity) { this.emit({ type: "full", member: m }); return "full"; }
    var returning = m.everSeated;
    this.seated.add(m.id);
    m.everSeated = true;
    m.lastActive = this.now();
    this.dirty = true;
    this.emit({ type: "join", member: m, returning: returning });
    this.refreshTop(false);
    return "joined";
  };

  /** Có tương tác (tim / quà / comment / action): giữ chỗ ngồi. Trả false nếu người này không đang ngồi. */
  Sect.prototype.touch = function (id) {
    var m = this.members.get(String(id));
    if (!m || !this.seated.has(m.id)) return false;
    m.lastActive = this.now();
    return true;
  };

  /** Cộng tu vi. Mặc định chỉ cho người đang ngồi; `force` dùng cho thưởng tỉ thí (người đã rời chỗ vẫn nhận). */
  Sect.prototype.addTuvi = function (user, amount, reason, force) {
    var id = String((user && user.id) || "");
    var m = force ? this.profile(user) : this.members.get(id);
    if (!m || (!force && !this.seated.has(id))) return { member: null, ups: [] };
    amount = Math.floor(Number(amount) || 0);
    if (amount <= 0) return { member: m, ups: [] };
    if (!force) m.lastActive = this.now();
    var before = this.levelOfMember(m);
    m.tuvi += amount;
    var after = this.levelOfMember(m);
    m.level = after.index;
    this.dirty = true;
    var ups = [];
    if (after.index > before.index) {
      var bigRealm = after.realmIdx > before.realmIdx;
      ups.push({ from: before, to: after, bigRealm: bigRealm });
      // Chỉ phát 1 sự kiện cho bậc cao nhất: nhảy 5 bậc vẫn chỉ đọc 1 câu.
      if (this.seated.has(m.id)) this.emit({ type: "level_up", member: m, from: before, to: after, bigRealm: bigRealm, reason: reason });
    }
    this.refreshTop();
    return { member: m, ups: ups };
  };

  /** Bị người tẩu hỏa nhập ma đánh: trừ tu vi (không âm), không đọc thông báo tụt cấp. Trả số tu vi thực bị trừ. */
  Sect.prototype.removeTuvi = function (id, amount) {
    var m = this.members.get(String(id));
    if (!m) return 0;
    var lost = Math.min(m.tuvi, Math.max(0, Math.floor(Number(amount) || 0)));
    if (!lost) return 0;
    m.tuvi -= lost;
    m.level = this.levelOfMember(m).index;
    this.dirty = true;
    this.refreshTop(false);
    return lost;
  };

  /** Gọi đều đặn: ai im lặng quá idleMs + fadeMs thì rời chỗ. */
  Sect.prototype.tick = function () {
    var now = this.now(), limit = this.idleMs + this.fadeMs, self = this, gone = [];
    this.seated.forEach(function (id) { var m = self.members.get(id); if (now - m.lastActive >= limit) gone.push(m); });
    // Rời chỗ chỉ là rời màn hình: tu vi giữ nguyên tới hết tuần (anh chốt 10/11, bỏ luật mất tu vi).
    gone.forEach(function (m) { self.seated.delete(m.id); self.emit({ type: "leave", member: m }); });
    if (gone.length) { this.dirty = true; this.refreshTop(false); }
    return gone.length;
  };

  /** Người đang ngồi, tu vi giảm dần (bằng nhau thì ai vào trước đứng trước). */
  Sect.prototype.ranking = function () {
    var self = this, list = [];
    this.seated.forEach(function (id) { list.push(self.members.get(id)); });
    return list.sort(function (a, b) { return b.tuvi - a.tuvi || a.joinedAt - b.joinedAt; });
  };

  Sect.prototype.sectRankOf = function (rank) {
    var ranks = this.config.sectRanks;
    for (var i = 0; i < ranks.length; i++) if (rank >= ranks[i].fromRank && rank <= ranks[i].toRank) return ranks[i];
    return ranks[ranks.length - 1];
  };

  /** Chỗ ngồi cho renderer: [{ rank, seatName, member, level, sectRank, fading }]. Mọi người đang ngồi đều có chỗ. */
  Sect.prototype.seating = function () {
    var self = this, now = this.now();
    return this.ranking().map(function (m, i) {
      return { rank: i + 1, seatName: TOP_SEATS[i] || null, member: m, level: self.levelOfMember(m), sectRank: self.sectRankOf(i + 1),
        fading: now - m.lastActive >= self.idleMs };
    });
  };

  /** announce = false khi đổi ghế do có người vào/rời chỗ (không đọc), true khi do vượt tu vi. */
  Sect.prototype.refreshTop = function (announce) {
    var top = this.ranking().slice(0, TOP_SEATS.length).map(function (m) { return m.id; });
    for (var i = 0; i < top.length; i++) {
      if (top[i] === this.topIds[i]) continue;
      var m = this.members.get(top[i]), oldIdx = this.topIds.indexOf(top[i]);
      // Chỉ báo khi người đó THĂNG lên ghế này (không báo khi bị đẩy xuống), và đã đủ tu vi để đáng đọc.
      if (announce !== false && (oldIdx === -1 || oldIdx > i) && m.tuvi >= (this.config.seatAnnounceMinTuvi || 0)) {
        this.emit({ type: "seat_change", member: m, seat: TOP_SEATS[i], previousHolderId: this.topIds[i] || null });
      }
    }
    this.topIds = top;
  };

  /** Sang tuần mới: tu vi mọi người về 0, giữ tên và ai đang ngồi vẫn ngồi. */
  Sect.prototype.resetTuvi = function () {
    this.members.forEach(function (m) { m.tuvi = 0; m.level = 0; m.pets = {}; m.passed = -1; });   // linh thú reset cùng tu vi, mỗi kỳ 2 tuần (anh chốt 13/11)
    this.topIds = [];
    this.dirty = true;
    this.refreshTop(false);
  };

  Sect.prototype.reset = function () { this.members.clear(); this.seated.clear(); this.topIds = []; this.dirty = true; };

  /** Lưu tu vi trong tuần (không lưu chỗ ngồi: tải lại trang thì mọi người thả tim / tặng quà để vào lại). */
  Sect.prototype.serialize = function () {
    var out = [];
    // Cột 5 (extra): linh thú { loại: số sao } (bản 2) + "_kiep": cảnh giới cao nhất đã độ kiếp (bản 3). Bản cũ chỉ có 4 cột vẫn đọc được.
    this.members.forEach(function (m) { out.push([m.id, m.nickname, m.tuvi, m.joinedAt, extraOf(m)]); });
    return { v: 3, members: out };
  };

  Sect.prototype.load = function (data) {
    if (!data || data.v !== 3 || !Array.isArray(data.members)) return false;
    this.reset();
    var self = this;
    data.members.forEach(function (r) {
      var m = { id: r[0], nickname: r[1], tuvi: r[2] || 0, joinedAt: r[3] || 0, lastActive: 0, everSeated: true, pets: cleanPets(r[4]), lastGainAt: 0, passed: passedOf(self, r) };
      m.level = self.levelOfMember(m).index;
      self.members.set(r[0], m);
    });
    this.dirty = false;
    return true;
  };

  /**
   * Gộp dữ liệu tuần đọc từ hub vào (không xóa ai đang ngồi): mỗi người lấy tu vi lớn hơn giữa bản đang có và bản đọc vào.
   * Dùng khi dữ liệu từ hub về sau lúc game đã mở. Trả về true nếu có thay đổi.
   */
  Sect.prototype.merge = function (data) {
    if (!data || data.v !== 3 || !Array.isArray(data.members)) return false;
    var self = this, changed = false;
    data.members.forEach(function (r) {
      var tuvi = r[2] || 0, m = self.members.get(r[0]);
      if (!m) {
        var nm = { id: r[0], nickname: r[1], tuvi: tuvi, joinedAt: r[3] || 0, lastActive: 0, everSeated: true, pets: cleanPets(r[4]), lastGainAt: 0, passed: passedOf(self, r) };
        nm.level = self.levelOfMember(nm).index;
        self.members.set(r[0], nm);
        changed = true;
      } else {
        m.everSeated = true;
        var pass = passedOf(self, r);
        if (pass > (m.passed == null ? -1 : m.passed)) { m.passed = pass; changed = true; }
        if (tuvi > m.tuvi) { m.tuvi = tuvi; changed = true; }
        m.level = self.levelOfMember(m).index;
        var p = cleanPets(r[4]);
        Object.keys(p).forEach(function (k) { if (!m.pets) m.pets = {}; if ((m.pets[k] || 0) < p[k]) { m.pets[k] = p[k]; changed = true; } });
      }
    });
    if (changed) { this.dirty = true; this.refreshTop(false); }
    return changed;
  };

  /** Cột extra khi lưu: linh thú + "_kiep" (cảnh giới cao nhất đã độ kiếp, -1 = chưa qua lần nào). Luôn ghi để tải lại không bị "miễn kiếp". */
  function extraOf(m) {
    var out = cleanPets(m.pets);
    out._kiep = m.passed == null ? -1 : m.passed;
    return out;
  }
  /**
   * Cảnh giới đã độ kiếp đọc từ dữ liệu lưu. Dữ liệu cũ (trước luật độ kiếp chặn đột phá) không có "_kiep":
   * coi như đã qua mọi cảnh giới đang có theo tu vi, để không ai bị tụt cảnh giới khi cập nhật bản mới.
   */
  function passedOf(sect, r) {
    var e = r[4], k = e && typeof e === "object" ? Number(e._kiep) : NaN;
    if (isFinite(k) && k >= -1) return Math.floor(k);
    return sect.levelOf(r[2] || 0).realmIdx;
  }

  /** Linh thú hợp lệ: { loại: số sao 1..9 }. Khóa bắt đầu bằng "_" là dữ liệu khác (vd. "_kiep"), không phải linh thú. */
  function cleanPets(p) {
    var out = {};
    if (p && typeof p === "object") Object.keys(p).forEach(function (k) { var n = Math.floor(Number(p[k]) || 0); if (/^[a-z][a-z_]{0,23}$/.test(k) && n > 0) out[k] = Math.min(9, n); });
    return out;
  }

  return { Sect: Sect, cleanPets: cleanPets, buildLadder: buildLadder, cleanName: cleanName, TOP_SEATS: TOP_SEATS };
}));
