/*
 * Âm thanh hiệu ứng (vd. lên cấp). Dùng nhiều bản Audio để các lần phát gần nhau có thể chồng lên nhau,
 * nhưng giới hạn khoảng cách tối thiểu để live đông (nhiều người lên cấp cùng lúc) không thành tiếng ồn.
 *
 *   const levelUp = NgaMi.createSfx("src/assets/game/magic-fairy.mp3");
 *   levelUp.play(0.8);
 */
(function (root, factory) {
  root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  function createSfx(url, opts) {
    opts = opts || {};
    var poolSize = opts.poolSize || 4, minGapMs = opts.minGapMs || 180, pool = [], next = 0, last = 0, broken = false;
    for (var i = 0; i < poolSize; i++) {
      var a = new Audio(url);
      a.preload = "auto";
      a.onerror = function () { if (!broken) console.warn("[NgaMi] không tải được âm thanh " + url); broken = true; };
      pool.push(a);
    }
    return {
      play: function (volume) {
        var now = Date.now();
        if (broken || now - last < minGapMs) return false;
        last = now;
        var a = pool[next];
        next = (next + 1) % pool.length;
        try {
          a.volume = Math.max(0, Math.min(1, volume == null ? 1 : volume));
          a.currentTime = 0;
          var p = a.play();
          if (p && p.catch) p.catch(function () { /* trình duyệt chặn tự phát: bỏ qua */ });
        } catch (e) { return false; }
        return true;
      }
    };
  }

  return { createSfx: createSfx };
}));
