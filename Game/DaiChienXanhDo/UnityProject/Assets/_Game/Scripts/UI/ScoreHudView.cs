using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-3.1: Tỉ số góc trên bên trái ("XANH &lt;blue&gt; : &lt;red&gt; ĐỎ"), luôn hiện ở cả 2 chế độ.
    /// Có hiệu ứng nảy mượt (punch-scale) khi đổi điểm và hiển thị số lính mỗi phe gọn gàng.
    /// </summary>
    public class ScoreHudView : MonoBehaviour
    {
        [Header("Backdrop")]
        public RectTransform panelRoot;
        public Image backdropImage;
        public Image borderImage;

        [Header("Score Texts")]
        public TMP_Text blueLabelText;
        public TMP_Text blueScoreText;
        public TMP_Text separatorText;
        public TMP_Text redScoreText;
        public TMP_Text redLabelText;
        [Tooltip("Tùy chọn: nếu chỉ dùng 1 TMP_Text rich-text thay vì tách riêng từng chữ số.")]
        public TMP_Text combinedScoreText;

        [Header("Fighter Count Pills (Optional)")]
        public GameObject fighterCountRow;
        public Image blueCountPill;
        public TMP_Text blueCountText;
        public Image redCountPill;
        public TMP_Text redCountText;

        [Header("Punch Animation")]
        public float punchScale = 1.38f;
        public float punchDuration = 0.36f;

        int lastBlue = -1;
        int lastRed = -1;
        int lastBlueCount = -1;
        int lastRedCount = -1;

        float bluePunchTimer;
        float redPunchTimer;
        float panelPunchTimer;

        void Awake()
        {
            UIStyleHelper.EnsureRoundedSprite(backdropImage, 18, 64);
            UIStyleHelper.EnsureRoundedOutlineSprite(borderImage, 18, 2, 64);
            UIStyleHelper.EnsureRoundedSprite(blueCountPill, 12, 48);
            UIStyleHelper.EnsureRoundedSprite(redCountPill, 12, 48);

            if (blueLabelText != null) blueLabelText.color = UIStyleHelper.BlueColor;
            if (blueScoreText != null) blueScoreText.color = UIStyleHelper.BlueColor;
            if (redLabelText != null) redLabelText.color = UIStyleHelper.RedColor;
            if (redScoreText != null) redScoreText.color = UIStyleHelper.RedColor;
        }

        void OnEnable()
        {
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.FighterCountChanged += OnFighterCountChanged;
        }

        void OnDisable()
        {
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.FighterCountChanged -= OnFighterCountChanged;
        }

        void Start()
        {
            if (MatchManager.I != null)
            {
                ApplyScore(MatchManager.I.ScoreBlue, MatchManager.I.ScoreRed, animate: false);
            }
            else
            {
                ApplyScore(0, 0, animate: false);
            }

            if (FighterManager.I != null)
            {
                ApplyFighterCount(FighterManager.I.Count(Team.Blue), FighterManager.I.Count(Team.Red));
            }
            else
            {
                ApplyFighterCount(0, 0);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (bluePunchTimer > 0f)
            {
                bluePunchTimer = Mathf.Max(0f, bluePunchTimer - dt);
                ApplyPunchTransform(blueScoreText != null ? blueScoreText.rectTransform : null, bluePunchTimer, punchScale);
            }

            if (redPunchTimer > 0f)
            {
                redPunchTimer = Mathf.Max(0f, redPunchTimer - dt);
                ApplyPunchTransform(redScoreText != null ? redScoreText.rectTransform : null, redPunchTimer, punchScale);
            }

            if (panelPunchTimer > 0f)
            {
                panelPunchTimer = Mathf.Max(0f, panelPunchTimer - dt);
                ApplyPunchTransform(panelRoot, panelPunchTimer, 1.06f);
            }
        }

        void OnScoreChanged(int blue, int red)
        {
            ApplyScore(blue, red, animate: true);
        }

        void OnFighterCountChanged(int blue, int red)
        {
            ApplyFighterCount(blue, red);
        }

        void ApplyScore(int blue, int red, bool animate)
        {
            bool blueChanged = lastBlue >= 0 && blue != lastBlue;
            bool redChanged = lastRed >= 0 && red != lastRed;

            lastBlue = blue;
            lastRed = red;

            if (blueScoreText != null) blueScoreText.SetText("{0:0}", (float)blue);
            if (redScoreText != null) redScoreText.SetText("{0:0}", (float)red);

            if (combinedScoreText != null)
            {
                combinedScoreText.text = $"<color=#2D7DFF>XANH {blue}</color> : <color=#FF3B3B>{red} ĐỎ</color>";
            }

            if (!animate) return;

            if (blueChanged) bluePunchTimer = punchDuration;
            if (redChanged) redPunchTimer = punchDuration;
            if (blueChanged || redChanged) panelPunchTimer = punchDuration * 0.85f;
        }

        void ApplyFighterCount(int blue, int red)
        {
            if (blue != lastBlueCount)
            {
                lastBlueCount = blue;
                if (blueCountText != null) blueCountText.SetText("{0:0}", (float)blue);
            }

            if (red != lastRedCount)
            {
                lastRedCount = red;
                if (redCountText != null) redCountText.SetText("{0:0}", (float)red);
            }
        }

        void ApplyPunchTransform(RectTransform target, float remaining, float peakScale)
        {
            if (target == null) return;
            if (remaining <= 0f || punchDuration <= 0.0001f)
            {
                target.localScale = Vector3.one;
                return;
            }

            float t = 1f - Mathf.Clamp01(remaining / punchDuration);
            // Nhún nhanh lên đỉnh rồi hồi mượt về 1
            float bump = Mathf.Sin(t * Mathf.PI) * (1f - 0.35f * t);
            float s = 1f + (peakScale - 1f) * bump;
            target.localScale = new Vector3(s, s, 1f);
        }
    }
}
