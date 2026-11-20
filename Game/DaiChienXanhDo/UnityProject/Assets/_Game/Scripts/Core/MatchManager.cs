using System.Collections;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Trận vô hạn: lõi một phe về 0 → phe kia +1 điểm → ăn mừng → tự reset ván mới.
    /// Lệnh đến trong lúc ăn mừng được giữ lại và chạy ngay khi ván mới bắt đầu.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager I { get; private set; }

        enum State { Fighting, Celebrating }
        State state = State.Fighting;
        readonly CoreHealth[] cores = new CoreHealth[2];

        public bool IsFighting => state == State.Fighting;
        public int ScoreBlue => DailyStore.Data.blueWins;
        public int ScoreRed => DailyStore.Data.redWins;
        public CoreHealth Core(Team team) => cores[(int)team];
        public Team? LastWinner { get; private set; }

        void Awake()
        {
            I = this;
            foreach (var c in FindObjectsByType<CoreHealth>(FindObjectsSortMode.None)) cores[(int)c.team] = c;
            DailyStore.Load();
        }

        void Start()
        {
            GameEvents.RaiseScoreChanged(ScoreBlue, ScoreRed);
            foreach (var c in cores) if (c != null) c.ResetHp();
        }

        void Update()
        {
            if (DailyStore.Tick())
            {
                GameEvents.RaiseScoreChanged(ScoreBlue, ScoreRed);
                GameEvents.RaiseNotice("Ngày mới! Tỉ số về 0 : 0", NoticeKind.Info);
            }
        }

        public void OnCoreDestroyed(Team loser)
        {
            if (!IsFighting) return;
            state = State.Celebrating;
            Team winner = loser.Other();
            LastWinner = winner;
            if (MatchEvents.I != null) MatchEvents.I.ResetRound(); // tắt X2 / cảnh báo thảm họa, không còn ý nghĩa khi đã có phe thắng
            DailyStore.AddWin(winner);
            var mvp = PlayerRegistry.Mvp(winner);
            GameEvents.RaiseScoreChanged(ScoreBlue, ScoreRed);
            GameEvents.RaiseRoundWon(winner, mvp);
            GameEvents.RaiseNotice($"PHE {winner.DisplayName()} THẮNG!" + (mvp != null ? $" MVP: {mvp.Nickname}" : ""), NoticeKind.Info);
            Debug.Log($"[Match] Phe {winner} thắng ván. Tỉ số {ScoreBlue}:{ScoreRed}. MVP: {mvp?.Nickname ?? "-"}");
            if (CameraRig.I != null) CameraRig.I.Shake(1.2f, 1.5f);
            var lost = Core(loser);
            if (lost != null) { Fx.I.Explosion(lost.transform.position + Vector3.up * 2f, 2.5f); Sfx.Explosion(1f); }
            Sfx.Victory();
            StartCoroutine(ResetAfter(GameSettings.Current.celebrateSeconds));
        }

        IEnumerator ResetAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ResetRound();
        }

        /// <summary>Dọn sân, hồi đầy 2 lõi, xóa danh sách người chơi của ván. Không đổi tỉ số.</summary>
        public void ResetRound()
        {
            StopAllCoroutines();
            if (EffectManager.I != null) EffectManager.I.ClearAll();
            if (FighterManager.I != null) FighterManager.I.ClearAll();
            PlayerRegistry.Clear();
            TeamPower.Reset();
            if (MatchEvents.I != null) MatchEvents.I.ResetRound();
            foreach (var c in cores) if (c != null) c.ResetHp();
            state = State.Fighting;
            GameEvents.RaiseRoundReset();
            int pending = CommandRouter.PendingCount;
            CommandRouter.FlushPending();
            Debug.Log($"[Match] Ván mới. Chạy {pending} lệnh đã giữ lại trong lúc ăn mừng.");
        }

        public void ResetScore()
        {
            DailyStore.ResetScore();
            GameEvents.RaiseScoreChanged(ScoreBlue, ScoreRed);
        }
    }
}
