using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DaiChienXanhDo.EditorTools
{
    /// <summary>
    /// Gắn tài nguyên thật (đã tải về Art/Audio) vào GameAssets: model người + hoạt ảnh, xe tăng, robot, thiên thạch,
    /// vật liệu hạt, âm thanh. Được ProjectSetup gọi. Chạy lại nhiều lần không sao.
    /// </summary>
    public static class ModelSetup
    {
        const string Art = "Assets/_Game/Art";
        const string Out = "Assets/_Game/Animation";
        const string Vfx = "Assets/_Game/VFX";

        static readonly string[] Characters =
        {
            "BaseCharacter", "BlueSoldier_Male", "BlueSoldier_Female", "Casual_Male", "Casual_Female", "Knight_Male",
            "Knight_Golden_Male", "Ninja_Male", "Soldier_Male", "Soldier_Female", "Viking_Male", "Worker_Male",
        };

        public static void Apply(GameAssets a)
        {
            Directory.CreateDirectory(Out);
            Directory.CreateDirectory(Vfx);

            // --- Người ---
            string charDir = $"{Art}/Characters/Quaternius_Characters";
            var models = new List<GameObject>();
            foreach (var name in Characters)
            {
                string p = $"{charDir}/{name}.fbx";
                if (!File.Exists(p)) continue;
                SetLoops(p, "Idle", "Walk", "Victory");
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) models.Add(go);
            }
            a.fighterModels = models.ToArray();
            string baseChar = $"{charDir}/BaseCharacter.fbx";
            if (File.Exists(baseChar))
            {
                var c = Clips(baseChar);
                a.fighterController = BuildController($"{Out}/Fighter.controller",
                    Pick(c, "Idle"), Pick(c, "Walk"), Pick(c, "Punch"), Pick(c, "RecieveHit"), Pick(c, "Defeat"), Pick(c, "Victory"),
                    walkSpeedRef: 1.6f, punchSpeed: 1.7f);
            }

            // --- Robot ---
            string robot = $"{Art}/Robot/Quaternius_Mechs/George.fbx";
            if (File.Exists(robot))
            {
                SetLoops(robot, "Idle", "Walk", "Run", "Dance");
                var c = Clips(robot);
                a.robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(robot);
                a.robotTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Robot/Quaternius_Mechs/Textures/George_Texture.png");
                a.robotController = BuildController($"{Out}/Robot.controller",
                    Pick(c, "Idle"), Pick(c, "Walk"), Pick(c, "Punch"), Pick(c, "HitRecieve_1"), Pick(c, "Death"), Pick(c, "Dance"),
                    walkSpeedRef: 2.5f, punchSpeed: 1.2f);
            }

            // --- Xe tăng, thiên thạch ---
            a.tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Vehicles/T12_Tank/T12_SovietTank.fbx");
            a.meteorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Environment/Kenney_SpaceKit/meteor_detailed.fbx");

            // --- Vật liệu hạt ---
            var soft = SoftTexture();
            a.fxAdditiveMaterial = ParticleMat("FX_Additive", soft, additive: true);
            a.fxSmokeMaterial = ParticleMat("FX_Smoke", soft, additive: false);
            if (a.boltMaterial != null) { a.boltMaterial.SetColor("_BaseColor", new Color(0.85f, 0.93f, 1f)); EditorUtility.SetDirty(a.boltMaterial); }

            // --- Âm thanh ---
            const string sfx = "Assets/_Game/Audio/SFX";
            a.sfxPunch = Audio($"{sfx}/sfx_punch_01.ogg", $"{sfx}/sfx_punch_02.ogg");
            a.sfxPunchHeavy = Audio($"{sfx}/sfx_punch_heavy_01.ogg", $"{sfx}/sfx_punch_heavy_02.ogg");
            a.sfxExplosion = Audio($"{sfx}/sfx_explosion_01.ogg", $"{sfx}/sfx_explosion_02.ogg", $"{sfx}/sfx_explosion_low_01.ogg");
            a.sfxThunder = Audio($"{sfx}/sfx_thunder_01.ogg", $"{sfx}/sfx_lightning_zap_01.ogg", $"{sfx}/sfx_lightning_zap_02.ogg");
            a.sfxTankShot = Audio($"{sfx}/sfx_tank_shot_01.ogg", $"{sfx}/sfx_tank_shot_02.ogg");
            a.sfxRobotStomp = Audio($"{sfx}/sfx_robot_stomp_01.ogg", $"{sfx}/sfx_robot_stomp_02.ogg");
            a.sfxVictory = Audio($"{sfx}/sfx_victory_fanfare.wav").FirstOrDefault();
            a.sfxWarning = Audio($"{sfx}/sfx_warning_alert_01.ogg").FirstOrDefault();
            a.sfxSiren = Audio($"{sfx}/sfx_disaster_siren.ogg").FirstOrDefault();
            a.sfxGift = Audio($"{sfx}/sfx_gift_chime.ogg").FirstOrDefault();
            a.music = Audio("Assets/_Game/Audio/Music/bgm_battle_fast_loop.ogg").FirstOrDefault();

            EditorUtility.SetDirty(a);
            Debug.Log($"[Setup] Model: {a.fighterModels.Length} nhân vật, robot={(a.robotPrefab != null)}, xe tăng={(a.tankPrefab != null)}, thiên thạch={(a.meteorPrefab != null)}, nhạc={(a.music != null)}");
        }

        static void SetLoops(string path, params string[] loopNames)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) return;
            var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
            bool changed = false;
            foreach (var c in clips)
            {
                bool loop = loopNames.Any(n => Short(c.name) == n);
                if (c.loopTime != loop) { c.loopTime = loop; changed = true; }
            }
            if (changed || imp.clipAnimations.Length == 0)
            {
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
            }
        }

        static string Short(string clipName) { int i = clipName.LastIndexOf('|'); return i >= 0 ? clipName.Substring(i + 1) : clipName; }

        static Dictionary<string, AnimationClip> Clips(string path)
        {
            var d = new Dictionary<string, AnimationClip>();
            foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
            {
                if (c.name.StartsWith("__preview")) continue;
                string k = Short(c.name);
                if (!d.ContainsKey(k) || c.name.Contains("|")) d[k] = c;
            }
            return d;
        }

        static AnimationClip Pick(Dictionary<string, AnimationClip> clips, string name) => clips.TryGetValue(name, out var c) ? c : null;

        /// <summary>Idle ⇄ Walk theo Speed; Punch / Hit bằng trigger; Dead / Win là trạng thái cuối.</summary>
        static AnimatorController BuildController(string path, AnimationClip idle, AnimationClip walk, AnimationClip punch,
            AnimationClip hit, AnimationClip death, AnimationClip win, float walkSpeedRef, float punchSpeed)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Punch", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Win", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;

            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sWalk = sm.AddState("Walk"); sWalk.motion = walk; sWalk.speedParameterActive = false; sWalk.speed = 1.3f;
            var sPunch = sm.AddState("Punch"); sPunch.motion = punch; sPunch.speed = punchSpeed;
            var sHit = sm.AddState("Hit"); sHit.motion = hit; sHit.speed = 1.4f;
            var sDeath = sm.AddState("Death"); sDeath.motion = death; sDeath.speed = 1.6f;
            var sWin = sm.AddState("Victory"); sWin.motion = win;
            sm.defaultState = sIdle;

            var t = sIdle.AddTransition(sWalk); t.hasExitTime = false; t.duration = 0.12f; t.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");
            t = sWalk.AddTransition(sIdle); t.hasExitTime = false; t.duration = 0.15f; t.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");

            t = sm.AddAnyStateTransition(sPunch); t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = true;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Punch"); t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead"); t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Win");
            t = sPunch.AddTransition(sIdle); t.hasExitTime = true; t.exitTime = 0.8f; t.duration = 0.1f;

            t = sm.AddAnyStateTransition(sHit); t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Hit"); t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            t = sHit.AddTransition(sIdle); t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.1f;

            t = sm.AddAnyStateTransition(sDeath); t.hasExitTime = false; t.duration = 0.08f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            t = sm.AddAnyStateTransition(sWin); t.hasExitTime = false; t.duration = 0.2f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Win"); t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>Ảnh chấm tròn mờ dần (64x64) dùng cho mọi hạt.</summary>
        static Texture2D SoftTexture()
        {
            string path = $"{Vfx}/fx_soft.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
                        float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material ParticleMat(string name, Texture2D tex, bool additive)
        {
            string path = $"{Vfx}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);                              // trong suốt
            m.SetFloat("_Blend", additive ? 2f : 0f);                // 2 = cộng sáng, 0 = alpha
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }

        static AudioClip[] Audio(params string[] paths) =>
            paths.Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p)).Where(c => c != null).ToArray();
    }
}
