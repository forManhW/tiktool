using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>Sinh/thu hồi lính (object pool), tra cứu địch gần nhất, giết hàng loạt cho sấm sét/thảm họa.</summary>
    public class FighterManager : MonoBehaviour
    {
        public static FighterManager I { get; private set; }

        readonly List<Fighter>[] alive = { new List<Fighter>(), new List<Fighter>() };
        readonly Stack<Fighter>[] pool = { new Stack<Fighter>(), new Stack<Fighter>() };
        readonly List<Fighter> dying = new List<Fighter>();
        bool countDirty;

        public int Count(Team team) => alive[(int)team].Count;
        public int Total => alive[0].Count + alive[1].Count;
        public IReadOnlyList<Fighter> List(Team team) => alive[(int)team];

        void Awake() { I = this; }
        void OnEnable() { GameEvents.RoundWon += OnRoundWon; }
        void OnDisable() { GameEvents.RoundWon -= OnRoundWon; }

        void OnRoundWon(Team winner, Viewer mvp)
        {
            foreach (var f in alive[(int)winner]) f.Celebrate();
        }

        void LateUpdate()
        {
            Separate();
            if (!countDirty) return;
            countDirty = false;
            GameEvents.RaiseFighterCountChanged(Count(Team.Blue), Count(Team.Red));
        }

        // ---------- Đẩy nhẹ để lính không chồng lên nhau (lưới chia ô, chạy nhẹ với 200 lính) ----------
        const float Cell = 1.2f, MinGap = 0.85f;
        readonly Dictionary<long, List<Fighter>> grid = new Dictionary<long, List<Fighter>>();
        readonly Stack<List<Fighter>> listPool = new Stack<List<Fighter>>();

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        void Separate()
        {
            foreach (var l in grid.Values) { l.Clear(); listPool.Push(l); }
            grid.Clear();
            for (int t = 0; t < 2; t++)
                foreach (var f in alive[t])
                {
                    var p = f.transform.position;
                    long k = Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
                    if (!grid.TryGetValue(k, out var list)) { list = listPool.Count > 0 ? listPool.Pop() : new List<Fighter>(); grid[k] = list; }
                    list.Add(f);
                }
            float max = 2.5f * Time.deltaTime;
            for (int t = 0; t < 2; t++)
                foreach (var f in alive[t])
                {
                    Vector3 p = f.transform.position, push = Vector3.zero;
                    int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (!grid.TryGetValue(Key(cx + dx, cz + dz), out var list)) continue;
                            foreach (var o in list)
                            {
                                if (o == f) continue;
                                Vector3 d = p - o.transform.position; d.y = 0f;
                                float m2 = d.sqrMagnitude;
                                if (m2 >= MinGap * MinGap) continue;
                                if (m2 < 1e-6f) { d = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f); m2 = d.sqrMagnitude + 1e-6f; }
                                float m = Mathf.Sqrt(m2);
                                push += d / m * (MinGap - m);
                            }
                        }
                    if (push.sqrMagnitude > 1e-6f) f.Nudge(Vector3.ClampMagnitude(push * 0.5f, max));
                }
        }

        /// <summary>Sinh lính cạnh lõi phe mình. Trả null nếu sân đã chạm giới hạn cứng.</summary>
        public Fighter Spawn(Team team, Viewer owner, bool isMain)
        {
            if (Total >= GameSettings.Current.hardCap) return null;
            var f = pool[(int)team].Count > 0 ? pool[(int)team].Pop() : Create(team);
            f.Init(team, owner, isMain, SpawnPoint(team));
            alive[(int)team].Add(f);
            countDirty = true;
            return f;
        }

        Vector3 SpawnPoint(Team team)
        {
            var core = MatchManager.I != null ? MatchManager.I.Core(team) : null;
            Vector3 corePos = core != null ? core.transform.position : Arena.CorePosition(team);
            Vector3 toCenter = (Vector3.zero - corePos); toCenter.y = 0f; toCenter.Normalize();
            Vector2 r = Random.insideUnitCircle * 2f;
            return Arena.Clamp(corePos + toCenter * ((core != null ? core.Radius : 1.5f) + 1.5f) + new Vector3(r.x, 0f, r.y));
        }

        Fighter Create(Team team)
        {
            var assets = GameAssets.I;
            if (assets.HasFighterModels) return CreateFromModel(team, assets);

            var prefab = assets.FighterPrefab(team);
            Fighter f;
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform);
                f = go.GetComponent<Fighter>();
                if (f == null) f = go.AddComponent<Fighter>();
                if (go.transform.childCount > 0) f.SetBody(go.transform.GetChild(0));
            }
            else
            {
                var root = new GameObject($"Fighter_{team}");
                root.transform.SetParent(transform);
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = Vector3.up;
                var fist = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fist.name = "Fist";
                Destroy(fist.GetComponent<Collider>());
                fist.transform.SetParent(body.transform, false);
                fist.transform.localPosition = new Vector3(0.35f, 0.1f, 0.45f);
                fist.transform.localScale = Vector3.one * 0.3f;
                var mat = assets.TeamMaterial(team);
                if (mat != null) { body.GetComponent<Renderer>().sharedMaterial = mat; fist.GetComponent<Renderer>().sharedMaterial = mat; }
                f = root.AddComponent<Fighter>();
                f.SetBody(body.transform);
            }
            return f;
        }

        /// <summary>Người thật: model ngẫu nhiên, chỉnh về cùng chiều cao, gắn hoạt ảnh, vòng màu phe dưới chân.</summary>
        Fighter CreateFromModel(Team team, GameAssets assets)
        {
            var root = new GameObject($"Fighter_{team}");
            root.transform.SetParent(transform);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);

            var prefab = assets.fighterModels[Random.Range(0, assets.fighterModels.Length)];
            var model = Instantiate(prefab, body, false);
            model.name = "Model";
            float h = MeasureHeight(model);
            if (h > 0.01f) model.transform.localScale *= assets.fighterHeight / h;
            TeamMaterials.DressFighter(model, team); // áo màu phe, da người, chi tiết vàng
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = assets.fighterController;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var col = root.AddComponent<CapsuleCollider>(); // để click chọn nhân vật ở chế độ Thường
            col.center = new Vector3(0f, assets.fighterHeight * 0.5f, 0f);
            col.height = assets.fighterHeight;
            col.radius = 0.45f;

            // Thanh máu trên đầu: nền tối + phần máu màu phe, luôn quay về phía camera
            var bar = new GameObject("HpBar").transform;
            bar.SetParent(root.transform, false);
            bar.localPosition = new Vector3(0f, assets.fighterHeight + 0.35f, 0f);
            MakeQuad(bar, "Back", TeamMaterials.HpBack(), new Vector3(0.85f, 0.11f, 1f), 0.001f);
            var fill = MakeQuad(bar, "Fill", TeamMaterials.HpFill(team), new Vector3(0.8f, 0.07f, 1f), 0f);

            var f = root.AddComponent<Fighter>();
            f.SetBody(body, anim);
            f.SetHpBar(bar, fill);
            return f;
        }

        public static Transform MakeQuad(Transform parent, string name, Material mat, Vector3 scale, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localScale = scale;
            q.transform.localPosition = new Vector3(0f, 0f, z);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }

        public static float MeasureHeight(GameObject go)
        {
            bool has = false;
            Bounds b = default;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            return has ? b.size.y : 0f;
        }

        /// <summary>Lính rời trận (chết): bỏ khỏi danh sách đang đánh. Hình có thể còn nằm lại để chạy hoạt ảnh gục.</summary>
        public void Release(Fighter f)
        {
            alive[(int)f.Team].Remove(f);
            if (f.Owner != null && f.Owner.Fighter == f) f.Owner.Fighter = null;
            dying.Add(f);
            countDirty = true;
        }

        /// <summary>Cất hình vào pool để dùng lại.</summary>
        public void ReturnToPool(Fighter f)
        {
            if (!dying.Remove(f)) return;
            f.gameObject.SetActive(false);
            pool[(int)f.Team].Push(f);
        }

        public Fighter NearestEnemy(Fighter from)
        {
            var enemies = alive[(int)from.Team.Other()];
            Vector3 p = from.transform.position;
            Fighter best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                float d = (enemies[i].transform.position - p).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = enemies[i]; }
            }
            return best;
        }

        /// <summary>Lấy ngẫu nhiên tối đa count lính còn sống của một phe.</summary>
        public List<Fighter> RandomAlive(Team team, int count)
        {
            var list = new List<Fighter>(alive[(int)team]);
            for (int i = 0; i < list.Count; i++) { int j = Random.Range(i, list.Count); (list[i], list[j]) = (list[j], list[i]); }
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        /// <summary>Số lính sẽ chết theo tỉ lệ (chừa ít nhất keepMin), không giết ngay.</summary>
        public int KillQuota(Team team, float percent, int keepMin)
        {
            int n = Count(team);
            return Mathf.Clamp(Mathf.RoundToInt(n * percent), 0, Mathf.Max(0, n - keepMin));
        }

        /// <summary>Giết một tỉ lệ lính, luôn chừa lại ít nhất keepMin lính (dùng cho thảm họa).</summary>
        public int KillPercent(Team team, float percent, int keepMin)
        {
            int n = Count(team);
            int kill = Mathf.Clamp(Mathf.RoundToInt(n * percent), 0, Mathf.Max(0, n - keepMin));
            foreach (var f in RandomAlive(team, kill)) f.Die();
            return kill;
        }

        public int CountInRadius(Team team, Vector3 center, float radius)
        {
            var list = alive[(int)team];
            float r2 = radius * radius;
            int n = 0;
            for (int i = 0; i < list.Count; i++) { Vector3 d = list[i].transform.position - center; d.y = 0f; if (d.sqrMagnitude <= r2) n++; }
            return n;
        }

        public int KillInRadius(Team team, Vector3 center, float radius)
        {
            var list = alive[(int)team];
            float r2 = radius * radius;
            int killed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                Vector3 d = list[i].transform.position - center; d.y = 0f;
                if (d.sqrMagnitude <= r2) { list[i].Die(); killed++; }
            }
            return killed;
        }

        /// <summary>Reset ván: dọn sạch cả lính đang sống lẫn lính đang nằm gục.</summary>
        public void ClearAll()
        {
            for (int t = 0; t < 2; t++)
                for (int i = alive[t].Count - 1; i >= 0; i--)
                    if (i < alive[t].Count) alive[t][i].Die();
            for (int i = dying.Count - 1; i >= 0; i--) ReturnToPool(dying[i]);
        }
    }
}
