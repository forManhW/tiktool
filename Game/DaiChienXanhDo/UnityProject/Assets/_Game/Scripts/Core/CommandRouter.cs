using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Một lệnh đã được TikTool (hoặc phím test) chuyển tới game.</summary>
    public sealed class GameCommand
    {
        public string Id;
        public string Action;            // vd. blue_punch
        public int Amount = 1;
        public ViewerInfo User;
        public string EventType;         // gift | like | comment | follow | share | test
        public string GiftName;
        public int GiftDiamonds;
        public Action<bool, string> Reply;   // trả kết quả cho nguồn lệnh (TikTool cần command_result)
    }

    public sealed class EffectDef
    {
        public string Id;
        public string DisplayName;
        public int Tier;
        public EffectDef(string id, string name, int tier) { Id = id; DisplayName = name; Tier = tier; }
    }

    /// <summary>
    /// Tên hàm → phe + hiệu ứng.
    ///   blue_&lt;hiệu ứng&gt; / red_&lt;hiệu ứng&gt;  : luôn cho phe đó (quà cố định phe)
    ///   ally_&lt;hiệu ứng&gt;                 : cho phe người đó đang ở; chưa tham gia thì bỏ qua
    ///   admin_reset_round, admin_reset_score, event_x2, event_disaster
    /// </summary>
    public static class CommandRouter
    {
        public static readonly Dictionary<string, EffectDef> Effects = new Dictionary<string, EffectDef>
        {
            { "join",        new EffectDef("join", "Vào phe", 0) },
            { "spawn",       new EffectDef("spawn", "Thêm lính", 0) },
            { "like_power",  new EffectDef("like_power", "Tim tiếp sức", 0) },
            { "punch",       new EffectDef("punch", "Đấm thường", 1) },
            { "combo_punch", new EffectDef("combo_punch", "Đấm liên hoàn", 2) },
            { "superhero",   new EffectDef("superhero", "Siêu Chiến Binh", 3) },
            { "lightning",   new EffectDef("lightning", "Sấm sét", 4) },
            { "tank",        new EffectDef("tank", "Xe tăng", 5) },
            { "meteor",      new EffectDef("meteor", "Mưa thiên thạch", 6) },
            { "giant_robot", new EffectDef("giant_robot", "Robot khổng lồ", 7) },
        };

        static readonly int[] TierPoints = { 0, 1, 5, 20, 100, 300, 1000, 5000 };
        static readonly Queue<GameCommand> pending = new Queue<GameCommand>();

        public static void Handle(GameCommand c)
        {
            (bool ok, string error) result;
            try { result = Route(c); }
            catch (Exception e) { Debug.LogException(e); result = (false, e.Message); }
            c.Reply?.Invoke(result.ok, result.error);
        }

        static (bool, string) Route(GameCommand c)
        {
            string a = (c.Action ?? "").Trim().ToLowerInvariant();
            if (a.StartsWith("admin_")) return Admin(a);
            if (a.StartsWith("event_")) return MatchEvent(a);
            if (a.StartsWith("debug_")) return DebugHooks.Handle(a);
            if (MatchManager.I == null) return (false, "Game chưa sẵn sàng");

            if (!TryParse(c, a, out Team team, out EffectDef def, out string error)) return (false, error);

            // Đang ăn mừng thắng ván: giữ lệnh lại, chạy khi ván mới bắt đầu (quà không bị mất).
            if (!MatchManager.I.IsFighting)
            {
                pending.Enqueue(c);
                while (pending.Count > 500) pending.Dequeue();
                return (true, null);
            }
            Run(c, team, def);
            return (true, null);
        }

        static bool TryParse(GameCommand c, string a, out Team team, out EffectDef def, out string error)
        {
            team = Team.Blue; def = null; error = null;
            string effect;
            if (a.StartsWith("blue_")) { team = Team.Blue; effect = a.Substring(5); }
            else if (a.StartsWith("red_")) { team = Team.Red; effect = a.Substring(4); }
            else if (a.StartsWith("ally_"))
            {
                effect = a.Substring(5);
                if (!c.User.IsValid || !PlayerRegistry.TryGet(c.User.Id, out var v)) { error = "Người xem chưa tham gia phe nào"; return false; }
                team = v.Team;
            }
            else { error = $"Lệnh không hỗ trợ: {c.Action}"; return false; }

            if (!Effects.TryGetValue(effect, out def)) { error = $"Hiệu ứng không tồn tại: {effect}"; return false; }
            return true;
        }

        static void Run(GameCommand c, Team team, EffectDef def)
        {
            if (!c.User.IsValid) c.User = new ViewerInfo { Id = "test-" + team.Key(), Nickname = "Khán giả " + team.DisplayName() };
            var viewer = PlayerRegistry.GetOrCreate(c.User, team, out bool isNew);
            viewer.LastActionTime = Time.time;

            bool ownTeam = viewer.Team == team;
            Fighter actor = null;
            if (ownTeam)
            {
                bool hadFighter = viewer.HasLiveFighter;
                actor = EnsureMainFighter(viewer);
                if (isNew && actor != null)
                {
                    GameEvents.RaisePlayerJoined(viewer);
                    GameEvents.RaiseNotice($"{viewer.Nickname} vào phe {team.DisplayName()}", NoticeKind.Join);
                }
                else if (!hadFighter && actor != null)
                {
                    GameEvents.RaiseNotice($"{viewer.Nickname} hồi sinh!", NoticeKind.Join);
                }
            }

            int amount = Mathf.Max(1, c.Amount);
            int points = c.GiftDiamonds > 0 ? c.GiftDiamonds : TierPoints[Mathf.Clamp(def.Tier, 0, TierPoints.Length - 1)];
            if (def.Tier > 0 || c.GiftDiamonds > 0)
            {
                viewer.RoundPoints += points;
                DailyStore.AddPoints(viewer, points);
            }

            if (def.Id != "join") EffectManager.I.Play(def, team, viewer, actor, amount);
        }

        static Fighter EnsureMainFighter(Viewer viewer)
        {
            if (viewer.HasLiveFighter) return viewer.Fighter;
            var f = FighterManager.I.Spawn(viewer.Team, viewer, true);
            viewer.Fighter = f;
            return f;
        }

        public static int PendingCount => pending.Count;

        public static void FlushPending()
        {
            int n = pending.Count;
            for (int i = 0; i < n && pending.Count > 0; i++)
            {
                var c = pending.Dequeue();
                c.Reply = null; // đã trả lời TikTool lúc nhận
                Handle(c);
            }
        }

        static (bool, string) Admin(string a)
        {
            var m = MatchManager.I;
            if (m == null) return (false, "Game chưa sẵn sàng");
            switch (a)
            {
                case "admin_reset_round": m.ResetRound(); return (true, null);
                case "admin_reset_score": m.ResetScore(); return (true, null);
                default: return (false, $"Lệnh quản trị không hỗ trợ: {a}");
            }
        }

        static (bool, string) MatchEvent(string a)
        {
            var ev = MatchEvents.I;
            if (ev == null) return (false, "Game chưa sẵn sàng");
            switch (a)
            {
                case "event_x2": ev.StartX2(GameSettings.Current.x2Duration); return (true, null);
                case "event_disaster": ev.TriggerDisaster(); return (true, null);
                default: return (false, $"Sự kiện không hỗ trợ: {a}");
            }
        }
    }
}
