"use strict";
/* TikTool dashboard: game library, event mapping (gift/like/comment/follow/share), live session, logs. */

const TRIGGERS = {
  gift: { label: "Quà", icon: "🎁" },
  like: { label: "Tim", icon: "❤" },
  comment: { label: "Comment", icon: "💬" },
  follow: { label: "Follow", icon: "➕" },
  share: { label: "Share", icon: "↗" }
};
const MATCH_LABEL = { contains: "chứa", exact: "đúng", startsWith: "bắt đầu bằng", any: "mọi comment" };

const state = {
  games: [], selected: null, sessions: [], listener: {}, mappings: [], gifts: [], catalog: [],
  filter: "all", stats: null, username: safeGet("tiktool.username") || ""
};
const editor = { id: null, trigger: "gift", effectImage: "" };

const $ = (s) => document.querySelector(s);
const $$ = (s) => [...document.querySelectorAll(s)];
const esc = (v) => String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

function safeGet(key) { try { return localStorage.getItem(key); } catch { return null; } }
function safeSet(key, value) { try { localStorage.setItem(key, value); } catch { /* ignore */ } }

async function api(url, opt = {}) {
  const r = await fetch(url, { headers: { "Content-Type": "application/json" }, ...opt });
  const t = await r.text(), b = t ? JSON.parse(t) : null;
  if (!r.ok) throw Error(b?.error || "Không thể thực hiện thao tác này");
  return b;
}

/* ---------- notifications ---------- */
const notices = [];
function notice(message, kind = "success") {
  notices.unshift({ message, kind });
  notices.splice(30);
  const card = document.createElement("p");
  card.className = `toast ${kind}`;
  card.textContent = message;
  $("#notification-list").append(card);
  setTimeout(() => card.remove(), 3000);
  $("#notification-history-list").innerHTML = notices.map((x) => `<p class="${x.kind}">${esc(x.message)}</p>`).join("");
  $("#notice-count").hidden = false;
  $("#notice-count").textContent = notices.length;
}

/* ---------- helpers ---------- */
const selected = () => state.games.find((x) => x.id === state.selected);
const live = () => state.sessions[0];
const triggersOf = (game) => (game?.triggers || Object.keys(TRIGGERS)).filter((t) => TRIGGERS[t]);
function page(name) {
  $$(".page").forEach((x) => x.classList.toggle("active", x.id === `${name}-page`));
  $$("nav button").forEach((x) => x.classList.toggle("active", x.dataset.page === name));
}

function triggerTitle(r) {
  if (r.eventType === "gift") return r.giftName || (r.giftId ? `Quà ${r.giftId}` : "Mọi quà");
  if (r.eventType === "like") return `Mỗi ${r.likeEvery} tim`;
  if (r.eventType === "comment") return r.matchMode === "any" ? "Mọi comment" : (r.keywords || []).join(", ");
  if (r.eventType === "follow") return "Người xem follow";
  return "Người xem share";
}
function triggerDetail(r) {
  if (r.eventType === "gift") return [r.giftId ? `ID ${r.giftId}` : "bất kỳ quà nào", r.giftDiamonds != null ? `${r.giftDiamonds} xu` : ""].filter(Boolean).join(" · ");
  if (r.eventType === "like") return r.likeScope === "user" ? "đếm riêng từng người" : "cộng dồn cả phòng";
  if (r.eventType === "comment") return `khớp ${MATCH_LABEL[r.matchMode] || r.matchMode}`;
  if (r.eventType === "follow") return "1 lần mỗi người / phiên";
  return "mỗi lượt share";
}

/* ---------- render ---------- */
function renderGames() {
  const list = $("#game-list");
  list.innerHTML = state.games.map((g) => `<article class="game-card"><div class="game-mark">${esc(g.name[0])}</div><p class="eyebrow">${esc(g.id)}</p><h2>${esc(g.name)}</h2><p>${g.actions.length} effect · ${triggersOf(g).map((t) => TRIGGERS[t].label).join(", ")}</p><button class="button primary" data-select="${esc(g.id)}">Chọn game</button>${g.weeklyBoard ? `<div class="week-export"><a class="button secondary" href="/api/games/${encodeURIComponent(g.id)}/top.csv?week=current">Top tuần này</a><a class="button secondary" href="/api/games/${encodeURIComponent(g.id)}/top.csv?week=previous">Top tuần trước</a></div>` : ""}</article>`).join("") || '<div class="empty-panel">Chưa có game trong thư mục Game/.</div>';
  list.querySelectorAll("[data-select]").forEach((b) => (b.onclick = () => selectGame(b.dataset.select)));
}

function renderStatus() {
  const g = selected(), l = live(), same = l?.gameId === g?.id, status = $("#live-state");
  const otherName = l ? state.games.find((x) => x.id === l.gameId)?.name || l.gameId : "";
  $("#effect-title").textContent = g?.name || "Chọn game trước";
  $("#effect-subtitle").textContent = l && !same ? `Phiên ${otherName} đang chạy. Dừng phiên đó trước.` : "Thiết lập quà, tim, comment, follow, share và effect của game này.";
  $("#start-game").disabled = !g || !!l;
  $("#stop-session").hidden = !l;
  $("#add-effect").disabled = !g;
  if (!l) status.textContent = "Chưa khởi động game";
  else if (!same) status.textContent = `Đang chạy: ${otherName}`;
  else if (!l.gameOnline) status.textContent = "Game offline · hãy mở lại game hoặc dừng phiên";
  else if (state.listener.state === "connected") status.textContent = "LIVE đã kết nối";
  else if (state.listener.state === "error") status.textContent = `Lỗi LIVE: ${state.listener.lastError || "không rõ lỗi"}`;
  else status.textContent = "Game online · chưa kết nối LIVE";
  status.className = `status ${l?.gameOnline && state.listener.state !== "error" ? "ready" : ""}`;
  $("#connect-live").disabled = !l || !same || !l.gameOnline;
  $("#connect-live").textContent = state.listener.state === "connected" ? "Ngắt LIVE" : state.listener.state === "error" ? "Thử kết nối lại" : "Kết nối LIVE";
  renderStats();
}

function renderStats() {
  const box = $("#live-stats"), l = live();
  if (!l || l.gameId !== state.selected || !state.stats) { box.hidden = true; return; }
  const s = state.stats.stats || {};
  const progress = (state.stats.likeProgress || []).map((p) => `<span><b>${p.current}/${p.every}</b>tim tới lần kế</span>`).join("");
  box.hidden = false;
  box.innerHTML = `<span><b>${s.gift || 0}</b>quà</span><span><b>${s.like || 0}</b>tim</span><span><b>${s.comment || 0}</b>comment</span><span><b>${s.follow || 0}</b>follow</span><span><b>${s.share || 0}</b>share</span>${progress}`;
}

function renderFilter() {
  const g = selected(), types = triggersOf(g);
  const counts = Object.fromEntries(types.map((t) => [t, state.mappings.filter((m) => m.eventType === t).length]));
  $("#trigger-filter").innerHTML = [["all", `Tất cả (${state.mappings.length})`], ...types.map((t) => [t, `${TRIGGERS[t].label} (${counts[t]})`])]
    .map(([id, label]) => `<button type="button" role="tab" data-filter="${id}" aria-selected="${state.filter === id}">${esc(label)}</button>`).join("");
  $$("#trigger-filter [data-filter]").forEach((b) => (b.onclick = () => { state.filter = b.dataset.filter; renderMappings(); }));
}

function renderMappings() {
  const g = selected();
  renderFilter();
  const rows = state.mappings.filter((m) => state.filter === "all" || m.eventType === state.filter);
  const pic = (src, fallback) => (src ? `<img src="${esc(src)}" alt="">` : `<i>${fallback}</i>`);
  // Hiện tham số theo nhãn trong manifest (vd. "Tu vi 50") thay vì tên kỹ thuật ("amount 50").
  const params = (r) => { const defs = g?.actions.find((a) => a.id === r.action)?.parameters || {}; return Object.entries(r.params || {}).map(([k, v]) => `${defs[k]?.label || k} ${v}`).join(" · "); };
  // Bảng vẽ lại mỗi khi có log mới: giữ chữ đang gõ trong các ô nhập và vị trí con trỏ.
  const active = document.activeElement, keep = {};
  $$("#mapping-list input").forEach((x) => (keep[x.id || x.dataset.commentFor] = x.value));
  const focusKey = active?.closest?.("#mapping-list") ? active.id || active.dataset.commentFor : null, caret = focusKey ? active.selectionStart : null;
  $("#mapping-list").innerHTML = rows.length
    ? `<div class="table-head"><span>Sự kiện LIVE</span><span>Effect game</span><span><input id="test-viewer" class="test-viewer" value="${esc(testViewer())}" placeholder="Người xem test" title="Tên người xem dùng khi bấm Test"></span></div>${rows.map((r) => `<article class="mapping-row ${r.enabled === false ? "disabled" : ""}"><div class="asset">${pic(r.eventType === "gift" ? r.giftImage : "", TRIGGERS[r.eventType]?.icon || "✦")}<strong><span class="trigger-badge">${esc(TRIGGERS[r.eventType]?.label || r.eventType)}</span>${esc(triggerTitle(r))}</strong>${r.eventType === "comment" && r.matchMode === "any" ? `<input class="row-comment" data-comment-for="${esc(r.id)}" placeholder="Nhập comment rồi bấm Test">` : `<small>${esc(triggerDetail(r))}</small>`}</div><div class="asset">${pic(r.effectImage, "◌")}<strong>${esc(g?.actions.find((a) => a.id === r.action)?.name || r.action)}</strong><small>${esc(params(r) || "Không có tham số")}${r.repeat === "once" ? " · chạy 1 lần" : ""} · cooldown ${r.cooldownMs}ms${r.userCooldownMs ? ` / người ${r.userCooldownMs}ms` : ""}${r.enabled === false ? " · đang tắt" : ""}</small></div><div class="row-actions"><button data-test="${esc(r.id)}">Test</button><button data-edit="${esc(r.id)}">Sửa</button><button data-delete="${esc(r.id)}">Xóa</button></div></article>`).join("")}`
    : '<div class="empty-panel">Chưa có mapping cho mục này.</div>';
  $$("#mapping-list input").forEach((x) => {
    const key = x.id || x.dataset.commentFor;
    if (key in keep) x.value = keep[key];
    if (key === focusKey) { x.focus(); try { x.setSelectionRange(caret, caret); } catch {} }
  });
  $$("#mapping-list [data-test]").forEach((b) => (b.onclick = () => testMapping(b.dataset.test)));
  $$("#mapping-list [data-comment-for]").forEach((input) => (input.onkeydown = (e) => { if (e.key === "Enter") testMapping(input.dataset.commentFor); }));
  const viewer = $("#test-viewer");
  if (viewer) viewer.oninput = () => { try { localStorage.setItem("tiktool.testViewer", viewer.value.trim()); } catch {} };
  $$("#mapping-list [data-edit]").forEach((b) => (b.onclick = () => openEditor(b.dataset.edit)));
  $$("#mapping-list [data-delete]").forEach((b) => (b.onclick = () => removeMapping(b.dataset.delete)));
}

function renderLogs(entries) {
  $("#log-list").innerHTML = entries.length
    ? entries.map((v) => `<article class="log-row"><time>${new Date(v.at).toLocaleString("vi-VN")}</time><strong>${esc(v.message)}</strong><p>${esc(JSON.stringify(v.details))}</p></article>`).join("")
    : '<div class="empty-panel">Chưa có lịch sử.</div>';
}

/* ---------- data ---------- */
async function loadMappings() {
  state.mappings = state.selected ? await api(`/api/mappings?gameId=${encodeURIComponent(state.selected)}`) : [];
}
async function loadStats() {
  const l = live();
  state.stats = l ? await api(`/api/sessions/${l.id}/stats`).catch(() => null) : null;
}
async function refresh() {
  const [games, status, logs, gifts] = await Promise.all([api("/api/games"), api("/api/status"), api("/api/logs"), api("/api/gift-library")]);
  state.games = games;
  state.sessions = status.sessions || [];
  state.listener = status.listener || {};
  state.gifts = gifts || [];
  if (!state.selected && games[0]) state.selected = games[0].id;
  await Promise.all([loadMappings(), loadStats()]);
  renderGames();
  renderStatus();
  renderMappings();
  renderLogs(logs);
}
async function selectGame(id) {
  state.selected = id;
  state.filter = "all";
  await loadMappings();
  renderStatus();
  renderMappings();
  page("effects");
}

/* ---------- session ---------- */
async function startGame() {
  try {
    const g = selected();
    const s = await api("/api/sessions", { method: "POST", body: JSON.stringify({ gameId: g.id, username: state.username || "local-demo" }) });
    if (window.tiktoolDesktop) await window.tiktoolDesktop.launchGame({ gameId: g.id, url: s.gameUrl, window: g.window });
    else window.open(s.gameUrl, "_blank");
    notice("Đã khởi động game. Chờ game kết nối.");
    await refresh();
  } catch (e) { notice(e.message, "error"); }
}
async function stopSession() {
  const s = live();
  if (!s || !confirm("Dừng phiên game hiện tại?")) return;
  try {
    await api(`/api/sessions/${s.id}/stop`, { method: "POST" });
    if (state.listener.sessionId === s.id) await api("/api/listener/disconnect", { method: "POST" });
    notice("Đã dừng phiên.");
    await refresh();
  } catch (e) { notice(e.message, "error"); }
}
async function toggleLive() {
  const s = live();
  try {
    if (state.listener.state === "connected") await api("/api/listener/disconnect", { method: "POST" });
    else await api(`/api/sessions/${s.id}/listener/connect`, { method: "POST" });
    await refresh();
  } catch (e) { notice(e.message, "error"); }
}
function testViewer() {
  const field = $("#test-viewer");
  if (field && field.value.trim()) return field.value.trim();
  try { return localStorage.getItem("tiktool.testViewer") || "viewer_demo"; } catch { return "viewer_demo"; }
}
async function testMapping(id) {
  const r = state.mappings.find((x) => x.id === id), s = live();
  if (!s) return notice("Hãy Start Game trước khi test effect.", "error");
  if (s.gameId !== r.gameId) return notice("Game đang chạy không khớp mapping.", "error");
  // Comment / tim / quà: gửi sự kiện thật qua đúng mapping, người xem lấy từ ô "Người xem" ở đầu bảng.
  // Gọi thẳng effect sẽ không có người xem, game cần biết ai tặng quà / ai comment sẽ báo lỗi.
  if (["comment", "gift", "like"].includes(r.eventType)) {
    const name = testViewer(), body = { sessionId: s.id, type: r.eventType, eventId: crypto.randomUUID(), user: { id: name, username: name, nickname: name }, source: { mode: "injector" } };
    const input = $(`#mapping-list [data-comment-for="${CSS.escape(r.id)}"]`);
    if (r.eventType === "comment") body.text = r.matchMode === "any" ? (input?.value.trim() || "") : (r.keywords || [])[0] || "";
    if (r.eventType === "comment" && !body.text) { input?.focus(); return notice("Nhập nội dung comment trên dòng này rồi bấm Test.", "error"); }
    if (r.eventType === "gift") Object.assign(body, { giftId: r.giftId || "5655", count: 1 });
    if (r.eventType === "like") body.count = r.likeEvery || 1;
    try {
      const res = await api("/api/events", { method: "POST", body: JSON.stringify(body) });
      const match = res.matches.find((m) => m.commandId) || res.matches[0];
      if (!match) return notice(`${name}: không có mapping nào khớp.`, "error");
      const actionName = selected()?.actions.find((a) => a.id === match.action)?.name || match.action || "-";
      if (!match.commandId) return notice(`${name} · ${actionName}: ${match.status}`, "error");
      const result = (await waitCommandResults([match.commandId]))[match.commandId];
      if (!result) return notice(`${name} · ${actionName}: đã gửi, game chưa trả lời (game có đang mở?)`, "error");
      if (result.status !== "success") return notice(`${name} · ${actionName}: game báo lỗi: ${result.error || "thất bại"}`, "error");
      // Bảng có thể đã vẽ lại trong lúc chờ: tìm lại ô nhập hiện tại để xóa chữ đã gửi.
      const current = $(`#mapping-list [data-comment-for="${CSS.escape(r.id)}"]`);
      if (current) current.value = "";
      return notice(`${name} · ${actionName}: game đã chạy.`);
    } catch (e) { return notice(e.message, "error"); }
  }
  try {
    await api("/api/test", { method: "POST", body: JSON.stringify({ sessionId: s.id, gameId: r.gameId, action: r.action, params: r.params }) });
    notice("Đã gửi effect test vào game.");
  } catch (e) { notice(e.message, "error"); }
}
async function removeMapping(id) {
  if (!confirm("Xóa mapping này?")) return;
  try {
    await api(`/api/mappings/${id}`, { method: "DELETE" });
    await loadMappings();
    renderMappings();
    notice("Đã xóa mapping.");
  } catch (e) { notice(e.message, "error"); }
}

/* ---------- mapping editor ---------- */
function preview(target, url) {
  const img = $(target === "gift" ? "#gift-preview" : "#effect-preview");
  if (url) img.src = url; else img.removeAttribute("src");
}
function paramsFor(actionId, values = {}) {
  const defs = selected()?.actions.find((x) => x.id === actionId)?.parameters || {};
  $("#effect-params").innerHTML = Object.entries(defs).map(([name, def]) => {
    const value = values[name] ?? def.default ?? def.minimum ?? 1;
    return `<label>${esc(def.label || name)}<input data-param="${esc(name)}" type="number" ${def.minimum != null ? `min="${def.minimum}"` : ""} ${def.maximum != null ? `max="${def.maximum}"` : ""} step="${def.step ?? "any"}" ${def.required ? "required" : ""} value="${esc(value)}"></label>`;
  }).join("");
}
function setTrigger(type) {
  editor.trigger = type;
  $$("#trigger-tabs [data-trigger]").forEach((b) => b.setAttribute("aria-selected", String(b.dataset.trigger === type)));
  $$(".trigger-pane [data-pane]").forEach((p) => (p.hidden = p.dataset.pane !== type));
  $("#trigger-summary").hidden = type !== "gift";
  updateCommentMode();
}
function updateCommentMode() { $("#keywords-field").hidden = $("#comment-mode").value === "any"; }

function chooseGift(g) {
  $("#gift-id").value = g.giftId;
  $("#gift-name").value = g.name;
  $("#selected-gift-name").textContent = g.name || "Quà thủ công";
  $("#selected-gift-id").textContent = `Gift ID: ${g.giftId || "—"}${g.diamonds ? ` · ${g.diamonds} diamond` : ""}`;
  preview("gift", g.imageUrl || "");
  $$("#gift-browser button").forEach((x) => x.classList.toggle("selected", x.dataset.id === g.giftId));
}
function renderGiftList() {
  const query = $("#gift-search").value.trim().toLocaleLowerCase("vi"), sort = $("#gift-sort").value;
  const used = new Set(state.mappings.filter((m) => m.eventType === "gift" && m.id !== editor.id).map((x) => x.giftId));
  const items = state.catalog.filter((x) => !used.has(x.giftId) && x.name.toLocaleLowerCase("vi").includes(query))
    .sort((a, b) => (sort === "diamonds-desc" ? (b.diamonds || 0) - (a.diamonds || 0) : sort === "diamonds-asc" ? (a.diamonds || 0) - (b.diamonds || 0) : a.name.localeCompare(b.name, "vi")));
  $("#gift-total").textContent = `${items.length} quà`;
  $("#gift-browser").innerHTML = items.map((g) => `<button type="button" class="gift-item" data-id="${esc(g.giftId)}">${g.imageUrl ? `<img src="${esc(g.imageUrl)}" alt="">` : ""}<span><strong>${esc(g.name)}</strong><small>ID ${esc(g.giftId)} · ${g.diamonds || 0} ◆</small></span></button>`).join("") || "<p>Không tìm thấy quà phù hợp.</p>";
  $$("#gift-browser [data-id]").forEach((b) => (b.onclick = () => chooseGift(state.catalog.find((g) => g.giftId === b.dataset.id))));
}

async function openEditor(id = null) {
  const game = selected();
  if (!game) return;
  const rule = id ? state.mappings.find((x) => x.id === id) : null;
  editor.id = rule?.id || null;
  editor.effectImage = rule?.effectImage || "";
  const types = triggersOf(game);
  $("#mapping-eyebrow").textContent = rule ? "SỬA MAPPING" : "TẠO MAPPING";
  $("#mapping-heading").textContent = rule ? "Cập nhật effect game" : "Sự kiện LIVE → Effect game";
  $("#trigger-tabs").innerHTML = types.map((t) => `<button type="button" role="tab" data-trigger="${t}" ${rule && rule.eventType !== t ? "disabled" : ""}>${TRIGGERS[t].icon} ${TRIGGERS[t].label}</button>`).join("");
  $$("#trigger-tabs [data-trigger]").forEach((b) => (b.onclick = () => setTrigger(b.dataset.trigger)));

  // gift
  $("#gift-search").value = "";
  $("#manual-gift-fields").hidden = true;
  $("#manual-gift-id").value = "";
  $("#manual-gift-name").value = "";
  if (rule?.eventType === "gift") chooseGift({ giftId: rule.giftId, name: rule.giftName, imageUrl: rule.giftImage, diamonds: rule.giftDiamonds });
  else { $("#gift-id").value = ""; $("#gift-name").value = ""; $("#selected-gift-name").textContent = "Chọn một quà bên trái"; $("#selected-gift-id").textContent = "Gift ID sẽ tự điền"; preview("gift", ""); }
  // like / comment
  $("#like-every").value = rule?.likeEvery || 100;
  $("#like-scope").value = rule?.likeScope || "room";
  $("#comment-mode").value = rule?.matchMode || "contains";
  $("#comment-keywords").value = (rule?.keywords || []).join(", ");
  // effect
  $("#effect-action").innerHTML = game.actions.map((a) => `<option value="${esc(a.id)}">${esc(a.name)}</option>`).join("");
  if (rule) $("#effect-action").value = rule.action;
  paramsFor($("#effect-action").value, rule?.params);
  $("#repeat-mode").value = rule?.repeat || "multiply";
  $("#cooldown").value = rule?.cooldownMs ?? 0;
  $("#user-cooldown").value = rule?.userCooldownMs ?? 0;
  $("#rule-enabled").checked = rule ? rule.enabled !== false : true;
  preview("effect", editor.effectImage);
  setTrigger(rule?.eventType || (state.filter !== "all" && types.includes(state.filter) ? state.filter : types[0]));
  $("#mapping-dialog").showModal();

  try {
    state.catalog = await api("/api/gift-catalog");
    $("#catalog-empty").hidden = !!state.catalog.length;
  } catch { state.catalog = []; $("#catalog-empty").hidden = false; }
  renderGiftList();
}

function editorPayload() {
  const game = selected(), type = editor.trigger;
  const params = Object.fromEntries($$("[data-param]").map((x) => [x.dataset.param, Number(x.value)]));
  const payload = {
    gameId: game.id, eventType: type, action: $("#effect-action").value, params,
    repeat: $("#repeat-mode").value, effectImage: editor.effectImage,
    cooldownMs: Number($("#cooldown").value) || 0, userCooldownMs: Number($("#user-cooldown").value) || 0,
    enabled: $("#rule-enabled").checked
  };
  if (type === "gift") {
    payload.giftId = $("#gift-id").value.trim();
    payload.giftName = $("#gift-name").value.trim();
    payload.giftImage = $("#gift-preview").getAttribute("src") || "";
    if (!payload.giftId || !payload.giftName) throw Error("Hãy chọn quà từ danh mục hoặc nhập quà thủ công.");
  }
  if (type === "like") { payload.likeEvery = Number($("#like-every").value) || 1; payload.likeScope = $("#like-scope").value; }
  if (type === "comment") {
    payload.matchMode = $("#comment-mode").value;
    payload.keywords = $("#comment-keywords").value.split(",").map((x) => x.trim()).filter(Boolean);
    if (payload.matchMode !== "any" && !payload.keywords.length) throw Error("Nhập ít nhất một từ khóa comment.");
  }
  return payload;
}

async function saveEditor(event) {
  event.preventDefault();
  try {
    const payload = editorPayload();
    await api(editor.id ? `/api/mappings/${editor.id}` : "/api/mappings", { method: editor.id ? "PATCH" : "POST", body: JSON.stringify(payload) });
    $("#mapping-dialog").close();
    editor.id = null;
    notice("Đã lưu mapping.");
    await loadMappings();
    renderMappings();
  } catch (e) { notice(e.message, "error"); }
}

async function pickEffectImage() {
  if (window.tiktoolDesktop) {
    const x = await window.tiktoolDesktop.pickImage();
    if (x) { editor.effectImage = x.url; preview("effect", x.url); }
    return;
  }
  $("#effect-file").click();
}
async function uploadEffectFile() {
  const f = $("#effect-file").files[0];
  if (!f) return;
  try {
    const dataUrl = await new Promise((ok, no) => { const r = new FileReader(); r.onload = () => ok(r.result); r.onerror = no; r.readAsDataURL(f); });
    const x = await api("/api/assets", { method: "POST", body: JSON.stringify({ dataUrl }) });
    editor.effectImage = x.url;
    preview("effect", x.url);
    notice("Đã lưu ảnh vào thư viện local.");
  } catch (e) { notice(e.message, "error"); }
}

/* ---------- injector ---------- */
function updateInjector() {
  const type = $("#inject-type").value;
  $$("[data-inject]").forEach((x) => (x.hidden = !x.dataset.inject.split(" ").includes(type)));
}
async function waitCommandResults(ids, timeoutMs = 3000) {
  const found = {}, until = Date.now() + timeoutMs;
  while (Date.now() < until && Object.keys(found).length < ids.length) {
    await new Promise((resolve) => setTimeout(resolve, 300));
    try {
      for (const log of await api("/api/logs")) if (log.message === "Game command result" && ids.includes(log.details?.commandId)) found[log.details.commandId] = log.details;
    } catch { break; }
  }
  return found;
}
async function inject(event) {
  event.preventDefault();
  const s = live();
  if (!s) return notice("Hãy Start Game trước khi giả lập sự kiện.", "error");
  const type = $("#inject-type").value, name = $("#inject-user").value.trim() || "viewer_demo";
  const body = { sessionId: s.id, type, eventId: crypto.randomUUID(), user: { id: name, username: name, nickname: name }, source: { mode: "injector" } };
  if (type === "gift") Object.assign(body, { giftId: $("#inject-gift").value.trim() || "5655", count: Number($("#inject-count").value) || 1 });
  if (type === "like" || type === "share") body.count = Number($("#inject-count").value) || 1;
  if (type === "comment") body.text = $("#inject-text").value;
  try {
    const r = await api("/api/events", { method: "POST", body: JSON.stringify(body) });
    const line = (m, extra) => `${m.ruleId ? state.mappings.find((x) => x.id === m.ruleId)?.action || m.ruleId : "-"}: ${extra || m.status}${m.times ? ` x${m.times}` : ""}${m.progress ? ` (${m.progress.current}/${m.progress.every})` : ""}`;
    $("#inject-result").textContent = `${name} · ` + (r.matches.length ? r.matches.map((m) => line(m)).join("\n") : "Không có mapping nào khớp.");
    // Chờ game trả kết quả để hiện thành công / lỗi kèm lý do (vd. "người xem chưa tham gia"), thay vì chỉ "queued".
    const pending = r.matches.filter((m) => m.commandId && m.status === "queued");
    if (pending.length) {
      const results = await waitCommandResults(pending.map((m) => m.commandId));
      $("#inject-result").textContent = `${name} · ` + r.matches.map((m) => {
        const res = results[m.commandId];
        return line(m, res ? (res.status === "success" ? "✓ game đã chạy" : `✗ game báo lỗi: ${res.error || "thất bại"}`) : undefined);
      }).join("\n");
    }
    await loadStats();
    renderStats();
  } catch (e) { notice(e.message, "error"); }
}

/* ---------- library ---------- */
function openLibrary(title, rows) {
  $("#library-title").textContent = title;
  $("#library-list").innerHTML = rows.length
    ? rows.map((x) => `<article class="library-row">${x.imageUrl ? `<img src="${esc(x.imageUrl)}" alt="">` : ""}<p><strong>${esc(x.name)}</strong><br><small>${esc(x.detail)}</small></p></article>`).join("")
    : "<p>Chưa có dữ liệu. Hãy kết nối TikTok LIVE để tải danh mục quà của phòng.</p>";
  $("#library-dialog").showModal();
}

/* ---------- wire up ---------- */
document.addEventListener("DOMContentLoaded", () => {
  $$("nav button").forEach((b) => (b.onclick = () => page(b.dataset.page)));
  $("#change-game").onclick = () => page("games");
  $("#start-game").onclick = startGame;
  $("#stop-session").onclick = stopSession;
  $("#connect-live").onclick = toggleLive;
  $("#add-effect").onclick = () => openEditor();
  $("#notice-bell").onclick = () => ($("#notification-history").hidden = !$("#notification-history").hidden);

  $("#profile").onclick = () => { $("#tiktok-id").value = state.username; $("#profile-dialog").showModal(); };
  $("#cancel-profile").onclick = () => $("#profile-dialog").close();
  $("#save-profile").onclick = async () => {
    state.username = $("#tiktok-id").value.trim().replace(/^@/, "");
    safeSet("tiktool.username", state.username);
    $("#profile-name").textContent = state.username || "streamer";
    if (live()) await api(`/api/sessions/${live().id}`, { method: "PATCH", body: JSON.stringify({ username: state.username }) }).catch((e) => notice(e.message, "error"));
    notice("Đã lưu TikTok ID.");
  };
  $("#profile-name").textContent = state.username || "streamer";

  $("#mapping-form").onsubmit = saveEditor;
  $("#cancel-mapping").onclick = () => { editor.id = null; $("#mapping-dialog").close(); };
  $("#effect-action").onchange = () => paramsFor($("#effect-action").value);
  $("#comment-mode").onchange = updateCommentMode;
  $("#gift-search").oninput = renderGiftList;
  $("#gift-sort").onchange = renderGiftList;
  $("#manual-gift").onclick = () => ($("#manual-gift-fields").hidden = !$("#manual-gift-fields").hidden);
  const manual = () => chooseGift({ giftId: $("#manual-gift-id").value.trim(), name: $("#manual-gift-name").value.trim() || "Quà thủ công" });
  $("#manual-gift-id").oninput = manual;
  $("#manual-gift-name").oninput = manual;
  $("#pick-effect-image").onclick = pickEffectImage;
  $("#effect-file").onchange = uploadEffectFile;

  $("#inject-type").onchange = updateInjector;
  $("#injector-form").onsubmit = inject;
  updateInjector();

  $("#show-effects").onclick = () => openLibrary("Effect của game", (selected()?.actions || []).map((x) => ({ name: x.name, detail: `ID effect: ${x.id}` })));
  $("#show-gifts").onclick = async () => {
    try {
      const gifts = await api("/api/gift-catalog");
      openLibrary("Danh mục quà TikTok", gifts.map((x) => ({ name: x.name, detail: `Gift ID: ${x.giftId} · ${x.diamonds || 0} diamond`, imageUrl: x.imageUrl })));
    } catch (e) { notice(e.message, "error"); }
  };

  const stream = new EventSource("/api/stream");
  // Mỗi lệnh game sinh nhiều sự kiện log/session. Gộp lại: tải lại dashboard tối đa 1 lần / 1 giây,
  // nếu không hub bị dội hàng chục request mỗi giây và lệnh game chạy trễ.
  let refreshTimer = null, refreshing = false, refreshAgain = false;
  const scheduleRefresh = () => {
    if (refreshTimer) return;
    refreshTimer = setTimeout(async () => {
      refreshTimer = null;
      if (refreshing) { refreshAgain = true; return; }
      refreshing = true;
      try { await refresh(); } catch (e) { notice(e.message, "error"); }
      refreshing = false;
      if (refreshAgain) { refreshAgain = false; scheduleRefresh(); }
    }, 1000);
  };
  ["log", "session", "listener"].forEach((t) => stream.addEventListener(t, scheduleRefresh));
  stream.addEventListener("live_stats", (e) => {
    const data = JSON.parse(e.data);
    if (live()?.id !== data.sessionId) return;
    loadStats().then(renderStats);
  });
  refresh().catch((e) => notice(e.message, "error"));
});
