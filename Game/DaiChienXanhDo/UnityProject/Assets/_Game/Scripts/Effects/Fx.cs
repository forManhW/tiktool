using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Hiệu ứng hạt dùng chung, rất nhẹ: mỗi loại chỉ có MỘT ParticleSystem, mọi vụ nổ/tia lửa đều gọi Emit vào đó.
    /// Tự tạo lúc chạy, không cần prefab. Vật liệu lấy từ GameAssets (fxAdditiveMaterial / fxSmokeMaterial).
    /// </summary>
    public class Fx : MonoBehaviour
    {
        static Fx instance;
        public static Fx I
        {
            get
            {
                if (instance == null) instance = new GameObject("Fx").AddComponent<Fx>();
                return instance;
            }
        }

        ParticleSystem sparks, flash, fire, smoke;
        float tornadoAccum;

        void Awake()
        {
            instance = this;
            var a = GameAssets.I;
            sparks = Make("Sparks", a.fxAdditiveMaterial, 1.6f, ParticleSystemRenderMode.Stretch, shrink: true);
            flash = Make("Flash", a.fxAdditiveMaterial, 0f, ParticleSystemRenderMode.Billboard, shrink: false);
            fire = Make("Fire", a.fxAdditiveMaterial, -0.4f, ParticleSystemRenderMode.Billboard, shrink: true);
            smoke = Make("Smoke", a.fxSmokeMaterial, -0.15f, ParticleSystemRenderMode.Billboard, shrink: false, grow: true);
        }

        ParticleSystem Make(string name, Material mat, float gravity, ParticleSystemRenderMode mode, bool shrink, bool grow = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            main.gravityModifier = gravity;
            main.startSpeed = 0f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            if (shrink || grow)
            {
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, grow ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = 0.06f; r.lengthScale = 1.6f; }
            if (mat != null) r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color color)
        {
            var ep = new ParticleSystem.EmitParams { position = pos, velocity = vel, startSize = size, startLifetime = life, startColor = color, applyShapeToPosition = false };
            ps.Emit(ep, 1);
        }

        /// <summary>Cú đấm trúng: chớp sáng + tia lửa văng ra.</summary>
        public void Hit(Vector3 pos, Color teamColor, bool strong)
        {
            Color hot = Color.Lerp(teamColor, Color.white, 0.55f);
            Emit(flash, pos, Vector3.zero, strong ? 3f : 1.7f, strong ? 0.16f : 0.11f, hot);
            Emit(flash, pos, Vector3.zero, strong ? 1.4f : 0.8f, 0.07f, Color.white);
            int n = strong ? 26 : 12;
            float speed = strong ? 10f : 6f;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(0.4f, 1f) * speed; v.y = Mathf.Abs(v.y) + 1.5f;
                Emit(sparks, pos, v, Random.Range(0.08f, strong ? 0.2f : 0.14f), Random.Range(0.18f, 0.4f), i % 3 == 0 ? hot : new Color(1f, 0.85f, 0.4f));
            }
            if (strong) Emit(flash, pos, Vector3.zero, 3.5f, 0.2f, new Color(teamColor.r, teamColor.g, teamColor.b, 0.6f));
        }

        /// <summary>Vụ nổ nhiều tầng: lõi chớp trắng-vàng + cầu lửa rực + tia lửa văng + khói đen bốc cao. scale 1 ≈ nổ đạn xe tăng.</summary>
        public void Explosion(Vector3 pos, float scale)
        {
            // Chớp lõi trắng-vàng chói + hào quang cam rộng
            Emit(flash, pos + Vector3.up * 0.6f, Vector3.zero, 3.4f * scale, 0.12f, new Color(1f, 0.98f, 0.86f));
            Emit(flash, pos + Vector3.up * 0.5f, Vector3.zero, 7.2f * scale, 0.22f, new Color(1f, 0.58f, 0.14f, 0.92f));

            // Cầu lửa 2 lớp (vàng nóng ở tâm + cam đỏ bùng ra ngoài)
            int fires = Mathf.Clamp(Mathf.RoundToInt(20 * scale), 10, 34);
            for (int i = 0; i < fires; i++)
            {
                bool inner = i < fires / 3;
                Vector3 v = Random.insideUnitSphere * (inner ? 2.6f : 5.2f) * scale;
                v.y = Mathf.Abs(v.y) * 1.45f + (inner ? 1.8f : 1.1f);
                Color col = inner
                    ? new Color(1f, Random.Range(0.72f, 0.92f), 0.28f)
                    : new Color(1f, Random.Range(0.24f, 0.52f), 0.06f);
                Emit(fire, pos + Random.insideUnitSphere * 0.45f * scale, v, Random.Range(1.05f, 2.25f) * scale, Random.Range(0.32f, 0.68f), col);
            }

            // Tia lửa bắn tốc độ cao
            int sparkCount = Mathf.Clamp(Mathf.RoundToInt(18 * scale), 10, 32);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(7f, 16f) * Mathf.Sqrt(scale);
                v.y = Mathf.Abs(v.y) + 2.5f;
                Color sc = i % 3 == 0 ? new Color(1f, 0.96f, 0.72f) : new Color(1f, 0.62f, 0.14f);
                Emit(sparks, pos + Vector3.up * 0.3f, v, Random.Range(0.11f, 0.26f), Random.Range(0.35f, 0.85f), sc);
            }

            // Cột khói đen đậm tăng tương phản
            int smokes = Mathf.Clamp(Mathf.RoundToInt(10 * scale), 6, 18);
            for (int i = 0; i < smokes; i++)
            {
                Vector3 v = Random.insideUnitSphere * 1.8f * scale;
                v.y = Mathf.Abs(v.y) + 1.6f;
                float shade = Random.Range(0.14f, 0.24f);
                Emit(smoke, pos + Random.insideUnitSphere * 0.9f * scale + Vector3.up * 0.4f, v, Random.Range(1.9f, 3.3f) * scale, Random.Range(1.0f, 1.9f), new Color(shade, shade * 0.95f, shade * 0.92f, 0.76f));
            }
        }

        /// <summary>Bụi tung lên quanh chân (sinh lính, robot giẫm, lính gục).</summary>
        public void Dust(Vector3 pos, float radius)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(radius * 6f), 4, 40);
            for (int i = 0; i < n; i++)
            {
                float ang = i * Mathf.PI * 2f / n;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Emit(smoke, pos + dir * radius * 0.3f + Vector3.up * 0.2f, dir * radius * 1.8f + Vector3.up * 0.6f, Random.Range(0.8f, 1.4f) * Mathf.Max(0.6f, radius * 0.5f), Random.Range(0.6f, 1.1f), new Color(0.75f, 0.68f, 0.55f, 0.55f));
            }
        }

        /// <summary>Ngọn lửa rực trên đường bay thiên thạch/đạn pháo.</summary>
        public void FireTrail(Vector3 pos, float size)
        {
            Emit(fire, pos + Random.insideUnitSphere * size * 0.22f, Random.insideUnitSphere * 0.6f + Vector3.up * 0.8f,
                size * Random.Range(0.85f, 1.35f), Random.Range(0.22f, 0.42f), new Color(1f, Random.Range(0.5f, 0.82f), 0.14f));
            if (Random.value < 0.45f)
                Emit(fire, pos + Random.insideUnitSphere * size * 0.35f, Random.insideUnitSphere * 0.8f,
                    size * Random.Range(0.65f, 1.05f), Random.Range(0.18f, 0.34f), new Color(1f, 0.28f, 0.05f));
        }

        /// <summary>Vệt khói núi lửa đen đậm phía sau thiên thạch để tạo độ tương phản sắc nét.</summary>
        public void SmokeTrail(Vector3 pos, float size)
        {
            Emit(smoke, pos + Random.insideUnitSphere * size * 0.28f, Random.insideUnitSphere * 0.45f + Vector3.up * 0.6f,
                size * Random.Range(1.1f, 1.65f), Random.Range(0.7f, 1.15f), new Color(0.11f, 0.09f, 0.09f, 0.78f));
        }

        /// <summary>Tia tàn lửa rực bắn ngược dọc đường rơi của thiên thạch.</summary>
        public void MeteorEmbers(Vector3 pos, Vector3 trailDir, float size)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 jitter = Random.insideUnitSphere * (1.8f + size * 0.5f);
                Vector3 vel = trailDir * Random.Range(5f, 11f) + jitter;
                Color col = i == 0 ? new Color(1f, 0.96f, 0.7f) : new Color(1f, Random.Range(0.45f, 0.72f), 0.1f);
                Emit(sparks, pos + Random.insideUnitSphere * size * 0.28f, vel, Random.Range(0.12f, 0.24f), Random.Range(0.25f, 0.55f), col);
            }
        }

        /// <summary>Chớp sáng lớn (sét đánh, nòng pháo).</summary>
        public void Flash(Vector3 pos, float size, Color color)
        {
            Emit(flash, pos, Vector3.zero, size, 0.22f, color);
            for (int i = 0; i < 14; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(4f, 10f); v.y = Mathf.Abs(v.y) + 2f;
                Emit(sparks, pos, v, Random.Range(0.1f, 0.22f), Random.Range(0.25f, 0.5f), Color.Lerp(color, Color.white, 0.5f));
            }
        }

        // ---------- Icon hiệu ứng bật lên trong sân ----------

        class Pop { public SpriteRenderer sr; public Vector3 start; public float t0, size; }
        readonly System.Collections.Generic.List<Pop> pops = new System.Collections.Generic.List<Pop>();
        const float PopLife = 1.6f;

        /// <summary>Icon bật to ra, bay lên rồi mờ dần ở vị trí hiệu ứng.</summary>
        public void IconPop(Sprite icon, Vector3 pos, float size)
        {
            if (icon == null) return;
            Pop p = null;
            foreach (var x in pops) if (!x.sr.gameObject.activeSelf) { p = x; break; }
            if (p == null)
            {
                if (pops.Count >= 24) return;
                var go = new GameObject("IconPop");
                go.transform.SetParent(transform, false);
                p = new Pop { sr = go.AddComponent<SpriteRenderer>() };
                p.sr.sortingOrder = 100;
                pops.Add(p);
            }
            p.sr.sprite = icon;
            p.start = pos; p.t0 = Time.time; p.size = size;
            p.sr.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            var cam = CameraRig.I != null ? CameraRig.I.cam : Camera.main;
            foreach (var p in pops)
            {
                if (!p.sr.gameObject.activeSelf) continue;
                float t = (Time.time - p.t0) / PopLife;
                if (t >= 1f) { p.sr.gameObject.SetActive(false); continue; }
                float pop = t < 0.15f ? Mathf.SmoothStep(0f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
                var tr = p.sr.transform;
                tr.position = p.start + Vector3.up * (t * 1.8f);
                if (cam != null) tr.rotation = cam.transform.rotation;
                float px = p.sr.sprite.rect.width / p.sr.sprite.pixelsPerUnit;
                tr.localScale = Vector3.one * (p.size * pop / Mathf.Max(0.01f, px));
                p.sr.color = new Color(1f, 1f, 1f, t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f);
            }
        }

        /// <summary>
        /// Hiệu ứng hạt cho lốc xoáy: vệt gió trắng-xanh sắc nét xoắn quanh phễu + sương bão gọn + vòng bụi tung ở chân.
        /// Gọi mỗi frame; tự tiết chế tần suất phát hạt để giữ ổn định 60 FPS.
        /// </summary>
        public void Tornado(Vector3 basePos, float time)
        {
            tornadoAccum += Time.deltaTime;
            const float step = 1f / 45f;
            if (tornadoAccum < step) return;
            int ticks = Mathf.Min(2, Mathf.FloorToInt(tornadoAccum / step));
            tornadoAccum -= ticks * step;

            for (int tick = 0; tick < ticks; tick++)
            {
                // Vệt gió trắng-băng sắc nét quấn xoắn ốc lên cao
                for (int k = 0; k < 7; k++)
                {
                    float h = Random.Range(0.4f, 14.5f);
                    float normH = h / 16f;
                    float r = Mathf.Lerp(0.85f, 7.1f, Mathf.Pow(normH, 1.5f) * 0.75f + normH * 0.25f) + Random.Range(-0.25f, 0.55f);
                    float ang = time * 8.5f + h * 0.95f + Random.Range(0f, Mathf.PI * 2f);
                    Vector3 offset = new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r);
                    Vector3 tangent = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (7.5f + r * 2.8f);
                    Color streakCol = k % 2 == 0 ? new Color(0.86f, 0.96f, 1f) : new Color(0.52f, 0.85f, 1f);
                    Emit(sparks, basePos + offset, tangent + Vector3.up * Random.Range(3.5f, 6.5f),
                        Random.Range(0.14f, 0.28f), Random.Range(0.24f, 0.45f), streakCol);
                }

                // Làn sương bão xanh-xám gọn gàng (không che lấp phễu 3D)
                for (int k = 0; k < 5; k++)
                {
                    float h = Random.Range(0.6f, 14.5f);
                    float normH = h / 16f;
                    float r = Mathf.Lerp(0.7f, 6.4f, Mathf.Pow(normH, 1.5f) * 0.75f + normH * 0.25f);
                    float ang = time * 6.5f + h * 0.8f + Random.Range(0f, Mathf.PI * 2f);
                    Vector3 offset = new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r);
                    Vector3 tangent = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (4.5f + r * 2.2f);
                    Color mist = k % 2 == 0
                        ? new Color(0.75f, 0.88f, 0.97f, 0.42f)
                        : new Color(0.22f, 0.33f, 0.46f, 0.52f);
                    Emit(smoke, basePos + offset, tangent + Vector3.up * 2.8f,
                        Random.Range(0.95f, 1.65f) + normH * 1.1f, Random.Range(0.28f, 0.5f), mist);
                }

                // Vòng bụi sáng + tia gió tung mạnh dưới chân lốc xoáy (khớp bán kính hút 5m)
                for (int k = 0; k < 5; k++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 swirl = new Vector3(-dir.z, 0f, dir.x);
                    float dist = Random.Range(1.2f, 4.6f);
                    Emit(smoke, basePos + dir * dist + Vector3.up * 0.25f,
                        (dir * 3.8f + swirl * 5.2f) + Vector3.up * Random.Range(1.2f, 2.4f),
                        Random.Range(1.2f, 2.2f), Random.Range(0.45f, 0.75f),
                        k % 2 == 0 ? new Color(0.84f, 0.88f, 0.92f, 0.58f) : new Color(0.76f, 0.72f, 0.64f, 0.52f));
                }
            }
        }

        /// <summary>Xoáy gió bốc thẳng lên trời khi lính bị lốc xoáy hút trúng.</summary>
        public void VortexPull(Vector3 pos)
        {
            Emit(flash, pos + Vector3.up * 1.1f, Vector3.up * 4f, 2.6f, 0.18f, new Color(0.72f, 0.94f, 1f, 0.95f));
            for (int i = 0; i < 10; i++)
            {
                float ang = i * Mathf.PI * 2f / 10f;
                Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                Emit(sparks, pos + radial * 0.55f + Vector3.up * Random.Range(0.2f, 1.2f),
                    tangent * 6.5f + Vector3.up * Random.Range(8.5f, 14f),
                    Random.Range(0.14f, 0.26f), Random.Range(0.32f, 0.58f),
                    i % 2 == 0 ? Color.white : new Color(0.55f, 0.9f, 1f));
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 off = Random.insideUnitSphere * 0.6f; off.y = Mathf.Abs(off.y);
                Emit(smoke, pos + off, Vector3.up * Random.Range(4.5f, 7.5f) + Random.insideUnitSphere * 1.5f,
                    Random.Range(1.0f, 1.7f), Random.Range(0.45f, 0.75f), new Color(0.78f, 0.9f, 0.98f, 0.55f));
            }
        }

        /// <summary>Cột bụi + tia địa chấn cam + mảnh đá tung lên trong Động Đất.</summary>
        public void QuakeBurst(Vector3 pos, float scale)
        {
            Emit(flash, pos + Vector3.up * 0.4f, Vector3.zero, 2.8f * scale, 0.16f, new Color(1f, 0.65f, 0.18f, 0.85f));
            int dusts = Mathf.Clamp(Mathf.RoundToInt(7 * scale), 4, 14);
            for (int i = 0; i < dusts; i++)
            {
                Vector3 spread = Random.insideUnitSphere * 1.8f * scale;
                spread.y = Mathf.Abs(spread.y) * 2.2f + Random.Range(3.2f, 6.2f) * scale;
                Emit(smoke, pos + new Vector3(Random.Range(-0.9f, 0.9f), 0.25f, Random.Range(-0.9f, 0.9f)),
                    spread, Random.Range(1.2f, 2.3f) * scale, Random.Range(0.65f, 1.15f),
                    new Color(0.68f, 0.58f, 0.46f, 0.68f));
            }
            int chips = Mathf.Clamp(Mathf.RoundToInt(8 * scale), 5, 16);
            for (int i = 0; i < chips; i++)
            {
                Vector3 v = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(6f, 12f), Random.Range(-3.5f, 3.5f)) * Mathf.Sqrt(scale);
                Color col = i % 3 == 0 ? new Color(1f, 0.72f, 0.22f) : new Color(0.55f, 0.44f, 0.34f);
                Emit(sparks, pos + Vector3.up * 0.3f, v, Random.Range(0.14f, 0.26f), Random.Range(0.45f, 0.85f), col);
            }
        }

        /// <summary>Tàn lửa và hơi nóng bốc nhẹ từ vũng dung nham hoặc hố thiên thạch đang nguội.</summary>
        public void MagmaBubble(Vector3 pos, float radius)
        {
            Vector2 r = Random.insideUnitCircle * radius * 0.6f;
            Vector3 p = pos + new Vector3(r.x, 0.15f, r.y);
            Emit(fire, p, Vector3.up * Random.Range(1.6f, 3.2f) + Random.insideUnitSphere * 0.4f,
                Random.Range(0.35f, 0.7f), Random.Range(0.3f, 0.55f), new Color(1f, Random.Range(0.45f, 0.78f), 0.12f));
            if (Random.value < 0.45f)
                Emit(sparks, p, Vector3.up * Random.Range(3.5f, 6.5f) + Random.insideUnitSphere * 1.2f,
                    Random.Range(0.09f, 0.16f), Random.Range(0.35f, 0.65f), new Color(1f, 0.85f, 0.3f));
        }

        /// <summary>Hào quang Siêu Chiến Binh (gọi vài lần mỗi giây).</summary>
        public void Aura(Vector3 pos, Color teamColor)
        {
            Vector3 v = Random.insideUnitSphere * 0.6f; v.y = Random.Range(1.5f, 3f);
            Emit(fire, pos + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0f, 1.5f), Random.Range(-0.6f, 0.6f)), v, Random.Range(0.3f, 0.6f), Random.Range(0.3f, 0.6f), Color.Lerp(teamColor, Color.white, 0.4f));
        }
    }
}
