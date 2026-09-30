using System;

namespace OrdinalScale.Core.Spatial
{
    /// <summary>配置に使う純粋な計算。Unity の座標系（Y上・Z前・左手系）を前提にする。</summary>
    public static class PlacementMath
    {
        /// <summary>これより近いと向きが定まらないとみなす水平距離（m）。</summary>
        public const float MinFacingDistance = 0.01f;

        /// <summary>
        /// (fromX, fromZ) に立つ敵が (toX, toZ) を正面に見るためのY軸回転（度）。
        /// Unity の Quaternion.Euler(0, yaw, 0) にそのまま渡せる値（+Z が 0°、+X が 90°）。
        /// 2点がほぼ同じ位置なら向きが決まらないので false。
        /// </summary>
        public static bool TryYawDegreesToFace(float fromX, float fromZ, float toX, float toZ, out float yawDegrees)
        {
            var dx = toX - fromX;
            var dz = toZ - fromZ;
            if (dx * dx + dz * dz < MinFacingDistance * MinFacingDistance)
            {
                yawDegrees = 0f;
                return false;
            }

            yawDegrees = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
            return true;
        }
    }
}
