using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// Quest の APK ビルドの間だけ、画面の向きを Landscape Left にする。
    ///
    /// 背景：PlayerSettings.defaultInterfaceOrientation は Android と iOS で共有される1つの値。
    /// iPhone の検証用に IOSARSetup が Portrait（縦持ち）に固定している一方、OpenXR の Meta Quest Support 機能は
    /// ビルド前の検証で Landscape Left 以外をエラーにする（"Meta Quest HMDs only support Landscape Left orientation."、
    /// com.unity.xr.openxr 1.17.1 の MetaQuestFeature）。どちらかに固定すると他方のビルドが壊れるため、
    /// Quest のビルドの間だけ切り替え、成功・失敗・例外にかかわらず元の値に戻す。
    ///
    /// Unity が途中で落ちて finally が走らなかった場合に備え、変更前の値を Library/ に書いておき、
    /// 次に Editor を開いたとき（ビルド中でなければ）自動で元に戻す。
    /// </summary>
    [InitializeOnLoad]
    internal sealed class QuestBuildOrientation : IDisposable
    {
        /// <summary>Meta Quest が要求する向き。</summary>
        public const UIOrientation Required = UIOrientation.LandscapeLeft;

        // Library/ はマシンごとでコミットされない。プロジェクトを開き直しても残る
        private static string BackupPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "OrdinalScale_QuestOrientationBackup.txt"));

        private readonly UIOrientation _original;
        private readonly bool _changed;
        private bool _disposed;

        static QuestBuildOrientation()
        {
            // 前回のビルド中に Editor が落ちた場合の復旧。ビルド中のドメイン再読み込みでは戻さない
            if (BuildPipeline.isBuildingPlayer) return;
            EditorApplication.delayCall += RecoverIfInterrupted;
        }

        private QuestBuildOrientation(UIOrientation original, bool changed)
        {
            _original = original;
            _changed = changed;
        }

        /// <summary>変更前の向き（ログ・証拠用）。</summary>
        public UIOrientation Original => _original;

        /// <summary>今回のビルドのために向きを変えたか（もともと Landscape Left なら false）。</summary>
        public bool Changed => _changed;

        /// <summary>
        /// Quest 用の向きに切り替える。using で囲み、BuildPipeline.BuildPlayer を中で呼ぶ。
        /// 自動回転（AutoRotation）が設定されている場合も、OpenXR の検証が求める Landscape Left に固定してから戻す。
        /// </summary>
        public static QuestBuildOrientation Apply()
        {
            var original = PlayerSettings.defaultInterfaceOrientation;
            if (original == Required) return new QuestBuildOrientation(original, false);

            WriteBackup(original);
            PlayerSettings.defaultInterfaceOrientation = Required;
            Debug.Log($"[OrdinalScale] Quest ビルドのため画面の向きを一時的に {original} → {Required} に変更します（ビルド後に戻します）。");
            return new QuestBuildOrientation(original, true);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (!_changed) return;

            Restore(_original, "Quest ビルド後");
        }

        /// <summary>前回の一時変更が残っているか（設定検証で使う）。</summary>
        public static bool HasPendingBackup(out UIOrientation original)
        {
            original = default;
            if (!File.Exists(BackupPath)) return false;
            return Enum.TryParse(File.ReadAllText(BackupPath).Trim(), out original);
        }

        private static void RecoverIfInterrupted()
        {
            if (BuildPipeline.isBuildingPlayer) return;
            if (!File.Exists(BackupPath)) return;

            if (HasPendingBackup(out var original))
            {
                Debug.LogWarning("[OrdinalScale] 前回の Quest ビルドが途中で終わり、画面の向きが戻っていませんでした。元の値に戻します。");
                Restore(original, "前回の中断からの復旧");
            }
            else
            {
                // 読めない控えは残しても役に立たないので消す（向きは変えない）
                DeleteBackup();
            }
        }

        private static void Restore(UIOrientation original, string when)
        {
            PlayerSettings.defaultInterfaceOrientation = original;
            // ビルド中に ProjectSettings.asset が Landscape Left のまま保存されていても、元の値で書き直す
            AssetDatabase.SaveAssets();
            DeleteBackup();
            Debug.Log($"[OrdinalScale] 画面の向きを {original} に戻しました（{when}）。");
        }

        private static void WriteBackup(UIOrientation original)
        {
            try
            {
                File.WriteAllText(BackupPath, original.ToString());
            }
            catch (Exception e)
            {
                // 控えが書けなくても、通常の復元（Dispose）は動く
                Debug.LogWarning($"[OrdinalScale] 画面の向きの控えを保存できませんでした: {e.Message}");
            }
        }

        private static void DeleteBackup()
        {
            try
            {
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OrdinalScale] 画面の向きの控えを削除できませんでした: {e.Message}");
            }
        }
    }
}
