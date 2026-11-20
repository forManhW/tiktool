using UnityEngine;

namespace DaiChienXanhDo
{
    /// <summary>
    /// Nơi gắn model/vật liệu. Nằm ở Assets/_Game/Resources/GameAssets.asset.
    /// Prefab để trống thì game dùng khối hình cơ bản (greybox), nên thay model thật chỉ cần kéo prefab vào đây.
    /// </summary>
    [CreateAssetMenu(menuName = "Dai Chien Xanh Do/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Prefab (để trống = greybox)")]
        public GameObject blueFighterPrefab;
        public GameObject redFighterPrefab;
        public GameObject tankPrefab;
        public GameObject robotPrefab;
        public GameObject meteorPrefab;

        [Header("Vật liệu")]
        public Material blueMaterial;
        public Material redMaterial;
        public Material superMaterial;
        public Material boltMaterial;
        public Material meteorMaterial;
        public Material groundMaterial;

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
    }
}
