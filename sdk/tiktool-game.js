/*
 * TikTool Game SDK (browser).
 *
 * Load from a game page:  <script src="/sdk/tiktool-game.js"></script>
 *
 *   const live = TikTool.connect({
 *     gameId: "my-game",                       // must match manifest.json "id"
 *     getState: () => ({ score }),             // snapshot mirrored to the OBS overlay
 *     onAction(action, params, source) {       // mapped effect from gift/like/comment/follow/share
 *       if (action === "build") { build(params.amount); return true; }
 *       throw new Error("Unsupported action");  // or return false -> TikTool logs a failed command
 *     },
 *     onLiveEvent(event) {},                   // raw events listed in manifest "liveEvents"
 *     onState(state) {},                       // overlay (OBS) mode only
 *     onStatus(status) {}                      // demo | connecting | connected | overlay_connected | reconnecting | unauthorized | duplicate_game
 *   });
 *   live.emit();        // push getState() now (call after every state change)
 *   live.isOverlay;     // true when opened as OBS mirror
 *
 * onAction may also return a Promise resolving to true/false for async effects.
 */
(function (global) {
  "use strict";

  function connect(options) {
    options = options || {};
    var query = new URLSearchParams(global.location.search);
    var sessionId = query.get("session");
    var token = query.get("token");
    var isOverlay = query.get("overlay") === "1";
    var wsUrl = query.get("ws") || "ws://127.0.0.1:8765";
    var socket = null, reconnectTimer = null, authenticated = false, stopped = false, lastEmit = 0, emitTimer = null;

    function status(value) { if (typeof options.onStatus === "function") options.onStatus(value); }
    function send(value) {
      if (socket && socket.readyState === WebSocket.OPEN) { socket.send(JSON.stringify(value)); return true; }
      return false;
    }

    function emitNow() {
      if (isOverlay || !authenticated) return false;
      lastEmit = Date.now();
      var state = typeof options.getState === "function" ? options.getState() : {};
      return send({ type: "game_state", state: state || {} });
    }
    // Throttled to ~5/s so per-frame calls do not flood the gateway.
    function emit() {
      var wait = 200 - (Date.now() - lastEmit);
      if (wait <= 0) return emitNow();
      clearTimeout(emitTimer);
      emitTimer = setTimeout(emitNow, wait);
      return true;
    }

    function result(command, connection, ok, error) {
      if (connection !== socket || connection.readyState !== WebSocket.OPEN) return;
      send({ type: "command_result", commandId: command.id, status: ok ? "success" : "failed", error: ok ? undefined : (error || "Action unavailable"), executedAt: new Date().toISOString() });
    }

    function applyCommand(command, connection) {
      if (isOverlay || !authenticated || typeof options.onAction !== "function") return result(command, connection, false, "Game is not ready");
      var response;
      try { response = options.onAction(command.action, command.params || {}, command.source || {}); }
      catch (error) { return result(command, connection, false, error && error.message); }
      if (response && typeof response.then === "function") {
        response.then(function (ok) { result(command, connection, ok === true, ok === true ? null : "Action unavailable"); },
          function (error) { result(command, connection, false, error && error.message); });
        return;
      }
      result(command, connection, response === true, response === true ? null : "Action unavailable");
    }

    function connectSocket() {
      if (stopped || !sessionId || !token) return;
      clearTimeout(reconnectTimer);
      authenticated = false;
      status("connecting");
      try { socket = new WebSocket(wsUrl); }
      catch (error) { status("reconnecting"); reconnectTimer = setTimeout(connectSocket, 3000); return; }
      var connection = socket;
      connection.addEventListener("open", function () {
        send({ type: "game_hello", gameId: options.gameId, sessionId: sessionId, token: token, role: isOverlay ? "overlay" : "game" });
      });
      connection.addEventListener("message", function (event) {
        var message;
        try { message = JSON.parse(event.data); } catch (error) { return; }
        if (message.type === "game_hello_ack") {
          authenticated = true;
          status(isOverlay ? "overlay_connected" : "connected");
          if (!isOverlay) emitNow();
        } else if (message.type === "game_state" && isOverlay) {
          if (typeof options.onState === "function") options.onState(message.state || {});
        } else if (message.type === "game_action" && !isOverlay) {
          applyCommand(message, connection);
        } else if (message.type === "live_event") {
          if (typeof options.onLiveEvent === "function") {
            try { options.onLiveEvent(message.event); } catch (error) { /* game errors must not break the socket */ }
          }
        } else if (message.type === "game_effect" && isOverlay) {
          if (typeof options.onEffect === "function") options.onEffect(message.action, message.params || {});
        }
      });
      connection.addEventListener("close", function (event) {
        if (connection !== socket) return;
        authenticated = false;
        if (event.code === 4003) { stopped = true; status("unauthorized"); return; }
        status(event.code === 4009 ? "duplicate_game" : "reconnecting");
        reconnectTimer = setTimeout(connectSocket, 3000);
      });
      connection.addEventListener("error", function () { connection.close(); });
    }

    if (sessionId && token) connectSocket(); else status("demo");
    return { emit: emit, emitNow: emitNow, isOverlay: isOverlay, isDemo: !(sessionId && token) };
  }

  global.TikTool = Object.freeze({ connect: connect, version: "1.0.0" });
}(typeof window !== "undefined" ? window : this));
