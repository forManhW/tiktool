namespace DaiChienXanhDo
{
    /// <summary>Trạng thái chung: chế độ (Live tự động / Thường), ẩn hiện HUD, trạng thái kết nối TikTool.</summary>
    public static class GameState
    {
        public static GameMode Mode { get; private set; } = GameMode.LiveAuto;
        public static bool HudVisible { get; private set; } = true;
        public static string ConnectionStatus { get; private set; } = "demo";

        public static void Init() { Mode = GameSettings.Current.Mode; }

        public static void SetMode(GameMode mode)
        {
            Mode = mode;
            GameSettings.Current.mode = mode == GameMode.Manual ? "manual" : "live";
            GameSettings.Save();
            GameEvents.RaiseModeChanged(mode);
        }

        public static void ToggleMode() => SetMode(Mode == GameMode.LiveAuto ? GameMode.Manual : GameMode.LiveAuto);

        public static void ToggleHud()
        {
            HudVisible = !HudVisible;
            GameEvents.RaiseHudVisibilityChanged(HudVisible);
        }

        public static void SetConnectionStatus(string status)
        {
            if (ConnectionStatus == status) return;
            ConnectionStatus = status;
            GameEvents.RaiseConnectionStatusChanged(status);
        }
    }
}
