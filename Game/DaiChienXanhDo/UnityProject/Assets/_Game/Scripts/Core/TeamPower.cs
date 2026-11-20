using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Hệ số sức mạnh của mỗi phe = tim tiếp sức × sự kiện X2.</summary>
    public static class TeamPower
    {
        static readonly float[] likeBonus = new float[2];
        static readonly float[] likeUntil = new float[2];
        const float LikeDuration = 5f;
        const float LikePerUnit = 0.05f;
        const float LikeMaxBonus = 1f;

        public static void AddLikePower(Team team, int amount)
        {
            int i = (int)team;
            if (Time.time > likeUntil[i]) likeBonus[i] = 0f;
            likeBonus[i] = Mathf.Min(LikeMaxBonus, likeBonus[i] + LikePerUnit * Mathf.Max(1, amount));
            likeUntil[i] = Time.time + LikeDuration;
        }

        public static float LikeMultiplier(Team team)
        {
            int i = (int)team;
            return Time.time <= likeUntil[i] ? 1f + likeBonus[i] : 1f;
        }

        public static float Of(Team team)
        {
            float events = MatchEvents.I != null ? MatchEvents.I.Multiplier(team) : 1f;
            return LikeMultiplier(team) * events;
        }

        public static void Reset()
        {
            for (int i = 0; i < 2; i++) { likeBonus[i] = 0f; likeUntil[i] = 0f; }
        }
    }
}
