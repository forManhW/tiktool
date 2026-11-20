using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Chạy trước khi scene nạp: đọc settings, đặt cửa sổ vuông, chạy nền khi mất focus (đang live dùng OBS).</summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            GameSettings.Load();
            GameState.Init();
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
#if !UNITY_EDITOR
            // Cửa sổ vuông, nhưng không cao hơn màn hình (trừ thanh tiêu đề + taskbar), để màn 1080p vẫn thấy hết.
            int fit = Display.main.systemHeight > 0 ? Display.main.systemHeight - 120 : 1080;
            int size = Mathf.Clamp(Mathf.Min(GameSettings.Current.windowSize, fit), 480, 2160);
            Screen.SetResolution(size, size, FullScreenMode.Windowed);
#endif
        }
    }
}
