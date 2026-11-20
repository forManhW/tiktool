using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Kết nối Game Gateway của TikTool (ws://127.0.0.1:8765), cùng giao thức với sdk/tiktool-game.js:
    ///   gửi game_hello → nhận game_hello_ack → nhận game_action → trả command_result (trong 5 giây) → gửi game_state (≤ 5 lần/giây).
    /// sessionId/token lấy từ dòng lệnh: --session &lt;id&gt; --token &lt;token&gt; [--ws &lt;url&gt;].
    /// Không có session thì chạy chế độ demo (dùng phím F5–F9).
    /// </summary>
    public class TikToolSource : MonoBehaviour
    {
        public const string GameId = "red-blue-brawl";

        [Header("Chỉ dùng khi chạy trong Unity Editor (copy từ Game URL của TikTool)")]
        public string editorSessionId;
        public string editorToken;
        public string editorWsUrl = "ws://127.0.0.1:8765";

        string sessionId, token, wsUrl = "ws://127.0.0.1:8765";
        ClientWebSocket socket;
        CancellationTokenSource cts;
        readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        bool authenticated, stateDirty;
        float nextStateAt;

        void Start()
        {
            ParseArgs();
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
            {
                GameState.SetConnectionStatus("demo");
                Debug.Log("[TikTool] Không có --session/--token: chạy chế độ demo.");
                return;
            }
            GameEvents.ScoreChanged += OnDirty2;
            GameEvents.CoreHealthChanged += OnDirty3;
            GameEvents.FighterCountChanged += OnDirty2;
            GameEvents.ModeChanged += OnDirtyMode;
            cts = new CancellationTokenSource();
            _ = RunLoop(cts.Token);
        }

        void OnDirty2(int a, int b) => stateDirty = true;
        void OnDirty3(Team t, float a, float b) => stateDirty = true;
        void OnDirtyMode(GameMode m) => stateDirty = true;

        void ParseArgs()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "--session": sessionId = args[i + 1]; break;
                    case "--token": token = args[i + 1]; break;
                    case "--ws": wsUrl = args[i + 1]; break;
                }
            }
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(sessionId)) sessionId = editorSessionId;
            if (string.IsNullOrEmpty(token)) token = editorToken;
            if (!string.IsNullOrEmpty(editorWsUrl) && wsUrl == "ws://127.0.0.1:8765") wsUrl = editorWsUrl;
#endif
        }

        async Task RunLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                GameState.SetConnectionStatus("connecting");
                socket = new ClientWebSocket();
                int? closeCode = null;
                try
                {
                    await socket.ConnectAsync(new Uri(wsUrl), ct);
                    await Send(new JObject { ["type"] = "game_hello", ["gameId"] = GameId, ["sessionId"] = sessionId, ["token"] = token, ["role"] = "game" });
                    await ReceiveLoop(ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e) { Debug.LogWarning($"[TikTool] Mất kết nối: {e.Message}"); }

                authenticated = false;
                if (socket.CloseStatus.HasValue) closeCode = (int)socket.CloseStatus.Value;
                socket.Dispose();
                socket = null;

                if (closeCode == 4003) { GameState.SetConnectionStatus("unauthorized"); Debug.LogError("[TikTool] Token không hợp lệ (4003). Bấm Start Game lại trong TikTool."); break; }
                GameState.SetConnectionStatus(closeCode == 4009 ? "duplicate_game" : "reconnecting");
                try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
            }
        }

        async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[16 * 1024];
            while (socket != null && socket.State == WebSocketState.Open)
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (r.MessageType == WebSocketMessageType.Close)
                        {
                            try { await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
                            return;
                        }
                        ms.Write(buffer, 0, r.Count);
                    } while (!r.EndOfMessage);
                    // Unity chạy phần tiếp theo của await trên luồng chính, nên gọi luật chơi trực tiếp được.
                    Handle(Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
        }

        void Handle(string json)
        {
            JObject msg;
            try { msg = JObject.Parse(json); } catch { return; }
            switch ((string)msg["type"])
            {
                case "game_hello_ack":
                    authenticated = true;
                    GameState.SetConnectionStatus("connected");
                    Debug.Log("[TikTool] Đã kết nối.");
                    stateDirty = true;
                    nextStateAt = 0f;
                    break;
                case "game_action":
                    HandleAction(msg);
                    break;
            }
        }

        void HandleAction(JObject msg)
        {
            string commandId = (string)msg["id"];
            var p = msg["params"] as JObject;
            var source = msg["source"] as JObject;
            var user = source?["user"] as JObject;
            var gift = source?["gift"] as JObject;

            // params.count = số lần TikTool kích hoạt (combo quà). Mapping không đặt amount thì lấy count làm số lượng.
            int count = Mathf.Max(1, Mathf.RoundToInt(p?["count"]?.Value<float>() ?? 1f));
            var cmd = new GameCommand
            {
                Id = commandId,
                Action = (string)msg["action"],
                Amount = Mathf.Max(1, Mathf.RoundToInt(p?["amount"]?.Value<float>() ?? count)),
                EventType = (string)source?["eventType"] ?? "test",
                GiftName = (string)gift?["name"],
                GiftDiamonds = (gift?["diamonds"]?.Value<int>() ?? 0) * count,
                User = new ViewerInfo
                {
                    Id = (string)user?["id"] ?? (string)user?["uniqueId"] ?? (string)user?["username"],
                    Nickname = (string)user?["nickname"] ?? (string)user?["username"],
                    AvatarUrl = (string)user?["avatarUrl"],
                },
            };
            cmd.Reply = (ok, error) => _ = Send(new JObject
            {
                ["type"] = "command_result",
                ["commandId"] = commandId,
                ["status"] = ok ? "success" : "failed",
                ["error"] = ok ? null : (error ?? "Action unavailable"),
                ["executedAt"] = DateTime.UtcNow.ToString("o"),
            });
            CommandRouter.Handle(cmd);
        }

        void Update()
        {
            if (!authenticated) return;
            if (Time.unscaledTime >= nextStateAt && (stateDirty || Time.unscaledTime - nextStateAt > 2f))
            {
                stateDirty = false;
                nextStateAt = Time.unscaledTime + 0.2f;
                _ = Send(new JObject { ["type"] = "game_state", ["state"] = BuildState() });
            }
        }

        static JObject BuildState()
        {
            var m = MatchManager.I;
            var fm = FighterManager.I;
            return new JObject
            {
                ["score"] = new JObject { ["blue"] = m != null ? m.ScoreBlue : 0, ["red"] = m != null ? m.ScoreRed : 0 },
                ["cores"] = new JObject
                {
                    ["blue"] = m?.Core(Team.Blue) != null ? Mathf.RoundToInt(m.Core(Team.Blue).Hp) : 0,
                    ["red"] = m?.Core(Team.Red) != null ? Mathf.RoundToInt(m.Core(Team.Red).Hp) : 0,
                },
                ["fighters"] = new JObject { ["blue"] = fm != null ? fm.Count(Team.Blue) : 0, ["red"] = fm != null ? fm.Count(Team.Red) : 0 },
                ["event"] = MatchEvents.I != null ? MatchEvents.I.CurrentEventId : "",
                ["mode"] = GameState.Mode == GameMode.Manual ? "manual" : "live",
            };
        }

        async Task Send(JObject value)
        {
            var s = socket;
            if (s == null || s.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(value.ToString(Newtonsoft.Json.Formatting.None));
            await sendLock.WaitAsync();
            try { await s.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
            catch (Exception e) { Debug.LogWarning($"[TikTool] Gửi thất bại: {e.Message}"); }
            finally { sendLock.Release(); }
        }

        void OnDestroy()
        {
            GameEvents.ScoreChanged -= OnDirty2;
            GameEvents.CoreHealthChanged -= OnDirty3;
            GameEvents.FighterCountChanged -= OnDirty2;
            GameEvents.ModeChanged -= OnDirtyMode;
            cts?.Cancel();
            try { socket?.Abort(); } catch { }
        }
    }
}
