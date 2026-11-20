using UnityEngine;

namespace DaiChienXanhDo
{
    public enum Team { Blue = 0, Red = 1 }

    public enum GameMode { LiveAuto = 0, Manual = 1 }

    public static class TeamUtil
    {
        public static readonly Color BlueColor = new Color32(0x2D, 0x7D, 0xFF, 0xFF);
        public static readonly Color RedColor = new Color32(0xFF, 0x3B, 0x3B, 0xFF);

        public static Team Other(this Team team) => team == Team.Blue ? Team.Red : Team.Blue;
        public static Color TeamColor(this Team team) => team == Team.Blue ? BlueColor : RedColor;
        public static string DisplayName(this Team team) => team == Team.Blue ? "XANH" : "ĐỎ";
        public static string Key(this Team team) => team == Team.Blue ? "blue" : "red";
    }
}
