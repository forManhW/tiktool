using UnityEngine;
using UnityEngine.InputSystem;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Camera xoay quanh một điểm (pivot). Chế độ Thường: streamer điều khiển hoàn toàn.
    /// Chế độ Live tự động: CameraDirector gọi Focus/Overview.
    /// Luôn giữ khung hình vuông (thêm viền đen nếu cửa sổ không vuông).
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }

        public const float OverviewDistance = 46f;
        const float MinDistance = 8f, MaxDistance = 75f, MinPitch = 25f, MaxPitch = 85f;

        public Camera cam;

        Vector3 pivot, goalPivot;
        float yaw, goalYaw, pitch = 55f, goalPitch = 55f, distance = OverviewDistance, goalDistance = OverviewDistance;
        Transform follow;
        float shakeAmp, shakeUntil, shakeDuration;
        readonly Vector4[] saved = new Vector4[4];     // x,z pivot, yaw, distance
        readonly bool[] hasSaved = new bool[4];
        int tabIndex;

        public Transform Following => follow;

        void Awake()
        {
            I = this;
            if (cam == null) cam = GetComponentInChildren<Camera>();
            Overview(true);
        }

        public void Overview(bool instant = false)
        {
            follow = null;
            goalPivot = Vector3.zero; goalYaw = 0f; goalPitch = 55f; goalDistance = OverviewDistance;
            if (instant) { pivot = goalPivot; yaw = goalYaw; pitch = goalPitch; distance = goalDistance; }
        }

        public void Focus(Transform target, Vector3 position, float dist)
        {
            follow = target;
            goalPivot = target != null ? target.position : position;
            goalDistance = Mathf.Clamp(dist, MinDistance, MaxDistance);
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmp && Time.time < shakeUntil) return;
            shakeAmp = amplitude; shakeDuration = Mathf.Max(0.01f, duration); shakeUntil = Time.time + duration;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (follow != null)
            {
                if (!follow.gameObject.activeInHierarchy) follow = null;
                else goalPivot = follow.position;
            }
            if (GameState.Mode == GameMode.Manual) HandleInput(dt);

            float k = 1f - Mathf.Exp(-5f * dt);
            pivot = Vector3.Lerp(pivot, goalPivot, k);
            yaw = Mathf.LerpAngle(yaw, goalYaw, k);
            pitch = Mathf.Lerp(pitch, goalPitch, k);
            distance = Mathf.Lerp(distance, goalDistance, k);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pos = pivot - rot * Vector3.forward * distance;
            if (Time.time < shakeUntil)
            {
                float remaining = (shakeUntil - Time.time) / shakeDuration;
                pos += Random.insideUnitSphere * shakeAmp * remaining;
            }
            if (cam != null)
            {
                cam.transform.SetPositionAndRotation(pos, rot);
                KeepSquare();
            }
        }

        void KeepSquare()
        {
            float w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;
            Rect r = w > h ? new Rect((1f - h / w) * 0.5f, 0f, h / w, 1f) : new Rect(0f, (1f - w / h) * 0.5f, 1f, w / h);
            if (cam.rect != r) cam.rect = r;
        }

        void HandleInput(float dt)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool ctrl = kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
            var yawRot = Quaternion.Euler(0f, goalYaw, 0f);

            if (kb != null)
            {
                Vector3 move = Vector3.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += Vector3.forward;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move += Vector3.back;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move += Vector3.left;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += Vector3.right;
                if (move != Vector3.zero && !ctrl)
                {
                    follow = null;
                    goalPivot += yawRot * move.normalized * (goalDistance * 0.8f) * dt;
                }
                if (kb.qKey.isPressed) goalYaw -= 90f * dt;
                if (kb.eKey.isPressed) goalYaw += 90f * dt;
                if (kb.spaceKey.wasPressedThisFrame) Overview();
                if (kb.escapeKey.wasPressedThisFrame) follow = null;
                if (kb.tabKey.wasPressedThisFrame) CycleTopPlayers();

                Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
                for (int i = 0; i < 4; i++)
                {
                    if (!kb[digits[i]].wasPressedThisFrame) continue;
                    if (ctrl) { saved[i] = new Vector4(goalPivot.x, goalPivot.z, goalYaw, goalDistance); hasSaved[i] = true; GameEvents.RaiseNotice($"Đã lưu góc camera {i + 1}", NoticeKind.Info); }
                    else if (hasSaved[i]) { follow = null; goalPivot = new Vector3(saved[i].x, 0f, saved[i].y); goalYaw = saved[i].z; goalDistance = saved[i].w; }
                }
            }

            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (mouse.rightButton.isPressed)
                {
                    goalYaw += delta.x * 0.25f;
                    goalPitch = Mathf.Clamp(goalPitch - delta.y * 0.2f, MinPitch, MaxPitch);
                }
                if (mouse.middleButton.isPressed)
                {
                    follow = null;
                    float speed = goalDistance * 0.0025f;
                    goalPivot += yawRot * new Vector3(-delta.x, 0f, -delta.y) * speed;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    goalDistance = Mathf.Clamp(goalDistance * (scroll > 0 ? 0.88f : 1.12f), MinDistance, MaxDistance);
                if (mouse.leftButton.wasPressedThisFrame && cam != null)
                {
                    var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    if (Physics.Raycast(ray, out var hit, 500f))
                    {
                        var f = hit.collider.GetComponentInParent<Fighter>();
                        if (f != null && f.IsAlive) Focus(f.transform, f.transform.position, 14f);
                    }
                }
            }
            goalPivot = Arena.Clamp(goalPivot);
        }

        void CycleTopPlayers()
        {
            var list = new System.Collections.Generic.List<Viewer>();
            list.AddRange(PlayerRegistry.Top(Team.Blue, 5));
            list.AddRange(PlayerRegistry.Top(Team.Red, 5));
            list.RemoveAll(v => !v.HasLiveFighter);
            if (list.Count == 0) return;
            tabIndex = (tabIndex + 1) % list.Count;
            var f = list[tabIndex].Fighter;
            Focus(f.transform, f.transform.position, 14f);
        }
    }
}
