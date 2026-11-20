using System;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Một hiệu ứng vừa bắt đầu (do lệnh từ TikTool hoặc phím test).</summary>
    public struct EffectNotice
    {
        public Team Team;
        public string EffectId;      // punch, combo_punch, superhero, lightning, tank, meteor, giant_robot, spawn, like_power, join
        public string DisplayName;   // "Xe tăng"
        public int Tier;             // 0..7 theo bảng hiệu ứng
        public int Amount;
        public Viewer By;            // có thể null
        public Vector3 Position;
        public Transform Target;     // vật thể để camera bám theo (nhân vật, xe tăng, robot), có thể null
    }

    /// <summary>Sự kiện giữa trận: X2, X2 phe yếu, thảm họa.</summary>
    public struct MatchEventNotice
    {
        public string Id;            // x2, x2_weak, disaster_warning, disaster
        public string DisplayName;
        public float Duration;       // giây, 0 nếu tức thời
        public bool HasTeam;
        public Team Team;            // chỉ có nghĩa khi HasTeam
    }

    public enum NoticeKind { Info, Join, Gift, Warning, Guide }

    /// <summary>
    /// HỢP ĐỒNG giữa phần lõi (Core/Fighters/Effects/Camera/Net) và phần giao diện (UI).
    /// UI chỉ được ĐĂNG KÝ các event này và đọc trạng thái qua GameQuery; không gọi ngược vào lõi để đổi luật chơi.
    /// Chỉ lõi mới gọi các hàm Raise*.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int, int> ScoreChanged;                 // (xanh, đỏ) tỉ số hôm nay
        public static event Action<Team, float, float> CoreHealthChanged;  // (phe, máu, máu tối đa)
        public static event Action<Viewer> PlayerJoined;                   // người mới có nhân vật trong ván
        public static event Action<EffectNotice> EffectStarted;
        public static event Action<Team, Viewer> RoundWon;                 // (phe thắng, MVP có thể null)
        public static event Action RoundReset;
        public static event Action<GameMode> ModeChanged;
        public static event Action<bool> HudVisibilityChanged;
        public static event Action<string> ConnectionStatusChanged;        // demo | connecting | connected | reconnecting | unauthorized | duplicate_game
        public static event Action<MatchEventNotice> MatchEventStarted;
        public static event Action<MatchEventNotice> MatchEventEnded;
        public static event Action<int, int> FighterCountChanged;          // (xanh, đỏ)
        public static event Action<string, NoticeKind> Notice;             // thông báo chữ (UI chỉ hiện ở chế độ Live tự động)
        public static event Action SettingsMenuRequested;                  // streamer bấm F10; UI có menu thì đăng ký event này

        /// <summary>Trả false nếu chưa có UI nào làm menu cài đặt (khi đó F10 mở thẳng file settings.json).</summary>
        public static bool RaiseSettingsMenuRequested()
        {
            if (SettingsMenuRequested == null) return false;
            SettingsMenuRequested.Invoke();
            return true;
        }

        public static void RaiseScoreChanged(int blue, int red) => ScoreChanged?.Invoke(blue, red);
        public static void RaiseCoreHealthChanged(Team team, float hp, float max) => CoreHealthChanged?.Invoke(team, hp, max);
        public static void RaisePlayerJoined(Viewer viewer) => PlayerJoined?.Invoke(viewer);
        public static void RaiseEffectStarted(EffectNotice notice) => EffectStarted?.Invoke(notice);
        public static void RaiseRoundWon(Team winner, Viewer mvp) => RoundWon?.Invoke(winner, mvp);
        public static void RaiseRoundReset() => RoundReset?.Invoke();
        public static void RaiseModeChanged(GameMode mode) => ModeChanged?.Invoke(mode);
        public static void RaiseHudVisibilityChanged(bool visible) => HudVisibilityChanged?.Invoke(visible);
        public static void RaiseConnectionStatusChanged(string status) => ConnectionStatusChanged?.Invoke(status);
        public static void RaiseMatchEventStarted(MatchEventNotice notice) => MatchEventStarted?.Invoke(notice);
        public static void RaiseMatchEventEnded(MatchEventNotice notice) => MatchEventEnded?.Invoke(notice);
        public static void RaiseFighterCountChanged(int blue, int red) => FighterCountChanged?.Invoke(blue, red);
        public static void RaiseNotice(string text, NoticeKind kind) => Notice?.Invoke(text, kind);
    }
}
