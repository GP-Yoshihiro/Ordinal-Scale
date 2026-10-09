using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// Quest 3／3S 用の XR パッケージを Unity の Package Manager で追加する。
    /// manifest.json と packages-lock.json は Unity に書かせ、手書きしない（Claude/docs/unity-project-files.md の方針）。
    /// このメニューは XR パッケージが未導入でもコンパイルされる（後続のメニューは導入後に現れる）。
    /// 版の選定理由は Claude/docs/quest-xr-setup.md。
    /// </summary>
    public static class QuestPackageInstaller
    {
        /// <summary>Unity 6000.5 向けに公開されている版（2026-10-09 時点の Unity マニュアルで確認）。</summary>
        public static readonly string[] Packages =
        {
            "com.unity.xr.openxr@1.17.1",
            "com.unity.xr.meta-openxr@2.5.1",
        };

        private static AddAndRemoveRequest s_Request;

        [MenuItem("OrdinalScale/Quest MR/1. XRパッケージを追加（OpenXR・Meta）", priority = 101)]
        public static void AddPackages()
        {
            if (s_Request != null && !s_Request.IsCompleted)
            {
                Debug.LogWarning("[OrdinalScale] パッケージの追加が実行中です。完了を待ってください。");
                return;
            }

            Debug.Log($"[OrdinalScale] パッケージを追加します: {string.Join(", ", Packages)}");
            s_Request = Client.AddAndRemove(Packages, null);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (s_Request == null || !s_Request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (s_Request.Status == StatusCode.Success)
            {
                foreach (var p in s_Request.Result)
                {
                    if (p.name.StartsWith("com.unity.xr")) Debug.Log($"[OrdinalScale] 導入済み: {p.name} {p.version}");
                }

                Debug.Log("[OrdinalScale] XRパッケージの追加が完了しました。再コンパイル後、メニュー「OrdinalScale/Quest MR/2. Android・OpenXR設定を適用」へ進んでください。" +
                          "Packages/manifest.json と packages-lock.json の変更はコミットしてください。");
            }
            else
            {
                Debug.LogError($"[OrdinalScale] パッケージの追加に失敗しました: {s_Request.Error?.message}");
            }

            s_Request = null;
        }
    }
}
