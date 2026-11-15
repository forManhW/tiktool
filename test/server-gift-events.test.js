const test = require("node:test");
const assert = require("node:assert/strict");
const { spawn } = require("node:child_process");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const net = require("node:net");
const { once } = require("node:events");
const WebSocket = require("ws");

async function port() {
  const server = net.createServer();
  server.listen(0, "127.0.0.1"); await once(server, "listening");
  const p = server.address().port; await new Promise((resolve) => server.close(resolve)); return p;
}

test("hub chuyển quà thô với mapped đúng trước action, kể cả chế độ an toàn", { timeout: 15000 }, async (t) => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "tiktool-gift-test-"));
  const httpPort = await port(), wsPort = await port();
  const child = spawn(process.execPath, [path.join(__dirname, "../server.js")], {
    env: { ...process.env, PORT: String(httpPort), GAME_PORT: String(wsPort), TIKTOOL_DATA_DIR: tempRoot, TIKTOOL_SEED_DIR: "" },
    stdio: "ignore", windowsHide: true
  });
  let ws;
  t.after(async () => {
    if (ws) ws.terminate();
    if (child.exitCode == null) { const exited = once(child, "exit"); child.kill(); await exited; }
    const resolved = path.resolve(tempRoot), parent = path.resolve(os.tmpdir());
    if (path.dirname(resolved) === parent && path.basename(resolved).startsWith("tiktool-gift-test-")) fs.rmSync(resolved, { recursive: true, force: true });
  });
  const base = `http://127.0.0.1:${httpPort}`;
  let ready = false;
  for (let i = 0; i < 100 && !ready; i++) {
    try { ready = (await fetch(base + "/api/status")).ok; } catch {}
    if (!ready) await new Promise((resolve) => setTimeout(resolve, 50));
  }
  assert.equal(ready, true);
  async function post(url, data) {
    const r = await fetch(base + url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data) });
    assert.ok(r.ok, `${url}: ${r.status}`); return r.json();
  }
  const session = await post("/api/sessions", { gameId: "ngami-tutien" });
  const token = new URL(session.gameUrl).searchParams.get("token");
  ws = new WebSocket(`ws://127.0.0.1:${wsPort}`);
  const messages = [];
  ws.on("message", (raw) => {
    const message = JSON.parse(raw); messages.push(message);
    if (message.type === "game_action") ws.send(JSON.stringify({ type: "command_result", commandId: message.id, status: "success" }));
  });
  await once(ws, "open");
  ws.send(JSON.stringify({ type: "game_hello", sessionId: session.id, token, gameId: "ngami-tutien", role: "game" }));
  async function waitFor(fn) {
    for (let i = 0; i < 100; i++) { const value = fn(); if (value) return value; await new Promise((resolve) => setTimeout(resolve, 20)); }
    assert.fail("Chờ thông điệp từ hub quá lâu");
  }
  await waitFor(() => messages.find((m) => m.type === "game_hello_ack"));
  const event = (id, giftId) => ({ sessionId: session.id, eventId: id, type: "gift", giftId, giftName: "Rose", user: { id: "viewer", nickname: "An" }, source: { mode: "tiktok" } });
  await post("/api/events", event("mapped", "5655"));
  await waitFor(() => messages.find((m) => m.type === "game_action" && m.source.eventId === "mapped"));
  const mapped = messages.findIndex((m) => m.type === "live_event" && m.event.eventId === "mapped");
  const action = messages.findIndex((m) => m.type === "game_action" && m.source.eventId === "mapped");
  assert.ok(mapped >= 0 && mapped < action); assert.equal(messages[mapped].event.mapped, true);
  await post("/api/events", event("unmapped", "999999"));
  assert.equal((await waitFor(() => messages.find((m) => m.type === "live_event" && m.event.eventId === "unmapped"))).event.mapped, false);
  await post("/api/safe-mode", { enabled: true });
  await post("/api/events", event("safe", "5655"));
  assert.equal((await waitFor(() => messages.find((m) => m.type === "live_event" && m.event.eventId === "safe"))).event.mapped, false);
  assert.equal(messages.some((m) => m.type === "game_action" && m.source.eventId === "safe"), false);
});
