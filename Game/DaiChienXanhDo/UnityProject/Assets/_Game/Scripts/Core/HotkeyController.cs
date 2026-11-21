using UnityEngine;
using UnityEngine.InputSystem;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Phím chung: F1 đổi chế độ Live tự động ↔ Thường, F2 ẩn/hiện giao diện, M bật/tắt nhạc nền, N bật/tắt âm hiệu ứng, F10 cài đặt,
    /// Ctrl+R reset ván (không tính điểm), Ctrl+Shift+R reset tỉ số hôm nay.
    /// </summary>
    public class HotkeyController : MonoBehaviour
    {
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb.f1Key.wasPressedThisFrame)
            {
                GameState.ToggleMode();
                Debug.Log($"[Game] Chế độ: {GameState.Mode}");
            }
            if (kb.f2Key.wasPressedThisFrame) GameState.ToggleHud();
            if (kb.mKey.wasPressedThisFrame && !ctrl)
            {
                GameSettings.Current.musicEnabled = !GameSettings.Current.musicEnabled;
                GameSettings.Save();
                GameEvents.RaiseNotice(GameSettings.Current.musicEnabled ? "Đã bật nhạc nền" : "Đã tắt nhạc nền", NoticeKind.Info);
            }
            if (kb.nKey.wasPressedThisFrame && !ctrl)
            {
                GameSettings.Current.sfxEnabled = !GameSettings.Current.sfxEnabled;
                GameSettings.Save();
                GameEvents.RaiseNotice(GameSettings.Current.sfxEnabled ? "Đã bật âm thanh hiệu ứng" : "Đã tắt âm thanh hiệu ứng", NoticeKind.Info);
            }
            if (kb.f10Key.wasPressedThisFrame && !GameEvents.RaiseSettingsMenuRequested())
            {
                GameSettings.Save();
                Application.OpenURL("file:///" + GameSettings.FilePath.Replace('\\', '/'));
            }
            if (ctrl && kb.rKey.wasPressedThisFrame && MatchManager.I != null)
            {
                if (shift) MatchManager.I.ResetScore(); else MatchManager.I.ResetRound();
            }
        }
    }
}
