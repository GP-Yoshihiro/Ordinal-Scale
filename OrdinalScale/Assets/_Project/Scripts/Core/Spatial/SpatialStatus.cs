namespace OrdinalScale.Core.Spatial
{
    /// <summary>カメラ使用許可の状態。</summary>
    public enum CameraPermission
    {
        /// <summary>まだ問い合わせ中、または取得できない（許可ダイアログ表示中など）。</summary>
        Unknown,
        Granted,
        Denied,
    }

    /// <summary>
    /// 空間追跡の段階。ARKit / Meta XR / Editor の差を吸収した共通表現。
    /// 各プラットフォームの詳細な状態は Platform 層でこの4段階に写像する。
    /// </summary>
    public enum TrackingPhase
    {
        /// <summary>端末またはOSがAR追跡に対応していない。</summary>
        Unsupported,
        /// <summary>起動直後など、追跡の準備中。</summary>
        Initializing,
        /// <summary>追跡はしているが品質が低い（暗い・特徴が少ない・動きが速い・位置の再推定中）。</summary>
        Limited,
        /// <summary>正常に追跡している。</summary>
        Tracking,
    }

    /// <summary>空間認識の状態をまとめたスナップショット。配置可否の判定に使う。</summary>
    public readonly struct SpatialStatus
    {
        public CameraPermission CameraPermission { get; }
        public TrackingPhase TrackingPhase { get; }

        /// <summary>検出済みの水平面（上向き）の数。</summary>
        public int HorizontalPlaneCount { get; }

        public SpatialStatus(CameraPermission cameraPermission, TrackingPhase trackingPhase, int horizontalPlaneCount)
        {
            // 実際に追跡中ならカメラは稼働中。WebCam API の古い拒否値より実測状態を優先する。
            CameraPermission = trackingPhase == TrackingPhase.Tracking
                ? CameraPermission.Granted
                : cameraPermission;
            TrackingPhase = trackingPhase;
            HorizontalPlaneCount = horizontalPlaneCount < 0 ? 0 : horizontalPlaneCount;
        }
    }
}
