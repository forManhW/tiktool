using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Hình học sân đấu. Camera nhìn theo trục +Z nên trên màn hình:
    /// Lõi Xanh (-X,-Z) ở góc dưới bên trái, Lõi Đỏ (+X,+Z) ở góc trên bên phải.
    /// </summary>
    public static class Arena
    {
        public const float HalfSize = 28f;      // sân 56 x 56 (đủ xa để 2 bên giao chiến giữa sân trước khi tới lâu đài)
        public const float CoreOffset = 22f;

        public static Vector3 CorePosition(Team team) =>
            team == Team.Blue ? new Vector3(-CoreOffset, 0f, -CoreOffset) : new Vector3(CoreOffset, 0f, CoreOffset);

        public static Vector3 Clamp(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, -HalfSize, HalfSize), p.y, Mathf.Clamp(p.z, -HalfSize, HalfSize));

        /// <summary>Điểm ngẫu nhiên trên nửa sân của một phe (nửa sân chia theo đường chéo x+z=0).</summary>
        public static Vector3 RandomPointOnHalf(Team team)
        {
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector3(Random.Range(-HalfSize, HalfSize), 0f, Random.Range(-HalfSize, HalfSize));
                bool blueHalf = p.x + p.z < 0f;
                if (blueHalf == (team == Team.Blue)) return p;
            }
            return CorePosition(team) * 0.5f;
        }
    }
}
