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
        bool countDirty;

        public int Count(Team team) => alive[(int)team].Count;
        public int Total => alive[0].Count + alive[1].Count;
        public IReadOnlyList<Fighter> List(Team team) => alive[(int)team];

        void Awake() { I = this; }

        void LateUpdate()
        {
            if (!countDirty) return;
            countDirty = false;
            GameEvents.RaiseFighterCountChanged(Count(Team.Blue), Count(Team.Red));
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
            var prefab = GameAssets.I.FighterPrefab(team);
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
                var mat = GameAssets.I.TeamMaterial(team);
                if (mat != null) { body.GetComponent<Renderer>().sharedMaterial = mat; fist.GetComponent<Renderer>().sharedMaterial = mat; }
                f = root.AddComponent<Fighter>();
                f.SetBody(body.transform);
            }
            return f;
        }

        public void Release(Fighter f)
        {
            alive[(int)f.Team].Remove(f);
            if (f.Owner != null && f.Owner.Fighter == f) f.Owner.Fighter = null;
            f.gameObject.SetActive(false);
            pool[(int)f.Team].Push(f);
            countDirty = true;
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

        /// <summary>Giết một tỉ lệ lính, luôn chừa lại ít nhất keepMin lính (dùng cho thảm họa).</summary>
        public int KillPercent(Team team, float percent, int keepMin)
        {
            int n = Count(team);
            int kill = Mathf.Clamp(Mathf.RoundToInt(n * percent), 0, Mathf.Max(0, n - keepMin));
            foreach (var f in RandomAlive(team, kill)) f.Die();
            return kill;
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

        public void ClearAll()
        {
            for (int t = 0; t < 2; t++)
                for (int i = alive[t].Count - 1; i >= 0; i--)
                    if (i < alive[t].Count) alive[t][i].Die();
        }
    }
}
