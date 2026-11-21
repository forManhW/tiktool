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

        enum VehiclePart
        {
            TurretArmor,
            HullArmor,
            GoldAccent,
            GunmetalSilver,
            TreadsWheels,
            Headlight,
            OpticVisor,
        }

        static readonly System.Collections.Generic.Dictionary<(Material, Team, VehiclePart), Material> vehicleMats =
            new System.Collections.Generic.Dictionary<(Material, Team, VehiclePart), Material>();

        static Material vehicleTrimGold;
        static Material vehicleTrimSilver;
        static Material vehicleTrimHeadlight;
        static readonly Material[] vehicleTrimVisor = new Material[2];

        static Color TankTurretColor(Team t) => t == Team.Blue ? Hex("3388FF") : Hex("FF3B3B");
        static Color TankHullColor(Team t) => t == Team.Blue ? Hex("1A4BB8") : Hex("B81D24");
        static Color TankVisorColor(Team t) => t == Team.Blue ? Hex("66C2FF") : Hex("FF7766");

        static VehiclePart ClassifyVehiclePart(string rendererName, string matName)
        {
            string r = (rendererName ?? "").ToLowerInvariant();
            string m = (matName ?? "").Replace(" (Instance)", "").ToLowerInvariant();

            bool isTurret = r.Contains("tower") || r.Contains("turret") || r.Contains("cannon") || r.Contains("barrel");
            bool isTrack = r.Contains("track") || r.Contains("tread") || r.Contains("wheel");

            if (r.Contains("light") || r.Contains("lamp") || m.Contains("light") || m.Contains("lamp") || m.Contains("glow") || m.Contains("emissive"))
                return VehiclePart.Headlight;
            if (r.Contains("optic") || r.Contains("visor") || m.Contains("optic") || m.Contains("visor") || m.Contains("lens") || m.Contains("glass"))
                return VehiclePart.OpticVisor;
            if (m.Contains("brown") || m.Contains("gold") || m.Contains("brass") || m.Contains("copper")
                || m.Contains("detail") || m.Contains("accent") || m.Contains("trim") || m.Contains("exhaust")
                || r.Contains("exhaust") || r.Contains("trim") || r.Contains("hatch"))
                return VehiclePart.GoldAccent;
            if (m.Contains("camo") || m.Contains("armor") || m.Contains("paint") || m.Contains("body")
                || m.Contains("hull") || m.Contains("main") || m.Contains("turret"))
                return isTrack ? VehiclePart.TreadsWheels : (isTurret ? VehiclePart.TurretArmor : VehiclePart.HullArmor);
            if (m.Contains("grey") || m.Contains("gray") || m.Contains("silver") || m.Contains("steel")
                || m.Contains("chrome") || m.Contains("gun") || m.Contains("cannon") || m.Contains("barrel") || m.Contains("muzzle"))
                return VehiclePart.GunmetalSilver;
            if (m.Contains("track") || m.Contains("tread") || m.Contains("wheel") || m.Contains("rubber"))
                return VehiclePart.TreadsWheels;
            if (m.Contains("black") || m.Contains("dark") || m.Contains("metal"))
                return isTurret ? VehiclePart.GunmetalSilver : VehiclePart.TreadsWheels;

            if (isTrack) return VehiclePart.TreadsWheels;
            return isTurret ? VehiclePart.TurretArmor : VehiclePart.HullArmor;
        }

        static void ApplyVehicleSurface(Material m, VehiclePart part, Team team)
        {
            Color baseCol;
            float metallic;
            float smoothness;
            Color emission = Color.black;

            switch (part)
            {
                case VehiclePart.TurretArmor:
                    baseCol = TankTurretColor(team);
                    metallic = 0.08f;
                    smoothness = 0.34f;
                    emission = baseCol * 0.12f;
                    break;
                case VehiclePart.HullArmor:
                    baseCol = TankHullColor(team);
                    metallic = 0.06f;
                    smoothness = 0.28f;
                    emission = baseCol * 0.07f;
                    break;
                case VehiclePart.GoldAccent:
                    baseCol = Hex("F5C43D");
                    metallic = 0.25f;
                    smoothness = 0.46f;
                    emission = baseCol * 0.10f;
                    break;
                case VehiclePart.GunmetalSilver:
                    baseCol = Hex("D4DCE8");
                    metallic = 0.28f;
                    smoothness = 0.45f;
                    emission = baseCol * 0.05f;
                    break;
                case VehiclePart.Headlight:
                    baseCol = Hex("FFF2B2");
                    metallic = 0f;
                    smoothness = 0.6f;
                    emission = Hex("FFE89A") * 0.9f;
                    break;
                case VehiclePart.OpticVisor:
                    baseCol = TankVisorColor(team);
                    metallic = 0.1f;
                    smoothness = 0.7f;
                    emission = baseCol * 0.85f;
                    break;
                default: // TreadsWheels
                    baseCol = Hex("232736");
                    metallic = 0.12f;
                    smoothness = 0.26f;
                    break;
            }

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 1f);
            if (emission.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            }
        }

        static Material VehicleTrimMat(VehiclePart part, Team team)
        {
            if (part == VehiclePart.GoldAccent)
                return vehicleTrimGold ??= CreateVehicleTrimMat("TankTrim_Gold", part, team);
            if (part == VehiclePart.OpticVisor)
                return vehicleTrimVisor[(int)team] ??= CreateVehicleTrimMat($"TankTrim_Visor_{team}", part, team);
            if (part == VehiclePart.Headlight)
                return vehicleTrimHeadlight ??= CreateVehicleTrimMat("TankTrim_Headlight", part, team);
            return vehicleTrimSilver ??= CreateVehicleTrimMat("TankTrim_Silver", VehiclePart.GunmetalSilver, team);
        }

        static Material CreateVehicleTrimMat(string name, VehiclePart part, Team team)
        {
            var baseMat = GameAssets.I.TeamMaterial(team);
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = name;
            ApplyVehicleSurface(m, part, team);
            if (part == VehiclePart.GunmetalSilver && m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", Hex("E6EEF9"));
            return m;
        }

        static void AddTrimPiece(Transform parent, PrimitiveType primitive, string name, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void AttachTankAccents(GameObject model, Team team)
        {
            Transform towerTf = null;
            Transform bodyTf = null;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("t-26") && n.Contains("tower")) towerTf = r.transform;
                else if (n.Contains("t-26") && n.Contains("body")) bodyTf = r.transform;
            }

            if (towerTf != null && towerTf.Find("TankTrim") == null)
            {
                var mf = towerTf.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    if (mb.size.y > mb.size.z && mb.size.z > 0.0001f)
                    {
                        float u = mb.size.z / 1.2725f;
                        float xSign = Mathf.Abs(mb.max.x) >= Mathf.Abs(mb.min.x) ? 1f : -1f;
                        float ySign = Mathf.Abs(mb.min.y) >= Mathf.Abs(mb.max.y) ? 1f : -1f;
                        float z0 = mb.min.z;

                        var trim = new GameObject("TankTrim").transform;
                        trim.SetParent(towerTf, false);

                        var goldMat = VehicleTrimMat(VehiclePart.GoldAccent, team);
                        var silverMat = VehicleTrimMat(VehiclePart.GunmetalSilver, team);
                        var visorMat = VehicleTrimMat(VehiclePart.OpticVisor, team);

                        // Vòng nòng vàng kim nổi bật ở đầu pháo chính + vòng bạc thân nòng
                        AddTrimPiece(trim, PrimitiveType.Cylinder, "MuzzleGold",
                            new Vector3(0.001f * xSign * u, -1.78f * ySign * u, z0 + 0.361f * u),
                            Vector3.zero,
                            new Vector3(0.16f * u, 0.11f * u, 0.16f * u),
                            goldMat);
                        AddTrimPiece(trim, PrimitiveType.Cylinder, "BarrelCollar",
                            new Vector3(0.001f * xSign * u, -1.42f * ySign * u, z0 + 0.361f * u),
                            Vector3.zero,
                            new Vector3(0.125f * u, 0.14f * u, 0.125f * u),
                            silverMat);

                        // Nắp nóc chỉ huy vàng kim + kính ngắm phát sáng màu phe
                        AddTrimPiece(trim, PrimitiveType.Cylinder, "CupolaCap",
                            new Vector3(-0.142f * xSign * u, 0.383f * ySign * u, z0 + 1.278f * u),
                            new Vector3(90f, 0f, 0f),
                            new Vector3(0.42f * u, 0.032f * u, 0.42f * u),
                            goldMat);
                        AddTrimPiece(trim, PrimitiveType.Cube, "CommanderVisor",
                            new Vector3(-0.142f * xSign * u, 0.095f * ySign * u, z0 + 1.13f * u),
                            Vector3.zero,
                            new Vector3(0.30f * u, 0.07f * u, 0.09f * u),
                            visorMat);
                    }
                }
            }

            if (bodyTf != null && bodyTf.Find("TankTrim") == null)
            {
                var mf = bodyTf.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    if (mb.size.y > mb.size.z && mb.size.z > 0.0001f)
                    {
                        float u = mb.size.z / 1.5684f;
                        float frontSign = (towerTf != null && towerTf.parent == bodyTf && towerTf.localPosition.y > 0f) ? -1f : 1f;
                        float z0 = mb.min.z;

                        var trim = new GameObject("TankTrim").transform;
                        trim.SetParent(bodyTf, false);

                        var goldMat = VehicleTrimMat(VehiclePart.GoldAccent, team);
                        var lightMat = VehicleTrimMat(VehiclePart.Headlight, team);
                        var silverMat = VehicleTrimMat(VehiclePart.GunmetalSilver, team);

                        // Đèn pha đôi phát sáng ấm với viền vàng kim ở hai mũi chắn bùn trước
                        for (int s = -1; s <= 1; s += 2)
                        {
                            AddTrimPiece(trim, PrimitiveType.Cube, s < 0 ? "HeadlightBezel_L" : "HeadlightBezel_R",
                                new Vector3(s * 1.06f * u, -2.42f * frontSign * u, z0 + 1.20f * u),
                                Vector3.zero,
                                new Vector3(0.28f * u, 0.24f * u, 0.15f * u),
                                goldMat);
                            AddTrimPiece(trim, PrimitiveType.Cube, s < 0 ? "HeadlightGlow_L" : "HeadlightGlow_R",
                                new Vector3(s * 1.06f * u, -2.53f * frontSign * u, z0 + 1.20f * u),
                                Vector3.zero,
                                new Vector3(0.20f * u, 0.07f * u, 0.10f * u),
                                lightMat);
                        }

                        // Nẹp giáp bạc sáng phía trước thân xe tăng tạo điểm nhấn 3 tông
                        AddTrimPiece(trim, PrimitiveType.Cube, "FrontArmorTrim",
                            new Vector3(0f, -2.02f * frontSign * u, z0 + 1.482f * u),
                            Vector3.zero,
                            new Vector3(1.18f * u, 0.22f * u, 0.05f * u),
                            silverMat);
                    }
                }
            }
        }

        /// <summary>
        /// Phối màu xe tăng phong cách arcade rực rỡ, độ tương phản cao từ góc nhìn trên cao:
        /// tháp pháo màu phe sáng, thân giáp màu phe đậm sạch (không xám đục), nòng phụ bạc thép,
        /// ống xả/chi tiết vàng kim, bánh xích than hải quân, kèm đèn pha &amp; vòng nòng vàng kim.
        /// </summary>
        public static void DressVehicle(GameObject model, Team team)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r.transform.parent != null && r.transform.parent.name == "TankTrim") continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    var part = ClassifyVehiclePart(r.name, src.name);
                    var key = (src, team, part);
                    if (!vehicleMats.TryGetValue(key, out var c))
                    {
                        c = new Material(Matte(src)) { name = $"{src.name}_{team}_{part}" };
                        ApplyVehicleSurface(c, part, team);
                        vehicleMats[key] = c;
                    }
                    mats[i] = c;
                }
                r.sharedMaterials = mats;
            }
            AttachTankAccents(model, team);
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
