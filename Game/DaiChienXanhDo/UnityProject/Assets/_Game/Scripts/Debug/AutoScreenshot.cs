using System;
using System.Collections;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Công cụ test: chạy game với  --screenshot &lt;file.png&gt; [--screenshot-after &lt;giây&gt;]
    /// thì game tự chụp chính khung hình của nó (không chụp desktop). Không có tham số thì không làm gì.
    /// </summary>
    public class AutoScreenshot : MonoBehaviour
    {
        string path;
        float after = 8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            string file = null; float delay = 8f;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--screenshot") file = args[i + 1];
                if (args[i] == "--screenshot-after") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out delay);
            }
            if (string.IsNullOrEmpty(file)) return;
            var go = new GameObject("AutoScreenshot");
            DontDestroyOnLoad(go);
            var shot = go.AddComponent<AutoScreenshot>();
            shot.path = file; shot.after = delay;
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(after);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Screenshot] {path}");
        }
    }
}
