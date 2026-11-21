using System.Collections.Generic;
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
    /// Dựng lại project từ đầu bằng code: URP, vật liệu, GameAssets, scene Main.
    /// Menu: Dai Chien Xanh Do → Setup project. Dòng lệnh: -executeMethod DaiChienXanhDo.EditorTools.ProjectSetup.Run
    /// CẢNH BÁO: tạo lại scene Main.unity, mọi chỉnh tay trong scene sẽ mất. Vật liệu và GameAssets có sẵn được giữ.
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/Main.unity";
        const string Castle = "Assets/_Game/Art/Environment/Kenney_CastleKit";
        const string Nature = "Assets/_Game/Art/Environment/Kenney_NatureKit";
        const string Space = "Assets/_Game/Art/Environment/Kenney_SpaceKit";
        const string Dirt = "Assets/_Game/Art/Textures/Ground054";
        const string Generated = "Assets/_Game/Art/Generated";

        sealed class ArenaPalette
        {
            public Material StoneWall;
            public Material StoneLight;
            public Material DarkSlate;
            public Material SlateRock;
            public Material GoldTrim;
            public Material GoldBright;
            public Material GoldCrystal;
            public Material BrazierIron;
            public Material BrazierFlame;
            public Material CampWood;
            public Material PlazaStone;
            public Material BlueBanner;
            public Material RedBanner;
            public Material BlueRoof;
            public Material RedRoof;
            public Material BlueBorder;
            public Material RedBorder;
            public Material VertexTintMat;

            public Mesh PyramidCap;
            public Mesh CrystalSpire;
            public Mesh HeraldicShield;
            public Mesh OctagonRing;
            public Mesh BrazierBowl;
            public Mesh UnitBox;
        }

        [MenuItem("Dai Chien Xanh Do/Setup project (tạo lại scene Main)")]
        public static void Run()
        {
            foreach (var dir in new[] { "Settings", "Materials", "Resources", "Scenes", "Prefabs", "Art", "Audio", "VFX" })
                Directory.CreateDirectory(Path.Combine(Root, dir));
            Directory.CreateDirectory(Generated);
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
            rp.shadowDistance = 95f;
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
            a.tankYawOffset = 0f;
            EditorUtility.SetDirty(a);
            return a;
        }

        static ArenaPalette BuildPalette()
        {
            Directory.CreateDirectory(Generated);
            var p = new ArenaPalette
            {
                StoneWall = TintedCastleMat("Arena_StoneWall", new Color(0.95f, 0.93f, 0.90f), smoothness: 0.14f),
                StoneLight = CreateLitMat("Arena_StoneLight", new Color(0.89f, 0.87f, 0.84f), metallic: 0.0f, smoothness: 0.16f),
                DarkSlate = CreateLitMat("Arena_DarkSlate", new Color(0.24f, 0.27f, 0.35f), metallic: 0.08f, smoothness: 0.22f),
                SlateRock = CreateLitMat("Arena_SlateRock", new Color(0.46f, 0.50f, 0.59f), metallic: 0.02f, smoothness: 0.16f),
                GoldTrim = CreateLitMat("Arena_GoldTrim", new Color(0.91f, 0.72f, 0.29f), metallic: 0.55f, smoothness: 0.62f, emission: new Color(0.91f, 0.72f, 0.29f) * 0.14f),
                GoldBright = CreateLitMat("Arena_GoldBright", new Color(1.0f, 0.82f, 0.32f), metallic: 0.45f, smoothness: 0.72f, emission: new Color(1.0f, 0.78f, 0.24f) * 0.38f),
                GoldCrystal = CreateLitMat("Arena_GoldCrystal", new Color(1.0f, 0.83f, 0.26f), metallic: 0.15f, smoothness: 0.86f, emission: new Color(1.0f, 0.74f, 0.14f) * 1.15f),
                BrazierIron = CreateLitMat("Arena_BrazierIron", new Color(0.18f, 0.20f, 0.25f), metallic: 0.65f, smoothness: 0.45f),
                BrazierFlame = CreateLitMat("Arena_BrazierFlame", new Color(1.0f, 0.68f, 0.20f), metallic: 0.0f, smoothness: 0.5f, emission: new Color(1.0f, 0.52f, 0.08f) * 2.1f),
                CampWood = TintedCastleMat("Castle_Wood", new Color(0.98f, 0.92f, 0.84f), smoothness: 0.12f),
                PlazaStone = CreateLitMat("Arena_PlazaStone", new Color(0.86f, 0.84f, 0.80f), metallic: 0.0f, smoothness: 0.12f),
                BlueBanner = CreateLitMat("Banner_Blue", new Color(0.16f, 0.48f, 0.98f), metallic: 0.0f, smoothness: 0.22f, emission: new Color(0.16f, 0.48f, 0.98f) * 0.12f),
                RedBanner = CreateLitMat("Banner_Red", new Color(0.95f, 0.20f, 0.22f), metallic: 0.0f, smoothness: 0.22f, emission: new Color(0.95f, 0.20f, 0.22f) * 0.12f),
                BlueRoof = TintedCastleMat("Castle_Blue_Roof", new Color(0.24f, 0.58f, 1.0f), smoothness: 0.22f),
                RedRoof = TintedCastleMat("Castle_Red_Roof", new Color(1.0f, 0.28f, 0.28f), smoothness: 0.22f),
                BlueBorder = CreateLitMat("Arena_BlueBorder", new Color(0.20f, 0.56f, 1.0f), metallic: 0.05f, smoothness: 0.35f, emission: new Color(0.18f, 0.52f, 1.0f) * 0.48f),
                RedBorder = CreateLitMat("Arena_RedBorder", new Color(1.0f, 0.26f, 0.26f), metallic: 0.05f, smoothness: 0.35f, emission: new Color(1.0f, 0.22f, 0.22f) * 0.48f),
                VertexTintMat = CreateVertexTintMat("Arena_TerritoryTint"),

                PyramidCap = SaveMeshAsset(CreatePyramidCapMesh(), "Mesh_PyramidCap"),
                CrystalSpire = SaveMeshAsset(CreateCrystalSpireMesh(), "Mesh_CrystalSpire"),
                HeraldicShield = SaveMeshAsset(CreateHeraldicShieldMesh(), "Mesh_HeraldicShield"),
                OctagonRing = SaveMeshAsset(CreateOctagonRingMesh(0.76f, 1.0f, 0.32f), "Mesh_OctagonRing"),
                BrazierBowl = SaveMeshAsset(CreateBrazierBowlMesh(), "Mesh_BrazierBowl"),
                UnitBox = SaveMeshAsset(CreateUnitBoxMesh(), "Mesh_UnitBox"),
            };
            return p;
        }

        static void BuildScene(GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var pal = BuildPalette();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.42f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            BuildArena(pal);

            MakeCore(Team.Blue, assets.blueMaterial, pal);
            MakeCore(Team.Red, assets.redMaterial, pal);

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

        // ---------------- Lâu đài / Lõi ----------------

        static void MakeCore(Team team, Material mat, ArenaPalette pal)
        {
            var root = new GameObject($"Core_{team}");
            root.transform.position = Arena.CorePosition(team);
            var core = root.AddComponent<CoreHealth>();
            core.team = team;
            core.radius = 4f;

            // Lâu đài hướng thẳng về trung tâm sân đấu
            Vector3 toCenter = -root.transform.position; toCenter.y = 0f;
            var castle = new GameObject("Castle").transform;
            castle.SetParent(root.transform, false);
            castle.rotation = Quaternion.LookRotation(toCenter.normalized);

            Color main = TeamUtil.TeamColor(team);
            var stone = TintedCastleMat($"Castle_{team}_Stone", Color.Lerp(new Color(0.96f, 0.95f, 0.93f), main, 0.18f), smoothness: 0.14f);
            var roof = team == Team.Blue ? pal.BlueRoof : pal.RedRoof;
            var bannerMat = team == Team.Blue ? pal.BlueBanner : pal.RedBanner;
            var glowMat = CreateLitMat(
                $"Core_{team}_Crystal",
                Color.Lerp(main, Color.white, 0.16f),
                metallic: 0.08f,
                smoothness: 0.86f,
                emission: main * 1.1f);

            // Bệ đá bát giác nâng lâu đài + viền vàng hoàng gia
            var plinthMesh = SaveMeshAsset(CreateOctagonPrismMesh(4.45f, 0.52f), $"Mesh_CastlePlinth_{team}");
            PlaceLocalMesh("PlinthStone", plinthMesh, pal.StoneLight, castle, Vector3.zero, Quaternion.identity, Vector3.one);
            PlaceLocalMesh("PlinthGoldRim", pal.OctagonRing, pal.GoldTrim, castle, new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(4.58f, 0.48f, 4.58f));
            PlaceLocalMesh("PlinthSlateBase", pal.OctagonRing, pal.DarkSlate, castle, new Vector3(0f, 0.0f, 0f), Quaternion.identity, new Vector3(4.78f, 0.58f, 4.78f));

            // Bậc thang mặt trước + cổng vòm dẫn thẳng ra đường chiến đấu
            var stairsPrefab = CastlePiece("stairs-stone-square") ?? CastlePiece("stairs-stone");
            if (stairsPrefab != null)
            {
                var st = Place(stairsPrefab, castle, castle.TransformPoint(new Vector3(0f, 0f, 3.65f)), castle.eulerAngles.y, width: 2.8f);
                FitHeight(st, 0.62f);
                SetAllMats(st, pal.StoneLight);
            }
            PlaceLocalMesh("StairRailL", pal.UnitBox, pal.GoldTrim, castle, new Vector3(-1.42f, 0.30f, 3.55f), Quaternion.Euler(14f, 0f, 0f), new Vector3(0.24f, 0.24f, 2.25f));
            PlaceLocalMesh("StairRailR", pal.UnitBox, pal.GoldTrim, castle, new Vector3(1.42f, 0.30f, 3.55f), Quaternion.Euler(14f, 0f, 0f), new Vector3(0.24f, 0.24f, 2.25f));

            var gatePrefab = CastlePiece("gate") ?? CastlePiece("door");
            if (gatePrefab != null)
            {
                var g = Place(gatePrefab, castle, castle.TransformPoint(new Vector3(0f, 0.45f, 2.02f)), castle.eulerAngles.y, height: 2.3f);
                SetAllMats(g, pal.DarkSlate);
            }
            PlaceLocalMesh("GateGoldArch", pal.UnitBox, pal.GoldTrim, castle, new Vector3(0f, 2.55f, 2.18f), Quaternion.identity, new Vector3(2.35f, 0.26f, 0.32f));

            // Tháp chính ở giữa + vương miện vàng bao quanh đỉnh
            float topY = Stack(castle, new Vector3(0f, 0.42f, 0f), 4.05f, stone, stone,
                "tower-square-base-color", "tower-square-mid-windows", "tower-square-top-color");
            PlaceLocalMesh("KeepGoldCollar", pal.OctagonRing, pal.GoldBright, castle, new Vector3(0f, topY - 0.14f, 0f), Quaternion.identity, new Vector3(2.45f, 1.35f, 2.45f));
            PlaceLocalMesh("KeepSlateRim", pal.OctagonRing, pal.DarkSlate, castle, new Vector3(0f, topY - 0.38f, 0f), Quaternion.identity, new Vector3(2.58f, 0.75f, 2.58f));

            // Viên pha lê khổng lồ đa diện xoay & phát sáng trên đỉnh tháp chính (tương thích CoreVisual)
            var glowRenderers = new List<Renderer>();
            var crystalPivot = new GameObject("CrystalCrown").transform;
            crystalPivot.SetParent(castle, false);
            crystalPivot.localPosition = new Vector3(0f, topY + 0.92f, 0f);

            var mainCrystal = PlaceLocalMesh("MainCrystal", pal.CrystalSpire, glowMat, crystalPivot, Vector3.zero, Quaternion.identity, new Vector3(1.15f, 1.12f, 1.15f));
            glowRenderers.Add(mainCrystal.GetComponent<Renderer>());

            for (int k = 0; k < 4; k++)
            {
                float ang = k * 90f + 45f;
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                var shard = PlaceLocalMesh(
                    $"CrystalShard_{k}",
                    pal.CrystalSpire,
                    glowMat,
                    crystalPivot,
                    dir * 0.92f + Vector3.down * 0.42f,
                    Quaternion.Euler(dir.z * 22f, ang, -dir.x * 22f),
                    new Vector3(0.42f, 0.56f, 0.42f));
                glowRenderers.Add(shard.GetComponent<Renderer>());
            }

            // 2 tháp lục giác 2 bên có mái màu phe + chóp vàng + băng rôn dài
            var bannerLong = CastlePiece("flag-banner-long");
            var pennant = CastlePiece("flag-pennant");
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 tLocal = new Vector3(side * 3.55f, 0.35f, -0.95f);
                float sideTop = Stack(castle, tLocal, 2.45f, stone, roof, "tower-hexagon-base", "tower-hexagon-mid", "tower-hexagon-roof");
                PlaceLocalMesh($"SideTowerCap_{(side < 0 ? "L" : "R")}", pal.PyramidCap, pal.GoldBright, castle,
                    new Vector3(tLocal.x, sideTop - 0.12f, tLocal.z), Quaternion.identity, new Vector3(0.92f, 1.15f, 0.92f));
                PlaceLocalMesh($"SideTowerRing_{(side < 0 ? "L" : "R")}", pal.OctagonRing, pal.GoldTrim, castle,
                    new Vector3(tLocal.x, sideTop - 1.45f, tLocal.z), Quaternion.identity, new Vector3(1.32f, 0.65f, 1.32f));

                if (bannerLong != null)
                {
                    var b = Place(bannerLong, castle, castle.TransformPoint(new Vector3(tLocal.x, 1.25f, tLocal.z + 1.25f)), castle.eulerAngles.y, height: 2.35f);
                    SetAllMats(b, bannerMat);
                }

                // 2 trụ canh nhỏ phía trước 2 bên bậc thang (tạo dáng tứ trụ vững chãi)
                Vector3 fLocal = new Vector3(side * 2.55f, 0.35f, 2.05f);
                float frontTop = Stack(castle, fLocal, 1.55f, stone, stone, "tower-square-base", "tower-square-top");
                PlaceLocalMesh($"FrontTurretCollar_{(side < 0 ? "L" : "R")}", pal.OctagonRing, pal.GoldTrim, castle,
                    new Vector3(fLocal.x, frontTop - 0.14f, fLocal.z), Quaternion.identity, new Vector3(0.95f, 0.55f, 0.95f));
                PlaceLocalMesh($"FrontTurretCap_{(side < 0 ? "L" : "R")}", pal.PyramidCap, pal.GoldBright, castle,
                    new Vector3(fLocal.x, frontTop, fLocal.z), Quaternion.identity, new Vector3(1.18f, 1.15f, 1.18f));
                if (pennant != null)
                {
                    var pFlag = Place(pennant, castle, castle.TransformPoint(new Vector3(fLocal.x, 0.7f, fLocal.z + 0.82f)), castle.eulerAngles.y, height: 1.6f);
                    SetAllMats(pFlag, bannerMat);
                }
            }

            // Khiên biểu tượng phe gắn mặt trước tháp chính
            PlaceShieldEmblem(castle, new Vector3(0f, 3.85f, 2.08f), Quaternion.identity, 1.35f, bannerMat, pal.GoldBright, pal);

            // Khối va chạm giữ nguyên để click / raycast & UI thanh máu lõi hoạt động chính xác
            var hit = root.AddComponent<CapsuleCollider>();
            hit.center = new Vector3(0f, 3f, 0f); hit.height = 6f; hit.radius = 2.5f;

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, topY + 1.6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = main;
            light.range = 18f;
            light.intensity = 4.2f;
            light.shadows = LightShadows.None;

            var visual = root.AddComponent<CoreVisual>();
            visual.crystal = crystalPivot;
            visual.glow = light;
            visual.glowRenderers = glowRenderers.ToArray();
        }

        /// <summary>Chồng các khối tháp lên nhau, cùng một tỉ lệ (theo chiều rộng khối đáy). Khối cuối (mái) tô màu roof. Trả về độ cao đỉnh.</summary>
        static float Stack(Transform parent, Vector3 localPos, float width, Material stone, Material roof, params string[] pieces)
        {
            Vector3 world = parent.TransformPoint(localPos);
            float y = world.y, scale = -1f;
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
            return y - parent.position.y;
        }

        /// <summary>Vật liệu lâu đài: ảnh màu gốc của Kenney (colormap) nhân với màu tint.</summary>
        static Material TintedCastleMat(string name, Color tint, float smoothness = 0.12f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Castle}/Textures/colormap.png");
            if (map != null) m.SetTexture("_BaseMap", map);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            SaveMat(m, name);
            return m;
        }

        static Material CreateLitMat(string name, Color color, float metallic = 0f, float smoothness = 0.2f, Color? emission = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            SaveMat(m, name);
            return m;
        }

        static Material CreateVertexTintMat(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 2f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            SaveMat(m, name);
            return m;
        }

        // ---------------- Sân đấu ----------------

        /// <summary>Trời + sương mù + mặt cỏ + vùng lãnh thổ + tường thành đấu trường + trại vây thành + tinh thể góc + rừng ngoài.</summary>
        static void BuildArena(ArenaPalette pal)
        {
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.038f);
            sky.SetFloat("_AtmosphereThickness", 0.82f);
            sky.SetColor("_SkyTint", new Color(0.40f, 0.63f, 0.96f));
            sky.SetColor("_GroundColor", new Color(0.38f, 0.48f, 0.34f));
            sky.SetFloat("_Exposure", 1.18f);
            SaveMat(sky, "Sky");
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.78f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.62f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.37f, 0.35f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.78f, 0.92f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 215f;

            // Mặt cỏ thật (ambientCG Grass004, CC0), lát lặp
            const string tex = "Assets/_Game/Art/Textures/Grass004";
            var normalImp = AssetImporter.GetAtPath($"{tex}/Grass004_1K-JPG_NormalGL.jpg") as TextureImporter;
            if (normalImp != null && normalImp.textureType != TextureImporterType.NormalMap)
            {
                normalImp.textureType = TextureImporterType.NormalMap;
                normalImp.SaveAndReimport();
            }
            var grass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/Grass004_1K-JPG_Color.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/Grass004_1K-JPG_NormalGL.jpg");
            if (color != null) { grass.SetTexture("_BaseMap", color); grass.SetTextureScale("_BaseMap", new Vector2(65f, 65f)); }
            if (normal != null) { grass.SetTexture("_BumpMap", normal); grass.EnableKeyword("_NORMALMAP"); grass.SetFloat("_BumpScale", 0.55f); }
            grass.SetColor("_BaseColor", new Color(0.84f, 0.98f, 0.78f));
            grass.SetFloat("_Smoothness", 0.05f);
            SaveMat(grass, "Ground_Grass");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().sharedMaterial = grass;

            // Đường đất + huy hiệu trung tâm + vùng lãnh thổ Xanh/Đỏ (hoàn toàn phẳng y <= 0.04, không collider)
            BuildDirt(pal);

            var deco = new GameObject("Decor").transform;

            // Tường thành đấu trường, cột viền vàng, khiên phe, đèn đuốc, tháp canh 4 góc
            float edge = Arena.HalfSize + 1.6f;
            BuildWalls(deco, edge, pal);

            // Cụm đá pha lê vàng ở 2 góc trung lập (nằm hẳn ngoài đường giao chiến)
            BuildCrystalCorners(deco, pal);

            // Trại vây thành Xanh & Đỏ bên hông lâu đài
            BuildCamps(deco, pal);

            // Rừng cây & vách đá bao quanh bên ngoài tường thành
            BuildOuterWorld(deco, edge, pal);
        }

        static GameObject CastlePiece(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Castle}/{name}.fbx");
        static GameObject NaturePiece(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Nature}/{name}.fbx");
        static GameObject SpacePiece(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Space}/{name}.fbx");

        static void SetAllMats(GameObject go, Material m)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = m;
                r.sharedMaterials = arr;
            }
        }

        /// <summary>Co/giãn chiều cao một khối về đúng target mét, đáy vẫn chạm đất.</summary>
        static void FitHeight(GameObject go, float target)
        {
            var b = WorldBounds(go);
            if (b.size.y < 0.001f) return;
            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x, s.y * target / b.size.y, s.z);
            b = WorldBounds(go);
            go.transform.position += Vector3.up * -b.min.y;
        }

        /// <summary>Tường thành đấu trường: cạnh trên + phải cao 4,5m; cạnh dưới + trái thấp 1,45m không che góc nhìn.</summary>
        static void BuildWalls(Transform parent, float edge, ArenaPalette pal)
        {
            var high = CastlePiece("wall");
            var low = CastlePiece("wall-half") ?? high;
            var bannerLong = CastlePiece("flag-banner-long");
            var bannerShort = CastlePiece("flag-banner-short");
            var pillarPrefab = CastlePiece("tower-square-base") ?? CastlePiece("wall-pillar");
            var gatePrefab = CastlePiece("gate");
            var pennant = CastlePiece("flag-pennant");
            if (high == null) return;

            var wallRoot = new GameObject("Walls").transform;
            wallRoot.SetParent(parent, false);

            const int n = 15; // số lẻ để đoạn i=7 nằm chính giữa tường
            float seg = (edge * 2f) / n;
            float[] pillarOffsets = { -21.2f, -12.8f, -4.3f, 4.3f, 12.8f, 21.2f };

            for (int side = 0; side < 4; side++)
            {
                bool tall = side == 1 || side == 3; // 1 = trên (z+), 3 = phải (x+)
                float wallH = tall ? 4.45f : 1.45f;
                Vector3 inward; float yaw;
                switch (side)
                {
                    case 0: yaw = 0f; inward = Vector3.forward; break;
                    case 1: yaw = 180f; inward = Vector3.back; break;
                    case 2: yaw = 90f; inward = Vector3.right; break;
                    default: yaw = 270f; inward = Vector3.left; break;
                }
                Quaternion facingIn = Quaternion.LookRotation(inward, Vector3.up);
                Vector3 tangent = Vector3.Cross(Vector3.up, inward).normalized;
                Vector3 wallCenter = -inward * edge;

                // Các đoạn tường đá
                for (int i = 0; i < n; i++)
                {
                    float along = -edge + seg * (i + 0.5f);
                    Vector3 p = wallCenter + tangent * along;
                    var w = Place(tall ? high : low, wallRoot, p, yaw, width: seg + 0.08f);
                    SetAllMats(w, pal.StoneWall);
                    FitHeight(w, wallH);
                }

                // Thanh viền vàng chạy dọc mép trên tường + chân tường đá xám đậm
                float span = edge * 2f - 3.0f;
                PlaceWorldMesh($"WallGoldCoping_{side}", pal.UnitBox, pal.GoldTrim, wallRoot,
                    wallCenter + Vector3.up * (wallH + 0.02f), facingIn, new Vector3(span, tall ? 0.28f : 0.18f, tall ? 1.05f : 0.85f));
                PlaceWorldMesh($"WallSlatePlinth_{side}", pal.UnitBox, pal.DarkSlate, wallRoot,
                    wallCenter + inward * 0.55f + Vector3.up * (tall ? 0.34f : 0.18f), facingIn, new Vector3(span, tall ? 0.68f : 0.36f, 0.52f));

                // Trụ tường xen kẽ có mũ kim tự tháp vàng
                foreach (float off in pillarOffsets)
                {
                    Vector3 pp = wallCenter + tangent * off;
                    float pilH = tall ? 4.95f : 1.92f;
                    if (pillarPrefab != null)
                    {
                        var pil = Place(pillarPrefab, wallRoot, pp, yaw, width: tall ? 1.65f : 1.35f);
                        SetAllMats(pil, pal.StoneLight);
                        FitHeight(pil, pilH);
                    }
                    else
                    {
                        PlaceWorldMesh("WallPillarBox", pal.UnitBox, pal.StoneLight, wallRoot,
                            pp + Vector3.up * (pilH * 0.5f), facingIn, new Vector3(1.5f, pilH, 1.5f));
                    }
                    PlaceWorldMesh("PillarGoldRing", pal.OctagonRing, pal.GoldTrim, wallRoot,
                        pp + Vector3.up * (pilH - 0.18f), facingIn, new Vector3(tall ? 1.05f : 0.88f, 0.55f, tall ? 1.05f : 0.88f));

                    // Đặt chậu đuốc lửa trên các trụ trọng điểm; trụ còn lại đội chóp vàng
                    bool hasBrazier =
                        (side == 1 && (Mathf.Abs(pp.x) < 5f || Mathf.Abs(pp.x) > 19f)) ||
                        (side == 3 && Mathf.Abs(pp.z) < 5f) ||
                        (side == 2 && pp.z > 10f && pp.z < 15f);

                    if (hasBrazier)
                    {
                        bool addPointLight =
                            (side == 1 && Mathf.Abs(pp.x) < 5f) ||     // 2 đèn cạnh cổng chính tường trên
                            (side == 3 && pp.z > 0f && pp.z < 5f) ||   // 1 đèn tường phải
                            (side == 2 && pp.z > 10f && pp.z < 15f);   // 1 đèn góc trên-trái tường trái
                        PlaceBrazier(wallRoot, pp + Vector3.up * pilH, inward, addPointLight, pal);
                    }
                    else
                    {
                        PlaceWorldMesh("PillarGoldCap", pal.PyramidCap, pal.GoldBright, wallRoot,
                            pp + Vector3.up * pilH, facingIn, new Vector3(tall ? 1.32f : 1.08f, tall ? 1.25f : 0.92f, tall ? 1.32f : 1.08f));
                    }
                }

                // Cổng chính, khiên huy hiệu & băng rôn phe trên từng mặt tường
                if (side == 1)
                {
                    // Tường trên (z+): Cổng vòm trung tâm + Khiên Vàng Hoàng Gia, cánh trái Xanh, cánh phải Đỏ
                    if (gatePrefab != null)
                    {
                        var gate = Place(gatePrefab, wallRoot, wallCenter + inward * 0.2f, yaw, width: 5.4f);
                        FitHeight(gate, 5.15f);
                        SetAllMats(gate, pal.StoneLight);
                    }
                    PlaceWorldMesh("TopGateBacking", pal.UnitBox, pal.DarkSlate, wallRoot,
                        wallCenter + Vector3.up * 2.1f, facingIn, new Vector3(3.4f, 4.1f, 0.8f));
                    PlaceShieldEmblem(wallRoot, wallCenter + inward * 0.85f + Vector3.up * 3.85f, facingIn, 2.25f, pal.GoldCrystal, pal.GoldBright, pal);

                    // Khiên Xanh cánh trái (x=-12.8) & Khiên Đỏ cánh phải (x=+12.8)
                    PlaceShieldEmblem(wallRoot, wallCenter + tangent * 12.8f + inward * 0.95f + Vector3.up * 3.05f, facingIn, 1.55f, pal.BlueBanner, pal.GoldBright, pal);
                    PlaceShieldEmblem(wallRoot, wallCenter - tangent * 12.8f + inward * 0.95f + Vector3.up * 3.05f, facingIn, 1.55f, pal.RedBanner, pal.GoldBright, pal);

                    if (bannerLong != null)
                    {
                        foreach (float xOff in new[] { -17.0f, -8.6f, 8.6f, 17.0f })
                        {
                            var b = Place(bannerLong, wallRoot, new Vector3(xOff, 0.7f, edge) + inward * 0.95f, yaw, height: 3.25f);
                            SetAllMats(b, xOff < 0f ? pal.BlueBanner : pal.RedBanner);
                        }
                    }
                }
                else if (side == 3)
                {
                    // Tường phải (x+, lãnh thổ Đỏ): Khiên Đỏ + băng rôn Đỏ
                    PlaceWorldMesh("RightCenterPlaque", pal.UnitBox, pal.StoneLight, wallRoot,
                        new Vector3(edge, 2.7f, 0f) + inward * 0.45f, facingIn, new Vector3(2.2f, 3.6f, 0.7f));
                    PlaceShieldEmblem(wallRoot, new Vector3(edge, 3.05f, 0f) + inward * 0.88f, facingIn, 1.75f, pal.RedBanner, pal.GoldBright, pal);
                    PlaceShieldEmblem(wallRoot, new Vector3(edge, 3.05f, 12.8f) + inward * 0.95f, facingIn, 1.50f, pal.RedBanner, pal.GoldBright, pal);
                    if (bannerLong != null)
                    {
                        foreach (float zOff in new[] { 17.0f, 8.6f, -8.6f, -17.0f })
                        {
                            var b = Place(bannerLong, wallRoot, new Vector3(edge, 0.7f, zOff) + inward * 0.95f, yaw, height: 3.25f);
                            SetAllMats(b, pal.RedBanner);
                        }
                    }
                }
                else if (side == 2)
                {
                    // Tường trái (x-, thấp): Khiên Xanh & băng rôn ngắn ở đoạn trên-trái dễ thấy trong khung 1080x1080
                    PlaceShieldEmblem(wallRoot, new Vector3(-edge, 1.32f, 12.8f) + inward * 0.82f, facingIn, 1.25f, pal.BlueBanner, pal.GoldBright, pal);
                    PlaceShieldEmblem(wallRoot, new Vector3(-edge, 1.32f, 0f) + inward * 0.82f, facingIn, 1.25f, pal.BlueBanner, pal.GoldBright, pal);
                    if (bannerShort != null)
                    {
                        foreach (float zOff in new[] { 17.0f, 8.6f, -8.6f })
                        {
                            var b = Place(bannerShort, wallRoot, new Vector3(-edge, 0.2f, zOff) + inward * 0.78f, yaw, height: 1.55f);
                            SetAllMats(b, pal.BlueBanner);
                        }
                    }
                }
                else
                {
                    // Tường dưới (z-, thấp): Khiên Xanh trung tâm & băng rôn ngắn
                    PlaceShieldEmblem(wallRoot, new Vector3(0f, 1.32f, -edge) + inward * 0.82f, facingIn, 1.35f, pal.BlueBanner, pal.GoldBright, pal);
                    if (bannerShort != null)
                    {
                        foreach (float xOff in new[] { -8.6f, 8.6f })
                        {
                            var b = Place(bannerShort, wallRoot, new Vector3(xOff, 0.2f, -edge) + inward * 0.78f, yaw, height: 1.55f);
                            SetAllMats(b, pal.BlueBanner);
                        }
                    }
                }
            }

            // 4 tháp góc: 3 góc xa cao uy nghi có chóp vàng + băng rôn, góc dưới-trái gần camera làm bệ thấp để không che Lõi Xanh
            BuildCornerTower(wallRoot, new Vector3(-edge, 0f, edge), 4.9f, tall: true, roofMat: pal.DarkSlate, bannerMat: pal.BlueBanner, pal, bannerLong, pennant);
            BuildCornerTower(wallRoot, new Vector3(edge, 0f, edge), 5.1f, tall: true, roofMat: pal.RedRoof, bannerMat: pal.RedBanner, pal, bannerLong, pennant);
            BuildCornerTower(wallRoot, new Vector3(edge, 0f, -edge), 4.6f, tall: true, roofMat: pal.DarkSlate, bannerMat: pal.RedBanner, pal, bannerLong, pennant);
            BuildCornerTower(wallRoot, new Vector3(-edge, 0f, -edge), 4.1f, tall: false, roofMat: pal.BlueRoof, bannerMat: pal.BlueBanner, pal, bannerShort, null);
        }

        static void BuildCornerTower(Transform parent, Vector3 pos, float width, bool tall, Material roofMat, Material bannerMat,
            ArenaPalette pal, GameObject bannerPrefab, GameObject pennantPrefab)
        {
            var tRoot = new GameObject($"CornerTower_{(pos.x < 0 ? "L" : "R")}_{(pos.z < 0 ? "B" : "T")}").transform;
            tRoot.SetParent(parent, false);
            tRoot.position = pos;

            Vector3 inward = (-pos).normalized; inward.y = 0f;
            float topY;
            if (tall)
            {
                topY = Stack(tRoot, Vector3.zero, width, pal.StoneWall, pal.StoneLight,
                    "tower-square-base-border", "tower-square-mid-windows", "tower-square-top-color");
                PlaceLocalMesh("CornerSlateBase", pal.OctagonRing, pal.DarkSlate, tRoot, new Vector3(0f, 0.1f, 0f), Quaternion.identity, new Vector3(width * 0.62f, 1.3f, width * 0.62f));
                PlaceLocalMesh("CornerGoldCollar", pal.OctagonRing, pal.GoldTrim, tRoot, new Vector3(0f, topY - 0.25f, 0f), Quaternion.identity, new Vector3(width * 0.56f, 1.15f, width * 0.56f));
                PlaceLocalMesh("CornerRoofBase", pal.PyramidCap, roofMat, tRoot, new Vector3(0f, topY - 0.05f, 0f), Quaternion.identity, new Vector3(width * 0.72f, 2.0f, width * 0.72f));
                PlaceLocalMesh("CornerGoldPyramid", pal.PyramidCap, pal.GoldBright, tRoot, new Vector3(0f, topY + 0.55f, 0f), Quaternion.identity, new Vector3(width * 0.52f, 2.25f, width * 0.52f));
            }
            else
            {
                // Góc dưới-trái gần camera: trụ pháo đài thấp gọn, không chắn tầm nhìn
                var basePrefab = CastlePiece("tower-square-base-border") ?? CastlePiece("tower-square-base");
                topY = 2.15f;
                if (basePrefab != null)
                {
                    var b = Place(basePrefab, tRoot, pos, 0f, width: width);
                    SetAllMats(b, pal.StoneWall);
                    FitHeight(b, topY);
                }
                PlaceLocalMesh("LowCornerCollar", pal.OctagonRing, pal.GoldTrim, tRoot, new Vector3(0f, topY - 0.12f, 0f), Quaternion.identity, new Vector3(width * 0.54f, 0.65f, width * 0.54f));
                PlaceLocalMesh("LowCornerCap", pal.PyramidCap, pal.GoldBright, tRoot, new Vector3(0f, topY, 0f), Quaternion.identity, new Vector3(width * 0.55f, 1.15f, width * 0.55f));
            }

            if (bannerPrefab != null)
            {
                float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                var b = Place(bannerPrefab, tRoot, pos + inward * (width * 0.52f) + Vector3.up * (tall ? 2.6f : 0.25f), yaw, height: tall ? 3.5f : 1.5f);
                SetAllMats(b, bannerMat);
            }
            if (tall && pennantPrefab != null)
            {
                float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                var pf = Place(pennantPrefab, tRoot, pos + Vector3.up * (topY + 1.85f), yaw, height: 2.0f);
                SetAllMats(pf, bannerMat);
            }
        }

        static void PlaceBrazier(Transform parent, Vector3 topPos, Vector3 inward, bool withLight, ArenaPalette pal)
        {
            var root = new GameObject("Brazier").transform;
            root.SetParent(parent, false);
            root.position = topPos;

            PlaceLocalMesh("Bowl", pal.BrazierBowl, pal.BrazierIron, root, Vector3.zero, Quaternion.identity, new Vector3(1.15f, 1.1f, 1.15f));
            PlaceLocalMesh("BowlRim", pal.OctagonRing, pal.GoldTrim, root, new Vector3(0f, 0.36f, 0f), Quaternion.identity, new Vector3(0.68f, 0.42f, 0.68f));
            PlaceLocalMesh("FlameCore", pal.CrystalSpire, pal.BrazierFlame, root, new Vector3(0f, 0.72f, 0f), Quaternion.identity, new Vector3(0.38f, 0.55f, 0.38f));

            if (withLight)
            {
                var lGo = new GameObject("BrazierLight");
                lGo.transform.SetParent(root, false);
                lGo.transform.localPosition = inward * 0.85f + Vector3.up * 1.15f;
                var l = lGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.72f, 0.34f);
                l.range = 15f;
                l.intensity = 2.8f;
                l.shadows = LightShadows.None;
            }
        }

        static void PlaceShieldEmblem(Transform parent, Vector3 pos, Quaternion rot, float scale, Material coreMat, Material frameMat, ArenaPalette pal)
        {
            var go = new GameObject("ShieldEmblem").transform;
            go.SetParent(parent, false);
            if (parent.name == "Castle")
            {
                go.localPosition = pos;
                go.localRotation = rot;
            }
            else
            {
                go.position = pos;
                go.rotation = rot;
            }
            PlaceLocalMesh("ShieldGoldFrame", pal.HeraldicShield, frameMat, go, Vector3.zero, Quaternion.identity, new Vector3(scale, scale, scale * 0.9f));
            PlaceLocalMesh("ShieldTeamCore", pal.HeraldicShield, coreMat, go, new Vector3(0f, 0.02f, 0.08f * scale), Quaternion.identity, new Vector3(scale * 0.72f, scale * 0.72f, scale * 0.85f));
        }

        /// <summary>Cụm đá pha lê vàng đặc trưng ở 2 góc trung lập (nằm ngoài hoàn toàn làn đường giao chiến).</summary>
        static void BuildCrystalCorners(Transform parent, ArenaPalette pal)
        {
            var root = new GameObject("CrystalCorners").transform;
            root.SetParent(parent, false);

            BuildCrystalCluster(root, new Vector3(-23.2f, 0f, 23.2f), 135f, pal);
            BuildCrystalCluster(root, new Vector3(21.4f, 0f, -20.2f), -45f, pal);
        }

        static void BuildCrystalCluster(Transform parent, Vector3 center, float facingYaw, ArenaPalette pal)
        {
            var cluster = new GameObject($"CrystalCluster_{(center.x < 0 ? "TL" : "BR")}").transform;
            cluster.SetParent(parent, false);
            cluster.position = center;
            cluster.rotation = Quaternion.Euler(0f, facingYaw, 0f);

            // Đá tảng nền xanh-xám
            var rockSpecs = new[]
            {
                ("rock_tallA", new Vector3(-1.35f, 0f, -1.65f), 25f, 3.6f),
                ("rock_largeA", new Vector3(1.55f, 0f, -1.35f), 110f, 2.75f),
                ("rock_largeB", new Vector3(-2.45f, 0f, 0.35f), 205f, 2.35f),
                ("rock_largeC", new Vector3(0.45f, 0f, -2.55f), 70f, 2.5f),
                ("rock_smallA", new Vector3(2.25f, 0f, 0.75f), 40f, 1.25f),
                ("rock_smallC", new Vector3(-1.15f, 0f, 1.85f), 160f, 1.15f),
            };
            foreach (var (name, offset, yaw, h) in rockSpecs)
            {
                var prefab = NaturePiece(name);
                if (prefab == null) continue;
                var r = Place(prefab, cluster, cluster.TransformPoint(offset), facingYaw + yaw, height: h);
                SetAllMats(r, pal.SlateRock);
            }

            // Mẫu đá tinh thể SpaceKit làm nền bổ sung
            var spaceCrystal = SpacePiece("rock_crystalsLargeA") ?? SpacePiece("rock_crystals");
            if (spaceCrystal != null)
            {
                var sc = Place(spaceCrystal, cluster, cluster.TransformPoint(new Vector3(-0.35f, 0f, -0.55f)), facingYaw + 30f, height: 2.2f);
                SetAllMats(sc, pal.SlateRock);
            }

            // Các trụ pha lê vàng đa diện phát sáng mọc xen kẽ giữa khe đá
            var shards = new[]
            {
                (new Vector3(0.15f, 1.45f, -0.35f), new Vector3(10f, 18f, -8f), new Vector3(0.88f, 1.45f, 0.88f)),
                (new Vector3(-0.95f, 1.15f, 0.35f), new Vector3(-14f, 55f, 16f), new Vector3(0.62f, 1.05f, 0.62f)),
                (new Vector3(1.15f, 0.95f, 0.20f), new Vector3(16f, -35f, -18f), new Vector3(0.56f, 0.92f, 0.56f)),
                (new Vector3(-0.20f, 2.25f, -1.45f), new Vector3(-8f, 80f, 12f), new Vector3(0.68f, 1.18f, 0.68f)),
                (new Vector3(0.65f, 0.62f, 1.35f), new Vector3(22f, 12f, -10f), new Vector3(0.42f, 0.68f, 0.42f)),
            };
            for (int i = 0; i < shards.Length; i++)
            {
                var (lPos, lRot, lScale) = shards[i];
                PlaceLocalMesh($"GoldCrystal_{i}", pal.CrystalSpire, pal.GoldCrystal, cluster, lPos, Quaternion.Euler(lRot), lScale);
            }

            // Bụi cây xanh ôm chân đá cho mềm mại
            var bush = NaturePiece("plant_bush");
            if (bush != null)
            {
                Place(bush, cluster, cluster.TransformPoint(new Vector3(-2.6f, 0f, 1.55f)), facingYaw + 20f, height: 1.05f);
                Place(bush, cluster, cluster.TransformPoint(new Vector3(2.55f, 0f, -0.55f)), facingYaw + 140f, height: 1.1f);
            }
        }

        /// <summary>Trại vây thành bên hông mỗi lâu đài: máy bắn đá/nỏ lớn, lửa trại, thùng gỗ, bó củi & cờ phe.</summary>
        static void BuildCamps(Transform parent, ArenaPalette pal)
        {
            var campRoot = new GameObject("SiegeCamps").transform;
            campRoot.SetParent(parent, false);

            var flagShort = CastlePiece("flag-banner-short");
            var campfireStones = NaturePiece("campfire_stones");
            var campfireLogs = NaturePiece("campfire_logs");
            var logStack = NaturePiece("log_stack");
            var barrels = SpacePiece("barrels") ?? SpacePiece("barrel");

            foreach (Team team in new[] { Team.Blue, Team.Red })
            {
                Vector3 core = Arena.CorePosition(team);
                Vector3 toCenter = (-core).normalized;
                Vector3 perp = new Vector3(toCenter.z, 0f, -toCenter.x);
                float yaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
                var bannerMat = team == Team.Blue ? pal.BlueBanner : pal.RedBanner;
                string[] engines = team == Team.Blue
                    ? new[] { "siege-catapult", "siege-ballista" }
                    : new[] { "siege-trebuchet", "siege-ballista" };

                for (int k = 0; k < 2; k++)
                {
                    float sideSign = k == 0 ? 1f : -1f;
                    var prefab = CastlePiece(engines[k]);
                    if (prefab == null) continue;

                    Vector3 pos = core + perp * (sideSign * 9.2f) + toCenter * 4.4f;
                    pos.x = Mathf.Clamp(pos.x, -Arena.HalfSize + 2.8f, Arena.HalfSize - 2.8f);
                    pos.z = Mathf.Clamp(pos.z, -Arena.HalfSize + 2.8f, Arena.HalfSize - 2.8f);

                    var e = Place(prefab, campRoot, pos, yaw, height: 3.15f);
                    SetAllMats(e, pal.CampWood);

                    if (flagShort != null)
                    {
                        var f = Place(flagShort, campRoot, pos - toCenter * 2.0f + perp * (sideSign * 1.45f), yaw, height: 2.65f);
                        SetAllMats(f, bannerMat);
                    }

                    if (logStack != null)
                        Place(logStack, campRoot, pos - toCenter * 2.1f - perp * (sideSign * 1.25f), yaw + 25f * sideSign, height: 0.82f);

                    if (barrels != null)
                    {
                        var br = Place(barrels, campRoot, pos + perp * (sideSign * 2.05f) - toCenter * 0.45f, yaw + 40f, height: 0.95f);
                        SetAllMats(br, pal.CampWood);
                    }

                    // Lửa trại có lõi than hồng phát sáng cạnh mỗi cụm trại
                    Vector3 firePos = pos - toCenter * 0.55f - perp * (sideSign * 2.25f);
                    if (campfireStones != null) Place(campfireStones, campRoot, firePos, yaw, height: 0.42f);
                    if (campfireLogs != null) Place(campfireLogs, campRoot, firePos, yaw + 20f, height: 0.52f);
                    PlaceWorldMesh($"CampEmber_{team}_{k}", pal.CrystalSpire, pal.BrazierFlame, campRoot,
                        firePos + Vector3.up * 0.42f, Quaternion.Euler(0f, yaw, 0f), new Vector3(0.28f, 0.36f, 0.28f), castShadows: false);
                }
            }
        }

        /// <summary>Mặt sân: đường đất chéo, bãi giao chiến trung tâm, vùng lãnh thổ Xanh/Đỏ, huy hiệu vàng trung tâm (phẳng y <= 0.04, không collider).</summary>
        static void BuildDirt(ArenaPalette pal)
        {
            var groundDecals = new GameObject("ArenaMarkings").transform;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dirt}/Ground054_1K-JPG_Color.jpg");
            if (tex != null)
            {
                var normalImp = AssetImporter.GetAtPath($"{Dirt}/Ground054_1K-JPG_NormalGL.jpg") as TextureImporter;
                if (normalImp != null && normalImp.textureType != TextureImporterType.NormalMap)
                {
                    normalImp.textureType = TextureImporterType.NormalMap;
                    normalImp.SaveAndReimport();
                }
                var dirtMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                dirtMat.SetTexture("_BaseMap", tex);
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dirt}/Ground054_1K-JPG_NormalGL.jpg");
                if (normal != null) { dirtMat.SetTexture("_BumpMap", normal); dirtMat.EnableKeyword("_NORMALMAP"); dirtMat.SetFloat("_BumpScale", 0.65f); }
                dirtMat.SetColor("_BaseColor", new Color(0.95f, 0.88f, 0.76f));
                dirtMat.SetFloat("_Smoothness", 0.04f);
                SaveMat(dirtMat, "Ground_Dirt");

                Vector3 a = Arena.CorePosition(Team.Blue), b = Arena.CorePosition(Team.Red);
                MeshObject("DirtRoad", RoadMesh(a, b, 4.15f), dirtMat, 0.012f, groundDecals);
                MeshObject("DirtCenter", BlobMesh(Vector3.zero, 11.8f), dirtMat, 0.015f, groundDecals);
                MeshObject("DirtBlueYard", BlobMesh(a + (Vector3.zero - a).normalized * 2.5f, 9.8f), dirtMat, 0.013f, groundDecals);
                MeshObject("DirtRedYard", BlobMesh(b + (Vector3.zero - b).normalized * 2.5f, 9.8f), dirtMat, 0.013f, groundDecals);
            }

            // Lớp phủ chuyển sắc nhẹ phân chia lãnh thổ Xanh / Đỏ (vertex alpha mờ dần vào giữa sân để vẫn thấy rõ vân cỏ/đất)
            MeshObject("TerritoryWash_Blue", TerritoryWashMesh(Team.Blue), pal.VertexTintMat, 0.018f, groundDecals);
            MeshObject("TerritoryWash_Red", TerritoryWashMesh(Team.Red), pal.VertexTintMat, 0.018f, groundDecals);

            // Sân đá bát giác & vòng viền lãnh thổ quanh Lõi Xanh & Lõi Đỏ
            foreach (Team team in new[] { Team.Blue, Team.Red })
            {
                Vector3 c = Arena.CorePosition(team);
                Material borderMat = team == Team.Blue ? pal.BlueBorder : pal.RedBorder;

                MeshObject($"YardPad_{team}", FlatPolygonMesh(c, 7.25f, 8, 22.5f), pal.PlazaStone, 0.021f, groundDecals);
                MeshObject($"YardGoldRim_{team}", FlatRingMesh(c, 7.05f, 7.50f, 8, 22.5f), pal.GoldTrim, 0.026f, groundDecals);
                MeshObject($"YardOuterSlate_{team}", FlatRingMesh(c, 10.65f, 11.55f, 32, 0f), pal.DarkSlate, 0.022f, groundDecals);
                MeshObject($"YardTeamRing_{team}", FlatRingMesh(c, 10.85f, 11.35f, 32, 0f), borderMat, 0.027f, groundDecals);
            }

            // Huy hiệu giao tranh trung tâm tại (0, 0) - hoàn toàn phẳng y <= 0.036, không cản đường lính
            MeshObject("CenterStonePad", FlatPolygonMesh(Vector3.zero, 5.45f, 8, 22.5f), pal.PlazaStone, 0.024f, groundDecals);
            MeshObject("CenterSlateRing", FlatRingMesh(Vector3.zero, 3.35f, 4.65f, 8, 22.5f), pal.DarkSlate, 0.027f, groundDecals);
            MeshObject("CenterGoldOuterRing", FlatRingMesh(Vector3.zero, 4.70f, 5.32f, 8, 22.5f), pal.GoldTrim, 0.031f, groundDecals);
            MeshObject("CenterGoldInnerRing", FlatRingMesh(Vector3.zero, 2.85f, 3.32f, 8, 22.5f), pal.GoldBright, 0.032f, groundDecals);
            MeshObject("CenterGoldStar", FlatStarMesh(Vector3.zero, 2.15f, 1.05f, 4, 0f), pal.GoldBright, 0.035f, groundDecals);

            // Đường phân tuyến chéo (x + z = 0) chia 2 nửa sân Xanh / Đỏ, ngắt gọn tại huy hiệu trung tâm
            Vector3 divDir = new Vector3(1f, 0f, -1f).normalized;
            Vector3 toRed = new Vector3(1f, 0f, 1f).normalized;
            MeshObject("ClashLineGold", SplitStripMesh(divDir, Vector3.zero, 5.65f, 25.5f, 0.34f), pal.GoldTrim, 0.028f, groundDecals);
            MeshObject("ClashLineBlue", SplitStripMesh(divDir, -toRed * 0.28f, 5.85f, 25.0f, 0.16f), pal.BlueBorder, 0.029f, groundDecals);
            MeshObject("ClashLineRed", SplitStripMesh(divDir, toRed * 0.28f, 5.85f, 25.0f, 0.16f), pal.RedBorder, 0.029f, groundDecals);

            // Phiến đá viền phẳng chạy dọc 2 mép đường giao chiến chéo
            MeshObject("RoadTrimStones", RoadCurbStonesMesh(), pal.PlazaStone, 0.021f, groundDecals);

            // Một vài bụi cỏ nhỏ gọn sát chân tường (tránh xa hoàn toàn đường đánh & giữa sân)
            var bush = NaturePiece("plant_bush");
            if (bush != null)
            {
                var rimDecor = new GameObject("FieldRimDecor").transform;
                var rimPositions = new[]
                {
                    new Vector3(-11.5f, 0f, 27.6f), new Vector3(11.5f, 0f, 27.6f),
                    new Vector3(27.6f, 0f, 4.5f), new Vector3(27.6f, 0f, -9.5f),
                    new Vector3(-27.6f, 0f, 9.5f), new Vector3(-27.6f, 0f, -4.5f),
                    new Vector3(-6.5f, 0f, -27.6f), new Vector3(9.5f, 0f, -27.6f),
                };
                for (int i = 0; i < rimPositions.Length; i++)
                    Place(bush, rimDecor, rimPositions[i], i * 47f, height: 0.72f);
            }
        }

        /// <summary>Rừng cây thông/sồi nhiều tầng, vách đá và bụi cây bao quanh phía ngoài tường thành.</summary>
        static void BuildOuterWorld(Transform parent, float edge, ArenaPalette pal)
        {
            var outer = new GameObject("OuterWorld").transform;
            outer.SetParent(parent, false);

            string[] trees =
            {
                $"{Nature}/tree_pineTallA.fbx", $"{Nature}/tree_pineDefaultA.fbx",
                $"{Nature}/tree_oak.fbx", $"{Nature}/tree_detailed.fbx",
                $"{Nature}/tree_default.fbx", $"{Castle}/tree-large.fbx",
            };
            string[] rocks =
            {
                $"{Nature}/cliff_rock.fbx", $"{Nature}/rock_largeA.fbx",
                $"{Nature}/rock_largeB.fbx", $"{Nature}/rock_tallA.fbx",
                $"{Nature}/plant_bushLarge.fbx", $"{Nature}/plant_bush.fbx",
            };

            var rng = new System.Random(19);

            // Vành đai gần tường (tạo khung nền xanh mướt ngay ngoài tường trên/trái/phải trong khung hình 1080x1080)
            int nearPlaced = 0;
            for (int i = 0; i < 420 && nearPlaced < 95; i++)
            {
                float x = (float)(rng.NextDouble() * 108 - 54), z = (float)(rng.NextDouble() * 108 - 54);
                float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                if (d < edge + 3.2f || d > edge + 22f) continue;
                bool isTree = rng.NextDouble() < 0.72;
                var path = isTree ? trees[rng.Next(trees.Length)] : rocks[rng.Next(rocks.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                float h = isTree ? (float)(5.2 + rng.NextDouble() * 4.2) : (float)(1.4 + rng.NextDouble() * 2.4);
                var go = Place(prefab, outer, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360), height: h);
                if (!isTree && path.Contains("rock")) SetAllMats(go, pal.SlateRock);
                nearPlaced++;
            }

            // Vành đai rừng xa tạo chiều sâu không gian
            int farPlaced = 0;
            for (int i = 0; i < 650 && farPlaced < 150; i++)
            {
                float x = (float)(rng.NextDouble() * 176 - 88), z = (float)(rng.NextDouble() * 176 - 88);
                float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                if (d < edge + 20f) continue;
                bool isTree = rng.NextDouble() < 0.76;
                var path = isTree ? trees[rng.Next(trees.Length)] : rocks[rng.Next(rocks.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                float h = isTree ? (float)(6.0 + rng.NextDouble() * 4.5) : (float)(1.8 + rng.NextDouble() * 2.6);
                var go = Place(prefab, outer, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360), height: h);
                if (!isTree && path.Contains("rock")) SetAllMats(go, pal.SlateRock);
                farPlaced++;
            }
        }

        // ---------------- Procedural Mesh Builders ----------------

        static Mesh SaveMeshAsset(Mesh mesh, string name)
        {
            mesh.name = name;
            string path = $"{Generated}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null || File.Exists(path))
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void MeshObject(string name, Mesh mesh, Material mat, float y, Transform parent = null, bool castShadows = false)
        {
            SaveMeshAsset(mesh, name);
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, y, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        static GameObject PlaceLocalMesh(string name, Mesh mesh, Material mat, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static GameObject PlaceWorldMesh(string name, Mesh mesh, Material mat, Transform parent, Vector3 worldPos, Quaternion worldRot, Vector3 scale, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static void AddFlatTri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            int idx = verts.Count;
            if (Vector3.Dot(n, outward) < 0f)
            {
                verts.Add(a); verts.Add(c); verts.Add(b);
            }
            else
            {
                verts.Add(a); verts.Add(b); verts.Add(c);
            }
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        }

        static void AddFlatQuad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            AddFlatTri(verts, tris, a, b, c, outward);
            AddFlatTri(verts, tris, a, c, d, outward);
        }

        static Mesh FinalizeFlatMesh(List<Vector3> verts, List<int> tris)
        {
            var uvs = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++) uvs[i] = new Vector2(verts[i].x, verts[i].z);
            var m = new Mesh { vertices = verts.ToArray(), triangles = tris.ToArray(), uv = uvs };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh CreateUnitBoxMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 p000 = new Vector3(-0.5f, -0.5f, -0.5f), p100 = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 p110 = new Vector3(0.5f, 0.5f, -0.5f), p010 = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 p001 = new Vector3(-0.5f, -0.5f, 0.5f), p101 = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 p111 = new Vector3(0.5f, 0.5f, 0.5f), p011 = new Vector3(-0.5f, 0.5f, 0.5f);
            AddFlatQuad(v, t, p010, p110, p111, p011, Vector3.up);
            AddFlatQuad(v, t, p000, p001, p101, p100, Vector3.down);
            AddFlatQuad(v, t, p001, p011, p111, p101, Vector3.forward);
            AddFlatQuad(v, t, p100, p110, p010, p000, Vector3.back);
            AddFlatQuad(v, t, p101, p111, p110, p100, Vector3.right);
            AddFlatQuad(v, t, p000, p010, p011, p001, Vector3.left);
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreatePyramidCapMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            float w = 0.5f, bh = 0.14f, topH = 0.78f;
            Vector3[] b = { new Vector3(-w, 0, -w), new Vector3(w, 0, -w), new Vector3(w, 0, w), new Vector3(-w, 0, w) };
            Vector3[] m = { new Vector3(-w, bh, -w), new Vector3(w, bh, -w), new Vector3(w, bh, w), new Vector3(-w, bh, w) };
            Vector3 apex = new Vector3(0f, topH, 0f);
            for (int i = 0; i < 4; i++)
            {
                int ni = (i + 1) % 4;
                Vector3 outDir = ((b[i] + b[ni]) * 0.5f).normalized;
                AddFlatQuad(v, t, b[i], b[ni], m[ni], m[i], outDir);
                AddFlatTri(v, t, m[i], m[ni], apex, (outDir + Vector3.up * 0.7f).normalized);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreateCrystalSpireMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int sides = 6;
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                ring[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            }
            Vector3 top = new Vector3(0f, 1.85f, 0f);
            Vector3 bot = new Vector3(0f, -0.78f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int ni = (i + 1) % sides;
                Vector3 outDir = ((ring[i] + ring[ni]) * 0.5f).normalized;
                AddFlatTri(v, t, ring[i], ring[ni], top, (outDir + Vector3.up * 0.4f).normalized);
                AddFlatTri(v, t, ring[ni], ring[i], bot, (outDir + Vector3.down * 0.5f).normalized);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreateHeraldicShieldMesh()
        {
            // Khiên 5 đỉnh hướng mặt trước về +Z
            var v = new List<Vector3>(); var t = new List<int>();
            Vector2[] pts =
            {
                new Vector2(0f, 0.62f),
                new Vector2(0.48f, 0.46f),
                new Vector2(0.42f, -0.10f),
                new Vector2(0f, -0.64f),
                new Vector2(-0.42f, -0.10f),
                new Vector2(-0.48f, 0.46f),
            };
            float halfD = 0.08f, peakZ = 0.20f;
            Vector3 frontCenter = new Vector3(0f, 0.05f, peakZ);
            Vector3 backCenter = new Vector3(0f, 0.05f, -halfD);
            for (int i = 0; i < pts.Length; i++)
            {
                int ni = (i + 1) % pts.Length;
                Vector3 f0 = new Vector3(pts[i].x, pts[i].y, halfD);
                Vector3 f1 = new Vector3(pts[ni].x, pts[ni].y, halfD);
                Vector3 b0 = new Vector3(pts[i].x, pts[i].y, -halfD);
                Vector3 b1 = new Vector3(pts[ni].x, pts[ni].y, -halfD);
                Vector3 edgeOut = new Vector3(pts[i].x + pts[ni].x, pts[i].y + pts[ni].y, 0f).normalized;
                AddFlatTri(v, t, frontCenter, f0, f1, (Vector3.forward + edgeOut * 0.25f).normalized);
                AddFlatTri(v, t, backCenter, b1, b0, Vector3.back);
                AddFlatQuad(v, t, f0, b0, b1, f1, edgeOut);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreateOctagonRingMesh(float innerR, float outerR, float height)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int n = 8;
            float offset = 22.5f * Mathf.Deg2Rad;
            for (int i = 0; i < n; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / n;
                float a1 = offset + (i + 1) * Mathf.PI * 2f / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 outDir = (d0 + d1).normalized;

                Vector3 ob0 = d0 * outerR, ob1 = d1 * outerR;
                Vector3 ot0 = ob0 + Vector3.up * height, ot1 = ob1 + Vector3.up * height;
                Vector3 ib0 = d0 * innerR, ib1 = d1 * innerR;
                Vector3 it0 = ib0 + Vector3.up * height, it1 = ib1 + Vector3.up * height;

                AddFlatQuad(v, t, ob0, ob1, ot1, ot0, outDir);
                AddFlatQuad(v, t, ib1, ib0, it0, it1, -outDir);
                AddFlatQuad(v, t, it0, ot0, ot1, it1, Vector3.up);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreateOctagonPrismMesh(float radius, float height)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int n = 8;
            float offset = 22.5f * Mathf.Deg2Rad;
            Vector3 topCenter = new Vector3(0f, height, 0f);
            for (int i = 0; i < n; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / n;
                float a1 = offset + (i + 1) * Mathf.PI * 2f / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 outDir = (d0 + d1).normalized;

                Vector3 b0 = d0 * (radius * 1.04f), b1 = d1 * (radius * 1.04f);
                Vector3 t0 = d0 * radius + Vector3.up * height, t1 = d1 * radius + Vector3.up * height;
                AddFlatQuad(v, t, b0, b1, t1, t0, outDir);
                AddFlatTri(v, t, topCenter, t0, t1, Vector3.up);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh CreateBrazierBowlMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int n = 6;
            float botR = 0.28f, topR = 0.58f, h = 0.44f;
            Vector3 topCenter = new Vector3(0f, h * 0.78f, 0f);
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n;
                float a1 = (i + 1) * Mathf.PI * 2f / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 outDir = (d0 + d1).normalized;

                Vector3 b0 = d0 * botR, b1 = d1 * botR;
                Vector3 t0 = d0 * topR + Vector3.up * h, t1 = d1 * topR + Vector3.up * h;
                AddFlatQuad(v, t, b0, b1, t1, t0, (outDir + Vector3.down * 0.35f).normalized);
                AddFlatTri(v, t, topCenter, t0, t1, Vector3.up);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh TerritoryWashMesh(Team team)
        {
            // Lưới phẳng phủ nửa sân của phe với vertex alpha mượt mà từ góc lâu đài mờ dần về đường giữa
            const int grid = 20;
            float half = Arena.HalfSize;
            Vector3 core = Arena.CorePosition(team);
            Color baseCol = team == Team.Blue ? new Color(0.18f, 0.52f, 1.0f, 1f) : new Color(1.0f, 0.24f, 0.24f, 1f);

            var verts = new Vector3[(grid + 1) * (grid + 1)];
            var uvs = new Vector2[verts.Length];
            var cols = new Color[verts.Length];
            var tris = new int[grid * grid * 6];

            for (int iz = 0; iz <= grid; iz++)
            {
                for (int ix = 0; ix <= grid; ix++)
                {
                    float x = Mathf.Lerp(-half, half, (float)ix / grid);
                    float z = Mathf.Lerp(-half, half, (float)iz / grid);
                    int idx = iz * (grid + 1) + ix;
                    verts[idx] = new Vector3(x, 0f, z);
                    uvs[idx] = new Vector2((float)ix / grid, (float)iz / grid);

                    float signedDiag = (x + z) * (team == Team.Red ? 1f : -1f); // > 0 trong nửa sân phe mình
                    float sideFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((signedDiag - 2.0f) / 22.0f));
                    float distCore = Vector2.Distance(new Vector2(x, z), new Vector2(core.x, core.z));
                    float coreBoost = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((distCore - 8.5f) / 17.0f));
                    float alpha = Mathf.Clamp01(sideFade * 0.16f + coreBoost * 0.18f);
                    if (signedDiag <= 0.5f) alpha = 0f;
                    cols[idx] = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
                }
            }

            int k = 0;
            for (int iz = 0; iz < grid; iz++)
            {
                for (int ix = 0; ix < grid; ix++)
                {
                    int v00 = iz * (grid + 1) + ix;
                    int v10 = v00 + 1;
                    int v01 = v00 + (grid + 1);
                    int v11 = v01 + 1;
                    tris[k++] = v00; tris[k++] = v01; tris[k++] = v11;
                    tris[k++] = v00; tris[k++] = v11; tris[k++] = v10;
                }
            }

            var m = new Mesh { vertices = verts, uv = uvs, colors = cols, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh FlatPolygonMesh(Vector3 center, float radius, int sides, float angleOffsetDeg)
        {
            var verts = new Vector3[sides + 1];
            var uvs = new Vector2[sides + 1];
            var tris = new int[sides * 3];
            verts[0] = center; uvs[0] = new Vector2(0.5f, 0.5f);
            float off = angleOffsetDeg * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float a = off + i * Mathf.PI * 2f / sides;
                verts[i + 1] = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = (i + 1) % sides + 1;
                tris[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh FlatRingMesh(Vector3 center, float innerR, float outerR, int sides, float angleOffsetDeg)
        {
            var verts = new Vector3[sides * 2];
            var uvs = new Vector2[sides * 2];
            var tris = new int[sides * 6];
            float off = angleOffsetDeg * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float a = off + i * Mathf.PI * 2f / sides;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = center + dir * innerR;
                verts[i * 2 + 1] = center + dir * outerR;
                uvs[i * 2] = new Vector2((float)i / sides, 0f);
                uvs[i * 2 + 1] = new Vector2((float)i / sides, 1f);

                int next = (i + 1) % sides;
                int k = i * 6;
                tris[k] = i * 2; tris[k + 1] = next * 2; tris[k + 2] = i * 2 + 1;
                tris[k + 3] = i * 2 + 1; tris[k + 4] = next * 2; tris[k + 5] = next * 2 + 1;
            }
            var m = new Mesh { vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh FlatStarMesh(Vector3 center, float outerR, float innerR, int points, float angleOffsetDeg)
        {
            int count = points * 2;
            var verts = new Vector3[count + 1];
            var uvs = new Vector2[count + 1];
            var tris = new int[count * 3];
            verts[0] = center; uvs[0] = new Vector2(0.5f, 0.5f);
            float off = angleOffsetDeg * Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                float a = off + i * Mathf.PI * 2f / count;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts[i + 1] = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = (i + 1) % count + 1;
                tris[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh SplitStripMesh(Vector3 dir, Vector3 lateralOffset, float startDist, float endDist, float width)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) * (width * 0.5f);
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector3 p0 = lateralOffset + dir * (sign * startDist);
                Vector3 p1 = lateralOffset + dir * (sign * endDist);
                AddFlatQuad(v, t, p0 - side, p1 - side, p1 + side, p0 + side, Vector3.up);
            }
            return FinalizeFlatMesh(v, t);
        }

        static Mesh RoadCurbStonesMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 roadDir = new Vector3(1f, 0f, 1f).normalized;
            Vector3 roadSide = new Vector3(1f, 0f, -1f).normalized;
            float stoneLen = 1.45f, stoneW = 0.52f;
            foreach (float endSign in new[] { -1f, 1f })
            {
                for (float d = 7.4f; d <= 18.2f; d += 2.65f)
                {
                    foreach (float latSign in new[] { -1f, 1f })
                    {
                        Vector3 c = roadDir * (endSign * d) + roadSide * (latSign * 4.45f);
                        Vector3 dl = roadDir * (stoneLen * 0.5f);
                        Vector3 dw = roadSide * (stoneW * 0.5f);
                        AddFlatQuad(v, t, c - dl - dw, c + dl - dw, c + dl + dw, c - dl + dw, Vector3.up);
                    }
                }
            }
            return FinalizeFlatMesh(v, t);
        }

        static Vector2 DirtUv(Vector3 p) => new Vector2(p.x, p.z) / 7f;

        /// <summary>Mảng đất tròn, viền lượn sóng.</summary>
        static Mesh BlobMesh(Vector3 center, float radius)
        {
            const int n = 64;
            var verts = new Vector3[n + 1]; var uvs = new Vector2[n + 1]; var tris = new int[n * 3];
            verts[0] = center; uvs[0] = DirtUv(center);
            float seed = center.x * 0.37f + center.z * 0.11f;
            for (int i = 0; i < n; i++)
            {
                float ang = i * Mathf.PI * 2f / n;
                float r = radius * (1f + 0.10f * Mathf.Sin(ang * 3f + seed) + 0.06f * Mathf.Sin(ang * 7f + seed * 2f) + 0.03f * Mathf.Sin(ang * 13f));
                var p = center + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                verts[i + 1] = p; uvs[i + 1] = DirtUv(p);
                tris[i * 3] = 0; tris[i * 3 + 1] = (i + 1) % n + 1; tris[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { vertices = verts, uv = uvs, triangles = tris, name = "DirtBlob" };
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Dải đường đất từ a tới b, hai mép lượn sóng.</summary>
        static Mesh RoadMesh(Vector3 a, Vector3 b, float halfWidth)
        {
            const int n = 60;
            Vector3 dir = (b - a).normalized, side = new Vector3(dir.z, 0f, -dir.x);
            var verts = new Vector3[(n + 1) * 2]; var uvs = new Vector2[verts.Length]; var tris = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                Vector3 c = Vector3.Lerp(a, b, t);
                float w1 = halfWidth * (1f + 0.14f * Mathf.Sin(t * 23f) + 0.08f * Mathf.Sin(t * 57f + 1f));
                float w2 = halfWidth * (1f + 0.14f * Mathf.Sin(t * 19f + 2f) + 0.08f * Mathf.Sin(t * 61f));
                verts[i * 2] = c + side * w1; verts[i * 2 + 1] = c - side * w2;
                uvs[i * 2] = DirtUv(verts[i * 2]); uvs[i * 2 + 1] = DirtUv(verts[i * 2 + 1]);
                if (i < n)
                {
                    int k = i * 6, v = i * 2;
                    tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
                    tris[k + 3] = v + 1; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
                }
            }
            var m = new Mesh { vertices = verts, uv = uvs, triangles = tris, name = "DirtRoad" };
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            if (m.normals.Length > 0 && m.normals[0].y < 0f) { System.Array.Reverse(tris); m.triangles = tris; m.RecalculateNormals(); }
            return m;
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
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null || File.Exists(path))
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
        }

        /// <summary>Hậu kỳ: bloom gọn cho pha lê/đèn đuốc/kỹ năng, màu sắc sắc nét tươi tắn, viền tối dịu.</summary>
        static void MakePostFx(Camera cam)
        {
            const string path = Root + "/Settings/PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null || File.Exists(path))
                AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.75f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.62f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(20f); color.contrast.Override(12f); color.postExposure.Override(0.12f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
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
