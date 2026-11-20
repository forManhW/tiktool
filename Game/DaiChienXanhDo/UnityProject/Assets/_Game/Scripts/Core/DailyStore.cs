using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DaiChienXanhDo
{
    [Serializable]
    public class DonorEntry
    {
        public string id;
        public string nickname;
        public string team;
        public int points;
    }

    [Serializable]
    public class DayData
    {
        public string date;
        public int blueWins;
        public int redWins;
        public int rounds;
        public List<DonorEntry> donors = new List<DonorEntry>();
    }

    /// <summary>Tỉ số + top người đóng góp, lưu theo NGÀY ở &lt;persistentDataPath&gt;/data/YYYY-MM-DD.json. Giữ 30 ngày.</summary>
    public static class DailyStore
    {
        const int KeepDays = 30;
        public static DayData Data { get; private set; } = new DayData();
        static bool dirty;
        static float nextSaveAt, nextDateCheckAt;

        static string Dir => Path.Combine(Application.persistentDataPath, "data");
        static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        static string FileFor(string date) => Path.Combine(Dir, date + ".json");

        public static void Load()
        {
            string date = Today;
            Data = new DayData { date = date };
            try
            {
                Directory.CreateDirectory(Dir);
                if (File.Exists(FileFor(date)))
                    Data = JsonUtility.FromJson<DayData>(File.ReadAllText(FileFor(date))) ?? Data;
                Data.date = date;
                Prune();
            }
            catch (Exception e) { Debug.LogWarning($"[DailyStore] {e.Message}"); }
        }

        /// <summary>Gọi mỗi frame: tự lưu và tự sang ngày mới lúc 0h. Trả true nếu vừa sang ngày mới.</summary>
        public static bool Tick()
        {
            bool newDay = false;
            if (Time.unscaledTime >= nextDateCheckAt)
            {
                nextDateCheckAt = Time.unscaledTime + 10f;
                if (Data.date != Today) { Save(); Load(); newDay = true; }
            }
            if (dirty && Time.unscaledTime >= nextSaveAt) Save();
            return newDay;
        }

        public static void AddWin(Team team)
        {
            if (team == Team.Blue) Data.blueWins++; else Data.redWins++;
            Data.rounds++;
            Save();
        }

        public static void AddPoints(Viewer viewer, int points)
        {
            if (viewer == null || points <= 0) return;
            var entry = Data.donors.FirstOrDefault(d => d.id == viewer.Id);
            if (entry == null) { entry = new DonorEntry { id = viewer.Id }; Data.donors.Add(entry); }
            entry.nickname = viewer.Nickname;
            entry.team = viewer.Team.Key();
            entry.points += points;
            MarkDirty();
        }

        public static List<DonorEntry> Top(Team team, int count) =>
            Data.donors.Where(d => d.team == team.Key()).OrderByDescending(d => d.points).Take(count).ToList();

        public static void ResetScore()
        {
            Data.blueWins = 0; Data.redWins = 0; Data.rounds = 0;
            Save();
        }

        static void MarkDirty()
        {
            if (!dirty) nextSaveAt = Time.unscaledTime + 15f;
            dirty = true;
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(FileFor(Data.date), JsonUtility.ToJson(Data, true));
                dirty = false;
            }
            catch (Exception e) { Debug.LogWarning($"[DailyStore] Không lưu được: {e.Message}"); }
        }

        static void Prune()
        {
            var cutoff = DateTime.Now.Date.AddDays(-KeepDays);
            foreach (var file in Directory.GetFiles(Dir, "????-??-??.json"))
            {
                if (DateTime.TryParse(Path.GetFileNameWithoutExtension(file), out var d) && d < cutoff)
                    File.Delete(file);
            }
        }
    }
}
