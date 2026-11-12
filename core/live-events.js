"use strict";
/*
 * TikTool live event core.
 *
 * Turns raw TikTok LIVE events (gift, like, comment, follow, share) into game
 * commands. Pure logic with no I/O so it can be unit tested and reused by the
 * server, the test lab and future packaged builds.
 *
 * Trigger rules per event type:
 *   gift    - giftId (empty = any gift). times = gift count (combo delta).
 *   like    - every `likeEvery` likes, counted per room or per viewer
 *             (`likeScope`). times = thresholds crossed by this batch.
 *   comment - `keywords` matched with `matchMode` (any|contains|exact|startsWith).
 *   follow  - first follow of each viewer in a session only.
 *   share   - each share.
 *
 * Gift and comment events fire only the highest-priority matching rule (one
 * gift or keyword maps to one effect). Like, follow and share fire every
 * matching rule, so "100 likes -> build" and "1000 likes -> heal" both work.
 */

const crypto = require("crypto");

const EVENT_TYPES = Object.freeze(["gift", "like", "comment", "follow", "share"]);
const EXCLUSIVE_TYPES = new Set(["gift", "comment"]);
const MATCH_MODES = Object.freeze(["any", "contains", "exact", "startsWith"]);
const LIKE_SCOPES = Object.freeze(["room", "user"]);
const REPEAT_MODES = Object.freeze(["multiply", "once"]);
const MAX_TIMES_PER_EVENT = 1000;

function text(value, max = 300) {
  return String(value ?? "").slice(0, max);
}

function positiveInt(value, fallback) {
  const n = Math.floor(Number(value));
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

function foldText(value) {
  // Lowercase and strip Vietnamese diacritics so "xây" matches "XAY".
  return String(value || "")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .trim();
}

function normalizeEvent(input) {
  if (!input || !EVENT_TYPES.includes(input.type)) throw new Error("Unsupported event type");
  const user = input.user || {};
  const event = {
    eventId: text(input.eventId || crypto.randomUUID(), 200),
    type: input.type,
    ts: input.ts || new Date().toISOString(),
    roomId: text(input.roomId || "local-demo", 100),
    user: {
      id: text(user.id || user.uniqueId || user.username || "demo", 100),
      username: text(user.username || user.nickname || user.id || "Demo", 100),
      nickname: text(user.nickname || user.username || user.id || "Demo", 100),
      avatarUrl: text(user.avatarUrl || "", 1000)
    },
    source: { mode: "injector", ...(input.source || {}) }
  };
  if (event.type === "gift") {
    const id = input.giftId ?? input.gift?.id;
    if (id == null || id === "") throw new Error("Gift event requires giftId");
    event.gift = {
      id: text(id, 50),
      name: text(input.giftName ?? input.gift?.name ?? "Gift", 100),
      count: positiveInt(input.count ?? input.gift?.count, 1),
      diamonds: Math.max(0, Number(input.diamondCount ?? input.gift?.diamonds ?? 0) || 0)
    };
  }
  if (event.type === "like") event.like = { count: positiveInt(input.count ?? input.like?.count, 1), total: Number(input.total ?? input.like?.total) || null };
  if (event.type === "comment") event.comment = { text: text(input.text ?? input.comment?.text ?? "", 500) };
  if (event.type === "share") event.share = { count: positiveInt(input.count ?? input.share?.count, 1) };
  return event;
}

/** Fills defaults and upgrades old mappings (keyword, milestone) to the current shape. */
function normalizeRule(rule) {
  const out = { ...rule };
  out.eventType = EVENT_TYPES.includes(out.eventType) ? out.eventType : "gift";
  out.params = out.params && typeof out.params === "object" ? out.params : {};
  out.cooldownMs = Math.max(0, Number(out.cooldownMs) || 0);
  out.userCooldownMs = Math.max(0, Number(out.userCooldownMs) || 0);
  out.priority = Number(out.priority) || 0;
  out.enabled = out.enabled !== false;
  out.repeat = REPEAT_MODES.includes(out.repeat) ? out.repeat : "multiply";
  out.maxPerEvent = Math.min(MAX_TIMES_PER_EVENT, positiveInt(out.maxPerEvent, 100));
  if (out.eventType === "gift") out.giftId = out.giftId == null ? "" : String(out.giftId);
  if (out.eventType === "like") {
    out.likeEvery = positiveInt(out.likeEvery ?? out.milestone, 100);
    out.likeScope = LIKE_SCOPES.includes(out.likeScope) ? out.likeScope : "room";
    delete out.milestone;
  }
  if (out.eventType === "comment") {
    const raw = Array.isArray(out.keywords) ? out.keywords : String(out.keywords ?? out.keyword ?? "").split(",");
    out.keywords = [...new Set(raw.map((k) => String(k).trim()).filter(Boolean))].slice(0, 20);
    out.matchMode = MATCH_MODES.includes(out.matchMode) ? out.matchMode : (out.keywords.length ? "contains" : "any");
    delete out.keyword;
  }
  return out;
}

function validateRule(rule, manifest) {
  const r = normalizeRule(rule);
  if (!r.gameId || !r.action) throw new Error("Mapping requires gameId, eventType and action");
  const action = manifest?.actions?.find((a) => a.id === r.action);
  if (!action) throw new Error("Action is not declared in manifest");
  if (Array.isArray(manifest.triggers) && !manifest.triggers.includes(r.eventType)) throw new Error(`Game không hỗ trợ sự kiện ${r.eventType}`);
  if (r.eventType === "comment" && r.matchMode !== "any" && !r.keywords.length) throw new Error("Mapping comment cần ít nhất một từ khóa");
  for (const [name, def] of Object.entries(action.parameters || {})) {
    if (def.required && r.params[name] == null) throw new Error(`Thiếu tham số ${name}`);
    if (r.params[name] != null && def.type === "number") {
      const n = Number(r.params[name]);
      if (!Number.isFinite(n)) throw new Error(`Tham số ${name} phải là số`);
      if (def.minimum != null && n < def.minimum) throw new Error(`Tham số ${name} tối thiểu ${def.minimum}`);
      if (def.maximum != null && n > def.maximum) throw new Error(`Tham số ${name} tối đa ${def.maximum}`);
      r.params[name] = n;
    }
  }
  return r;
}

function commentMatches(rule, commentText) {
  if (rule.matchMode === "any") return true;
  const body = foldText(commentText);
  return rule.keywords.some((keyword) => {
    const k = foldText(keyword);
    if (!k) return false;
    if (rule.matchMode === "exact") return body === k;
    if (rule.matchMode === "startsWith") return body.startsWith(k);
    return body.includes(k);
  });
}

function baseMatch(rule, event) {
  if (!rule.enabled || rule.eventType !== event.type) return false;
  if (event.type === "gift") return !rule.giftId || rule.giftId === event.gift.id;
  if (event.type === "comment") return commentMatches(rule, event.comment.text);
  return true;
}

/** Scales numeric params by `times` when the rule repeats by multiplying. */
function buildParams(rule, times, manifestAction) {
  const params = { ...rule.params };
  if (rule.repeat === "multiply" && times > 1) {
    for (const [name, value] of Object.entries(params)) {
      const def = manifestAction?.parameters?.[name];
      if (typeof value === "number" && def?.scale !== false) {
        params[name] = def?.maximum != null ? Math.min(def.maximum, value * times) : value * times;
      }
    }
  }
  params.count = times;
  return params;
}

/**
 * Keeps per-session counters (likes, follows, cooldowns) so live events can be
 * evaluated statefully. One instance per live session.
 */
class LiveRuleEngine {
  constructor({ now = () => Date.now() } = {}) {
    this.now = now;
    this.likeCounters = new Map();
    this.followers = new Set();
    this.ruleCooldowns = new Map();
    this.userCooldowns = new Map();
    this.stats = { gift: 0, like: 0, comment: 0, follow: 0, share: 0 };
  }

  reset() {
    this.likeCounters.clear();
    this.followers.clear();
    this.ruleCooldowns.clear();
    this.userCooldowns.clear();
    for (const k of Object.keys(this.stats)) this.stats[k] = 0;
  }

  /** Returns how many times a rule fires for this event, before cooldowns. */
  times(rule, event) {
    if (event.type === "gift") return event.gift.count;
    if (event.type === "share") return event.share.count;
    if (event.type === "comment") return 1;
    if (event.type === "follow") return 1;
    if (event.type === "like") {
      const key = rule.likeScope === "user" ? `${rule.id}:${event.user.id}` : rule.id;
      const total = (this.likeCounters.get(key) || 0) + event.like.count;
      const fired = Math.floor(total / rule.likeEvery);
      this.likeCounters.set(key, total - fired * rule.likeEvery);
      if (this.likeCounters.size > 20000) this.likeCounters.delete(this.likeCounters.keys().next().value);
      return fired;
    }
    return 0;
  }

  likeProgress(rule, userId) {
    const key = rule.likeScope === "user" ? `${rule.id}:${userId}` : rule.id;
    return { current: this.likeCounters.get(key) || 0, every: rule.likeEvery };
  }

  /**
   * Evaluates one normalized event against rules.
   * Returns { commands, results }: commands to queue and a per-rule status list.
   * `actionFor(rule)` returns the manifest action used to scale params.
   */
  evaluate(event, rules, { actionFor = () => null } = {}) {
    this.stats[event.type] += event.type === "like" ? event.like.count : 1;
    if (event.type === "follow") {
      if (this.followers.has(event.user.id)) return { commands: [], results: [{ status: "duplicate_follow" }] };
      this.followers.add(event.user.id);
    }
    let candidates = rules.map(normalizeRule).filter((r) => baseMatch(r, event)).sort((a, b) => b.priority - a.priority);
    if (EXCLUSIVE_TYPES.has(event.type)) candidates = candidates.slice(0, 1);
    const now = this.now(), commands = [], results = [];
    for (const rule of candidates) {
      let times = this.times(rule, event);
      if (times <= 0) { results.push({ ruleId: rule.id, status: "accumulating", progress: event.type === "like" ? this.likeProgress(rule, event.user.id) : undefined }); continue; }
      if (rule.cooldownMs && this.ruleCooldowns.has(rule.id) && now - this.ruleCooldowns.get(rule.id) < rule.cooldownMs) { results.push({ ruleId: rule.id, status: "cooldown" }); continue; }
      const userKey = `${rule.id}:${event.user.id}`;
      if (rule.userCooldownMs && this.userCooldowns.has(userKey) && now - this.userCooldowns.get(userKey) < rule.userCooldownMs) { results.push({ ruleId: rule.id, status: "user_cooldown" }); continue; }
      this.ruleCooldowns.set(rule.id, now);
      if (rule.userCooldownMs) {
        this.userCooldowns.set(userKey, now);
        if (this.userCooldowns.size > 20000) this.userCooldowns.delete(this.userCooldowns.keys().next().value);
      }
      times = Math.min(times, rule.maxPerEvent);
      const command = {
        id: crypto.randomUUID(),
        type: "game_action",
        action: rule.action,
        params: buildParams(rule, rule.repeat === "once" ? 1 : times, actionFor(rule)),
        source: {
          mode: event.source.mode,
          eventId: event.eventId,
          eventType: event.type,
          ruleId: rule.id,
          user: event.user,
          gift: event.gift,
          comment: event.comment,
          receivedAt: event.ts
        },
        createdAt: new Date(now).toISOString()
      };
      commands.push(command);
      results.push({ ruleId: rule.id, action: rule.action, status: "matched", times, commandId: command.id });
    }
    return { commands, results };
  }
}

/** Public, minimal event payload forwarded to games that subscribe to raw events. */
function publicEvent(event) {
  const out = { eventId: event.eventId, type: event.type, ts: event.ts, user: { id: event.user.id, nickname: event.user.nickname, avatarUrl: event.user.avatarUrl } };
  if (event.gift) out.gift = { id: event.gift.id, name: event.gift.name, count: event.gift.count, diamonds: event.gift.diamonds };
  if (event.like) out.like = { count: event.like.count, total: event.like.total };
  if (event.comment) out.comment = { text: event.comment.text };
  if (event.share) out.share = { count: event.share.count };
  return out;
}

module.exports = { EVENT_TYPES, MATCH_MODES, LIKE_SCOPES, REPEAT_MODES, foldText, normalizeEvent, normalizeRule, validateRule, LiveRuleEngine, publicEvent };
