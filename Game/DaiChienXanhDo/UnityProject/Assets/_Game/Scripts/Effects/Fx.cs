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
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = 0.06f; r.lengthScale = 1.5f; }
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

        /// <summary>Vụ nổ: chớp + lửa + tia lửa + khói. scale 1 ≈ nổ đạn xe tăng.</summary>
        public void Explosion(Vector3 pos, float scale)
        {
            Emit(flash, pos + Vector3.up * 0.5f, Vector3.zero, 6f * scale, 0.18f, new Color(1f, 0.9f, 0.6f));
            int fires = Mathf.RoundToInt(18 * scale);
            for (int i = 0; i < fires; i++)
            {
                Vector3 v = Random.insideUnitSphere * 4f * scale; v.y = Mathf.Abs(v.y) * 1.4f + 1f;
                Emit(fire, pos + Random.insideUnitSphere * 0.4f * scale, v, Random.Range(1f, 2.2f) * scale, Random.Range(0.35f, 0.7f), new Color(1f, Random.Range(0.35f, 0.65f), 0.1f));
            }
            for (int i = 0; i < Mathf.RoundToInt(16 * scale); i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(6f, 14f) * Mathf.Sqrt(scale); v.y = Mathf.Abs(v.y) + 2f;
                Emit(sparks, pos, v, Random.Range(0.1f, 0.25f), Random.Range(0.4f, 0.9f), new Color(1f, 0.75f, 0.3f));
            }
            for (int i = 0; i < Mathf.RoundToInt(10 * scale); i++)
            {
                Vector3 v = Random.insideUnitSphere * 1.5f * scale; v.y = Mathf.Abs(v.y) + 1.2f;
                Emit(smoke, pos + Random.insideUnitSphere * scale, v, Random.Range(2f, 3.5f) * scale, Random.Range(1.2f, 2.2f), new Color(0.25f, 0.24f, 0.23f, 0.7f));
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

        /// <summary>Một hạt lửa (gọi mỗi frame để làm đuôi thiên thạch).</summary>
        public void FireTrail(Vector3 pos, float size)
        {
            Emit(fire, pos + Random.insideUnitSphere * size * 0.3f, Random.insideUnitSphere * 0.5f, size * Random.Range(0.8f, 1.4f), Random.Range(0.25f, 0.45f), new Color(1f, Random.Range(0.4f, 0.7f), 0.15f));
            if (Random.value < 0.3f) Emit(smoke, pos, Vector3.up * 0.5f, size * 1.5f, 0.9f, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        }

        /// <summary>Chớp sáng lớn (sét đánh).</summary>
        public void Flash(Vector3 pos, float size, Color color)
        {
            Emit(flash, pos, Vector3.zero, size, 0.22f, color);
            for (int i = 0; i < 14; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(4f, 10f); v.y = Mathf.Abs(v.y) + 2f;
                Emit(sparks, pos, v, Random.Range(0.1f, 0.22f), Random.Range(0.25f, 0.5f), Color.Lerp(color, Color.white, 0.5f));
            }
        }

        /// <summary>Hào quang Siêu Chiến Binh (gọi vài lần mỗi giây).</summary>
        public void Aura(Vector3 pos, Color teamColor)
        {
            Vector3 v = Random.insideUnitSphere * 0.6f; v.y = Random.Range(1.5f, 3f);
            Emit(fire, pos + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0f, 1.5f), Random.Range(-0.6f, 0.6f)), v, Random.Range(0.3f, 0.6f), Random.Range(0.3f, 0.6f), Color.Lerp(teamColor, Color.white, 0.4f));
        }
    }
}
