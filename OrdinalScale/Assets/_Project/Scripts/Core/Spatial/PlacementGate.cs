namespace OrdinalScale.Core.Spatial
{
    /// <summary>敵を配置できない理由。None なら配置してよい。</summary>
    public enum PlacementBlockReason
    {
        None,
        CameraPermissionDenied,
        TrackingUnsupported,
        TrackingInitializing,
        TrackingLimited,
        NoPlaneDetected,
        /// <summary>選んだ位置（タップ先など）が検出済みの平面上にない。</summary>
        TargetNotOnPlane,
    }

    /// <summary>
    /// 配置可否の判定。iPhone先行検証の失敗条件（権限なし・追跡未準備・平面未検出・無効な位置）を
    /// デバイス非依存のルールとして持つ。Quest でも同じ判定を使う。
    /// </summary>
    public static class PlacementGate
    {
        /// <summary>
        /// 現在の状態で配置操作を受け付けられるか。優先順位は「権限 → 対応端末 → 追跡 → 平面」で、
        /// 利用者が最初に直すべき原因を1つだけ返す。
        /// </summary>
        public static PlacementBlockReason Evaluate(in SpatialStatus status)
        {
            if (status.CameraPermission == CameraPermission.Denied) return PlacementBlockReason.CameraPermissionDenied;

            switch (status.TrackingPhase)
            {
                case TrackingPhase.Unsupported: return PlacementBlockReason.TrackingUnsupported;
                case TrackingPhase.Initializing: return PlacementBlockReason.TrackingInitializing;
                case TrackingPhase.Limited: return PlacementBlockReason.TrackingLimited;
            }

            return status.HorizontalPlaneCount > 0 ? PlacementBlockReason.None : PlacementBlockReason.NoPlaneDetected;
        }

        /// <summary>
        /// 特定の位置への配置を試みたときの判定。状態が配置可能でも、選んだ位置が平面上でなければ拒否する。
        /// </summary>
        public static PlacementBlockReason EvaluateTarget(in SpatialStatus status, bool targetIsOnPlane)
        {
            var reason = Evaluate(status);
            if (reason != PlacementBlockReason.None) return reason;
            return targetIsOnPlane ? PlacementBlockReason.None : PlacementBlockReason.TargetNotOnPlane;
        }
    }
}
