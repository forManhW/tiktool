/*
 * Thông báo: luôn hiện chữ trên màn hình; đọc giọng nói chỉ khi Cài đặt bật "Giọng nói AI".
 *
 *   const ann = new NgaMi.Announcer(cfg.voice, { show(item) {...}, speaker: NgaMi.webSpeechSpeaker() });
 *   ann.say({ text, speech?, priority, key? })
 *
 * priority: 3 = tỉ thí / đại cảnh giới / ghế top, 2 = tiểu cảnh, 1 = nhập môn.
 * Hàng đợi giọng có giới hạn: khi đầy sẽ bỏ câu ưu tiên thấp nhất, câu cùng `key` (vd. cùng 1 người) thì thay bằng câu mới.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, factory());
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  /** Tên để ĐỌC: bỏ emoji, ký tự trang trí, chữ không phải Latin/Việt. Không còn gì thì trả "" (gọi là "đạo hữu"). */
  function speakableName(name) {
    var s = String(name || "").normalize("NFC")
      .replace(/[^\p{Script=Latin}\p{M}\p{N}\s._-]/gu, " ")
      .replace(/[._-]+/g, " ")
      .replace(/\s+/g, " ").trim();
    if (!/[\p{L}\p{N}]/u.test(s)) return "";
    return Array.from(s).slice(0, 24).join("").trim();
  }

  /** Tách theo "kí tự nhìn thấy" (emoji ghép nhiều mã vẫn là 1), để cắt tên không làm vỡ icon. */
  function graphemes(s) {
    if (typeof Intl !== "undefined" && Intl.Segmenter) return Array.from(new Intl.Segmenter("vi", { granularity: "grapheme" }).segment(s), function (x) { return x.segment; });
    return Array.from(s);
  }
  /** Tên để HIỆN: giữ nguyên (cả icon, kí tự đặc biệt) nhưng cắt ngắn, không cắt đôi icon. */
  function displayName(name, max) {
    var s = graphemes(String(name || "Đạo hữu").trim());
    max = max || 14;
    return s.length > max ? s.slice(0, max - 1).join("") + "…" : s.join("");
  }

  function Announcer(voiceCfg, hooks) {
    this.cfg = voiceCfg;
    this.hooks = hooks || {};
    this.voiceOn = !!voiceCfg.enabled;
    this.queue = [];
    this.speaking = false;
  }

  Announcer.prototype.setVoice = function (on) {
    this.voiceOn = !!on;
    if (!on) { this.queue = []; if (this.hooks.speaker) this.hooks.speaker.cancel(); this.speaking = false; }
  };

  Announcer.prototype.say = function (item) {
    item = Object.assign({ priority: 1, at: Date.now() }, item);
    if (typeof this.hooks.show === "function") this.hooks.show(item);   // chữ luôn hiện
    if (!this.voiceOn || !this.hooks.speaker || item.silent) return;
    var q = this.queue;
    if (item.key) for (var i = 0; i < q.length; i++) if (q[i].key === item.key) { q[i] = item; return this.pump(); }
    q.push(item);
    // Hàng chờ đầy: bỏ câu ưu tiên thấp nhất; cùng mức thì bỏ câu CŨ nhất (câu mới đúng với màn hình hơn).
    while (q.length > this.cfg.maxQueue) {
      var drop = 0;
      for (var k = 1; k < q.length; k++) if (q[k].priority < q[drop].priority) drop = k;
      q.splice(drop, 1);
    }
    q.sort(function (a, b) { return b.priority - a.priority; });   // ổn định: cùng mức giữ thứ tự đến
    // Bộ đọc chạy trên máy (Piper qua hub) cần khoảng 0,5–1,3 giây để tạo câu: tạo trước ngay khi câu vào hàng chờ,
    // để lúc tới lượt đọc là có tiếng luôn.
    if (q.indexOf(item) >= 0 && typeof this.hooks.speaker.prepare === "function") item.prepared = this.hooks.speaker.prepare(item.speech || item.text);
    this.pump();
  };

  Announcer.prototype.pump = function () {
    if (this.speaking || !this.queue.length || !this.voiceOn) return;
    // Câu chờ quá lâu (giọng đang bận đọc câu trước) thì bỏ, để giọng không đọc chuyện đã qua.
    var self = this, item = this.queue.shift(), now = Date.now();
    while (item && now - item.at > (item.priority >= 3 ? this.cfg.maxWaitImportantMs || 12000 : this.cfg.maxWaitMs || 6000)) item = this.queue.shift();
    if (!item) return;
    this.speaking = true;
    Promise.resolve(this.hooks.speaker.speak(item.speech || item.text, this.cfg, item.prepared))
      .catch(function () { /* giọng lỗi thì bỏ qua, chữ đã hiện */ })
      .then(function () { self.speaking = false; self.pump(); });
  };

  /** Bộ đọc dùng Web Speech API có sẵn trong Chromium/Electron. Cần máy có giọng vi-VN (xem KE_HOACH.md mục 5). */
  function webSpeechSpeaker(globalObj) {
    var g = globalObj || (typeof window !== "undefined" ? window : {});
    var synth = g.speechSynthesis, voice = null;
    function pick() {
      if (!synth) return;
      var list = synth.getVoices();
      voice = list.find(function (v) { return /^vi/i.test(v.lang); }) || null;
    }
    if (synth) { pick(); synth.onvoiceschanged = pick; }
    return {
      available: function () { return !!(synth && voice); },
      voiceName: function () { return voice ? voice.name : null; },
      cancel: function () { if (synth) synth.cancel(); },
      speak: function (text, cfg) {
        return new Promise(function (resolve) {
          if (!synth || !voice) return resolve(false);
          var u = new g.SpeechSynthesisUtterance(text);
          u.voice = voice; u.lang = voice.lang; u.rate = cfg.rate || 1; u.volume = cfg.volume == null ? 1 : cfg.volume;
          var done = function () { clearTimeout(guard); resolve(true); };
          var guard = setTimeout(done, 15000);   // Chromium đôi khi không gọi onend
          u.onend = done; u.onerror = done;
          synth.speak(u);
        });
      }
    };
  }

  /**
   * Bộ đọc gọi giọng AI chạy trên máy qua hub TikTool (POST /api/tts -> audio/wav, Piper).
   * prepare(text) tạo trước file tiếng; speak(text, cfg, prepared) phát (dùng bản đã tạo nếu có).
   */
  function httpSpeaker(url) {
    var current = null;
    function once(text) {
      return fetch(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ text: text }) })
        .then(function (r) { if (!r.ok) throw new Error("TTS " + r.status); return r.blob(); });
    }
    // Hub đang bận / vừa khởi động lại: thử lại 1 lần sau 0,7 giây thay vì bỏ câu.
    function load(text) {
      return once(text).catch(function () { return new Promise(function (r) { setTimeout(r, 700); }).then(function () { return once(text); }); });
    }
    return {
      available: function () { return true; },
      voiceName: function () { return "Giọng AI trên máy (Piper)"; },
      cancel: function () { if (current) { current.pause(); current = null; } },
      prepare: function (text) { var p = load(text); p.catch(function () {}); return p; },
      speak: function (text, cfg, prepared) {
        // Bản tạo trước bị lỗi (vd. hub khởi động lại) thì tạo lại lúc đọc.
        var ready = prepared ? prepared.catch(function () { return load(text); }) : load(text);
        return ready.then(function (blob) {
          return new Promise(function (resolve) {
            var src = URL.createObjectURL(blob), a = current = new Audio(src);
            var finished = false;
            var done = function (ok) { if (finished) return; finished = true; clearTimeout(guard); URL.revokeObjectURL(src); resolve(ok); };
            var guard = setTimeout(function () { done(true); }, 20000);
            a.volume = cfg && cfg.volume != null ? Math.max(0, Math.min(1, cfg.volume)) : 1;
            a.playbackRate = cfg && cfg.rate ? Math.max(0.8, Math.min(1.3, cfg.rate)) : 1;
            a.onended = function () { done(true); };
            a.onerror = function () { done(false); };
            a.play().catch(function () { done(false); });
          });
        });
      }
    };
  }

  return { Announcer: Announcer, speakableName: speakableName, displayName: displayName, webSpeechSpeaker: webSpeechSpeaker, httpSpeaker: httpSpeaker };
}));
