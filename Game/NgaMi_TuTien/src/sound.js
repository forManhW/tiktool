/*
 * Âm thanh hiệu ứng (vd. lên cấp). Dùng nhiều bản Audio để các lần phát gần nhau có thể chồng lên nhau,
 * nhưng giới hạn khoảng cách tối thiểu để live đông (nhiều người lên cấp cùng lúc) không thành tiếng ồn.
 *
 *   const levelUp = NgaMi.createSfx("src/assets/game/magic-fairy.mp3");
 *   levelUp.play(0.8);
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
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

  // Mỗi pháp bảo có âm riêng; các nhóm còn lại phục vụ hiệu ứng ngoài pháp bảo.
  var itemSounds = {
    item_linh_thach: "linh_thach.mp3", item_tu_khi_dan: "tu_khi_dan.mp3",
    item_chu_qua: "chu_qua.mp3", item_ban_dao: "ban_dao.mp3",
    item_linh_chi: "thanh_nien_linh_chi.mp3", item_ngoc_dich: "ngoc_dich_quynh_tuong.mp3",
    item_bi_tich: "bi_tich_cong_phap.mp3", item_phi_kiem: "phi_kiem_tran.mp3",
    item_long_chau: "ngoc_rong.mp3"
  };
  function gameSfxGain(name) { return Object.prototype.hasOwnProperty.call(itemSounds, name) ? 6 : 1; }
  function gameSfxUrl(name, variant) {
    if (Object.prototype.hasOwnProperty.call(itemSounds, name)) return "src/assets/game/" + itemSounds[name];
    if (!/^(kiem|va_cham|trung|ti_thi)$/.test(name) || (variant !== 1 && variant !== 2)) return null;
    return "src/assets/game/sfx_" + name + "_" + variant + ".wav";
  }

  /** Ngân sách âm dùng chung cho toàn game; âm lỗi hoặc bị trình duyệt chặn không giữ chỗ. */
  function createSfxMixer(opts) {
    opts = opts || {};
    var max = Math.max(1, Math.min(4, Number(opts.maxVoices) || 4));
    var gap = opts.minGapMs == null ? 180 : Math.max(0, Number(opts.minGapMs) || 0);
    var timeout = Math.max(100, Number(opts.maxDurationMs) || 10000);
    var duck = opts.duckFactor == null ? 0.4 : Math.max(0, Math.min(1, Number(opts.duckFactor)));
    var AudioClass = opts.Audio || (typeof Audio !== "undefined" && Audio);
    var ContextClass = opts.AudioContext || (typeof window !== "undefined" && (window.AudioContext || window.webkitAudioContext));
    var clock = opts.now || Date.now, voices = [], last = {}, speaking = false, context = null;
    function unlock() {
      if (!context && ContextClass) { try { context = new ContextClass(); } catch (e) {} }
      if (context && context.state === "suspended") {
        try { var resumed = context.resume(); if (resumed && resumed.catch) resumed.catch(function () {}); } catch (e) {}
      }
    }
    function volume(voice) {
      var value = voice.base * (speaking ? duck : 1);
      if (voice.gain) voice.gain.gain.value = value;
      else voice.audio.volume = Math.min(1, value);
    }
    function release(voice) {
      var i = voices.indexOf(voice);
      if (i < 0) return;
      voices.splice(i, 1); clearTimeout(voice.timer); voice.audio.onended = null; voice.audio.onerror = null;
      if (voice.source) voice.source.disconnect();
      if (voice.gain) voice.gain.disconnect();
    }
    return {
      unlock: unlock,
      play: function (key, url, amount) {
        var now = clock();
        if (!AudioClass || !url || (last[key] != null && now - last[key] < gap)) return false;
        if (voices.length >= max && (/^item_/.test(key) || key === "trung")) {
          var thunder = voices.filter(function (voice) { return voice.key === "thunder"; })[0];
          if (thunder) { try { thunder.audio.pause(); thunder.audio.currentTime = 0; } catch (e) {} release(thunder); }
        }
        if (voices.length >= max) return false;
        var audio;
        try { audio = new AudioClass(url); } catch (e) { return false; }
        var base = amount == null ? 1 : Number(amount);
        if (!isFinite(base)) base = 0;
        var voice = { key: key, audio: audio, base: Math.max(0, base) };
        if (voice.base > 1) {
          unlock();
          if (context) {
            try {
              voice.gain = context.createGain();
              voice.source = context.createMediaElementSource(audio);
              voice.source.connect(voice.gain); voice.gain.connect(context.destination);
            } catch (e) {
              if (voice.source) voice.source.disconnect();
              if (voice.gain) voice.gain.disconnect();
              voice.source = voice.gain = null;
            }
          }
        }
        voices.push(voice); last[key] = now;
        audio.preload = "auto";
        audio.onended = audio.onerror = function () { release(voice); };
        voice.timer = setTimeout(function () { try { audio.pause(); } catch (e) {} release(voice); }, timeout);
        try {
          volume(voice);
          var p = audio.play();
          if (p && typeof p.catch === "function") p.catch(function () { release(voice); });
        } catch (e) { release(voice); return false; }
        return true;
      },
      setSpeaking: function (on) { speaking = !!on; voices.forEach(volume); },
      stop: function (key) { voices.slice().forEach(function (voice) { if (voice.key !== key) return; try { voice.audio.pause(); voice.audio.currentTime = 0; } catch (e) {} release(voice); }); },
      stopAll: function () { voices.slice().forEach(function (voice) { try { voice.audio.pause(); voice.audio.currentTime = 0; } catch (e) {} release(voice); }); },
      stats: function () { return { active: voices.length, speaking: speaking }; }
    };
  }

  /** Phát lặp tiếng sấm, các lượt cách 4 giây để chồng nhẹ file gốc 5 giây. */
  function createThunderLoop(mixer, opts) {
    opts = opts || {};
    var clock = opts.now || Date.now, currentId = null, nextAt = 0;
    return {
      sync: function (kiep, enabled, volume) {
        var id = enabled && kiep && kiep.phase === "strike" ? kiep.id : null;
        if (id !== currentId) {
          if (currentId !== null) mixer.stop("thunder");
          currentId = id; nextAt = 0;
        }
        if (id === null) return;
        var now = clock();
        if (now >= nextAt && mixer.play("thunder", "src/assets/game/thunder.mp3", volume * 2.34)) nextAt = now + 4000;
      },
      stop: function () { if (currentId !== null) mixer.stop("thunder"); currentId = null; nextAt = 0; }
    };
  }

  return { createSfx: createSfx, createSfxMixer: createSfxMixer, gameSfxUrl: gameSfxUrl, gameSfxGain: gameSfxGain, createThunderLoop: createThunderLoop };
}));
