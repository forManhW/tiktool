using UnityEngine;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// Tự động nạp prefab UI gốc từ Resources/UI/UIRoot sau khi scene Main được nạp
    /// và tắt giao diện tạm DebugHud (không sửa Main.unity).
    /// </summary>
    public static class UIBootstrap
    {
        public const string PrefabResourcePath = "UI/UIRoot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<UIRootController>() == null)
            {
                var prefab = Resources.Load<GameObject>(PrefabResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[UI] Không tìm thấy Resources/{PrefabResourcePath}. Giữ DebugHud hoạt động.");
                    return;
                }

                var instance = Object.Instantiate(prefab);
                instance.name = "UIRoot";
                Object.DontDestroyOnLoad(instance);
            }

            var debugHud = Object.FindFirstObjectByType<DebugHud>();
            if (debugHud != null)
            {
                debugHud.enabled = false;
            }
        }
    }
}
