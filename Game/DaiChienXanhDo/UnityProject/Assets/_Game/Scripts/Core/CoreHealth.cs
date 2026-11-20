using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Lõi của một phe. Không có cách hồi máu; về 0 là thua ván.</summary>
    public class CoreHealth : MonoBehaviour
    {
        public Team team;
        public float radius = 1.8f;

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

        void Update()
        {
            if (Hp <= 0f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(baseScale.x * 1.4f, baseScale.y * 0.1f, baseScale.z * 1.4f), 5f * Time.deltaTime);
                return;
            }
            float s = Time.time < shakeUntil ? 1f + Random.Range(-0.05f, 0.05f) : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * s, 20f * Time.deltaTime);
        }
    }
}
