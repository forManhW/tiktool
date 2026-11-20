using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Sự kiện giữa trận do game tự chạy (cài trong settings.json, không phải TikTool):
    /// X2 Sức Mạnh định kỳ, X2 Phe Yếu khi một lõi dưới ngưỡng (1 lần/ván), Thảm họa khi sân quá đông.
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

            // X2 Phe Yếu
            if (!weakUsed && !weakActive && !x2Active)
            {
                foreach (Team t in new[] { Team.Blue, Team.Red })
                {
                    var core = match.Core(t);
                    if (core != null && core.Hp > 0f && core.Ratio < s.weakX2Threshold)
                    {
                        weakUsed = true; weakActive = true; weakTeam = t; weakUntil = now + s.weakX2Duration;
                        GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "x2_weak", DisplayName = $"X2 CHO PHE {t.DisplayName()}", Duration = s.weakX2Duration, HasTeam = true, Team = t });
                        GameEvents.RaiseNotice($"Phe {t.DisplayName()} được X2 sức mạnh {s.weakX2Duration:0} giây!", NoticeKind.Warning);
                        break;
                    }
                }
            }
            if (weakActive && now >= weakUntil) EndWeak();

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
        public void TriggerDisaster()
        {
            if (disasterPending) return;
            disasterPending = true;
            disasterAt = Time.time + 3f;
            disasterName = DisasterNames[Random.Range(0, DisasterNames.Length)];
            GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "disaster_warning", DisplayName = $"CẢNH BÁO {disasterName}!", Duration = 3f });
            GameEvents.RaiseNotice($"CẢNH BÁO: {disasterName} sắp ập tới!", NoticeKind.Warning);
            Sfx.Warning();
        }

        void RunDisaster()
        {
            var s = GameSettings.Current;
            disasterPending = false;
            disasterReadyAt = Time.time + s.disasterCooldown;
            int blue = FighterManager.I.KillPercent(Team.Blue, s.disasterKillPercent, s.disasterMinPerTeam);
            int red = FighterManager.I.KillPercent(Team.Red, s.disasterKillPercent, s.disasterMinPerTeam);
            if (CameraRig.I != null) CameraRig.I.Shake(1.5f, 2f);
            if (EffectManager.I != null) EffectManager.I.PlayDisasterVisual();
            GameEvents.RaiseMatchEventEnded(new MatchEventNotice { Id = "disaster_warning", DisplayName = disasterName });
            GameEvents.RaiseMatchEventStarted(new MatchEventNotice { Id = "disaster", DisplayName = disasterName, Duration = 0f });
            GameEvents.RaiseNotice($"{disasterName}! Xanh mất {blue}, Đỏ mất {red} chiến binh", NoticeKind.Warning);
            Debug.Log($"[Match] Thảm họa {disasterName}: Xanh mất {blue}, Đỏ mất {red}. Còn {FighterManager.I.Count(Team.Blue)}/{FighterManager.I.Count(Team.Red)}");
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
