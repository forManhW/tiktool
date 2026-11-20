using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// Bảng màu dùng chung, bộ sinh sprite bo góc 9-slice / hình tròn tại runtime (có cache)
    /// và event tách ghép khi cài đặt được lưu.
    /// </summary>
    public static class UIStyleHelper
    {
        // Bảng màu chính của Đại Chiến Xanh Đỏ
        public static readonly Color BlueColor = new Color32(0x2D, 0x7D, 0xFF, 0xFF);          // #2D7DFF
        public static readonly Color BlueGlowColor = new Color32(0x69, 0xA7, 0xFF, 0xFF);      // #69A7FF
        public static readonly Color BlueDarkColor = new Color32(0x10, 0x2B, 0x5C, 0xE6);      // #102B5C

        public static readonly Color RedColor = new Color32(0xFF, 0x3B, 0x3B, 0xFF);           // #FF3B3B
        public static readonly Color RedGlowColor = new Color32(0xFF, 0x7A, 0x7A, 0xFF);       // #FF7A7A
        public static readonly Color RedDarkColor = new Color32(0x5C, 0x12, 0x12, 0xE6);       // #5C1212

        public static readonly Color GoldColor = new Color32(0xFF, 0xD4, 0x3B, 0xFF);          // #FFD43B
        public static readonly Color HighlightGoldColor = new Color32(0xFF, 0xE0, 0x66, 0xFF); // #FFE066

        public static readonly Color PanelDarkColor = new Color32(0x0D, 0x12, 0x1E, 0xCC);     // ~80% alpha
        public static readonly Color PanelDeepColor = new Color32(0x08, 0x0B, 0x14, 0xE8);     // ~91% alpha
        public static readonly Color PanelSoftColor = new Color32(0x16, 0x1E, 0x2E, 0xBF);     // ~75% alpha
        public static readonly Color PanelBorderColor = new Color32(0xFF, 0xFF, 0xFF, 0x2E);

        public static readonly Color TextPrimaryColor = new Color32(0xF8, 0xFA, 0xFC, 0xFF);
        public static readonly Color TextMutedColor = new Color32(0xCB, 0xD5, 0xE1, 0xD0);

        public static readonly Color StatusConnectedColor = new Color32(0x22, 0xC5, 0x5E, 0xFF);
        public static readonly Color StatusDemoColor = new Color32(0x38, 0xBD, 0xF8, 0xFF);
        public static readonly Color StatusWarnColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);
        public static readonly Color StatusErrorColor = new Color32(0xFF, 0x2B, 0x2B, 0xFF);

        /// <summary>Event thông báo cài đặt đã được lưu (giữ tách ghép giữa menu F10 và dải hướng dẫn).</summary>
        public static event Action OnSettingsSaved;
        public static void NotifySettingsSaved() => OnSettingsSaved?.Invoke();

        static readonly Dictionary<long, Sprite> roundedRectCache = new Dictionary<long, Sprite>();
        static readonly Dictionary<long, Sprite> roundedOutlineCache = new Dictionary<long, Sprite>();
        static Sprite circleSprite;
        static Sprite whiteSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            roundedRectCache.Clear();
            roundedOutlineCache.Clear();
            circleSprite = null;
            whiteSprite = null;
            OnSettingsSaved = null;
        }

        public static Color GetTeamColor(Team team) => team == Team.Blue ? BlueColor : RedColor;

        public static Color GetConnectionStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status)) return StatusDemoColor;
            switch (status)
            {
                case "connected":
                    return StatusConnectedColor;
                case "demo":
                    return StatusDemoColor;
                case "connecting":
                case "reconnecting":
                    return StatusWarnColor;
                case "unauthorized":
                case "duplicate_game":
                    return StatusErrorColor;
                default:
                    return TextMutedColor;
            }
        }

        /// <summary>Sprite trắng 4x4 dùng cho Image.Type.Filled (UGUI bắt buộc có sprite khác null mới cắt fillAmount).</summary>
        public static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null) return whiteSprite;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "UI_White_4x4",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var colors = new Color32[16];
            var w = new Color32(255, 255, 255, 255);
            for (int i = 0; i < colors.Length; i++) colors[i] = w;
            tex.SetPixels32(colors);
            tex.Apply(false, true);

            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            whiteSprite.name = "UI_White_Sprite";
            whiteSprite.hideFlags = HideFlags.DontSave;
            return whiteSprite;
        }

        /// <summary>Sinh và cache sprite hình chữ nhật bo góc 9-slice khử răng cưa (SDF).</summary>
        public static Sprite GetRoundedRectSprite(int radius = 16, int size = 64)
        {
            size = Mathf.Clamp(size, 16, 256);
            radius = Mathf.Clamp(radius, 2, size / 2 - 2);
            long key = ((long)size << 16) | (uint)radius;

            if (roundedRectCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"UI_RoundedRect_{size}_r{radius}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

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
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[row + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            float border = Mathf.Min(radius + 2, size / 2 - 1);
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = $"UI_RoundedRectSprite_{size}_r{radius}";
            sprite.hideFlags = HideFlags.DontSave;

            roundedRectCache[key] = sprite;
            return sprite;
        }

        /// <summary>Sinh và cache sprite viền bo góc 9-slice khử răng cưa.</summary>
        public static Sprite GetRoundedOutlineSprite(int radius = 16, int thickness = 2, int size = 64)
        {
            size = Mathf.Clamp(size, 16, 256);
            radius = Mathf.Clamp(radius, 2, size / 2 - 2);
            thickness = Mathf.Clamp(thickness, 1, radius);
            long key = ((long)size << 32) | ((long)radius << 16) | (uint)thickness;

            if (roundedOutlineCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"UI_RoundedOutline_{size}_r{radius}_t{thickness}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

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
                    float outerAlpha = Mathf.Clamp01(0.5f - dist);
                    float innerAlpha = Mathf.Clamp01(0.5f - (dist + thickness));
                    float ring = Mathf.Clamp01(outerAlpha - innerAlpha);
                    byte a = (byte)Mathf.RoundToInt(ring * 255f);
                    pixels[row + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            float border = Mathf.Min(radius + 2, size / 2 - 1);
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = $"UI_RoundedOutlineSprite_{size}_r{radius}_t{thickness}";
            sprite.hideFlags = HideFlags.DontSave;

            roundedOutlineCache[key] = sprite;
            return sprite;
        }

        /// <summary>Sinh và cache sprite hình tròn khử răng cưa (dùng cho chấm trạng thái).</summary>
        public static Sprite GetCircleSprite(int size = 32)
        {
            if (circleSprite != null) return circleSprite;

            size = Mathf.Clamp(size, 16, 128);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"UI_Circle_{size}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

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
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[row + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "UI_CircleSprite";
            circleSprite.hideFlags = HideFlags.DontSave;
            return circleSprite;
        }

        /// <summary>Gán sprite bo góc 9-slice cho Image nếu chưa có sprite (hoặc khi force = true).</summary>
        public static void EnsureRoundedSprite(Image image, int radius = 16, int size = 64, bool force = false)
        {
            if (image == null) return;
            if (!force && image.sprite != null) return;
            image.sprite = GetRoundedRectSprite(radius, size);
            image.type = Image.Type.Sliced;
        }

        /// <summary>Gán sprite viền bo góc 9-slice cho Image nếu chưa có sprite.</summary>
        public static void EnsureRoundedOutlineSprite(Image image, int radius = 16, int thickness = 2, int size = 64, bool force = false)
        {
            if (image == null) return;
            if (!force && image.sprite != null) return;
            image.sprite = GetRoundedOutlineSprite(radius, thickness, size);
            image.type = Image.Type.Sliced;
        }

        /// <summary>Gán sprite hình tròn cho Image nếu chưa có sprite.</summary>
        public static void EnsureCircleSprite(Image image, bool force = false)
        {
            if (image == null) return;
            if (!force && image.sprite != null) return;
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
        }

        /// <summary>Đảm bảo Image thanh fill có sprite khác null và dùng Horizontal Filled.</summary>
        public static void EnsureHorizontalFill(Image image)
        {
            if (image == null) return;
            if (image.sprite == null) image.sprite = GetWhiteSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
