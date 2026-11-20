using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Một chiến binh. AI: tìm địch gần nhất → đi tới → đấm. Phe địch hết lính thì đi đánh lõi địch.
    /// Không dùng physics để chạy nhẹ với 200 lính.
    /// </summary>
    public class Fighter : MonoBehaviour
    {
        public Team Team { get; private set; }
        public Viewer Owner { get; private set; }
        public bool IsMain { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsSuper => Time.time < superUntil;
        public float SpawnTime { get; private set; }
        public string Label => Owner != null ? Owner.Nickname : "";

        [SerializeField] Transform body;      // phần hình để làm hoạt ảnh đấm/biến hình
        Renderer[] renderers;
        Fighter target;
        float retargetAt, nextPunchAt, superUntil, flashUntil, lungeUntil;
        Vector3 knock, personalOffset;
        int chargedPunches, comboPunches;
        bool superVisual;

        public void SetBody(Transform bodyTransform) { body = bodyTransform; }

        public void Init(Team team, Viewer owner, bool isMain, Vector3 position)
        {
            var s = GameSettings.Current;
            Team = team; Owner = owner; IsMain = isMain;
            MaxHp = s.fighterHp; Hp = MaxHp;
            IsAlive = true;
            SpawnTime = Time.time;
            target = null; retargetAt = 0f; nextPunchAt = Time.time + Random.Range(0f, 0.5f);
            superUntil = 0f; flashUntil = 0f; lungeUntil = 0f;
            knock = Vector3.zero;
            chargedPunches = 0; comboPunches = 0;
            personalOffset = Random.insideUnitSphere * 1.5f; personalOffset.y = 0f;
            transform.position = position;
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            renderers = GetComponentsInChildren<Renderer>(true);
            superVisual = true; ApplySuperVisual(false);
            gameObject.SetActive(true);
        }

        public void AddChargedPunches(int count) { chargedPunches += Mathf.Max(1, count); nextPunchAt = Mathf.Min(nextPunchAt, Time.time); }
        public void AddCombo(int punches) { comboPunches += Mathf.Max(1, punches); nextPunchAt = Mathf.Min(nextPunchAt, Time.time); }

        public void MakeSuper(float seconds)
        {
            superUntil = Mathf.Max(superUntil, Time.time) + seconds;
            Hp = MaxHp;
        }

        public void TakeHit(float damage, Vector3 knockback)
        {
            if (!IsAlive) return;
            if (IsSuper) { damage *= 0.25f; knockback *= 0.2f; }
            Hp -= damage;
            knock += knockback;
            flashUntil = Time.time + 0.08f;
            if (Hp <= 0f) Die();
        }

        public void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            FighterManager.I.Release(this);
        }

        void Update()
        {
            if (!IsAlive) return;
            var match = MatchManager.I;
            if (match == null || !match.IsFighting) { Animate(); return; }

            float dt = Time.deltaTime;
            var s = GameSettings.Current;
            Vector3 pos = transform.position;

            if (knock.sqrMagnitude > 0.01f)
            {
                pos += knock * dt;
                knock = Vector3.Lerp(knock, Vector3.zero, 6f * dt);
            }

            if (target == null || !target.IsAlive || Time.time >= retargetAt)
            {
                target = FighterManager.I.NearestEnemy(this);
                retargetAt = Time.time + Random.Range(0.3f, 0.6f);
            }

            CoreHealth enemyCore = null;
            Vector3 aim;      // thứ bị đánh (để tính tầm đấm và hướng mặt)
            Vector3 dest;     // điểm đi tới
            float range;
            if (target != null)
            {
                range = IsSuper ? 1.8f : 1.2f;
                aim = target.transform.position;
                // Mỗi lính tiếp cận từ một hướng riêng để vây quanh mục tiêu, không dồn thành một cục.
                Vector3 side = personalOffset.sqrMagnitude > 0.01f ? personalOffset.normalized : Vector3.forward;
                dest = aim + side * range * 0.75f;
            }
            else
            {
                enemyCore = match.Core(Team.Other());
                if (enemyCore == null) { transform.position = pos; Animate(); return; }
                range = enemyCore.Radius + 0.9f;
                aim = enemyCore.transform.position;
                Vector3 side = personalOffset.sqrMagnitude > 0.01f ? personalOffset.normalized : Vector3.forward;
                dest = aim + side * (enemyCore.Radius + 0.6f);
            }

            Vector3 toAim = aim - pos; toAim.y = 0f;
            float aimDist = toAim.magnitude;
            Vector3 toDest = dest - pos; toDest.y = 0f;
            float destDist = toDest.magnitude;

            if (aimDist <= range)
            {
                if (Time.time >= nextPunchAt) Punch(target, enemyCore);
                if (destDist > 0.05f) pos += toDest / destDist * Mathf.Min(destDist, s.fighterSpeed * 0.3f * dt); // nhích dần vào chỗ vây
            }
            else if (destDist > 0.01f)
            {
                float speed = s.fighterSpeed * (IsSuper ? 1.6f : 1f);
                pos += toDest / destDist * Mathf.Min(destDist, speed * dt);
            }
            if (aimDist > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toAim / aimDist), 10f * dt);

            pos.y = 0f;
            transform.position = Arena.Clamp(pos);
            Animate();
        }

        void Punch(Fighter enemy, CoreHealth core)
        {
            var s = GameSettings.Current;
            float power = TeamPower.Of(Team);
            float damage = s.fighterDamage * power;
            float interval = s.punchInterval;
            float knockForce = 2f;

            if (comboPunches > 0) { comboPunches--; damage *= 2f; interval = 0.15f; knockForce = 3f; }
            else if (chargedPunches > 0) { chargedPunches--; damage *= 4f; knockForce = 9f; flashUntil = Time.time + 0.15f; }
            if (IsSuper) { damage *= 5f; knockForce *= 1.5f; interval *= 0.6f; }

            nextPunchAt = Time.time + interval;
            lungeUntil = Time.time + 0.12f;

            if (enemy != null)
            {
                Vector3 dir = enemy.transform.position - transform.position; dir.y = 0f;
                enemy.TakeHit(damage, dir.normalized * knockForce);
            }
            else if (core != null)
            {
                core.TakeDamage(s.coreDamagePerPunch * power * (IsSuper ? 3f : 1f), Owner);
            }
        }

        void Animate()
        {
            if (body == null) return;
            bool super = IsSuper;
            if (super != superVisual) ApplySuperVisual(super);

            float lunge = Time.time < lungeUntil ? 0.35f : 0f;
            float hover = super ? 1.2f + Mathf.Sin(Time.time * 4f) * 0.15f : 0f;
            Vector3 targetLocal = new Vector3(0f, 1f + hover, lunge);
            body.localPosition = Vector3.Lerp(body.localPosition, targetLocal, 25f * Time.deltaTime);

            float flash = Time.time < flashUntil ? 1.25f : 1f;
            float scale = (super ? 1.6f : 1f) * flash;
            body.localScale = Vector3.Lerp(body.localScale, Vector3.one * scale, 20f * Time.deltaTime);
        }

        void ApplySuperVisual(bool super)
        {
            superVisual = super;
            var assets = GameAssets.I;
            var mat = super && assets.superMaterial != null ? assets.superMaterial : assets.TeamMaterial(Team);
            if (mat == null || renderers == null) return;
            // Chỉ đổi vật liệu cho greybox; prefab model thật tự lo hình dáng của nó.
            if (assets.FighterPrefab(Team) != null) return;
            foreach (var r in renderers) r.sharedMaterial = mat;
        }
    }
}
