using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Sự kiện giữa trận do game tự chạy (cài trong settings.json, không phải TikTool):
    /// X2 Sức Mạnh định kỳ, Thảm họa khi sân quá đông. (Đã bỏ X2 Phe Yếu theo yêu cầu.)
    /// </summary>
    public class MatchEvents : MonoBehaviour
    {
        public static MatchEvents I { get; private set; }

        static readonly string[] DisasterNames = { "ĐỘNG ĐẤT", "LỐC XOÁY", "NÚI LỬA PHUN TRÀO" };

        float nextX2At, x2Until;
        bool x2Warned, x2Active;
        bool weakUsed, weakActive;
        Team weakTeam;
        float weakUntil;
        float disasterReadyAt, disasterAt;
        bool disasterPending;
        string disasterName;
        int disasterKind;

        public bool X2Active => x2Active;
        public bool WeakX2Active => weakActive;
        public Team WeakTeam => weakTeam;
        public string CurrentEventId => disasterPending ? "disaster_warning" : x2Active ? "x2" : weakActive ? "x2_weak" : "";

        void Awake() { I = this; }
        void Start() { ScheduleX2(); }

        void ScheduleX2()
        {
            var s = GameSettings.Current;
            nextX2At = Time.time + Random.Range(s.x2IntervalMin, Mathf.Max(s.x2IntervalMin, s.x2IntervalMax));
            x2Warned = false;
        }

        public float Multiplier(Team team)
        {
            float m = x2Active ? 2f : 1f;
            if (weakActive && team == weakTeam) m *= 2f;
            return m;
        }

        void Update()
        {
            var match = MatchManager.I;
            if (match == null || !match.IsFighting) return;
            var s = GameSettings.Current;
            float now = Time.time;

            // X2 Sức Mạnh
            if (!x2Active && !x2Warned && now >= nextX2At - 5f)
            {
                x2Warned = true;
                GameEvents.RaiseNotice("X2 SỨC MẠNH sau 5 giây!", NoticeKind.Warning);
            }
            if (!x2Active && now >= nextX2At) StartX2(s.x2Duration);
            if (x2Active && now >= x2Until) EndX2();

            // Thảm họa
            if (!disasterPending && now >= disasterReadyAt && FighterManager.I != null && FighterManager.I.Total > s.disasterThreshold)
                TriggerDisaster();
            if (disasterPending && now >= disasterAt) RunDisaster();
        }

        public void StartX2(float duration)
        {
            x2Active = true;
            x2Until = Time.time + duration;
            GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "x2", DisplayName = "X2 SỨC MẠNH", Duration = duration });
        }

        void EndX2()
        {
            x2Active = false;
            GameEvents.RaiseMatchEventEnded(new MatchEventNotice { Id = "x2", DisplayName = "X2 SỨC MẠNH" });
            ScheduleX2();
        }

        void EndWeak()
        {
            weakActive = false;
            GameEvents.RaiseMatchEventEnded(new MatchEventNotice { Id = "x2_weak", DisplayName = $"X2 CHO PHE {weakTeam.DisplayName()}", HasTeam = true, Team = weakTeam });
        }

        /// <summary>Cảnh báo 3 giây rồi giết cùng tỉ lệ lính hai phe.</summary>
        public void TriggerDisaster(int forceKind = -1)
        {
            if (disasterPending) return;
            disasterPending = true;
            disasterAt = Time.time + 3f;
            disasterKind = forceKind >= 0 && forceKind < DisasterNames.Length ? forceKind : Random.Range(0, DisasterNames.Length);
            disasterName = DisasterNames[disasterKind];
            GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "disaster_warning", DisplayName = $"CẢNH BÁO {disasterName}!", Duration = 3f });
            GameEvents.RaiseNotice($"CẢNH BÁO: {disasterName} sắp ập tới!", NoticeKind.Warning);
            Sfx.Warning();
        }

        void RunDisaster()
        {
            var s = GameSettings.Current;
            disasterPending = false;
            disasterReadyAt = Time.time + s.disasterCooldown;
            int blue = FighterManager.I.KillQuota(Team.Blue, s.disasterKillPercent, s.disasterMinPerTeam);
            int red = FighterManager.I.KillQuota(Team.Red, s.disasterKillPercent, s.disasterMinPerTeam);
            if (EffectManager.I != null) EffectManager.I.PlayDisaster(disasterKind, blue, red); // lính chết đúng chỗ thảm họa quét qua
            GameEvents.RaiseMatchEventEnded(new MatchEventNotice { Id = "disaster_warning", DisplayName = disasterName });
            GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "disaster", DisplayName = disasterName, Duration = 0f });
            GameEvents.RaiseNotice($"{disasterName}! Xanh mất {blue}, Đỏ mất {red} chiến binh", NoticeKind.Warning);
            Debug.Log($"[Match] Thảm họa {disasterName}: Xanh mất {blue}, Đỏ mất {red} (chết dần theo thảm họa trong 4,5 giây)");
        }

        public void ResetRound()
        {
            if (x2Active) EndX2();
            if (weakActive) EndWeak();
            weakUsed = false;
            disasterPending = false;
        }
    }
}
