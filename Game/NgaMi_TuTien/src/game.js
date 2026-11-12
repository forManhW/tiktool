/*
 * Bộ não của game: action từ TikTool -> tu vi -> tỉ thí -> thông báo. Không vẽ gì (renderer ở src/render.js).
 *
 * Hai đường nhận dữ liệu từ TikTool:
 *   1. Action (mapping streamer cài trong TikTool, có bộ mặc định ở manifest.defaultMappings):
 *      binh_luan, tu_luyen, vao_phe, start_duel, end_duel. Đây là đường DUY NHẤT cộng tu vi.
 *   2. Sự kiện thô (manifest.liveEvents: like, gift): KHÔNG cộng tu vi. Thả tim / tặng quà = THAM GIA (anh chốt 10/11):
 *      người chưa có chỗ thì được vào chỗ (nếu còn), người đang ngồi thì giữ chỗ.
 * Game không tự thêm nhân vật nào: chỉ người xem thật (hoặc sự kiện giả lập trong TikTool) mới có mặt.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory(require("./cultivation"), require("./duel"), require("./announcer"), require("./boss"), require("./possession"));
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory(root.NgaMi, root.NgaMi, root.NgaMi, root.NgaMi, root.NgaMi));
}(typeof self !== "undefined" ? self : this, function (C, D, A, B, PS) {
  "use strict";

  var SETTINGS_KEY = "ngami.tutien.settings.v1";
  var SETTINGS_VERSION = 2;      // v2 (10/11): chu kỳ 7' tu + 3' tỉ thí, thêm quái rồng
  var WHEEL_SHOW_MS = 3000;      // vòng quay dừng rồi còn hiện kết quả 3 giây
  var INFO_QUIET_MS = 3000;     // im 3 giây không có thông báo nào thì hiện luật chơi
  var BUBBLE_MS = 4000;         // bóng thoại hiện 4 giây

  function clone(o) { return JSON.parse(JSON.stringify(o)); }

  /** Mã tuần theo giờ máy: ngày thứ Hai đầu tuần, dạng "2026-11-09". Tuần mới bắt đầu 0h thứ Hai. */
  function weekKey(ms) {
    var d = new Date(ms), back = (d.getDay() + 6) % 7, m = new Date(d.getFullYear(), d.getMonth(), d.getDate() - back);
    return m.getFullYear() + "-" + String(m.getMonth() + 1).padStart(2, "0") + "-" + String(m.getDate()).padStart(2, "0");
  }

  /** Cài đặt streamer chỉnh trên màn Cài đặt. Giá trị mặc định lấy từ config. */
  function defaultSettings(cfg) {
    return { voice: cfg.voice.enabled, duel: cfg.duel.enabled, volume: cfg.voice.volume, sfx: true, sfxVolume: cfg.sounds.volume, infoVoiceEverySec: cfg.voice.infoEverySec,
      duelEveryMin: cfg.duel.everyMs / 60000, duelDurationMin: cfg.duel.durationMs / 60000, announceStageUps: cfg.voice.announceStageUps,
      boss: cfg.boss.enabled, bossHp: cfg.boss.hp, bossEveryMin: cfg.boss.everyMs / 60000,
      tauHoa: cfg.possession.enabled, tauHoaTuvi: cfg.possession.thresholdTuvi, tauHoaWindowSec: cfg.possession.windowMs / 1000,
      kiep: cfg.tribulation.enabled, linhKhi: cfg.surge.enabled,
      settingsVersion: SETTINGS_VERSION };
  }

  /** "đạo hữu ABC" hoặc chỉ "đạo hữu" khi tên toàn ký tự lạ. */
  // Câu ĐỌC: tên lọc chỉ còn chữ đọc được (icon, kí tự lạ, chữ nước ngoài bỏ đi); không còn gì thì đọc "đạo hữu".
  function daoHuu(name) { var n = A.speakableName(name || ""); return n ? "đạo hữu " + n : "đạo hữu"; }
  // Câu HIỆN CHỮ: giữ nguyên tên (cả icon, kí tự đặc biệt).
  function daoHuuText(name) { return name ? "đạo hữu " + name : "đạo hữu"; }
  function cap(s) { return s.charAt(0).toUpperCase() + s.slice(1); }
  function shown(name) { return name ? A.displayName(name) : "Đạo hữu"; }
  function duration(ms) {
    var s = Math.max(0, Math.ceil(ms / 1000));
    return s < 60 ? s + " giây" : Math.floor(s / 60) + " phút " + (s % 60) + " giây";
  }

  function userOf(source) {
    var u = source && source.user;
    // Test Lab của TikTool gửi action không kèm người xem: dùng 1 đệ tử thử để streamer bấm thử được.
    if ((!u || !u.id) && source && source.mode === "test") return { id: "tiktool-test", nickname: "Đạo hữu thử" };
    if (!u || !u.id) throw new Error("Sự kiện không có người xem");
    return u;
  }


  function Game(config, deps) {
    deps = deps || {};
    var self = this;
    this.cfg = clone(config);
    this.cfg.layout = config.layout;
    this.now = deps.now || function () { return Date.now(); };
    this.storage = deps.storage || null;
    // Tu vi lưu THEO TUẦN (anh chốt 10/11): giữ qua mọi buổi live trong tuần, sang 0h thứ Hai thì reset.
    // scope tách dữ liệu live thật ("live") với lúc mở thử không qua TikTool ("demo").
    this.scope = deps.scope === "demo" ? "demo" : "live";
    this.week = weekKey(this.now());
    this.pendingJoins = [];
    this.pendingSides = { 1: [], 2: [] };   // người vừa vào phe, gom lại đọc 1 câu
    this.lastSideFlush = 0;
    this.lastJoinFlush = 0;
    this.running = false;
    this.bubbles = new Map();        // id -> { id, text, until }
    this.bubbleSeq = 0;
    this.fullNoticeAt = new Map();   // id -> lần cuối báo hết chỗ
    this.lastEventBannerAt = 0;
    this.lastInfoAt = 0;
    this.infoTurn = 0;

    // Thông báo gần đây được đưa vào getState() để renderer/overlay hiện banner và hiệu ứng trên đúng nhân vật.
    this.banners = [];
    this.bannerSeq = 0;
    this.announcer = new A.Announcer(this.cfg.voice, { speaker: deps.speaker, show: function (item) {
      var b = { id: ++self.bannerSeq, kind: item.kind, text: item.text, priority: item.priority, memberId: item.member ? item.member.id : null };
      self.banners.push(b);
      if (self.banners.length > 8) self.banners.shift();
      if (item.kind !== "info") self.lastEventBannerAt = self.now();
      if (typeof deps.show === "function") deps.show(item, b);
    } });
    this.sect = new C.Sect(this.cfg, { now: this.now, onEvent: function (e) { self.onSectEvent(e); } });
    this.duel = new D.Duel(this.cfg, { onEvent: function (e) { self.onDuelEvent(e); } });
    this.boss = new B.Boss(this.cfg, { onEvent: function (e) { self.onBossEvent(e); } });
    this.possession = new PS.Possession(this.cfg, { onEvent: function (e) { self.onPossessEvent(e); } });
    this.possessPending = null;   // người vừa vượt ngưỡng, chờ tỉ thí / đánh quái xong
    this.possessHits = [];        // đòn đánh của người tẩu hỏa + đòn giải cứu, cho renderer
    this.possessHitSeq = 0;
    this.random = deps.random || Math.random;
    this.cycleStart = 0;          // đầu chu kỳ tu luyện / tỉ thí; quái rồng ra ở cycleStart + boss.everyMs
    this.bossHits = [];           // vài cú đánh gần nhất, để renderer hiện số sát thương bay lên
    this.bossAttacks = [];        // đòn rồng đánh người chơi (thường / phun lửa), cho renderer
    this.bossAttackSeq = 0;
    this.bossAttackCount = 0;
    this.lastBossAttackAt = 0;
    this.bossHitSeq = 0;
    this.duelHits = [];           // đòn đánh giữa hai phe khi tỉ thí, cho renderer
    this.duelHitSeq = 0;
    this.itemFx = [];             // pháp bảo vừa dùng (renderer chạy hiệu ứng tại chỗ người tặng)
    this.itemFxSeq = 0;
    this.hatches = [];            // trứng đang chờ nở { id, fxId, userId, due }
    this.hatchFx = [];            // kết quả nở cho renderer { id, fxId, userId, pet, stars, result }
    this.hatchSeq = 0;
    this.kiep = null;             // thiên kiếp đang diễn ra
    this.kiepQueue = [];
    this.kiepSeq = 0;
    this.surge = null;            // Linh Khí Triều Dâng đang diễn ra { startAt, endsAt }
    this.nextSurgeAt = 0;
    this.wheel = null;            // vòng quay đang chạy
    this.wheelQueue = [];
    this.wheelSeq = 0;

    var stored = this.read(SETTINGS_KEY) || {};
    // Cài đặt cũ (trước v2): chu kỳ tỉ thí mặc định cũ 15'/5' đổi sang luật mới 10'/3'.
    if ((stored.settingsVersion || 1) < SETTINGS_VERSION) { delete stored.duelEveryMin; delete stored.duelDurationMin; stored.settingsVersion = SETTINGS_VERSION; }
    this.settings = Object.assign(defaultSettings(this.cfg), stored);
    this.applySettings(this.settings);
    var saved = this.read(this.saveKey());
    if (saved && saved.week === this.week) this.sect.load(saved.sect);
  }

  // ---------- Lưu trữ ----------
  Game.prototype.read = function (key) { try { return this.storage ? this.storage.get(key) : null; } catch (e) { return null; } };
  Game.prototype.write = function (key, value) { try { if (this.storage) this.storage.set(key, value); } catch (e) { /* đầy bộ nhớ: bỏ qua */ } };
  Game.prototype.saveKey = function () { return this.cfg.save.key + "." + this.scope; };
  /** Dữ liệu tuần đọc từ hub về (bất đồng bộ): gộp vào, mỗi người lấy tu vi lớn hơn. Bỏ qua nếu khác tuần. */
  Game.prototype.mergeSaved = function (saved) {
    if (!saved || saved.week !== this.week) return false;
    return this.sect.merge(saved.sect);
  };
  Game.prototype.save = function () {
    if (!this.sect.dirty) return;
    this.write(this.saveKey(), { week: this.week, sect: this.sect.serialize() });
    this.sect.dirty = false;
  };

  // ---------- Cài đặt (màn hình trước khi vào game và khi bấm Thoát) ----------
  Game.prototype.applySettings = function (next) {
    var s = this.settings = Object.assign({}, this.settings, next || {});
    this.cfg.duel.everyMs = Math.max(5, Number(s.duelEveryMin) || 15) * 60000;
    this.cfg.duel.durationMs = Math.min(this.cfg.duel.everyMs - 60000, Math.max(1, Number(s.duelDurationMin) || 5) * 60000);
    this.cfg.voice.volume = Math.min(1, Math.max(0, Number(s.volume)));
    this.cfg.voice.announceStageUps = s.announceStageUps !== false;
    this.cfg.boss.hp = Math.max(100, Math.floor(Number(s.bossHp) || 30000));
    this.cfg.boss.everyMs = Math.max(5, Number(s.bossEveryMin) || 30) * 60000;
    this.cfg.possession.thresholdTuvi = Math.max(1, Math.floor(Number(s.tauHoaTuvi) || 5000));
    this.cfg.possession.windowMs = Math.max(5, Number(s.tauHoaWindowSec) || 60) * 1000;
    if (!s.tauHoa && this.possession) {
      this.possessPending = null;
      if (this.possession.phase !== "idle") { this.possession.cancel(); this.restartCycle(this.now()); }
    }
    if (!s.kiep) { this.kiepQueue = []; this.kiep = null; }
    if (!s.linhKhi) this.surge = null;
    this.announcer.setVoice(!!s.voice);
    if (this.boss.phase !== "idle") {
      if (!s.boss) { this.boss.cancel(); this.cycleStart = this.now(); this.duel.setEnabled(!!s.duel && this.running, this.now()); }
    } else this.duel.setEnabled(!!s.duel && this.running, this.now());
    this.write(SETTINGS_KEY, s);
    return s;
  };

  /** Nút "Vào game" trên màn Cài đặt. */
  Game.prototype.enter = function () {
    this.running = true;
    this.announcer.setVoice(!!this.settings.voice);
    this.lastEventBannerAt = this.now();
    this.lastInfoSpokenAt = 0;
    this.cycleStart = this.now();
    if (this.settings.duel) { this.duel.enabled = true; this.duel.schedule(this.now()); }
    this.scheduleSurge(this.now());
  };

  /** Nút "Thoát": dừng nhận action, lưu, quay về màn Cài đặt. Trận tỉ thí dở dang bị hủy (không thưởng). */
  Game.prototype.exit = function () {
    this.running = false;
    this.duel.setEnabled(false, this.now());
    this.boss.cancel();               // đang đánh quái mà thoát: hủy, không thưởng; vào lại thì chu kỳ tính lại từ đầu
    this.possession.cancel();
    this.possessPending = null;
    this.tickHatches(Infinity);       // trứng đã mua mà chưa kịp nở: nở luôn, không để mất linh thú
    this.kiep = null; this.kiepQueue = [];
    this.surge = null;
    this.announcer.setVoice(false);   // xóa hàng đợi giọng đang chờ
    this.sect.dirty = true;
    this.save();
  };

  Game.prototype.resetAllData = function () {
    this.sect.reset(); this.save();
    if (this.storage && typeof this.storage.clear === "function") this.storage.clear(this.saveKey(), this.week);
  };

  // ---------- Sự kiện thô từ TikTool: thả tim / tặng quà = tham gia + giữ chỗ (KHÔNG cộng tu vi) ----------
  Game.prototype.handleLiveEvent = function (event) {
    if (!this.running || !event || !event.user || !event.user.id) return false;
    if (event.type !== "like" && event.type !== "gift") return false;
    return this.enterSeat(event.user);
  };

  /** Vào chỗ nếu chưa ngồi (hết chỗ thì báo "Tông môn đã đủ chỗ"), đang ngồi thì giữ chỗ. Trả true nếu đang có chỗ. */
  Game.prototype.enterSeat = function (user) {
    var r = this.sect.join(user);
    return r === "joined" || r === "already";
  };

  /** Bóng thoại trên đầu nhân vật. Lọc ký tự lạ như tên; comment là lệnh chọn phe "1"/"2" thì không hiện. */
  Game.prototype.say = function (id, text) {
    var t = String(text || "").normalize("NFC").replace(/[^\p{Script=Latin}\p{M}\p{N}\p{P} ]/gu, " ").replace(/\s+/g, " ").trim();
    if (!t || /^[12]$/.test(t)) return false;
    var arr = Array.from(t);
    if (arr.length > 40) t = arr.slice(0, 39).join("") + "…";
    this.bubbles.set(String(id), { id: ++this.bubbleSeq, text: t, until: this.now() + BUBBLE_MS });
    return true;
  };

  // ---------- Các function sự kiện cho TikTool (khai báo trong manifest.json "actions") ----------
  var ACTIONS = {
    /** Comment tự do (mapping "mọi comment", priority thấp nhất): hiện bóng thoại trên đầu người đang ngồi. */
    binh_luan: function (params, source) {
      var user = userOf(source);
      if (!this.sect.touch(user.id)) throw new Error("Người xem chưa tham gia (cần thả tim hoặc tặng quà)");
      if (!this.say(user.id, source.comment && source.comment.text)) throw new Error("Comment rỗng sau khi lọc ký tự lạ");
      return true;
    },
    /**
     * Cộng tu vi cho người đang ngồi. Số tu vi do streamer đặt trong TỪNG mapping (mỗi món quà một số, tim một số...),
     * TikTool tự nhân theo combo / số mốc tim. Đang tỉ thí và đã vào phe thì cộng cùng số đó vào điểm phe.
     */
    tu_luyen: function (params, source) {
      var user = userOf(source), amount = Math.floor(Number(params.amount) || 0);
      if (amount <= 0) throw new Error("amount phải > 0");
      this.cultivate(user, amount, source);
      return true;
    },
    /** Vào phe tỉ thí. Streamer map comment "1" -> side 1, comment "2" -> side 2. Chỉ có hiệu lực khi đang tỉ thí. */
    vao_phe: function (params, source) {
      var user = userOf(source);
      if (!this.settings.duel) throw new Error("Tỉ thí đang tắt trong Cài đặt");
      if (!this.sect.touch(user.id)) throw new Error("Người xem chưa tham gia (cần thả tim hoặc tặng quà)");
      var m = this.sect.members.get(String(user.id));
      var r = this.duel.join({ id: m.id, nickname: m.nickname }, Number(params.side));
      if (r !== true) throw new Error(r);
      return true;
    },
    end_duel: function () {
      if (this.duel.phase !== "duel") throw new Error("Không có trận nào");
      this.duel.finish(this.now());
      return true;
    },
    start_duel: function () {
      if (!this.settings.duel) throw new Error("Tỉ thí đang tắt trong Cài đặt");
      if (this.duel.phase !== "idle") throw new Error("Đang có trận");
      this.duel.start(this.now());
      return true;
    }
  };

  // Pháp bảo (bản 2): mỗi món là 1 action riêng, cộng tu vi như "tu_luyen" + hiệu ứng riêng. Thứ tự từ rẻ tới đắt.
  var ITEM_IDS = ["linh_thach", "tu_khi_dan", "chu_qua", "ban_dao", "linh_chi", "ngoc_dich", "bi_tich", "phi_kiem", "long_chau", "linh_thu_dan"];
  ITEM_IDS.forEach(function (itemId) {
    ACTIONS[itemId] = function (params, source) { return this.useItem(itemId, params, source); };
  });

  /** SDK onAction. Trả true khi chạy được; throw lỗi để TikTool ghi log "failed" kèm lý do. */
  Game.prototype.handleAction = function (action, params, source) {
    if (!this.running) throw new Error("Game đang ở màn Cài đặt");
    var fn = ACTIONS[action];
    if (!fn) throw new Error("Lệnh không hỗ trợ: " + action);
    return fn.call(this, params || {}, source || {});
  };

  // ---------- Vòng lặp ----------
  Game.prototype.tick = function (now) {
    // Sang tuần mới (0h thứ Hai) giữa buổi live: làm mới tu vi mọi người, ai đang ngồi vẫn ngồi.
    var week = weekKey(now);
    if (week !== this.week) {
      this.week = week;
      this.sect.resetTuvi();
      this.save();
      if (this.running) this.announcer.say({ kind: "week", priority: 3, text: "Tuần mới bắt đầu! Tu vi của mọi đạo hữu được làm mới",
        speech: "Tuần mới bắt đầu. Tu vi của mọi đạo hữu đã được làm mới, cùng tu luyện lại từ đầu nào" });
    }
    if (this.running) {
      this.sect.tick();
      this.duel.tick(now);
      this.boss.tick(now);
      if (this.boss.phase === "fight") this.bossAttackTick(now);
      this.possession.tick(now);
      // Ưu tiên: tẩu hỏa nhập ma chờ sẵn -> bắt đầu ngay khi không có tỉ thí / đánh quái.
      this.tryStartPossession(now);
      if (this.possession.phase === "active") this.sect.touch(this.possession.member.id);   // người bị tẩu hỏa không bị rời chỗ vì im lặng
      // Phút 30 của chu kỳ: quái rồng xuất hiện (chờ trận tỉ thí đang dở và màn kết quả xong).
      if (this.settings.boss && this.boss.phase === "idle" && this.duel.phase === "idle" && this.possession.phase === "idle" && !this.possessPending
          && !this.kiep && now - this.cycleStart >= this.cfg.boss.everyMs) this.spawnBoss(now);
      this.tickHatches(now);
      this.tickKiep(now);
      this.tryStartKiep(now);
      this.tickSurge(now);
      this.tickWheel(now);
      var self = this;
      this.bubbles.forEach(function (b, id) { if (b.until <= now || !self.sect.isSeated(id)) self.bubbles.delete(id); });
      if (this.pendingJoins.length && now - this.lastJoinFlush >= this.cfg.voice.joinBatchMs) this.flushJoins(now);
      if ((this.pendingSides[1].length || this.pendingSides[2].length) && now - this.lastSideFlush >= this.cfg.voice.sideJoinBatchMs) this.flushSides(now);
      if (now - this.lastEventBannerAt >= INFO_QUIET_MS && now - this.lastInfoAt >= INFO_QUIET_MS) this.showInfo(now);
    }
    if (!this.lastSave || now - this.lastSave >= this.cfg.save.everyMs) { this.lastSave = now; this.save(); }
  };


  /** Im 3 giây không có thông báo sự kiện: hiện luật chơi (chỉ chữ, không đọc). */
  /**
   * Im 3 giây không có thông báo sự kiện: hiện câu nhắc luật chơi (còn bao lâu tới tỉ thí, comment gì, cách nhập môn).
   * Chữ hiện mỗi 3 giây; giọng chỉ đọc tối đa 1 lần mỗi `infoVoiceEverySec` giây (Cài đặt, 0 = không đọc) để không ồn.
   */
  Game.prototype.showInfo = function (now) {
    this.lastInfoAt = now;
    var d = this.duel, c = this.cfg.duel, bonus = c.winBonus.toLocaleString("vi-VN");
    var join = { text: "Thả tim hoặc tặng quà để nhập môn Nga Mi", speech: "Thả tim hoặc tặng quà để nhập môn Nga Mi" };
    var info = join, bs = this.boss, psn = this.possession;
    if (this.kiep) return;   // đang độ kiếp: để banner thiên kiếp, không chen luật chơi
    if (this.surgeActive(now)) {
      info = { text: "Linh khí triều dâng · mọi tu vi x" + this.cfg.surge.multiplier + " · còn " + duration(this.surge.endsAt - now),
        speech: "Linh khí triều dâng, còn " + duration(this.surge.endsAt - now) + ", mọi tu vi nhân đôi" };
    } else if (psn.phase === "active") {
      var pn = psn.member.nickname, phpText = psn.hp.toLocaleString("vi-VN");
      info = { text: "Giải cứu " + shown(pn) + "! Thả tim, tặng quà để đánh thức · cứu thành công được nhân đôi tu vi đã góp",
        speech: "Còn " + phpText + " tu vi nữa là giải cứu được " + daoHuu(pn) + ". Thả tim và tặng quà để đánh thức, cứu thành công được nhân đôi tu vi đã góp" };
    } else if (psn.phase === "result") return;
    else if (bs.phase === "fight") {
      var hpText = bs.hp.toLocaleString("vi-VN") + "/" + bs.maxHp.toLocaleString("vi-VN"), mvpB = this.cfg.boss.mvpBonus.toLocaleString("vi-VN");
      info = { text: "Đánh quái! Thả tim, tặng quà để tấn công · gây sát thương nhiều nhất +" + mvpB + " tu vi + 1 lượt quay thưởng",
        speech: "Quái rồng còn " + hpText.replace("/", " trên ") + " máu. Thả tim và tặng quà để tấn công. Đạo hữu gây sát thương nhiều nhất được cộng " + mvpB + " tu vi và một lượt quay thưởng" };
    } else if (bs.phase === "result") return;
    else if (this.settings.boss && this.infoTurn % 4 === 3) {
      this.infoTurn++;
      var bossLeft = duration(this.cycleStart + this.cfg.boss.everyMs - now);
      info = { text: "Quái rồng xuất hiện sau " + bossLeft + ". Cả tông môn cùng đánh, công đầu được quay thưởng",
        speech: "Còn " + bossLeft + " nữa quái rồng sẽ xuất hiện. Cả tông môn hãy chuẩn bị" };
    } else if (this.settings.duel && d.phase === "idle" && d.nextStartAt) {
      var left = duration(d.nextStartAt - now);
      var duelInfo = { text: "Tỉ thí sẽ bắt đầu sau " + left + ", phe thắng mỗi người +" + bonus + " tu vi",
        speech: "Còn " + left + " nữa là tới đại hội tỉ thí. Phe thắng mỗi người được cộng " + bonus + " tu vi" };
      info = this.infoTurn++ % 3 === 2 ? join : duelInfo;
    } else if (this.settings.duel && d.phase === "duel") {
      var remain = duration(d.endsAt - now), sc = d.scores;
      var tally = this.sideName(1) + " " + sc[1].toLocaleString("vi-VN") + " điểm, " + this.sideName(2) + " " + sc[2].toLocaleString("vi-VN") + " điểm";
      info = { text: "Bình luận 1 vào " + this.sideName(1) + ", 2 vào " + this.sideName(2) + " · phe thắng mỗi người +" + bonus + " tu vi",
        speech: "Tỉ thí còn " + remain + ". Hiện tại " + tally + ". Bình luận số một vào phe " + this.sideName(1) + ", số hai vào phe " + this.sideName(2) + ". Phe thắng mỗi người được cộng " + bonus + " tu vi" };
    } else if (d.phase === "result") return;
    var every = Math.max(0, Number(this.settings.infoVoiceEverySec) || 0) * 1000;
    var speak = every > 0 && now - (this.lastInfoSpokenAt || 0) >= every;
    if (speak) this.lastInfoSpokenAt = now;
    this.announcer.say({ kind: "info", priority: 0, silent: !speak, text: info.text, speech: info.speech });
  };

  // ---------- Câu thông báo ----------
  Game.prototype.onSectEvent = function (e) {
    var name = e.member ? e.member.nickname : "";
    // returning = đã có trong dữ liệu (đã từng tham gia trong tuần này), ngược lại là người chơi lần đầu.
    if (e.type === "join") { this.pendingJoins.push({ name: name, returning: !!e.returning }); return; }
    if (e.type === "leave") { this.bubbles.delete(e.member.id); return; }
    if (e.type === "full") {
      var last = this.fullNoticeAt.get(e.member.id) || 0, now = this.now();
      if (now - last < this.cfg.fullNoticeCooldownMs) return;
      this.fullNoticeAt.set(e.member.id, now);
      var line = "Tông môn đã đủ chỗ, hẹn " + daoHuu(name) + " lần tuyển sinh tiếp theo";
      this.announcer.say({ kind: "full", priority: 1, key: "full", text: "Tông môn đã đủ chỗ, hẹn " + daoHuuText(name ? A.displayName(name) : "") + " lần tuyển sinh tiếp theo", speech: line });
      return;
    }
    if (e.type === "level_up") {
      var verb = e.bigRealm ? " đột phá thành công, bước vào cảnh giới " : " đạt cảnh giới ";
      var item = { kind: e.bigRealm ? "realm_up" : "level_up", member: e.member, key: "lv:" + e.member.id, priority: e.bigRealm ? 3 : 2,
        text: "Chúc mừng " + daoHuuText(name ? A.displayName(name) : "") + verb + e.to.label,
        speech: "Chúc mừng " + daoHuu(name) + verb + e.to.label };
      if (!e.bigRealm && !this.cfg.voice.announceStageUps) item.silent = true;
      this.announcer.say(item);
      if (e.bigRealm && e.reason !== "kiep" && e.to.realmIdx >= this.cfg.tribulation.fromRealm) this.queueKiep(e.member, e.to);
      return;
    }
    if (e.type === "seat_change") {
      this.announcer.say({ kind: "seat", member: e.member, priority: 3, key: "seat:" + e.seat,
        text: cap(daoHuuText(name ? A.displayName(name) : "")) + " trở thành " + e.seat + " Nga Mi",
        speech: cap(daoHuu(name)) + " trở thành " + e.seat + " phái Nga Mi" });
    }
  };

  /**
   * Gom người mới vào chỗ (10 giây) rồi chào 1 câu:
   *   người chơi lần đầu  -> "Hoan nghênh ... gia nhập Nga Mi"
   *   đã có trong dữ liệu -> "Chào mừng ... quay trở lại"
   */
  Game.prototype.flushJoins = function (now) {
    var all = this.pendingJoins.splice(0);
    this.lastJoinFlush = now;
    function group(list) {
      if (!list.length) return null;
      var named = list.map(function (j) { return j.name; }).filter(Boolean), first = named.slice(0, 3), rest = list.length - first.length;
      var spoken = list.map(function (j) { return A.speakableName(j.name); }).filter(Boolean), firstSp = spoken.slice(0, 3), restSp = list.length - firstSp.length;
      var who = firstSp.length ? firstSp.join(", ") + (restSp > 0 ? " cùng " + restSp + " vị đạo hữu khác" : "") : (list.length > 1 ? list.length + " vị đạo hữu" : "đạo hữu");
      var whoShown = first.length ? first.map(function (n) { return A.displayName(n); }).join(", ") + (rest > 0 ? " +" + rest : "") : (list.length > 1 ? list.length + " vị đạo hữu" : "đạo hữu");
      return { who: who, whoShown: whoShown, named: firstSp.length > 0 };
    }
    var fresh = group(all.filter(function (j) { return !j.returning; })), back = group(all.filter(function (j) { return j.returning; }));
    var texts = [], speeches = [];
    if (fresh) {
      texts.push("Hoan nghênh " + fresh.whoShown + " gia nhập Nga Mi");
      speeches.push("Hoan nghênh " + (fresh.named ? "đạo hữu " : "") + fresh.who + " gia nhập Nga Mi");
    }
    if (back) {
      texts.push("Chào mừng " + back.whoShown + " quay trở lại");
      speeches.push("Chào mừng " + (back.named ? "đạo hữu " : "") + back.who + " quay trở lại");
    }
    if (!texts.length) return;
    this.announcer.say({ kind: "join", priority: 1, key: "join", text: texts.join(" · "), speech: speeches.join(". ") });
  };


  /** Đọc người vừa vào phe: gom trong vài giây thành 1 câu mỗi phe, vd. "An, Bình cùng 3 vị khác gia nhập phe Thanh Liên". */
  Game.prototype.flushSides = function (now) {
    this.lastSideFlush = now;
    var self = this, shownParts = [], spokenParts = [];
    [1, 2].forEach(function (side) {
      var names = self.pendingSides[side].splice(0);
      if (!names.length) return;
      var named = names.filter(Boolean).slice(0, 3), rest = names.length - named.length;
      var sp = names.map(function (n) { return A.speakableName(n); }).filter(Boolean).slice(0, 3), restSp = names.length - sp.length;
      var who = sp.length ? sp.join(", ") + (restSp > 0 ? " cùng " + restSp + " vị đạo hữu khác" : "") : (names.length > 1 ? names.length + " vị đạo hữu" : "một vị đạo hữu");
      var whoShown = named.length ? named.map(function (n) { return A.displayName(n); }).join(", ") + (rest > 0 ? " +" + rest : "") : (names.length > 1 ? names.length + " vị đạo hữu" : "một vị đạo hữu");
      shownParts.push(whoShown + " vào " + self.sideName(side));
      spokenParts.push(who + " gia nhập phe " + self.sideName(side));
    });
    if (!shownParts.length) return;
    this.announcer.say({ kind: "side_join", priority: 3, key: "side_join", text: shownParts.join(" · "), speech: spokenParts.join(". ") });
  };

  Game.prototype.sideName = function (id) {
    var s = this.cfg.duel.sides.find(function (x) { return x.id === id; });
    return s ? s.name : "phe " + id;
  };

  Game.prototype.onDuelEvent = function (e) {
    var s1 = this.sideName(1), s2 = this.sideName(2), self = this;
    if (e.type === "duel_soon") {
      this.announcer.say({ kind: "duel_soon", priority: 3, text: "1 phút nữa bắt đầu tỉ thí!",
        speech: "Còn một phút nữa, đại hội tỉ thí Nga Mi bắt đầu. Chuẩn bị chọn phe " + s1 + " hoặc " + s2 });
    } else if (e.type === "duel_start") {
      this.announcer.say({ kind: "duel_start", priority: 3, text: "Tỉ thí bắt đầu! Bình luận 1 vào " + s1 + ", 2 vào " + s2,
        speech: "Tỉ thí bắt đầu! Bình luận số một để vào phe " + s1 + ", số hai để vào phe " + s2 + ". Thả tim và tặng quà để giành chiến thắng" });
    } else if (e.type === "duel_join") {
      var jm = this.sect.members.get(String(e.userId));
      this.pendingSides[e.side].push(jm ? jm.nickname : "");
    } else if (e.type === "duel_end") {
      var c = this.cfg.duel, win = e.winner;
      this.pendingSides = { 1: [], 2: [] };
      var mvp = win && e.mvp[win];
      // Công đầu phe thắng: thêm đúng số tu vi đã góp trong trận (x2). Người khác phe thắng: +300. Hòa: mỗi người +100.
      var mvpGain = mvp ? Math.floor(mvp.points * ((c.mvpMultiplier || 2) - 1)) : 0;
      e.participants.forEach(function (p) {
        var bonus = win === 0 ? c.drawBonus : (p.side === win ? c.winBonus : 0);
        if (mvp && mvp.id === p.id) bonus = mvpGain;
        if (bonus > 0) self.sect.addTuvi({ id: p.id }, bonus, "duel", true);
      });
      this.lastDuelResult = { winner: win, scores: e.scores, mvp: mvp ? (mvp.nickname || "Đạo hữu") : null };
      if (mvp && c.mvpWheel) this.queueWheel({ id: mvp.id, nickname: mvp.nickname }, "duel");
      var fmt = function (n) { return n.toLocaleString("vi-VN"); }, score = fmt(e.scores[1]) + " – " + fmt(e.scores[2]);
      this.announcer.say({ kind: "duel_end", priority: 3,
        text: win ? "Phe " + this.sideName(win) + " thắng " + score + "! Mỗi người +" + fmt(c.winBonus) + " tu vi" + (mvp ? ", công đầu " + shown(mvp.nickname) + " x" + (c.mvpMultiplier || 2) + " tu vi đã góp (+" + fmt(mvpGain) + ")" : "")
                  : "Hai phe hòa " + score + "! Mỗi người +" + fmt(c.drawBonus) + " tu vi",
        speech: win ? "Phe " + this.sideName(win) + " chiến thắng với tỉ số " + score.replace("–", "trên") + ". Mỗi đạo hữu phe thắng được cộng " + fmt(c.winBonus) + " tu vi"
                      + (mvp ? ". Công đầu thuộc về " + daoHuu(mvp.nickname) + ", được nhân đôi tu vi đã góp, cộng thêm " + fmt(mvpGain) + " tu vi và một lượt quay thưởng" : "")
                    : "Trận tỉ thí kết thúc, hai phe bất phân thắng bại. Mỗi đạo hữu tham gia được cộng " + fmt(c.drawBonus) + " tu vi" });
    }
  };

  // ---------- Tẩu hỏa nhập ma ----------
  /** Bắt đầu tẩu hỏa nếu đang chờ và không có tỉ thí / đánh quái. Trả true nếu đã bắt đầu. */
  /** Người phe A góp tu vi khi tỉ thí = tung 1 đòn trúng ngẫu nhiên 1 người đang ngồi của phe B. Không trừ tu vi ai. */
  Game.prototype.duelAttack = function (attackerId, amount, dmg) {
    if (dmg == null) dmg = amount;
    var me = this.duel.players.get(attackerId);
    if (!me) return;
    var foes = [], self = this;
    this.duel.players.forEach(function (p) { if (p.side !== me.side && self.sect.seated.has(p.id)) foes.push(p.id); });
    var target = foes.length ? foes[Math.floor(this.random() * foes.length)] : null;
    // Người trúng đòn mất 50% lượng tu vi của đòn (không xuống dưới 0, không trừ điểm phe).
    var lost = target != null ? this.sect.removeTuvi(target, Math.floor(dmg * (this.cfg.duel.hitLossRatio || 0))) : 0;
    this.duelHits.push({ id: ++this.duelHitSeq, from: attackerId, to: target, side: me.side, amount: amount, lost: lost });
    if (this.duelHits.length > 12) this.duelHits.shift();
  };

  Game.prototype.tryStartPossession = function (now) {
    if (!this.possessPending || this.duel.phase !== "idle" || this.boss.phase !== "idle" || this.possession.phase !== "idle" || this.kiep) return false;
    var m = this.possessPending; this.possessPending = null;
    if (!this.sect.members.has(m.id)) return false;
    this.duel.pause();
    this.possessHits = [];
    this.possession.start(now, m, this.cfg.boss.hp);
    return true;
  };

  /** Người tẩu hỏa nhận X tu vi -> đánh 1 người ngẫu nhiên đang ngồi, người đó mất X tu vi (không âm). */
  Game.prototype.possessAttack = function (attackerId, amount) {
    var self = this, targets = [];
    this.sect.seated.forEach(function (id) { if (id !== attackerId && self.sect.members.get(id).tuvi > 0) targets.push(id); });
    if (!targets.length) return;
    var target = targets[Math.floor(this.random() * targets.length)], lost = this.sect.removeTuvi(target, amount);
    if (!lost) return;
    this.possessHits.push({ id: ++this.possessHitSeq, kind: "attack", targetId: target, amount: lost });
    if (this.possessHits.length > 16) this.possessHits.shift();
    var t = this.sect.members.get(target), a = this.sect.members.get(attackerId);
    this.announcer.say({ kind: "possess_attack", priority: 1, silent: true, key: "possess_attack",
      text: shown(a ? a.nickname : "") + " đánh trúng " + shown(t ? t.nickname : "") + ": -" + lost.toLocaleString("vi-VN") + " tu vi" });
  };

  Game.prototype.restartCycle = function (now) {
    this.cycleStart = now;
    if (this.settings.duel && this.running) { this.duel.enabled = true; this.duel.schedule(now); }
  };

  Game.prototype.onPossessEvent = function (e) {
    var fmt = function (n) { return n.toLocaleString("vi-VN"); }, self = this;
    if (e.type === "possess_start") {
      this.announcer.say({ kind: "possess_start", priority: 3, member: e.member,
        text: cap(daoHuuText(shown(e.member.nickname))) + " tẩu hỏa nhập ma! Thả tim, tặng quà để giải cứu (" + fmt(e.hp) + ")",
        speech: cap(daoHuu(e.member.nickname)) + " tu luyện quá nhanh, đã tẩu hỏa nhập ma và đang tấn công đồng môn! Cả tông môn thả tim, tặng quà để giải cứu. Cứu thành công được nhân đôi tu vi đã góp" });
    } else if (e.type === "possess_rescue_hit") {
      this.possessHits.push({ id: ++this.possessHitSeq, kind: "rescue", userId: e.userId, amount: e.amount });
      if (this.possessHits.length > 16) this.possessHits.shift();
    } else if (e.type === "possess_milestone") {
      this.announcer.say({ kind: "possess_milestone", priority: 2, key: "possess_milestone", text: "Sắp giải cứu được! Còn " + e.percent + "%",
        speech: "Chỉ còn " + e.percent + " phần trăm nữa là giải cứu được, cố lên!" });
    } else if (e.type === "possess_freed") {
      var mult = this.cfg.possession.rewardMultiplier;
      e.rescuers.forEach(function (r) { self.sect.addTuvi({ id: r.id }, Math.floor(r.amount * (mult - 1)), "rescue", true); });
      var top = e.rescuers[0], n = e.rescuers.length;
      this.lastPossessResult = { name: shown(e.member.nickname), rescuers: n, top: top ? shown(top.nickname) : null };
      this.announcer.say({ kind: "possess_freed", priority: 3, member: e.member,
        text: cap(daoHuuText(shown(e.member.nickname))) + " đã được giải cứu! " + n + " người góp sức được x" + mult + " tu vi đã góp",
        speech: cap(daoHuu(e.member.nickname)) + " đã được giải cứu! " + n + " vị đạo hữu góp sức được nhân " + (mult === 2 ? "đôi" : mult) + " tu vi đã góp"
          + (top ? ". Góp sức nhiều nhất là " + daoHuu(top.nickname) : "") });
    } else if (e.type === "possess_over") {
      // Hết tẩu hỏa: chu kỳ tính lại từ đầu (7 phút sau có tỉ thí).
      this.restartCycle(this.now());
    }
  };

  // ---------- Tu luyện chung: tim, quà thường, pháp bảo ----------
  /** % cộng thêm từ linh thú: { tuviPct, dmgPct }. Mỗi sao cộng đúng % của 1 sao (5% / 10% / 15%). */
  Game.prototype.petBonus = function (m) {
    var out = { tuviPct: 0, dmgPct: 0 };
    if (!m || !m.pets) return out;
    this.cfg.pets.types.forEach(function (t) { var n = m.pets[t.id] || 0; out.tuviPct += t.tuviPct * n; out.dmgPct += t.dmgPct * n; });
    return out;
  };

  Game.prototype.surgeActive = function (now) { return !!this.surge && now < this.surge.endsAt; };

  /**
   * Cộng tu vi từ tim / quà / pháp bảo (đường duy nhất cộng tu vi theo mapping). Trả số tu vi thực nhận.
   * Tu vi = gốc x (1 + % linh thú) x (2 khi Linh Khí Triều Dâng). Sát thương (rồng, giải cứu, kiếm tỉ thí) = tu vi x (1 + % sát thương).
   */
  Game.prototype.cultivate = function (user, base, source) {
    // Tim / quà là tham gia: chưa có chỗ thì vào chỗ luôn (sự kiện thô thường đã làm việc này trước).
    if (!this.enterSeat(user)) throw new Error("Tông môn đã đủ chỗ, không cộng tu vi");
    var id = String(user.id), now = this.now(), m = this.sect.members.get(id), bonus = this.petBonus(m);
    var amount = Math.floor(base * (1 + bonus.tuviPct / 100) * (this.surgeActive(now) ? this.cfg.surge.multiplier : 1));
    var dmg = Math.floor(amount * (1 + bonus.dmgPct / 100));
    this.sect.addTuvi(user, amount, (source && source.eventType) || "action");
    if (m) m.lastGainAt = now;     // hào quang sau lưng
    // Tỉ thí: chỉ tính khi đang đấu và người này đã vào phe; mỗi lần góp là 1 đòn kiếm sang phe kia.
    if (this.duel.addPoints(id, amount)) this.duelAttack(id, amount, dmg);
    // Tẩu hỏa nhập ma: người bị tẩu hỏa đánh 1 người ngẫu nhiên; người khác góp sức giải cứu.
    var ps = this.possession;
    if (ps.phase === "active") {
      if (ps.isPossessed(id)) this.possessAttack(id, amount);
      else ps.rescueHit({ id: id, nickname: m ? m.nickname : "" }, dmg, now);
    } else if (this.settings.tauHoa && this.running && ps.phase === "idle" && !this.possessPending && ps.track(id, amount, now)) {
      this.possessPending = { id: id, nickname: m ? m.nickname : "" };
      if (!this.tryStartPossession(now)) {
        this.announcer.say({ kind: "possess_warn", priority: 2, text: cap(daoHuuText(shown(this.possessPending.nickname))) + " khí tức hỗn loạn, sắp tẩu hỏa nhập ma!",
          speech: cap(daoHuu(this.possessPending.nickname)) + " tu luyện quá nhanh, khí tức hỗn loạn, sắp tẩu hỏa nhập ma!" });
      }
    }
    // Đang đánh quái: sát thương lên rồng.
    if (this.boss.phase === "fight") this.boss.hit({ id: id, nickname: m ? m.nickname : "" }, dmg, now);
    // Thiên kiếp: người khác góp tu vi = hộ pháp, dựng kết giới cho người đang độ kiếp.
    var k = this.kiep;
    if (k && k.phase === "strike" && k.targetId !== id) { k.shield += amount; k.helpers[id] = (k.helpers[id] || 0) + amount; }
    return amount;
  };

  // ---------- Pháp bảo ----------
  Game.prototype.itemDef = function (itemId) {
    return this.cfg.items.filter(function (it) { return it.id === itemId; })[0] || { id: itemId, name: itemId, fx: "rise", color: "#ffffff" };
  };

  Game.prototype.useItem = function (itemId, params, source) {
    var user = userOf(source), base = Math.floor(Number(params.amount) || 0);
    if (base <= 0) throw new Error("amount phải > 0");
    var item = this.itemDef(itemId), count = Math.max(1, Math.floor(Number(params.count) || 1)), now = this.now();
    var gained = this.cultivate(user, base, source), id = String(user.id), m = this.sect.members.get(id);
    var fx = { id: ++this.itemFxSeq, item: itemId, userId: id, count: count, amount: gained };
    this.itemFx.push(fx);
    if (this.itemFx.length > 16) this.itemFx.shift();
    if (itemId === "linh_thu_dan") {
      var eggs = Math.min(count, this.cfg.pets.maxEggsPerEvent);
      for (var i = 0; i < eggs; i++) this.hatches.push({ id: ++this.hatchSeq, fxId: fx.id, userId: id, due: now + this.cfg.pets.hatchMs + i * 700 });
    }
    var name = m ? m.nickname : "", times = count > 1 ? " x" + count : "", fmt = gained.toLocaleString("vi-VN");
    this.announcer.say({ kind: "item", priority: item.speak ? 2 : 1, silent: !item.speak, member: m, key: "item:" + id + ":" + itemId,
      text: shown(name) + " dùng " + item.name + times + "! +" + fmt + " tu vi",
      speech: cap(daoHuu(name)) + " dùng " + item.name + ", tăng " + fmt + " tu vi" });
    return true;
  };

  // ---------- Linh thú: trứng nở ----------
  Game.prototype.pickPet = function () {
    var types = this.cfg.pets.types, total = types.reduce(function (n, t) { return n + t.weight; }, 0), r = this.random() * total;
    for (var i = 0; i < types.length; i++) { r -= types[i].weight; if (r < 0) return types[i]; }
    return types[types.length - 1];
  };

  /** Trứng tới giờ nở: chọn linh thú ngẫu nhiên; chưa có thì 1 sao, trùng thì +1 sao, đã tối đa sao thì 1 lượt vòng quay. */
  Game.prototype.tickHatches = function (now) {
    var self = this, P = this.cfg.pets;
    this.hatches = this.hatches.filter(function (h) {
      if (h.due > now) return true;
      var m = self.sect.members.get(h.userId);
      if (!m) return false;
      if (!m.pets) m.pets = {};
      var t = self.pickPet(), had = m.pets[t.id] || 0, result;
      if (had >= P.maxStars) { result = "wheel"; self.queueWheel({ id: m.id, nickname: m.nickname }, "pet"); }
      else { m.pets[t.id] = had + 1; result = had ? "star" : "new"; }
      self.sect.dirty = true;
      var stars = m.pets[t.id], b = self.petBonus({ pets: (function () { var o = {}; o[t.id] = stars; return o; })() });
      self.hatchFx.push({ id: h.id, fxId: h.fxId, userId: m.id, pet: t.id, stars: stars, result: result });
      if (self.hatchFx.length > 12) self.hatchFx.shift();
      var effect = [b.tuviPct ? "+" + b.tuviPct + "% tu vi" : "", b.dmgPct ? "+" + b.dmgPct + "% sát thương" : ""].filter(Boolean).join(", ");
      var starText = stars + " sao";
      var text = result === "wheel" ? shown(m.nickname) + " nở ra " + t.name + " nhưng đã đủ " + P.maxStars + " sao: được 1 lượt quay thưởng!"
        : shown(m.nickname) + (result === "new" ? " nở ra " + t.name + "! " : " nở thêm " + t.name + ", hợp thể lên " + starText + "! ") + "(" + effect + ")";
      var speech = result === "wheel" ? cap(daoHuu(m.nickname)) + " nở ra " + t.name + " đã đủ " + P.maxStars + " sao, được một lượt quay thưởng"
        : cap(daoHuu(m.nickname)) + (result === "new" ? " nở ra linh thú " + t.name : " có " + t.name + " hợp thể lên " + starText) + ", " + effect.replace(/%/g, " phần trăm");
      self.announcer.say({ kind: "hatch", priority: 3, member: m, text: text, speech: speech });
      return false;
    });
  };

  // ---------- Thiên kiếp ----------
  Game.prototype.queueKiep = function (member, level) {
    if (!this.settings.kiep || !this.running || !member) return;
    var id = String(member.id), q = this.kiepQueue.filter(function (x) { return x.id === id; })[0];
    if (this.kiep && this.kiep.targetId === id) return;
    if (q) { q.realmIdx = Math.max(q.realmIdx, level.realmIdx); return; }
    if (this.kiepQueue.length >= this.cfg.tribulation.maxQueue) return;
    this.kiepQueue.push({ id: id, realmIdx: level.realmIdx });
  };

  /** Bắt đầu thiên kiếp khi không có tỉ thí / đánh quái / tẩu hỏa, và tỉ thí kế tiếp còn đủ xa. */
  Game.prototype.tryStartKiep = function (now) {
    var T = this.cfg.tribulation;
    if (this.kiep || !this.kiepQueue.length || !this.settings.kiep) return false;
    if (this.duel.phase !== "idle" || this.boss.phase !== "idle" || this.possession.phase !== "idle" || this.possessPending) return false;
    if (this.duel.enabled && this.duel.nextStartAt && this.duel.nextStartAt - now < T.durationMs + T.resultMs + 2000) return false;
    var q = this.kiepQueue.shift(), m = this.sect.members.get(q.id);
    if (!m || !this.sect.isSeated(q.id)) return false;
    var realm = this.cfg.realms[q.realmIdx], need = Math.max(100, Math.min(T.shieldMax, Math.round(realm.stages[0] * T.shieldPct / 100)));
    this.kiep = { id: ++this.kiepSeq, targetId: q.id, name: m.nickname, realmIdx: q.realmIdx, realmName: realm.name, startAt: now, endsAt: now + T.durationMs,
      need: need, shield: 0, helpers: {}, strikes: T.strikes[q.realmIdx] || 3, struck: 0, log: [], phase: "strike", success: null };
    this.announcer.say({ kind: "kiep_start", priority: 3, member: m,
      text: "Lôi kiếp giáng xuống " + shown(m.nickname) + "! Thả tim, tặng quà hộ pháp (" + need.toLocaleString("vi-VN") + ")",
      speech: cap(daoHuu(m.nickname)) + " đột phá " + realm.name + ", lôi kiếp giáng xuống! Đồng môn mau thả tim, tặng quà hộ pháp" });
    return true;
  };

  Game.prototype.tickKiep = function (now) {
    var k = this.kiep, T = this.cfg.tribulation;
    if (!k) return;
    if (k.phase === "strike") {
      this.sect.touch(k.targetId);   // đang độ kiếp thì không bị rời chỗ vì im lặng
      // Các đạo sét chia đều trong thời gian độ kiếp. Kết giới đủ phần của đạo đó thì chặn được.
      while (k.struck < k.strikes && now >= k.startAt + (k.struck + 1) * T.durationMs / (k.strikes + 1)) {
        k.struck++;
        k.log.push({ n: k.struck, blocked: k.shield >= k.need * k.struck / k.strikes });
      }
      if (now < k.endsAt) return;
      var m = this.sect.members.get(k.targetId);
      k.success = k.shield >= k.need;
      k.phase = "result";
      k.resultUntil = now + T.resultMs;
      var helpers = Object.keys(k.helpers).sort(function (a, b) { return k.helpers[b] - k.helpers[a]; }), top = helpers[0] ? this.sect.members.get(helpers[0]) : null;
      if (k.success && m) {
        k.bonus = Math.floor(m.tuvi * T.successBonusPct / 100);
        this.sect.addTuvi({ id: m.id }, k.bonus, "kiep", true);
        if (top) this.sect.addTuvi({ id: top.id }, T.topHelperBonus, "kiep", true);
      }
      k.topHelper = top ? top.nickname : null;
      var fmt = function (n) { return (n || 0).toLocaleString("vi-VN"); };
      this.announcer.say({ kind: "kiep_end", priority: 3, member: m,
        text: k.success ? shown(k.name) + " độ kiếp thành công! +" + fmt(k.bonus) + " tu vi" + (top ? " · hộ pháp đầu " + shown(top.nickname) + " +" + fmt(T.topHelperBonus) : "")
                        : shown(k.name) + " vượt qua lôi kiếp, kết giới chưa đủ nên không có thưởng",
        speech: k.success ? "Chúc mừng " + daoHuu(k.name) + " độ kiếp thành công, được cộng " + fmt(k.bonus) + " tu vi" + (top ? ". Hộ pháp nhiều nhất là " + daoHuu(top.nickname) : "")
                          : cap(daoHuu(k.name)) + " đã vượt qua lôi kiếp. Lần sau đồng môn hộ pháp nhiều hơn để được thưởng nhé" });
      return;
    }
    if (k.phase === "result" && now >= k.resultUntil) this.kiep = null;
  };

  // ---------- Linh Khí Triều Dâng ----------
  Game.prototype.scheduleSurge = function (now) {
    var S = this.cfg.surge;
    this.nextSurgeAt = now + S.minEveryMs + Math.floor(this.random() * Math.max(0, S.maxEveryMs - S.minEveryMs));
  };

  Game.prototype.tickSurge = function (now) {
    var S = this.cfg.surge;
    if (this.surge && now >= this.surge.endsAt) {
      this.surge = null;
      this.scheduleSurge(now);
      this.announcer.say({ kind: "surge_end", priority: 1, silent: true, text: "Linh khí đã lắng xuống" });
      return;
    }
    if (this.surge || !this.settings.linhKhi || !this.nextSurgeAt || now < this.nextSurgeAt) return;
    this.surge = { startAt: now, endsAt: now + S.durationMs };
    var sec = Math.round(S.durationMs / 1000);
    this.announcer.say({ kind: "surge_start", priority: 3, text: "Linh khí triều dâng! " + sec + " giây tới mọi tu vi x" + S.multiplier,
      speech: "Linh khí triều dâng! Trong " + sec + " giây tới, mọi tu vi nhận được đều nhân " + (S.multiplier === 2 ? "đôi" : S.multiplier) + ". Thả tim, tặng quà ngay!" });
  };

  // ---------- Quái rồng ----------
  Game.prototype.spawnBoss = function (now) {
    this.duel.pause();
    this.bossHits = [];
    this.bossAttacks = [];
    this.bossAttackCount = 0;
    this.lastBossAttackAt = now;
    this.boss.start(now, this.cfg.boss.hp);
  };

  /** Rồng đánh người chơi: mỗi attackEveryMs 1 đòn thường (1 người ngẫu nhiên), cứ đòn thứ fireEvery thì phun lửa (tất cả). */
  Game.prototype.bossAttackTick = function (now) {
    var b = this.cfg.boss;
    if (now - this.lastBossAttackAt < b.attackEveryMs) return;
    this.lastBossAttackAt = now;
    this.bossAttackCount += 1;
    var self = this, seated = [];
    this.sect.seated.forEach(function (id) { seated.push(id); });
    if (!seated.length) return;
    var fire = this.bossAttackCount % b.fireEvery === 0, hits = [];
    if (fire) {
      seated.forEach(function (id) { var lost = self.sect.removeTuvi(id, b.fireDamage); hits.push({ id: id, amount: lost }); });
      this.announcer.say({ kind: "boss_fire", priority: 2, key: "boss_fire", text: b.name + " phun lửa! Cả tông môn -" + b.fireDamage + " tu vi",
        speech: b.name + " phun lửa! Cả tông môn mất " + b.fireDamage + " tu vi" });
    } else {
      var target = seated[Math.floor(this.random() * seated.length)];
      hits.push({ id: target, amount: this.sect.removeTuvi(target, b.attackDamage) });
      var t = this.sect.members.get(target);
      this.announcer.say({ kind: "boss_attack", priority: 1, silent: true, key: "boss_attack",
        text: b.name + " đánh trúng " + shown(t ? t.nickname : "") + ": -" + hits[0].amount + " tu vi" });
    }
    this.bossAttacks.push({ id: ++this.bossAttackSeq, kind: fire ? "fire" : "single", hits: hits });
    if (this.bossAttacks.length > 6) this.bossAttacks.shift();
  };

  Game.prototype.onBossEvent = function (e) {
    var fmt = function (n) { return n.toLocaleString("vi-VN"); }, b = this.cfg.boss;
    if (e.type === "boss_start") {
      this.announcer.say({ kind: "boss_start", priority: 3, text: "Quái rồng " + b.name + " xuất hiện! Thả tim, tặng quà để tấn công (" + fmt(e.hp) + " máu)",
        speech: "Quái rồng " + b.name + " đã xuất hiện! Cả tông môn thả tim và tặng quà để tấn công. Đạo hữu gây sát thương nhiều nhất được cộng " + fmt(b.mvpBonus) + " tu vi và một lượt quay thưởng" });
    } else if (e.type === "boss_hit") {
      this.bossHits.push({ id: ++this.bossHitSeq, userId: e.userId, amount: e.amount });
      if (this.bossHits.length > 12) this.bossHits.shift();
    } else if (e.type === "boss_milestone") {
      this.announcer.say({ kind: "boss_milestone", priority: 2, key: "boss_milestone", text: "Quái rồng chỉ còn " + e.percent + "% máu!",
        speech: "Quái rồng chỉ còn " + e.percent + " phần trăm máu. Cố lên chư vị đạo hữu!" });
    } else if (e.type === "boss_killed") {
      var top = e.top;
      if (top) {
        this.sect.addTuvi({ id: top.id }, b.mvpBonus, "boss", true);
        this.queueWheel({ id: top.id, nickname: top.nickname }, "boss");
      }
      this.lastBossResult = top ? { name: shown(top.nickname), damage: top.damage } : null;
      this.announcer.say({ kind: "boss_killed", priority: 3,
        text: "Quái rồng đã bị tiêu diệt!" + (top ? " Công đầu " + shown(top.nickname) + " (" + fmt(top.damage) + " sát thương) +" + fmt(b.mvpBonus) + " tu vi + 1 lượt quay" : ""),
        speech: "Quái rồng đã bị tiêu diệt!" + (top ? " Công đầu thuộc về " + daoHuu(top.nickname) + " với " + fmt(top.damage) + " sát thương, được cộng " + fmt(b.mvpBonus) + " tu vi và một lượt quay thưởng" : "") });
    } else if (e.type === "boss_over") {
      // Rồng chết xong: chu kỳ tu luyện / tỉ thí tính lại từ đầu (tẩu hỏa đang chờ sẽ bắt đầu ngay ở tick sau).
      this.restartCycle(this.now());
    }
  };

  // ---------- Vòng quay thưởng ----------
  /** Chọn ô thưởng theo trọng số. Trả chỉ số ô. */
  Game.prototype.pickPrize = function () {
    var prizes = this.cfg.wheel.prizes, total = prizes.reduce(function (n, p) { return n + p.weight; }, 0), r = this.random() * total;
    for (var i = 0; i < prizes.length; i++) { r -= prizes[i].weight; if (r < 0) return i; }
    return prizes.length - 1;
  };

  Game.prototype.queueWheel = function (member, reason) {
    this.wheelQueue.push({ member: member, reason: reason });
    if (!this.wheel) this.startWheel(this.now());
  };

  Game.prototype.startWheel = function (now) {
    var next = this.wheelQueue.shift();
    if (!next) { this.wheel = null; return; }
    var index = this.pickPrize(), m = this.sect.members.get(String(next.member.id));
    var name = m ? m.nickname : next.member.nickname;
    this.wheel = { id: ++this.wheelSeq, memberId: String(next.member.id), name: name, reason: next.reason, index: index,
      prize: this.cfg.wheel.prizes[index].tuvi, startAt: now, spinMs: this.cfg.wheel.spinMs, applied: false };
    this.announcer.say({ kind: "wheel_start", priority: 3, text: shown(name) + " quay thưởng!",
      speech: daoHuu(name).replace(/^đ/, "Đ") + " được một lượt quay thưởng" });
  };

  Game.prototype.tickWheel = function (now) {
    var w = this.wheel;
    if (!w) return;
    if (!w.applied && now >= w.startAt + w.spinMs) {
      w.applied = true;
      this.sect.addTuvi({ id: w.memberId }, w.prize, "wheel", true);
      var fmt = w.prize.toLocaleString("vi-VN");
      this.announcer.say({ kind: "wheel_result", priority: 3, member: this.sect.members.get(w.memberId),
        text: shown(w.name) + " quay trúng +" + fmt + " tu vi!", speech: "Chúc mừng " + daoHuu(w.name) + " quay trúng " + fmt + " tu vi" });
    }
    if (w.applied && now >= w.startAt + w.spinMs + WHEEL_SHOW_MS) this.startWheel(now);
  };

  // ---------- State gửi lên TikTool (overlay mirror) và renderer ----------
  Game.prototype.getState = function () {
    var self = this;
    var now = this.now(), duel = this.duel, showSides = duel.phase !== "idle", bubbles = this.bubbles;
    var psn = this.possession, possessedId = psn.phase !== "idle" && psn.member ? psn.member.id : null, sect = this.sect;
    var auraMs = this.cfg.aura.ms, petTypes = this.cfg.pets.types, k = this.kiep;
    return {
      running: this.running,
      settings: { voice: this.settings.voice, duel: this.settings.duel },
      // Người đang tẩu hỏa đứng ở chỗ quái, không ngồi trong hàng; các ghế dồn lên.
      // Người đang độ kiếp cũng đứng riêng phía trên (renderer vẽ to dưới mây giông), không ngồi trong hàng.
      seats: this.sect.seating().filter(function (s) { return (!possessedId || s.member.id !== possessedId) && (!k || s.member.id !== k.targetId); }).map(function (s, i) {
        var b = bubbles.get(s.member.id);
        return { rank: i + 1, seat: C.TOP_SEATS[i] || null, id: s.member.id, name: shown(s.member.nickname), tuvi: s.member.tuvi,
          realm: s.level.realmIdx, level: s.level.index, label: s.level.label, progress: s.level.progress, sectRank: s.sectRank.id,
          fading: s.fading, bubble: b ? { id: b.id, text: b.text } : null,
          side: showSides && duel.players.has(s.member.id) ? duel.players.get(s.member.id).side : 0,
          duelPoints: showSides && duel.players.has(s.member.id) ? duel.players.get(s.member.id).points : 0,
          aura: !!s.member.lastGainAt && now - s.member.lastGainAt < auraMs,
          pets: petTypes.filter(function (t) { return s.member.pets && s.member.pets[t.id]; }).map(function (t) { return { id: t.id, stars: s.member.pets[t.id] }; }) };
      }),
      capacity: this.cfg.capacity,
      total: this.sect.seated.size,
      duel: Object.assign(duel.snapshot(now), { sides: this.cfg.duel.sides, lastResult: this.lastDuelResult || null, hits: this.duelHits.slice() }),
      boss: Object.assign(this.boss.snapshot(), { enabled: !!this.settings.boss, hits: this.bossHits.slice(), attacks: this.bossAttacks.slice(), lastResult: this.lastBossResult || null,
        nextInMs: this.settings.boss && this.boss.phase === "idle" ? Math.ceil(Math.max(0, this.cycleStart + this.cfg.boss.everyMs - now) / 1000) * 1000 : null }),
      possession: psn.phase === "idle" ? { phase: "idle", pending: this.possessPending ? shown(this.possessPending.nickname) : null }
        : Object.assign(psn.snapshot(), (function () {
            var m = sect.members.get(possessedId), lv = m ? sect.levelOf(m.tuvi) : null;
            return { name: shown(psn.member.nickname), tuvi: m ? m.tuvi : 0, label: lv ? lv.label : "", realm: lv ? lv.realmIdx : 0,
              hits: self.possessHits.slice(), lastResult: self.lastPossessResult || null,
              top: psn.snapshot().top.map(function (t) { return { id: t.id, name: shown(t.name), amount: t.amount }; }) };
          })()),
      wheel: this.wheel ? { id: this.wheel.id, name: shown(this.wheel.name), memberId: this.wheel.memberId, reason: this.wheel.reason, index: this.wheel.index,
        prize: this.wheel.prize, spinMs: this.wheel.spinMs, elapsed: now - this.wheel.startAt, applied: this.wheel.applied,
        prizes: this.cfg.wheel.prizes.map(function (p) { return p.tuvi; }) } : null,
      items: this.itemFx.slice(),
      hatches: this.hatchFx.slice(),
      kiep: k ? { id: k.id, targetId: k.targetId, name: shown(k.name), realmName: k.realmName, phase: k.phase, need: k.need, shield: k.shield,
        realm: (function () { var km = sect.members.get(k.targetId); return km ? sect.levelOf(km.tuvi).realmIdx : k.realmIdx; })(),
        strikes: k.strikes, struck: k.struck, log: k.log.slice(), success: k.success, bonus: k.bonus || 0,
        remainingMs: k.phase === "strike" ? Math.ceil(Math.max(0, k.endsAt - now) / 1000) * 1000 : 0 } : null,
      surge: this.surgeActive(now) ? { multiplier: this.cfg.surge.multiplier, remainingMs: Math.ceil((this.surge.endsAt - now) / 1000) * 1000 } : null,
      banners: this.banners.slice()
    };
  };

  /** localStorage có try/catch (bộ nhớ có thể bị chặn hoặc đầy). */
  function localStore(ls) {
    ls = ls || (typeof localStorage !== "undefined" ? localStorage : null);
    return {
      get: function (k) { try { var v = ls && ls.getItem(k); return v ? JSON.parse(v) : null; } catch (e) { return null; } },
      set: function (k, v) { try { if (ls) ls.setItem(k, JSON.stringify(v)); } catch (e) { /* bỏ qua */ } }
    };
  }

  /**
   * Lưu tu vi tuần vào hub TikTool (SQLite, /api/games/<gameId>/players), localStorage giữ bản sao dự phòng.
   * Chỉ các khóa bắt đầu bằng `prefix` (dữ liệu tuần) đi lên hub; cài đặt vẫn ở localStorage.
   * Mỗi lần lưu chỉ gửi những người đổi tu vi / tên; gửi lỗi thì tự gửi lại sau 5 giây.
   */
  function hubStore(opts) {
    var local = opts.local || localStore(), doFetch = opts.fetch || (typeof fetch !== "undefined" ? fetch.bind(null) : null);
    var url = (opts.base || "") + "/api/games/" + encodeURIComponent(opts.gameId) + "/players", prefix = opts.prefix;
    var sent = {};          // key -> { week, rows: { id: "nick|tuvi|joinedAt" } } (bản hub đang có)
    var pending = {};       // key -> giá trị chờ gửi
    var busy = {}, retryTimer = null, status = { ok: null, lastError: "" };
    function scopeOf(k) { return k.slice(prefix.length + 1) || "live"; }
    function sig(r) { return r[1] + "|" + r[2] + "|" + r[3] + "|" + JSON.stringify(r[4] || {}); }
    function post(k, payload, keepalive) {
      if (!doFetch) return Promise.resolve(false);
      return doFetch(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload), keepalive: !!keepalive })
        .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); status.ok = true; status.lastError = ""; return true; })
        .catch(function (e) { status.ok = false; status.lastError = e && e.message || String(e); return false; });
    }
    function push(k, keepalive) {
      var v = pending[k];
      if (!v || busy[k]) return;
      delete pending[k];
      var prev = sent[k] && sent[k].week === v.week ? sent[k].rows : {}, rows = {}, diff = [];
      (v.sect && v.sect.members || []).forEach(function (r) { rows[r[0]] = sig(r); if (prev[r[0]] !== rows[r[0]]) diff.push(r); });
      var removed = Object.keys(prev).some(function (id) { return !(id in rows); });
      if (!diff.length && !removed) return;
      var payload = { scope: scopeOf(k), week: v.week, members: removed ? v.sect.members : diff, replace: removed };
      busy[k] = true;
      post(k, payload, keepalive).then(function (ok) {
        busy[k] = false;
        if (ok) { var base = removed ? {} : Object.assign({}, prev); diff.forEach(function (r) { base[r[0]] = sig(r); }); if (removed) base = rows; sent[k] = { week: v.week, rows: base }; }
        else if (!pending[k]) pending[k] = v;            // lỗi: giữ lại để gửi lần sau
        if (pending[k]) schedule();
      });
    }
    function schedule() { if (!retryTimer) retryTimer = setTimeout(function () { retryTimer = null; Object.keys(pending).forEach(function (k) { push(k); }); }, 5000); }
    return {
      status: status,
      get: function (k) { return local.get(k); },
      set: function (k, v) {
        local.set(k, v);
        if (k.indexOf(prefix) !== 0 || !v || !v.week) return;
        pending[k] = v; push(k, opts.unloading && opts.unloading());
      },
      /** Xóa dữ liệu 1 tuần trên hub (nút "Xóa dữ liệu tuần này"). */
      clear: function (k, week) {
        delete pending[k]; sent[k] = { week: week, rows: {} };
        post(k, { scope: scopeOf(k), week: week, members: [], replace: true });
      },
      /** Đọc dữ liệu tuần từ hub. Trả về Promise<{ week, sect }|null>; nhớ bản hub đang có để lần lưu sau chỉ gửi phần đổi. */
      load: function (k, week) {
        if (!doFetch) return Promise.resolve(null);
        return doFetch(url + "?scope=" + encodeURIComponent(scopeOf(k)) + "&week=" + encodeURIComponent(week))
          .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
          .then(function (d) {
            var rows = {};
            (d.members || []).forEach(function (r) { rows[r[0]] = sig(r); });
            sent[k] = { week: d.week, rows: rows };
            status.ok = true;
            return { week: d.week, sect: { v: 3, members: d.members || [] } };
          })
          .catch(function (e) { status.ok = false; status.lastError = e && e.message || String(e); return null; });
      }
    };
  }

  return { Game: Game, hubStore: hubStore, ACTION_IDS: Object.keys(ACTIONS), localStore: localStore, defaultSettings: defaultSettings, weekKey: weekKey };
}));
