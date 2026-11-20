using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-3.2: Thanh máu 2 lõi (Xanh / Đỏ) nổi trên đầu mỗi lõi theo tọa độ thế giới → màn hình.
    /// Có thanh trượt máu mượt, thanh đuôi trắng khi mất máu, nháy sáng khi trúng đòn,
    /// và rung + nhấp nháy đỏ cảnh báo liên tục khi máu dưới 20%.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CoreHealthBarsView : MonoBehaviour
    {
        [Serializable]
        public class CoreBarWidgets
        {
            public RectTransform root;
            public RectTransform shakeTarget;
            public Image backgroundImage;
            public Image borderImage;
            public Image trailFillImage;
            public Image mainFillImage;
            public Image hitFlashOverlay;
            public TMP_Text hpText;
            public TMP_Text teamTagText;
        }

        [Header("Canvas & Positioning")]
        public RectTransform containerRect;
        public Canvas parentCanvas;
        public Vector3 worldOffset = new Vector3(0f, 4.5f, 0f);
        public float offscreenMargin = 90f;

        [Header("Core Bar Widgets")]
        public CoreBarWidgets blueBar = new CoreBarWidgets();
        public CoreBarWidgets redBar = new CoreBarWidgets();

        [Header("Animation Tuning")]
        public float fillLerpSpeed = 16f;
        public float trailHoldSeconds = 0.25f;
        public float trailDrainSpeed = 1.2f;
        public float hitFlashDuration = 0.16f;
        public float lowHpThreshold = 0.2f;
        public float lowHpShakeAmplitude = 4.5f;
        public float lowHpPulseSpeed = 11f;

        struct BarState
        {
            public float hp;
            public float maxHp;
            public float targetRatio;
            public float displayRatio;
            public float trailRatio;
            public float trailHoldTimer;
            public float hitFlashTimer;
            public int lastDisplayedHp;
            public int lastDisplayedMax;
            public bool initialized;
        }

        readonly BarState[] states = new BarState[2];
        readonly CoreBarWidgets[] bars = new CoreBarWidgets[2];

        void Awake()
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            if (containerRect == null) containerRect = transform as RectTransform;

            bars[(int)Team.Blue] = blueBar;
            bars[(int)Team.Red] = redBar;

            SetupBarVisuals(blueBar, Team.Blue);
            SetupBarVisuals(redBar, Team.Red);
        }

        void SetupBarVisuals(CoreBarWidgets w, Team team)
        {
            if (w == null) return;
            UIStyleHelper.EnsureRoundedSprite(w.backgroundImage, 10, 48);
            UIStyleHelper.EnsureRoundedOutlineSprite(w.borderImage, 10, 2, 48);
            UIStyleHelper.EnsureHorizontalFill(w.trailFillImage);
            UIStyleHelper.EnsureHorizontalFill(w.mainFillImage);

            if (w.trailFillImage != null) w.trailFillImage.color = new Color(1f, 1f, 1f, 0.85f);
            if (w.mainFillImage != null) w.mainFillImage.color = UIStyleHelper.GetTeamColor(team);
            if (w.hitFlashOverlay != null)
            {
                UIStyleHelper.EnsureRoundedSprite(w.hitFlashOverlay, 10, 48);
                w.hitFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        void OnEnable()
        {
            GameEvents.CoreHealthChanged += OnCoreHealthChanged;
            GameEvents.RoundReset += OnRoundReset;
        }

        void OnDisable()
        {
            GameEvents.CoreHealthChanged -= OnCoreHealthChanged;
            GameEvents.RoundReset -= OnRoundReset;
        }

        void Start()
        {
            SyncInitial(Team.Blue);
            SyncInitial(Team.Red);
        }

        void SyncInitial(Team team)
        {
            var match = MatchManager.I;
            CoreHealth core = match != null ? match.Core(team) : null;
            if (core != null && core.MaxHp > 0f)
            {
                ApplyHealth(team, core.Hp, core.MaxHp, instant: true);
            }
            else
            {
                float max = GameSettings.Current != null ? GameSettings.Current.coreMaxHp : 1000f;
                ApplyHealth(team, max, max, instant: true);
            }
        }

        void OnCoreHealthChanged(Team team, float hp, float max)
        {
            ApplyHealth(team, hp, max, instant: false);
        }

        void OnRoundReset()
        {
            SyncInitial(Team.Blue);
            SyncInitial(Team.Red);
        }

        void ApplyHealth(Team team, float hp, float max, bool instant)
        {
            int idx = (int)team;
            ref BarState s = ref states[idx];
            float safeMax = Mathf.Max(1f, max);
            float safeHp = Mathf.Clamp(hp, 0f, safeMax);
            float ratio = safeHp / safeMax;

            bool damaged = s.initialized && !instant && safeHp < s.hp - 0.001f;
            bool resetOrHealed = !s.initialized || instant || ratio > s.targetRatio + 0.001f;

            s.hp = safeHp;
            s.maxHp = safeMax;
            s.targetRatio = ratio;
            s.initialized = true;

            if (resetOrHealed)
            {
                s.displayRatio = ratio;
                s.trailRatio = ratio;
                s.trailHoldTimer = 0f;
                s.hitFlashTimer = 0f;
            }
            else if (damaged)
            {
                s.hitFlashTimer = hitFlashDuration;
                s.trailHoldTimer = trailHoldSeconds;
            }

            UpdateHpText(team, ref s);
        }

        void UpdateHpText(Team team, ref BarState s)
        {
            var w = bars[(int)team];
            if (w == null || w.hpText == null) return;

            int ceilHp = Mathf.CeilToInt(s.hp);
            int ceilMax = Mathf.CeilToInt(s.maxHp);
            if (ceilHp == s.lastDisplayedHp && ceilMax == s.lastDisplayedMax) return;

            s.lastDisplayedHp = ceilHp;
            s.lastDisplayedMax = ceilMax;
            w.hpText.SetText("{0:0} / {1:0}", (float)ceilHp, (float)ceilMax);
        }

        void LateUpdate()
        {
            Camera cam = CameraRig.I != null && CameraRig.I.cam != null ? CameraRig.I.cam : Camera.main;
            if (cam == null || containerRect == null) return;

            Camera uiCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? parentCanvas.worldCamera
                : null;

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            UpdateTeamBar(Team.Blue, cam, uiCam, dt, now);
            UpdateTeamBar(Team.Red, cam, uiCam, dt, now);
        }

        void UpdateTeamBar(Team team, Camera cam, Camera uiCam, float dt, float now)
        {
            int idx = (int)team;
            var w = bars[idx];
            if (w == null || w.root == null) return;

            var match = MatchManager.I;
            CoreHealth core = match != null ? match.Core(team) : null;
            if (core == null)
            {
                if (w.root.gameObject.activeSelf) w.root.gameObject.SetActive(false);
                return;
            }

            ref BarState s = ref states[idx];
            if (!s.initialized && core.MaxHp > 0f)
            {
                ApplyHealth(team, core.Hp, core.MaxHp, instant: true);
            }

            // World-to-screen -> Canvas local position
            Vector3 sp = cam.WorldToScreenPoint(core.transform.position + Vector3.up * CoreHealth.BarHeight);
            Rect px = cam.pixelRect;
            bool visible = sp.z > 0f; // lâu đài ra ngoài khung thì vẫn hiện, dính ở mép màn hình (đoạn kẹp vị trí bên dưới)

            if (!visible)
            {
                if (w.root.gameObject.activeSelf) w.root.gameObject.SetActive(false);
                return;
            }

            if (!w.root.gameObject.activeSelf) w.root.gameObject.SetActive(true);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, new Vector2(sp.x, sp.y), uiCam, out Vector2 localPt))
            {
                Rect bounds = containerRect.rect;
                float width = w.root.rect.width > 0f ? w.root.rect.width : 226f;
                float height = w.root.rect.height > 0f ? w.root.rect.height : 32f;
                Vector2 pivot = w.root.pivot;

                float minX = bounds.xMin + width * pivot.x + 12f;
                float maxX = bounds.xMax - width * (1f - pivot.x) - 12f;
                if (minX <= maxX) localPt.x = Mathf.Clamp(localPt.x, minX, maxX);

                float minY = bounds.yMin + height * pivot.y + 12f;
                float maxY = bounds.yMax - height * (1f - pivot.y) - 48f; // chừa chỗ cho nhãn "LÕI ĐỎ" phía trên thanh máu
                if (minY <= maxY) localPt.y = Mathf.Clamp(localPt.y, minY, maxY);

                w.root.anchoredPosition = localPt;
            }

            // Smooth fill & trailing white damage bar
            float k = 1f - Mathf.Exp(-fillLerpSpeed * dt);
            s.displayRatio = Mathf.Lerp(s.displayRatio, s.targetRatio, k);
            if (Mathf.Abs(s.displayRatio - s.targetRatio) < 0.0005f) s.displayRatio = s.targetRatio;

            if (s.trailHoldTimer > 0f)
            {
                s.trailHoldTimer = Mathf.Max(0f, s.trailHoldTimer - dt);
            }
            else if (s.trailRatio > s.targetRatio)
            {
                s.trailRatio = Mathf.MoveTowards(s.trailRatio, s.targetRatio, trailDrainSpeed * dt);
            }
            else
            {
                s.trailRatio = s.targetRatio;
            }

            if (w.mainFillImage != null) w.mainFillImage.fillAmount = s.displayRatio;
            if (w.trailFillImage != null) w.trailFillImage.fillAmount = s.trailRatio;

            // Hit flash
            float flashNorm = 0f;
            if (s.hitFlashTimer > 0f)
            {
                s.hitFlashTimer = Mathf.Max(0f, s.hitFlashTimer - dt);
                flashNorm = hitFlashDuration > 0.0001f ? Mathf.Clamp01(s.hitFlashTimer / hitFlashDuration) : 0f;
            }

            if (w.hitFlashOverlay != null)
            {
                var fc = w.hitFlashOverlay.color;
                fc.a = flashNorm * 0.72f;
                w.hitFlashOverlay.color = fc;
            }

            // Low-HP (<20% và >0) continuous shake & red-pulse warning
            bool lowHp = s.targetRatio > 0f && s.targetRatio < lowHpThreshold;
            Color baseTeamColor = UIStyleHelper.GetTeamColor(team);

            if (lowHp)
            {
                float urgency = Mathf.Clamp01(1f - s.targetRatio / lowHpThreshold);
                float pulse = 0.5f + 0.5f * Mathf.Sin(now * lowHpPulseSpeed);
                float amp = Mathf.Lerp(lowHpShakeAmplitude * 0.6f, lowHpShakeAmplitude, urgency);
                Vector2 shakeOffset = new Vector2(
                    Mathf.Sin(now * 67f + idx * 1.7f) * amp,
                    Mathf.Cos(now * 53f + idx * 2.3f) * amp * 0.7f);

                if (w.shakeTarget != null)
                {
                    w.shakeTarget.anchoredPosition = shakeOffset;
                }
                else
                {
                    w.root.anchoredPosition += shakeOffset;
                }

                if (w.borderImage != null)
                {
                    Color warn = UIStyleHelper.StatusErrorColor;
                    warn.a = Mathf.Lerp(0.45f, 1f, pulse);
                    w.borderImage.color = warn;
                }

                if (w.mainFillImage != null)
                {
                    Color c = Color.Lerp(baseTeamColor, UIStyleHelper.StatusErrorColor, pulse * 0.65f);
                    if (w.hitFlashOverlay == null && flashNorm > 0f) c = Color.Lerp(c, Color.white, flashNorm * 0.6f);
                    w.mainFillImage.color = c;
                }
            }
            else
            {
                if (w.shakeTarget != null && w.shakeTarget.anchoredPosition != Vector2.zero)
                {
                    w.shakeTarget.anchoredPosition = Vector2.zero;
                }

                if (w.borderImage != null)
                {
                    w.borderImage.color = UIStyleHelper.PanelBorderColor;
                }

                if (w.mainFillImage != null)
                {
                    w.mainFillImage.color = (w.hitFlashOverlay == null && flashNorm > 0f)
                        ? Color.Lerp(baseTeamColor, Color.white, flashNorm * 0.6f)
                        : baseTeamColor;
                }
            }
        }
    }
}
