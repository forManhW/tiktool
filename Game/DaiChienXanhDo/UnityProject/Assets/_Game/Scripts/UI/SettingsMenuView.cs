using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// UI-5: Menu cài đặt F10 (nằm NGOÀI liveModeRoot để mở được ở cả Live tự động và Thường).
    /// Đăng ký GameEvents.SettingsMenuRequested để bật/tắt modal (khi đã đăng ký, F10 không mở settings.json ngoài).
    /// Cho phép sửa đầy đủ 12 hạng mục của GameSettings.Current + GameState.Mode, lưu settings.json và
    /// phát sự kiện SettingsSaved để GuideTickerView cập nhật tức thì.
    /// </summary>
    public class SettingsMenuView : MonoBehaviour
    {
        public const string RequiredNoteText = "Máu lõi áp dụng từ ván sau, kích thước cửa sổ áp dụng khi mở lại game";

        static readonly int[] WindowSizeChoices = { 720, 1080, 1440 };

        /// <summary>Phát ra sau khi người dùng bấm Lưu cài đặt thành công.</summary>
        public static event Action SettingsSaved;

        class GuideLineRow
        {
            public GameObject Root;
            public TMP_InputField Input;
            public Button DeleteButton;
        }

        [Header("Modal Root (toggled on/off; keep this script's GameObject active)")]
        public GameObject modalRoot;
        public ScrollRect scrollRect;

        [Header("Numeric Settings Inputs")]
        public TMP_InputField coreMaxHpInput;
        public TMP_InputField hardCapInput;
        public TMP_InputField disasterThresholdInput;
        /// <summary>Hiển thị và nhập theo % (0–100), tự chuyển đổi 0.0–1.0 vào GameSettings.disasterKillPercent.</summary>
        public TMP_InputField disasterKillPercentInput;
        public TMP_InputField x2IntervalMinInput;
        public TMP_InputField x2IntervalMaxInput;
        public TMP_InputField x2DurationInput;
        /// <summary>Hiển thị và nhập theo % (0–100), tự chuyển đổi 0.0–1.0 vào GameSettings.weakX2Threshold.</summary>
        public TMP_InputField weakX2ThresholdInput;
        public TMP_InputField fullNameLimitInput;

        [Header("Selectors")]
        /// <summary>Options: 720 x 720, 1080 x 1080, 1440 x 1440.</summary>
        public TMP_Dropdown windowSizeDropdown;
        /// <summary>Options: Live tự động (0), Thường (1).</summary>
        public TMP_Dropdown modeDropdown;
        /// <summary>Options: Bật (0), Tắt (1).</summary>
        public TMP_Dropdown musicDropdown;
        /// <summary>Options: Bật (0), Tắt (1).</summary>
        public TMP_Dropdown sfxDropdown;

        [Header("Dynamic Guide Lines List")]
        public Transform guideLinesContainer;
        /// <summary>Template dòng ẩn sẵn chứa 1 TMP_InputField và 1 Button xóa.</summary>
        public GameObject guideLineRowTemplate;
        public Button addGuideLineButton;

        [Header("Footer Actions & Labels")]
        public Button saveButton;
        public Button closeButton;
        public Button cancelButton;
        public TMP_Text noteText;
        public TMP_Text statusFeedbackText;

        readonly List<GuideLineRow> guideRows = new List<GuideLineRow>(8);
        bool buttonsBound;
        float feedbackHideAt;

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        void Awake()
        {
            EnsureStaticDropdownOptions();
            EnsureNoteText();
            BindButtons();

            if (guideLineRowTemplate != null)
            {
                guideLineRowTemplate.SetActive(false);
            }

            if (modalRoot != null && modalRoot != gameObject)
            {
                modalRoot.SetActive(false);
            }

            ClearFeedback();
        }

        void OnEnable()
        {
            GameEvents.SettingsMenuRequested += OnSettingsMenuRequested;
            GameEvents.ModeChanged += OnExternalModeChanged;
        }

        void OnDisable()
        {
            GameEvents.SettingsMenuRequested -= OnSettingsMenuRequested;
            GameEvents.ModeChanged -= OnExternalModeChanged;
        }

        void Update()
        {
            if (feedbackHideAt > 0f && Time.unscaledTime >= feedbackHideAt)
            {
                ClearFeedback();
            }

            if (!IsOpen) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        void BindButtons()
        {
            if (buttonsBound) return;
            buttonsBound = true;

            if (saveButton != null) saveButton.onClick.AddListener(SaveAndApply);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            if (addGuideLineButton != null) addGuideLineButton.onClick.AddListener(OnAddGuideLineClicked);
        }

        void EnsureStaticDropdownOptions()
        {
            if (windowSizeDropdown != null && windowSizeDropdown.options.Count != WindowSizeChoices.Length)
            {
                windowSizeDropdown.ClearOptions();
                windowSizeDropdown.AddOptions(new List<string>
                {
                    "720 x 720",
                    "1080 x 1080",
                    "1440 x 1440"
                });
            }

            if (modeDropdown != null && modeDropdown.options.Count != 2)
            {
                modeDropdown.ClearOptions();
                modeDropdown.AddOptions(new List<string>
                {
                    "Live tự động",
                    "Thường"
                });
            }
        }

        void EnsureNoteText()
        {
            if (noteText != null)
            {
                noteText.text = RequiredNoteText;
            }
        }

        void OnSettingsMenuRequested()
        {
            if (IsOpen) Close();
            else Open();
        }

        void OnExternalModeChanged(GameMode mode)
        {
            if (!IsOpen || modeDropdown == null) return;
            modeDropdown.SetValueWithoutNotify(mode == GameMode.Manual ? 1 : 0);
        }

        public void Open()
        {
            EnsureStaticDropdownOptions();
            EnsureNoteText();
            LoadFromSettings();
            ClearFeedback();

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            if (modalRoot != null && modalRoot != gameObject)
            {
                modalRoot.SetActive(false);
            }
            ClearFeedback();
        }

        void LoadFromSettings()
        {
            var s = GameSettings.Current ?? new GameSettings();

            SetInput(coreMaxHpInput, FormatFloat(s.coreMaxHp));
            SetInput(hardCapInput, s.hardCap.ToString(CultureInfo.InvariantCulture));
            SetInput(disasterThresholdInput, s.disasterThreshold.ToString(CultureInfo.InvariantCulture));
            SetInput(disasterKillPercentInput, FormatFloat(Mathf.Round(s.disasterKillPercent * 1000f) / 10f));
            SetInput(x2IntervalMinInput, FormatFloat(s.x2IntervalMin));
            SetInput(x2IntervalMaxInput, FormatFloat(s.x2IntervalMax));
            SetInput(x2DurationInput, FormatFloat(s.x2Duration));
            SetInput(weakX2ThresholdInput, FormatFloat(Mathf.Round(s.weakX2Threshold * 1000f) / 10f));
            SetInput(fullNameLimitInput, s.fullNameLimit.ToString(CultureInfo.InvariantCulture));

            if (windowSizeDropdown != null)
            {
                int idx = 1; // mặc định 1080
                for (int i = 0; i < WindowSizeChoices.Length; i++)
                {
                    if (WindowSizeChoices[i] == s.windowSize)
                    {
                        idx = i;
                        break;
                    }
                }
                windowSizeDropdown.SetValueWithoutNotify(idx);
                windowSizeDropdown.RefreshShownValue();
            }

            if (modeDropdown != null)
            {
                modeDropdown.SetValueWithoutNotify(GameState.Mode == GameMode.Manual ? 1 : 0);
                modeDropdown.RefreshShownValue();
            }
            if (musicDropdown != null) { musicDropdown.SetValueWithoutNotify(s.musicEnabled ? 0 : 1); musicDropdown.RefreshShownValue(); }
            if (sfxDropdown != null) { sfxDropdown.SetValueWithoutNotify(s.sfxEnabled ? 0 : 1); sfxDropdown.RefreshShownValue(); }

            RebuildGuideLineRows(s.guideLines);
        }

        void RebuildGuideLineRows(string[] lines)
        {
            ClearGuideLineRows();

            if (lines != null && lines.Length > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    CreateGuideLineRow(lines[i] ?? string.Empty);
                }
            }
            else
            {
                CreateGuideLineRow(string.Empty);
            }

            RebuildLayout();
        }

        void ClearGuideLineRows()
        {
            for (int i = 0; i < guideRows.Count; i++)
            {
                if (guideRows[i].Root != null)
                {
                    Destroy(guideRows[i].Root);
                }
            }
            guideRows.Clear();
        }

        void OnAddGuideLineClicked()
        {
            var row = CreateGuideLineRow(string.Empty);
            RebuildLayout();
            if (row != null && row.Input != null)
            {
                row.Input.ActivateInputField();
            }
        }

        GuideLineRow CreateGuideLineRow(string initialText)
        {
            if (guideLinesContainer == null || guideLineRowTemplate == null) return null;

            GameObject rowObj = Instantiate(guideLineRowTemplate, guideLinesContainer);
            rowObj.name = $"GuideLineRow_{guideRows.Count + 1}";
            rowObj.SetActive(true);

            var input = rowObj.GetComponentInChildren<TMP_InputField>(true);
            var delBtn = rowObj.GetComponentInChildren<Button>(true);

            var row = new GuideLineRow
            {
                Root = rowObj,
                Input = input,
                DeleteButton = delBtn
            };

            if (input != null)
            {
                input.text = initialText ?? string.Empty;
            }

            if (delBtn != null)
            {
                delBtn.onClick.RemoveAllListeners();
                delBtn.onClick.AddListener(() => RemoveGuideLineRow(row));
            }

            guideRows.Add(row);
            return row;
        }

        void RemoveGuideLineRow(GuideLineRow row)
        {
            if (row == null) return;
            guideRows.Remove(row);
            if (row.Root != null)
            {
                Destroy(row.Root);
            }
            RebuildLayout();
        }

        void RebuildLayout()
        {
            if (guideLinesContainer is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
        }

        public void SaveAndApply()
        {
            var s = GameSettings.Current;
            if (s == null) return;

            s.coreMaxHp = Mathf.Clamp(ParseFloat(coreMaxHpInput, s.coreMaxHp), 100f, 100000f);
            s.hardCap = Mathf.Clamp(ParseInt(hardCapInput, s.hardCap), 10, 500);
            s.disasterThreshold = Mathf.Clamp(ParseInt(disasterThresholdInput, s.disasterThreshold), 10, 500);

            float killPct = Mathf.Clamp(ParseFloat(disasterKillPercentInput, s.disasterKillPercent * 100f), 0f, 100f);
            s.disasterKillPercent = killPct / 100f;

            float minX2 = Mathf.Clamp(ParseFloat(x2IntervalMinInput, s.x2IntervalMin), 10f, 3600f);
            float maxX2 = Mathf.Clamp(ParseFloat(x2IntervalMaxInput, s.x2IntervalMax), minX2, 3600f);
            s.x2IntervalMin = minX2;
            s.x2IntervalMax = maxX2;

            s.x2Duration = Mathf.Clamp(ParseFloat(x2DurationInput, s.x2Duration), 5f, 600f);

            float weakPct = Mathf.Clamp(ParseFloat(weakX2ThresholdInput, s.weakX2Threshold * 100f), 0f, 100f);
            s.weakX2Threshold = weakPct / 100f;

            s.fullNameLimit = Mathf.Clamp(ParseInt(fullNameLimitInput, s.fullNameLimit), 5, 500);

            if (windowSizeDropdown != null)
            {
                int idx = Mathf.Clamp(windowSizeDropdown.value, 0, WindowSizeChoices.Length - 1);
                s.windowSize = WindowSizeChoices[idx];
            }

            // Thu thập danh sách guideLines (loại bỏ dòng trống)
            var collectedLines = new List<string>(guideRows.Count);
            for (int i = 0; i < guideRows.Count; i++)
            {
                var input = guideRows[i].Input;
                if (input == null) continue;
                string trimmed = input.text != null ? input.text.Trim() : string.Empty;
                if (!string.IsNullOrEmpty(trimmed))
                {
                    collectedLines.Add(trimmed);
                }
            }
            s.guideLines = collectedLines.ToArray();

            if (musicDropdown != null) s.musicEnabled = musicDropdown.value == 0;
            if (sfxDropdown != null) s.sfxEnabled = sfxDropdown.value == 0;

            GameMode targetMode = GameState.Mode;
            if (modeDropdown != null)
            {
                targetMode = modeDropdown.value == 1 ? GameMode.Manual : GameMode.LiveAuto;
            }

            if (targetMode != GameState.Mode)
            {
                // GameState.SetMode cập nhật GameSettings.Current.mode, gọi GameSettings.Save() và phát ModeChanged
                GameState.SetMode(targetMode);
            }
            else
            {
                s.mode = targetMode == GameMode.Manual ? "manual" : "live";
                GameSettings.Save();
            }

            // Đồng bộ lại các ô hiển thị sau khi clamp
            LoadFromSettings();

            SettingsSaved?.Invoke();
            ShowFeedback("Đã lưu cài đặt!", new Color32(0x4A, 0xDE, 0x80, 0xFF));
        }

        void ShowFeedback(string message, Color color)
        {
            if (statusFeedbackText == null) return;
            statusFeedbackText.text = message;
            statusFeedbackText.color = color;
            statusFeedbackText.gameObject.SetActive(true);
            feedbackHideAt = Time.unscaledTime + 2.5f;
        }

        void ClearFeedback()
        {
            feedbackHideAt = 0f;
            if (statusFeedbackText == null) return;
            statusFeedbackText.text = string.Empty;
            statusFeedbackText.gameObject.SetActive(false);
        }

        static void SetInput(TMP_InputField field, string value)
        {
            if (field != null) field.SetTextWithoutNotify(value);
        }

        static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static float ParseFloat(TMP_InputField field, float fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text)) return fallback;
            string raw = field.text.Trim().Replace(',', '.');
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        static int ParseInt(TMP_InputField field, int fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text)) return fallback;
            string raw = field.text.Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;
            if (float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat))
                return Mathf.RoundToInt(parsedFloat);
            return fallback;
        }
    }
}
