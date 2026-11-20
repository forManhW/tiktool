using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DaiChienXanhDo.UI
{
    /// <summary>
    /// Điều khiển khung UI gốc: gắn Canvas theo Screen Space - Camera (CameraRig.I.cam),
    /// bảo đảm EventSystem dùng InputSystemUIInputModule, xử lý F2 ẩn/hiện HUD và F1 đổi chế độ Live/Thường.
    /// </summary>
    public class UIRootController : MonoBehaviour
    {
        [Header("Canvas & Containers")]
        public Canvas mainCanvas;
        public CanvasGroup rootGroup;
        public GameObject liveModeRoot;

        [Header("Core Views (Always Visible)")]
        public ScoreHudView scoreHudView;
        public CoreHealthBarsView coreHealthBarsView;
        public FighterNameplatePool fighterNameplatePool;
        public StatusBadgeView statusBadgeView;

        [Header("Camera Binding")]
        public float planeDistance = 5f;

        void Awake()
        {
            if (mainCanvas == null) mainCanvas = GetComponentInChildren<Canvas>(true);
            if (rootGroup == null && mainCanvas != null) rootGroup = mainCanvas.GetComponent<CanvasGroup>();

            EnsureEventSystem();
            TryBindCamera();
        }

        void OnEnable()
        {
            GameEvents.HudVisibilityChanged += OnHudVisibilityChanged;
            GameEvents.ModeChanged += OnModeChanged;
        }

        void OnDisable()
        {
            GameEvents.HudVisibilityChanged -= OnHudVisibilityChanged;
            GameEvents.ModeChanged -= OnModeChanged;
        }

        void Start()
        {
            TryBindCamera();
            ApplyMode(GameState.Mode);
            ApplyHudVisibility(GameState.HudVisible);
        }

        void Update()
        {
            if (mainCanvas == null) return;
            Camera desired = ResolveCamera();
            if (desired != null && (mainCanvas.worldCamera != desired || mainCanvas.renderMode != RenderMode.ScreenSpaceCamera))
            {
                BindCamera(desired);
            }
        }

        Camera ResolveCamera()
        {
            if (CameraRig.I != null && CameraRig.I.cam != null) return CameraRig.I.cam;
            return Camera.main;
        }

        void TryBindCamera()
        {
            if (mainCanvas == null) return;
            Camera cam = ResolveCamera();
            if (cam != null) BindCamera(cam);
        }

        void BindCamera(Camera cam)
        {
            mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainCanvas.worldCamera = cam;
            mainCanvas.planeDistance = planeDistance;
        }

        void EnsureEventSystem()
        {
            var es = EventSystem.current;
            if (es == null) es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                go.transform.SetParent(transform, false);
                es = go.AddComponent<EventSystem>();
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        void OnModeChanged(GameMode mode)
        {
            ApplyMode(mode);
        }

        void OnHudVisibilityChanged(bool visible)
        {
            ApplyHudVisibility(visible);
        }

        void ApplyMode(GameMode mode)
        {
            if (liveModeRoot != null)
            {
                liveModeRoot.SetActive(mode == GameMode.LiveAuto);
            }
        }

        void ApplyHudVisibility(bool visible)
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = visible ? 1f : 0f;
                rootGroup.interactable = visible;
                rootGroup.blocksRaycasts = visible;
            }
            else if (mainCanvas != null)
            {
                mainCanvas.enabled = visible;
            }
        }
    }
}
