/*
 * Tẩu hỏa nhập ma (anh chốt 10/11). Logic thuần, test được bằng node.
 *
 * - Phát hiện: một người nhận >= `thresholdTuvi` tu vi (từ tim / quà, action tu_luyen) trong `windowMs` thì bị tẩu hỏa.
 * - Người bị tẩu hỏa đứng ở chỗ quái (trên cùng giữa). Mỗi lần họ tặng quà / thả tim được X tu vi -> đánh 1 người
 *   ngẫu nhiên đang ngồi, người đó mất X tu vi (không âm). Họ ngừng tặng thì ngừng đánh.
 * - Mọi người tấn công để giải cứu: tu vi họ kiếm được trong lúc này trừ vào "máu" (chung với máu rồng, Cài đặt).
 * - Không giới hạn thời gian. Cứu thành công: mỗi người góp sức được thưởng thêm đúng số tu vi đã góp (x2).
 *
 * Sự kiện qua onEvent:
 *   { type: "possess_start", member, hp }
 *   { type: "possess_attack", attackerId, targetId, amount }       (game tự trừ tu vi người bị đánh)
 *   { type: "possess_rescue_hit", userId, amount, hp, maxHp }
 *   { type: "possess_milestone", percent }
 *   { type: "possess_freed", member, rescuers: [{ id, nickname, amount }] }
 *   { type: "possess_over" }
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  var MILESTONES = [50, 25, 10];

  function Possession(config, hooks) {
    this.cfg = config.possession;
    this.hooks = hooks || {};
    this.phase = "idle";        // idle | active | result
    this.member = null;         // { id, nickname }
    this.hp = 0;
    this.maxHp = 0;
    this.rescuers = new Map();  // id -> { id, nickname, amount }
    this.gains = new Map();     // id -> [{ at, amount }] để phát hiện tu luyện quá nhanh
    this.milestonesHit = {};
    this.resultUntil = 0;
  }

  Possession.prototype.emit = function (e) { if (typeof this.hooks.onEvent === "function") this.hooks.onEvent(e); };

  /**
   * Ghi nhận tu vi vừa nhận (chỉ từ tim / quà). Trả true nếu người này vừa vượt ngưỡng tẩu hỏa.
   * Không theo dõi khi đang có tẩu hỏa (mỗi lúc chỉ 1 người).
   */
  Possession.prototype.track = function (id, amount, now) {
    id = String(id);
    var list = this.gains.get(id) || [], from = now - this.cfg.windowMs;
    list.push({ at: now, amount: amount });
    while (list.length && list[0].at < from) list.shift();
    this.gains.set(id, list);
    if (this.gains.size > 500) { var self = this; this.gains.forEach(function (l, k) { if (!l.length || l[l.length - 1].at < from) self.gains.delete(k); }); }
    var sum = list.reduce(function (n, g) { return n + g.amount; }, 0);
    return sum >= this.cfg.thresholdTuvi;
  };

  Possession.prototype.start = function (now, member, maxHp) {
    this.phase = "active";
    this.member = { id: String(member.id), nickname: member.nickname || "" };
    this.maxHp = this.hp = Math.max(1, Math.floor(Number(maxHp) || 1));
    this.rescuers = new Map();
    this.milestonesHit = {};
    this.gains = new Map();
    this.emit({ type: "possess_start", member: this.member, hp: this.maxHp });
  };

  Possession.prototype.isPossessed = function (id) { return this.phase === "active" && this.member && this.member.id === String(id); };

  /** Người khác góp sức giải cứu. Trả số tu vi được tính (không vượt quá máu còn lại). */
  Possession.prototype.rescueHit = function (user, amount, now) {
    if (this.phase !== "active" || !user || !user.id || this.isPossessed(user.id)) return 0;
    var hit = Math.min(this.hp, Math.max(0, Math.floor(Number(amount) || 0)));
    if (!hit) return 0;
    this.hp -= hit;
    var r = this.rescuers.get(String(user.id)) || { id: String(user.id), nickname: user.nickname || "", amount: 0 };
    r.amount += hit;
    if (user.nickname != null) r.nickname = user.nickname;
    this.rescuers.set(r.id, r);
    this.emit({ type: "possess_rescue_hit", userId: r.id, amount: hit, hp: this.hp, maxHp: this.maxHp });
    var self = this, pct = 100 * this.hp / this.maxHp;
    MILESTONES.forEach(function (m) { if (pct <= m && pct > 0 && !self.milestonesHit[m]) { self.milestonesHit[m] = true; self.emit({ type: "possess_milestone", percent: m }); } });
    if (this.hp <= 0) this.free(now);
    return hit;
  };

  Possession.prototype.rescuerList = function () {
    var list = []; this.rescuers.forEach(function (r) { list.push(r); });
    return list.sort(function (a, b) { return b.amount - a.amount; });
  };

  Possession.prototype.free = function (now) {
    this.phase = "result";
    this.resultUntil = (now || 0) + this.cfg.resultMs;
    this.emit({ type: "possess_freed", member: this.member, rescuers: this.rescuerList() });
  };

  /** Streamer tắt sự kiện / thoát game: hủy, không thưởng. */
  Possession.prototype.cancel = function () { this.phase = "idle"; this.member = null; this.hp = 0; this.rescuers = new Map(); };

  Possession.prototype.tick = function (now) {
    if (this.phase === "result" && now >= this.resultUntil) { this.phase = "idle"; this.member = null; this.emit({ type: "possess_over" }); }
  };

  Possession.prototype.snapshot = function () {
    return { phase: this.phase, hp: this.hp, maxHp: this.maxHp, memberId: this.member ? this.member.id : null,
      top: this.rescuerList().slice(0, 3).map(function (r) { return { id: r.id, name: r.nickname, amount: r.amount }; }) };
  };

  return { Possession: Possession };
}));
