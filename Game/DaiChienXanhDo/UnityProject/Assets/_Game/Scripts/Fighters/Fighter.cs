using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Một chiến binh. AI: tìm địch gần nhất → đi tới → đấm. Phe địch hết lính thì đi đánh lõi địch.
    /// Có model thật (GameAssets.fighterModels) thì dùng hoạt ảnh: Speed, Punch, Hit, Dead, Win.
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
        public bool IsDying => dying;
        public bool IsSuper => Time.time < superUntil;
        public float SpawnTime { get; private set; }
        public string Label => Owner != null ? Owner.Nickname : "";

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int PunchId = Animator.StringToHash("Punch");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");
        static readonly int WinId = Animator.StringToHash("Win");

        [SerializeField] Transform body;      // phần hình để làm hiệu ứng nhún/biến hình
        Animator anim;
        Renderer[] renderers;
        Material[][] originalMaterials;
        float bodyBaseY;
        Fighter target;
        Vehicle targetVehicle;   // xe tăng / robot địch đang vây đánh
        float retargetAt, nextPunchAt, superUntil, flashUntil, lungeUntil, deathAt, nextAuraAt;
        Vector3 knock, personalOffset, lastPos;
        int chargedPunches, comboPunches;
        bool superVisual, dying;

        Transform hpBar, hpFill;
        static MaterialPropertyBlock flashBlock;
        bool flashing;
        float whiteUntil;

        public void SetHpBar(Transform bar, Transform fill) { hpBar = bar; hpFill = fill; }

        /// <summary>Thanh máu: chỉ hiện khi đã mất máu, luôn quay về camera.</summary>
        void UpdateHpBar()
        {
            if (hpBar == null) return;
            float ratio = MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f;
            bool show = IsAlive && (IsMain || IsSuper) && ratio < 0.999f; // nhân vật chính và siêu nhân
            if (hpBar.gameObject.activeSelf != show) hpBar.gameObject.SetActive(show);
            if (!show) return;
            var cam = CameraRig.I != null ? CameraRig.I.cam : null;
            if (cam != null) hpBar.rotation = cam.transform.rotation;
            float h = GameAssets.I.fighterHeight;
            hpBar.localPosition = new Vector3(0f, IsSuper ? h * 1.6f + 1.5f : h + 0.35f, 0f); // Siêu Chiến Binh to x1,6 và bay cao 1,2
            hpFill.localScale = new Vector3(ratio, hpFill.localScale.y, 1f);
            hpFill.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0f, 0f);
        }

        /// <summary>Trúng đòn: cả người chớp trắng một thoáng.</summary>
        void UpdateHitFlash()
        {
            bool want = Time.time < whiteUntil;
            if (want == flashing || renderers == null) return;
            flashing = want;
            if (flashBlock == null) { flashBlock = new MaterialPropertyBlock(); flashBlock.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f)); }
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(want ? flashBlock : null);
        }

        public void SetBody(Transform bodyTransform, Animator animator = null)
        {
            body = bodyTransform;
            anim = animator;
            bodyBaseY = body != null ? body.localPosition.y : 0f;
            renderers = body != null ? body.GetComponentsInChildren<Renderer>(true) : GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) originalMaterials[i] = renderers[i].sharedMaterials;
        }

        public void Init(Team team, Viewer owner, bool isMain, Vector3 position)
        {
            var s = GameSettings.Current;
            Team = team; Owner = owner; IsMain = isMain;
            MaxHp = s.fighterHp; Hp = MaxHp;
            IsAlive = true; dying = false;
            SpawnTime = Time.time;
            target = null; retargetAt = 0f; nextPunchAt = Time.time + Random.Range(0f, 0.5f);
            superUntil = 0f; flashUntil = 0f; lungeUntil = 0f;
            knock = Vector3.zero;
            chargedPunches = 0; comboPunches = 0;
            personalOffset = Random.insideUnitSphere * 1.5f; personalOffset.y = 0f;
            transform.position = position;
            lastPos = position;
            if (body == null && transform.childCount > 0) SetBody(transform.GetChild(0));
            gameObject.SetActive(true);
            if (anim != null)
            {
                anim.Rebind();
                anim.SetBool(DeadId, false);
                anim.SetBool(WinId, false);
            }
            superVisual = true; ApplySuperVisual(false);
            Fx.I.Dust(position, 0.8f);
        }

        public void AddChargedPunches(int count) { chargedPunches += Mathf.Max(1, count); nextPunchAt = Mathf.Min(nextPunchAt, Time.time); }
        public void AddCombo(int punches) { comboPunches += Mathf.Max(1, punches); nextPunchAt = Mathf.Min(nextPunchAt, Time.time); }

        public void MakeSuper(float seconds)
        {
            superUntil = Mathf.Max(superUntil, Time.time) + seconds;
            Hp = MaxHp;
            Fx.I.Flash(transform.position + Vector3.up * 1.5f, 4f, Team.TeamColor());
        }

        /// <summary>Tên trạng thái hoạt ảnh hiện tại (để test): Idle, Walk, Punch, Hit, Death, Victory.</summary>
        public string AnimState
        {
            get
            {
                if (anim == null || !anim.isActiveAndEnabled) return "none";
                var info = anim.GetCurrentAnimatorStateInfo(0);
                foreach (var n in new[] { "Idle", "Walk", "Punch", "Hit", "Death", "Victory" }) if (info.IsName(n)) return n;
                return "?";
            }
        }

        /// <summary>Bị đẩy nhẹ (tránh chồng lên lính khác). Không tính là đi bộ.</summary>
        public void Nudge(Vector3 offset)
        {
            var p = Arena.Clamp(transform.position + offset);
            lastPos += p - transform.position;
            transform.position = p;
        }

        public void Celebrate()
        {
            if (anim != null && IsAlive) anim.SetBool(WinId, true);
        }

        public void TakeHit(float damage, Vector3 knockback)
        {
            if (!IsAlive) return;
            if (IsSuper) { damage *= 0.25f; knockback *= 0.2f; }
            Hp -= damage;
            knock += knockback;
            flashUntil = Time.time + 0.08f;
            whiteUntil = Time.time + 0.07f;
            // Bị đấm thì giật người lại, trừ khi chính mình đang ra đòn (để 2 bên thấy rõ đấm qua đấm lại)
            if (anim != null && Time.time > lungeUntil + 0.25f) anim.SetTrigger(HitId);
            if (Hp <= 0f) Die();
        }

        /// <summary>Chết: rời khỏi trận ngay (không còn bị nhắm), nhưng nằm lại 1,2 giây để chạy hoạt ảnh gục.</summary>
        public void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            FighterManager.I.Release(this);
            if (anim != null && gameObject.activeInHierarchy)
            {
                dying = true;
                deathAt = Time.time + 1.2f;
                anim.SetBool(DeadId, true);
            }
            else FighterManager.I.ReturnToPool(this);
        }

        void Update()
        {
            if (dying)
            {
                UpdateHpBar();
                if (flashing) { whiteUntil = 0f; UpdateHitFlash(); }
                if (Time.time >= deathAt) { dying = false; FighterManager.I.ReturnToPool(this); }
                return;
            }
            if (!IsAlive) return;
            var match = MatchManager.I;
            if (match == null || !match.IsFighting) { SetSpeed(0f); Animate(); return; }

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
                targetVehicle = Vehicle.Nearest(Team.Other(), transform.position, out _);
                retargetAt = Time.time + Random.Range(0.3f, 0.6f);
            }

            CoreHealth enemyCore = null;
            Vector3 aim;      // thứ bị đánh (để tính tầm đấm và hướng mặt)
            Vector3 dest;     // điểm đi tới
            float range;
            // Xe tăng / robot địch ở gần (không xa hơn lính địch gần nhất quá 6m) thì xông vào vây đánh nó trước
            bool onVehicle = false;
            if (targetVehicle != null && targetVehicle.Alive)
            {
                Vector3 dv = targetVehicle.transform.position - pos; dv.y = 0f;
                float targetDist = target != null ? Vector3.Distance(target.transform.position, pos) : float.MaxValue;
                onVehicle = dv.magnitude < targetDist + 6f;
            }
            if (onVehicle)
            {
                range = targetVehicle.HitRadius + 0.6f;   // đứng ngoài vùng bị cán / giẫm
                aim = targetVehicle.transform.position;
                Vector3 side = personalOffset.sqrMagnitude > 0.01f ? personalOffset.normalized : Vector3.forward;
                dest = aim + side * (targetVehicle.HitRadius + 0.35f);
            }
            else if (target != null)
            {
                range = IsSuper ? 1.9f : 1.3f;
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

            bool walking = false;
            if (aimDist <= range)
            {
                // Đã áp sát: đứng tấn mà đấm. Chỉ nhích rất chậm vào chỗ vây, không tính là đi bộ.
                if (Time.time >= nextPunchAt) Punch(onVehicle ? null : target, onVehicle ? null : enemyCore, onVehicle ? targetVehicle : null);
                if (destDist > 0.05f) pos += toDest / destDist * Mathf.Min(destDist, s.fighterSpeed * 0.15f * dt);
            }
            else if (destDist > 0.01f)
            {
                float speed = s.fighterSpeed * (IsSuper ? 1.6f : 1f);
                pos += toDest / destDist * Mathf.Min(destDist, speed * dt);
                walking = true;
            }
            if (aimDist > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toAim / aimDist), 12f * dt);

            pos.y = 0f;
            pos = Arena.Clamp(pos);
            SetSpeed(walking && dt > 0f ? (pos - lastPos).magnitude / dt : 0f);
            lastPos = pos;
            transform.position = pos;

            if (IsSuper && Time.time >= nextAuraAt) { nextAuraAt = Time.time + 0.05f; Fx.I.Aura(pos, Team.TeamColor()); }
            Animate();
        }

        void SetSpeed(float speed)
        {
            if (anim != null) anim.SetFloat(SpeedId, speed);
        }

        void Punch(Fighter enemy, CoreHealth core, Vehicle vehicle = null)
        {
            var s = GameSettings.Current;
            float power = TeamPower.Of(Team);
            float damage = s.fighterDamage * power;
            float interval = s.punchInterval;
            float knockForce = 2f;
            bool strong = false;

            if (comboPunches > 0) { comboPunches--; damage *= 2f; interval = 0.22f; knockForce = 3f; }
            else if (chargedPunches > 0) { chargedPunches--; damage *= 4f; knockForce = 9f; flashUntil = Time.time + 0.15f; strong = true; }
            if (IsSuper) { damage *= 5f; knockForce *= 1.5f; interval *= 0.6f; strong = true; }

            nextPunchAt = Time.time + interval * Random.Range(0.9f, 1.15f); // lệch nhịp nhẹ để 2 bên không đấm cùng lúc
            lungeUntil = Time.time + 0.3f;
            if (anim != null) { anim.ResetTrigger(PunchId); anim.SetTrigger(PunchId); }

            Vector3 hitPoint;
            if (enemy != null)
            {
                Vector3 dir = enemy.transform.position - transform.position; dir.y = 0f;
                enemy.TakeHit(damage, dir.normalized * knockForce);
                hitPoint = Vector3.Lerp(transform.position, enemy.transform.position, 0.6f) + Vector3.up * (IsSuper ? 2.2f : 1.25f);
            }
            else if (vehicle != null)
            {
                vehicle.TakeDamage(damage);
                hitPoint = Vector3.Lerp(transform.position, vehicle.transform.position, 0.45f) + Vector3.up * 1.4f;
            }
            else if (core != null)
            {
                core.TakeDamage(s.coreDamagePerPunch * power * (IsSuper ? 3f : 1f), Owner);
                hitPoint = Vector3.Lerp(transform.position, core.transform.position, 0.5f) + Vector3.up * 1.4f;
            }
            else return;

            Fx.I.Hit(hitPoint, Team.TeamColor(), strong);
            Sfx.Punch(strong);
        }

        void Animate()
        {
            UpdateHpBar();
            UpdateHitFlash();
            if (body == null) return;
            bool super = IsSuper;
            if (super != superVisual) ApplySuperVisual(super);

            // Có hoạt ảnh thật thì chỉ nhích nhẹ người tới khi đấm; greybox thì lao mạnh hơn cho dễ thấy.
            float lunge = Time.time < lungeUntil - 0.18f ? (anim != null ? 0.2f : 0.35f) : 0f;
            float hover = super ? 1.2f + Mathf.Sin(Time.time * 4f) * 0.15f : 0f;
            Vector3 targetLocal = new Vector3(0f, bodyBaseY + hover, lunge);
            body.localPosition = Vector3.Lerp(body.localPosition, targetLocal, 25f * Time.deltaTime);

            float flash = Time.time < flashUntil ? 1.15f : 1f;
            float scale = (super ? 1.6f : 1f) * flash;
            body.localScale = Vector3.Lerp(body.localScale, Vector3.one * scale, 20f * Time.deltaTime);
        }

        void ApplySuperVisual(bool super)
        {
            superVisual = super;
            if (renderers == null) return;
            var assets = GameAssets.I;
            bool greybox = anim == null && assets.FighterPrefab(Team) == null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (super)
                {
                    var arr = new Material[renderers[i].sharedMaterials.Length];
                    for (int k = 0; k < arr.Length; k++)
                        arr[k] = anim != null && k < originalMaterials[i].Length ? TeamMaterials.Hero(originalMaterials[i][k], Team) : TeamMaterials.Super(Team);
                    renderers[i].sharedMaterials = arr;
                }
                else if (greybox)
                {
                    var mat = assets.TeamMaterial(Team);
                    if (mat != null) renderers[i].sharedMaterial = mat;
                }
                else
                {
                    renderers[i].sharedMaterials = originalMaterials[i];
                }
            }
        }
    }
}
