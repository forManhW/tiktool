using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Nơi gắn model, hoạt ảnh, vật liệu, âm thanh. Nằm ở Assets/_Game/Resources/GameAssets.asset.
    /// ProjectSetup tự điền các ô này từ thư mục Art/Audio. Ô nào trống thì game dùng khối hình cơ bản / im lặng.
    /// </summary>
    [CreateAssetMenu(menuName = "Dai Chien Xanh Do/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Nhân vật (chọn ngẫu nhiên mỗi người)")]
        public GameObject[] fighterModels;
        public RuntimeAnimatorController fighterController;
        public float fighterHeight = 1.9f;

        [Header("Prefab cũ (để trống = greybox)")]
        public GameObject blueFighterPrefab;
        public GameObject redFighterPrefab;

        [Header("Xe tăng, robot, thiên thạch")]
        public GameObject tankPrefab;
        public float tankLength = 4.2f;
        public float tankYawOffset = 180f;
        public GameObject robotPrefab;
        public Texture2D robotTexture;
        public Texture2D robotTextureBlue;     // họa tiết robot đã tô lại theo màu phe (ModelSetup tạo)
        public Texture2D robotTextureRed;
        public Texture2D RobotTexture(Team t) { var x = t == Team.Blue ? robotTextureBlue : robotTextureRed; return x != null ? x : robotTexture; }
        public RuntimeAnimatorController robotController;
        public float robotHeight = 8f;
        public GameObject meteorPrefab;
        public GameObject flagPrefab;          // cờ cắm lên xe tăng/robot (tô màu phe)

        [Header("Vật liệu")]
        public Material blueMaterial;
        public Material redMaterial;
        public Material superMaterial;
        public Material boltMaterial;
        public Material meteorMaterial;
        public Material groundMaterial;
        public Material fxAdditiveMaterial;   // hạt cộng sáng: tia lửa, chớp, lửa
        public Material fxSmokeMaterial;      // hạt pha trộn: khói, bụi

        [Header("Icon hiệu ứng (UI/Icons/icon_<id>.png) — bật lên trong sân khi có quà")]
        public Sprite[] effectIcons;

        public Sprite EffectIcon(string effectId)
        {
            if (effectIcons == null) return null;
            string name = "icon_" + effectId;
            foreach (var s in effectIcons) if (s != null && s.name == name) return s;
            return null;
        }

        [Header("Font tên trên đầu xe tăng/robot (TextMeshPro, có dấu tiếng Việt)")]
        public TMPro.TMP_FontAsset nameFont;

        [Header("Âm thanh")]
        public AudioClip[] sfxPunch;
        public AudioClip[] sfxPunchHeavy;
        public AudioClip[] sfxExplosion;
        public AudioClip[] sfxThunder;
        public AudioClip[] sfxTankShot;
        public AudioClip[] sfxRobotStomp;
        public AudioClip sfxVictory;
        public AudioClip sfxWarning;
        public AudioClip sfxSiren;
        public AudioClip sfxGift;
        public AudioClip music;

        static GameAssets instance;
        public static GameAssets I
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameAssets>("GameAssets");
                if (instance == null) instance = CreateInstance<GameAssets>();
                return instance;
            }
        }

        public Material TeamMaterial(Team team) => team == Team.Blue ? blueMaterial : redMaterial;
        public GameObject FighterPrefab(Team team) => team == Team.Blue ? blueFighterPrefab : redFighterPrefab;
        public bool HasFighterModels => fighterModels != null && fighterModels.Length > 0 && fighterController != null;
    }
}
