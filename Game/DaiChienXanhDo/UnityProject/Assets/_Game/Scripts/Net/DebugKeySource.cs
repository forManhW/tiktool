using UnityEngine;
using UnityEngine.InputSystem;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Giả lập lệnh không cần TikTool:
    ///   F5 = người mới vào phe Xanh, F6 = người mới vào phe Đỏ,
    ///   F7 = quà phe Xanh (lần lượt bậc 1 → 7), F8 = quà phe Đỏ (lần lượt bậc 1 → 7),
    ///   F9 = 30 người vào ngẫu nhiên (thử tải). Giữ Shift + F7/F8 = Robot khổng lồ ngay.
    /// </summary>
    public class DebugKeySource : MonoBehaviour
    {
        static readonly string[] Ho = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô" };
        static readonly string[] Ten = { "An", "Bình", "Châu", "Dũng", "Giang", "Hà", "Khánh", "Linh", "Minh", "Ngọc", "Phúc", "Quân", "Sơn", "Thảo", "Uyên", "Vy", "Yến", "Tuấn", "Hùng", "Lan" };
        static readonly string[] Gifts = { "punch", "combo_punch", "superhero", "lightning", "tank", "meteor", "giant_robot" };

        readonly int[] giftIndex = new int[2];
        int counter;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb.f5Key.wasPressedThisFrame) Send("blue_join", NewViewer());
            if (kb.f6Key.wasPressedThisFrame) Send("red_join", NewViewer());
            if (kb.f7Key.wasPressedThisFrame) Gift(Team.Blue, shift);
            if (kb.f8Key.wasPressedThisFrame) Gift(Team.Red, shift);
            if (kb.f9Key.wasPressedThisFrame)
                for (int i = 0; i < 30; i++) Send(Random.value < 0.5f ? "blue_join" : "red_join", NewViewer());
        }

        void Gift(Team team, bool robot)
        {
            int i = (int)team;
            string effect = robot ? "giant_robot" : Gifts[giftIndex[i]++ % Gifts.Length];
            Send($"{team.Key()}_{effect}", ExistingOrNew(team));
        }

        ViewerInfo NewViewer()
        {
            counter++;
            return new ViewerInfo { Id = $"test-{counter}-{Random.Range(1000, 9999)}", Nickname = $"{Ho[Random.Range(0, Ho.Length)]} {Ten[Random.Range(0, Ten.Length)]}" };
        }

        ViewerInfo ExistingOrNew(Team team)
        {
            foreach (var v in PlayerRegistry.All)
                if (v.Team == team && Random.value < 0.3f) return new ViewerInfo { Id = v.Id, Nickname = v.Nickname };
            return NewViewer();
        }

        static void Send(string action, ViewerInfo user)
        {
            CommandRouter.Handle(new GameCommand
            {
                Id = System.Guid.NewGuid().ToString(),
                Action = action,
                Amount = 1,
                User = user,
                EventType = "test",
                Reply = (ok, error) => { if (!ok) Debug.LogWarning($"[Test] {action} thất bại: {error}"); },
            });
        }
    }
}
