using System;
using System.IO;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Luật riêng của game (không phải mapping quà, mapping nằm ở TikTool).
    /// Lưu ở &lt;persistentDataPath&gt;/settings.json, sửa bằng menu F10 hoặc sửa file rồi khởi động lại.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public string mode = "live";              // live | manual
        public int windowSize = 1080;             // cửa sổ vuông

        public float coreMaxHp = 1000f;
        public float fighterHp = 300f;            // 100 thì bị vây đánh 2–3 nhịp đã chết, quá nhanh
        public float fighterDamage = 10f;
        public float fighterSpeed = 3.5f;
        public float punchInterval = 0.8f;
        public float coreDamagePerPunch = 5f;     // lính đánh lõi khi phe kia chết hết

        public int hardCap = 200;                 // tổng số lính tối đa trên sân
        public int disasterThreshold = 120;       // vượt ngưỡng thì gọi thảm họa
        public float disasterKillPercent = 0.4f;
        public int disasterMinPerTeam = 3;
        public float disasterCooldown = 20f;

        public float x2IntervalMin = 180f;
        public float x2IntervalMax = 300f;
        public float x2Duration = 30f;
        public float weakX2Threshold = 0.3f;      // lõi dưới 30% máu
        public float weakX2Duration = 20f;

        public float celebrateSeconds = 7f;
        public float musicVolume = 0.35f;
        public float sfxVolume = 0.8f;
        public int fullNameLimit = 40;            // sân đông hơn thì chỉ hiện tên đầy đủ cho người nổi bật

        public string[] guideLines =
        {
            "Comment XANH hoặc ĐỎ để vào trận!",
            "Tặng quà để tiếp sức cho phe của bạn",
            "Phe nào bị phá lõi sẽ thua, phe kia +1 điểm",
        };

        public static GameSettings Current { get; private set; } = new GameSettings();
        public static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");

        public GameMode Mode => mode == "manual" ? GameMode.Manual : GameMode.LiveAuto;

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath));
                    if (loaded != null) Current = loaded;
                    // Nâng cấp file cài đặt cũ: v1 có máu lính 100, quá thấp
                    if (Current.version < 2 && Current.fighterHp <= 100f) Current.fighterHp = 300f;
                    Current.version = CurrentVersion;
                }
                Save(); // ghi lại để file luôn có đủ trường mới
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Không đọc được settings.json, dùng mặc định: {e.Message}");
                Current = new GameSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e) { Debug.LogWarning($"[Settings] Không lưu được: {e.Message}"); }
        }
    }
}
