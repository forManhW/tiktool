using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-4.4: Banner sự kiện giữa trận đặt ở phía trên chính giữa (chỉ hiện ở Live tự động).
    /// Hỗ trợ đếm ngược trực tiếp:
    /// - x2: vàng rực (#FFD43B) đập nhịp
    /// - x2_weak: màu phe được ưu tiên
    /// - disaster_warning: đỏ khẩn cấp (#FF2B2B) nhấp nháy + rung nhẹ
    /// - disaster: banner tác động kịch tính trong 2.5 giây
    /// </summary>
    public class MatchEventBannerView : MonoBehaviour
    {
        struct ActiveEvent
        {
            public string Id;
            public string DisplayName;
            public bool HasTeam;
            public Team Team;
            public bool HasCountdown;
            public float StartTime;
            public float EndTime;
            public int Priority;
        }

        static readonly Color GoldColor = new Color32(0xFF, 0xD4, 0x3B, 0xFF);
        static readonly Color GoldBgColor = new Color32(0x2A, 0x20, 0x05, 0xE6);
        static readonly Color UrgentRedColor = new Color32(0xFF, 0x2B, 0x2B, 0xFF);
        static readonly Color UrgentFlashColor = new Color32(0xFF, 0x92, 0x2B, 0xFF);
        static readonly Color UrgentBgColor = new Color32(0x36, 0x08, 0x08, 0xF0);
        static readonly Color BlueBgColor = new Color32(0x0B, 0x1E, 0x3D, 0xE8);
        static readonly Color RedBgColor = new Color32(0x38, 0x0D, 0x12, 0xE8);

        [Header("Banner References")]
        public GameObject bannerRoot;
        public RectTransform animatedRect;
        public CanvasGroup canvasGroup;
        public Image backgroundImage;
        public Image borderImage;
        public TMP_Text titleText;
        public TMP_Text countdownText;

        [Header("Settings")]
        public float disasterImpactDuration = 2.5f;

        readonly List<ActiveEvent> activeEvents = new List<ActiveEvent>(4);

        Vector2 baseAnchoredPos;
        bool basePosCaptured;
        string displayedEventKey;
        int lastDisplayedSeconds = -1;

        void Awake()
        {
            CaptureBasePosition();
            HideImmediate();
        }

        void OnEnable()
        {
            CaptureBasePosition();
            GameEvents.MatchEventStarted += OnMatchEventStarted;
            GameEvents.MatchEventEnded += OnMatchEventEnded;
            GameEvents.RoundReset += OnRoundReset;
            RefreshDisplayedEvent(force: true);
        }

        void OnDisable()
        {
            GameEvents.MatchEventStarted -= OnMatchEventStarted;
            GameEvents.MatchEventEnded -= OnMatchEventEnded;
            GameEvents.RoundReset -= OnRoundReset;
            ClearAll();
        }

        void CaptureBasePosition()
        {
            if (basePosCaptured || animatedRect == null) return;
            baseAnchoredPos = animatedRect.anchoredPosition;
            basePosCaptured = true;
        }

        void OnMatchEventStarted(MatchEventNotice notice)
        {
            if (string.IsNullOrEmpty(notice.Id)) return;

            float now = Time.time;
            bool isImpact = notice.Id == "disaster" || notice.Duration <= 0f;
            float duration = isImpact ? Mathf.Max(0.5f, disasterImpactDuration) : notice.Duration;

            // Ghi đè sự kiện cùng Id nếu đã tồn tại
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (activeEvents[i].Id == notice.Id)
                    activeEvents.RemoveAt(i);
            }

            activeEvents.Add(new ActiveEvent
            {
                Id = notice.Id,
                DisplayName = string.IsNullOrEmpty(notice.DisplayName) ? notice.Id.ToUpperInvariant() : notice.DisplayName,
                HasTeam = notice.HasTeam,
                Team = notice.Team,
                HasCountdown = !isImpact && notice.Duration > 0f,
                StartTime = now,
                EndTime = now + duration,
                Priority = GetPriority(notice.Id)
            });

            RefreshDisplayedEvent(force: true);
        }

        void OnMatchEventEnded(MatchEventNotice notice)
        {
            if (string.IsNullOrEmpty(notice.Id)) return;

            bool changed = false;
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (activeEvents[i].Id == notice.Id)
                {
                    activeEvents.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RefreshDisplayedEvent(force: true);
            }
        }

        void OnRoundReset()
        {
            ClearAll();
        }

        public void ClearAll()
        {
            activeEvents.Clear();
            HideImmediate();
        }

        void HideImmediate()
        {
            displayedEventKey = null;
            lastDisplayedSeconds = -1;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (animatedRect != null && basePosCaptured)
            {
                animatedRect.anchoredPosition = baseAnchoredPos;
                animatedRect.localScale = Vector3.one;
            }
            if (bannerRoot != null && bannerRoot != gameObject)
            {
                bannerRoot.SetActive(false);
            }
        }

        void Update()
        {
            if (activeEvents.Count == 0) return;

            float now = Time.time;
            bool removed = false;
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (now >= activeEvents[i].EndTime)
                {
                    activeEvents.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                RefreshDisplayedEvent(force: true);
                if (activeEvents.Count == 0) return;
            }

            int bestIdx = GetHighestPriorityIndex();
            if (bestIdx < 0) return;

            ActiveEvent ev = activeEvents[bestIdx];
            UpdateCountdown(ev, now);
            UpdateAnimation(ev, now);
        }

        void RefreshDisplayedEvent(bool force)
        {
            int bestIdx = GetHighestPriorityIndex();
            if (bestIdx < 0)
            {
                HideImmediate();
                return;
            }

            ActiveEvent ev = activeEvents[bestIdx];
            string key = ev.Id + (ev.HasTeam ? (ev.Team == Team.Blue ? "_B" : "_R") : "");
            if (!force && key == displayedEventKey) return;

            displayedEventKey = key;
            lastDisplayedSeconds = -1;

            if (bannerRoot != null && bannerRoot != gameObject)
            {
                bannerRoot.SetActive(true);
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (titleText != null)
            {
                titleText.text = ev.DisplayName;
            }

            ApplyStaticStyle(ev);
            UpdateCountdown(ev, Time.time);
        }

        void ApplyStaticStyle(ActiveEvent ev)
        {
            Color accent;
            Color bg;
            Color titleColor;

            switch (ev.Id)
            {
                case "x2":
                    accent = GoldColor;
                    bg = GoldBgColor;
                    titleColor = GoldColor;
                    break;
                case "x2_weak":
                    accent = ev.HasTeam ? ev.Team.TeamColor() : GoldColor;
                    bg = ev.HasTeam && ev.Team == Team.Blue ? BlueBgColor : RedBgColor;
                    titleColor = Color.white;
                    break;
                case "disaster_warning":
                case "disaster":
                    accent = UrgentRedColor;
                    bg = UrgentBgColor;
                    titleColor = Color.white;
                    break;
                default:
                    accent = GoldColor;
                    bg = GoldBgColor;
                    titleColor = Color.white;
                    break;
            }

            if (borderImage != null) borderImage.color = accent;
            if (backgroundImage != null) backgroundImage.color = bg;
            if (titleText != null) titleText.color = titleColor;
            if (countdownText != null)
            {
                countdownText.color = ev.Id == "x2" ? Color.white : accent;
                countdownText.gameObject.SetActive(ev.HasCountdown);
            }
        }

        void UpdateCountdown(ActiveEvent ev, float now)
        {
            if (countdownText == null || !ev.HasCountdown) return;

            int seconds = Mathf.Max(0, Mathf.CeilToInt(ev.EndTime - now));
            if (seconds != lastDisplayedSeconds)
            {
                lastDisplayedSeconds = seconds;
                countdownText.text = $"{seconds}s";
            }
        }

        void UpdateAnimation(ActiveEvent ev, float now)
        {
            float elapsed = now - ev.StartTime;
            float remaining = ev.EndTime - now;

            float scale = 1f;
            float alpha = 1f;
            Vector2 offset = Vector2.zero;

            if (ev.Id == "disaster_warning")
            {
                // Đỏ khẩn cấp nhấp nháy nhanh + rung nhẹ
                float flash = 0.5f + 0.5f * Mathf.Sin(elapsed * 14f);
                if (borderImage != null)
                    borderImage.color = Color.Lerp(UrgentRedColor, UrgentFlashColor, flash);
                if (backgroundImage != null)
                    backgroundImage.color = Color.Lerp(UrgentBgColor, new Color(0.55f, 0.05f, 0.05f, 0.95f), flash * 0.65f);

                scale = 1f + 0.045f * Mathf.Sin(elapsed * 14f);
                offset = new Vector2(Mathf.Sin(elapsed * 42f) * 4f, Mathf.Cos(elapsed * 37f) * 2f);
            }
            else if (ev.Id == "disaster")
            {
                // Banner tác động mạnh mẽ trong 2.5 giây
                float punchT = Mathf.Clamp01(elapsed / 0.25f);
                scale = Mathf.Lerp(1.25f, 1f, 1f - (1f - punchT) * (1f - punchT));

                float flash = 0.5f + 0.5f * Mathf.Sin(elapsed * 16f);
                if (borderImage != null)
                    borderImage.color = Color.Lerp(UrgentRedColor, GoldColor, flash * 0.5f);

                if (elapsed < 0.45f)
                {
                    float mag = (1f - elapsed / 0.45f) * 6f;
                    offset = new Vector2(Mathf.Sin(elapsed * 55f) * mag, Mathf.Cos(elapsed * 47f) * mag * 0.5f);
                }

                if (remaining < 0.4f)
                {
                    alpha = Mathf.Clamp01(remaining / 0.4f);
                }
            }
            else if (ev.Id == "x2")
            {
                // Vàng rực đập nhịp nhẹ
                scale = 1f + 0.04f * Mathf.Sin(elapsed * 6f);
                if (remaining < 0.35f) alpha = Mathf.Clamp01(remaining / 0.35f);
            }
            else
            {
                // x2_weak hoặc sự kiện khác
                scale = 1f + 0.03f * Mathf.Sin(elapsed * 5f);
                if (remaining < 0.35f) alpha = Mathf.Clamp01(remaining / 0.35f);
            }

            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (animatedRect != null && basePosCaptured)
            {
                animatedRect.anchoredPosition = baseAnchoredPos + offset;
                animatedRect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        int GetHighestPriorityIndex()
        {
            int best = -1;
            int bestPriority = int.MinValue;
            for (int i = 0; i < activeEvents.Count; i++)
            {
                if (activeEvents[i].Priority > bestPriority)
                {
                    bestPriority = activeEvents[i].Priority;
                    best = i;
                }
            }
            return best;
        }

        static int GetPriority(string id)
        {
            switch (id)
            {
                case "disaster_warning": return 40;
                case "disaster": return 30;
                case "x2_weak": return 20;
                case "x2": return 10;
                default: return 5;
            }
        }
    }
}
