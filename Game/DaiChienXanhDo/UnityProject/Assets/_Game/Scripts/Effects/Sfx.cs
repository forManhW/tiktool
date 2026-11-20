using System.Collections.Generic;
using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Âm thanh: một nhóm AudioSource dùng chung + nhạc nền lặp. Âm thanh 2D (người xem nghe qua live, không cần vị trí).
    /// Mỗi loại có giới hạn số lần phát để 200 lính đấm cùng lúc không thành tiếng ồn.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;
        public static Sfx I
        {
            get
            {
                if (instance == null) instance = new GameObject("Sfx").AddComponent<Sfx>();
                return instance;
            }
        }

        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        AudioSource music;
        int next;

        void Awake()
        {
            instance = this;
            for (int i = 0; i < 20; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool.Add(s);
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
        }

        void Start()
        {
            var clip = GameAssets.I.music;
            if (clip != null)
            {
                music.clip = clip;
                music.volume = GameSettings.Current.musicVolume;
                music.Play();
            }
        }

        /// <summary>Phát ngẫu nhiên một clip trong bộ. minInterval: khoảng cách tối thiểu giữa 2 lần cùng loại.</summary>
        public void Play(string key, AudioClip[] set, float volume, float minInterval, float pitchJitter = 0.08f)
        {
            if (set == null || set.Length == 0) return;
            Play(key, set[Random.Range(0, set.Length)], volume, minInterval, pitchJitter);
        }

        public void Play(string key, AudioClip clip, float volume, float minInterval, float pitchJitter = 0f)
        {
            if (clip == null) return;
            float now = Time.unscaledTime;
            if (lastPlay.TryGetValue(key, out float t) && now - t < minInterval) return;
            lastPlay[key] = now;
            var s = pool[next];
            next = (next + 1) % pool.Count;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume * GameSettings.Current.sfxVolume);
        }

        // Lối tắt cho các loại âm thanh trong game
        public static void Punch(bool heavy)
        {
            var a = GameAssets.I;
            if (heavy) I.Play("punch_heavy", a.sfxPunchHeavy, 0.9f, 0.08f);
            else I.Play("punch", a.sfxPunch, 0.45f, 0.07f, 0.15f);
        }
        public static void Explosion(float volume = 1f) => I.Play("explosion", GameAssets.I.sfxExplosion, volume, 0.08f);
        public static void Thunder() { I.Play("thunder", GameAssets.I.sfxThunder, 1f, 0.15f); }
        public static void TankShot() => I.Play("tank", GameAssets.I.sfxTankShot, 0.8f, 0.1f);
        public static void Stomp() => I.Play("stomp", GameAssets.I.sfxRobotStomp, 1f, 0.2f);
        public static void Victory() => I.Play("victory", GameAssets.I.sfxVictory, 0.9f, 1f);
        public static void Warning() => I.Play("warning", GameAssets.I.sfxWarning, 0.8f, 1f);
        public static void Siren() => I.Play("siren", GameAssets.I.sfxSiren, 0.8f, 2f);
        public static void Gift() => I.Play("gift", GameAssets.I.sfxGift, 0.7f, 0.3f);
    }
}
