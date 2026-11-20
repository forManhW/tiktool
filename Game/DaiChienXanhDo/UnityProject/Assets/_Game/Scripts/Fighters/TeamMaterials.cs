using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Vật liệu màu phe tạo lúc chạy (vòng dưới chân, Siêu Chiến Binh). Tạo một lần, dùng chung.</summary>
    public static class TeamMaterials
    {
        static readonly Material[] ring = new Material[2];
        static readonly Material[] super = new Material[2];

        static Material Emissive(Team team, float whiteMix, float glow, string name)
        {
            var baseMat = GameAssets.I.TeamMaterial(team);
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = name;
            Color c = team.TeamColor();
            m.SetColor("_BaseColor", Color.Lerp(c, Color.white, whiteMix));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * glow);
            return m;
        }

        /// <summary>Vòng dưới chân để nhìn là biết phe: màu đậm, phát sáng nhẹ (sáng quá sẽ bạc thành hồng/xanh nhạt).</summary>
        public static Material Ring(Team team) => ring[(int)team] ??= Emissive(team, 0f, 0.3f, $"Ring_{team}");

        static readonly System.Collections.Generic.Dictionary<Material, Material> matte = new System.Collections.Generic.Dictionary<Material, Material>();

        /// <summary>
        /// Vật liệu nhập từ FBX thường bị đặt Metallic cao nên trong URP nhìn đen sì. Tạo bản sao không kim loại, dùng chung.
        /// </summary>
        public static Material Matte(Material m, Texture2D texture = null)
        {
            if (m == null) return null;
            if (matte.TryGetValue(m, out var c) && texture == null) return c;
            c = new Material(m) { name = m.name + "_Matte" };
            if (c.HasProperty("_Metallic")) c.SetFloat("_Metallic", 0f);
            if (c.HasProperty("_Smoothness")) c.SetFloat("_Smoothness", 0.15f);
            if (c.HasProperty("_SpecularHighlights")) c.SetFloat("_SpecularHighlights", 0f);
            if (texture != null) { c.SetTexture("_BaseMap", texture); c.SetColor("_BaseColor", Color.white); }
            else matte[m] = c;
            return c;
        }

        static readonly System.Collections.Generic.Dictionary<(Material, Team, float), Material> tinted = new System.Collections.Generic.Dictionary<(Material, Team, float), Material>();

        static bool IsSkin(Material m)
        {
            string n = m.name.ToLowerInvariant();
            return n.Contains("skin") || n.Contains("face") || n.Contains("eye") || n.Contains("hair");
        }

        /// <summary>
        /// Nhuộm model theo màu phe: quần áo/giáp chuyển sang Xanh hoặc Đỏ, giữ nguyên da/tóc/mắt.
        /// texture: gắn ảnh nếu model thiếu (robot). amount: mức nhuộm 0..1.
        /// </summary>
        public static void MakeTeam(GameObject go, Team team, float amount, Texture2D texture = null)
        {
            Color tc = team.TeamColor();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    var key = (src, team, amount);
                    if (!tinted.TryGetValue(key, out var c))
                    {
                        c = Matte(src, texture);
                        c = new Material(c) { name = $"{src.name}_{team}" };
                        if (texture != null) c.SetTexture("_BaseMap", texture);
                        Color orig = texture != null ? Color.white : (c.HasProperty("_BaseColor") ? c.GetColor("_BaseColor") : Color.white);
                        if (!IsSkin(src) || texture != null)
                        {
                            // giữ độ sáng tối của màu gốc để vẫn thấy chi tiết, nhưng đổi sắc sang màu phe
                            float lum = orig.r * 0.3f + orig.g * 0.59f + orig.b * 0.11f;
                            Color shade = tc * Mathf.Lerp(0.55f, 1.25f, lum);
                            shade.a = 1f;
                            c.SetColor("_BaseColor", Color.Lerp(orig, shade, amount));
                        }
                        tinted[key] = c;
                    }
                    mats[i] = c;
                }
                r.sharedMaterials = mats;
            }
        }

        public static void MakeMatte(GameObject go, Texture2D texture = null)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Matte(mats[i], texture);
                r.sharedMaterials = mats;
            }
        }

        /// <summary>Siêu Chiến Binh: cả người phát sáng màu phe.</summary>
        public static Material Super(Team team) => super[(int)team] ??= Emissive(team, 0.05f, 0.9f, $"Super_{team}");
    }
}
