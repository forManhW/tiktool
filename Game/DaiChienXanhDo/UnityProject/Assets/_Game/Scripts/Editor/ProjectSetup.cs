using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaiChienXanhDo.EditorTools
{
    /// <summary>
    /// Dựng lại project từ đầu bằng code: URP, vật liệu, GameAssets, scene Main (greybox).
    /// Menu: Dai Chien Xanh Do → Setup project. Dòng lệnh: -executeMethod DaiChienXanhDo.EditorTools.ProjectSetup.Run
    /// CẢNH BÁO: tạo lại scene Main.unity, mọi chỉnh tay trong scene sẽ mất. Vật liệu và GameAssets có sẵn được giữ.
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Dai Chien Xanh Do/Setup project (tạo lại scene Main)")]
        public static void Run()
        {
            foreach (var dir in new[] { "Settings", "Materials", "Resources", "Scenes", "Prefabs", "Art", "Audio", "VFX" })
                Directory.CreateDirectory(Path.Combine(Root, dir));
            AssetDatabase.Refresh();

            EnsureUrp();
            var assets = EnsureGameAssets();
            BuildScene(assets);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.visibleInBackground = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Xong. Mở Assets/_Game/Scenes/Main.unity rồi bấm Play.");
        }

        static void EnsureUrp()
        {
            const string assetPath = Root + "/Settings/URP_Asset.asset";
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (rp == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Root + "/Settings/URP_Renderer.asset");
                rp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(rp, assetPath);
            }
            rp.msaaSampleCount = 4;
            rp.shadowDistance = 90f;
            EditorUtility.SetDirty(rp);
            GraphicsSettings.defaultRenderPipeline = rp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = rp;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static Material Mat(string name, Color color, bool emissive = false, bool unlit = false)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            if (!unlit) m.SetFloat("_Smoothness", 0.25f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2.5f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameAssets EnsureGameAssets()
        {
            const string path = Root + "/Resources/GameAssets.asset";
            var a = AssetDatabase.LoadAssetAtPath<GameAssets>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(a, path);
            }
            if (a.blueMaterial == null) a.blueMaterial = Mat("Team_Blue", TeamUtil.BlueColor);
            if (a.redMaterial == null) a.redMaterial = Mat("Team_Red", TeamUtil.RedColor);
            if (a.superMaterial == null) a.superMaterial = Mat("Super_Gold", new Color(1f, 0.8f, 0.2f), emissive: true);
            if (a.boltMaterial == null) a.boltMaterial = Mat("Bolt", new Color(0.75f, 0.9f, 1f), unlit: true);
            if (a.meteorMaterial == null) a.meteorMaterial = Mat("Meteor", new Color(1f, 0.45f, 0.1f), emissive: true);
            if (a.groundMaterial == null) a.groundMaterial = Mat("Ground", new Color(0.36f, 0.52f, 0.34f));
            EditorUtility.SetDirty(a);
            return a;
        }

        static void BuildScene(GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(Arena.HalfSize * 0.2f + 0.2f, 1f, Arena.HalfSize * 0.2f + 0.2f);
            ground.GetComponent<Renderer>().sharedMaterial = assets.groundMaterial;

            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "FrontLine";
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.transform.position = new Vector3(0f, 0.01f, 0f);
            line.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            line.transform.localScale = new Vector3(Arena.HalfSize * 2.6f, 0.02f, 0.25f);
            line.GetComponent<Renderer>().sharedMaterial = Mat("FrontLine", new Color(0.9f, 0.9f, 0.85f));

            MakeCore(Team.Blue, assets.blueMaterial);
            MakeCore(Team.Red, assets.redMaterial);

            var rig = new GameObject("CameraRig").AddComponent<CameraRig>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
            cam.fieldOfView = 40f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            rig.cam = cam;
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 37.7f, -26.4f), Quaternion.Euler(55f, 0f, 0f));

            // Camera nền tô đen phần viền khi cửa sổ không vuông.
            var bgGo = new GameObject("LetterboxCamera");
            var bg = bgGo.AddComponent<Camera>();
            bg.depth = -100;
            bg.clearFlags = CameraClearFlags.SolidColor;
            bg.backgroundColor = Color.black;
            bg.cullingMask = 0;
            cam.depth = 0;

            var game = new GameObject("Game");
            game.AddComponent<MatchManager>();
            game.AddComponent<FighterManager>();
            game.AddComponent<EffectManager>();
            game.AddComponent<MatchEvents>();
            game.AddComponent<CameraDirector>();
            game.AddComponent<HotkeyController>();
            game.AddComponent<TikToolSource>();
            game.AddComponent<DebugKeySource>();
            game.AddComponent<DebugHud>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void MakeCore(Team team, Material mat)
        {
            var root = new GameObject($"Core_{team}");
            root.transform.position = Arena.CorePosition(team);
            var core = root.AddComponent<CoreHealth>();
            core.team = team;
            core.radius = 1.8f;

            var baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCyl.name = "Base";
            baseCyl.transform.SetParent(root.transform, false);
            baseCyl.transform.localScale = new Vector3(3.4f, 1.2f, 3.4f);
            baseCyl.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            baseCyl.GetComponent<Renderer>().sharedMaterial = mat;

            var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "Gem";
            Object.DestroyImmediate(gem.GetComponent<Collider>());
            gem.transform.SetParent(root.transform, false);
            gem.transform.localScale = Vector3.one * 1.8f;
            gem.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            gem.GetComponent<Renderer>().sharedMaterial = Mat($"Core_{team}_Glow", team.TeamColor(), emissive: true);
        }
    }
}
