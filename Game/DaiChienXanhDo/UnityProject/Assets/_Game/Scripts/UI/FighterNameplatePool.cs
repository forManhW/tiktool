using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-3.3: Pool tên nhân vật trên đầu chiến binh (tối đa 200 label dựng sẵn).
    /// Tối ưu hiệu năng cho 200 lính >= 60 FPS: không Instantiate/Destroy mỗi frame,
    /// không LINQ / string.Split mỗi frame, chỉ đổi .text khi chuỗi thực sự thay đổi.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class FighterNameplatePool : MonoBehaviour
    {
        [Header("References")]
        public RectTransform containerRect;
        public Canvas parentCanvas;
        [Tooltip("Mẫu TMP_Text (đã gắn font Be Vietnam Pro + outline/underlay) để nhân bản sẵn 200 slot khi Awake.")]
        public TMP_Text labelTemplate;
        public TMP_FontAsset fallbackFont;
        [Tooltip("Tùy chọn: nếu UISetupBuilder đã tạo sẵn các TMP_Text con thì gán vào đây.")]
        public TMP_Text[] prebuiltLabels;

        [Header("Pool & Layout")]
        public int poolSize = 200;
        public float normalWorldHeight = 2.6f;
        public float superWorldHeight = 4.4f;

        [Header("Typography & Colors")]
        public float mainFontSize = 19f;
        public float superFontSize = 22f;
        public float subFontSize = 13.5f;
        public Color mainColor = Color.white;
        public Color subColor = new Color(1f, 1f, 1f, 0.68f);
        public Color highlightColor = new Color32(0xFF, 0xE0, 0x66, 0xFF); // #FFE066

        [Header("Crowding Rules")]
        public float topRefreshInterval = 0.5f;
        public float recentActionSeconds = 5f;

        struct PoolSlot
        {
            public GameObject go;
            public RectTransform rect;
            public TMP_Text text;
            public string lastText;
            public Color lastColor;
            public float lastFontSize;
            public FontStyles lastFontStyle;
            public bool active;
        }

        PoolSlot[] slots;
        int prevUsedCount;

        readonly HashSet<string> topViewerIds = new HashSet<string>(16);
        readonly Dictionary<string, string> initialsCache = new Dictionary<string, string>(256);
        readonly char[] initialsBuf = new char[3];
        float nextTopRefreshTime;

        void Awake()
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            if (containerRect == null) containerRect = transform as RectTransform;

            if (labelTemplate != null && labelTemplate.gameObject.scene.IsValid())
            {
                labelTemplate.gameObject.SetActive(false);
            }

            BuildPool();
        }

        void OnEnable()
        {
            GameEvents.RoundReset += OnRoundReset;
        }

        void OnDisable()
        {
            GameEvents.RoundReset -= OnRoundReset;
        }

        void OnRoundReset()
        {
            topViewerIds.Clear();
            nextTopRefreshTime = 0f;
            if (initialsCache.Count > 1024) initialsCache.Clear();
            HideUnused(0);
        }

        void BuildPool()
        {
            int count = Mathf.Max(1, poolSize);
            slots = new PoolSlot[count];

            int prebuiltCount = prebuiltLabels != null ? prebuiltLabels.Length : 0;

            for (int i = 0; i < count; i++)
            {
                TMP_Text label = null;
                if (i < prebuiltCount && prebuiltLabels[i] != null)
                {
                    label = prebuiltLabels[i];
                }
                else if (labelTemplate != null)
                {
                    label = Instantiate(labelTemplate, containerRect, false);
                    label.name = $"Nameplate_{i}";
                }
                else
                {
                    var go = new GameObject($"Nameplate_{i}", typeof(RectTransform), typeof(TextMeshProUGUI));
                    go.transform.SetParent(containerRect, false);
                    label = go.GetComponent<TextMeshProUGUI>();
                    if (fallbackFont != null) label.font = fallbackFont;
                }

                ConfigureLabel(label);

                slots[i] = new PoolSlot
                {
                    go = label.gameObject,
                    rect = label.rectTransform,
                    text = label,
                    lastText = null,
                    lastColor = default,
                    lastFontSize = -1f,
                    lastFontStyle = (FontStyles)(-1),
                    active = false
                };

                label.gameObject.SetActive(false);
            }

            prevUsedCount = 0;
        }

        void ConfigureLabel(TMP_Text label)
        {
            label.raycastTarget = false;
            label.maskable = false;
            label.richText = false;
            label.extraPadding = false;
            label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(240f, 32f);
        }

        void LateUpdate()
        {
            if (slots == null || containerRect == null) return;

            var fm = FighterManager.I;
            Camera cam = CameraRig.I != null && CameraRig.I.cam != null ? CameraRig.I.cam : Camera.main;
            if (fm == null || cam == null || fm.Total == 0)
            {
                if (prevUsedCount > 0) HideUnused(0);
                return;
            }

            float now = Time.time;
            if (now >= nextTopRefreshTime)
            {
                nextTopRefreshTime = now + topRefreshInterval;
                RefreshTopViewers();
            }

            int fullLimit = GameSettings.Current != null ? GameSettings.Current.fullNameLimit : 40;
            bool crowded = fm.Total > fullLimit;
            Transform followed = CameraRig.I != null ? CameraRig.I.Following : null;
            Rect camPx = cam.pixelRect;

            Camera uiCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? parentCanvas.worldCamera
                : null;

            // 1) Gom ứng viên, 2) xếp theo độ ưu tiên, 3) đặt lần lượt, nhãn nào chồng lên nhãn đã đặt
            //    (hoặc đè thanh máu lõi) thì bỏ. Không cấp phát bộ nhớ mỗi frame.
            candCount = 0;
            CollectTeam(fm.List(Team.Blue), crowded, followed, now, cam, camPx);
            CollectTeam(fm.List(Team.Red), crowded, followed, now, cam, camPx);
            Array.Sort(cands, 0, candCount, CandComparer.Instance);

            float uiScale = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
            placedCount = 0;
            ReserveCoreBar(Team.Blue, cam, uiScale);
            ReserveCoreBar(Team.Red, cam, uiScale);
            labeledOwners.Clear();

            int used = 0;
            for (int c = 0; c < candCount && used < slots.Length; c++)
            {
                ref Cand cd = ref cands[c];
                // Lính phụ chỉ hiện tên khi chủ của nó chưa có nhãn nào trên màn hình.

                float fontSize = cd.isMain ? (cd.isSuper ? superFontSize : mainFontSize) : subFontSize;
                float w = (cd.display.Length * 0.56f * fontSize + 8f) * uiScale;
                float h = fontSize * 1.25f * uiScale;
                // Vướng nhãn khác / thanh máu lõi thì đẩy nhãn lên từng nấc (tối đa 3), vẫn vướng mới bỏ.
                // Nhân vật chính có ưu tiên cao thì được thử nhiều nấc hơn lính phụ.
                int maxSteps = cd.priority >= 2 ? 3 : 1;
                float y = cd.sy;
                var r = new Rect(cd.sx - w * 0.5f, y, w, h);
                bool ok = cd.priority >= PriorityFollowed || !Overlaps(r);
                for (int step = 1; !ok && step <= maxSteps; step++)
                {
                    y = cd.sy + step * h * 1.05f;
                    r = new Rect(cd.sx - w * 0.5f, y, w, h);
                    ok = !Overlaps(r);
                }
                if (!ok) continue;
                if (placedCount < placed.Length) placed[placedCount++] = r;
                if (!string.IsNullOrEmpty(cd.ownerId)) labeledOwners.Add(cd.ownerId);

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, new Vector2(cd.sx, y), uiCam, out Vector2 localPt))
                    continue;
                ApplySlot(used, cd.display, cd.isMain, cd.isSuper, cd.highlight, localPt);
                used++;
            }

            HideUnused(used);
        }

        const int PriorityFollowed = 4;

        struct Cand
        {
            public int priority;
            public float sx, sy;
            public string display, ownerId;
            public bool isMain, isSuper, highlight;
        }

        sealed class CandComparer : IComparer<Cand>
        {
            public static readonly CandComparer Instance = new CandComparer();
            public int Compare(Cand a, Cand b) => b.priority != a.priority ? b.priority.CompareTo(a.priority) : b.sy.CompareTo(a.sy);
        }

        readonly Cand[] cands = new Cand[400];
        int candCount;
        readonly Rect[] placed = new Rect[402];
        int placedCount;
        readonly HashSet<string> labeledOwners = new HashSet<string>();

        void CollectTeam(IReadOnlyList<Fighter> list, bool crowded, Transform followed, float now, Camera cam, Rect camPx)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count && candCount < cands.Length; i++)
            {
                Fighter f = list[i];
                if (f == null || !f.IsAlive) continue;
                Viewer owner = f.Owner;
                if (owner == null) continue;

                bool isMain = f.IsMain;
                bool isFollowed = f.transform == followed;
                bool highlight = isFollowed
                    || (!string.IsNullOrEmpty(owner.Id) && topViewerIds.Contains(owner.Id))
                    || (now - owner.LastActionTime < recentActionSeconds);

                string display = (crowded && !highlight) ? GetInitials(owner.Nickname) : owner.Nickname;
                if (string.IsNullOrEmpty(display)) continue;

                Vector3 worldPos = f.transform.position;
                worldPos.y += f.IsSuper ? superWorldHeight : normalWorldHeight;
                Vector3 sp = cam.WorldToScreenPoint(worldPos);
                if (sp.z <= 0f || sp.x < camPx.xMin || sp.x > camPx.xMax || sp.y < camPx.yMin || sp.y > camPx.yMax) continue;

                cands[candCount++] = new Cand
                {
                    priority = isFollowed ? PriorityFollowed : isMain ? (highlight ? 3 : 2) : (highlight ? 1 : 0),
                    sx = sp.x, sy = sp.y,
                    display = display, ownerId = owner.Id,
                    isMain = isMain, isSuper = f.IsSuper, highlight = highlight,
                };
            }
        }

        /// <summary>Chừa vùng thanh máu + nhãn "LÕI XANH/ĐỎ" để tên lính không đè lên.</summary>
        void ReserveCoreBar(Team team, Camera cam, float uiScale)
        {
            var core = MatchManager.I != null ? MatchManager.I.Core(team) : null;
            if (core == null) return;
            Vector3 sp = cam.WorldToScreenPoint(core.transform.position + Vector3.up * CoreHealth.BarHeight);
            if (sp.z <= 0f) return;
            float w = 250f * uiScale, h = 80f * uiScale;
            placed[placedCount++] = new Rect(sp.x - w * 0.5f, sp.y - 24f * uiScale, w, h);
        }

        bool Overlaps(Rect r)
        {
            for (int i = 0; i < placedCount; i++) if (placed[i].Overlaps(r)) return true;
            return false;
        }

        void ApplySlot(int used, string display, bool isMain, bool isSuper, bool highlight, Vector2 localPt)
        {
                ref PoolSlot slot = ref slots[used];
                if (!slot.active)
                {
                    slot.go.SetActive(true);
                    slot.active = true;
                }

                // Chỉ cập nhật thuộc tính TMP khi thực sự đổi để tránh rebuild mesh mỗi frame
                if (!ReferenceEquals(slot.lastText, display) && !string.Equals(slot.lastText, display, StringComparison.Ordinal))
                {
                    slot.lastText = display;
                    slot.text.text = display;
                }

                float desiredSize = isMain ? (isSuper ? superFontSize : mainFontSize) : subFontSize;
                if (slot.lastFontSize != desiredSize)
                {
                    slot.lastFontSize = desiredSize;
                    slot.text.fontSize = desiredSize;
                }

                FontStyles desiredStyle = isMain ? FontStyles.Bold : FontStyles.Normal;
                if (slot.lastFontStyle != desiredStyle)
                {
                    slot.lastFontStyle = desiredStyle;
                    slot.text.fontStyle = desiredStyle;
                }

                Color desiredColor;
                if (isMain)
                {
                    desiredColor = highlight ? highlightColor : mainColor;
                }
                else
                {
                    desiredColor = highlight
                        ? new Color(highlightColor.r, highlightColor.g, highlightColor.b, subColor.a)
                        : subColor;
                }

                if (slot.lastColor != desiredColor)
                {
                    slot.lastColor = desiredColor;
                    slot.text.color = desiredColor;
                }

                slot.rect.anchoredPosition = localPt;
        }

        void HideUnused(int used)
        {
            for (int i = used; i < prevUsedCount; i++)
            {
                ref PoolSlot slot = ref slots[i];
                if (slot.active)
                {
                    slot.go.SetActive(false);
                    slot.active = false;
                }
            }
            prevUsedCount = used;
        }

        /// <summary>Quét top 3 mỗi phe không dùng LINQ, chạy định kỳ mỗi 0.5s để giữ GC gần như bằng 0.</summary>
        void RefreshTopViewers()
        {
            topViewerIds.Clear();

            Viewer b0 = null, b1 = null, b2 = null;
            Viewer r0 = null, r1 = null, r2 = null;

            foreach (Viewer v in PlayerRegistry.All)
            {
                if (v == null || v.RoundPoints <= 0) continue;

                if (v.Team == Team.Blue)
                {
                    InsertTop3(v, ref b0, ref b1, ref b2);
                }
                else
                {
                    InsertTop3(v, ref r0, ref r1, ref r2);
                }
            }

            if (b0 != null && !string.IsNullOrEmpty(b0.Id)) topViewerIds.Add(b0.Id);
            if (b1 != null && !string.IsNullOrEmpty(b1.Id)) topViewerIds.Add(b1.Id);
            if (b2 != null && !string.IsNullOrEmpty(b2.Id)) topViewerIds.Add(b2.Id);
            if (r0 != null && !string.IsNullOrEmpty(r0.Id)) topViewerIds.Add(r0.Id);
            if (r1 != null && !string.IsNullOrEmpty(r1.Id)) topViewerIds.Add(r1.Id);
            if (r2 != null && !string.IsNullOrEmpty(r2.Id)) topViewerIds.Add(r2.Id);
        }

        static void InsertTop3(Viewer candidate, ref Viewer top0, ref Viewer top1, ref Viewer top2)
        {
            int pts = candidate.RoundPoints;
            if (top0 == null || pts > top0.RoundPoints)
            {
                top2 = top1;
                top1 = top0;
                top0 = candidate;
            }
            else if (top1 == null || pts > top1.RoundPoints)
            {
                top2 = top1;
                top1 = candidate;
            }
            else if (top2 == null || pts > top2.RoundPoints)
            {
                top2 = candidate;
            }
        }

        /// <summary>Tính chữ viết tắt tối đa 3 ký tự hoa (VD: "Nguyễn Thảo Uyên" -> "NTU") và cache lại.</summary>
        string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (initialsCache.TryGetValue(name, out string cached)) return cached;

            if (initialsCache.Count > 2048) initialsCache.Clear();

            int count = 0;
            bool inWord = false;
            int len = name.Length;
            for (int i = 0; i < len && count < 3; i++)
            {
                char c = name[i];
                if (char.IsWhiteSpace(c))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    initialsBuf[count++] = char.ToUpperInvariant(c);
                }
            }

            string result = count > 0 ? new string(initialsBuf, 0, count) : string.Empty;
            initialsCache[name] = result;
            return result;
        }
    }
}
