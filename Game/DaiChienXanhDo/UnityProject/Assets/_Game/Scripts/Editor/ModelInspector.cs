using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DaiChienXanhDo.EditorTools
{
    /// <summary>Liệt kê model trong Assets/_Game/Art: kiểu rig, hoạt ảnh, kích thước. Dòng lệnh: -executeMethod DaiChienXanhDo.EditorTools.ModelInspector.Dump</summary>
    public static class ModelInspector
    {
        [MenuItem("Dai Chien Xanh Do/Liệt kê model + hoạt ảnh")]
        public static void Dump()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Game/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => $"{c.name}({c.length:0.0}s)").ToArray();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var b = new Bounds();
                bool has = false;
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
                Debug.Log($"[Model] {path} | rig={importer?.animationType} | size={b.size.x:0.00}x{b.size.y:0.00}x{b.size.z:0.00} | clips={clips.Length}: {string.Join(", ", clips)}");
                var mats = go.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct()
                    .Select(m => $"{m.name}#{ColorUtility.ToHtmlStringRGB(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color)}{(m.HasProperty("_Metallic") ? $"/met{m.GetFloat("_Metallic"):0.0}" : "")}{(m.mainTexture != null ? "/tex" : "")}");
                Debug.Log($"[Mats] {path} | {string.Join(", ", mats)}");
                if (path.Contains("Vehicles"))
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        Debug.Log($"[Part] {path} | {r.name} | mats={string.Join("+", r.sharedMaterials.Select(m => m != null ? m.name : "-"))} | size={r.bounds.size.x:0.00}x{r.bounds.size.y:0.00}x{r.bounds.size.z:0.00}");
            }
        }
    }
}
