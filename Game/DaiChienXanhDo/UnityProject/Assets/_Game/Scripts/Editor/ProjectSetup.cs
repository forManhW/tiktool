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
            ModelSetup.Apply(a);
            EditorUtility.SetDirty(a);
            return a;
        }

        static void BuildScene(GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            BuildArena();

            MakeCore(Team.Blue, assets.blueMaterial);
            MakeCore(Team.Red, assets.redMaterial);

            var rig = new GameObject("CameraRig").AddComponent<CameraRig>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
            cam.fieldOfView = 40f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            rig.cam = cam;
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 37.7f, -26.4f), Quaternion.Euler(55f, 0f, 0f));
            MakePostFx(cam);

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
            core.radius = 4f;

            // Lâu đài (Kenney Castle Kit, CC0) quay mặt vào giữa sân: tháp chính + 2 tháp phụ, cờ màu phe.
            Vector3 toCenter = -root.transform.position; toCenter.y = 0f;
            var castle = new GameObject("Castle").transform;
            castle.SetParent(root.transform, false);
            castle.rotation = Quaternion.LookRotation(toCenter.normalized);

            Color main = TeamUtil.TeamColor(team);
            var stone = TintedCastleMat($"Castle_{team}_Stone", Color.Lerp(Color.white, main, 0.28f));
            var roof = TintedCastleMat($"Castle_{team}_Roof", Color.Lerp(main, Color.white, 0.1f));
            var glowMat = Mat($"Core_{team}_Crystal", main, emissive: true);

            float topY = Stack(castle, Vector3.zero, 4.2f, stone, roof,
                "tower-square-base-color", "tower-square-mid-windows", "tower-square-mid", "tower-square-top-roof-high");
            Stack(castle, new Vector3(-3.7f, 0f, -1.2f), 2.6f, stone, roof, "tower-hexagon-base", "tower-hexagon-mid", "tower-hexagon-roof");
            Stack(castle, new Vector3(3.7f, 0f, -1.2f), 2.6f, stone, roof, "tower-hexagon-base", "tower-hexagon-mid", "tower-hexagon-roof");

            var flagRenderers = new System.Collections.Generic.List<Renderer>();
            var flag = AssetDatabase.LoadAssetAtPath<GameObject>($"{Castle}/flag.fbx");
            if (flag != null)
            {
                var f = Place(flag, castle, castle.TransformPoint(new Vector3(0f, 0f, 0f)) + Vector3.up * (topY - 0.2f), castle.eulerAngles.y, height: 2.4f);
                foreach (var r in f.GetComponentsInChildren<Renderer>())
                {
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = glowMat;
                    r.sharedMaterials = arr;
                    flagRenderers.Add(r);
                }
            }

            // Khối va chạm để click / raycast
            var hit = root.AddComponent<CapsuleCollider>();
            hit.center = new Vector3(0f, 3f, 0f); hit.height = 6f; hit.radius = 2.5f;

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = main;
            light.range = 16f;
            light.intensity = 4f;
            light.shadows = LightShadows.None;

            var visual = root.AddComponent<CoreVisual>();
            visual.glow = light;
            visual.glowRenderers = flagRenderers.ToArray();
        }

        /// <summary>Chồng các khối tháp lên nhau, cùng một tỉ lệ (theo chiều rộng khối đáy). Khối cuối (mái) tô màu phe. Trả về độ cao đỉnh.</summary>
        static float Stack(Transform parent, Vector3 localPos, float width, Material stone, Material roof, params string[] pieces)
        {
            float y = 0f, scale = -1f;
            Vector3 world = parent.TransformPoint(localPos);
            for (int i = 0; i < pieces.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Castle}/{pieces[i]}.fbx");
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(parent, false);
                go.transform.rotation = parent.rotation * go.transform.rotation;
                var b = WorldBounds(go);
                if (scale < 0f) scale = width / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.z));
                go.transform.localScale *= scale;
                b = WorldBounds(go);
                go.transform.position += new Vector3(world.x - b.center.x, y - b.min.y, world.z - b.center.z);
                y += b.size.y;
                bool isRoof = i == pieces.Length - 1;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int k = 0; k < arr.Length; k++) arr[k] = isRoof ? roof : stone;
                    r.sharedMaterials = arr;
                }
                foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            return y;
        }

        /// <summary>Vật liệu lâu đài: ảnh màu gốc của Kenney (colormap) nhân với màu phe, không kim loại.</summary>
        static Material TintedCastleMat(string name, Color tint)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Castle}/Textures/colormap.png");
            if (map != null) m.SetTexture("_BaseMap", map);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.1f);
            SaveMat(m, name);
            return m;
        }

        // ---------------- Sân đấu ----------------

        const string Castle = "Assets/_Game/Art/Environment/Kenney_CastleKit";
        const string Nature = "Assets/_Game/Art/Environment/Kenney_NatureKit";

        /// <summary>Trời + sương mù + mặt cỏ rộng 200x200 + hàng rào quanh sân + cây đá bên ngoài.</summary>
        static void BuildArena()
        {
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.42f, 0.62f, 0.95f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.5f, 0.36f));
            sky.SetFloat("_Exposure", 1.2f);
            SaveMat(sky, "Sky");
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.6f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.33f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.77f, 0.9f);
            RenderSettings.fogStartDistance = 95f;
            RenderSettings.fogEndDistance = 210f;

            // Mặt cỏ thật (ambientCG Grass004, CC0), lát lặp
            const string tex = "Assets/_Game/Art/Textures/Grass004";
            var normalImp = AssetImporter.GetAtPath($"{tex}/Grass004_1K-JPG_NormalGL.jpg") as TextureImporter;
            if (normalImp != null && normalImp.textureType != TextureImporterType.NormalMap) { normalImp.textureType = TextureImporterType.NormalMap; normalImp.SaveAndReimport(); }
            var grass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/Grass004_1K-JPG_Color.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/Grass004_1K-JPG_NormalGL.jpg");
            if (color != null) { grass.SetTexture("_BaseMap", color); grass.SetTextureScale("_BaseMap", new Vector2(70f, 70f)); }
            if (normal != null) { grass.SetTexture("_BumpMap", normal); grass.EnableKeyword("_NORMALMAP"); grass.SetFloat("_BumpScale", 0.6f); }
            grass.SetColor("_BaseColor", new Color(0.92f, 1f, 0.85f));
            grass.SetFloat("_Smoothness", 0.05f);
            SaveMat(grass, "Ground_Grass");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().sharedMaterial = grass;

            // Vạch giữa sân (mờ, như vôi kẻ)
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "FrontLine";
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.transform.position = new Vector3(0f, 0.01f, 0f);
            line.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            line.transform.localScale = new Vector3(Arena.HalfSize * 2.7f, 0.02f, 0.18f);
            line.GetComponent<Renderer>().sharedMaterial = Mat("FrontLine", new Color(0.95f, 0.93f, 0.85f));

            var deco = new GameObject("Decor").transform;

            // Hàng rào gỗ quanh sân
            float edge = Arena.HalfSize + 1.6f;
            var fence = AssetDatabase.LoadAssetAtPath<GameObject>($"{Castle}/wall-narrow-wood-fence.fbx");
            if (fence != null)
            {
                const float seg = 3f;
                int n = Mathf.CeilToInt(edge * 2f / seg);
                for (int side = 0; side < 4; side++)
                    for (int i = 0; i < n; i++)
                    {
                        float along = -edge + seg * (i + 0.5f);
                        Vector3 p; float yaw;
                        switch (side)
                        {
                            case 0: p = new Vector3(along, 0f, -edge); yaw = 0f; break;
                            case 1: p = new Vector3(along, 0f, edge); yaw = 0f; break;
                            case 2: p = new Vector3(-edge, 0f, along); yaw = 90f; break;
                            default: p = new Vector3(edge, 0f, along); yaw = 90f; break;
                        }
                        var fp = Place(fence, deco, p, yaw, width: seg);
                        var fb = WorldBounds(fp);
                        if (fb.size.y > 1.1f) { var s0 = fp.transform.localScale; fp.transform.localScale = new Vector3(s0.x, s0.y * 1.1f / fb.size.y, s0.z); } // rào thấp ngang hông, không che sân
                    }
            }

            // Cây, đá bên ngoài sân (cố định để lần nào dựng cũng giống nhau)
            string[] trees = { $"{Nature}/tree_oak.fbx", $"{Nature}/tree_default.fbx", $"{Nature}/tree_detailed.fbx", $"{Nature}/tree_pineTallA.fbx", $"{Nature}/tree_pineDefaultA.fbx", $"{Castle}/tree-large.fbx" };
            string[] rocks = { $"{Nature}/rock_largeA.fbx", $"{Nature}/rock_largeB.fbx", $"{Nature}/rock_largeC.fbx", $"{Nature}/rock_tallA.fbx", $"{Nature}/plant_bushLarge.fbx" };
            var rng = new System.Random(7);
            int placed = 0;
            for (int i = 0; i < 600 && placed < 200; i++)
            {
                float x = (float)(rng.NextDouble() * 170 - 85), z = (float)(rng.NextDouble() * 170 - 85);
                float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                if (d < edge + 2.5f) continue;                       // không đặt trong sân
                bool tree = rng.NextDouble() < 0.7;
                var path = tree ? trees[rng.Next(trees.Length)] : rocks[rng.Next(rocks.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                float h = tree ? (float)(5 + rng.NextDouble() * 4) : (float)(1.2 + rng.NextDouble() * 2);
                Place(prefab, deco, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360), height: h);
                placed++;
            }
        }

        /// <summary>Đặt model, tự co giãn theo chiều cao hoặc chiều rộng mong muốn, đáy chạm đất.</summary>
        static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, float height = 0f, float width = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;
            var b = WorldBounds(go);
            float s = 1f;
            if (height > 0f && b.size.y > 0.001f) s = height / b.size.y;
            else if (width > 0f) { float w = Mathf.Max(b.size.x, b.size.z); if (w > 0.001f) s = width / w; }
            go.transform.localScale *= s;
            b = WorldBounds(go);
            go.transform.position += pos - new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        static Bounds WorldBounds(GameObject go)
        {
            bool has = false; Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            return b;
        }

        static void SaveMat(Material m, string name)
        {
            string path = $"{Root}/Materials/{name}.mat";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
        }

        static GameObject Part(GameObject root, string name, PrimitiveType type, Vector3 scale, float y, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localScale = scale;
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Hậu kỳ: bloom cho ánh sáng tỏa, tăng độ đậm màu, viền tối nhẹ.</summary>
        static void MakePostFx(Camera cam)
        {
            const string path = Root + "/Settings/PostFX.asset";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f); bloom.threshold.Override(0.95f); bloom.scatter.Override(0.7f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(22f); color.contrast.Override(10f); color.postExposure.Override(0.15f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
    }
}
