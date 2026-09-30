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
        /// <summary>選んだ位置は検出済みの面だが、上向きの水平面（床・机の天板など）ではない（壁・天井など）。</summary>
        TargetNotHorizontal,
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
        /// 特定の位置への配置を試みたときの判定。状態が配置可能でも、選んだ位置が
        /// 検出済みの上向き水平面でなければ拒否する。状態の問題を先に返す。
        /// </summary>
        public static PlacementBlockReason EvaluateTarget(in SpatialStatus status, SurfaceKind targetSurface)
        {
            var reason = Evaluate(status);
            if (reason != PlacementBlockReason.None) return reason;

            switch (targetSurface)
            {
                case SurfaceKind.HorizontalUp: return PlacementBlockReason.None;
                case SurfaceKind.None: return PlacementBlockReason.TargetNotOnPlane;
                default: return PlacementBlockReason.TargetNotHorizontal;
            }
        }

        /// <summary>選んだ位置が上向き水平面か否かだけが分かる場合の簡易版。</summary>
        public static PlacementBlockReason EvaluateTarget(in SpatialStatus status, bool targetIsOnPlane)
            => EvaluateTarget(status, targetIsOnPlane ? SurfaceKind.HorizontalUp : SurfaceKind.None);
    }
}
