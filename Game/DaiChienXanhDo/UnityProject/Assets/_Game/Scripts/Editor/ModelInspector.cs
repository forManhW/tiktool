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
            }
        }
    }
}
