using System.Collections.Generic;
using System.Linq;

namespace DaiChienXanhDo
{
    /// <summary>Thông tin người xem đi kèm một lệnh.</summary>
    public struct ViewerInfo
    {
        public string Id;
        public string Nickname;
        public string AvatarUrl;
        public bool IsValid => !string.IsNullOrEmpty(Id);
    }

    /// <summary>Một người xem đã có nhân vật trong ván hiện tại.</summary>
    public sealed class Viewer
    {
        public string Id;
        public string Nickname;
        public string AvatarUrl;
        public Team Team;                 // phe của lệnh đầu tiên trong ván
        public Fighter Fighter;           // nhân vật chính, null khi đã chết
        public int RoundPoints;           // điểm đóng góp trong ván
        public float LastActionTime;
        public bool HasLiveFighter => Fighter != null && Fighter.IsAlive && Fighter.Owner == this && Fighter.IsMain;
    }

    /// <summary>Người xem ↔ nhân vật ↔ phe trong một ván. Reset mỗi ván.</summary>
    public static class PlayerRegistry
    {
        static readonly Dictionary<string, Viewer> viewers = new Dictionary<string, Viewer>();

        public static IReadOnlyCollection<Viewer> All => viewers.Values;

        public static bool TryGet(string id, out Viewer viewer) => viewers.TryGetValue(id ?? "", out viewer);

        /// <summary>Lấy người xem, tạo mới (gán phe) nếu lần đầu trong ván.</summary>
        public static Viewer GetOrCreate(ViewerInfo info, Team team, out bool isNew)
        {
            if (viewers.TryGetValue(info.Id, out var viewer))
            {
                isNew = false;
                if (!string.IsNullOrEmpty(info.Nickname)) viewer.Nickname = info.Nickname;
                return viewer;
            }
            viewer = new Viewer
            {
                Id = info.Id,
                Nickname = string.IsNullOrEmpty(info.Nickname) ? info.Id : info.Nickname,
                AvatarUrl = info.AvatarUrl,
                Team = team,
            };
            viewers[info.Id] = viewer;
            isNew = true;
            return viewer;
        }

        public static Viewer Mvp(Team team) =>
            viewers.Values.Where(v => v.Team == team && v.RoundPoints > 0).OrderByDescending(v => v.RoundPoints).FirstOrDefault();

        public static List<Viewer> Top(Team team, int count) =>
            viewers.Values.Where(v => v.Team == team && v.RoundPoints > 0).OrderByDescending(v => v.RoundPoints).Take(count).ToList();

        public static void Clear() => viewers.Clear();
    }
}
