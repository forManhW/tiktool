using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DaiChienXanhDo.EditorTools
{
    /// <summary>
    /// Build ra Game/DaiChienXanhDo/Build/DaiChienXanhDo.exe (đúng chỗ manifest.json trỏ tới).
    /// Menu: Dai Chien Xanh Do → Build Windows. Dòng lệnh: -executeMethod DaiChienXanhDo.EditorTools.BuildScript.BuildWindows
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Dai Chien Xanh Do/Build Windows (.exe)")]
        public static void BuildWindows()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Build"));
            Directory.CreateDirectory(outDir);
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(outDir, "DaiChienXanhDo.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Build] {s.result}: {options.locationPathName} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s, {s.totalErrors} lỗi)");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
