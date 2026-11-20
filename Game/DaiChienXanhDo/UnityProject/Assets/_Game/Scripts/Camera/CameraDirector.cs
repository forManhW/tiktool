using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Đạo diễn camera cho chế độ Live tự động. Ưu tiên:
    /// thắng ván (4) &gt; quà bậc 4+ (3) &gt; người mới vào (2) &gt; lõi sắp vỡ (1.5) &gt; quà bậc 1–3 (1) &gt; toàn cảnh.
    /// Mỗi cảnh giữ tối thiểu 1 giây trước khi bị cảnh ưu tiên cao hơn chen ngang; cảnh chờ quá 6 giây thì bỏ.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        class Shot
        {
            public Transform Target;
            public Vector3 Position;
            public float Priority, Hold, Distance, Created;
        }

        const float MinHold = 1f, StaleAfter = 6f, BurstWindow = 2f;
        const int BurstLimit = 3;

        readonly List<Shot> queue = new List<Shot>();
        readonly List<float> recentJoins = new List<float>();
        readonly int[] burstCount = new int[2];
        float burstFlushAt;
        Shot current;
        float currentStart;
        readonly float[] lastCoreShot = new float[2];

        void OnEnable()
        {
            GameEvents.PlayerJoined += OnJoined;
            GameEvents.EffectStarted += OnEffect;
            GameEvents.CoreHealthChanged += OnCore;
            GameEvents.RoundWon += OnWon;
            GameEvents.RoundReset += OnReset;
            GameEvents.ModeChanged += OnMode;
        }

        void OnDisable()
        {
            GameEvents.PlayerJoined -= OnJoined;
            GameEvents.EffectStarted -= OnEffect;
            GameEvents.CoreHealthChanged -= OnCore;
            GameEvents.RoundWon -= OnWon;
            GameEvents.RoundReset -= OnReset;
            GameEvents.ModeChanged -= OnMode;
        }

        void Request(Transform target, Vector3 pos, float priority, float hold, float distance)
        {
            if (GameState.Mode != GameMode.LiveAuto) return;
            queue.Add(new Shot { Target = target, Position = pos, Priority = priority, Hold = hold, Distance = distance, Created = Time.time });
            if (queue.Count > 30) queue.RemoveAt(0);
        }

        void OnJoined(Viewer v)
        {
            float now = Time.time;
            recentJoins.RemoveAll(t => now - t > BurstWindow);
            recentJoins.Add(now);
            if (recentJoins.Count > BurstLimit)
            {
                // Quá nhiều người vào cùng lúc: gom thành một thông báo thay vì giật camera liên tục.
                burstCount[(int)v.Team]++;
                if (burstFlushAt <= 0f) burstFlushAt = now + BurstWindow;
                return;
            }
            if (v.Fighter != null) Request(v.Fighter.transform, v.Fighter.transform.position, 2f, 2.5f, 13f);
        }

        void OnEffect(EffectNotice n)
        {
            if (n.Tier >= 4) Request(n.Target, n.Position, 3f, 3.5f, n.EffectId == "giant_robot" ? 30f : 22f);
            else if (n.Tier >= 1) Request(n.Target, n.Position, 1f, 1.8f, 13f);
        }

        void OnCore(Team team, float hp, float max)
        {
            if (max <= 0f || hp <= 0f || hp / max > 0.2f) return;
            if (Time.time - lastCoreShot[(int)team] < 15f) return;
            lastCoreShot[(int)team] = Time.time;
            var core = MatchManager.I != null ? MatchManager.I.Core(team) : null;
            if (core != null) Request(null, core.transform.position, 1.5f, 2.5f, 20f);
        }

        void OnWon(Team winner, Viewer mvp)
        {
            var loserCore = MatchManager.I != null ? MatchManager.I.Core(winner.Other()) : null;
            queue.Clear();
            current = null;
            if (loserCore != null) Request(null, loserCore.transform.position, 4f, GameSettings.Current.celebrateSeconds, 22f);
        }

        void OnReset()
        {
            queue.Clear();
            current = null;
            if (GameState.Mode == GameMode.LiveAuto && CameraRig.I != null) CameraRig.I.Overview();
        }

        void OnMode(GameMode mode)
        {
            queue.Clear();
            current = null;
            if (mode == GameMode.LiveAuto && CameraRig.I != null) CameraRig.I.Overview();
        }

        void Update()
        {
            if (burstFlushAt > 0f && Time.time >= burstFlushAt)
            {
                burstFlushAt = 0f;
                for (int t = 0; t < 2; t++)
                {
                    if (burstCount[t] > 0) GameEvents.RaiseNotice($"+{burstCount[t]} chiến binh mới vào phe {((Team)t).DisplayName()}", NoticeKind.Join);
                    burstCount[t] = 0;
                }
            }

            if (GameState.Mode != GameMode.LiveAuto || CameraRig.I == null) return;
            float now = Time.time;
            queue.RemoveAll(s => now - s.Created > StaleAfter);

            if (current != null)
            {
                float elapsed = now - currentStart;
                bool targetGone = current.Target != null && !current.Target.gameObject.activeInHierarchy;
                Shot better = Best();
                if (elapsed >= current.Hold || targetGone) current = null;
                else if (better != null && better.Priority > current.Priority && elapsed >= MinHold) current = null;
            }

            if (current == null)
            {
                var next = Best();
                if (next != null)
                {
                    queue.Remove(next);
                    current = next;
                    currentStart = now;
                    CameraRig.I.Focus(next.Target, next.Position, next.Distance);
                }
                else if (CameraRig.I.Following != null || now - currentStart > 0.5f)
                {
                    CameraRig.I.Overview();
                }
            }
        }

        Shot Best()
        {
            Shot best = null;
            foreach (var s in queue)
                if (best == null || s.Priority > best.Priority || (Mathf.Approximately(s.Priority, best.Priority) && s.Created < best.Created))
                    best = s;
            return best;
        }
    }
}
