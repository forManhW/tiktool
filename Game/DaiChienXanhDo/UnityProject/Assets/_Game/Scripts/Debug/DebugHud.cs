using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// GIAO DIỆN TẠM (IMGUI) để test giai đoạn 1. Sẽ được thay bằng UI thật trong Scripts/UI/ (phần của Antigravity).
    /// Khi UI thật xong: tắt component này trong scene (bỏ tick), không cần xóa.
    /// Chỉ đọc GameEvents / GameState / FighterManager, không đổi luật chơi.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        struct Line { public string Text; public float Until; public Color Color; }

        readonly List<Line> notices = new List<Line>();
        string banner; float bannerUntil; Color bannerColor;
        string eventText; float eventUntil;
        GUIStyle label, big, small, center;
        Texture2D white;
        float scale = 1f;
        Camera cam;

        void OnEnable()
        {
            GameEvents.Notice += OnNotice;
            GameEvents.RoundWon += OnWon;
            GameEvents.MatchEventStarted += OnEventStart;
            GameEvents.MatchEventEnded += OnEventEnd;
        }

        void OnDisable()
        {
            GameEvents.Notice -= OnNotice;
            GameEvents.RoundWon -= OnWon;
            GameEvents.MatchEventStarted -= OnEventStart;
            GameEvents.MatchEventEnded -= OnEventEnd;
        }

        void OnNotice(string text, NoticeKind kind)
        {
            var color = kind == NoticeKind.Warning ? new Color(1f, 0.85f, 0.2f) : kind == NoticeKind.Gift ? new Color(1f, 0.6f, 1f) : Color.white;
            notices.Add(new Line { Text = text, Until = Time.time + 4f, Color = color });
            if (notices.Count > 5) notices.RemoveAt(0);
        }

        void OnWon(Team winner, Viewer mvp)
        {
            banner = $"PHE {winner.DisplayName()} THẮNG!" + (mvp != null ? $"\nMVP: {mvp.Nickname}" : "");
            bannerColor = winner.TeamColor();
            bannerUntil = Time.time + GameSettings.Current.celebrateSeconds;
        }

        void OnEventStart(MatchEventNotice n) { eventText = n.DisplayName; eventUntil = Time.time + Mathf.Max(2f, n.Duration); }
        void OnEventEnd(MatchEventNotice n) { if (eventText == n.DisplayName) eventUntil = 0f; }

        void EnsureStyles()
        {
            if (label != null) return;
            white = Texture2D.whiteTexture;
            label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true };
            big = new GUIStyle(label);
            small = new GUIStyle(label);
            center = new GUIStyle(label) { wordWrap = true };
        }

        void OnGUI()
        {
            if (!GameState.HudVisible) return;
            if (cam == null) cam = CameraRig.I != null ? CameraRig.I.cam : Camera.main;
            if (cam == null) return;
            EnsureStyles();

            Rect px = cam.pixelRect;
            Rect area = new Rect(px.x, Screen.height - px.yMax, px.width, px.height);
            scale = area.height / 1080f;
            label.fontSize = Mathf.RoundToInt(22 * scale);
            small.fontSize = Mathf.RoundToInt(16 * scale);
            big.fontSize = Mathf.RoundToInt(46 * scale);
            center.fontSize = Mathf.RoundToInt(30 * scale);

            DrawNames(area);
            DrawCores(area);
            DrawScore(area);

            bool live = GameState.Mode == GameMode.LiveAuto;
            if (live) DrawLiveNotices(area);
            DrawStatus(area, live);
        }

        void DrawScore(Rect area)
        {
            var m = MatchManager.I;
            if (m == null) return;
            var r = new Rect(area.x + 24 * scale, area.y + 20 * scale, 420 * scale, 60 * scale);
            Box(r, new Color(0, 0, 0, 0.55f));
            big.alignment = TextAnchor.MiddleLeft;
            var blue = new Rect(r.x + 16 * scale, r.y, r.width, r.height);
            Shadow(blue, $"<color=#2D7DFF>XANH {m.ScoreBlue}</color> : <color=#FF3B3B>{m.ScoreRed} ĐỎ</color>", big);
            big.alignment = TextAnchor.MiddleCenter;
        }

        void DrawCores(Rect area)
        {
            var m = MatchManager.I;
            if (m == null) return;
            foreach (Team t in new[] { Team.Blue, Team.Red })
            {
                var core = m.Core(t);
                if (core == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(core.transform.position + Vector3.up * 4.5f);
                if (sp.z < 0) continue;
                float w = 220 * scale, h = 22 * scale;
                var r = new Rect(sp.x - w / 2, Screen.height - sp.y - h, w, h);
                Box(r, new Color(0, 0, 0, 0.6f));
                Box(new Rect(r.x + 2, r.y + 2, (r.width - 4) * core.Ratio, r.height - 4), t.TeamColor());
                Shadow(r, $"{Mathf.CeilToInt(core.Hp)}", small);
            }
        }

        void DrawNames(Rect area)
        {
            var fm = FighterManager.I;
            if (fm == null) return;
            int total = fm.Total;
            bool crowded = total > GameSettings.Current.fullNameLimit;
            var topIds = new HashSet<string>();
            foreach (var v in PlayerRegistry.Top(Team.Blue, 3)) topIds.Add(v.Id);
            foreach (var v in PlayerRegistry.Top(Team.Red, 3)) topIds.Add(v.Id);
            Transform followed = CameraRig.I != null ? CameraRig.I.Following : null;

            foreach (Team t in new[] { Team.Blue, Team.Red })
            {
                var list = fm.List(t);
                for (int i = 0; i < list.Count; i++)
                {
                    var f = list[i];
                    if (f.Owner == null) continue;
                    bool highlight = f.transform == followed || topIds.Contains(f.Owner.Id) || Time.time - f.Owner.LastActionTime < 5f;
                    if (!f.IsMain && crowded) continue;
                    string text = crowded && !highlight ? Initials(f.Owner.Nickname) : f.Owner.Nickname;
                    Vector3 sp = cam.WorldToScreenPoint(f.transform.position + Vector3.up * (f.IsSuper ? 4.4f : 2.6f));
                    if (sp.z < 0) continue;
                    var style = f.IsMain ? label : small;
                    var size = style.CalcSize(new GUIContent(text));
                    var r = new Rect(sp.x - size.x / 2, Screen.height - sp.y - size.y, size.x, size.y);
                    if (!area.Overlaps(r)) continue;
                    var c = GUI.color;
                    GUI.color = f.IsMain ? Color.white : new Color(1, 1, 1, 0.7f);
                    Shadow(r, highlight ? $"<color=#FFE066>{text}</color>" : text, style);
                    GUI.color = c;
                }
            }
        }

        void DrawLiveNotices(Rect area)
        {
            // Dải hướng dẫn luân phiên ở đáy khung
            var lines = GameSettings.Current.guideLines;
            if (lines != null && lines.Length > 0)
            {
                string guide = lines[(int)(Time.time / 6f) % lines.Length];
                var r = new Rect(area.x, area.yMax - 60 * scale, area.width, 50 * scale);
                Box(new Rect(area.x + area.width * 0.15f, r.y, area.width * 0.7f, r.height), new Color(0, 0, 0, 0.5f));
                Shadow(r, guide, label);
            }

            // Thông báo gần nhất
            notices.RemoveAll(n => Time.time > n.Until);
            for (int i = 0; i < notices.Count; i++)
            {
                var r = new Rect(area.x, area.y + area.height * 0.62f + i * 34 * scale, area.width, 32 * scale);
                var c = GUI.color; GUI.color = notices[i].Color;
                Shadow(r, notices[i].Text, label);
                GUI.color = c;
            }

            if (Time.time < eventUntil && !string.IsNullOrEmpty(eventText))
            {
                float left = eventUntil - Time.time;
                var r = new Rect(area.x, area.y + 100 * scale, area.width, 50 * scale);
                Shadow(r, $"<color=#FFD43B>{eventText}  {left:0}s</color>", center);
            }

            if (Time.time < bannerUntil)
            {
                var r = new Rect(area.x, area.y + area.height * 0.35f, area.width, 140 * scale);
                Box(r, new Color(0, 0, 0, 0.6f));
                var c = GUI.color; GUI.color = bannerColor;
                Shadow(r, banner, big);
                GUI.color = c;
            }
        }

        void DrawStatus(Rect area, bool live)
        {
            string status = GameState.ConnectionStatus;
            string mode = live ? "LIVE TỰ ĐỘNG" : "THƯỜNG";
            string text = $"{mode} · {status} · {(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)):0} FPS · {FighterManager.I?.Total ?? 0} lính";
            var r = new Rect(area.xMax - 520 * scale, area.yMax - 26 * scale, 510 * scale, 22 * scale);
            small.alignment = TextAnchor.MiddleRight;
            Shadow(r, text, small);
            small.alignment = TextAnchor.MiddleCenter;
        }

        static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var parts = name.Split(' ');
            string s = "";
            foreach (var p in parts) if (p.Length > 0) s += char.ToUpperInvariant(p[0]);
            return s.Length > 3 ? s.Substring(0, 3) : s;
        }

        void Box(Rect r, Color color)
        {
            var c = GUI.color; GUI.color = color;
            GUI.DrawTexture(r, white);
            GUI.color = c;
        }

        static void Shadow(Rect r, string text, GUIStyle style)
        {
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, c.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), StripColor(text), style);
            GUI.color = c;
            GUI.Label(r, text, style);
        }

        static string StripColor(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<.*?>", "");
    }
}
