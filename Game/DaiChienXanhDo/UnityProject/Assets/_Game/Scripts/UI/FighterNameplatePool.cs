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

            int used = 0;
            used = RenderTeam(fm.List(Team.Blue), used, crowded, followed, now, cam, camPx, uiCam);
            used = RenderTeam(fm.List(Team.Red), used, crowded, followed, now, cam, camPx, uiCam);

            HideUnused(used);
        }

        int RenderTeam(
            IReadOnlyList<Fighter> list,
            int used,
            bool crowded,
            Transform followed,
            float now,
            Camera cam,
            Rect camPx,
            Camera uiCam)
        {
            if (list == null) return used;
            int listCount = list.Count;
            int maxSlots = slots.Length;

            for (int i = 0; i < listCount && used < maxSlots; i++)
            {
                Fighter f = list[i];
                if (f == null || !f.IsAlive) continue;

                Viewer owner = f.Owner;
                if (owner == null) continue;

                bool isMain = f.IsMain;
                if (crowded && !isMain) continue;

                bool highlight = f.transform == followed
                    || (!string.IsNullOrEmpty(owner.Id) && topViewerIds.Contains(owner.Id))
                    || (now - owner.LastActionTime < recentActionSeconds);

                string nickname = owner.Nickname;
                string display = (crowded && !highlight) ? GetInitials(nickname) : nickname;
                if (string.IsNullOrEmpty(display)) continue;

                bool isSuper = f.IsSuper;
                Vector3 worldPos = f.transform.position;
                worldPos.y += isSuper ? superWorldHeight : normalWorldHeight;

                Vector3 sp = cam.WorldToScreenPoint(worldPos);
                if (sp.z <= 0f || sp.x < camPx.xMin || sp.x > camPx.xMax || sp.y < camPx.yMin || sp.y > camPx.yMax)
                {
                    continue;
                }

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, new Vector2(sp.x, sp.y), uiCam, out Vector2 localPt))
                {
                    continue;
                }

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
                used++;
            }

            return used;
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
