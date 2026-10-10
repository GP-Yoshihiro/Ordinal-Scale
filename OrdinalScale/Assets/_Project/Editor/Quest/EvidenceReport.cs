using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// OK/NG の一覧を作り、リポジトリの Claude/reports/evidence/ とクリップボードに残す（Quest 用）。
    /// iOS 用（IOSARSetup の内部クラス）と同じ書式。アセンブリが分かれているため複製している。
    /// </summary>
    internal sealed class EvidenceReport
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _title;

        public int FailureCount { get; private set; }
        public string Text => _sb.ToString();

        public EvidenceReport(string title)
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
            if (!Application.isBatchMode) EditorGUIUtility.systemCopyBuffer = Text;

            try
            {
                // Application.dataPath = <repo>/OrdinalScale/Assets
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Claude", "reports", "evidence"));
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd-HHmmss}_{_title.Replace(' ', '_')}.txt");
                File.WriteAllText(path, Text, new UTF8Encoding(false));
                savedTo = $"保存先: {path}";
            }
            catch (Exception e)
            {
                savedTo = $"ファイル保存に失敗: {e.Message}";
            }
        }
    }
}
