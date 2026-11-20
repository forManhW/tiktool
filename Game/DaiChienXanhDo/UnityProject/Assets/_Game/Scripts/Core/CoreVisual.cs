using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Hình lõi: viên pha lê màu phe xoay + nhịp sáng, đèn rọi màu phe. Bị đánh thì lóe sáng; dưới 25% máu nháy gấp; vỡ thì tắt.
    /// </summary>
    public class CoreVisual : MonoBehaviour
    {
        public Transform crystal;
        public Light glow;
        public Renderer[] glowRenderers;

        CoreHealth core;
        Material[] mats;
        Color baseEmission;
        float lastHp, hitFlashUntil;

        void Start()
        {
            core = GetComponent<CoreHealth>();
            Color c = core != null ? core.team.TeamColor() : Color.white;
            baseEmission = c * 0.8f; // vừa đủ sáng; cao hơn thì bloom làm màu bạc thành hồng/xanh nhạt
            if (mats == null && glowRenderers != null) { }
            foreach (var r in glowRenderers ?? new Renderer[0])
                if (r != null) r.material.SetColor("_BaseColor", c * 0.85f);
            if (glowRenderers != null)
            {
                mats = new Material[glowRenderers.Length];
                for (int i = 0; i < glowRenderers.Length; i++) { mats[i] = glowRenderers[i].material; } // bản riêng để nhịp sáng
            }
            lastHp = core != null ? core.Hp : 0f;
        }

        void Update()
        {
            if (core == null) return;
            if (core.Hp < lastHp) hitFlashUntil = Time.time + 0.12f;
            lastHp = core.Hp;

            bool dead = core.Hp <= 0f;
            float low = core.Ratio < 0.25f ? 1f : 0f;
            float pulse = 1f + 0.25f * Mathf.Sin(Time.time * (low > 0 ? 12f : 2.5f));
            float k = dead ? 0.05f : (Time.time < hitFlashUntil ? 2.2f : pulse);

            if (crystal != null && !dead)
            {
                crystal.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);
                crystal.localPosition = new Vector3(0f, crystal.localPosition.y + Mathf.Sin(Time.time * 1.8f) * 0.002f, 0f);
            }
            if (mats != null)
                foreach (var m in mats) if (m != null) m.SetColor("_EmissionColor", baseEmission * k);
            if (glow != null) glow.intensity = dead ? 0f : 4f * k;
        }
    }
}
