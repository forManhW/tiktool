using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI.EditorTools
{
    /// <summary>
    /// Công cụ Editor tự động dựng toàn bộ tài nguyên UI cho Đại Chiến Xanh Đỏ:
    /// - Import TMP Essential Resources & cấu hình TMP_Settings (ẩn ký tự thiếu/emoji không hiện ô vuông).
    /// - Tạo Dynamic TMP_FontAsset từ BeVietnamPro-Bold / ExtraBold (hỗ trợ đầy đủ tiếng Việt + vật liệu outline/shadow).
    /// - Import toàn bộ Icons & Sprites sang Sprite, sinh sprite bo góc 9-slice sắc nét.
    /// - Tạo prefab hoàn chỉnh Assets/_Game/Resources/UI/UIRoot.prefab và nối đủ mọi trường serialized.
    /// </summary>
    public static class UISetupBuilder
    {
        const string FontsDir = "Assets/_Game/UI/Fonts";
        const string IconsDir = "Assets/_Game/UI/Icons";
        const string SpritesDir = "Assets/_Game/UI/Sprites";
        const string PrefabDir = "Assets/_Game/Resources/UI";
        const string PrefabPath = PrefabDir + "/UIRoot.prefab";

        const string BoldTtfPath = FontsDir + "/BeVietnamPro-Bold.ttf";
        const string ExtraBoldTtfPath = FontsDir + "/BeVietnamPro-ExtraBold.ttf";
        const string SemiBoldTtfPath = FontsDir + "/BeVietnamPro-SemiBold.ttf";

        const string BoldFontAssetPath = FontsDir + "/BeVietnamPro-Bold SDF.asset";
        const string ExtraBoldFontAssetPath = FontsDir + "/BeVietnamPro-ExtraBold SDF.asset";
        const string SemiBoldFontAssetPath = FontsDir + "/BeVietnamPro-SemiBold SDF.asset";

        const string BoldOutlineMatPath = FontsDir + "/BeVietnamPro-Bold SDF - Outline.mat";
        const string ExtraBoldOutlineMatPath = FontsDir + "/BeVietnamPro-ExtraBold SDF - Outline.mat";

        const string RoundedRectPath = SpritesDir + "/ui_rounded_rect.png";
        const string RoundedRectSmPath = SpritesDir + "/ui_rounded_rect_sm.png";
        const string RoundedOutlinePath = SpritesDir + "/ui_rounded_outline.png";
        const string CircleSpritePath = SpritesDir + "/ui_circle.png";
        const string WhiteSpritePath = SpritesDir + "/ui_white.png";
        const string ScoreFramePath = SpritesDir + "/score_frame.png";

        // Tập ký tự ASCII + toàn bộ bảng chữ cái tiếng Việt có dấu để pre-bake vào Dynamic atlas
        const string VietnamesePrebakeChars =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
            "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊị" +
            "ỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ" +
            "·–—•×…✓✕▼▲";

        static readonly string[] EffectIconIds =
        {
            "join",
            "spawn",
            "like_power",
            "punch",
            "combo_punch",
            "superhero",
            "lightning",
            "tank",
            "meteor",
            "giant_robot"
        };

        struct BuiltSprites
        {
            public Sprite RoundedRect;
            public Sprite RoundedRectSm;
            public Sprite RoundedOutline;
            public Sprite Circle;
            public Sprite White;
            public Sprite ScoreFrame;
        }

        struct BuiltFonts
        {
            public TMP_FontAsset Bold;
            public TMP_FontAsset ExtraBold;
            public TMP_FontAsset SemiBold;
            public Material BoldOutlineMat;
            public Material ExtraBoldOutlineMat;
        }

        [MenuItem("Dai Chien Xanh Do/UI/Setup UI (Fonts + UIRoot Prefab)")]
        public static void Build() => BuildAll();

        public static void BuildAll()
        {
            Debug.Log("[UISetupBuilder] Bắt đầu dựng hệ thống UI Đại Chiến Xanh Đỏ...");

            EnsureDirectory(FontsDir);
            EnsureDirectory(SpritesDir);
            EnsureDirectory(PrefabDir);

            // Bước A: Import TMP Essential Resources
            EnsureTmpEssentialResources();

            // Bước B: Tạo Dynamic TMP_FontAsset cho Be Vietnam Pro + cấu hình TMP_Settings
            BuiltFonts fonts = BuildFonts();
            ConfigureTmpSettings(fonts.Bold);

            // Bước C: Sinh sprite 9-slice + cấu hình import Sprite cho toàn bộ Icons & Sprites
            BuiltSprites sprites = BuildAndConfigureSprites();

            // Bước D: Dựng prefab Assets/_Game/Resources/UI/UIRoot.prefab
            BuildUIRootPrefab(fonts, sprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UISetupBuilder] Hoàn tất! Prefab: {PrefabPath}");
        }

        #region Step A - TMP Essential Resources

        static void EnsureTmpEssentialResources()
        {
            string[] existing = AssetDatabase.FindAssets("t:TMP_Settings");
            bool hasShader = Shader.Find("TextMeshPro/Mobile/Distance Field") != null
                          || Shader.Find("TextMeshPro/Distance Field") != null;
            if (existing != null && existing.Length > 0 && hasShader)
            {
                Debug.Log("[UISetupBuilder] Đã có TMP_Settings và TMP Shaders trong project.");
                return;
            }

            string pkgPath = FindTmpEssentialResourcesPackage();
            if (!string.IsNullOrEmpty(pkgPath) && File.Exists(pkgPath))
            {
                Debug.Log($"[UISetupBuilder] Importing TMP Essential Resources từ: {pkgPath}");
                AssetDatabase.ImportPackage(pkgPath, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            existing = AssetDatabase.FindAssets("t:TMP_Settings");
            if (existing == null || existing.Length == 0)
            {
                const string tmpResDir = "Assets/TextMesh Pro/Resources";
                EnsureDirectory(tmpResDir);
                var created = ScriptableObject.CreateInstance<TMP_Settings>();
                AssetDatabase.CreateAsset(created, tmpResDir + "/TMP Settings.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[UISetupBuilder] Đã tạo TMP Settings.asset tại " + tmpResDir);
            }
        }

        static string FindTmpEssentialResourcesPackage()
        {
            string direct = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (File.Exists(direct)) return direct;

            string cacheRoot = Path.GetFullPath("Library/PackageCache");
            if (Directory.Exists(cacheRoot))
            {
                foreach (string dir in Directory.GetDirectories(cacheRoot, "com.unity.ugui*"))
                {
                    string candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            return null;
        }

        static void ConfigureTmpSettings(TMP_FontAsset defaultFont)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_Settings");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[UISetupBuilder] Không tìm thấy TMP_Settings để cấu hình.");
                return;
            }

            string settingsPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
            if (settings == null) return;

            var so = new SerializedObject(settings);

            // 0x200B (Zero-Width Space) được TMP tự synthesize trên mọi font asset -> ký tự thiếu (emoji) ẩn hoàn toàn, không hiện ô vuông!
            var missingProp = so.FindProperty("m_missingGlyphCharacter");
            if (missingProp != null) missingProp.intValue = 0x200B;

            var warnProp = so.FindProperty("m_warningsDisabled");
            if (warnProp != null) warnProp.boolValue = true;

            if (defaultFont != null)
            {
                var defFontProp = so.FindProperty("m_defaultFontAsset");
                if (defFontProp != null) defFontProp.objectReferenceValue = defaultFont;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[UISetupBuilder] Đã cấu hình TMP_Settings (missingGlyphCharacter=0x200B, warningsDisabled=true, defaultFont=BeVietnamPro-Bold SDF).");
        }

        #endregion

        #region Step B - Dynamic TMP Font Assets

        static BuiltFonts BuildFonts()
        {
            var result = new BuiltFonts
            {
                Bold = CreateOrUpdateDynamicFontAsset(BoldTtfPath, BoldFontAssetPath, 72),
                ExtraBold = CreateOrUpdateDynamicFontAsset(ExtraBoldTtfPath, ExtraBoldFontAssetPath, 72),
                SemiBold = File.Exists(SemiBoldTtfPath)
                    ? CreateOrUpdateDynamicFontAsset(SemiBoldTtfPath, SemiBoldFontAssetPath, 72)
                    : null
            };

            if (result.SemiBold == null) result.SemiBold = result.Bold;

            result.BoldOutlineMat = CreateOrUpdateOutlinePreset(result.Bold, BoldOutlineMatPath, 0.16f, 0.65f, -0.75f);
            result.ExtraBoldOutlineMat = CreateOrUpdateOutlinePreset(result.ExtraBold, ExtraBoldOutlineMatPath, 0.18f, 0.7f, -0.85f);

            AssetDatabase.SaveAssets();
            return result;
        }

        static TMP_FontAsset CreateOrUpdateDynamicFontAsset(string ttfPath, string outAssetPath, int pointSize)
        {
            if (!File.Exists(ttfPath))
            {
                Debug.LogError($"[UISetupBuilder] Không tìm thấy font nguồn: {ttfPath}");
                return null;
            }

            // Đảm bảo TTF có includeFontData = true để Dynamic font hoạt động trong bản build .exe
            var importer = AssetImporter.GetAtPath(ttfPath) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);
                sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            }

            if (sourceFont == null)
            {
                Debug.LogError($"[UISetupBuilder] Không thể nạp Font từ {ttfPath}");
                return null;
            }

            // Xóa bản cũ nếu tồn tại để tái tạo sạch sẽ
            if (File.Exists(outAssetPath))
            {
                AssetDatabase.DeleteAsset(outAssetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                pointSize,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                Debug.LogError($"[UISetupBuilder] TMP_FontAsset.CreateFontAsset thất bại cho {ttfPath}");
                return null;
            }

            string baseName = Path.GetFileNameWithoutExtension(outAssetPath);
            fontAsset.name = baseName;
            fontAsset.isMultiAtlasTexturesEnabled = true;

            // Lưu Asset chính và thêm sub-asset Atlas + Material
            AssetDatabase.CreateAsset(fontAsset, outAssetPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = baseName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = baseName + " Material";
                // Bật drop-shadow nhẹ trên material mặc định để chữ rõ trên nền 3D
                ApplyUnderlay(fontAsset.material, new Color(0.02f, 0.04f, 0.09f, 0.85f), 0.45f, -0.55f, 0.2f, 0.25f);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            // Đảm bảo GUID font nguồn và giữ dữ liệu pre-bake tiếng Việt trong bản build
            string sourceGuid = AssetDatabase.AssetPathToGUID(ttfPath);
            var so = new SerializedObject(fontAsset);
            var guidProp = so.FindProperty("m_SourceFontFileGUID");
            if (guidProp != null) guidProp.stringValue = sourceGuid;
            var edRefProp = so.FindProperty("m_SourceFontFile_EditorRef");
            if (edRefProp != null) edRefProp.objectReferenceValue = sourceFont;
            var clearOnBuildProp = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuildProp != null) clearOnBuildProp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Pre-bake bộ ký tự tiếng Việt + ASCII vào atlas (vẫn giữ Dynamic cho mọi ký tự lạ khi chạy)
            fontAsset.TryAddCharacters(VietnamesePrebakeChars, out string missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.Log($"[UISetupBuilder] Font {baseName}: một vài ký tự trang trí không có trong font (bỏ qua): '{missing}'");
            }

            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                EditorUtility.SetDirty(fontAsset.atlasTextures[0]);
            }
            if (fontAsset.material != null)
            {
                EditorUtility.SetDirty(fontAsset.material);
            }

            Debug.Log($"[UISetupBuilder] Đã tạo Dynamic TMP_FontAsset: {outAssetPath} ({fontAsset.characterTable.Count} ký tự pre-baked)");
            return fontAsset;
        }

        static Material CreateOrUpdateOutlinePreset(TMP_FontAsset fontAsset, string matPath, float outlineWidth, float offsetX, float offsetY)
        {
            if (fontAsset == null || fontAsset.material == null) return null;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(fontAsset.material);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.CopyPropertiesFromMaterial(fontAsset.material);
            }

            mat.name = Path.GetFileNameWithoutExtension(matPath);
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.03f, 0.05f, 0.11f, 0.96f));
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.06f);
            ApplyUnderlay(mat, new Color(0.0f, 0.0f, 0.0f, 0.9f), offsetX, offsetY, 0.3f, 0.25f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ApplyUnderlay(Material mat, Color color, float offsetX, float offsetY, float dilate, float softness)
        {
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, color);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offsetX);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offsetY);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        }

        #endregion

        #region Step C - Procedural 9-Slice & Sprite Import

        static BuiltSprites BuildAndConfigureSprites()
        {
            WriteRoundedRectPng(RoundedRectPath, 64, 16, 0);
            WriteRoundedRectPng(RoundedRectSmPath, 48, 10, 0);
            WriteRoundedRectPng(RoundedOutlinePath, 64, 16, 3);
            WriteCirclePng(CircleSpritePath, 32);
            WriteWhitePng(WhiteSpritePath, 8);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSpriteImporter(RoundedRectPath, new Vector4(18, 18, 18, 18));
            ConfigureSpriteImporter(RoundedRectSmPath, new Vector4(12, 12, 12, 12));
            ConfigureSpriteImporter(RoundedOutlinePath, new Vector4(18, 18, 18, 18));
            ConfigureSpriteImporter(CircleSpritePath, Vector4.zero);
            ConfigureSpriteImporter(WhiteSpritePath, Vector4.zero);

            // Import toàn bộ PNG có sẵn trong Icons và Sprites sang Sprite
            ImportAllPngsAsSprites(IconsDir);
            ImportAllPngsAsSprites(SpritesDir);

            return new BuiltSprites
            {
                RoundedRect = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath),
                RoundedRectSm = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectSmPath),
                RoundedOutline = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedOutlinePath),
                Circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath),
                White = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath),
                ScoreFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ScoreFramePath)
            };
        }

        static void ImportAllPngsAsSprites(string folder)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                string assetPath = file.Replace('\\', '/');
                if (assetPath == RoundedRectPath || assetPath == RoundedRectSmPath || assetPath == RoundedOutlinePath)
                    continue;
                ConfigureSpriteImporter(assetPath, Vector4.zero);
            }
        }

        static void ConfigureSpriteImporter(string assetPath, Vector4 border)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;

            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
            if (imp.filterMode != FilterMode.Bilinear) { imp.filterMode = FilterMode.Bilinear; dirty = true; }
            if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (imp.spriteBorder != border) { imp.spriteBorder = border; dirty = true; }

            if (dirty) imp.SaveAndReimport();
        }

        static void WriteRoundedRectPng(string assetPath, int size, int radius, int outlineThickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float r = radius;
            float bx = half - r - 1f;
            float by = half - r - 1f;

            for (int y = 0; y < size; y++)
            {
                float py = Mathf.Abs((y + 0.5f) - half) - by;
                float qy = Mathf.Max(py, 0f);
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs((x + 0.5f) - half) - bx;
                    float qx = Mathf.Max(px, 0f);
                    float dist = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(px, py), 0f) - r;
                    float outer = Mathf.Clamp01(0.5f - dist);
                    float alpha = outlineThickness > 0
                        ? Mathf.Clamp01(outer - Mathf.Clamp01(0.5f - (dist + outlineThickness)))
                        : outer;
                    pixels[row + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void WriteCirclePng(string assetPath, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float r = half - 1.5f;

            for (int y = 0; y < size; y++)
            {
                float dy = (y + 0.5f) - half;
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[row + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void WriteWhitePng(string assetPath, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        #endregion

        #region Step D - Build UIRoot.prefab

        static void BuildUIRootPrefab(BuiltFonts fonts, BuiltSprites sprites)
        {
            var root = new GameObject("UIRoot");
            try
            {
                var controller = root.AddComponent<UIRootController>();
                controller.planeDistance = 5f;

                // EventSystem dùng InputSystemUIInputModule (KHÔNG dùng StandaloneInputModule)
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(root.transform, false);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();

                // MainCanvas (Screen Space - Camera, 1080x1080, match 0.5)
                var canvasGo = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(root.transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.planeDistance = 5f;
                canvas.sortingOrder = 10;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                // HudGroup chứa toàn bộ HUD (F2 ẩn/hiện thông qua rootGroup; SettingsMenu nằm ngoài để F10 luôn mở được)
                var hudGroupGo = CreateStretchChild("HudGroup", canvasGo.transform);
                var hudGroup = hudGroupGo.AddComponent<CanvasGroup>();

                // 1. FighterNameplates (UI-3.3 - sau cùng để không che HUD)
                var nameplates = BuildFighterNameplatePool(hudGroupGo.transform, canvas, fonts);

                // 2. CoreHealthBars (UI-3.2 - nổi trên 2 lõi)
                var coreBars = BuildCoreHealthBars(hudGroupGo.transform, canvas, fonts, sprites);

                // 3. LiveModeRoot (UI-4 - ẩn ngay khi chuyển chế độ Thường)
                var liveModeRoot = CreateStretchChild("LiveModeRoot", hudGroupGo.transform);
                BuildGuideTicker(liveModeRoot.transform, fonts, sprites);
                BuildNoticeFeed(liveModeRoot.transform, fonts, sprites);
                BuildGiftBanner(liveModeRoot.transform, fonts, sprites);
                BuildMatchEventBanner(liveModeRoot.transform, fonts, sprites);
                BuildWinBanner(liveModeRoot.transform, fonts, sprites);

                // 4. ScoreHud (UI-3.1 - góc trên trái, luôn hiện)
                var scoreHud = BuildScoreHud(hudGroupGo.transform, fonts, sprites);

                // 5. StatusBadge (UI-3.4 - góc dưới phải, mờ nhẹ, luôn hiện)
                var statusBadge = BuildStatusBadge(hudGroupGo.transform, fonts, sprites);

                // 6. SettingsMenu (UI-5 - NGOÀI LiveModeRoot, luôn nằm trên cùng)
                BuildSettingsMenu(canvasGo.transform, fonts, sprites);

                // Wire UIRootController
                controller.mainCanvas = canvas;
                controller.rootGroup = hudGroup;
                controller.liveModeRoot = liveModeRoot;
                controller.scoreHudView = scoreHud;
                controller.coreHealthBarsView = coreBars;
                controller.fighterNameplatePool = nameplates;
                controller.statusBadgeView = statusBadge;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static FighterNameplatePool BuildFighterNameplatePool(Transform parent, Canvas canvas, BuiltFonts fonts)
        {
            var go = CreateStretchChild("FighterNameplates", parent);
            var pool = go.AddComponent<FighterNameplatePool>();
            pool.containerRect = go.GetComponent<RectTransform>();
            pool.parentCanvas = canvas;
            pool.fallbackFont = fonts.Bold;
            pool.poolSize = 200;
            pool.normalWorldHeight = 2.6f;
            pool.superWorldHeight = 4.4f;
            pool.mainFontSize = 19f;
            pool.superFontSize = 22f;
            pool.subFontSize = 13.5f;

            // Template TMP_Text gắn sẵn font BeVietnamPro-Bold SDF + Outline preset
            var tplGo = new GameObject("NameplateTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
            tplGo.transform.SetParent(go.transform, false);
            var tplRt = tplGo.GetComponent<RectTransform>();
            tplRt.anchorMin = new Vector2(0.5f, 0.5f);
            tplRt.anchorMax = new Vector2(0.5f, 0.5f);
            tplRt.pivot = new Vector2(0.5f, 0f);
            tplRt.sizeDelta = new Vector2(240f, 32f);

            var tplTmp = tplGo.GetComponent<TextMeshProUGUI>();
            tplTmp.font = fonts.Bold;
            if (fonts.BoldOutlineMat != null) tplTmp.fontSharedMaterial = fonts.BoldOutlineMat;
            tplTmp.fontSize = 19f;
            tplTmp.alignment = TextAlignmentOptions.Bottom;
            tplTmp.textWrappingMode = TextWrappingModes.NoWrap;
            tplTmp.overflowMode = TextOverflowModes.Overflow;
            tplTmp.raycastTarget = false;
            tplTmp.maskable = false;
            tplTmp.richText = false;
            tplTmp.text = "Tên";
            tplGo.SetActive(false);

            pool.labelTemplate = tplTmp;
            return pool;
        }

        static CoreHealthBarsView BuildCoreHealthBars(Transform parent, Canvas canvas, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = CreateStretchChild("CoreHealthBars", parent);
            var view = go.AddComponent<CoreHealthBarsView>();
            view.containerRect = go.GetComponent<RectTransform>();
            view.parentCanvas = canvas;
            view.worldOffset = new Vector3(0f, 4.5f, 0f);
            view.blueBar = BuildSingleCoreBar(go.transform, "BlueCoreBar", "LÕI XANH", UIStyleHelper.BlueColor, fonts, sprites);
            view.redBar = BuildSingleCoreBar(go.transform, "RedCoreBar", "LÕI ĐỎ", UIStyleHelper.RedColor, fonts, sprites);
            return view;
        }

        static CoreHealthBarsView.CoreBarWidgets BuildSingleCoreBar(
            Transform parent, string name, string tagLabel, Color teamColor, BuiltFonts fonts, BuiltSprites sprites)
        {
            var rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(226f, 32f);

            var shakeGo = CreateStretchChild("ShakeTarget", rootGo.transform);
            var shakeRt = shakeGo.GetComponent<RectTransform>();

            // Nền tối bo góc
            var bgImg = CreateImage("Background", shakeGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x09, 0x0E, 0x1A, 0xDF), false);
            Stretch(bgImg.rectTransform);

            // Vùng chứa thanh fill (lùi vào 4px)
            var trackGo = CreateStretchChild("FillTrack", shakeGo.transform, 4f, 4f, 4f, 4f);

            var trailImg = CreateImage("TrailFill", trackGo.transform, sprites.White, Image.Type.Filled, new Color(1f, 1f, 1f, 0.85f), false);
            trailImg.fillMethod = Image.FillMethod.Horizontal;
            trailImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            trailImg.fillAmount = 1f;
            Stretch(trailImg.rectTransform);

            var mainImg = CreateImage("MainFill", trackGo.transform, sprites.White, Image.Type.Filled, teamColor, false);
            mainImg.fillMethod = Image.FillMethod.Horizontal;
            mainImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            mainImg.fillAmount = 1f;
            Stretch(mainImg.rectTransform);

            // Lớp nháy trắng khi trúng đòn
            var flashImg = CreateImage("HitFlash", shakeGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color(1f, 1f, 1f, 0f), false);
            Stretch(flashImg.rectTransform);

            // Viền bo góc (đổi đỏ nhấp nháy khi < 20%)
            var borderImg = CreateImage("Border", shakeGo.transform, sprites.RoundedOutline, Image.Type.Sliced, UIStyleHelper.PanelBorderColor, false);
            Stretch(borderImg.rectTransform);

            // Nhãn phe nhỏ phía trên thanh máu
            var tagTmp = CreateTmp("TeamTag", shakeGo.transform, tagLabel, fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 12.5f, teamColor, TextAlignmentOptions.Bottom);
            tagTmp.rectTransform.anchorMin = new Vector2(0f, 1f);
            tagTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            tagTmp.rectTransform.pivot = new Vector2(0.5f, 0f);
            tagTmp.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            tagTmp.rectTransform.sizeDelta = new Vector2(0f, 18f);

            // Số máu ở giữa thanh
            var hpTmp = CreateTmp("HpText", shakeGo.transform, "1000 / 1000", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 15.5f, Color.white, TextAlignmentOptions.Center);
            Stretch(hpTmp.rectTransform);

            return new CoreHealthBarsView.CoreBarWidgets
            {
                root = rootRt,
                shakeTarget = shakeRt,
                backgroundImage = bgImg,
                borderImage = borderImg,
                trailFillImage = trailImg,
                mainFillImage = mainImg,
                hitFlashOverlay = flashImg,
                hpText = hpTmp,
                teamTagText = tagTmp
            };
        }

        static ScoreHudView BuildScoreHud(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var rootGo = new GameObject("ScoreHud", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = new Vector2(16f, -12f);
            rootRt.sizeDelta = new Vector2(436f, 118f);

            var view = rootGo.AddComponent<ScoreHudView>();

            // Khung tỉ số chính (dùng sprite score_frame.png 800x160 nếu có, fallback dark rounded card)
            var panelGo = new GameObject("ScorePanel", typeof(RectTransform));
            panelGo.transform.SetParent(rootGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(0f, 88f);
            view.panelRoot = panelRt;

            bool useCustomFrame = sprites.ScoreFrame != null;
            if (useCustomFrame)
            {
                var bgImg = CreateImage("Backdrop", panelGo.transform, sprites.ScoreFrame, Image.Type.Simple, Color.white, false);
                bgImg.preserveAspect = true;
                Stretch(bgImg.rectTransform);
                view.backdropImage = bgImg;

                // Khung trái khớp chính xác ô trắng bên trái của score_frame.png (x: 0.205..0.449, y: 0.144..0.519)
                var leftBox = CreateAnchoredChild("BlueBox", panelGo.transform, new Vector2(0.195f, 0.12f), new Vector2(0.455f, 0.56f));
                var leftLayout = leftBox.AddComponent<HorizontalLayoutGroup>();
                leftLayout.childAlignment = TextAnchor.MiddleCenter;
                leftLayout.spacing = 6f;
                leftLayout.childControlWidth = false;
                leftLayout.childControlHeight = true;
                leftLayout.childForceExpandWidth = false;
                leftLayout.childForceExpandHeight = false;

                view.blueLabelText = CreateTmp("BlueLabel", leftBox.transform, "XANH", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 15f, UIStyleHelper.BlueColor, TextAlignmentOptions.MidlineRight);
                view.blueLabelText.rectTransform.sizeDelta = new Vector2(54f, 34f);

                view.blueScoreText = CreateTmp("BlueScore", leftBox.transform, "0", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 29f, UIStyleHelper.BlueColor, TextAlignmentOptions.MidlineLeft);
                view.blueScoreText.rectTransform.sizeDelta = new Vector2(44f, 38f);

                // Khung phải khớp ô trắng bên phải của score_frame.png (x: 0.554..0.791, y: 0.144..0.519)
                var rightBox = CreateAnchoredChild("RedBox", panelGo.transform, new Vector2(0.545f, 0.12f), new Vector2(0.805f, 0.56f));
                var rightLayout = rightBox.AddComponent<HorizontalLayoutGroup>();
                rightLayout.childAlignment = TextAnchor.MiddleCenter;
                rightLayout.spacing = 6f;
                rightLayout.childControlWidth = false;
                rightLayout.childControlHeight = true;
                rightLayout.childForceExpandWidth = false;
                rightLayout.childForceExpandHeight = false;

                view.redScoreText = CreateTmp("RedScore", rightBox.transform, "0", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 29f, UIStyleHelper.RedColor, TextAlignmentOptions.MidlineRight);
                view.redScoreText.rectTransform.sizeDelta = new Vector2(44f, 38f);

                view.redLabelText = CreateTmp("RedLabel", rightBox.transform, "ĐỎ", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 15f, UIStyleHelper.RedColor, TextAlignmentOptions.MidlineLeft);
                view.redLabelText.rectTransform.sizeDelta = new Vector2(40f, 34f);
            }
            else
            {
                var bgImg = CreateImage("Backdrop", panelGo.transform, sprites.RoundedRect, Image.Type.Sliced, UIStyleHelper.PanelDarkColor, false);
                Stretch(bgImg.rectTransform);
                view.backdropImage = bgImg;

                var borderImg = CreateImage("Border", panelGo.transform, sprites.RoundedOutline, Image.Type.Sliced, UIStyleHelper.PanelBorderColor, false);
                Stretch(borderImg.rectTransform);
                view.borderImage = borderImg;

                var row = CreateStretchChild("Row", panelGo.transform, 16f, 16f, 6f, 6f);
                var hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 10f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;

                view.blueLabelText = CreateTmp("BlueLabel", row.transform, "XANH", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 24f, UIStyleHelper.BlueColor, TextAlignmentOptions.Center);
                view.blueLabelText.rectTransform.sizeDelta = new Vector2(82f, 44f);
                view.blueScoreText = CreateTmp("BlueScore", row.transform, "0", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 40f, UIStyleHelper.BlueColor, TextAlignmentOptions.Center);
                view.blueScoreText.rectTransform.sizeDelta = new Vector2(54f, 48f);
                view.separatorText = CreateTmp("Separator", row.transform, ":", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 36f, Color.white, TextAlignmentOptions.Center);
                view.separatorText.rectTransform.sizeDelta = new Vector2(20f, 44f);
                view.redScoreText = CreateTmp("RedScore", row.transform, "0", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 40f, UIStyleHelper.RedColor, TextAlignmentOptions.Center);
                view.redScoreText.rectTransform.sizeDelta = new Vector2(54f, 48f);
                view.redLabelText = CreateTmp("RedLabel", row.transform, "ĐỎ", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 24f, UIStyleHelper.RedColor, TextAlignmentOptions.Center);
                view.redLabelText.rectTransform.sizeDelta = new Vector2(54f, 44f);
            }

            // Hàng đếm số lính nhỏ gọn phía dưới bảng tỉ số
            var countRowGo = new GameObject("FighterCountRow", typeof(RectTransform));
            countRowGo.transform.SetParent(rootGo.transform, false);
            var countRt = countRowGo.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0.5f, 0f);
            countRt.anchorMax = new Vector2(0.5f, 0f);
            countRt.pivot = new Vector2(0.5f, 0f);
            countRt.anchoredPosition = new Vector2(0f, 2f);
            countRt.sizeDelta = new Vector2(260f, 26f);

            var countLayout = countRowGo.AddComponent<HorizontalLayoutGroup>();
            countLayout.childAlignment = TextAnchor.MiddleCenter;
            countLayout.spacing = 18f;
            countLayout.childControlWidth = false;
            countLayout.childControlHeight = true;
            countLayout.childForceExpandWidth = false;
            countLayout.childForceExpandHeight = false;

            view.fighterCountRow = countRowGo;
            BuildFighterCountPill(countRowGo.transform, "BlueCountPill", UIStyleHelper.BlueDarkColor, UIStyleHelper.BlueColor, fonts, sprites, out view.blueCountPill, out view.blueCountText);
            BuildFighterCountPill(countRowGo.transform, "RedCountPill", UIStyleHelper.RedDarkColor, UIStyleHelper.RedColor, fonts, sprites, out view.redCountPill, out view.redCountText);

            return view;
        }

        static void BuildFighterCountPill(
            Transform parent, string name, Color bgColor, Color dotColor, BuiltFonts fonts, BuiltSprites sprites,
            out Image pillImg, out TMP_Text countTmp)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, 24f);

            pillImg = go.AddComponent<Image>();
            pillImg.sprite = sprites.RoundedRectSm;
            pillImg.type = Image.Type.Sliced;
            pillImg.color = bgColor;
            pillImg.raycastTarget = false;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 2, 2);
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var dot = CreateImage("Dot", go.transform, sprites.Circle, Image.Type.Simple, dotColor, false);
            dot.rectTransform.sizeDelta = new Vector2(10f, 10f);

            countTmp = CreateTmp("Count", go.transform, "0", fonts.Bold, null, 14f, Color.white, TextAlignmentOptions.Center);
            countTmp.rectTransform.sizeDelta = new Vector2(36f, 20f);

            var unit = CreateTmp("Unit", go.transform, "lính", fonts.SemiBold, null, 11.5f, UIStyleHelper.TextMutedColor, TextAlignmentOptions.Left);
            unit.rectTransform.sizeDelta = new Vector2(24f, 20f);
        }

        static StatusBadgeView BuildStatusBadge(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 8f);
            rt.sizeDelta = new Vector2(346f, 24f);

            var view = go.AddComponent<StatusBadgeView>();
            view.badgeGroup = go.GetComponent<CanvasGroup>();
            view.idleAlpha = 0.72f;

            var bg = CreateImage("Backdrop", go.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x08, 0x0B, 0x14, 0xB8), false);
            Stretch(bg.rectTransform);
            view.backdropImage = bg;

            var row = CreateStretchChild("Row", go.transform, 10f, 10f, 2f, 2f);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.spacing = 7f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;

            var dot = CreateImage("StatusDot", row.transform, sprites.Circle, Image.Type.Simple, UIStyleHelper.StatusDemoColor, false);
            dot.rectTransform.sizeDelta = new Vector2(9f, 9f);
            view.statusDotImage = dot;

            var txt = CreateTmp("StatusText", row.transform, "LIVE TỰ ĐỘNG · demo · 60 FPS · 0 lính", fonts.SemiBold, null, 12.5f, UIStyleHelper.TextMutedColor, TextAlignmentOptions.MidlineRight);
            txt.rectTransform.sizeDelta = new Vector2(304f, 20f);
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Ellipsis;
            view.statusText = txt;

            return view;
        }

        static void BuildGuideTicker(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("GuideTicker", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 38f);
            rt.sizeDelta = new Vector2(760f, 44f);

            var view = go.AddComponent<GuideTickerView>();

            // BarRoot tách khỏi script GO để GuideTickerView bật/tắt sạch khi danh sách rỗng
            var barGo = CreateStretchChild("BarRoot", go.transform);
            var barGroup = barGo.AddComponent<CanvasGroup>();
            barGroup.blocksRaycasts = false;
            barGroup.interactable = false;

            var bg = CreateImage("Background", barGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color32(0x0B, 0x11, 0x20, 0xCC), false);
            Stretch(bg.rectTransform);

            var border = CreateImage("Border", barGo.transform, sprites.RoundedOutline, Image.Type.Sliced, new Color32(0xFF, 0xD4, 0x3B, 0x55), false);
            Stretch(border.rectTransform);

            // Mask để chữ trượt mượt bên trong thanh
            var maskGo = CreateStretchChild("TextMask", barGo.transform, 22f, 22f, 4f, 4f);
            maskGo.AddComponent<RectMask2D>();

            var txtTmp = CreateTmp("GuideText", maskGo.transform, "Comment XANH hoặc ĐỎ để vào trận!", fonts.Bold, null, 19f, new Color32(0xF8, 0xFA, 0xFC, 0xFF), TextAlignmentOptions.Center);
            Stretch(txtTmp.rectTransform);
            txtTmp.textWrappingMode = TextWrappingModes.NoWrap;
            txtTmp.overflowMode = TextOverflowModes.Ellipsis;
            var txtGroup = txtTmp.gameObject.AddComponent<CanvasGroup>();

            view.barRoot = barGo;
            view.barGroup = barGroup;
            view.textGroup = txtGroup;
            view.textRect = txtTmp.rectTransform;
            view.guideText = txtTmp;
            view.cycleSeconds = 6f;
            view.transitionDuration = 0.45f;
            view.slideOffsetY = 14f;
        }

        static void BuildNoticeFeed(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("NoticeFeed", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 92f);
            rt.sizeDelta = new Vector2(640f, 174f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.spacing = 6f;
            vlg.reverseArrangement = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var view = go.AddComponent<NoticeFeedView>();
            view.slots = new NoticeFeedView.NoticeRowSlot[4];

            for (int i = 0; i < 4; i++)
            {
                var rowGo = new GameObject($"NoticeSlot_{i}", typeof(RectTransform), typeof(CanvasGroup));
                rowGo.transform.SetParent(go.transform, false);
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(640f, 36f);

                var rowCg = rowGo.GetComponent<CanvasGroup>();
                rowCg.alpha = 0f;
                rowCg.blocksRaycasts = false;
                rowCg.interactable = false;

                var bgImg = CreateImage("Background", rowGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x0D, 0x17, 0x26, 0xCC), false);
                Stretch(bgImg.rectTransform);

                var accentImg = CreateImage("AccentBar", rowGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x4D, 0xD0, 0xE1, 0xFF), false);
                accentImg.rectTransform.anchorMin = new Vector2(0f, 0.14f);
                accentImg.rectTransform.anchorMax = new Vector2(0f, 0.86f);
                accentImg.rectTransform.pivot = new Vector2(0f, 0.5f);
                accentImg.rectTransform.anchoredPosition = new Vector2(6f, 0f);
                accentImg.rectTransform.sizeDelta = new Vector2(5f, 0f);

                var lblTmp = CreateTmp("Label", rowGo.transform, string.Empty, fonts.Bold, null, 17f, Color.white, TextAlignmentOptions.MidlineLeft);
                Stretch(lblTmp.rectTransform, 20f, 14f, 2f, 2f);
                lblTmp.textWrappingMode = TextWrappingModes.NoWrap;
                lblTmp.overflowMode = TextOverflowModes.Ellipsis;

                rowGo.SetActive(false);

                view.slots[i] = new NoticeFeedView.NoticeRowSlot
                {
                    root = rowGo,
                    canvasGroup = rowCg,
                    background = bgImg,
                    accentBar = accentImg,
                    label = lblTmp
                };
            }
        }

        static void BuildGiftBanner(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("GiftBanner", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -198f);
            rt.sizeDelta = new Vector2(540f, 92f);

            var view = go.AddComponent<GiftBannerView>();

            var bannerGo = CreateStretchChild("BannerRoot", go.transform);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            var cg = bannerGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var glowImg = CreateImage("AccentGlow", bannerGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color(0.18f, 0.49f, 1f, 0.35f), false);
            Stretch(glowImg.rectTransform, -5f, -5f, -5f, -5f);

            var bgImg = CreateImage("Background", bannerGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color32(0x0B, 0x1E, 0x3D, 0xEA), false);
            Stretch(bgImg.rectTransform);

            var frameImg = CreateImage("Frame", bannerGo.transform, sprites.RoundedOutline, Image.Type.Sliced, UIStyleHelper.BlueColor, false);
            Stretch(frameImg.rectTransform);

            // Layout nội dung ngang: Icon bên trái + thông tin bên phải
            var contentGo = CreateStretchChild("Content", bannerGo.transform, 14f, 16f, 8f, 8f);
            var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;

            // Khung icon (tự ẩn khi không có icon)
            var iconContainerGo = new GameObject("IconContainer", typeof(RectTransform));
            iconContainerGo.transform.SetParent(contentGo.transform, false);
            iconContainerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(74f, 74f);
            var iconBg = CreateImage("IconBackdrop", iconContainerGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color(1f, 1f, 1f, 0.1f), false);
            Stretch(iconBg.rectTransform);
            var iconImg = CreateImage("IconImage", iconContainerGo.transform, null, Image.Type.Simple, Color.white, false);
            iconImg.preserveAspect = true;
            Stretch(iconImg.rectTransform, 4f, 4f, 4f, 4f);

            // Cột chữ bên phải
            var textCol = new GameObject("TextColumn", typeof(RectTransform));
            textCol.transform.SetParent(contentGo.transform, false);
            textCol.GetComponent<RectTransform>().sizeDelta = new Vector2(416f, 74f);
            var vlg = textCol.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 1f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            var teamBadge = CreateTmp("TeamBadgeText", textCol.transform, "PHE XANH", fonts.ExtraBold, null, 13f, UIStyleHelper.BlueColor, TextAlignmentOptions.MidlineLeft);
            teamBadge.rectTransform.sizeDelta = new Vector2(0f, 18f);

            var senderTmp = CreateTmp("SenderText", textCol.transform, "Người xem", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            senderTmp.rectTransform.sizeDelta = new Vector2(0f, 28f);
            senderTmp.textWrappingMode = TextWrappingModes.NoWrap;
            senderTmp.overflowMode = TextOverflowModes.Ellipsis;

            var effectTmp = CreateTmp("EffectText", textCol.transform, "Xe tăng", fonts.Bold, null, 18f, UIStyleHelper.GoldColor, TextAlignmentOptions.MidlineLeft);
            effectTmp.rectTransform.sizeDelta = new Vector2(0f, 24f);
            effectTmp.textWrappingMode = TextWrappingModes.NoWrap;
            effectTmp.overflowMode = TextOverflowModes.Ellipsis;

            bannerGo.SetActive(false);

            // Nối tất cả 10 icon hiệu ứng từ Assets/_Game/UI/Icons/
            var iconEntries = new List<GiftBannerView.EffectIconEntry>(EffectIconIds.Length);
            foreach (string id in EffectIconIds)
            {
                string path = $"{IconsDir}/icon_{id}.png";
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                iconEntries.Add(new GiftBannerView.EffectIconEntry
                {
                    effectId = id,
                    sprite = sp
                });
                if (sp == null)
                {
                    Debug.LogWarning($"[UISetupBuilder] Không tìm thấy sprite icon: {path}");
                }
            }

            view.bannerRoot = bannerGo;
            view.animatedRect = bannerRt;
            view.canvasGroup = cg;
            view.frameImage = frameImg;
            view.backgroundImage = bgImg;
            view.accentGlowImage = glowImg;
            view.iconContainer = iconContainerGo;
            view.iconImage = iconImg;
            view.senderText = senderTmp;
            view.effectText = effectTmp;
            view.teamBadgeText = teamBadge;
            view.icons = iconEntries.ToArray();
        }

        static void BuildMatchEventBanner(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("MatchEventBanner", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // Đặt dưới hẳn ScoreHud + FighterCountRow (y = -136) để không bao giờ đè lên ScoreHud góc trên trái
            rt.anchoredPosition = new Vector2(0f, -136f);
            rt.sizeDelta = new Vector2(460f, 52f);

            var view = go.AddComponent<MatchEventBannerView>();

            var bannerGo = CreateStretchChild("BannerRoot", go.transform);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            var cg = bannerGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bgImg = CreateImage("Background", bannerGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color32(0x2A, 0x20, 0x05, 0xE6), false);
            Stretch(bgImg.rectTransform);

            var borderImg = CreateImage("Border", bannerGo.transform, sprites.RoundedOutline, Image.Type.Sliced, UIStyleHelper.GoldColor, false);
            Stretch(borderImg.rectTransform);

            var row = CreateStretchChild("Content", bannerGo.transform, 18f, 18f, 4f, 4f);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 12f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;

            var titleTmp = CreateTmp("TitleText", row.transform, "X2 SỨC MẠNH!", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 22f, UIStyleHelper.GoldColor, TextAlignmentOptions.Center);
            titleTmp.rectTransform.sizeDelta = new Vector2(320f, 42f);
            titleTmp.textWrappingMode = TextWrappingModes.NoWrap;
            titleTmp.overflowMode = TextOverflowModes.Ellipsis;

            var cdTmp = CreateTmp("CountdownText", row.transform, "30s", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 24f, Color.white, TextAlignmentOptions.Center);
            cdTmp.rectTransform.sizeDelta = new Vector2(68f, 42f);

            bannerGo.SetActive(false);

            view.bannerRoot = bannerGo;
            view.animatedRect = bannerRt;
            view.canvasGroup = cg;
            view.backgroundImage = bgImg;
            view.borderImage = borderImg;
            view.titleText = titleTmp;
            view.countdownText = cdTmp;
        }

        static void BuildWinBanner(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject("WinBanner", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 48f);
            rt.sizeDelta = new Vector2(720f, 188f);

            var view = go.AddComponent<WinBannerView>();

            var bannerGo = CreateStretchChild("BannerRoot", go.transform);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            var cg = bannerGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bgImg = CreateImage("Background", bannerGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color32(0x0A, 0x1E, 0x42, 0xF0), false);
            Stretch(bgImg.rectTransform);

            var ribbonImg = CreateImage("AccentRibbon", bannerGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, UIStyleHelper.BlueColor, false);
            ribbonImg.rectTransform.anchorMin = new Vector2(0.06f, 1f);
            ribbonImg.rectTransform.anchorMax = new Vector2(0.94f, 1f);
            ribbonImg.rectTransform.pivot = new Vector2(0.5f, 1f);
            ribbonImg.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            ribbonImg.rectTransform.sizeDelta = new Vector2(0f, 8f);

            var borderImg = CreateImage("Border", bannerGo.transform, sprites.RoundedOutline, Image.Type.Sliced, UIStyleHelper.BlueColor, false);
            Stretch(borderImg.rectTransform);

            var col = CreateStretchChild("Content", bannerGo.transform, 28f, 28f, 20f, 18f);
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            var titleTmp = CreateTmp("TitleText", col.transform, "PHE XANH THẮNG!", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 48f, Color.white, TextAlignmentOptions.Center);
            titleTmp.rectTransform.sizeDelta = new Vector2(0f, 66f);

            var mvpBox = new GameObject("MvpContainer", typeof(RectTransform));
            mvpBox.transform.SetParent(col.transform, false);
            mvpBox.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
            var mvpBg = CreateImage("MvpBackdrop", mvpBox.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x00, 0x00, 0x00, 0x70), false);
            Stretch(mvpBg.rectTransform, 64f, 64f, 0f, 0f);
            var mvpTmp = CreateTmp("MvpText", mvpBox.transform, "MVP: Người chơi", fonts.ExtraBold, fonts.ExtraBoldOutlineMat, 25f, UIStyleHelper.HighlightGoldColor, TextAlignmentOptions.Center);
            Stretch(mvpTmp.rectTransform, 72f, 72f, 0f, 0f);
            mvpTmp.textWrappingMode = TextWrappingModes.NoWrap;
            mvpTmp.overflowMode = TextOverflowModes.Ellipsis;

            bannerGo.SetActive(false);

            view.bannerRoot = bannerGo;
            view.animatedRect = bannerRt;
            view.canvasGroup = cg;
            view.backgroundImage = bgImg;
            view.borderImage = borderImg;
            view.accentRibbonImage = ribbonImg;
            view.titleText = titleTmp;
            view.mvpContainer = mvpBox;
            view.mvpText = mvpTmp;
        }

        static void BuildSettingsMenu(Transform parent, BuiltFonts fonts, BuiltSprites sprites)
        {
            // SettingsMenuView nằm trên GO luôn hoạt động, NGOÀI LiveModeRoot
            var hostGo = CreateStretchChild("SettingsMenu", parent);
            var view = hostGo.AddComponent<SettingsMenuView>();

            // ModalRoot tắt sẵn lúc đầu
            var modalGo = CreateStretchChild("ModalRoot", hostGo.transform);

            // Dim nền toàn khung vuông chặn click xuống game
            var dimImg = CreateImage("BackdropDim", modalGo.transform, sprites.White, Image.Type.Simple, new Color32(0x04, 0x07, 0x10, 0xCC), true);
            Stretch(dimImg.rectTransform);

            // Hộp thoại cài đặt chính giữa
            var cardGo = new GameObject("DialogCard", typeof(RectTransform));
            cardGo.transform.SetParent(modalGo.transform, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(820f, 880f);

            var cardBg = CreateImage("CardBg", cardGo.transform, sprites.RoundedRect, Image.Type.Sliced, new Color32(0x0F, 0x17, 0x2A, 0xF7), true);
            Stretch(cardBg.rectTransform);
            var cardBorder = CreateImage("CardBorder", cardGo.transform, sprites.RoundedOutline, Image.Type.Sliced, new Color32(0x3B, 0x82, 0xF6, 0x88), false);
            Stretch(cardBorder.rectTransform);

            // Header (chiều cao 66)
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(cardGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 66f);

            var headerBg = CreateImage("HeaderBg", headerGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x16, 0x20, 0x36, 0xFF), false);
            Stretch(headerBg.rectTransform, 4f, 4f, 4f, 0f);

            var titleTmp = CreateTmp("Title", headerGo.transform, "CÀI ĐẶT TRẬN ĐẤU (F10)", fonts.ExtraBold, null, 25f, UIStyleHelper.GoldColor, TextAlignmentOptions.MidlineLeft);
            Stretch(titleTmp.rectTransform, 24f, 70f, 6f, 6f);

            view.closeButton = CreateButton("CloseButton", headerGo.transform, "X", fonts.ExtraBold, 20f, sprites.RoundedRectSm,
                new Color32(0x33, 0x41, 0x55, 0xFF), Color.white, new Vector2(40f, 40f));
            var closeRt = view.closeButton.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-16f, 0f);

            // ScrollRect Body (giữa header 66 và footer 116)
            var scrollGo = CreateStretchChild("ScrollView", cardGo.transform, 18f, 18f, 76f, 120f);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            var viewportGo = CreateStretchChild("Viewport", scrollGo.transform, 0f, 16f, 0f, 0f);
            viewportGo.AddComponent<RectMask2D>();
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0f);
            vpImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(8, 8, 8, 16);
            contentVlg.spacing = 10f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Scrollbar dọc
            var sbGo = new GameObject("ScrollbarVertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(scrollGo.transform, false);
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = Vector2.zero;
            sbRt.sizeDelta = new Vector2(10f, 0f);
            var sbBg = sbGo.GetComponent<Image>();
            sbBg.sprite = sprites.RoundedRectSm;
            sbBg.type = Image.Type.Sliced;
            sbBg.color = new Color32(0x1E, 0x29, 0x3B, 0xB0);

            var slideArea = CreateStretchChild("SlidingArea", sbGo.transform);
            var handleImg = CreateImage("Handle", slideArea.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x64, 0x74, 0x8B, 0xDD), true);
            Stretch(handleImg.rectTransform);
            var sb = sbGo.GetComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.handleRect = handleImg.rectTransform;
            sb.targetGraphic = handleImg;

            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content = contentRt;
            scrollRect.verticalScrollbar = sb;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // --- Các nhóm cài đặt bên trong Content ---
            CreateSettingsSectionHeader(contentGo.transform, "CHẾ ĐỘ & CỬA SỔ", fonts);
            view.modeDropdown = CreateSettingsDropdownRow(contentGo.transform, "Chế độ hoạt động", new[] { "Live tự động", "Thường" }, fonts, sprites);
            view.musicDropdown = CreateSettingsDropdownRow(contentGo.transform, "Nhạc nền (phím M)", new[] { "Bật", "Tắt" }, fonts, sprites);
            view.sfxDropdown = CreateSettingsDropdownRow(contentGo.transform, "Âm thanh hiệu ứng, đấm nhau (phím N)", new[] { "Bật", "Tắt" }, fonts, sprites);
            view.windowSizeDropdown = CreateSettingsDropdownRow(contentGo.transform, "Kích thước cửa sổ (vuông)", new[] { "720 x 720", "1080 x 1080", "1440 x 1440" }, fonts, sprites);

            CreateSettingsSectionHeader(contentGo.transform, "CHIẾN TRƯỜNG & HIỂN THỊ", fonts);
            view.coreMaxHpInput = CreateSettingsInputRow(contentGo.transform, "Máu tối đa của lõi", "1000", TMP_InputField.ContentType.DecimalNumber, fonts, sprites);
            view.hardCapInput = CreateSettingsInputRow(contentGo.transform, "Tổng số lính tối đa (Hard Cap)", "200", TMP_InputField.ContentType.IntegerNumber, fonts, sprites);
            view.fullNameLimitInput = CreateSettingsInputRow(contentGo.transform, "Ngưỡng hiện tên đầy đủ (số lính)", "40", TMP_InputField.ContentType.IntegerNumber, fonts, sprites);

            CreateSettingsSectionHeader(contentGo.transform, "THẢM HỌA & SỰ KIỆN X2", fonts);
            view.disasterThresholdInput = CreateSettingsInputRow(contentGo.transform, "Ngưỡng kích hoạt thảm họa (số lính)", "120", TMP_InputField.ContentType.IntegerNumber, fonts, sprites);
            view.disasterKillPercentInput = CreateSettingsInputRow(contentGo.transform, "Tỉ lệ quét lính khi thảm họa (%)", "40", TMP_InputField.ContentType.DecimalNumber, fonts, sprites);
            view.x2IntervalMinInput = CreateSettingsInputRow(contentGo.transform, "Chu kỳ X2 tối thiểu (giây)", "180", TMP_InputField.ContentType.DecimalNumber, fonts, sprites);
            view.x2IntervalMaxInput = CreateSettingsInputRow(contentGo.transform, "Chu kỳ X2 tối đa (giây)", "300", TMP_InputField.ContentType.DecimalNumber, fonts, sprites);
            view.x2DurationInput = CreateSettingsInputRow(contentGo.transform, "Thời lượng X2 toàn sân (giây)", "30", TMP_InputField.ContentType.DecimalNumber, fonts, sprites);

            // Nhóm GuideLines động
            var guideHeaderGo = new GameObject("GuideLinesHeader", typeof(RectTransform), typeof(LayoutElement));
            guideHeaderGo.transform.SetParent(contentGo.transform, false);
            guideHeaderGo.GetComponent<LayoutElement>().preferredHeight = 40f;
            var ghHlg = guideHeaderGo.AddComponent<HorizontalLayoutGroup>();
            ghHlg.childAlignment = TextAnchor.MiddleLeft;
            ghHlg.spacing = 10f;
            ghHlg.childControlWidth = true;
            ghHlg.childControlHeight = true;
            ghHlg.childForceExpandWidth = false;
            ghHlg.childForceExpandHeight = false;

            var guideSecLabel = CreateTmp("GuideHeaderLabel", guideHeaderGo.transform, "DẢI HƯỚNG DẪN (LIVE TỰ ĐỘNG)", fonts.ExtraBold, null, 15.5f, UIStyleHelper.GoldColor, TextAlignmentOptions.MidlineLeft);
            var glLe = guideSecLabel.gameObject.AddComponent<LayoutElement>();
            glLe.flexibleWidth = 1f;
            glLe.preferredHeight = 34f;

            view.addGuideLineButton = CreateButton("AddGuideLineButton", guideHeaderGo.transform, "+ Thêm dòng", fonts.Bold, 15f, sprites.RoundedRectSm,
                new Color32(0x16, 0xA3, 0x4A, 0xFF), Color.white, new Vector2(140f, 34f));
            var addBtnLe = view.addGuideLineButton.gameObject.AddComponent<LayoutElement>();
            addBtnLe.preferredWidth = 140f;
            addBtnLe.preferredHeight = 34f;

            var guideContainerGo = new GameObject("GuideLinesContainer", typeof(RectTransform));
            guideContainerGo.transform.SetParent(contentGo.transform, false);
            var gcVlg = guideContainerGo.AddComponent<VerticalLayoutGroup>();
            gcVlg.spacing = 8f;
            gcVlg.childControlWidth = true;
            gcVlg.childControlHeight = true;
            gcVlg.childForceExpandWidth = true;
            gcVlg.childForceExpandHeight = false;

            // Template dòng guideLine (ẩn sẵn, gồm 1 TMP_InputField + 1 nút Xóa)
            var rowTplGo = new GameObject("GuideLineRowTemplate", typeof(RectTransform), typeof(LayoutElement));
            rowTplGo.transform.SetParent(guideContainerGo.transform, false);
            rowTplGo.GetComponent<LayoutElement>().preferredHeight = 44f;
            var rowHlg = rowTplGo.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 10f;
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            var rowInput = CreateTmpInputField("LineInput", rowTplGo.transform, "Nhập nội dung hướng dẫn...", TMP_InputField.ContentType.Standard, fonts, sprites);
            var rowInputLe = rowInput.gameObject.AddComponent<LayoutElement>();
            rowInputLe.flexibleWidth = 1f;
            rowInputLe.preferredHeight = 42f;

            var delBtn = CreateButton("DeleteButton", rowTplGo.transform, "Xóa", fonts.Bold, 15f, sprites.RoundedRectSm,
                new Color32(0xDC, 0x26, 0x26, 0xFF), Color.white, new Vector2(72f, 42f));
            var delLe = delBtn.gameObject.AddComponent<LayoutElement>();
            delLe.preferredWidth = 72f;
            delLe.preferredHeight = 42f;

            rowTplGo.SetActive(false);

            view.guideLinesContainer = guideContainerGo.transform;
            view.guideLineRowTemplate = rowTplGo;

            // Footer (chiều cao 114)
            var footerGo = new GameObject("Footer", typeof(RectTransform));
            footerGo.transform.SetParent(cardGo.transform, false);
            var footerRt = footerGo.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = Vector2.zero;
            footerRt.sizeDelta = new Vector2(0f, 114f);

            var footerBg = CreateImage("FooterBg", footerGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x16, 0x20, 0x36, 0xFF), false);
            Stretch(footerBg.rectTransform, 4f, 4f, 0f, 4f);

            // Dòng ghi chú bắt buộc đúng nguyên văn
            var noteTmp = CreateTmp("NoteText", footerGo.transform, SettingsMenuView.RequiredNoteText, fonts.SemiBold, null, 14f, new Color32(0xFB, 0xBF, 0x24, 0xFF), TextAlignmentOptions.MidlineLeft);
            noteTmp.rectTransform.anchorMin = new Vector2(0f, 1f);
            noteTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            noteTmp.rectTransform.pivot = new Vector2(0.5f, 1f);
            noteTmp.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            noteTmp.rectTransform.sizeDelta = new Vector2(-40f, 34f);
            noteTmp.textWrappingMode = TextWrappingModes.Normal;
            view.noteText = noteTmp;

            // Hàng nút Lưu / Hủy + phản hồi trạng thái
            var btnRow = new GameObject("ActionRow", typeof(RectTransform));
            btnRow.transform.SetParent(footerGo.transform, false);
            var btnRowRt = btnRow.GetComponent<RectTransform>();
            btnRowRt.anchorMin = new Vector2(0f, 0f);
            btnRowRt.anchorMax = new Vector2(1f, 0f);
            btnRowRt.pivot = new Vector2(0.5f, 0f);
            btnRowRt.anchoredPosition = new Vector2(0f, 14f);
            btnRowRt.sizeDelta = new Vector2(-40f, 48f);

            var statusTmp = CreateTmp("StatusFeedbackText", btnRow.transform, string.Empty, fonts.Bold, null, 16f, UIStyleHelper.StatusConnectedColor, TextAlignmentOptions.MidlineLeft);
            statusTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusTmp.rectTransform.anchorMax = new Vector2(0.52f, 1f);
            statusTmp.rectTransform.offsetMin = Vector2.zero;
            statusTmp.rectTransform.offsetMax = Vector2.zero;
            statusTmp.gameObject.SetActive(false);
            view.statusFeedbackText = statusTmp;

            view.cancelButton = CreateButton("CancelButton", btnRow.transform, "Đóng", fonts.Bold, 17f, sprites.RoundedRectSm,
                new Color32(0x33, 0x41, 0x55, 0xFF), Color.white, new Vector2(126f, 44f));
            var cancelRt = view.cancelButton.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(1f, 0.5f);
            cancelRt.anchorMax = new Vector2(1f, 0.5f);
            cancelRt.pivot = new Vector2(1f, 0.5f);
            cancelRt.anchoredPosition = new Vector2(-184f, 0f);

            view.saveButton = CreateButton("SaveButton", btnRow.transform, "Lưu cài đặt", fonts.ExtraBold, 18f, sprites.RoundedRectSm,
                UIStyleHelper.BlueColor, Color.white, new Vector2(170f, 44f));
            var saveRt = view.saveButton.GetComponent<RectTransform>();
            saveRt.anchorMin = new Vector2(1f, 0.5f);
            saveRt.anchorMax = new Vector2(1f, 0.5f);
            saveRt.pivot = new Vector2(1f, 0.5f);
            saveRt.anchoredPosition = Vector2.zero;

            modalGo.SetActive(false);
            view.modalRoot = modalGo;
            view.scrollRect = scrollRect;
        }

        #endregion

        #region UI Primitive Builders

        static void CreateSettingsSectionHeader(Transform parent, string title, BuiltFonts fonts)
        {
            var go = new GameObject("Section_" + title, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 32f;

            var tmp = CreateTmp("Label", go.transform, title, fonts.ExtraBold, null, 15.5f, UIStyleHelper.GoldColor, TextAlignmentOptions.BottomLeft);
            Stretch(tmp.rectTransform, 2f, 2f, 4f, 2f);
        }

        static TMP_InputField CreateSettingsInputRow(
            Transform parent, string label, string placeholder, TMP_InputField.ContentType contentType, BuiltFonts fonts, BuiltSprites sprites)
        {
            var rowGo = new GameObject("Row_" + label, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            rowGo.transform.SetParent(parent, false);
            rowGo.GetComponent<LayoutElement>().preferredHeight = 44f;

            var bg = rowGo.GetComponent<Image>();
            bg.sprite = sprites.RoundedRectSm;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x16, 0x20, 0x36, 0xB8);
            bg.raycastTarget = false;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 10, 4, 4);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var lbl = CreateTmp("Label", rowGo.transform, label, fonts.SemiBold, null, 16f, UIStyleHelper.TextPrimaryColor, TextAlignmentOptions.MidlineLeft);
            var lblLe = lbl.gameObject.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1f;
            lblLe.preferredHeight = 36f;

            var input = CreateTmpInputField("Input", rowGo.transform, placeholder, contentType, fonts, sprites);
            var inLe = input.gameObject.AddComponent<LayoutElement>();
            inLe.preferredWidth = 190f;
            inLe.preferredHeight = 36f;
            return input;
        }

        static TMP_Dropdown CreateSettingsDropdownRow(
            Transform parent, string label, string[] options, BuiltFonts fonts, BuiltSprites sprites)
        {
            var rowGo = new GameObject("Row_" + label, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            rowGo.transform.SetParent(parent, false);
            rowGo.GetComponent<LayoutElement>().preferredHeight = 46f;

            var bg = rowGo.GetComponent<Image>();
            bg.sprite = sprites.RoundedRectSm;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x16, 0x20, 0x36, 0xB8);
            bg.raycastTarget = false;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 10, 4, 4);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var lbl = CreateTmp("Label", rowGo.transform, label, fonts.SemiBold, null, 16f, UIStyleHelper.TextPrimaryColor, TextAlignmentOptions.MidlineLeft);
            var lblLe = lbl.gameObject.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1f;
            lblLe.preferredHeight = 38f;

            var dd = CreateTmpDropdown("Dropdown", rowGo.transform, options, fonts, sprites);
            var ddLe = dd.gameObject.AddComponent<LayoutElement>();
            ddLe.preferredWidth = 220f;
            ddLe.preferredHeight = 38f;
            return dd;
        }

        static TMP_InputField CreateTmpInputField(
            string name, Transform parent, string placeholderText, TMP_InputField.ContentType contentType, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var bg = go.GetComponent<Image>();
            bg.sprite = sprites.RoundedRectSm;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x09, 0x0F, 0x1D, 0xFF);

            var textArea = CreateStretchChild("TextArea", go.transform, 10f, 10f, 4f, 4f);
            textArea.AddComponent<RectMask2D>();

            var phTmp = CreateTmp("Placeholder", textArea.transform, placeholderText, fonts.SemiBold, null, 15.5f, new Color32(0x64, 0x74, 0x8B, 0xCC), TextAlignmentOptions.MidlineLeft);
            Stretch(phTmp.rectTransform);
            phTmp.textWrappingMode = TextWrappingModes.NoWrap;

            var valTmp = CreateTmp("Text", textArea.transform, string.Empty, fonts.Bold, null, 16f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(valTmp.rectTransform);
            valTmp.textWrappingMode = TextWrappingModes.NoWrap;

            var input = go.GetComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = valTmp;
            input.placeholder = phTmp;
            input.fontAsset = fonts.Bold;
            input.pointSize = 16f;
            input.contentType = contentType;
            input.caretColor = UIStyleHelper.GoldColor;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.18f, 0.49f, 1f, 0.4f);

            return input;
        }

        static TMP_Dropdown CreateTmpDropdown(
            string name, Transform parent, string[] options, BuiltFonts fonts, BuiltSprites sprites)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);

            var bg = go.GetComponent<Image>();
            bg.sprite = sprites.RoundedRectSm;
            bg.type = Image.Type.Sliced;
            bg.color = new Color32(0x09, 0x0F, 0x1D, 0xFF);

            var captionTmp = CreateTmp("Label", go.transform, options.Length > 0 ? options[0] : "", fonts.Bold, null, 15.5f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(captionTmp.rectTransform, 12f, 28f, 2f, 2f);

            var arrowTmp = CreateTmp("Arrow", go.transform, "▼", fonts.Bold, null, 12f, UIStyleHelper.TextMutedColor, TextAlignmentOptions.Center);
            arrowTmp.rectTransform.anchorMin = new Vector2(1f, 0f);
            arrowTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            arrowTmp.rectTransform.pivot = new Vector2(1f, 0.5f);
            arrowTmp.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
            arrowTmp.rectTransform.sizeDelta = new Vector2(20f, 0f);

            // Template danh sách xổ xuống
            var tplGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            tplGo.transform.SetParent(go.transform, false);
            var tplRt = tplGo.GetComponent<RectTransform>();
            tplRt.anchorMin = new Vector2(0f, 0f);
            tplRt.anchorMax = new Vector2(1f, 0f);
            tplRt.pivot = new Vector2(0.5f, 1f);
            tplRt.anchoredPosition = new Vector2(0f, -2f);
            tplRt.sizeDelta = new Vector2(0f, 130f);

            var tplImg = tplGo.GetComponent<Image>();
            tplImg.sprite = sprites.RoundedRectSm;
            tplImg.type = Image.Type.Sliced;
            tplImg.color = new Color32(0x11, 0x18, 0x27, 0xFA);

            var vpGo = CreateStretchChild("Viewport", tplGo.transform, 2f, 2f, 2f, 2f);
            vpGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 36f);

            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 36f);

            var itemBg = CreateImage("ItemBackground", itemGo.transform, sprites.RoundedRectSm, Image.Type.Sliced, new Color32(0x1E, 0x29, 0x3B, 0xCC), true);
            Stretch(itemBg.rectTransform, 2f, 2f, 2f, 2f);

            var itemCheck = CreateImage("ItemCheckmark", itemGo.transform, sprites.Circle, Image.Type.Simple, UIStyleHelper.BlueColor, false);
            itemCheck.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            itemCheck.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            itemCheck.rectTransform.pivot = new Vector2(0f, 0.5f);
            itemCheck.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            itemCheck.rectTransform.sizeDelta = new Vector2(10f, 10f);

            var itemLbl = CreateTmp("ItemLabel", itemGo.transform, "Option", fonts.SemiBold, null, 15f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(itemLbl.rectTransform, 28f, 10f, 2f, 2f);

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg;
            toggle.graphic = itemCheck;
            toggle.isOn = true;

            var sr = tplGo.GetComponent<ScrollRect>();
            sr.viewport = vpGo.GetComponent<RectTransform>();
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            tplGo.SetActive(false);

            var dd = go.GetComponent<TMP_Dropdown>();
            dd.targetGraphic = bg;
            dd.template = tplRt;
            dd.captionText = captionTmp;
            dd.itemText = itemLbl;
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
            return dd;
        }

        static Button CreateButton(
            string name, Transform parent, string text, TMP_FontAsset font, float fontSize, Sprite bgSprite,
            Color bgColor, Color textColor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;

            var lbl = CreateTmp("Label", go.transform, text, font, null, fontSize, textColor, TextAlignmentOptions.Center);
            Stretch(lbl.rectTransform);
            return btn;
        }

        static GameObject CreateStretchChild(string name, Transform parent, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), left, right, top, bottom);
            return go;
        }

        static GameObject CreateAnchoredChild(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        static Image CreateImage(string name, Transform parent, Sprite sprite, Image.Type type, Color color, bool raycastTarget)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return img;
        }

        static TextMeshProUGUI CreateTmp(
            string name, Transform parent, string text, TMP_FontAsset font, Material materialPreset,
            float fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            if (materialPreset != null) tmp.fontSharedMaterial = materialPreset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        #endregion
    }
}
