using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// 公開リポジトリには含めないFabモデルのマテリアルを、iPhone用URP表示に合わせる。
    /// モデル原本は Assets/LocalLicensed/ にだけ置く。
    /// </summary>
    public static class FabDemonLord2LocalSetup
    {
        private const string MaterialsPath = "Assets/LocalLicensed/Source/DemonLord2/Materials";
        private const string PrefabPath = "Assets/LocalLicensed/Resources/DemonLord2.prefab";
        private const string AnimationsPath = "Assets/LocalLicensed/Source/DemonLord2/Animations";
        private const string PreviewControllerPath = "Assets/LocalLicensed/DemonLord2Preview.controller";

        [MenuItem("OrdinalScale/iOS AR/4. FabモデルをURPへ合わせる", priority = 4)]
        public static void Configure()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[OrdinalScale] Fabモデルがありません: {PrefabPath}");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[OrdinalScale] URP Litシェーダーが見つかりません。");
                return;
            }

            var paths = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath });
            var converted = 0;
            foreach (var guid in paths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Skin1") && !path.EndsWith("/Eye.mat")) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;

                var albedo = material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null
                    ? material.GetTexture("_BaseMap")
                    : material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                var normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
                var color = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

                material.shader = shader;
                material.SetColor("_BaseColor", color);
                if (albedo != null) material.SetTexture("_BaseMap", albedo);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(material);
                converted++;
            }

            var idle = LoadClip("DemonLord2@Idle1.fbx");
            var attack = LoadClip("DemonLord2@Attack1.fbx");
            if (idle == null || attack == null)
            {
                Debug.LogError("[OrdinalScale] 待機・攻撃のAnimationClipが見つかりません。");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PreviewControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(PreviewControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states)
                stateMachine.RemoveState(state.state);
            var idleState = stateMachine.AddState("Idle1");
            idleState.motion = idle;
            var attackState = stateMachine.AddState("Attack1");
            attackState.motion = attack;
            stateMachine.defaultState = idleState;

            var instance = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var prefabAnimator = instance.GetComponentInChildren<Animator>(true);
                if (prefabAnimator == null)
                {
                    Debug.LogError("[OrdinalScale] FabモデルにAnimatorがありません。");
                    return;
                }
                var idleBindings = CountMatchingBindings(idle, prefabAnimator.transform);
                var attackBindings = CountMatchingBindings(attack, prefabAnimator.transform);
                if (idleBindings == 0 || attackBindings == 0)
                {
                    Debug.LogError($"[OrdinalScale] クリップとモデル骨格が一致しません: " +
                                   $"Idle1={idleBindings}、Attack1={attackBindings}");
                    return;
                }
                prefabAnimator.runtimeAnimatorController = controller;
                prefabAnimator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }

            AssetDatabase.SaveAssets();
            var animator = prefab.GetComponentInChildren<Animator>(true);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[OrdinalScale] FabモデルURP変換: マテリアル{converted}件、" +
                      $"Animator={(animator != null ? "あり" : "なし")}、Renderer={renderers.Length}件、" +
                      $"待機・攻撃Controllerを設定。" +
                      $"原本はGit追跡対象外: {PrefabPath}");
        }

        private static AnimationClip LoadClip(string fileName)
        {
            return AssetDatabase.LoadAllAssetsAtPath($"{AnimationsPath}/{fileName}")
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        private static int CountMatchingBindings(AnimationClip clip, Transform root)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Count(binding => string.IsNullOrEmpty(binding.path) || root.Find(binding.path) != null);
        }
    }
}
