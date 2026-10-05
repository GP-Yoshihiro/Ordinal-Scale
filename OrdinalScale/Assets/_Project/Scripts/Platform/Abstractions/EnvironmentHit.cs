using OrdinalScale.Core.Spatial;
using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>現実環境へのレイキャスト結果。</summary>
    public readonly struct EnvironmentHit
    {
        /// <summary>当たった位置。回転の up は面の法線方向。</summary>
        public Pose Pose { get; }

        /// <summary>当たった面の種類。配置してよいのは HorizontalUp だけ（Core の PlacementGate で判定）。</summary>
        public SurfaceKind Surface { get; }

        /// <summary>レイの始点からの距離（m）。</summary>
        public float Distance { get; }

        public EnvironmentHit(Pose pose, SurfaceKind surface, float distance)
        {
            Pose = pose;
            Surface = surface;
            Distance = distance;
        }
    }
}
