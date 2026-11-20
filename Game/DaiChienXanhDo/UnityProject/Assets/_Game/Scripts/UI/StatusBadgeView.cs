using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-3.4: Chấm trạng thái nhỏ góc dưới bên phải (chế độ + kết nối + FPS + số lính),
    /// mờ nhẹ để không gây chú ý, luôn hiện ở cả 2 chế độ.
    /// </summary>
    public class StatusBadgeView : MonoBehaviour
    {
        [Header("Widgets")]
        public CanvasGroup badgeGroup;
        public Image backdropImage;
        public Image statusDotImage;
        public TMP_Text statusText;

        [Header("Appearance & Refresh")]
        [Range(0.2f, 1f)]
        public float idleAlpha = 0.72f;
        public float refreshInterval = 0.5f;

        readonly StringBuilder sb = new StringBuilder(96);
        GameMode currentMode = GameMode.LiveAuto;
        string currentConnection = "demo";
        int currentFighters;
        float nextRefreshTime;
        bool dirty = true;

        void Awake()
        {
            UIStyleHelper.EnsureRoundedSprite(backdropImage, 12, 48);
            UIStyleHelper.EnsureCircleSprite(statusDotImage);

            if (badgeGroup != null)
            {
                badgeGroup.alpha = idleAlpha;
                badgeGroup.blocksRaycasts = false;
                badgeGroup.interactable = false;
            }
        }

        void OnEnable()
        {
            GameEvents.ModeChanged += OnModeChanged;
            GameEvents.ConnectionStatusChanged += OnConnectionStatusChanged;
            GameEvents.FighterCountChanged += OnFighterCountChanged;
        }

        void OnDisable()
        {
            GameEvents.ModeChanged -= OnModeChanged;
            GameEvents.ConnectionStatusChanged -= OnConnectionStatusChanged;
            GameEvents.FighterCountChanged -= OnFighterCountChanged;
        }

        void Start()
        {
            currentMode = GameState.Mode;
            currentConnection = GameState.ConnectionStatus ?? "demo";
            currentFighters = FighterManager.I != null ? FighterManager.I.Total : 0;
            UpdateDotColor();
            RebuildText();
            nextRefreshTime = Time.unscaledTime + refreshInterval;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (!dirty && now < nextRefreshTime) return;

            nextRefreshTime = now + refreshInterval;
            dirty = false;
            if (FighterManager.I != null) currentFighters = FighterManager.I.Total;
            RebuildText();
        }

        void OnModeChanged(GameMode mode)
        {
            currentMode = mode;
            dirty = true;
        }

        void OnConnectionStatusChanged(string status)
        {
            currentConnection = string.IsNullOrEmpty(status) ? "demo" : status;
            UpdateDotColor();
            dirty = true;
        }

        void OnFighterCountChanged(int blue, int red)
        {
            currentFighters = blue + red;
            dirty = true;
        }

        void UpdateDotColor()
        {
            if (statusDotImage != null)
            {
                statusDotImage.color = UIStyleHelper.GetConnectionStatusColor(currentConnection);
            }
        }

        void RebuildText()
        {
            if (statusText == null) return;

            int fps = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime));
            sb.Clear();
            sb.Append(currentMode == GameMode.LiveAuto ? "LIVE TỰ ĐỘNG" : "THƯỜNG");
            sb.Append(" · ");
            sb.Append(currentConnection);
            sb.Append(" · ");
            sb.Append(fps);
            sb.Append(" FPS · ");
            sb.Append(currentFighters);
            sb.Append(" lính");

            statusText.SetText(sb);
        }
    }
}
