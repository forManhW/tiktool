"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const { normalizeEvent, normalizeRule, validateRule, LiveRuleEngine, foldText } = require("../core/live-events");

const manifest = { id: "g", actions: [{ id: "build", parameters: { amount: { type: "number", required: true, minimum: 1 } } }, { id: "heal", parameters: { amount: { type: "number", scale: false } } }] };
const actionFor = (rule) => manifest.actions.find((a) => a.id === rule.action);
const ev = (input) => normalizeEvent({ user: { id: "u1", nickname: "An" }, ...input });

test("gift multiplies numeric params by combo count", () => {
  const engine = new LiveRuleEngine();
  const { commands } = engine.evaluate(ev({ type: "gift", giftId: "5655", count: 3 }), [{ id: "r", gameId: "g", eventType: "gift", giftId: "5655", action: "build", params: { amount: 2 } }], { actionFor });
  assert.equal(commands.length, 1);
  assert.deepEqual(commands[0].params, { amount: 6, count: 3 });
});

test("gift only fires highest priority rule", () => {
  const engine = new LiveRuleEngine();
  const rules = [{ id: "low", gameId: "g", eventType: "gift", giftId: "", action: "build", params: { amount: 1 } }, { id: "high", gameId: "g", eventType: "gift", giftId: "1", action: "build", params: { amount: 1 }, priority: 5 }];
  const { results } = engine.evaluate(ev({ type: "gift", giftId: "1" }), rules, { actionFor });
  assert.deepEqual(results.map((r) => r.ruleId), ["high"]);
});

test("likes accumulate per room and fire each threshold", () => {
  const engine = new LiveRuleEngine();
  const rule = { id: "like", gameId: "g", eventType: "like", likeEvery: 100, action: "build", params: { amount: 1 } };
  assert.equal(engine.evaluate(ev({ type: "like", count: 60 }), [rule], { actionFor }).commands.length, 0);
  const second = engine.evaluate(ev({ type: "like", count: 150, user: { id: "u2" } }), [rule], { actionFor });
  assert.equal(second.commands[0].params.amount, 2);
  assert.equal(engine.likeProgress(normalizeRule(rule), "x").current, 10);
});

test("likes per user are counted separately", () => {
  const engine = new LiveRuleEngine();
  const rule = { id: "like", gameId: "g", eventType: "like", likeEvery: 10, likeScope: "user", action: "build", params: { amount: 1 } };
  engine.evaluate(ev({ type: "like", count: 6 }), [rule], { actionFor });
  assert.equal(engine.evaluate(ev({ type: "like", count: 6, user: { id: "u2" } }), [rule], { actionFor }).commands.length, 0);
  assert.equal(engine.evaluate(ev({ type: "like", count: 6 }), [rule], { actionFor }).commands.length, 1);
});

test("multiple like milestones all fire", () => {
  const engine = new LiveRuleEngine();
  const rules = [{ id: "a", gameId: "g", eventType: "like", likeEvery: 10, action: "build", params: { amount: 1 } }, { id: "b", gameId: "g", eventType: "like", likeEvery: 50, action: "heal", params: { amount: 1 } }];
  const { commands } = engine.evaluate(ev({ type: "like", count: 50 }), rules, { actionFor });
  assert.deepEqual(commands.map((c) => [c.action, c.params.amount]), [["build", 5], ["heal", 1]]);
});

test("comment keywords ignore case and Vietnamese accents", () => {
  const engine = new LiveRuleEngine();
  const rules = [{ id: "c", gameId: "g", eventType: "comment", keywords: "xây, build", action: "build", params: { amount: 1 } }];
  assert.equal(engine.evaluate(ev({ type: "comment", text: "XAY thap di" }), rules, { actionFor }).commands.length, 1);
  assert.equal(engine.evaluate(ev({ type: "comment", text: "phá" }), rules, { actionFor }).commands.length, 0);
  assert.equal(foldText("Đập Phá"), "dap pha");
});

test("comment exact mode and per-user cooldown", () => {
  let t = 0;
  const engine = new LiveRuleEngine({ now: () => t });
  const rules = [{ id: "c", gameId: "g", eventType: "comment", keywords: ["1"], matchMode: "exact", userCooldownMs: 5000, action: "build", params: { amount: 1 } }];
  assert.equal(engine.evaluate(ev({ type: "comment", text: "11" }), rules, { actionFor }).commands.length, 0);
  assert.equal(engine.evaluate(ev({ type: "comment", text: "1" }), rules, { actionFor }).commands.length, 1);
  t = 1000;
  assert.equal(engine.evaluate(ev({ type: "comment", text: "1" }), rules, { actionFor }).results[0].status, "user_cooldown");
  assert.equal(engine.evaluate(ev({ type: "comment", text: "1", user: { id: "u2" } }), rules, { actionFor }).commands.length, 1);
});

test("follow fires once per viewer", () => {
  const engine = new LiveRuleEngine();
  const rules = [{ id: "f", gameId: "g", eventType: "follow", action: "heal", params: { amount: 1 } }];
  assert.equal(engine.evaluate(ev({ type: "follow" }), rules, { actionFor }).commands.length, 1);
  assert.equal(engine.evaluate(ev({ type: "follow" }), rules, { actionFor }).commands.length, 0);
});

test("scale:false params are not multiplied and repeat once sends one unit", () => {
  const engine = new LiveRuleEngine();
  const heal = engine.evaluate(ev({ type: "gift", giftId: "1", count: 4 }), [{ id: "h", gameId: "g", eventType: "gift", giftId: "1", action: "heal", params: { amount: 1 } }], { actionFor });
  assert.equal(heal.commands[0].params.amount, 1);
  const once = engine.evaluate(ev({ type: "gift", giftId: "2", count: 4 }), [{ id: "o", gameId: "g", eventType: "gift", giftId: "2", action: "build", repeat: "once", params: { amount: 1 } }], { actionFor });
  assert.deepEqual(once.commands[0].params, { amount: 1, count: 1 });
});

test("old mappings upgrade and validation checks manifest", () => {
  assert.equal(normalizeRule({ eventType: "like", milestone: 30 }).likeEvery, 30);
  assert.deepEqual(normalizeRule({ eventType: "comment", keyword: "go" }).keywords, ["go"]);
  assert.throws(() => validateRule({ gameId: "g", eventType: "gift", action: "nope" }, manifest), /manifest/);
  assert.throws(() => validateRule({ gameId: "g", eventType: "comment", matchMode: "exact", action: "build", params: { amount: 1 } }, manifest), /từ khóa/);
  assert.throws(() => validateRule({ gameId: "g", eventType: "like", action: "build", params: { amount: 1 } }, { ...manifest, triggers: ["gift"] }), /không hỗ trợ/);
});
