/*
 * Tỉ thí 2 phe. Logic thuần theo thời gian, gọi tick(now) đều đặn (vd. mỗi 250ms).
 *
 * Pha: "idle" (đang tu luyện, chờ trận) -> "duel" (5 phút, action vao_phe để vào phe) -> "result" (hiện kết quả) -> "idle"
 *
 * Sự kiện qua onEvent:
 *   { type: "duel_soon", startsInMs }             còn 60 giây nữa bắt đầu
 *   { type: "duel_start", endsAt }
 *   { type: "duel_join", userId, nickname, side }
 *   { type: "duel_end", winner (1|2|0 = hòa), scores, mvp: { 1: {id,nickname,points}, 2: ... }, participants: [{id,side,points}] }
 *   Điểm phe = tu vi thành viên kiếm được từ lúc vào phe. MVP = người góp nhiều điểm nhất phe thắng.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  var SOON_MS = 60000;

  function Duel(config, hooks) {
    this.cfg = config.duel;
    this.hooks = hooks || {};
    this.enabled = !!this.cfg.enabled;
    this.phase = "idle";
    this.nextStartAt = 0;      // đặt khi start()
    this.endsAt = 0;
    this.resultUntil = 0;
    this.soonSent = false;
    this.lastStartAt = 0;
    this.reset();
  }

  Duel.prototype.emit = function (e) { if (typeof this.hooks.onEvent === "function") this.hooks.onEvent(e); };

  Duel.prototype.reset = function () {
    this.scores = { 1: 0, 2: 0 };
    this.players = new Map(); // id -> { id, nickname, side, points }
  };

  /** Gọi 1 lần khi vào game: hẹn trận đầu tiên sau `everyMs` (trừ thời lượng trận). */
  Duel.prototype.schedule = function (now) {
    this.nextStartAt = now + Math.max(0, this.cfg.everyMs - this.cfg.durationMs);
    this.soonSent = false;
  };

  /** Quái rồng xuất hiện: dừng lịch tỉ thí (không đếm giờ, không báo "sắp tỉ thí"). Rồng chết thì gọi schedule(now) lại. */
  Duel.prototype.pause = function () {
    if (this.phase === "duel") { this.phase = "idle"; this.reset(); }
    this.nextStartAt = 0;
    this.soonSent = false;
  };

  Duel.prototype.setEnabled = function (on, now) {
    this.enabled = !!on;
    if (!on && this.phase === "duel") { this.phase = "idle"; this.reset(); }
    if (on && this.phase === "idle") this.schedule(now);
  };

  /** Action "vao_phe" (streamer map comment "1"/"2"). Mỗi người chỉ chọn 1 lần / trận. Trả lý do nếu không vào được. */
  Duel.prototype.join = function (user, side) {
    if (this.phase !== "duel") return "Chưa tới giờ tỉ thí";
    if (!user || !user.id || (side !== 1 && side !== 2)) return "Thiếu người chơi hoặc phe";
    if (this.players.has(user.id)) return "Đã ở trong phe";
    this.players.set(user.id, { id: user.id, nickname: user.nickname || user.id, side: side, points: 0 });
    this.emit({ type: "duel_join", userId: user.id, nickname: user.nickname, side: side });
    return true;
  };

  /**
   * Điểm phe = tu vi thành viên KIẾM ĐƯỢC TỪ LÚC VÀO PHE (anh chốt 10/11), không tính tu vi có sẵn trước đó.
   * Game gọi hàm này mỗi lần cộng tu vi ("tu_luyen"). Đã góp thì giữ, kể cả khi người đó rời chỗ sau đó.
   */
  Duel.prototype.addPoints = function (userId, points) {
    if (this.phase !== "duel") return false;
    var p = this.players.get(String(userId));
    if (!p || !(points > 0)) return false;
    p.points += points;
    this.scores[p.side] += points;
    return true;
  };


  Duel.prototype.start = function (now) {
    this.reset();
    this.phase = "duel";
    this.lastStartAt = now;
    this.endsAt = now + this.cfg.durationMs;
    this.emit({ type: "duel_start", endsAt: this.endsAt });
  };

  Duel.prototype.finish = function (now) {
    var s1 = this.scores[1], s2 = this.scores[2];
    var winner = s1 === s2 ? 0 : (s1 > s2 ? 1 : 2);
    var mvp = { 1: null, 2: null }, participants = [];
    this.players.forEach(function (p) {
      participants.push({ id: p.id, nickname: p.nickname, side: p.side, points: p.points });
      if (p.points > 0 && (!mvp[p.side] || p.points > mvp[p.side].points)) mvp[p.side] = { id: p.id, nickname: p.nickname, points: p.points };
    });
    this.phase = "result";
    this.resultUntil = now + this.cfg.resultMs;
    this.emit({ type: "duel_end", winner: winner, scores: { 1: s1, 2: s2 }, mvp: mvp, participants: participants });
  };

  Duel.prototype.tick = function (now) {
    if (this.phase === "duel" && now >= this.endsAt) this.finish(now);
    else if (this.phase === "result" && now >= this.resultUntil) {
      this.phase = "idle";
      this.nextStartAt = this.lastStartAt + this.cfg.everyMs; // chu kỳ tính từ lúc bắt đầu trận trước
      if (this.nextStartAt <= now) this.nextStartAt = now + 1000;
      this.soonSent = false;
    } else if (this.phase === "idle" && this.enabled && this.nextStartAt) {
      if (!this.soonSent && this.nextStartAt - now <= SOON_MS) { this.soonSent = true; this.emit({ type: "duel_soon", startsInMs: this.nextStartAt - now }); }
      if (now >= this.nextStartAt) this.start(now);
    }
  };

  /** Ảnh chụp cho renderer / overlay OBS. */
  Duel.prototype.snapshot = function (now) {
    // Làm tròn lên theo giây: state chỉ đổi 1 lần/giây khi rảnh, đỡ phải gửi lên TikTool liên tục.
    var sec = function (ms) { return Math.ceil(Math.max(0, ms) / 1000) * 1000; };
    return { enabled: this.enabled, phase: this.phase, scores: { 1: this.scores[1], 2: this.scores[2] },
      players: this.players.size, remainingMs: this.phase === "duel" ? sec(this.endsAt - now) : 0,
      nextInMs: this.phase === "idle" && this.enabled ? sec(this.nextStartAt - now) : null };
  };

  return { Duel: Duel };
}));
