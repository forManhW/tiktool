using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-4.3 & UI-6: Banner quà lớn (Tier >= 4: lightning, tank, meteor, giant_robot).
    /// Xếp hàng phát lần lượt mỗi banner 2.5 giây với hiệu ứng trượt vào / mờ ra.
    /// Tự tra icon theo EffectId (từ mảng serialized hoặc fallback Resources/UI/Icons/icon_&lt;id&gt;),
    /// nếu thiếu icon thì ẩn khung icon nhẹ nhàng không báo lỗi.
    /// </summary>
    public class GiftBannerView : MonoBehaviour
    {
        [Serializable]
        public struct EffectIconEntry
        {
            public string effectId;
            public Sprite sprite;
        }

        const int MinBannerTier = 4;
        const int MaxQueueSize = 24;

        static readonly Color BlueBgTint = new Color32(0x0B, 0x1E, 0x3D, 0xEA);
        static readonly Color RedBgTint = new Color32(0x3B, 0x0E, 0x14, 0xEA);

        [Header("Banner References")]
        public GameObject bannerRoot;
        public RectTransform animatedRect;
        public CanvasGroup canvasGroup;
        public Image frameImage;
        public Image backgroundImage;
        public Image accentGlowImage;
        public GameObject iconContainer;
        public Image iconImage;
        public TMP_Text senderText;
        public TMP_Text effectText;
        public TMP_Text teamBadgeText;

        [Header("UI-6 Effect Icons (wired by UISetupBuilder)")]
        public EffectIconEntry[] icons;

        [Header("Timing & Animation")]
        public float displayDuration = 2.5f;
        public float slideInDuration = 0.28f;
        public float fadeOutDuration = 0.32f;
        public float slideOffsetX = 140f;

        readonly Queue<EffectNotice> pendingQueue = new Queue<EffectNotice>(8);
        readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>(16, StringComparer.OrdinalIgnoreCase);

        Vector2 baseAnchoredPos;
        bool basePosCaptured;
        bool iconMapBuilt;
        bool isShowing;
        float elapsed;

        void Awake()
        {
            CaptureBasePosition();
            BuildIconLookup();
            HideImmediate();
        }

        void OnEnable()
        {
            CaptureBasePosition();
            BuildIconLookup();
            GameEvents.EffectStarted += OnEffectStarted;
            GameEvents.RoundReset += OnRoundReset;
        }

        void OnDisable()
        {
            GameEvents.EffectStarted -= OnEffectStarted;
            GameEvents.RoundReset -= OnRoundReset;
            ClearQueueAndHide();
        }

        void CaptureBasePosition()
        {
            if (basePosCaptured || animatedRect == null) return;
            baseAnchoredPos = animatedRect.anchoredPosition;
            basePosCaptured = true;
        }

        void BuildIconLookup()
        {
            if (iconMapBuilt) return;
            iconMapBuilt = true;
            iconCache.Clear();

            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                string id = icons[i].effectId;
                if (!string.IsNullOrEmpty(id) && icons[i].sprite != null)
                {
                    iconCache[id] = icons[i].sprite;
                }
            }
        }

        void OnEffectStarted(EffectNotice notice)
        {
            if (notice.Tier < MinBannerTier) return;

            if (pendingQueue.Count >= MaxQueueSize)
            {
                pendingQueue.Dequeue();
            }

            pendingQueue.Enqueue(notice);

            if (!isShowing)
            {
                ShowNext();
            }
        }

        void OnRoundReset()
        {
            ClearQueueAndHide();
        }

        public void ClearQueueAndHide()
        {
            pendingQueue.Clear();
            HideImmediate();
        }

        void HideImmediate()
        {
            isShowing = false;
            elapsed = 0f;
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

        void ShowNext()
        {
            if (pendingQueue.Count == 0)
            {
                HideImmediate();
                return;
            }

            EffectNotice notice = pendingQueue.Dequeue();
            isShowing = true;
            elapsed = 0f;

            if (bannerRoot != null && bannerRoot != gameObject)
            {
                bannerRoot.SetActive(true);
            }

            Color teamColor = notice.Team.TeamColor();
            if (frameImage != null) frameImage.color = teamColor;
            if (accentGlowImage != null) accentGlowImage.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.38f);
            if (backgroundImage != null) backgroundImage.color = notice.Team == Team.Blue ? BlueBgTint : RedBgTint;

            if (teamBadgeText != null)
            {
                teamBadgeText.text = $"PHE {notice.Team.DisplayName()}";
                teamBadgeText.color = teamColor;
            }

            string senderName = (notice.By != null && !string.IsNullOrEmpty(notice.By.Nickname))
                ? notice.By.Nickname
                : "Người xem";
            if (senderText != null)
            {
                senderText.text = senderName;
            }

            string effectLabel = !string.IsNullOrEmpty(notice.DisplayName) ? notice.DisplayName : notice.EffectId;
            if (notice.Amount > 1)
            {
                effectLabel = $"{effectLabel} x{notice.Amount}";
            }
            if (effectText != null)
            {
                effectText.text = effectLabel;
            }

            Sprite resolvedIcon = ResolveIcon(notice.EffectId);
            bool hasIcon = resolvedIcon != null;
            if (iconImage != null)
            {
                iconImage.sprite = resolvedIcon;
                iconImage.preserveAspect = true;
                iconImage.gameObject.SetActive(hasIcon);
            }
            if (iconContainer != null)
            {
                iconContainer.SetActive(hasIcon);
            }

            ApplyAnimation(0f);
        }

        Sprite ResolveIcon(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;

            BuildIconLookup();

            if (iconCache.TryGetValue(effectId, out Sprite cached))
            {
                return cached;
            }

            // Fallback đọc từ Resources/UI/Icons/icon_<effectId> nếu có
            Sprite fallback = Resources.Load<Sprite>($"UI/Icons/icon_{effectId}");
            iconCache[effectId] = fallback;
            return fallback;
        }

        void Update()
        {
            if (!isShowing) return;

            elapsed += Time.deltaTime;
            float total = Mathf.Max(0.8f, displayDuration);

            if (elapsed >= total)
            {
                ShowNext();
                return;
            }

            ApplyAnimation(elapsed);
        }

        void ApplyAnimation(float t)
        {
            float total = Mathf.Max(0.8f, displayDuration);
            float inDur = Mathf.Clamp(slideInDuration, 0.05f, total * 0.4f);
            float outDur = Mathf.Clamp(fadeOutDuration, 0.05f, total * 0.4f);

            float alpha = 1f;
            float offsetX = 0f;
            float scale = 1f;

            if (t < inDur)
            {
                float p = Mathf.Clamp01(t / inDur);
                float easeOut = 1f - (1f - p) * (1f - p) * (1f - p);
                alpha = p;
                offsetX = -slideOffsetX * (1f - easeOut);
                // Nhún nhẹ khi vừa xuất hiện
                float overshoot = Mathf.Sin(p * Mathf.PI) * 0.06f;
                scale = Mathf.Lerp(0.88f, 1f, easeOut) + overshoot;
            }
            else if (t > total - outDur)
            {
                float p = Mathf.Clamp01((t - (total - outDur)) / outDur);
                float easeIn = p * p;
                alpha = 1f - easeIn;
                offsetX = slideOffsetX * 0.3f * easeIn;
                scale = Mathf.Lerp(1f, 0.96f, easeIn);
            }

            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (animatedRect != null && basePosCaptured)
            {
                animatedRect.anchoredPosition = new Vector2(baseAnchoredPos.x + offsetX, baseAnchoredPos.y);
                animatedRect.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
