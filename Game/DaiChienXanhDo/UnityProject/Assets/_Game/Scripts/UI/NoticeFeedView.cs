using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-4.2: Dòng thông báo thời gian thực (chỉ hiện ở chế độ Live tự động).
    /// Pool cố định 4 dòng (không Instantiate/Destroy), dòng mới đẩy dòng cũ lên trên,
    /// mỗi dòng tồn tại 4 giây và mờ dần trong 0.6 giây cuối, tô màu theo NoticeKind.
    /// </summary>
    public class NoticeFeedView : MonoBehaviour
    {
        [Serializable]
        public class NoticeRowSlot
        {
            public GameObject root;
            public CanvasGroup canvasGroup;
            public Image background;
            public Image accentBar;
            public TMP_Text label;
        }

        struct NoticeEntry
        {
            public string Text;
            public NoticeKind Kind;
            public float SpawnTime;
            public float ExpireTime;
        }

        const int MaxLines = 4;

        static readonly Color InfoTextColor = new Color32(0xE8, 0xF6, 0xFF, 0xFF);
        static readonly Color InfoAccentColor = new Color32(0x4D, 0xD0, 0xE1, 0xFF);
        static readonly Color InfoBgColor = new Color32(0x0D, 0x17, 0x26, 0xCC);

        static readonly Color JoinTextColor = new Color32(0xEC, 0xFD, 0xF5, 0xFF);
        static readonly Color JoinAccentColor = new Color32(0x4A, 0xDE, 0x80, 0xFF);
        static readonly Color JoinBgColor = new Color32(0x0B, 0x22, 0x18, 0xCC);

        static readonly Color GiftTextColor = new Color32(0xFF, 0xF3, 0xC4, 0xFF);
        static readonly Color GiftAccentColor = new Color32(0xF4, 0x72, 0xB6, 0xFF);
        static readonly Color GiftBgColor = new Color32(0x28, 0x10, 0x28, 0xD4);

        static readonly Color WarningTextColor = new Color32(0xFF, 0xE8, 0xCC, 0xFF);
        static readonly Color WarningAccentColor = new Color32(0xFF, 0x52, 0x52, 0xFF);
        static readonly Color WarningBgColor = new Color32(0x2E, 0x10, 0x10, 0xD8);

        static readonly Color GuideTextColor = new Color32(0xE0, 0xF2, 0xFE, 0xFF);
        static readonly Color GuideAccentColor = new Color32(0x38, 0xBD, 0xF8, 0xFF);
        static readonly Color GuideBgColor = new Color32(0x0C, 0x1E, 0x33, 0xCC);

        [Header("Pre-pooled Slots (4 rows, oldest at index 0 -> newest at bottom)")]
        public NoticeRowSlot[] slots = new NoticeRowSlot[MaxLines];

        [Header("Timing")]
        public float displayDuration = 4f;
        public float fadeOutDuration = 0.6f;
        public float fadeInDuration = 0.16f;

        readonly NoticeEntry[] entries = new NoticeEntry[MaxLines];
        int activeCount;
        bool layoutDirty;

        void Awake()
        {
            ClearAll();
        }

        void OnEnable()
        {
            GameEvents.Notice += OnNotice;
            GameEvents.RoundReset += OnRoundReset;
            RefreshSlotBindings();
        }

        void OnDisable()
        {
            GameEvents.Notice -= OnNotice;
            GameEvents.RoundReset -= OnRoundReset;
            ClearAll();
        }

        void OnNotice(string text, NoticeKind kind)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            int capacity = slots != null && slots.Length > 0 ? Mathf.Min(slots.Length, MaxLines) : MaxLines;
            float now = Time.time;

            if (activeCount >= capacity)
            {
                // Đẩy các dòng cũ lên trên, loại bỏ dòng cũ nhất ở index 0
                for (int i = 1; i < activeCount; i++)
                {
                    entries[i - 1] = entries[i];
                }
                activeCount = capacity - 1;
            }

            entries[activeCount] = new NoticeEntry
            {
                Text = text,
                Kind = kind,
                SpawnTime = now,
                ExpireTime = now + Mathf.Max(0.5f, displayDuration)
            };
            activeCount++;
            layoutDirty = true;
            RefreshSlotBindings();
        }

        void OnRoundReset()
        {
            ClearAll();
        }

        public void ClearAll()
        {
            activeCount = 0;
            layoutDirty = false;
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (slot.canvasGroup != null) slot.canvasGroup.alpha = 0f;
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        void Update()
        {
            if (activeCount == 0 || slots == null) return;

            float now = Time.time;
            bool removedAny = false;

            // Xóa các dòng đã hết hạn (giữ nguyên thứ tự cũ -> mới, không cấp phát bộ nhớ)
            int write = 0;
            for (int i = 0; i < activeCount; i++)
            {
                if (now < entries[i].ExpireTime)
                {
                    if (write != i) entries[write] = entries[i];
                    write++;
                }
                else
                {
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                activeCount = write;
                layoutDirty = true;
            }

            if (layoutDirty)
            {
                RefreshSlotBindings();
            }

            // Chỉ cập nhật alpha mỗi frame (0 GC allocation)
            float fadeOut = Mathf.Max(0.05f, fadeOutDuration);
            float fadeIn = Mathf.Max(0.01f, fadeInDuration);
            int limit = Mathf.Min(activeCount, slots.Length);

            for (int i = 0; i < limit; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.canvasGroup == null) continue;

                ref NoticeEntry entry = ref entries[i];
                float remaining = entry.ExpireTime - now;
                float elapsed = now - entry.SpawnTime;

                float alpha = 1f;
                if (elapsed < fadeIn)
                {
                    alpha = Mathf.Clamp01(elapsed / fadeIn);
                }
                else if (remaining < fadeOut)
                {
                    alpha = Mathf.Clamp01(remaining / fadeOut);
                }

                slot.canvasGroup.alpha = alpha;
            }
        }

        void RefreshSlotBindings()
        {
            layoutDirty = false;
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                bool show = i < activeCount;
                if (slot.root != null && slot.root.activeSelf != show)
                {
                    slot.root.SetActive(show);
                }

                if (!show)
                {
                    if (slot.canvasGroup != null) slot.canvasGroup.alpha = 0f;
                    continue;
                }

                ref NoticeEntry entry = ref entries[i];
                GetKindStyle(entry.Kind, out Color textColor, out Color accentColor, out Color bgColor);

                if (slot.label != null)
                {
                    slot.label.text = entry.Text;
                    slot.label.color = textColor;
                }
                if (slot.accentBar != null)
                {
                    slot.accentBar.color = accentColor;
                }
                if (slot.background != null)
                {
                    slot.background.color = bgColor;
                }
            }
        }

        static void GetKindStyle(NoticeKind kind, out Color textColor, out Color accentColor, out Color bgColor)
        {
            switch (kind)
            {
                case NoticeKind.Join:
                    textColor = JoinTextColor;
                    accentColor = JoinAccentColor;
                    bgColor = JoinBgColor;
                    break;
                case NoticeKind.Gift:
                    textColor = GiftTextColor;
                    accentColor = GiftAccentColor;
                    bgColor = GiftBgColor;
                    break;
                case NoticeKind.Warning:
                    textColor = WarningTextColor;
                    accentColor = WarningAccentColor;
                    bgColor = WarningBgColor;
                    break;
                case NoticeKind.Guide:
                    textColor = GuideTextColor;
                    accentColor = GuideAccentColor;
                    bgColor = GuideBgColor;
                    break;
                default:
                    textColor = InfoTextColor;
                    accentColor = InfoAccentColor;
                    bgColor = InfoBgColor;
                    break;
            }
        }
    }
}
