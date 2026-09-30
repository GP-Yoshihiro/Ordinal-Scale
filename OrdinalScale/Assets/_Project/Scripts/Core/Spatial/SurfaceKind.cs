using System;

namespace OrdinalScale.Core.Spatial
{
    /// <summary>レイが当たった面の種類。配置してよいのは HorizontalUp だけ。</summary>
    public enum SurfaceKind
    {
        /// <summary>検出済みの面に当たらなかった。</summary>
        None,
        /// <summary>上向きの水平面（床・机の天板など）。</summary>
        HorizontalUp,
        /// <summary>下向きの水平面（天井など）。</summary>
        HorizontalDown,
        /// <summary>垂直面（壁など）。</summary>
        Vertical,
        /// <summary>斜面など、上のどれにも当てはまらない面。</summary>
        Other,
    }

    /// <summary>
    /// 面の法線から面の種類を決める。AR SDK が面の向きを教えてくれない環境
    /// （Editor の物理コライダーなど）で使う。
    /// </summary>
    public static class SurfaceClassifier
    {
        /// <summary>水平・垂直とみなす角度の許容誤差（度）。</summary>
        public const float DefaultToleranceDegrees = 10f;

        /// <param name="normalY">正規化済み法線の上方向成分（-1〜1）。</param>
        /// <param name="toleranceDegrees">水平・垂直とみなす許容角度。</param>
        public static SurfaceKind Classify(float normalY, float toleranceDegrees = DefaultToleranceDegrees)
        {
            if (float.IsNaN(normalY)) return SurfaceKind.None;

            // 法線と真上の角度が許容誤差以内なら水平面。cos は角度が小さいほど大きい
            var horizontalThreshold = (float)Math.Cos(toleranceDegrees * Math.PI / 180.0);
            if (normalY >= horizontalThreshold) return SurfaceKind.HorizontalUp;
            if (normalY <= -horizontalThreshold) return SurfaceKind.HorizontalDown;

            // 法線が水平に近い（上方向成分がほぼ0）なら垂直面
            var verticalThreshold = (float)Math.Sin(toleranceDegrees * Math.PI / 180.0);
            if (Math.Abs(normalY) <= verticalThreshold) return SurfaceKind.Vertical;

            return SurfaceKind.Other;
        }
    }
}
