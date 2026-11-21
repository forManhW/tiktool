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

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int PunchId = Animator.StringToHash("Punch");

        struct Job { public EffectDef Def; public Team Team; public Viewer By; public int Amount; }

        readonly Queue<Job> bigQueue = new Queue<Job>();
        readonly List<GameObject> spawned = new List<GameObject>();
        float bigBusyUntil;

        public int QueuedBigEffects => bigQueue.Count;

        void Awake() { I = this; }

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
                    StartCoroutine(FallingRock(Arena.Clamp(zone + new Vector3(r.x, 0f, r.y)), enemy, 2.8f, Random.Range(2.2f, 3.4f), true)); // to, dễ thấy
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

        IEnumerator FallingRock(Vector3 point, Team? victims, float killRadius, float size, bool fiery)
        {
            var prefab = GameAssets.I.meteorPrefab;
            var rock = Track(prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere));
            foreach (var col in rock.GetComponentsInChildren<Collider>()) Destroy(col);
            float h = prefab != null ? FighterManager.MeasureHeight(rock) : 1f;
            rock.transform.localScale *= size / Mathf.Max(0.01f, h);
            if (prefab == null || fiery) SetMat(rock, fiery ? MeteorGlow() : GameAssets.I.meteorMaterial); // thiên thạch: đá đỏ rực phát sáng
            Vector3 from = point + new Vector3(10f, 34f, 10f); // rơi chéo để nhìn thấy cả đường bay
            Vector3 spin = Random.onUnitSphere * 360f;
            for (float t = 0f; t < 1f && rock != null; t += Time.deltaTime / (fiery ? 1.1f : 0.6f))
            {
                rock.transform.position = Vector3.Lerp(from, point, t * t);
                rock.transform.Rotate(spin * Time.deltaTime);
                if (fiery) { Fx.I.FireTrail(rock.transform.position, size * 1.1f); Fx.I.FireTrail(rock.transform.position + Vector3.up * size * 0.5f, size * 0.9f); Fx.I.SmokeTrail(rock.transform.position, size); }
                yield return null;
            }
            if (rock != null) Destroy(rock);
            if (fiery) { Fx.I.Explosion(point, 0.9f); Sfx.Explosion(0.6f); }
            else Fx.I.Dust(point, size * 1.5f);
            if (victims.HasValue) FighterManager.I.KillInRadius(victims.Value, point, killRadius);
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
                var funnel = Track(MakeTornado());
                float nextKill = 0f;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    Vector3 p = Vector3.Lerp(a, b, t / duration) + new Vector3(Mathf.Sin(t * 3f), 0f, Mathf.Cos(t * 2.3f)) * 2f;
                    float grow = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((duration - t) / 0.6f); // mọc lên rồi tan dần
                    if (funnel != null)
                    {
                        funnel.transform.position = p;
                        funnel.transform.localScale = new Vector3(1f, Mathf.Max(0.05f, grow), 1f);
                        funnel.transform.GetChild(0).Rotate(0f, 420f * Time.deltaTime, 0f);
                        funnel.transform.GetChild(1).Rotate(0f, -260f * Time.deltaTime, 0f);
                    }
                    Fx.I.Tornado(p, Time.time);
                    if (Time.time >= nextKill) { nextKill = Time.time + 0.12f; KillNear(p, 5f, quota, 1.6f); }
                    yield return null;
                }
                if (funnel != null) Destroy(funnel);
            }
            else if (kind == 0)
            {
                // Động đất: rung mạnh, bụi phụt lên từng đợt khắp sân
                CameraRig.I?.Shake(1.6f, duration);
                for (int wave = 0; wave < 6; wave++)
                {
                    for (int d = 0; d < 6; d++) Fx.I.Dust(new Vector3(Random.Range(-Arena.HalfSize, Arena.HalfSize), 0f, Random.Range(-Arena.HalfSize, Arena.HalfSize)), 3f);
                    for (int t = 0; t < 2; t++)
                    {
                        int n = Mathf.CeilToInt(quota[t] / (float)(6 - wave));
                        foreach (var f in FighterManager.I.RandomAlive((Team)t, n)) { Fx.I.Dust(f.transform.position, 1.5f); f.Die(); quota[t]--; }
                    }
                    yield return new WaitForSeconds(duration / 6f);
                }
            }
            else
            {
                // Núi lửa: mỗi viên đá lửa rơi trúng đúng một người
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

        static Mesh funnelMesh;
        static Texture2D windTex;

        /// <summary>Cột lốc xoáy: 2 lớp phễu xoắn (trong sẫm, ngoài sáng), loe rộng dần lên trên, vân gió chạy dọc.</summary>
        GameObject MakeTornado()
        {
            if (funnelMesh == null) funnelMesh = BuildFunnel(16f, 0.5f, 6.5f, 28, 18, 1.4f);
            if (windTex == null) windTex = BuildWindTexture();
            var root = new GameObject("Tornado");
            AddFunnelLayer(root, "Inner", 0.82f, new Color(0.42f, 0.38f, 0.34f, 0.85f));
            AddFunnelLayer(root, "Outer", 1.0f, new Color(0.78f, 0.74f, 0.68f, 0.55f));
            return root;
        }

        void AddFunnelLayer(GameObject root, string name, float scale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localScale = new Vector3(scale, 1f, scale);
            go.AddComponent<MeshFilter>().sharedMesh = funnelMesh;
            var r = go.AddComponent<MeshRenderer>();
            var m = new Material(GameAssets.I.fxSmokeMaterial);   // vật liệu hạt pha trộn: đã chạy được trong suốt
            m.SetTexture("_BaseMap", windTex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Cull", 0f);                                // thấy cả mặt trong phễu
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Phễu: các vòng tròn chồng lên nhau, bán kính tăng dần từ chân lên đỉnh, mỗi vòng xoắn lệch một góc.</summary>
        static Mesh BuildFunnel(float height, float rBottom, float rTop, int seg, int rings, float twist)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var t = new System.Collections.Generic.List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float k = (float)j / rings;
                float r = Mathf.Lerp(rBottom, rTop, k * k * 0.7f + k * 0.3f) * (1f + 0.08f * Mathf.Sin(k * 9f));
                float off = k * twist * Mathf.PI * 2f;
                Vector3 wobble = new Vector3(Mathf.Sin(k * 4f) * 0.8f * k, 0f, Mathf.Cos(k * 3f) * 0.6f * k);
                for (int i = 0; i <= seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2f + off;
                    v.Add(new Vector3(Mathf.Cos(a) * r, k * height, Mathf.Sin(a) * r) + wobble);
                    uv.Add(new Vector2((float)i / seg * 3f, k));
                }
            }
            int w = seg + 1;
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * w + i;
                    t.Add(a); t.Add(a + w); t.Add(a + 1);
                    t.Add(a + 1); t.Add(a + w); t.Add(a + w + 1);
                }
            var m = new Mesh { name = "TornadoFunnel" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Vân gió: các vệt xiên mờ dần ở chân và đỉnh phễu.</summary>
        static Texture2D BuildWindTexture()
        {
            const int W = 128, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H;
                    float streak = 0.5f + 0.5f * Mathf.Sin((u * 6f + v * 2.5f) * Mathf.PI * 2f);
                    streak = Mathf.Pow(streak, 3f) * 0.8f + Mathf.PerlinNoise(u * 12f, v * 6f) * 0.35f;
                    float fade = Mathf.SmoothStep(0f, 0.15f, v) * Mathf.SmoothStep(1f, 0.8f, v);
                    float a = Mathf.Clamp01(streak * fade);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }

        void KillNear(Vector3 center, float radius, int[] quota, float fling)
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
                    Fx.I.Dust(f.transform.position, fling);
                    f.Die();
                    quota[t]--;
                }
            }
        }

        IEnumerator LavaRock(Fighter victim)
        {
            Vector3 at = victim.transform.position;
            yield return FallingRock(at, null, 0f, Random.Range(1.4f, 2.2f), true);
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
            AttachFlag(root, team, new Vector3(0.4f, 1.6f, -1.2f), 2.4f);
            return Track(root);
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

        static Material meteorGlow;
        static Material MeteorGlow()
        {
            if (meteorGlow != null) return meteorGlow;
            meteorGlow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MeteorGlow" };
            meteorGlow.SetColor("_BaseColor", new Color(0.35f, 0.12f, 0.06f));
            meteorGlow.EnableKeyword("_EMISSION");
            meteorGlow.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.05f) * 2.2f);
            return meteorGlow;
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
