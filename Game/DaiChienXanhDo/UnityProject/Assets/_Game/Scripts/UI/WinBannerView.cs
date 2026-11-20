using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-4.5: Banner chiến thắng đặt giữa màn hình (chỉ hiện ở Live tự động).
    /// Hiện "PHE XANH THẮNG!" hoặc "PHE ĐỎ THẮNG!" kèm "MVP: &lt;tên&gt;" (ẩn dòng MVP nếu null).
    /// Duy trì trong GameSettings.Current.celebrateSeconds giây hoặc đến khi RoundReset.
    /// </summary>
    public class WinBannerView : MonoBehaviour
    {
        static readonly Color BlueBgColor = new Color32(0x0A, 0x1E, 0x42, 0xF0);
        static readonly Color RedBgColor = new Color32(0x42, 0x0B, 0x10, 0xF0);
        static readonly Color MvpGoldColor = new Color32(0xFF, 0xE0, 0x66, 0xFF);

        [Header("Banner References")]
        public GameObject bannerRoot;
        public RectTransform animatedRect;
        public CanvasGroup canvasGroup;
        public Image backgroundImage;
        public Image borderImage;
        public Image accentRibbonImage;
        public TMP_Text titleText;
        public GameObject mvpContainer;
        public TMP_Text mvpText;

        [Header("Animation")]
        public float scaleInDuration = 0.42f;
        public float fadeOutDuration = 0.35f;

        bool isShowing;
        float elapsed;
        float duration;

        void Awake()
        {
            HideImmediate();
        }

        void OnEnable()
        {
            GameEvents.RoundWon += OnRoundWon;
            GameEvents.RoundReset += OnRoundReset;
        }

        void OnDisable()
        {
            GameEvents.RoundWon -= OnRoundWon;
            GameEvents.RoundReset -= OnRoundReset;
            HideImmediate();
        }

        void OnRoundWon(Team winner, Viewer mvp)
        {
            Color teamColor = winner.TeamColor();

            if (titleText != null)
            {
                titleText.text = $"PHE {winner.DisplayName()} THẮNG!";
                titleText.color = Color.white;
            }

            bool hasMvp = mvp != null && !string.IsNullOrEmpty(mvp.Nickname);
            if (mvpText != null)
            {
                mvpText.text = hasMvp ? $"MVP: {mvp.Nickname}" : string.Empty;
                mvpText.color = MvpGoldColor;
                if (mvpContainer == null)
                    mvpText.gameObject.SetActive(hasMvp);
            }
            if (mvpContainer != null)
            {
                mvpContainer.SetActive(hasMvp);
            }

            if (borderImage != null) borderImage.color = teamColor;
            if (accentRibbonImage != null) accentRibbonImage.color = teamColor;
            if (backgroundImage != null) backgroundImage.color = winner == Team.Blue ? BlueBgColor : RedBgColor;

            duration = GameSettings.Current != null && GameSettings.Current.celebrateSeconds > 0f
                ? GameSettings.Current.celebrateSeconds
                : 7f;
            elapsed = 0f;
            isShowing = true;

            if (bannerRoot != null && bannerRoot != gameObject)
            {
                bannerRoot.SetActive(true);
            }

            ApplyAnimation(0f);
        }

        void OnRoundReset()
        {
            HideImmediate();
        }

        public void HideImmediate()
        {
            isShowing = false;
            elapsed = 0f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (animatedRect != null) animatedRect.localScale = Vector3.one;
            if (bannerRoot != null && bannerRoot != gameObject)
            {
                bannerRoot.SetActive(false);
            }
        }

        void Update()
        {
            if (!isShowing) return;

            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                HideImmediate();
                return;
            }

            ApplyAnimation(elapsed);
        }

        void ApplyAnimation(float t)
        {
            float inDur = Mathf.Clamp(scaleInDuration, 0.1f, duration * 0.4f);
            float outDur = Mathf.Clamp(fadeOutDuration, 0.05f, duration * 0.4f);

            float alpha = 1f;
            float scale = 1f;

            if (t < inDur)
            {
                float p = Mathf.Clamp01(t / inDur);
                // EaseOutBack phóng to ăn mừng
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float pm1 = p - 1f;
                float easeBack = 1f + c3 * pm1 * pm1 * pm1 + c1 * pm1 * pm1;
                scale = Mathf.LerpUnclamped(0.55f, 1f, easeBack);
                alpha = Mathf.Clamp01(p * 2f);
            }
            else if (t > duration - outDur)
            {
                float p = Mathf.Clamp01((t - (duration - outDur)) / outDur);
                alpha = 1f - p * p;
                scale = Mathf.Lerp(1f, 0.94f, p);
            }
            else
            {
                // Nhịp thở nhẹ trong lúc ăn mừng
                scale = 1f + 0.02f * Mathf.Sin((t - inDur) * 4f);
            }

            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (animatedRect != null) animatedRect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
