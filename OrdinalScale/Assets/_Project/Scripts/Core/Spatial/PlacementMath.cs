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

        /// <summary>
        /// 視線の水平成分がこれより短いと（真下・真上を見ている）前方が決まらないとみなす。
        /// 単位ベクトルの水平成分なので、約84°以上うつむく・見上げると false になる。
        /// </summary>
        public const float MinHorizontalForward = 0.1f;

        /// <summary>
        /// 頭の位置 (headX, headZ) から、視線 (forwardX, forwardZ) の水平方向へ distance(m) 進んだ床上の点。
        /// Quest の固定配置（起動時に正面へ敵を置く）で使う。視線の上下成分は無視する。
        /// 前方が決まらない、または距離が正でない場合は false。
        /// </summary>
        public static bool TryPointInFront(float headX, float headZ, float forwardX, float forwardZ, float distance,
            out float x, out float z)
        {
            var length = (float)Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
            if (distance <= 0f || length < MinHorizontalForward)
            {
                x = headX;
                z = headZ;
                return false;
            }

            x = headX + forwardX / length * distance;
            z = headZ + forwardZ / length * distance;
            return true;
        }
    }
}
