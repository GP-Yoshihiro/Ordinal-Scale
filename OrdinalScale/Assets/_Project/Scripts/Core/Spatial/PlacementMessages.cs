namespace OrdinalScale.Core.Spatial
{
    /// <summary>
    /// 配置できない理由・配置結果を利用者向けの文に直す。iPhone のオーバーレイと配置結果の表示で共用し、
    /// 端末ごとに文言がずれないようにする。
    /// </summary>
    public static class PlacementMessages
    {
        /// <summary>利用者が次にすべきこと。None は空文字。</summary>
        public static string Hint(PlacementBlockReason reason)
        {
            switch (reason)
            {
                case PlacementBlockReason.None: return string.Empty;
                case PlacementBlockReason.CameraPermissionDenied: return "設定アプリ > OrdinalScale > カメラ をオンにして再起動してください";
                case PlacementBlockReason.TrackingUnsupported: return "この端末ではAR追跡を使えません";
                case PlacementBlockReason.TrackingInitializing: return "AR追跡を準備中です。端末をゆっくり動かしてください";
                case PlacementBlockReason.TrackingLimited: return "追跡が不安定です（暗い・模様が少ない・動きが速い）";
                case PlacementBlockReason.NoPlaneDetected: return "床をゆっくり映して平面を検出させてください";
                case PlacementBlockReason.TargetNotOnPlane: return "検出された平面の上をタップしてください";
                case PlacementBlockReason.TargetNotHorizontal: return "床や机の上など、上向きの水平面をタップしてください";
                default: return "配置できません（" + reason + "）";
            }
        }

        /// <summary>配置操作1回分の結果を1行で表す。日本語が表示できない場合に備え、英語の結果コードを先頭に置く。</summary>
        public static string Describe(in PlacementAttemptResult result)
        {
            var prefix = "#" + result.AttemptNumber + " ";
            switch (result.Outcome)
            {
                case PlacementOutcome.Placed: return prefix + "Placed: 敵を配置しました";
                case PlacementOutcome.Moved: return prefix + "Moved: 敵を移動しました";
                default: return prefix + "Blocked " + result.Reason + ": " + Hint(result.Reason);
            }
        }
    }
}
