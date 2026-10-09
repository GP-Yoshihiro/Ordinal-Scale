using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// Quest 3／3S（Q0）用の Editor メニュー。XR パッケージ導入後にコンパイルされる。
    /// 1. パッケージ追加（QuestPackageInstaller）→ 2. 設定適用 → 3. シーン作成 → 4. 検証 → 5. APK ビルド の順に使う。
    /// iPhone 用の設定（iOS の XR ローダー・ビルド対象シーン・縦持ち固定）には触れない。
    /// 検証とビルドの結果は Claude/reports/evidence/ に残す。Quest 実機での合否はここでは判定しない。
    /// </summary>
    public static class QuestMRSetup
    {
        public const string ExpectedUnityVersion = "6000.5.10f1";
        public const string DefaultApplicationId = "com.gpyoshihiro.ordinalscale";
        public const string ApkPath = "Builds/Android/OrdinalScale_Quest_dev.apk";
        private const string IPhoneScenePath = "Assets/_Project/Scenes/iPhone_AR.unity";
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string MenuRoot = "OrdinalScale/Quest MR/";

        // Unity OpenXR: Meta の「Meta Quest: Camera (Passthrough)」が要求する最小 API（Android 12L）
        private const AndroidSdkVersions MinSdk = AndroidSdkVersions.AndroidApiLevel32;

        private const string MetaFeatureSetId = "com.unity.openxr.featureset.meta";

        /// <summary>有効にする OpenXR 機能（パッケージのソースで確認した featureId）。部屋のスキャン系（平面・メッシュ等）は Q0 では使わない。</summary>
        private static readonly (string id, string label)[] s_RequiredFeatures =
        {
            ("com.unity.openxr.feature.metaquest", "Meta Quest Support"),
            ("com.unity.openxr.feature.input.oculustouch", "Oculus Touch Controller Profile"),
            ("com.unity.openxr.feature.input.metaquestplus", "Meta Quest Touch Plus Controller Profile（Quest 3／3S 付属）"),
            ("com.unity.openxr.feature.arfoundation-meta-session", "Meta Quest: Session"),
            ("com.unity.openxr.feature.arfoundation-meta-camera", "Meta Quest: Camera (Passthrough)"),
        };

        private static readonly string[] s_Packages =
        {
            "com.unity.xr.openxr",
            "com.unity.xr.meta-openxr",
            "com.unity.xr.arfoundation",
            "com.unity.xr.arkit",
            "com.unity.xr.management",
            "com.unity.xr.core-utils",
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
        };

        [MenuItem(MenuRoot + "2. Android・OpenXR設定を適用", priority = 102)]
        public static void ApplySettings()
        {
            var android = NamedBuildTarget.Android;

            var id = PlayerSettings.GetApplicationIdentifier(android);
            if (string.IsNullOrEmpty(id) || id.StartsWith("com.DefaultCompany", StringComparison.OrdinalIgnoreCase))
                PlayerSettings.SetApplicationIdentifier(android, DefaultApplicationId);

            // Quest は 64bit ARM のみ。IL2CPP が ARM64 の前提
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (PlayerSettings.Android.minSdkVersion < MinSdk) PlayerSettings.Android.minSdkVersion = MinSdk;

            // URP でパススルーを合成するには Vulkan が最も確実（Unity OpenXR: Meta の検証ルール）
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            var loaderOk = AssignOpenXRLoaderForAndroid(out var loaderMessage);
            var featureMessage = EnableOpenXRFeaturesForAndroid();

            AssetDatabase.SaveAssets();
            Debug.Log("[OrdinalScale] Quest 用の設定を適用しました。\n" +
                      $"- Android: {PlayerSettings.GetApplicationIdentifier(android)} / IL2CPP / ARM64 / minSdk {(int)PlayerSettings.Android.minSdkVersion} / Vulkan\n" +
                      $"- XR ローダー: {loaderMessage}\n- OpenXR 機能: {featureMessage}\n" +
                      "ProjectSettings/ と Assets/XR/ の変更はコミットしてください。" +
                      (loaderOk ? "" : "\nXR ローダーの設定に失敗したため、Project Settings > XR Plug-in Management の Android タブで OpenXR を手動で有効にしてください。"));
        }

        [MenuItem(MenuRoot + "3. Questシーンを作成", priority = 103)]
        public static void CreateScene()
        {
            var exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(QuestSceneBuilder.ScenePath) != null;
            if (exists && !EditorUtility.DisplayDialog("OrdinalScale",
                    $"{QuestSceneBuilder.ScenePath} を作り直しますか？（手で加えた変更は消えます）", "作り直す", "やめる"))
                return;

            QuestSceneBuilder.Build(overwrite: true);
        }

        [MenuItem(MenuRoot + "4. 設定を検証（証拠を保存）", priority = 104)]
        public static void Validate()
        {
            var r = BuildValidationReport();
            r.Finish(out var savedTo);
            Debug.Log($"[OrdinalScale] Quest 設定検証: NG {r.FailureCount}件。{savedTo}\n{r.Text}");
        }

        [MenuItem(MenuRoot + "5. Android開発ビルド（APK）", priority = 105)]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("OrdinalScale", "先に File > Build Profiles で Android に切り替えてください（Switch Platform）。", "OK");
                return;
            }

            var result = BuildApkCore();
            if (result == BuildResult.Succeeded) EditorUtility.RevealInFinder(ApkPath);
        }

        /// <summary>
        /// コマンドラインからの APK ビルド（証拠用）。例：
        /// Unity -batchmode -projectPath OrdinalScale -buildTarget Android
        ///   -executeMethod OrdinalScale.EditorTools.QuestMRSetup.BuildApkFromCommandLine -logFile build_android.log
        /// 成功で終了コード 0、失敗で 1。設定の適用とシーンの作成は事前に済ませておく。
        /// </summary>
        public static void BuildApkFromCommandLine()
        {
            var code = 1;
            try
            {
                var validation = BuildValidationReport();
                validation.Finish(out var validationSaved);
                Debug.Log($"[OrdinalScale] Quest 設定検証: NG {validation.FailureCount}件。{validationSaved}\n{validation.Text}");

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                code = BuildApkCore() == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            EditorApplication.Exit(code);
        }

        private static BuildResult BuildApkCore()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(QuestSceneBuilder.ScenePath) == null)
            {
                Debug.LogError($"[OrdinalScale] {QuestSceneBuilder.ScenePath} がありません。メニュー「3. Questシーンを作成」を先に実行してください。");
                return BuildResult.Failed;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath) ?? "Builds");
            EditorUserBuildSettings.buildAppBundle = false;

            // ビルド対象一覧（iPhone のシーンが先頭）は変えず、Quest シーンだけを明示して APK を作る
            var options = new BuildPlayerOptions
            {
                scenes = new[] { QuestSceneBuilder.ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;

            var r = new EvidenceReport("Q0 Androidビルド");
            r.Check("ビルド結果", s.result == BuildResult.Succeeded, s.result.ToString());
            r.Info("出力先", Path.GetFullPath(ApkPath));
            r.Info("APKサイズ", File.Exists(ApkPath) ? $"{new FileInfo(ApkPath).Length / (1024f * 1024f):0.0} MB" : "なし");
            r.Info("所要時間", s.totalTime.ToString(@"hh\:mm\:ss"));
            r.Info("エラー／警告", $"{s.totalErrors} / {s.totalWarnings}");
            r.Info("シーン", QuestSceneBuilder.ScenePath);
            r.Info("Unity版", Application.unityVersion);
            r.Info("Application ID", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            r.Info("署名", "Unity 既定のデバッグ鍵（キーストアはリポジトリに入れない）");
            foreach (var name in new[] { "com.unity.xr.openxr", "com.unity.xr.meta-openxr", "com.unity.xr.arfoundation" })
            {
                var info = PackageInfo.FindForAssetPath($"Packages/{name}");
                r.Info($"Package {name}", info != null ? info.version : "未導入");
            }

            foreach (var step in report.steps.Where(st => st.messages.Any(m => m.type == LogType.Error || m.type == LogType.Exception)))
            {
                foreach (var m in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception).Take(5))
                    r.Info($"エラー（{step.name}）", m.content.Split('\n')[0]);
            }

            r.Finish(out var savedTo);
            Debug.Log($"[OrdinalScale] Android ビルド {s.result}。{savedTo}\n{r.Text}");
            return s.result;
        }

        private static EvidenceReport BuildValidationReport()
        {
            var r = new EvidenceReport("Q0 Quest設定検証");

            r.Check("Unity版", Application.unityVersion == ExpectedUnityVersion, $"{Application.unityVersion}（固定版: {ExpectedUnityVersion}）");
            r.Check("Android Build Support（Unity Hub のモジュール）",
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android),
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android) ? "導入済み" : "未導入（Unity Hub で Android Build Support・OpenJDK・Android SDK & NDK を追加）");
            r.Info("現在のビルドターゲット", EditorUserBuildSettings.activeBuildTarget.ToString());

            foreach (var name in s_Packages)
            {
                var info = PackageInfo.FindForAssetPath($"Packages/{name}");
                r.Check($"Package {name}", info != null, info != null ? info.version : "未導入");
            }

            // --- Android Player 設定 ---
            var android = NamedBuildTarget.Android;
            var appId = PlayerSettings.GetApplicationIdentifier(android);
            r.Check("Application ID", !string.IsNullOrEmpty(appId) && !appId.StartsWith("com.DefaultCompany", StringComparison.OrdinalIgnoreCase), appId);
            r.Check("Scripting Backend", PlayerSettings.GetScriptingBackend(android) == ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(android).ToString());
            r.Check("Target Architectures", PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures.ToString());
            r.Check("Minimum API Level", PlayerSettings.Android.minSdkVersion >= MinSdk, $"{(int)PlayerSettings.Android.minSdkVersion}（必要: {(int)MinSdk} 以上）");
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            r.Check("Graphics API（先頭が Vulkan）", apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan, string.Join(", ", apis));

            // --- XR ---
            var perTarget = GetPerBuildTargetSettings();
            var androidSettings = perTarget != null ? perTarget.SettingsForBuildTarget(BuildTargetGroup.Android) : null;
            var androidLoaders = LoaderNames(androidSettings);
            r.Check("XR Plug-in (Android) OpenXR", androidLoaders.Contains("OpenXRLoader"), androidLoaders.Length > 0 ? string.Join(", ", androidLoaders) : "なし");
            r.Check("XR Initialize on Startup (Android)", androidSettings != null && androidSettings.InitManagerOnStart,
                androidSettings != null ? androidSettings.InitManagerOnStart.ToString() : "設定なし");

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            foreach (var (featureId, label) in s_RequiredFeatures)
            {
                var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
                r.Check($"OpenXR機能 {label}", feature != null && feature.enabled,
                    feature == null ? "見つからない（パッケージ未導入）" : feature.enabled ? "有効" : "無効");
            }

            // --- iPhone 経路の回帰確認（Quest の設定で壊していないこと） ---
            var iosLoaders = LoaderNames(perTarget != null ? perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS) : null);
            r.Check("iPhone: XR Plug-in (iOS) Apple ARKit のまま", iosLoaders.Contains("ARKitLoader"), iosLoaders.Length > 0 ? string.Join(", ", iosLoaders) : "なし");
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(sc => sc.enabled);
            r.Check("iPhone: ビルド対象の先頭シーン", firstScene != null && firstScene.path == IPhoneScenePath, firstScene != null ? firstScene.path : "なし");

            // --- シーン ---
            QuestSceneBuilder.Inspect(r);

            r.Info("URP", "Quest 推奨（HDR・ポストプロセス無効）は iPhone と共有の URP アセットに関わるため Q4 で別の品質設定として扱う");
            return r;
        }

        private static bool AssignOpenXRLoaderForAndroid(out string message)
        {
            var perTarget = GetPerBuildTargetSettings();
            if (perTarget == null)
            {
                message = "XR Plug-in Management の設定アセットが見つかりません";
                return false;
            }

            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart = true;
            EditorUtility.SetDirty(settings);

            var manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var assigned = LoaderNames(settings).Contains("OpenXRLoader")
                           || XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderType, BuildTargetGroup.Android);
            message = assigned ? $"Android = {string.Join(", ", LoaderNames(settings))}" : "OpenXR ローダーを割り当てられませんでした";
            return assigned;
        }

        private static string EnableOpenXRFeaturesForAndroid()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);

            // UI の「OpenXR Feature Groups > Meta Quest」に相当する
            var featureSet = OpenXRFeatureSetManager.GetFeatureSetWithId(BuildTargetGroup.Android, MetaFeatureSetId);
            if (featureSet != null)
            {
                featureSet.isEnabled = true;
                OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.Android);
            }

            var results = s_RequiredFeatures.Select(f =>
            {
                var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, f.id);
                if (feature == null) return $"{f.label}=見つからない";
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                return $"{f.label}=有効";
            }).ToArray();

            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXrSettings != null) EditorUtility.SetDirty(openXrSettings);

            return (featureSet != null ? "Meta Quest 機能グループ=有効, " : "Meta Quest 機能グループ=見つからない, ") + string.Join(", ", results);
        }

        private static XRGeneralSettingsPerBuildTarget GetPerBuildTargetSettings()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings);
            return settings;
        }

        private static string[] LoaderNames(XRGeneralSettings settings)
        {
            var loaders = settings != null && settings.Manager != null ? settings.Manager.activeLoaders : null;
            return loaders != null ? loaders.Where(l => l != null).Select(l => l.GetType().Name).ToArray() : Array.Empty<string>();
        }
    }
}
