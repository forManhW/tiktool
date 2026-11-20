using UnityEngine;
using UnityEngine.InputSystem;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Phím chung: F1 đổi chế độ Live tự động ↔ Thường, F2 ẩn/hiện giao diện, F10 mở settings.json,
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
