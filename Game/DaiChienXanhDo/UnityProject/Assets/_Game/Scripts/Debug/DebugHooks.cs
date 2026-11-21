using System;
using System.IO;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Móc cho test tự động. Chỉ bật khi game chạy với  --test-dir &lt;thư mục&gt;.
    /// Khi bật, TikTool (Test Lab / API /api/test) gửi được các lệnh:
    ///   debug_screenshot  : chụp khung hình vào &lt;thư mục&gt;/shot_NN.png và ghi trạng thái vào Player.log
    ///   debug_state       : chỉ ghi trạng thái
    ///   debug_toggle_mode : đổi Live tự động ↔ Thường (như phím F1)
    /// Không bật thì các lệnh debug_* bị từ chối.
    /// </summary>
    public static class DebugHooks
    {
        static string dir;
        static int shot;

        public static bool Enabled => dir != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--test-dir") dir = args[i + 1];
            if (dir != null) { Directory.CreateDirectory(dir); Debug.Log($"[Test] Bật móc test, thư mục: {dir}"); }
        }

        public static (bool, string) Handle(string action)
        {
            if (!Enabled) return (false, "Móc test chưa bật (--test-dir)");
            switch (action)
            {
                case "debug_screenshot":
                    shot++;
                    string file = Path.Combine(dir, $"shot_{shot:00}.png");
                    ScreenCapture.CaptureScreenshot(file);
                    LogState($"shot_{shot:00}");
                    return (true, null);
                case "debug_state":
                    LogState("state");
                    return (true, null);
                case "debug_quake": case "debug_tornado": case "debug_volcano":
                    if (MatchEvents.I == null) return (false, "Chưa sẵn sàng");
                    MatchEvents.I.TriggerDisaster(action == "debug_quake" ? 0 : action == "debug_tornado" ? 1 : 2);
                    return (true, null);
                case "debug_toggle_mode":
                    GameState.ToggleMode();
                    LogState("mode");
                    return (true, null);
                default:
                    return (false, $"Lệnh test không hỗ trợ: {action}");
            }
        }

        public static void LogState(string tag)
        {
            var m = MatchManager.I;
            var fm = FighterManager.I;
            var ev = MatchEvents.I;
            string cores = m == null ? "-" : $"{Hp(m.Core(Team.Blue))}/{Hp(m.Core(Team.Red))}";
            int mains = 0;
            foreach (var v in PlayerRegistry.All) if (v.HasLiveFighter) mains++;
            int labels = 0;
            var plates = GameObject.Find("FighterNameplates");
            if (plates != null) foreach (Transform t in plates.transform) if (t.gameObject.activeSelf && t.name.StartsWith("Nameplate_")) labels++;
            var follow = CameraRig.I != null && CameraRig.I.Following != null ? CameraRig.I.Following.GetComponent<Fighter>() : null;
            var states = new System.Collections.Generic.Dictionary<string, int>();
            if (fm != null)
                foreach (Team t in new[] { Team.Blue, Team.Red })
                    foreach (var f in fm.List(t)) { string s = f.AnimState; states[s] = states.TryGetValue(s, out int k) ? k + 1 : 1; }
            tag += " anim=" + string.Join(",", System.Linq.Enumerable.Select(states, kv => $"{kv.Key}:{kv.Value}"));
            tag += $" labels={labels} follow={(follow != null && follow.Owner != null ? follow.Owner.Nickname : "-")}";
            Debug.Log($"[Test] {tag} t={Time.time:0.0} fighting={m?.IsFighting} score={m?.ScoreBlue}:{m?.ScoreRed} cores={cores} " +
                      $"fighters={fm?.Count(Team.Blue)}/{fm?.Count(Team.Red)} players={PlayerRegistry.All.Count} mainAlive={mains} " +
                      $"event={ev?.CurrentEventId} power={TeamPower.Of(Team.Blue):0.00}/{TeamPower.Of(Team.Red):0.00} " +
                      $"bigQueue={EffectManager.I?.QueuedBigEffects} mode={GameState.Mode} fps={1f / Mathf.Max(0.0001f, Time.smoothDeltaTime):0}");
        }

        static string Hp(CoreHealth c) => c == null ? "-" : Mathf.CeilToInt(c.Hp).ToString();
    }
}
