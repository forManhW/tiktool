using TMPro;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Xe tăng / robot triệu hồi: có máu, thanh máu và tên người triệu hồi trên đầu.
    /// Máu tự cạn dần theo thời gian tồn tại; lính phe địch đứng sát sẽ đấm làm mất máu nhanh hơn. Hết máu thì nổ.
    /// </summary>
    public class Vehicle : MonoBehaviour
    {
        public Team Team { get; private set; }
        public Viewer Owner { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool Alive => Hp > 0f;
        public float HitRadius => hitRadius;

        /// <summary>Xe tăng / robot đang có trên sân, để lính tìm mà vây đánh.</summary>
        public static readonly System.Collections.Generic.List<Vehicle> Active = new System.Collections.Generic.List<Vehicle>();
        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        public static Vehicle Nearest(Team team, Vector3 from, out float dist)
        {
            Vehicle best = null; dist = float.MaxValue;
            foreach (var v in Active)
            {
                if (v == null || v.Team != team || !v.Alive) continue;
                Vector3 d = v.transform.position - from; d.y = 0f;
                float m = d.magnitude;
                if (m < dist) { dist = m; best = v; }
            }
            return best;
        }

        /// <summary>Bị lính địch đấm.</summary>
        public void TakeDamage(float amount) { if (Alive) Hp = Mathf.Max(0f, Hp - amount); }

        float decayPerSecond, hitRadius, enemyDps, nextHitFx;
        Transform bar, fill;
        TextMeshPro label;

        public void Init(Team team, Viewer owner, float maxHp, float lifetime, float height, float hitRadius)
        {
            Team = team; Owner = owner;
            MaxHp = maxHp; Hp = maxHp;
            decayPerSecond = maxHp / Mathf.Max(1f, lifetime);
            this.hitRadius = hitRadius;
            enemyDps = GameSettings.Current.fighterDamage * 0.8f;

            var head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, height, 0f);

            bar = new GameObject("HpBar").transform;
            bar.SetParent(head, false);
            FighterManager.MakeQuad(bar, "Back", TeamMaterials.HpBack(), new Vector3(2.6f, 0.3f, 1f), 0.001f);
            fill = FighterManager.MakeQuad(bar, "Fill", TeamMaterials.HpFill(team), new Vector3(2.5f, 0.2f, 1f), 0f);

            string name = owner != null ? owner.Nickname : "";
            if (!string.IsNullOrEmpty(name))
            {
                var go = new GameObject("Name");
                go.transform.SetParent(head, false);
                go.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                label = go.AddComponent<TextMeshPro>();
                if (GameAssets.I.nameFont != null) label.font = GameAssets.I.nameFont;
                label.text = name;
                label.fontSize = 7f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = TeamMaterials.TeamLight(team);
                label.outlineWidth = 0.25f;
                label.outlineColor = new Color32(10, 10, 20, 255);
                label.rectTransform.sizeDelta = new Vector2(20f, 2f);
            }
        }

        void Update()
        {
            var match = MatchManager.I;
            if (match != null && match.IsFighting && Alive)
            {
                float dt = Time.deltaTime;
                Hp = Mathf.Max(0f, Hp - decayPerSecond * dt); // tự cạn theo thời gian; lính địch vây đánh thì mất thêm (TakeDamage)
            }

            var cam = CameraRig.I != null ? CameraRig.I.cam : null;
            if (cam != null)
            {
                if (bar != null) bar.rotation = cam.transform.rotation;
                if (label != null) label.transform.rotation = cam.transform.rotation;
            }
            if (fill != null)
            {
                float r = MaxHp > 0f ? Hp / MaxHp : 0f;
                fill.localScale = new Vector3(2.5f * r, fill.localScale.y, 1f);
                fill.localPosition = new Vector3(-(1f - r) * 1.25f, 0f, 0f);
            }
        }
    }
}
