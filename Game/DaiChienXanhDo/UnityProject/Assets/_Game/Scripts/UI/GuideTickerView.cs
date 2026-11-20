using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-4.1: Dải hướng dẫn ở đáy khung vuông (chỉ hiện ở chế độ Live tự động).
    /// Chạy lần lượt các dòng trong GameSettings.Current.guideLines mỗi 6 giây với hiệu ứng mờ/trượt.
    /// Tự làm mới ngay khi SettingsMenuView lưu cài đặt.
    /// </summary>
    public class GuideTickerView : MonoBehaviour
    {
        [Header("References")]
        public GameObject barRoot;
        public CanvasGroup barGroup;
        public CanvasGroup textGroup;
        public RectTransform textRect;
        public TMP_Text guideText;

        [Header("Timing & Animation")]
        public float cycleSeconds = 6f;
        public float transitionDuration = 0.45f;
        public float slideOffsetY = 14f;

        readonly List<string> activeLines = new List<string>(8);
        Vector2 baseTextPos;
        bool basePosCaptured;

        int currentIndex;
        float timer;
        bool transitioning;
        float transitionElapsed;
        bool swappedDuringTransition;

        void Awake()
        {
            CaptureBasePosition();
        }

        void OnEnable()
        {
            CaptureBasePosition();
            SettingsMenuView.SettingsSaved += OnSettingsSaved;
            RefreshLines(resetIndex: false);
        }

        void OnDisable()
        {
            SettingsMenuView.SettingsSaved -= OnSettingsSaved;
        }

        void CaptureBasePosition()
        {
            if (basePosCaptured || textRect == null) return;
            baseTextPos = textRect.anchoredPosition;
            basePosCaptured = true;
        }

        void OnSettingsSaved()
        {
            RefreshLines(resetIndex: true);
        }

        /// <summary>Đọc lại danh sách guideLines từ GameSettings.Current và cập nhật hiển thị ngay.</summary>
        public void RefreshLines(bool resetIndex = true)
        {
            activeLines.Clear();
            var rawLines = GameSettings.Current != null ? GameSettings.Current.guideLines : null;
            if (rawLines != null)
            {
                for (int i = 0; i < rawLines.Length; i++)
                {
                    string line = rawLines[i];
                    if (!string.IsNullOrWhiteSpace(line))
                        activeLines.Add(line.Trim());
                }
            }

            if (activeLines.Count == 0)
            {
                SetBarVisible(false);
                transitioning = false;
                timer = 0f;
                return;
            }

            SetBarVisible(true);

            if (resetIndex || currentIndex >= activeLines.Count)
                currentIndex = 0;

            transitioning = false;
            transitionElapsed = 0f;
            swappedDuringTransition = false;
            timer = 0f;

            ApplyLineImmediate(activeLines[currentIndex]);
        }

        void Update()
        {
            if (activeLines.Count <= 1) return;

            float dt = Time.deltaTime;
            float interval = Mathf.Max(1f, cycleSeconds);
            float transDur = Mathf.Clamp(transitionDuration, 0.1f, interval * 0.5f);

            if (!transitioning)
            {
                timer += dt;
                if (timer >= interval - transDur)
                {
                    transitioning = true;
                    transitionElapsed = 0f;
                    swappedDuringTransition = false;
                }
                return;
            }

            transitionElapsed += dt;
            float half = transDur * 0.5f;

            if (transitionElapsed < half)
            {
                // Pha 1: mờ dần và trượt nhẹ lên trên
                float t = Mathf.Clamp01(transitionElapsed / half);
                float ease = t * t;
                SetTextVisual(1f - ease, slideOffsetY * ease);
            }
            else
            {
                // Đổi sang dòng kế tiếp ở giữa hiệu ứng
                if (!swappedDuringTransition)
                {
                    swappedDuringTransition = true;
                    currentIndex = (currentIndex + 1) % activeLines.Count;
                    if (guideText != null) guideText.text = activeLines[currentIndex];
                }

                // Pha 2: hiện dần từ dưới trượt lên vị trí gốc
                float t = Mathf.Clamp01((transitionElapsed - half) / half);
                float easeOut = 1f - (1f - t) * (1f - t);
                SetTextVisual(easeOut, -slideOffsetY * (1f - easeOut));

                if (transitionElapsed >= transDur)
                {
                    transitioning = false;
                    timer = 0f;
                    SetTextVisual(1f, 0f);
                }
            }
        }

        void ApplyLineImmediate(string text)
        {
            if (guideText != null) guideText.text = text;
            SetTextVisual(1f, 0f);
        }

        void SetTextVisual(float alpha, float offsetY)
        {
            if (textGroup != null) textGroup.alpha = alpha;
            if (textRect != null && basePosCaptured)
            {
                textRect.anchoredPosition = new Vector2(baseTextPos.x, baseTextPos.y + offsetY);
            }
        }

        void SetBarVisible(bool visible)
        {
            if (barRoot != null && barRoot != gameObject)
            {
                barRoot.SetActive(visible);
            }
            if (barGroup != null)
            {
                barGroup.alpha = visible ? 1f : 0f;
                barGroup.interactable = false;
                barGroup.blocksRaycasts = false;
            }
        }
    }
}
