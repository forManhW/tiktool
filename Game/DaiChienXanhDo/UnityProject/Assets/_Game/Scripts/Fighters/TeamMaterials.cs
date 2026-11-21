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

        // ---------- Phối màu nhân vật theo từng phần (tên vật liệu của bộ Quaternius) ----------

        static readonly Color[] SkinTones = { Hex("F1C9A5"), Hex("E3B08C"), Hex("C98D66"), Hex("9E6B4A") };
        static readonly Color Gold = Hex("E8B84A");
        static readonly Color Cream = Hex("E9DCC4");

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        /// <summary>Màu phe: chính / đậm / nhạt.</summary>
        public static Color TeamMain(Team t) => t == Team.Blue ? Hex("2F6FE8") : Hex("E23B36");
        public static Color TeamDark(Team t) => t == Team.Blue ? Hex("1C3D8C") : Hex("8E1E1E");
        public static Color TeamLight(Team t) => t == Team.Blue ? Hex("78A8FF") : Hex("FF8A7E");

        static readonly System.Collections.Generic.Dictionary<(Material, Team, int), Material> dressed = new System.Collections.Generic.Dictionary<(Material, Team, int), Material>();

        /// <summary>
        /// Áo/mũ/giáp chính = màu phe; viền/chi tiết = vàng hoặc màu phe đậm; da = màu da người (4 tông);
        /// quần/thắt lưng/tóc giữ màu gốc nhưng sáng lên (màu gốc nhập vào bị tối).
        /// </summary>
        public static void DressFighter(GameObject model, Team team)
        {
            int tone = Random.Range(0, SkinTones.Length);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    var key = (src, team, tone);
                    if (!dressed.TryGetValue(key, out var c))
                    {
                        c = Matte(src);
                        c = new Material(c) { name = $"{src.name}_{team}_{tone}" };
                        Color orig = c.HasProperty("_BaseColor") ? c.GetColor("_BaseColor") : Color.gray;
                        c.SetColor("_BaseColor", PartColor(src.name, orig, team, tone));
                        dressed[key] = c;
                    }
                    mats[i] = c;
                }
                r.sharedMaterials = mats;
            }
        }

        static Color PartColor(string matName, Color orig, Team team, int tone)
        {
            string n = matName.Replace(" (Instance)", "");
            switch (n)
            {
                case "Skin": return SkinTones[tone];
                case "Face": return orig;
                // Áo chính màu phe; riêng đồ đen (ninja) giữ đen để nhìn ra loại nhân vật
                case "Main": return Lum(orig) < 0.1f ? Color.Lerp(Brighten(orig, 1.4f), TeamDark(team), 0.65f) : TeamMain(team);
                case "Shirt": case "Vest": return TeamMain(team);
                case "Helmet": case "Hat": case "DarkGreen": return TeamDark(team);
                // Hiệp sĩ giữ giáp thép/vàng, chỉ chi tiết đổi màu phe
                case "Armor": return Color.Lerp(Brighten(orig, 1.6f), TeamMain(team), 0.4f);
                case "Detail": case "Details": case "Red": return TeamMain(team);
                case "Light": return Cream;
                default: return Brighten(orig, 1.8f); // Pants, Belt, Black, Grey, Armor_Dark, Hair...
            }
        }

        /// <summary>Xe tăng: thân màu phe tông quân sự, kim loại xám súng, xích đen.</summary>
        public static void DressVehicle(GameObject model, Team team)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    bool turret = r.name.ToUpperInvariant().Contains("TOWER");
                    var key = (mats[i], team, turret ? 98 : 99);
                    if (!dressed.TryGetValue(key, out var c))
                    {
                        c = new Material(Matte(mats[i])) { name = mats[i].name + "_" + team + (turret ? "_turret" : "") };
                        string n = mats[i].name.ToLowerInvariant();
                        // Phối như robot: tháp pháo màu phe, thân thép xám pha màu phe đậm, chi tiết vàng đồng, xích đen
                        Color col = n.Contains("camo") ? (turret ? TeamMain(team) : Color.Lerp(TeamDark(team), Hex("5C6168"), 0.55f))
                                  : n.Contains("brown") ? Hex("C9A23F")
                                  : Hex("2A2C30");
                        c.SetColor("_BaseColor", col);
                        dressed[key] = c;
                    }
                    mats[i] = c;
                }
                r.sharedMaterials = mats;
            }
        }

        static readonly System.Collections.Generic.Dictionary<Material, Material> hero = new System.Collections.Generic.Dictionary<Material, Material>();

        /// <summary>
        /// Bộ giáp Siêu Chiến Binh dựa trên bộ đồ đã phối của lính: da/mặt/tóc giữ nguyên, áo chính sáng màu phe và hơi phát sáng,
        /// còn lại (giáp, mũ, quần, thắt lưng) đổi sang vàng kim.
        /// </summary>
        public static Material Hero(Material dressedMat, Team team)
        {
            if (dressedMat == null) return null;
            if (hero.TryGetValue(dressedMat, out var h)) return h;
            string n = dressedMat.name.ToLowerInvariant();
            if (n.StartsWith("skin") || n.StartsWith("face") || n.StartsWith("hair")) { hero[dressedMat] = dressedMat; return dressedMat; }
            h = new Material(dressedMat) { name = dressedMat.name + "_Hero" };
            bool cloth = n.StartsWith("main") || n.StartsWith("shirt") || n.StartsWith("vest");
            Color c = cloth ? TeamLight(team) : Hex("E8B84A");
            h.SetColor("_BaseColor", c);
            h.SetFloat("_Smoothness", cloth ? 0.2f : 0.45f);
            h.EnableKeyword("_EMISSION");
            h.SetColor("_EmissionColor", cloth ? TeamMain(team) * 0.45f : Hex("E8B84A") * 0.18f);
            hero[dressedMat] = h;
            return h;
        }

        static float Lum(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

        static Color Brighten(Color c, float k) => new Color(Mathf.Clamp01(c.r * k + 0.04f), Mathf.Clamp01(c.g * k + 0.04f), Mathf.Clamp01(c.b * k + 0.04f), 1f);

        static Material unlitBase;
        static readonly Material[] hpFill = new Material[2];
        static Material hpBack;

        static Material Unlit(Color c, string name)
        {
            if (unlitBase == null) unlitBase = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var m = new Material(unlitBase) { name = name };
            m.SetColor("_BaseColor", c);
            return m;
        }

        static readonly Material[] flag = new Material[2];
        /// <summary>Vải cờ màu phe (không phát sáng).</summary>
        public static Material Flag(Team team) => flag[(int)team] ??= MakeFlag(team);
        static Material MakeFlag(Team team)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = $"Flag_{team}" };
            m.SetColor("_BaseColor", TeamMain(team));
            m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        public static Material HpFill(Team team) => hpFill[(int)team] ??= Unlit(team == Team.Blue ? Hex("4FA3FF") : Hex("FF5A4F"), $"HpFill_{team}");
        public static Material HpBack() => hpBack ??= Unlit(new Color(0.08f, 0.08f, 0.1f), "HpBack");

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
        public static Material Super(Team team) => super[(int)team] ??= Emissive(team, 0f, 0.35f, $"Super_{team}");
    }
}
