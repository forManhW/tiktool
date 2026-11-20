using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Chạy hiệu ứng theo bảng 2.4 của kế hoạch. Bản hiện tại là greybox (khối hình cơ bản).
    /// Bậc 0–3 chạy ngay; bậc 4+ xếp hàng, mỗi cái cách nhau tối thiểu 3 giây để người xem kịp thấy tên.
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager I { get; private set; }

        const float BigEffectSpacing = 3f;
        const float SuperSecondsPerUnit = 15f;
        const float SuperMaxSeconds = 60f;

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
                    int made = 0;
                    for (int i = 0; i < amount; i++) if (FighterManager.I.Spawn(team, by, false) != null) made++;
                    if (made < amount) TeamPower.AddLikePower(team, amount - made); // sân đầy: đổi phần dư thành sức mạnh
                    break;
                case "like_power":
                    TeamPower.AddLikePower(team, amount);
                    break;
                case "punch":
                    actor = actor != null ? actor : Stand_in(team, by);
                    if (actor != null) actor.AddChargedPunches(amount);
                    break;
                case "combo_punch":
                    actor = actor != null ? actor : Stand_in(team, by);
                    if (actor != null) actor.AddCombo(5 * amount);
                    break;
                case "superhero":
                    actor = actor != null ? actor : Stand_in(team, by);
                    if (actor != null) actor.MakeSuper(Mathf.Min(SuperMaxSeconds, SuperSecondsPerUnit * amount));
                    break;
            }
            Announce(def, team, by, amount, actor != null ? actor.transform : null, actor != null ? actor.transform.position : CorePos(team));
        }

        /// <summary>Người tặng quà của phe khác phe mình: hiệu ứng chạy qua một lính tạm mang tên họ.</summary>
        Fighter Stand_in(Team team, Viewer by)
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
                    Bolt(CorePos(enemy));
                    Core(enemy)?.TakeDamage(30f * TeamPower.Of(team), by);
                }
                foreach (var f in targets) { Bolt(f.transform.position); f.Die(); }
                CameraRig.I?.Shake(0.5f, 0.3f);
                yield return new WaitForSeconds(0.45f);
            }
        }

        IEnumerator Tank(Team team, Viewer by, int index)
        {
            Team enemy = team.Other();
            var tank = MakeVehicle(GameAssets.I.tankPrefab, team, new Vector3(2f, 1.2f, 3f), $"Tank_{team}");
            Vector3 start = CorePos(team) + new Vector3(index * 2f - 2f, 0f, -(index * 2f - 2f));
            tank.transform.position = start;
            Announce(CommandRouter.Effects["tank"], team, by, 1, tank.transform, start);

            float end = Time.time + 20f, nextCrush = 0f, nextShot = Time.time + 1f;
            while (Time.time < end && tank != null)
            {
                Vector3 target = CorePos(enemy);
                Vector3 to = target - tank.transform.position; to.y = 0f;
                if (to.magnitude > 6f) tank.transform.position += to.normalized * 3f * Time.deltaTime;
                if (to.sqrMagnitude > 0.01f) tank.transform.rotation = Quaternion.LookRotation(to.normalized);
                if (Time.time >= nextCrush) { nextCrush = Time.time + 0.2f; FighterManager.I.KillInRadius(enemy, tank.transform.position, 1.8f); }
                if (Time.time >= nextShot)
                {
                    nextShot = Time.time + 2f;
                    StartCoroutine(Shell(tank.transform.position + Vector3.up * 1.2f, target + Vector3.up, team, by, 20f));
                }
                yield return null;
            }
            if (tank != null) Destroy(tank);
        }

        IEnumerator Shell(Vector3 from, Vector3 to, Team team, Viewer by, float coreDamage)
        {
            var shell = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            Destroy(shell.GetComponent<Collider>());
            shell.transform.localScale = Vector3.one * 0.5f;
            SetMat(shell, GameAssets.I.meteorMaterial);
            for (float t = 0f; t < 1f && shell != null; t += Time.deltaTime / 0.5f)
            {
                shell.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 3f;
                yield return null;
            }
            if (shell != null) Destroy(shell);
            Core(team.Other())?.TakeDamage(coreDamage * TeamPower.Of(team), by);
            CameraRig.I?.Shake(0.25f, 0.2f);
        }

        IEnumerator Meteor(Team team, Viewer by, int waves)
        {
            Team enemy = team.Other();
            Announce(CommandRouter.Effects["meteor"], team, by, waves, null, Vector3.Lerp(CorePos(enemy), Vector3.zero, 0.4f));
            for (int w = 0; w < waves; w++)
            {
                for (int i = 0; i < 12; i++)
                {
                    StartCoroutine(FallingRock(Arena.RandomPointOnHalf(enemy), enemy, 2.2f, 1.2f));
                    yield return new WaitForSeconds(0.25f);
                }
                Core(enemy)?.TakeDamage(80f * TeamPower.Of(team), by);
                CameraRig.I?.Shake(0.9f, 0.6f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator FallingRock(Vector3 point, Team? victims, float killRadius, float size)
        {
            var prefab = GameAssets.I.meteorPrefab;
            var rock = Track(prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere));
            var col = rock.GetComponent<Collider>(); if (col != null) Destroy(col);
            rock.transform.localScale = Vector3.one * size;
            if (prefab == null) SetMat(rock, GameAssets.I.meteorMaterial);
            Vector3 from = point + new Vector3(6f, 30f, 6f);
            for (float t = 0f; t < 1f && rock != null; t += Time.deltaTime / 0.6f)
            {
                rock.transform.position = Vector3.Lerp(from, point, t * t);
                yield return null;
            }
            if (rock != null) Destroy(rock, 0.3f);
            if (victims.HasValue) FighterManager.I.KillInRadius(victims.Value, point, killRadius);
            CameraRig.I?.Shake(0.2f, 0.15f);
        }

        IEnumerator Robot(Team team, Viewer by)
        {
            Team enemy = team.Other();
            var robot = MakeVehicle(GameAssets.I.robotPrefab, team, new Vector3(3f, 7f, 3f), $"Robot_{team}");
            robot.transform.position = CorePos(team) + Vector3.up * 25f;
            Announce(CommandRouter.Effects["giant_robot"], team, by, 1, robot.transform, CorePos(team));

            // Rơi xuống sân
            Vector3 land = CorePos(team) + (Vector3.zero - CorePos(team)).normalized * 4f;
            for (float t = 0f; t < 1f && robot != null; t += Time.deltaTime / 0.8f)
            {
                robot.transform.position = Vector3.Lerp(land + Vector3.up * 25f, land, t * t);
                yield return null;
            }
            CameraRig.I?.Shake(1.5f, 0.8f);

            float end = Time.time + 25f, nextStomp = 0f;
            while (Time.time < end && robot != null)
            {
                Vector3 to = CorePos(enemy) - robot.transform.position; to.y = 0f;
                bool atCore = to.magnitude < 4.5f;
                if (!atCore) robot.transform.position += to.normalized * 2.5f * Time.deltaTime;
                if (to.sqrMagnitude > 0.01f) robot.transform.rotation = Quaternion.LookRotation(to.normalized);
                if (Time.time >= nextStomp)
                {
                    nextStomp = Time.time + (atCore ? 1f : 0.25f);
                    FighterManager.I.KillInRadius(enemy, robot.transform.position, 3.5f);
                    if (atCore) { Core(enemy)?.TakeDamage(50f * TeamPower.Of(team), by); CameraRig.I?.Shake(0.6f, 0.3f); }
                }
                yield return null;
            }
            if (robot != null) Destroy(robot);
        }

        /// <summary>Hình ảnh thảm họa (đá rơi khắp sân). Số lính chết do MatchEvents quyết định.</summary>
        public void PlayDisasterVisual() => StartCoroutine(DisasterRocks());

        IEnumerator DisasterRocks()
        {
            for (int i = 0; i < 25; i++)
            {
                var p = new Vector3(Random.Range(-Arena.HalfSize, Arena.HalfSize), 0f, Random.Range(-Arena.HalfSize, Arena.HalfSize));
                StartCoroutine(FallingRock(p, null, 0f, Random.Range(0.8f, 1.8f)));
                yield return new WaitForSeconds(0.06f);
            }
        }

        // ---------------- Tiện ích ----------------

        void Announce(EffectDef def, Team team, Viewer by, int amount, Transform target, Vector3 position)
        {
            GameEvents.RaiseEffectStarted(new EffectNotice
            {
                Team = team, EffectId = def.Id, DisplayName = def.DisplayName, Tier = def.Tier,
                Amount = amount, By = by, Position = position, Target = target,
            });
            if (def.Tier >= 3 && by != null)
                GameEvents.RaiseNotice($"{by.Nickname}: {def.DisplayName} cho phe {team.DisplayName()}!", NoticeKind.Gift);
        }

        void Bolt(Vector3 at)
        {
            var go = Track(new GameObject("Bolt"));
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 8;
            lr.widthMultiplier = 0.35f;
            if (GameAssets.I.boltMaterial != null) lr.sharedMaterial = GameAssets.I.boltMaterial;
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                Vector3 p = Vector3.Lerp(at + Vector3.up * 28f, at, t);
                if (i > 0 && i < 7) p += new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
                lr.SetPosition(i, p);
            }
            Destroy(go, 0.25f);
        }

        GameObject MakeVehicle(GameObject prefab, Team team, Vector3 size, string name)
        {
            GameObject go;
            if (prefab != null) go = Instantiate(prefab);
            else
            {
                go = new GameObject(name);
                var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(hull.GetComponent<Collider>());
                hull.transform.SetParent(go.transform, false);
                hull.transform.localScale = size;
                hull.transform.localPosition = Vector3.up * size.y * 0.5f;
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(top.GetComponent<Collider>());
                top.transform.SetParent(go.transform, false);
                top.transform.localScale = new Vector3(size.x * 0.6f, size.y * 0.35f, size.z * 0.5f);
                top.transform.localPosition = new Vector3(0f, size.y * 1.15f, size.z * 0.1f);
                SetMat(go, GameAssets.I.TeamMaterial(team));
            }
            return Track(go);
        }

        static void SetMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
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
