using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Chạy hiệu ứng theo bảng 2.4 của kế hoạch. Có model thật trong GameAssets thì dùng, không thì dùng khối hình cơ bản.
    /// Bậc 0–3 chạy ngay; bậc 4+ xếp hàng, mỗi cái cách nhau tối thiểu 3 giây để người xem kịp thấy tên.
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager I { get; private set; }

        const float BigEffectSpacing = 3f;
        const float TornadoHeight = 16f;
        const float TornadoKillRadius = 5f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int PunchId = Animator.StringToHash("Punch");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        struct Job { public EffectDef Def; public Team Team; public Viewer By; public int Amount; }

        readonly Queue<Job> bigQueue = new Queue<Job>();
        readonly List<GameObject> spawned = new List<GameObject>();
        MaterialPropertyBlock mpb;
        float bigBusyUntil;

        public int QueuedBigEffects => bigQueue.Count;

        void Awake()
        {
            I = this;
            mpb = new MaterialPropertyBlock();
        }

        public void Play(EffectDef def, Team team, Viewer by, Fighter actor, int amount)
        {
            if (def.Tier >= 4) { bigQueue.Enqueue(new Job { Def = def, Team = team, By = by, Amount = amount }); return; }
            PlaySmall(def, team, by, actor, amount);
        }

        void Update()
        {
            spawned.RemoveAll(go => go == null);
            if (bigQueue.Count == 0 || Time.time < bigBusyUntil) return;
            if (MatchManager.I == null || !MatchManager.I.IsFighting) return;
            var job = bigQueue.Dequeue();
            bigBusyUntil = Time.time + BigEffectSpacing;
            PlayBig(job);
        }

        // ---------------- Bậc 0–3 ----------------

        void PlaySmall(EffectDef def, Team team, Viewer by, Fighter actor, int amount)
        {
            switch (def.Id)
            {
                case "spawn":
                    // Triệu hồi lính mang tên người tặng
                    int made = 0;
                    for (int i = 0; i < amount; i++)
                    {
                        var f = FighterManager.I.Spawn(team, by, false);
                        if (f == null) continue;
                        made++;
                        if (actor == null) actor = f;
                    }
                    if (made < amount) TeamPower.AddLikePower(team, amount - made); // sân đầy: đổi phần dư thành sức mạnh
                    break;
                case "like_power":
                    TeamPower.AddLikePower(team, amount);
                    break;
                case "superhero":
                    // Triệu hồi siêu nhân mang tên người tặng: to, bay, mạnh x5, chịu đòn tốt, sống tới khi bị hạ
                    Fighter first = null;
                    for (int i = 0; i < Mathf.Clamp(amount, 1, 10); i++)
                    {
                        var hero = FighterManager.I.Spawn(team, by, false);
                        if (hero == null) { TeamPower.AddLikePower(team, 5); continue; }
                        hero.MakeSuper(100000f);
                        if (first == null) first = hero;
                    }
                    if (first != null) actor = first;
                    break;
            }
            Announce(def, team, by, amount, actor != null ? actor.transform : null, actor != null ? actor.transform.position : CorePos(team));
        }

        /// <summary>Người tặng quà của phe khác phe mình: hiệu ứng chạy qua một lính tạm mang tên họ.</summary>
        Fighter StandIn(Team team, Viewer by)
        {
            var f = FighterManager.I.Spawn(team, by, false);
            if (f == null)
            {
                var list = FighterManager.I.RandomAlive(team, 1);
                f = list.Count > 0 ? list[0] : null;
            }
            return f;
        }

        // ---------------- Bậc 4+ ----------------

        void PlayBig(Job job)
        {
            switch (job.Def.Id)
            {
                case "lightning": StartCoroutine(Lightning(job.Team, job.By, job.Amount)); break;
                case "tank":
                    for (int i = 0; i < Mathf.Clamp(job.Amount, 1, 3); i++) StartCoroutine(Tank(job.Team, job.By, i));
                    break;
                case "meteor": StartCoroutine(Meteor(job.Team, job.By, Mathf.Clamp(job.Amount, 1, 3))); break;
                case "giant_robot": StartCoroutine(Robot(job.Team, job.By)); break;
            }
        }

        IEnumerator Lightning(Team team, Viewer by, int strikes)
        {
            Team enemy = team.Other();
            Announce(CommandRouter.Effects["lightning"], team, by, strikes, null, Vector3.Lerp(CorePos(enemy), Vector3.zero, 0.5f));
            for (int s = 0; s < Mathf.Clamp(strikes, 1, 10); s++)
            {
                var targets = FighterManager.I.RandomAlive(enemy, 5);
                if (targets.Count == 0)
                {
                    Strike(CorePos(enemy));
                    Core(enemy)?.TakeDamage(30f * TeamPower.Of(team), by);
                }
                foreach (var f in targets) { Strike(f.transform.position); f.Die(); }
                Sfx.Thunder();
                if (s == 0) CameraRig.I?.Shake(0.6f, 0.35f); // chỉ rung ở cú sét đầu
                yield return new WaitForSeconds(0.45f);
            }
        }

        void Strike(Vector3 at)
        {
            Bolt(at);
            Fx.I.Flash(at + Vector3.up * 0.8f, 5f, new Color(0.7f, 0.85f, 1f));
            Fx.I.Dust(at, 1.2f);
        }

        IEnumerator Tank(Team team, Viewer by, int index)
        {
            Team enemy = team.Other();
            var tank = MakeTank(team);
            var tankHp = tank.AddComponent<Vehicle>();
            tankHp.Init(team, by, 600f, 20f, 3.4f, 2.6f); // thanh máu + tên người triệu hồi
            Vector3 start = CorePos(team) + new Vector3(index * 2.5f - 2.5f, 0f, -(index * 2.5f - 2.5f));
            tank.transform.position = start;
            if (index == 0) Announce(CommandRouter.Effects["tank"], team, by, 1, tank.transform, start); // một lượt quà = một thông báo
            Fx.I.Dust(start, 2.5f);

            float end = Time.time + 20f, nextCrush = 0f, nextShot = Time.time + 1f, nextDust = 0f;
            while (tank != null && tankHp.Alive)
            {
                Vector3 target = CorePos(enemy);
                Vector3 to = target - tank.transform.position; to.y = 0f;
                bool moving = to.magnitude > 6f;
                if (moving) tank.transform.position += to.normalized * 4f * Time.deltaTime;
                if (to.sqrMagnitude > 0.01f) tank.transform.rotation = Quaternion.Slerp(tank.transform.rotation, Quaternion.LookRotation(to.normalized), 4f * Time.deltaTime);
                if (moving && Time.time >= nextDust) { nextDust = Time.time + 0.15f; Fx.I.Dust(tank.transform.position - tank.transform.forward * 1.5f, 0.6f); }
                if (Time.time >= nextCrush) { nextCrush = Time.time + 0.2f; FighterManager.I.KillInRadius(enemy, tank.transform.position, 2f); }
                if (Time.time >= nextShot)
                {
                    nextShot = Time.time + 2f;
                    Vector3 muzzle = tank.transform.position + tank.transform.forward * 2.4f + Vector3.up * 1.4f;
                    Fx.I.Flash(muzzle, 2.5f, new Color(1f, 0.8f, 0.4f));
                    Fx.I.Dust(muzzle, 0.9f);
                    Sfx.TankShot();
                    StartCoroutine(Shell(muzzle, target + Vector3.up, team, by, 20f));
                }
                yield return null;
            }
            if (tank != null) { Fx.I.Explosion(tank.transform.position + Vector3.up, 1.4f); Sfx.Explosion(0.9f); Destroy(tank); }
        }

        IEnumerator Shell(Vector3 from, Vector3 to, Team team, Viewer by, float coreDamage)
        {
            var shell = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            Destroy(shell.GetComponent<Collider>());
            shell.transform.localScale = Vector3.one * 0.45f;
            SetMat(shell, GameAssets.I.meteorMaterial);
            for (float t = 0f; t < 1f && shell != null; t += Time.deltaTime / 0.5f)
            {
                shell.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 3f;
                Fx.I.FireTrail(shell.transform.position, 0.35f);
                yield return null;
            }
            if (shell != null) Destroy(shell);
            Fx.I.Explosion(to, 1f);
            Sfx.Explosion(0.8f);
            Core(team.Other())?.TakeDamage(coreDamage * TeamPower.Of(team), by);
        }

        IEnumerator Meteor(Team team, Viewer by, int waves)
        {
            Team enemy = team.Other();
            // Dồn mưa vào chỗ quân địch đang đứng (không rải khắp nửa sân) để camera quay trọn cả trận mưa
            Vector3 zone = EnemyCenter(enemy);
            Announce(CommandRouter.Effects["meteor"], team, by, waves, null, zone);
            for (int w = 0; w < waves; w++)
            {
                if (w > 0) zone = EnemyCenter(enemy);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 r = Random.insideUnitCircle * 9f;
                    StartCoroutine(FallingRock(Arena.Clamp(zone + new Vector3(r.x, 0f, r.y)), enemy, 2.8f, Random.Range(1.8f, 2.5f), true));
                    yield return new WaitForSeconds(0.25f);
                }
                Core(enemy)?.TakeDamage(80f * TeamPower.Of(team), by);
                CameraRig.I?.Shake(0.9f, 0.6f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>Tâm đám quân địch; không còn ai thì chỗ giữa lõi địch và giữa sân.</summary>
        static Vector3 EnemyCenter(Team enemy)
        {
            var list = FighterManager.I.List(enemy);
            if (list.Count == 0) return Vector3.Lerp(CorePos(enemy), Vector3.zero, 0.3f);
            Vector3 sum = Vector3.zero;
            foreach (var f in list) sum += f.transform.position;
            return sum / list.Count;
        }

        IEnumerator FallingRock(Vector3 point, Team? victims, float killRadius, float size, bool fiery, bool volcanoSplash = false)
        {
            EnsureVfxResources();

            // Khung mang thiên thạch (tách riêng phần xoay đá và đuôi lửa hướng theo vận tốc)
            var carrier = Track(new GameObject(fiery ? "MeteorCarrier" : "RockCarrier"));
            var prefab = GameAssets.I.meteorPrefab;
            var rock = prefab != null ? Instantiate(prefab, carrier.transform, false) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (prefab == null) rock.transform.SetParent(carrier.transform, false);
            foreach (var col in rock.GetComponentsInChildren<Collider>()) Destroy(col);
            float h = prefab != null ? FighterManager.MeasureHeight(rock) : 1f;
            rock.transform.localScale *= size / Mathf.Max(0.01f, h);
            if (prefab == null || fiery) SetMat(rock, fiery ? MeteorGlow() : GameAssets.I.meteorMaterial);

            Vector3 from = point + new Vector3(10f, 25f, 10f); // rơi chéo rõ đường bay, tránh lọt sát ống kính camera
            Vector3 fallDir = (point - from).normalized;
            carrier.transform.position = from;
            carrier.transform.localScale = Vector3.one * 0.05f;

            GameObject plasmaRoot = null;
            TrailRenderer trail = null;
            GameObject telegraphRoot = null;
            Renderer teleDiscR = null, teleOuterR = null, teleInnerR = null;
            Transform teleInnerTr = null;
            float telegraphRadius = killRadius > 0.1f ? killRadius : Mathf.Max(1.9f, size * 1.05f);

            if (fiery)
            {
                // Lớp vỏ plasma lửa gọn sắc ôm theo thân đá và kéo đuôi ngược hướng rơi (trong suốt cam-đỏ, không chói vàng bệt)
                plasmaRoot = new GameObject("PlasmaCone");
                plasmaRoot.transform.SetParent(carrier.transform, false);
                plasmaRoot.transform.rotation = Quaternion.LookRotation(fallDir);

                var outerShell = MakeVfxMesh("PlasmaOuter", plasmaRoot.transform, plasmaConeMesh, matPlasmaOuter);
                outerShell.transform.localScale = new Vector3(size * 0.72f, size * 0.72f, size * 0.92f);
                var innerShell = MakeVfxMesh("PlasmaCore", plasmaRoot.transform, plasmaConeMesh, matPlasmaCore);
                innerShell.transform.localScale = new Vector3(size * 0.46f, size * 0.46f, size * 0.64f);

                // Dải lửa TrailRenderer rực trắng -> cam -> đỏ cam dọc đường bay
                trail = carrier.AddComponent<TrailRenderer>();
                trail.sharedMaterial = matMeteorTrail;
                trail.time = 0.36f;
                trail.minVertexDistance = 0.25f;
                trail.startWidth = size * 0.72f;
                trail.endWidth = 0.06f;
                trail.numCapVertices = 4;
                trail.textureMode = LineTextureMode.Stretch;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.colorGradient = meteorTrailGradient;
                trail.Clear();

                // Vòng cảnh báo điểm rơi ngay trên mặt đất
                telegraphRoot = Track(new GameObject("MeteorTelegraph"));
                telegraphRoot.transform.position = new Vector3(point.x, 0f, point.z);

                var disc = MakeVfxMesh("DangerDisc", telegraphRoot.transform, discMesh, matSolidAlpha);
                disc.transform.localPosition = new Vector3(0f, 0.065f, 0f);
                disc.transform.localScale = Vector3.one * telegraphRadius;
                teleDiscR = disc.GetComponent<Renderer>();

                var outer = MakeVfxMesh("OuterRing", telegraphRoot.transform, ringMesh, matSolidAdditive);
                outer.transform.localPosition = new Vector3(0f, 0.085f, 0f);
                outer.transform.localScale = Vector3.one * telegraphRadius;
                teleOuterR = outer.GetComponent<Renderer>();

                var inner = MakeVfxMesh("LockRing", telegraphRoot.transform, ringMesh, matSolidAdditive);
                inner.transform.localPosition = new Vector3(0f, 0.095f, 0f);
                inner.transform.localScale = Vector3.one * (telegraphRadius * 0.16f);
                teleInnerTr = inner.transform;
                teleInnerR = inner.GetComponent<Renderer>();
            }

            Vector3 spin = Random.onUnitSphere * 340f;
            float fallDuration = fiery ? 1.1f : 0.6f;
            float fxAccum = 0f;
            const float fxStep = 1f / 45f;

            for (float t = 0f; t < 1f && carrier != null; t += Time.deltaTime / fallDuration)
            {
                float progress = Mathf.Clamp01(t);
                // Tăng tốc rơi mượt theo đường cong và phóng to êm trong 0.15s đầu để không che đột ngột ống kính
                float spawnIn = Mathf.SmoothStep(0.05f, 1f, Mathf.Clamp01(progress / 0.15f));
                float curve = progress * progress * 0.8f + progress * 0.2f;
                Vector3 pos = Vector3.Lerp(from, point, curve);
                carrier.transform.position = pos;
                carrier.transform.localScale = Vector3.one * spawnIn;
                if (rock != null) rock.transform.Rotate(spin * Time.deltaTime);

                if (fiery)
                {
                    if (plasmaRoot != null)
                    {
                        float flicker = 0.94f + 0.12f * Mathf.Sin(Time.time * 42f + size * 7f);
                        plasmaRoot.transform.localScale = new Vector3(1f, 1f, flicker);
                    }

                    // Cập nhật vòng cảnh báo khóa mục tiêu (vòng trong mở rộng khớp khít vòng ngoài đúng lúc t=1)
                    if (telegraphRoot != null)
                    {
                        float popIn = Mathf.Clamp01(progress / 0.14f);
                        float pulse = 0.5f + 0.5f * Mathf.Sin(progress * 26f);
                        float urgency = Mathf.Clamp01((progress - 0.72f) / 0.28f);

                        Color discCol = new Color(0.95f, 0.14f, 0.03f, Mathf.Lerp(0.14f, 0.28f, progress) * popIn);
                        SetBlockColor(teleDiscR, ToLinearColor(discCol));

                        Color outerCol = Color.Lerp(new Color(1f, 0.28f, 0.05f, 0.88f), new Color(1f, 0.82f, 0.26f, 0.96f), urgency * 0.65f);
                        SetBlockColor(teleOuterR, ToLinearColor(outerCol, 1.38f + urgency * 0.45f));

                        float innerScale = Mathf.Lerp(0.16f, 1.0f, progress) * telegraphRadius;
                        teleInnerTr.localScale = new Vector3(innerScale, 1f, innerScale);
                        Color innerCol = Color.Lerp(new Color(1f, 0.72f, 0.14f, 0.86f), new Color(1f, 0.95f, 0.68f, 0.96f), urgency * 0.75f + pulse * 0.2f);
                        SetBlockColor(teleInnerR, ToLinearColor(innerCol, 1.52f + urgency * 0.42f));
                    }

                    // Đuôi lửa + khói đen + tia tàn lửa tiết chế theo bước thời gian cố định
                    fxAccum += Time.deltaTime;
                    if (fxAccum >= fxStep)
                    {
                        fxAccum -= fxStep;
                        Fx.I.FireTrail(pos, size * 0.88f);
                        Fx.I.SmokeTrail(pos - fallDir * (size * 0.65f), size * 0.85f);
                        Fx.I.MeteorEmbers(pos - fallDir * (size * 0.3f), -fallDir, size);
                    }
                }
                yield return null;
            }

            if (telegraphRoot != null) Destroy(telegraphRoot);

            if (carrier != null)
            {
                if (fiery && trail != null)
                {
                    // Ẩn đá và vỏ plasma ngay khi va chạm, giữ dải lửa tan tự nhiên 0.35s
                    if (rock != null) rock.SetActive(false);
                    if (plasmaRoot != null) plasmaRoot.SetActive(false);
                    Destroy(carrier, 0.35f);
                }
                else
                {
                    Destroy(carrier);
                }
            }

            if (fiery)
            {
                float impactScale = volcanoSplash ? 1.08f : 1.35f;
                Fx.I.Explosion(point, impactScale);
                Sfx.Explosion(volcanoSplash ? 0.55f : 0.7f);

                float shockMaxR = Mathf.Max(telegraphRadius, size) * 1.82f;
                StartCoroutine(ImpactShockwave(point, shockMaxR, 0.45f));
                StartCoroutine(ScorchedCrater(point, telegraphRadius * (volcanoSplash ? 0.95f : 1.05f), volcanoSplash ? 3.5f : 4.5f, volcanoSplash));
            }
            else
            {
                Fx.I.Dust(point, size * 1.5f);
            }

            if (victims.HasValue) FighterManager.I.KillInRadius(victims.Value, point, killRadius);
        }

        /// <summary>Sóng xung kích 2 vòng màu vàng-cam lan nhanh trên mặt đất khi thiên thạch/dung nham rơi trúng.</summary>
        IEnumerator ImpactShockwave(Vector3 point, float maxRadius, float duration)
        {
            EnsureVfxResources();
            var root = Track(new GameObject("ImpactShockwave"));
            root.transform.position = new Vector3(point.x, 0f, point.z);

            var lead = MakeVfxMesh("ShockLead", root.transform, ringMesh, matSolidAdditive);
            lead.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            var leadR = lead.GetComponent<Renderer>();

            var trailRing = MakeVfxMesh("ShockTrail", root.transform, ringMesh, matSolidAdditive);
            trailRing.transform.localPosition = new Vector3(0f, 0.105f, 0f);
            var trailR = trailRing.GetComponent<Renderer>();

            for (float t = 0f; t < 1f && root != null; t += Time.deltaTime / duration)
            {
                float p = Mathf.Clamp01(t);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                float rLead = Mathf.Lerp(0.55f, maxRadius, ease);
                float rTrail = Mathf.Lerp(0.35f, maxRadius * 0.82f, Mathf.Pow(ease, 1.15f));
                float fade = 1f - p * p;

                lead.transform.localScale = new Vector3(rLead, 1f, rLead);
                trailRing.transform.localScale = new Vector3(rTrail, 1f, rTrail);

                Color leadCol = Color.Lerp(new Color(1f, 0.94f, 0.68f, fade * 0.92f), new Color(1f, 0.52f, 0.1f, fade * 0.8f), p);
                Color trailCol = new Color(1f, 0.32f, 0.05f, fade * 0.72f);
                SetBlockColor(leadR, ToLinearColor(leadCol, 1.68f));
                SetBlockColor(trailR, ToLinearColor(trailCol, 1.38f));
                yield return null;
            }
            if (root != null) Destroy(root);
        }

        /// <summary>Hố cháy đen + khe dung nham phát sáng nguội dần (hoặc vũng dung nham sủi bọt cho Núi Lửa).</summary>
        IEnumerator ScorchedCrater(Vector3 point, float radius, float lifetime, bool magmaSplash)
        {
            EnsureVfxResources();
            var root = Track(new GameObject(magmaSplash ? "MagmaSplash" : "ScorchedCrater"));
            root.transform.position = new Vector3(point.x, 0f, point.z);
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var scorch = MakeVfxMesh("Scorch", root.transform, discMesh, magmaSplash ? matMagmaSplashBase : matCraterScorch);
            scorch.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            scorch.transform.localScale = Vector3.one * radius;
            var scorchR = scorch.GetComponent<Renderer>();

            var glow = MakeVfxMesh("MagmaGlow", root.transform, discMesh, magmaSplash ? matMagmaSplashGlow : matCraterMagma);
            glow.transform.localPosition = new Vector3(0f, 0.062f, 0f);
            glow.transform.localScale = Vector3.one * (radius * (magmaSplash ? 0.92f : 0.86f));
            var glowR = glow.GetComponent<Renderer>();

            float nextBubble = Time.time + Random.Range(0.2f, 0.5f);
            float elapsed = 0f;
            while (elapsed < lifetime && root != null)
            {
                elapsed += Time.deltaTime;
                float norm = Mathf.Clamp01(elapsed / lifetime);

                // Lớp than đen giữ sắc nét phần lớn thời gian rồi mờ dần ở 32% cuối
                float scorchAlpha = norm < 0.68f ? 0.92f : Mathf.Lerp(0.92f, 0f, (norm - 0.68f) / 0.32f);
                SetBlockColor(scorchR, new Color(1f, 1f, 1f, scorchAlpha));

                // Khe dung nham nhấp nháy nóng rực rồi nguội đỏ sẫm dần
                float glowFade = Mathf.Clamp01(1f - norm / 0.86f);
                float pulse = 0.82f + 0.18f * Mathf.Sin(Time.time * (magmaSplash ? 8.5f : 5.5f) + point.x);
                Color magmaCol = Color.Lerp(new Color(1f, 0.72f, 0.2f, glowFade), new Color(0.95f, 0.18f, 0.03f, glowFade * 0.7f), norm);
                SetBlockColor(glowR, ToLinearColor(magmaCol, (magmaSplash ? 2.25f : 2.0f) * pulse));

                if (glowFade > 0.25f && Time.time >= nextBubble)
                {
                    nextBubble = Time.time + (magmaSplash ? Random.Range(0.3f, 0.55f) : Random.Range(0.5f, 0.9f));
                    Fx.I.MagmaBubble(point, radius * 0.75f);
                }
                yield return null;
            }
            if (root != null) Destroy(root);
        }

        IEnumerator Robot(Team team, Viewer by)
        {
            Team enemy = team.Other();
            var robot = MakeRobot(team, out Animator anim);
            var robotHp = robot.AddComponent<Vehicle>();
            robotHp.Init(team, by, 2500f, 25f, GameAssets.I.robotHeight + 1.2f, 4f);
            Vector3 land = CorePos(team) + (Vector3.zero - CorePos(team)).normalized * 4f;
            robot.transform.position = land + Vector3.up * 25f;
            Vector3 face = CorePos(enemy) - land; face.y = 0f;
            robot.transform.rotation = Quaternion.LookRotation(face.normalized);
            Announce(CommandRouter.Effects["giant_robot"], team, by, 1, robot.transform, land);

            // Rơi xuống sân
            for (float t = 0f; t < 1f && robot != null; t += Time.deltaTime / 0.8f)
            {
                robot.transform.position = Vector3.Lerp(land + Vector3.up * 25f, land, t * t);
                yield return null;
            }
            Fx.I.Dust(land, 6f);
            Fx.I.Explosion(land, 1.2f);
            Sfx.Stomp(); Sfx.Explosion(1f);
            FighterManager.I.KillInRadius(enemy, land, 4f);
            CameraRig.I?.Shake(1.5f, 0.8f);

            float end = Time.time + 25f, nextStomp = 0f;
            while (robot != null && robotHp.Alive)
            {
                Vector3 to = CorePos(enemy) - robot.transform.position; to.y = 0f;
                bool atCore = to.magnitude < 5f;
                if (!atCore) robot.transform.position += to.normalized * 3.5f * Time.deltaTime;
                if (anim != null) anim.SetFloat(SpeedId, atCore ? 0f : 3.5f);
                if (to.sqrMagnitude > 0.01f) robot.transform.rotation = Quaternion.Slerp(robot.transform.rotation, Quaternion.LookRotation(to.normalized), 3f * Time.deltaTime);
                if (Time.time >= nextStomp)
                {
                    nextStomp = Time.time + (atCore ? 1f : 0.45f);
                    FighterManager.I.KillInRadius(enemy, robot.transform.position, 3.5f);
                    if (atCore)
                    {
                        if (anim != null) anim.SetTrigger(PunchId);
                        Vector3 hit = Vector3.Lerp(robot.transform.position, CorePos(enemy), 0.7f) + Vector3.up * 2.5f;
                        Fx.I.Explosion(hit, 0.8f);
                        Sfx.Stomp();
                        Core(enemy)?.TakeDamage(50f * TeamPower.Of(team), by);
                    }
                    else
                    {
                        Fx.I.Dust(robot.transform.position, 2.5f);
                        Sfx.Stomp();
                    }
                }
                yield return null;
            }
            if (robot != null) { Fx.I.Explosion(robot.transform.position + Vector3.up * 3f, 2f); Destroy(robot); }
        }

        /// <summary>
        /// Thảm họa: 0 = động đất (đất nứt, bụi tung, rung mạnh), 1 = lốc xoáy (cột gió quét ngang sân),
        /// 2 = núi lửa (mưa đá lửa trúng từng người). Lính chết dần ngay chỗ thảm họa đi qua; hết 4,5 giây
        /// mà chưa đủ số thì phần còn lại chết ngẫu nhiên, để tỉ lệ chết mỗi phe vẫn đúng như cài đặt.
        /// </summary>
        public void PlayDisaster(int kind, int blueKill, int redKill) => StartCoroutine(Disaster(kind, new[] { blueKill, redKill }));

        IEnumerator Disaster(int kind, int[] quota)
        {
            Sfx.Siren();
            const float duration = 4.5f;
            if (kind == 1)
            {
                // Lốc xoáy: đi từ mép này qua giữa sân sang mép kia, cuốn lính trong bán kính 5m
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector3 a = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Arena.HalfSize * 0.95f;
                Vector3 b = -a + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
                CameraRig.I?.Shake(0.5f, duration);
                var rig = MakeTornado();
                Track(rig.root);

                Vector2 coreUv = Vector2.zero, midUv = Vector2.zero, ribbonUv = Vector2.zero;
                float nextKill = 0f;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    float dt = Time.deltaTime;
                    Vector3 p = Vector3.Lerp(a, b, t / duration) + new Vector3(Mathf.Sin(t * 3f), 0f, Mathf.Cos(t * 2.3f)) * 2f;
                    float grow = Mathf.Clamp01(t / 0.55f) * Mathf.Clamp01((duration - t) / 0.55f); // mọc lên rồi tan dần
                    float horiz = Mathf.Lerp(0.25f, 1f, grow);

                    if (rig.root != null)
                    {
                        rig.root.transform.position = p;
                        rig.funnelBody.localScale = new Vector3(horiz, Mathf.Max(0.05f, grow), horiz);
                        rig.groundSuction.localScale = new Vector3(horiz, 1f, horiz);

                        // Xoay các lớp phễu + dải gió 3D + vành mây bão + vòng hút mặt đất
                        rig.coreLayer.Rotate(0f, 380f * dt, 0f);
                        rig.midLayer.Rotate(0f, 240f * dt, 0f);
                        rig.ribbonLayer.Rotate(0f, 520f * dt, 0f);
                        rig.outerLayer.Rotate(0f, -190f * dt, 0f);
                        rig.cloudCrown.Rotate(0f, 85f * dt, 0f);
                        rig.suctionVortex.Rotate(0f, -380f * dt, 0f);
                        rig.killRing.Rotate(0f, 120f * dt, 0f);

                        // Cuộn vân gió ngược lên trên & sang ngang tạo cảm giác cuồng phong xé gió rõ rệt
                        coreUv.x = (coreUv.x + 0.55f * dt) % 1f;
                        coreUv.y = (coreUv.y - 0.85f * dt) % 1f;
                        midUv.x = (midUv.x + 1.45f * dt) % 1f;
                        midUv.y = (midUv.y - 1.65f * dt) % 1f;
                        ribbonUv.y = (ribbonUv.y - 2.35f * dt) % 1f;
                        matTornadoCore.mainTextureOffset = coreUv;
                        matTornadoMid.mainTextureOffset = midUv;
                        matTornadoRibbon.mainTextureOffset = ribbonUv;

                        // Cập nhật các mảnh đá/gỗ bị cuốn xoay tròn leo dần lên quanh phễu
                        for (int i = 0; i < rig.debris.Length; i++)
                        {
                            var d = rig.debris[i];
                            d.heightNorm = (d.heightNorm + d.riseSpeed * dt) % 1f;
                            d.angle += d.orbitSpeed * dt;
                            rig.debris[i] = d;
                            float k = Mathf.Lerp(0.06f, 0.88f, d.heightNorm);
                            FunnelProfile(k, TornadoHeight, out Vector3 center, out float fr);
                            float rad = fr + d.radialOffset;
                            d.tr.localPosition = center + new Vector3(Mathf.Cos(d.angle) * rad, 0f, Mathf.Sin(d.angle) * rad);
                            d.tr.Rotate(d.tumble * dt);
                        }
                    }

                    Fx.I.Tornado(p, Time.time);
                    if (Time.time >= nextKill) { nextKill = Time.time + 0.12f; KillNear(p, TornadoKillRadius, quota); }
                    yield return null;
                }
                if (rig.root != null) Destroy(rig.root);
            }
            else if (kind == 0)
            {
                // Động đất: rung mạnh, 6 vết nứt địa chấn phát sáng xé toạc mặt đất + cột bụi & đá văng
                CameraRig.I?.Shake(1.6f, duration);
                float baseAngle = Random.Range(0f, Mathf.PI * 2f);
                for (int f = 0; f < 6; f++)
                {
                    float ang = baseAngle + f * (Mathf.PI * 2f / 6f) + Random.Range(-0.28f, 0.28f);
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 origin = dir * Random.Range(-4f, 4f) + new Vector3(-dir.z, 0f, dir.x) * Random.Range(-6f, 6f);
                    float len = Random.Range(11f, 18f);
                    float width = Random.Range(1.8f, 2.7f);
                    StartCoroutine(QuakeFissure(Arena.Clamp(origin), ang, len, width, duration + 2.0f, delay: (f / 2) * 0.45f));
                }

                for (int wave = 0; wave < 6; wave++)
                {
                    for (int d = 0; d < 5; d++)
                    {
                        var gp = new Vector3(Random.Range(-Arena.HalfSize * 0.88f, Arena.HalfSize * 0.88f), 0f, Random.Range(-Arena.HalfSize * 0.88f, Arena.HalfSize * 0.88f));
                        Fx.I.QuakeBurst(gp, Random.Range(0.95f, 1.45f));
                        if (d < 2) StartCoroutine(BouncingRock(gp));
                    }
                    for (int t = 0; t < 2; t++)
                    {
                        int n = Mathf.CeilToInt(quota[t] / (float)(6 - wave));
                        foreach (var f in FighterManager.I.RandomAlive((Team)t, n))
                        {
                            Fx.I.QuakeBurst(f.transform.position, 0.8f);
                            f.Die();
                            quota[t]--;
                        }
                    }
                    yield return new WaitForSeconds(duration / 6f);
                }
            }
            else
            {
                // Núi lửa: mỗi viên đá lửa có vòng cảnh báo, rơi rực cháy và để lại vũng dung nham sủi bọt
                CameraRig.I?.Shake(0.8f, 0.6f);
                var victims = new List<Fighter>();
                victims.AddRange(FighterManager.I.RandomAlive(Team.Blue, quota[0]));
                victims.AddRange(FighterManager.I.RandomAlive(Team.Red, quota[1]));
                for (int i = 0; i < victims.Count; i++) { int j = Random.Range(i, victims.Count); (victims[i], victims[j]) = (victims[j], victims[i]); }
                quota[0] = quota[1] = 0;
                float gap = duration / Mathf.Max(1, victims.Count);
                foreach (var v in victims)
                {
                    if (v != null && v.IsAlive) StartCoroutine(LavaRock(v));
                    yield return new WaitForSeconds(gap);
                }
                yield return new WaitForSeconds(1f);
            }
            // Còn thiếu thì cho chết ngẫu nhiên để đúng tỉ lệ
            for (int t = 0; t < 2; t++)
                foreach (var f in FighterManager.I.RandomAlive((Team)t, Mathf.Max(0, quota[t]))) { Fx.I.Dust(f.transform.position, 1.5f); f.Die(); }
        }

        /// <summary>Vết nứt địa chấn xé toạc mặt sân với rãnh tối và viền sáng cam-hổ phách, tự mờ sạch sau động đất.</summary>
        IEnumerator QuakeFissure(Vector3 origin, float angleRad, float length, float width, float totalLifetime, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            EnsureVfxResources();

            var root = Track(new GameObject("QuakeFissure"));
            root.transform.position = new Vector3(origin.x, 0f, origin.z);
            Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
            root.transform.rotation = Quaternion.LookRotation(dir);

            var chasm = MakeVfxMesh("Chasm", root.transform, fissureMesh, matFissureDark);
            chasm.transform.localPosition = new Vector3(0f, 0.058f, 0f);
            var chasmR = chasm.GetComponent<Renderer>();

            var glow = MakeVfxMesh("SeismicGlow", root.transform, fissureMesh, matFissureGlow);
            glow.transform.localPosition = new Vector3(0f, 0.066f, 0f);
            var glowR = glow.GetComponent<Renderer>();

            const float ripTime = 0.38f;
            float elapsed = 0f;
            float nextDust = 0f;
            while (elapsed < totalLifetime && root != null)
            {
                elapsed += Time.deltaTime;
                float rip = Mathf.Clamp01(elapsed / ripTime);
                float curLen = Mathf.Lerp(0.4f, length, 1f - Mathf.Pow(1f - rip, 2.2f));
                chasm.transform.localScale = new Vector3(width, 1f, curLen);
                glow.transform.localScale = new Vector3(width * 1.12f, 1f, curLen);

                if (rip < 1f && Time.time >= nextDust)
                {
                    nextDust = Time.time + 0.09f;
                    Vector3 tip = root.transform.position + root.transform.forward * curLen;
                    Fx.I.QuakeBurst(tip, 0.85f);
                }

                float norm = Mathf.Clamp01(elapsed / totalLifetime);
                float alpha = norm < 0.72f ? 1f : Mathf.Lerp(1f, 0f, (norm - 0.72f) / 0.28f);
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 10f + angleRad * 3f);

                SetBlockColor(chasmR, new Color(1f, 1f, 1f, 0.94f * alpha));
                SetBlockColor(glowR, ToLinearColor(new Color(1f, 0.52f, 0.08f, alpha), 2.15f * pulse));
                yield return null;
            }
            if (root != null) Destroy(root);
        }

        /// <summary>Tảng đá bật nảy tung lên mặt sân trong Động Đất.</summary>
        IEnumerator BouncingRock(Vector3 origin)
        {
            EnsureVfxResources();
            var go = Track(new GameObject("QuakeRock"));
            go.AddComponent<MeshFilter>().sharedMesh = debrisMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = matDebrisRock;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float scale = Random.Range(0.45f, 0.85f);
            go.transform.localScale = Vector3.one * scale;
            Vector3 pos = origin + Vector3.up * 0.3f;
            Vector3 vel = new Vector3(Random.Range(-4.5f, 4.5f), Random.Range(7.5f, 12.5f), Random.Range(-4.5f, 4.5f));
            Vector3 spin = Random.onUnitSphere * 420f;
            int bounces = 0;

            for (float t = 0f; t < 1.35f && go != null; t += Time.deltaTime)
            {
                float dt = Time.deltaTime;
                vel.y -= 26f * dt;
                pos += vel * dt;
                if (pos.y < 0.25f)
                {
                    pos.y = 0.25f;
                    vel.y = Mathf.Abs(vel.y) * 0.45f;
                    vel.x *= 0.7f; vel.z *= 0.7f;
                    if (bounces == 0) Fx.I.Dust(pos, 0.9f);
                    bounces++;
                }
                go.transform.position = pos;
                go.transform.Rotate(spin * dt);
                float fadeScale = t > 1.0f ? Mathf.Lerp(scale, 0.01f, (t - 1.0f) / 0.35f) : scale;
                go.transform.localScale = Vector3.one * fadeScale;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // ---------------- Cấu trúc Lốc Xoáy sắc nét ----------------

        struct DebrisOrbit
        {
            public Transform tr;
            public float heightNorm, angle, orbitSpeed, riseSpeed, radialOffset;
            public Vector3 tumble;
        }

        struct TornadoRig
        {
            public GameObject root;
            public Transform funnelBody, coreLayer, midLayer, ribbonLayer, outerLayer, cloudCrown;
            public Transform groundSuction, suctionVortex, killRing;
            public DebrisOrbit[] debris;
        }

        /// <summary>
        /// Cột lốc xoáy tương phản cao: lõi bão xanh-xám đậm (#26384C) + lớp gió xoắn sáng (#C8E4F8)
        /// + 4 dải lụa gió xoắn ốc 3D nổi bật + mũ mây bão + vòng hút mặt đất 5m + 9 mảnh vụn bay xoay quanh.
        /// </summary>
        TornadoRig MakeTornado()
        {
            EnsureVfxResources();
            var root = new GameObject("Tornado");

            var body = new GameObject("FunnelBody").transform;
            body.SetParent(root.transform, false);

            var core = MakeVfxMesh("Core", body, funnelMesh, matTornadoCore).transform;
            core.localScale = new Vector3(0.78f, 1f, 0.78f);

            var mid = MakeVfxMesh("Mid", body, funnelMesh, matTornadoMid).transform;
            mid.localScale = new Vector3(1.0f, 1f, 1.0f);

            var ribbons = MakeVfxMesh("Ribbons", body, ribbonHelixMesh, matTornadoRibbon).transform;
            ribbons.localScale = Vector3.one;

            var outer = MakeVfxMesh("OuterGust", body, funnelMesh, matTornadoOuter).transform;
            outer.localScale = new Vector3(1.13f, 0.98f, 1.13f);

            // Vành mây bão bán trong suốt xoáy mềm trên đỉnh phễu (không dùng khối đá đặc che tầm nhìn)
            var crown = new GameObject("CloudCrown").transform;
            crown.SetParent(body, false);
            crown.localPosition = new Vector3(0f, 14.0f, 0f);

            var cloudLow = MakeVfxMesh("StormCloudLow", crown, discMesh, matTornadoCloudDark);
            cloudLow.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            cloudLow.transform.localScale = new Vector3(7.6f, 1f, 7.6f);

            var cloudMid = MakeVfxMesh("StormCloudMid", crown, discMesh, matTornadoCloudDark);
            cloudMid.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            cloudMid.transform.localRotation = Quaternion.Euler(0f, 65f, 0f);
            cloudMid.transform.localScale = new Vector3(9.0f, 1f, 9.0f);

            var cloudRim = MakeVfxMesh("StormCloudRim", crown, discMesh, matTornadoCloudRim);
            cloudRim.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            cloudRim.transform.localRotation = Quaternion.Euler(0f, 145f, 0f);
            cloudRim.transform.localScale = new Vector3(8.2f, 1f, 8.2f);

            // 9 mảnh đá và gỗ văng xoắn ốc quanh cột gió
            const int debrisCount = 9;
            var debris = new DebrisOrbit[debrisCount];
            for (int i = 0; i < debrisCount; i++)
            {
                bool wood = i % 3 == 0;
                var chunk = MakeVfxMesh($"Debris_{i}", body, debrisMesh, wood ? matDebrisWood : matDebrisRock);
                chunk.transform.localScale = wood
                    ? new Vector3(0.32f, 0.22f, 1.15f)
                    : new Vector3(0.55f, 0.45f, 0.62f) * Random.Range(0.75f, 1.25f);
                debris[i] = new DebrisOrbit
                {
                    tr = chunk.transform,
                    heightNorm = (float)i / debrisCount,
                    angle = i * 1.4f,
                    orbitSpeed = Random.Range(4.8f, 7.6f),
                    riseSpeed = Random.Range(0.16f, 0.26f),
                    radialOffset = Random.Range(0.65f, 1.9f),
                    tumble = new Vector3(Random.Range(180f, 360f), Random.Range(180f, 360f), Random.Range(120f, 280f)),
                };
            }

            // Vòng hút mặt đất khớp đúng bán kính KillNear = 5m
            var ground = new GameObject("GroundSuction").transform;
            ground.SetParent(root.transform, false);

            var shadow = MakeVfxMesh("StormShadow", ground, discMesh, matTornadoShadow);
            shadow.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            shadow.transform.localScale = Vector3.one * (TornadoKillRadius * 0.94f);

            var vortex = MakeVfxMesh("SuctionVortex", ground, suctionRingMesh, matTornadoSuction).transform;
            vortex.localPosition = new Vector3(0f, 0.08f, 0f);
            vortex.localScale = Vector3.one * TornadoKillRadius;

            var killRing = MakeVfxMesh("KillRing", ground, ringMesh, matTornadoKillRing).transform;
            killRing.localPosition = new Vector3(0f, 0.09f, 0f);
            killRing.localScale = Vector3.one * TornadoKillRadius;

            return new TornadoRig
            {
                root = root,
                funnelBody = body,
                coreLayer = core,
                midLayer = mid,
                ribbonLayer = ribbons,
                outerLayer = outer,
                cloudCrown = crown,
                groundSuction = ground,
                suctionVortex = vortex,
                killRing = killRing,
                debris = debris,
            };
        }

        void KillNear(Vector3 center, float radius, int[] quota)
        {
            for (int t = 0; t < 2; t++)
            {
                if (quota[t] <= 0) continue;
                var list = FighterManager.I.List((Team)t);
                for (int i = list.Count - 1; i >= 0 && quota[t] > 0; i--)
                {
                    if (i >= list.Count) continue;
                    var f = list[i];
                    Vector3 d = f.transform.position - center; d.y = 0f;
                    if (d.sqrMagnitude > radius * radius) continue;
                    Fx.I.VortexPull(f.transform.position);
                    f.Die();
                    quota[t]--;
                }
            }
        }

        IEnumerator LavaRock(Fighter victim)
        {
            Vector3 at = victim.transform.position;
            yield return FallingRock(at, null, 0f, Random.Range(1.6f, 2.3f), fiery: true, volcanoSplash: true);
            if (victim != null && victim.IsAlive) victim.Die();
        }

        // ---------------- Tiện ích ----------------

        void Announce(EffectDef def, Team team, Viewer by, int amount, Transform target, Vector3 position)
        {
            GameEvents.RaiseEffectStarted(new EffectNotice
            {
                Team = team, EffectId = def.Id, DisplayName = def.DisplayName, Tier = def.Tier,
                Amount = amount, By = by, Position = position, Target = target,
            });
            if (def.Tier >= 3) Sfx.Gift();
            if (def.Tier >= 1)
            {
                Vector3 at = target != null ? target.position + Vector3.up * (def.Tier >= 5 ? 6f : 3.2f) : position + Vector3.up * 3f;
                Fx.I.IconPop(GameAssets.I.EffectIcon(def.Id), at, def.Tier >= 4 ? 3.2f : 1.6f);
            }
            if (def.Tier >= 3 && by != null)
                GameEvents.RaiseNotice($"{by.Nickname}: {def.DisplayName} cho phe {team.DisplayName()}!", NoticeKind.Gift);
        }

        void Bolt(Vector3 at)
        {
            for (int b = 0; b < 2; b++)
            {
                var go = Track(new GameObject("Bolt"));
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 10;
                lr.widthMultiplier = b == 0 ? 0.5f : 0.18f;
                if (GameAssets.I.boltMaterial != null) lr.sharedMaterial = GameAssets.I.boltMaterial;
                for (int i = 0; i < 10; i++)
                {
                    float t = i / 9f;
                    Vector3 p = Vector3.Lerp(at + Vector3.up * 30f, at, t);
                    if (i > 0 && i < 9) p += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                    lr.SetPosition(i, p);
                }
                Destroy(go, 0.2f);
            }
        }

        /// <summary>Thêm vòng màu phe dưới chân để biết xe tăng/robot của phe nào.</summary>
        static void AddTeamRing(GameObject root, Team team, float radius)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            var r = ring.GetComponent<Renderer>();
            r.sharedMaterial = TeamMaterials.Ring(team);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        GameObject MakeTank(Team team)
        {
            var a = GameAssets.I;
            if (a.tankPrefab == null) return MakeBox(team, new Vector3(2f, 1.2f, 3f), $"Tank_{team}");
            var root = new GameObject($"Tank_{team}");
            var model = Instantiate(a.tankPrefab, root.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, a.tankYawOffset, 0f) * model.transform.localRotation; // model gốc quay ngược
            TeamMaterials.DressVehicle(model, team); // thân màu phe, kim loại màu phe đậm, xích đen
            var b = Bounds(model);
            float length = Mathf.Max(b.size.x, b.size.z);
            if (length > 0.01f) model.transform.localScale *= a.tankLength / length;
            b = Bounds(model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z); // tâm xe về gốc, bánh chạm đất
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            AddHollowTeamRing(root, team, 2.35f);
            AttachFlag(root, team, new Vector3(0.4f, 1.6f, -1.2f), 2.4f);
            return Track(root);
        }

        /// <summary>Vòng rỗng màu phe sắc nét đặt sát mặt đất quanh xe tăng để nổi bật từ góc nhìn 55°.</summary>
        static void AddHollowTeamRing(GameObject root, Team team, float radius)
        {
            EnsureVfxResources();
            var ring = MakeVfxMesh("TeamGroundRing", root.transform, ringMesh, team == Team.Blue ? matTeamRingBlue : matTeamRingRed);
            ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            ring.transform.localScale = new Vector3(radius, 1f, radius);
        }

        GameObject MakeRobot(Team team, out Animator anim)
        {
            var a = GameAssets.I;
            anim = null;
            if (a.robotPrefab == null) return MakeBox(team, new Vector3(3f, 7f, 3f), $"Robot_{team}");
            var root = new GameObject($"Robot_{team}");
            var model = Instantiate(a.robotPrefab, root.transform, false);
            float h = FighterManager.MeasureHeight(model);
            if (h > 0.01f) model.transform.localScale *= a.robotHeight / h;
            TeamMaterials.MakeMatte(model, a.RobotTexture(team)); // họa tiết đã tô lại theo phe: sơn màu phe, chi tiết vàng, kim loại xám
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = a.robotController;
            anim.applyRootMotion = false;
            AttachFlag(root, team, new Vector3(0f, a.robotHeight * 0.75f, -1.4f), 3.5f);
            return Track(root);
        }

        /// <summary>Cắm cờ màu phe (Kenney Castle Kit) lên xe tăng/robot.</summary>
        static void AttachFlag(GameObject root, Team team, Vector3 localPos, float height)
        {
            var prefab = GameAssets.I.flagPrefab;
            if (prefab == null) return;
            var f = Instantiate(prefab, root.transform, false);
            f.name = "TeamFlag";
            float h = FighterManager.MeasureHeight(f);
            if (h > 0.01f) f.transform.localScale *= height / h;
            f.transform.localPosition = localPos;
            foreach (var c in f.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in f.GetComponentsInChildren<Renderer>())
            {
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = TeamMaterials.Flag(team);
                r.sharedMaterials = arr;
            }
        }

        GameObject MakeBox(Team team, Vector3 size, string name)
        {
            var go = new GameObject(name);
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(hull.GetComponent<Collider>());
            hull.transform.SetParent(go.transform, false);
            hull.transform.localScale = size;
            hull.transform.localPosition = Vector3.up * size.y * 0.5f;
            SetMat(go, GameAssets.I.TeamMaterial(team));
            return Track(go);
        }

        static Bounds Bounds(GameObject go)
        {
            bool has = false; Bounds b = default;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            return b;
        }

        static void SetMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        }

        static GameObject MakeVfxMesh(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        void SetBlockColor(Renderer r, Color linearColor)
        {
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, linearColor);
            r.SetPropertyBlock(mpb);
        }

        static Color ToLinearColor(Color srgb, float hdrMultiplier = 1f)
        {
            Color lin = srgb.linear;
            return new Color(lin.r * hdrMultiplier, lin.g * hdrMultiplier, lin.b * hdrMultiplier, srgb.a);
        }

        // ---------------- Tài nguyên VFX tạo một lần & dùng chung ----------------

        static bool vfxReady;
        static Mesh ringMesh, discMesh, suctionRingMesh, plasmaConeMesh, funnelMesh, ribbonHelixMesh, fissureMesh, debrisMesh;
        static Texture2D solidWhiteTex, softDiscTex, plasmaShellTex, meteorTrailTex;
        static Texture2D craterScorchTex, craterMagmaTex, magmaSplashTex, magmaSplashGlowTex;
        static Texture2D windCoreTex, windStreakTex, ribbonTex, suctionRingTex, stormCloudTex, fissureDarkTex, fissureGlowTex;
        static Material meteorGlow, matSolidAdditive, matSolidAlpha, matPlasmaOuter, matPlasmaCore, matMeteorTrail;
        static Material matCraterScorch, matCraterMagma, matMagmaSplashBase, matMagmaSplashGlow;
        static Material matTornadoCore, matTornadoMid, matTornadoRibbon, matTornadoOuter, matTornadoSuction, matTornadoKillRing, matTornadoShadow;
        static Material matTornadoCloudDark, matTornadoCloudRim, matDebrisRock, matDebrisWood, matFissureDark, matFissureGlow;
        static Material matTeamRingBlue, matTeamRingRed;
        static Gradient meteorTrailGradient;

        static void EnsureVfxResources()
        {
            if (vfxReady && ringMesh != null && matTornadoCore != null) return;
            vfxReady = true;

            // Meshes
            ringMesh = BuildFlatRing(0.88f, 1.0f, 48, "VfxRing");
            discMesh = BuildFlatDisc(1.0f, 40, "VfxDisc");
            suctionRingMesh = BuildPlanarAnnulus(0.18f, 1.0f, 48, "VfxSuctionAnnulus");
            plasmaConeMesh = BuildPlasmaCone(20, 14);
            funnelMesh = BuildFunnel(TornadoHeight, 36, 22, 1.75f);
            ribbonHelixMesh = BuildRibbonHelices(TornadoHeight, 4, 44);
            fissureMesh = BuildFissureStrip(18);
            debrisMesh = BuildDebrisChunk();

            // Textures
            solidWhiteTex = BuildSolidWhiteTexture();
            softDiscTex = BuildSoftDiscTexture();
            plasmaShellTex = BuildPlasmaShellTexture();
            meteorTrailTex = BuildMeteorTrailTexture();
            BuildCraterTextures(out craterScorchTex, out craterMagmaTex);
            BuildMagmaSplashTextures(out magmaSplashTex, out magmaSplashGlowTex);
            windCoreTex = BuildWindCoreTexture();
            windStreakTex = BuildWindStreakTexture();
            ribbonTex = BuildRibbonTexture();
            suctionRingTex = BuildSuctionRingTexture();
            stormCloudTex = BuildStormCloudTexture();
            BuildFissureTextures(out fissureDarkTex, out fissureGlowTex);

            // Materials (sao chép từ fxAdditiveMaterial / fxSmokeMaterial để đảm bảo URP shader luôn có mặt trong bản build)
            matSolidAdditive = CloneParticleMat("Vfx_SolidAdd", additive: true, solidWhiteTex, Color.white);
            matSolidAlpha = CloneParticleMat("Vfx_SolidAlpha", additive: false, solidWhiteTex, Color.white);
            matPlasmaOuter = CloneParticleMat("Vfx_PlasmaOuter", additive: true, plasmaShellTex, new Color(1.0f, 0.30f, 0.04f, 0.46f));
            matPlasmaCore = CloneParticleMat("Vfx_PlasmaCore", additive: true, plasmaShellTex, new Color(1.0f, 0.65f, 0.16f, 0.42f));
            matMeteorTrail = CloneParticleMat("Vfx_MeteorTrail", additive: true, meteorTrailTex, new Color(1.35f, 1.35f, 1.35f, 0.92f));

            matCraterScorch = CloneParticleMat("Vfx_CraterScorch", additive: false, craterScorchTex, Color.white);
            matCraterMagma = CloneParticleMat("Vfx_CraterMagma", additive: true, craterMagmaTex, Color.white);
            matMagmaSplashBase = CloneParticleMat("Vfx_MagmaSplashBase", additive: false, magmaSplashTex, Color.white);
            matMagmaSplashGlow = CloneParticleMat("Vfx_MagmaSplashGlow", additive: true, magmaSplashGlowTex, Color.white);

            // Lốc xoáy: phễu hai mặt tương phản cao xanh-xám bão (#26384C) + dải gió sáng (#C8E4F8) + ruy-băng xoắn + mây bão mềm
            matTornadoCore = CloneParticleMat("Vfx_TornadoCore", additive: false, windCoreTex, new Color(0.15f, 0.22f, 0.30f, 0.88f));
            matTornadoMid = CloneParticleMat("Vfx_TornadoMid", additive: false, windStreakTex, new Color(0.78f, 0.89f, 0.97f, 0.78f));
            matTornadoRibbon = CloneParticleMat("Vfx_TornadoRibbon", additive: true, ribbonTex, new Color(1.02f, 1.34f, 1.55f, 0.88f));
            matTornadoOuter = CloneParticleMat("Vfx_TornadoOuter", additive: false, windStreakTex, new Color(0.84f, 0.94f, 1f, 0.34f));
            matTornadoSuction = CloneParticleMat("Vfx_TornadoSuction", additive: false, suctionRingTex, new Color(0.62f, 0.91f, 1.0f, 0.68f));
            matTornadoKillRing = CloneParticleMat("Vfx_TornadoKillRing", additive: false, solidWhiteTex, new Color(0.32f, 0.84f, 1.0f, 0.82f));
            matTornadoShadow = CloneParticleMat("Vfx_TornadoShadow", additive: false, softDiscTex, new Color(0.08f, 0.14f, 0.22f, 0.38f));

            matTornadoCloudDark = CloneParticleMat("Vfx_TornadoCloudDark", additive: false, stormCloudTex, new Color(0.17f, 0.25f, 0.34f, 0.46f));
            matTornadoCloudRim = CloneParticleMat("Vfx_TornadoCloudRim", additive: false, stormCloudTex, new Color(0.60f, 0.75f, 0.86f, 0.34f));
            matDebrisRock = MakeLitMat("Vfx_DebrisRock", new Color(0.32f, 0.36f, 0.40f), 0.1f);
            matDebrisWood = MakeLitMat("Vfx_DebrisWood", new Color(0.56f, 0.39f, 0.24f), 0.12f);

            matFissureDark = CloneParticleMat("Vfx_FissureDark", additive: false, fissureDarkTex, Color.white);
            matFissureGlow = CloneParticleMat("Vfx_FissureGlow", additive: true, fissureGlowTex, Color.white);

            matTeamRingBlue = CloneParticleMat("Vfx_TeamRingBlue", additive: true, solidWhiteTex, new Color(0.26f, 0.68f, 1.35f, 0.85f));
            matTeamRingRed = CloneParticleMat("Vfx_TeamRingRed", additive: true, solidWhiteTex, new Color(1.35f, 0.28f, 0.24f, 0.85f));

            meteorTrailGradient = new Gradient();
            meteorTrailGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.97f, 0.82f), 0f),
                    new GradientColorKey(new Color(1f, 0.62f, 0.12f), 0.26f),
                    new GradientColorKey(new Color(0.95f, 0.20f, 0.04f), 0.65f),
                    new GradientColorKey(new Color(0.45f, 0.06f, 0.02f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.92f, 0.3f),
                    new GradientAlphaKey(0.52f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
        }

        static Material CloneParticleMat(string name, bool additive, Texture2D tex, Color baseColor)
        {
            var src = additive ? GameAssets.I.fxAdditiveMaterial : GameAssets.I.fxSmokeMaterial;
            Material m;
            if (src != null)
            {
                m = new Material(src) { name = name };
            }
            else
            {
                var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                m = new Material(sh) { name = name };
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetOverrideTag("RenderType", "Transparent");
            }
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Cull", 0f);
            return m;
        }

        static Material MakeLitMat(string name, Color color, float smoothness)
        {
            var src = GameAssets.I.groundMaterial ?? GameAssets.I.meteorMaterial;
            var sh = src != null ? src.shader : Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh) { name = name };
            m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        static Material MeteorGlow()
        {
            EnsureVfxResources();
            if (meteorGlow != null) return meteorGlow;
            var sh = GameAssets.I.meteorMaterial != null ? GameAssets.I.meteorMaterial.shader : Shader.Find("Universal Render Pipeline/Lit");
            meteorGlow = new Material(sh) { name = "MeteorGlow" };
            // Đá núi lửa obsidian tối nổi khối 3D rõ cạnh, phát sáng cam-đỏ vừa phải (không phụ thuộc UV palette FBX gây chói vàng bệt)
            meteorGlow.SetColor("_BaseColor", new Color(0.18f, 0.07f, 0.05f));
            if (meteorGlow.HasProperty("_Metallic")) meteorGlow.SetFloat("_Metallic", 0f);
            if (meteorGlow.HasProperty("_Smoothness")) meteorGlow.SetFloat("_Smoothness", 0.16f);
            meteorGlow.EnableKeyword("_EMISSION");
            meteorGlow.SetColor("_EmissionColor", new Color(1.0f, 0.30f, 0.04f) * 1.15f);
            return meteorGlow;
        }

        // ---------------- Tạo Mesh thủ tục ----------------

        static void FunnelProfile(float k, float height, out Vector3 center, out float radius)
        {
            radius = Mathf.Lerp(0.72f, 6.8f, Mathf.Pow(k, 1.55f) * 0.75f + k * 0.25f) * (1f + 0.05f * Mathf.Sin(k * 10f));
            center = new Vector3(Mathf.Sin(k * 3.8f) * 0.75f * k, k * height, Mathf.Cos(k * 2.9f) * 0.55f * k);
        }

        /// <summary>Phễu lốc xoáy 2 mặt (mặt ngoài + mặt trong đảo chiều) để nhìn từ 55° trên xuống vẫn thấy rõ lõi phễu.</summary>
        static Mesh BuildFunnel(float height, int seg, int rings, float twist)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float k = (float)j / rings;
                FunnelProfile(k, height, out Vector3 center, out float r);
                float off = k * twist * Mathf.PI * 2f;
                for (int i = 0; i <= seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2f + off;
                    v.Add(center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                    uv.Add(new Vector2((float)i / seg * 3f, k));
                }
            }
            int w = seg + 1;
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * w + i;
                    // Mặt ngoài
                    t.Add(a); t.Add(a + w); t.Add(a + 1);
                    t.Add(a + 1); t.Add(a + w); t.Add(a + w + 1);
                    // Mặt trong (đảo winding để không bị back-face culling khi nhìn từ 55° xuống miệng phễu)
                    t.Add(a); t.Add(a + 1); t.Add(a + w);
                    t.Add(a + 1); t.Add(a + w + 1); t.Add(a + w);
                }
            var m = new Mesh { name = "TornadoFunnel" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>4 dải gió xoắn ốc 3D hai mặt quấn quanh bên ngoài phễu để ngay cả ảnh tĩnh cũng thấy rõ chuyển động xoáy.</summary>
        static Mesh BuildRibbonHelices(float height, int ribbonCount, int steps)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int rIdx = 0; rIdx < ribbonCount; rIdx++)
            {
                float phase = rIdx * Mathf.PI * 2f / ribbonCount;
                float kMin = 0.03f + (rIdx % 2) * 0.04f;
                float kMax = 0.94f - (rIdx % 2) * 0.04f;
                int baseVert = v.Count;

                for (int s = 0; s <= steps; s++)
                {
                    float f = (float)s / steps;
                    float k = Mathf.Lerp(kMin, kMax, f);
                    FunnelProfile(k, height, out Vector3 center, out float fr);
                    float rr = fr + 0.28f + 0.45f * k;
                    float ang = phase + k * 2.35f * Mathf.PI * 2f;
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 p = center + radial * rr;

                    // Dải nghiêng nhẹ ra ngoài để góc nhìn 55° từ trên xuống luôn bắt trọn bề rộng dải sáng
                    float halfW = Mathf.Lerp(0.16f, 0.42f, k) * Mathf.Sin(f * Mathf.PI);
                    Vector3 bandDir = (Vector3.up * 0.85f + radial * 0.52f).normalized;
                    v.Add(p - bandDir * halfW);
                    v.Add(p + bandDir * halfW);
                    uv.Add(new Vector2(0f, f * 2.4f));
                    uv.Add(new Vector2(1f, f * 2.4f));
                }

                for (int s = 0; s < steps; s++)
                {
                    int a = baseVert + s * 2;
                    t.Add(a); t.Add(a + 2); t.Add(a + 1);
                    t.Add(a + 1); t.Add(a + 2); t.Add(a + 3);
                    t.Add(a); t.Add(a + 1); t.Add(a + 2);
                    t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
                }
            }
            var m = new Mesh { name = "TornadoRibbons" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Vỏ plasma lửa thuôn gọn dọc trục +Z (đầu ở +0.34 sau mũi đá, đuôi lửa vuốt nhọn về -3.2).</summary>
        static Mesh BuildPlasmaCone(int seg, int rings)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float k = (float)j / rings;
                float z, r;
                if (k < 0.14f)
                {
                    float n = k / 0.14f;
                    z = Mathf.Lerp(0.36f, 0.12f, n);
                    r = Mathf.Lerp(0.05f, 0.62f, Mathf.Sin(n * Mathf.PI * 0.5f));
                }
                else
                {
                    float n = (k - 0.14f) / 0.86f;
                    z = Mathf.Lerp(0.12f, -3.2f, n);
                    r = Mathf.Lerp(0.62f, 0.04f, Mathf.Pow(n, 0.85f)) * (1f + 0.04f * Mathf.Sin(n * 18f));
                }
                for (int i = 0; i <= seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z));
                    uv.Add(new Vector2((float)i / seg * 2f, k));
                }
            }
            int w = seg + 1;
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * w + i;
                    t.Add(a); t.Add(a + 1); t.Add(a + w);
                    t.Add(a + 1); t.Add(a + w + 1); t.Add(a + w);
                    t.Add(a); t.Add(a + w); t.Add(a + 1);
                    t.Add(a + 1); t.Add(a + w); t.Add(a + w + 1);
                }
            var m = new Mesh { name = "MeteorPlasmaCone" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Mesh BuildFlatRing(float innerR, float outerR, int seg, string name)
        {
            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float frac = (float)i / seg;
                float a = frac * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * innerR, 0f, s * innerR);
                verts[i * 2 + 1] = new Vector3(c * outerR, 0f, s * outerR);
                uvs[i * 2] = new Vector2(frac, 0f);
                uvs[i * 2 + 1] = new Vector2(frac, 1f);
                if (i < seg)
                {
                    int k = i * 6, b = i * 2;
                    tris[k] = b; tris[k + 1] = b + 2; tris[k + 2] = b + 1;
                    tris[k + 3] = b + 2; tris[k + 4] = b + 3; tris[k + 5] = b + 1;
                }
            }
            var m = new Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Mesh BuildFlatDisc(float radius, int seg, string name)
        {
            var verts = new Vector3[seg + 1];
            var uvs = new Vector2[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i + 1] = new Vector3(c * radius, 0f, s * radius);
                uvs[i + 1] = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = (i + 1) % seg + 1;
                tris[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Mesh BuildPlanarAnnulus(float innerR, float outerR, int seg, string name)
        {
            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * innerR, 0f, s * innerR);
                verts[i * 2 + 1] = new Vector3(c * outerR, 0f, s * outerR);
                uvs[i * 2] = new Vector2(c * innerR * 0.5f + 0.5f, s * innerR * 0.5f + 0.5f);
                uvs[i * 2 + 1] = new Vector2(c * outerR * 0.5f + 0.5f, s * outerR * 0.5f + 0.5f);
                if (i < seg)
                {
                    int k = i * 6, b = i * 2;
                    tris[k] = b; tris[k + 1] = b + 2; tris[k + 2] = b + 1;
                    tris[k + 3] = b + 2; tris[k + 4] = b + 3; tris[k + 5] = b + 1;
                }
            }
            var m = new Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Dải vết nứt răng cưa chạy dọc từ z = 0 đến z = 1 để scale Z xé toạc mặt đất mượt mà.</summary>
        static Mesh BuildFissureStrip(int seg)
        {
            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float taper = Mathf.Sin(t * Mathf.PI);
                float zig = taper * (0.28f * Mathf.Sin(t * 17.3f) + 0.14f * Mathf.Cos(t * 31.7f));
                float hw = 0.5f * Mathf.Lerp(0.08f, 1.0f, Mathf.Pow(taper, 0.55f)) * (0.85f + 0.25f * Mathf.Abs(Mathf.Sin(t * 23f)));
                verts[i * 2] = new Vector3(zig - hw, 0f, t);
                verts[i * 2 + 1] = new Vector3(zig + hw, 0f, t);
                uvs[i * 2] = new Vector2(0f, t * 2.5f);
                uvs[i * 2 + 1] = new Vector2(1f, t * 2.5f);
                if (i < seg)
                {
                    int k = i * 6, b = i * 2;
                    tris[k] = b; tris[k + 1] = b + 2; tris[k + 2] = b + 1;
                    tris[k + 3] = b + 1; tris[k + 4] = b + 2; tris[k + 5] = b + 3;
                }
            }
            var m = new Mesh { name = "QuakeFissureStrip", vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Khối đa giác thô (12 đỉnh) làm mảnh đá/gỗ bay quanh lốc xoáy và mây bão low-poly.</summary>
        static Mesh BuildDebrisChunk()
        {
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] raw =
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1),
            };
            for (int i = 0; i < raw.Length; i++)
            {
                float jitter = 0.42f + 0.14f * Mathf.Sin(i * 4.3f + 1.1f);
                raw[i] = raw[i].normalized * jitter;
            }
            int[] tris =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
            };
            // Tách đỉnh theo từng mặt để ánh sáng low-poly sắc cạnh rõ nét
            var verts = new Vector3[tris.Length];
            var idx = new int[tris.Length];
            for (int i = 0; i < tris.Length; i++) { verts[i] = raw[tris[i]]; idx[i] = i; }
            var m = new Mesh { name = "VfxDebrisChunk", vertices = verts, triangles = idx };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // ---------------- Tạo Texture thủ tục ----------------

        static Texture2D BuildSolidWhiteTexture()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D BuildSoftDiscTexture()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S * 0.5f, S * 0.5f)) / (S * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vỏ lửa plasma: đầu xung kích cam-vàng trong suốt, đuôi lửa xé rách mờ dần về cuối.</summary>
        static Texture2D BuildPlasmaShellTexture()
        {
            const int W = 64, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H;
                    float nose = Mathf.SmoothStep(0f, 0.07f, v);
                    float tailFade = Mathf.Pow(Mathf.Clamp01(1f - v), 1.35f);
                    float tongues = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI * 10f + v * 7f) * Mathf.PerlinNoise(u * 8f, v * 4f);
                    float band = Mathf.Lerp(1f, Mathf.Clamp01(tongues * 1.25f), Mathf.SmoothStep(0.12f, 0.65f, v));
                    float a = Mathf.Clamp01(nose * tailFade * band);
                    Color col = Color.Lerp(new Color(1f, 0.98f, 0.85f), new Color(1f, 0.38f, 0.06f), Mathf.SmoothStep(0.08f, 0.72f, v));
                    col.a = a;
                    tex.SetPixel(x, y, col);
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vệt sáng cho TrailRenderer: tâm trắng-nóng sắc nét, mép mềm đều.</summary>
        static Texture2D BuildMeteorTrailTexture()
        {
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / (W - 1);
                    float v = (float)y / (H - 1);
                    float across = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v * 2f - 1f)), 1.55f);
                    float along = Mathf.SmoothStep(0f, 0.06f, u) * Mathf.SmoothStep(1f, 0.15f, u);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(across * along)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Hố thiên thạch: vành cháy than đen + khe dung nham rực sáng tỏa ra từ tâm.</summary>
        static void BuildCraterTextures(out Texture2D scorch, out Texture2D magma)
        {
            const int S = 256;
            scorch = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            magma = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);

                    float rays = 0.5f + 0.5f * Mathf.Sin(ang * 11f + 0.4f) * Mathf.Cos(ang * 7f - 1.1f);
                    float n = Mathf.PerlinNoise(nx * 4.5f + 5f, ny * 4.5f + 5f);
                    float edge = 0.72f + 0.22f * rays + 0.08f * n;
                    float scorchA = Mathf.SmoothStep(edge, edge * 0.48f, r);
                    float shade = Mathf.Lerp(0.05f, 0.15f, Mathf.SmoothStep(0.15f, 0.65f, r) * n);
                    scorch.SetPixel(x, y, new Color(shade, shade * 0.75f, shade * 0.65f, scorchA * 0.94f));

                    // Khe nứt dung nham phát sáng tỏa tia ở tâm hố
                    float crackWave = Mathf.Abs(Mathf.Sin(ang * 6f + n * 2.8f));
                    float crackWave2 = Mathf.Abs(Mathf.Sin(ang * 9f - n * 2.2f + 1.1f));
                    float crackLine = 1f - Mathf.Min(crackWave, crackWave2);
                    float radialCrack = Mathf.SmoothStep(0.76f, 0.97f, crackLine) * Mathf.SmoothStep(0.72f, 0.12f, r);
                    float hotCenter = Mathf.SmoothStep(0.36f, 0.04f, r) * (0.65f + 0.35f * n);
                    float glow = Mathf.Clamp01(Mathf.Max(radialCrack, hotCenter));
                    Color mc = Color.Lerp(new Color(1f, 0.38f, 0.05f), new Color(1f, 0.95f, 0.55f), Mathf.Clamp01(hotCenter + radialCrack * 0.4f));
                    magma.SetPixel(x, y, new Color(mc.r, mc.g, mc.b, glow));
                }
            scorch.Apply();
            magma.Apply();
        }

        /// <summary>Vũng dung nham núi lửa: viền đá bazan tối bao quanh hồ dung nham sủi bọt nóng rực.</summary>
        static void BuildMagmaSplashTextures(out Texture2D baseTex, out Texture2D glowTex)
        {
            const int S = 128;
            baseTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            glowTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);
                    float blob = 0.72f + 0.16f * Mathf.Sin(ang * 5f + 0.7f) + 0.09f * Mathf.Sin(ang * 9f - 1.3f);
                    float n = Mathf.PerlinNoise(nx * 5f + 3f, ny * 5f + 7f);

                    float baseA = Mathf.SmoothStep(blob + 0.06f, blob - 0.14f, r);
                    baseTex.SetPixel(x, y, new Color(0.11f, 0.05f, 0.04f, baseA * 0.92f));

                    float pool = Mathf.SmoothStep(blob - 0.08f, blob * 0.32f, r);
                    float bubbles = Mathf.SmoothStep(0.45f, 0.88f, Mathf.PerlinNoise(nx * 7f + 11f, ny * 7f + 2f));
                    float gA = Mathf.Clamp01(pool * (0.68f + 0.32f * bubbles));
                    Color gc = Color.Lerp(new Color(1f, 0.32f, 0.04f), new Color(1f, 0.92f, 0.4f), bubbles * pool);
                    glowTex.SetPixel(x, y, new Color(gc.r, gc.g, gc.b, gA));
                }
            baseTex.Apply();
            glowTex.Apply();
        }

        /// <summary>Vân lõi lốc xoáy đậm chắc để tạo hình bóng xanh-xám nổi bật trên nền cỏ và đất.</summary>
        static Texture2D BuildWindCoreTexture()
        {
            const int W = 128, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H;
                    float band = 0.5f + 0.5f * Mathf.Sin((u * 4f + v * 2f) * Mathf.PI * 2f);
                    float n = Mathf.PerlinNoise(u * 8f + 1f, v * 5f + 3f);
                    float density = 0.62f + 0.28f * band + 0.15f * n;
                    float fade = Mathf.SmoothStep(0f, 0.08f, v) * Mathf.SmoothStep(1f, 0.9f, v);
                    float lum = 0.72f + 0.28f * band;
                    tex.SetPixel(x, y, new Color(lum, lum, lum, Mathf.Clamp01(density * fade)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vân gió xiên sắc nét tương phản cao cho lớp Mid lốc xoáy (cuộn lên mỗi frame).</summary>
        static Texture2D BuildWindStreakTexture()
        {
            const int W = 256, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H;
                    float s1 = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((u * 5f + v * 2f) * Mathf.PI * 2f), 4.2f);
                    float s2 = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((u * 10f + v * 3f + 0.35f) * Mathf.PI * 2f), 5.5f) * 0.75f;
                    float n = Mathf.PerlinNoise(u * 14f, v * 7f);
                    float streak = Mathf.Clamp01(s1 * 0.95f + s2 + n * 0.18f);
                    float fade = Mathf.SmoothStep(0f, 0.10f, v) * Mathf.SmoothStep(1f, 0.86f, v);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(streak * fade)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vân phát sáng cho các dải gió xoắn ốc 3D quanh lốc xoáy.</summary>
        static Texture2D BuildRibbonTexture()
        {
            const int W = 64, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = TextureWrapMode.Repeat,
            };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / (W - 1), v = (float)y / H;
                    float core = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.35f);
                    float surge = 0.52f + 0.48f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(v * Mathf.PI * 2f), 1.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core * surge)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vòng hút xoáy mặt đất cho Lốc Xoáy: 6 cung gió xoắn xanh băng trong suốt + mắt bão mở rõ lính bên dưới.</summary>
        static Texture2D BuildSuctionRingTexture()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);

                    float spiral = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(ang * 6f + r * 12.5f), 5.5f);
                    float bodyMask = Mathf.SmoothStep(0.24f, 0.42f, r) * Mathf.SmoothStep(0.95f, 0.58f, r);
                    float rimDash = Mathf.SmoothStep(0.87f, 0.92f, r) * Mathf.SmoothStep(0.99f, 0.94f, r) *
                                    (0.35f + 0.65f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(ang * 12f), 2.5f));
                    float a = Mathf.Clamp01(spiral * bodyMask * 0.85f + rimDash * 0.85f);
                    Color col = Color.Lerp(new Color(0.52f, 0.88f, 1f), new Color(0.92f, 0.98f, 1f), spiral * 0.6f);
                    col.a = a;
                    tex.SetPixel(x, y, col);
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vành mây bão xoáy mềm bán trong suốt cho đỉnh lốc xoáy (chừa mắt bão ở tâm để thấy rõ phễu bên trong).</summary>
        static Texture2D BuildStormCloudTexture()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);

                    float swirl = 0.5f + 0.5f * Mathf.Sin(ang * 5f + r * 7.5f);
                    float billow = Mathf.PerlinNoise(nx * 3.8f + 4.2f, ny * 3.8f + 1.9f);
                    float ringMask = Mathf.SmoothStep(0.26f, 0.56f, r) * Mathf.SmoothStep(1.0f, 0.64f, r);
                    float cloud = ringMask * (0.45f + 0.35f * billow + 0.25f * swirl);
                    float shade = 0.72f + 0.28f * swirl;
                    tex.SetPixel(x, y, new Color(shade, shade, shade, Mathf.Clamp01(cloud)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Vân vết nứt Động Đất: rãnh đất sâu đen kịt + đường nứt phát sáng cam-hổ phách dọc mép.</summary>
        static void BuildFissureTextures(out Texture2D darkTex, out Texture2D glowTex)
        {
            const int W = 128, H = 128;
            darkTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapModeU = TextureWrapMode.Clamp, wrapModeV = TextureWrapMode.Repeat };
            glowTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapModeU = TextureWrapMode.Clamp, wrapModeV = TextureWrapMode.Repeat };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / (W - 1), v = (float)y / H;
                    float jagged = (Mathf.PerlinNoise(v * 9f + 2f, u * 3f) - 0.5f) * 0.16f;
                    float dist = Mathf.Abs((u + jagged) - 0.5f) * 2f;

                    float chasmA = Mathf.SmoothStep(0.76f, 0.28f, dist);
                    float shade = Mathf.Lerp(0.03f, 0.16f, Mathf.SmoothStep(0.18f, 0.72f, dist));
                    darkTex.SetPixel(x, y, new Color(shade, shade * 0.78f, shade * 0.62f, chasmA * 0.95f));

                    float edgeGlow = Mathf.SmoothStep(0.62f, 0.34f, dist) * Mathf.SmoothStep(0.06f, 0.28f, dist);
                    float coreVein = Mathf.SmoothStep(0.16f, 0f, dist) * (0.55f + 0.45f * Mathf.Sin(v * Mathf.PI * 6f));
                    float gA = Mathf.Clamp01(edgeGlow * 0.9f + coreVein);
                    Color gc = Color.Lerp(new Color(1f, 0.45f, 0.06f), new Color(1f, 0.9f, 0.45f), coreVein);
                    glowTex.SetPixel(x, y, new Color(gc.r, gc.g, gc.b, gA));
                }
            darkTex.Apply();
            glowTex.Apply();
        }

        GameObject Track(GameObject go) { spawned.Add(go); return go; }
        static CoreHealth Core(Team team) => MatchManager.I != null ? MatchManager.I.Core(team) : null;
        static Vector3 CorePos(Team team) { var c = Core(team); return c != null ? c.transform.position : Arena.CorePosition(team); }

        /// <summary>Dọn vật thể hiệu ứng khi reset ván. Hàng đợi hiệu ứng lớn được giữ lại để quà không bị mất.</summary>
        public void ClearAll()
        {
            StopAllCoroutines();
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
        }
    }
}
