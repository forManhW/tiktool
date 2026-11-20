using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Lõi của một phe. Không có cách hồi máu; về 0 là thua ván.</summary>
    public class CoreHealth : MonoBehaviour
    {
        public Team team;
        public float radius = 1.8f;

        /// <summary>Độ cao đặt thanh máu lõi (lâu đài cao khoảng 10m).</summary>
        public static float BarHeight = 9.5f;

        public float Radius => radius;
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Ratio => MaxHp > 0f ? Hp / MaxHp : 0f;

        Vector3 baseScale;
        float shakeUntil;

        void Awake()
        {
            baseScale = transform.localScale;
            ResetHp();
        }

        public void ResetHp()
        {
            MaxHp = GameSettings.Current.coreMaxHp;
            Hp = MaxHp;
            transform.localScale = baseScale;
            GameEvents.RaiseCoreHealthChanged(team, Hp, MaxHp);
        }

        public void TakeDamage(float amount, Viewer by)
        {
            var match = MatchManager.I;
            if (match == null || !match.IsFighting || Hp <= 0f || amount <= 0f) return;
            Hp = Mathf.Max(0f, Hp - amount);
            shakeUntil = Time.time + 0.15f;
            GameEvents.RaiseCoreHealthChanged(team, Hp, MaxHp);
            if (Hp <= 0f) match.OnCoreDestroyed(team);
        }

        Transform castle;
        float nextRubbleAt;

        void Update()
        {
            if (castle == null) castle = transform.Find("Castle");
            if (Hp <= 0f)
            {
                // Lâu đài sụp: rung mạnh, nghiêng, chìm xuống đất, khói bụi bốc lên
                if (castle != null)
                {
                    var p = castle.localPosition;
                    castle.localPosition = new Vector3(Random.Range(-0.08f, 0.08f), Mathf.MoveTowards(p.y, -9f, 3.5f * Time.deltaTime), Random.Range(-0.08f, 0.08f));
                    castle.localRotation = Quaternion.Slerp(castle.localRotation, Quaternion.Euler(8f, castle.localEulerAngles.y, 6f), 0.8f * Time.deltaTime);
                    if (Time.time >= nextRubbleAt && p.y > -8f) { nextRubbleAt = Time.time + 0.12f; Fx.I.Dust(transform.position + Random.insideUnitSphere * 2.5f, 3f); }
                }
                else transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(baseScale.x * 1.4f, baseScale.y * 0.1f, baseScale.z * 1.4f), 5f * Time.deltaTime);
                return;
            }
            if (castle != null && castle.localPosition.y < 0f) { castle.localPosition = Vector3.zero; castle.localRotation = Quaternion.Euler(0f, castle.localEulerAngles.y, 0f); }
            float s = Time.time < shakeUntil ? 1f + Random.Range(-0.03f, 0.03f) : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * s, 20f * Time.deltaTime);
        }
    }
}
