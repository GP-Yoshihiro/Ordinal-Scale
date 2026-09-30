using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
#if OS_HAS_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// iPhone AR 先行検証（S1）用の Editor メニュー。
    /// 1. Player 設定の適用 → 2. 設定の検証 → 3. iOS 開発ビルド の順に使う。
    /// 検証とビルドの結果は Claude/reports/evidence/ にテキストで残し、S1 の証拠にする。
    /// </summary>
    public static class IOSARSetup
    {
        /// <summary>チームで固定する Unity 版。ProjectSettings/ProjectVersion.txt と合わせる。</summary>
        public const string ExpectedUnityVersion = "6000.5.10f1";

        public const string ScenePath = "Assets/_Project/Scenes/iPhone_AR_S1.unity";
        public const string DefaultBundleId = "com.gpyoshihiro.ordinalscale";
        public const string BuildPath = "Builds/iOS";

        private const string CameraUsageDescription = "敵を現実の空間に重ねて表示するためにカメラを使用します。";
        private const string MenuRoot = "OrdinalScale/iOS AR/";

        private static readonly string[] s_Packages =
        {
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
            "com.unity.xr.arfoundation",
            "com.unity.xr.arkit",
            "com.unity.xr.management",
            "com.unity.xr.core-utils",
            "com.unity.test-framework",
        };

        [MenuItem(MenuRoot + "1. Player設定を適用", priority = 1)]
        public static void ApplyPlayerSettings()
        {
            var ios = NamedBuildTarget.iOS;

            // 利用者が既に独自の Bundle ID を入れていれば上書きしない
            var id = PlayerSettings.GetApplicationIdentifier(ios);
            if (string.IsNullOrEmpty(id) || id.StartsWith("com.DefaultCompany", StringComparison.OrdinalIgnoreCase))
            {
                PlayerSettings.SetApplicationIdentifier(ios, DefaultBundleId);
            }

            // ARKit はこれが空だとビルドを失敗させる
            if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
            {
                PlayerSettings.iOS.cameraUsageDescription = CameraUsageDescription;
            }

            // 検証中は縦持ちに固定し、画面の証拠を比べやすくする
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            var sceneAdded = EnsureSceneIsFirstInBuild(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[OrdinalScale] iOS Player設定を適用しました。Bundle ID = {PlayerSettings.GetApplicationIdentifier(ios)}" +
                      (sceneAdded ? $"、ビルド対象シーン = {ScenePath}" : $"。{ScenePath} がまだ無いため、シーン作成後にもう一度実行してください。"));
        }

        [MenuItem(MenuRoot + "2. 設定を検証（証拠を保存）", priority = 2)]
        public static void Validate()
        {
            var r = new CheckReport("S1 設定検証");

            r.Check("Unity版", Application.unityVersion == ExpectedUnityVersion,
                $"{Application.unityVersion}（固定版: {ExpectedUnityVersion}）");
            r.Check("ビルドターゲット", EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS,
                EditorUserBuildSettings.activeBuildTarget.ToString());

            foreach (var name in s_Packages)
            {
                var info = PackageInfo.FindForAssetPath($"Packages/{name}");
                r.Check($"Package {name}", info != null, info != null ? info.version : "未導入");
            }

            CheckRenderPipeline(r);
            CheckXRLoader(r);

            var bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            r.Check("Bundle ID", !string.IsNullOrEmpty(bundleId) && !bundleId.StartsWith("com.DefaultCompany", StringComparison.OrdinalIgnoreCase), bundleId);
            r.Check("カメラ使用目的の文言", !string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription), PlayerSettings.iOS.cameraUsageDescription);

            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            r.Check("ビルド対象の先頭シーン", firstScene != null && firstScene.path == ScenePath, firstScene != null ? firstScene.path : "なし");

            r.Info("iOS最小バージョン", PlayerSettings.iOS.targetOSVersionString);
            r.Info("Xcode", ReadXcodeVersion());

            r.Finish(out var fileName);
            Debug.Log($"[OrdinalScale] 検証結果: NG {r.FailureCount}件。{fileName}\n{r.Text}");
        }

        [MenuItem(MenuRoot + "3. iOS開発ビルドを出力（Xcodeプロジェクト）", priority = 3)]
        public static void BuildIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                EditorUtility.DisplayDialog("OrdinalScale", "先に File > Build Profiles で iOS に切り替えてください（Switch Platform）。", "OK");
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("OrdinalScale", "ビルド対象シーンがありません。メニュー「1. Player設定を適用」を実行してください。", "OK");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;

            var r = new CheckReport("S1 iOSビルド");
            r.Check("ビルド結果", s.result == BuildResult.Succeeded, s.result.ToString());
            r.Info("出力先", Path.GetFullPath(BuildPath));
            r.Info("所要時間", s.totalTime.ToString(@"hh\:mm\:ss"));
            r.Info("エラー／警告", $"{s.totalErrors} / {s.totalWarnings}");
            r.Info("シーン", string.Join(", ", scenes));
            r.Info("Unity版", Application.unityVersion);
            r.Info("Bundle ID", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));
            r.Info("Xcode", ReadXcodeVersion());
            r.Finish(out var fileName);

            Debug.Log($"[OrdinalScale] iOSビルド {s.result}。{fileName}\n{r.Text}");
            if (s.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(BuildPath);
        }

        private static bool EnsureSceneIsFirstInBuild(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return false;

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            return true;
        }

        private static void CheckRenderPipeline(CheckReport r)
        {
            // 既定と各品質レベルで使われる URP アセットをすべて調べる（iOS 用の品質レベルが別アセットを使う場合がある）
            var assets = new List<RenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline != null) assets.Add(GraphicsSettings.defaultRenderPipeline);
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                var a = QualitySettings.GetRenderPipelineAssetAt(i);
                if (a != null && !assets.Contains(a)) assets.Add(a);
            }

            r.Check("URP アセット", assets.Count > 0 && assets.All(a => a.GetType().Name.Contains("Universal")),
                assets.Count > 0 ? string.Join(", ", assets.Select(a => $"{a.name}({a.GetType().Name})")) : "未設定（Built-in RP）");

            foreach (var asset in assets)
            {
                // URP の型に直接依存しないよう、シリアライズ済みフィールドから Renderer と Renderer Feature を読む
                var list = new SerializedObject(asset).FindProperty("m_RendererDataList");
                if (list == null || !list.isArray)
                {
                    r.Info($"AR Background ({asset.name})", "Renderer一覧を読めないため手動で確認してください");
                    continue;
                }

                for (var i = 0; i < list.arraySize; i++)
                {
                    var data = list.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (data == null) continue;

                    var features = new SerializedObject(data).FindProperty("m_RendererFeatures");
                    var found = false;
                    if (features != null && features.isArray)
                    {
                        for (var j = 0; j < features.arraySize && !found; j++)
                        {
                            var f = features.GetArrayElementAtIndex(j).objectReferenceValue;
                            found = f != null && f.GetType().Name == "ARBackgroundRendererFeature";
                        }
                    }

                    r.Check($"AR Background Renderer Feature ({data.name})", found,
                        found ? "追加済み" : "未追加（カメラ映像が映らず黒くなる）");
                }
            }
        }

        private static void CheckXRLoader(CheckReport r)
        {
#if OS_HAS_XR_MANAGEMENT
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.iOS);
            var loaders = settings != null && settings.Manager != null ? settings.Manager.activeLoaders : null;
            var names = loaders != null ? loaders.Where(l => l != null).Select(l => l.GetType().Name).ToArray() : Array.Empty<string>();
            r.Check("XR Plug-in (iOS) Apple ARKit", names.Contains("ARKitLoader"), names.Length > 0 ? string.Join(", ", names) : "なし");
            r.Check("XR Initialize on Startup (iOS)", settings != null && settings.InitManagerOnStart, settings != null ? settings.InitManagerOnStart.ToString() : "設定なし");
#else
            r.Check("XR Plug-in Management", false, "未導入（AR Foundation を入れると一緒に入る）");
#endif
        }

        private static string ReadXcodeVersion()
        {
            if (Application.platform != RuntimePlatform.OSXEditor) return "（Mac以外のため取得しない）";

            try
            {
                using (var p = Process.Start(new ProcessStartInfo("xcodebuild", "-version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }))
                {
                    if (p == null) return "取得失敗";
                    var output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(10000);
                    return output.Replace('\n', ' ').Trim();
                }
            }
            catch (Exception e)
            {
                return $"取得失敗: {e.Message}";
            }
        }

        /// <summary>OK/NG の一覧を作り、リポジトリの Claude/reports/evidence/ とクリップボードに残す。</summary>
        private sealed class CheckReport
        {
            private readonly StringBuilder _sb = new StringBuilder();
            private readonly string _title;

            public int FailureCount { get; private set; }
            public string Text => _sb.ToString();

            public CheckReport(string title)
            {
                _title = title;
                _sb.AppendLine($"# {title}  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _sb.AppendLine($"Machine: {SystemInfo.operatingSystem} / {SystemInfo.systemMemorySize}MB RAM");
            }

            public void Check(string item, bool ok, string detail)
            {
                if (!ok) FailureCount++;
                _sb.AppendLine($"[{(ok ? "OK" : "NG")}] {item}: {detail}");
            }

            public void Info(string item, string detail)
            {
                _sb.AppendLine($"[--] {item}: {detail}");
            }

            public void Finish(out string savedTo)
            {
                _sb.AppendLine($"NG: {FailureCount}");
                EditorGUIUtility.systemCopyBuffer = Text;

                try
                {
                    // Application.dataPath = <repo>/OrdinalScale/Assets
                    var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Claude", "reports", "evidence"));
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd-HHmmss}_{_title.Replace(' ', '_')}.txt");
                    File.WriteAllText(path, Text, new UTF8Encoding(false));
                    savedTo = $"保存先: {path}（クリップボードにもコピー済み）";
                }
                catch (Exception e)
                {
                    savedTo = $"ファイル保存に失敗（クリップボードにはコピー済み）: {e.Message}";
                }
            }
        }
    }
}
