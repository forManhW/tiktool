/*
 * Quái rồng: cả tông môn cùng đánh. Logic thuần (test được bằng node).
 *
 *   boss.start(now, maxHp)                 rồng xuất hiện
 *   boss.hit(user, amount)                 sát thương = tu vi người chơi vừa kiếm được (tim / quà theo mapping)
 *   boss.tick(now)                         hết thời gian hiện kết quả thì về "idle"
 *
 * Không giới hạn thời gian: đánh tới khi máu về 0.
 * Sự kiện qua onEvent:
 *   { type: "boss_start", hp }
 *   { type: "boss_hit", userId, amount, hp, maxHp }
 *   { type: "boss_milestone", percent }          còn 50% / 25% / 10% máu
 *   { type: "boss_killed", top: { id, nickname, damage }, killer, ranking: [{ id, nickname, damage }] }
 *   { type: "boss_over" }                        hết thời gian hiện kết quả
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  var MILESTONES = [50, 25, 10];

  function Boss(config, hooks) {
    this.cfg = config.boss;
    this.hooks = hooks || {};
    this.phase = "idle";     // idle | fight | result
    this.hp = 0;
    this.maxHp = 0;
    this.damage = new Map(); // id -> { id, nickname, damage }
    this.milestonesHit = {};
    this.resultUntil = 0;
    this.lastHitAt = 0;
  }

  Boss.prototype.emit = function (e) { if (typeof this.hooks.onEvent === "function") this.hooks.onEvent(e); };

  Boss.prototype.start = function (now, maxHp) {
    this.phase = "fight";
    this.maxHp = this.hp = Math.max(1, Math.floor(Number(maxHp) || this.cfg.hp));
    this.damage = new Map();
    this.milestonesHit = {};
    this.emit({ type: "boss_start", hp: this.maxHp });
  };

  /** Gây sát thương. Trả số sát thương thực (không vượt quá máu còn lại). */
  Boss.prototype.hit = function (user, amount, now) {
    if (this.phase !== "fight" || !user || !user.id) return 0;
    var dmg = Math.min(this.hp, Math.max(0, Math.floor(Number(amount) || 0)));
    if (!dmg) return 0;
    this.hp -= dmg;
    this.lastHitAt = now || 0;
    var d = this.damage.get(String(user.id)) || { id: String(user.id), nickname: user.nickname || "", damage: 0 };
    d.damage += dmg;
    if (user.nickname != null) d.nickname = user.nickname;
    this.damage.set(d.id, d);
    this.emit({ type: "boss_hit", userId: d.id, amount: dmg, hp: this.hp, maxHp: this.maxHp });
    var self = this, pct = 100 * this.hp / this.maxHp;
    MILESTONES.forEach(function (m) { if (pct <= m && pct > 0 && !self.milestonesHit[m]) { self.milestonesHit[m] = true; self.emit({ type: "boss_milestone", percent: m }); } });
    if (this.hp <= 0) this.kill(now, d);
    return dmg;
  };

  Boss.prototype.ranking = function () {
    var list = []; this.damage.forEach(function (d) { list.push(d); });
    return list.sort(function (a, b) { return b.damage - a.damage; });
  };

  Boss.prototype.kill = function (now, killer) {
    this.phase = "result";
    this.resultUntil = (now || 0) + this.cfg.resultMs;
    var ranking = this.ranking();
    this.emit({ type: "boss_killed", top: ranking[0] || null, killer: killer || null, ranking: ranking });
  };

  /** Streamer dừng đánh quái giữa chừng (vd. tắt trong Cài đặt): không thưởng. */
  Boss.prototype.cancel = function () { this.phase = "idle"; this.hp = 0; this.damage = new Map(); };

  Boss.prototype.tick = function (now) {
    if (this.phase === "result" && now >= this.resultUntil) { this.phase = "idle"; this.emit({ type: "boss_over" }); }
  };

  Boss.prototype.snapshot = function () {
    return { phase: this.phase, hp: this.hp, maxHp: this.maxHp, name: this.cfg.name,
      top: this.ranking().slice(0, 3).map(function (d) { return { id: d.id, name: d.nickname, damage: d.damage }; }) };
  };

  return { Boss: Boss };
}));
