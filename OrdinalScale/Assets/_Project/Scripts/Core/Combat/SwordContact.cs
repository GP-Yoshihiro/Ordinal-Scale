using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// 敵の体の当たり判定。足元 (BaseX, BaseY, BaseZ) に立つ、縦向きのカプセル1つ（条件 C6：部位差なし）。
    /// Gameplay の仮モデル（EnemyPlaceholder：高さ1.6m・直径0.5m）と同じ形を想定する。
    /// </summary>
    public readonly struct BodyCapsule
    {
        public float BaseX { get; }
        public float BaseY { get; }
        public float BaseZ { get; }

        /// <summary>全高（m）。半球部分を含む。</summary>
        public float Height { get; }

        public float Radius { get; }

        public BodyCapsule(float baseX, float baseY, float baseZ, float height, float radius)
        {
            if (!(radius > 0f)) throw new ArgumentOutOfRangeException(nameof(radius), radius, "Must be positive.");
            if (!(height >= radius * 2f)) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be at least the diameter.");
            BaseX = baseX;
            BaseY = baseY;
            BaseZ = baseZ;
            Height = height;
            Radius = radius;
        }

        /// <summary>中心線の下端（下の半球の中心）の高さ。</summary>
        public float AxisBottomY => BaseY + Radius;

        /// <summary>中心線の上端（上の半球の中心）の高さ。</summary>
        public float AxisTopY => BaseY + Height - Radius;
    }

    /// <summary>
    /// 刃（手元→刃先の線分）と体のカプセルの接触判定。UnityEngine を使わない純粋な幾何計算。
    /// 前フレームと今フレームの刃の間を補間して調べる（掃引）ので、1フレームで体を通り抜ける速い振りも接触として拾う。
    /// 回転を直線補間で近似するため、補間間隔（SwordTuning.SweepStepMeters）より細かい形の差は区別しない。
    /// </summary>
    public static class SwordContact
    {
        /// <summary>
        /// このフレームで刃が体に触れているか。previous が無い・追跡外なら current だけで判定する。
        /// current が追跡外なら触れていない扱い（位置が分からないため）。
        /// </summary>
        public static bool Touches(in BladeSample previous, bool hasPrevious, in BladeSample current,
            in BodyCapsule body, SwordTuning tuning)
        {
            if (!current.IsTracked) return false;
            tuning = tuning ?? SwordTuning.Default;
            var reach = body.Radius + tuning.BladeRadius;

            if (!hasPrevious || !previous.IsTracked)
                return SegmentToAxisDistance(current.HiltX, current.HiltY, current.HiltZ, current.TipX, current.TipY, current.TipZ, body) <= reach;

            // 刃先と手元のうち大きく動いた方の移動量で分割数を決める
            var move = Math.Max(
                Distance(previous.TipX, previous.TipY, previous.TipZ, current.TipX, current.TipY, current.TipZ),
                Distance(previous.HiltX, previous.HiltY, previous.HiltZ, current.HiltX, current.HiltY, current.HiltZ));
            var steps = (int)Math.Ceiling(move / tuning.SweepStepMeters);
            if (steps < 1) steps = 1;
            if (steps > tuning.MaxSweepSteps) steps = tuning.MaxSweepSteps;

            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var d = SegmentToAxisDistance(
                    Lerp(previous.HiltX, current.HiltX, t), Lerp(previous.HiltY, current.HiltY, t), Lerp(previous.HiltZ, current.HiltZ, t),
                    Lerp(previous.TipX, current.TipX, t), Lerp(previous.TipY, current.TipY, t), Lerp(previous.TipZ, current.TipZ, t),
                    body);
                if (d <= reach) return true;
            }

            return false;
        }

        /// <summary>線分 (a→b) とカプセルの中心線（縦の線分）の最短距離。</summary>
        public static float SegmentToAxisDistance(float ax, float ay, float az, float bx, float by, float bz, in BodyCapsule body)
        {
            return SegmentSegmentDistance(
                ax, ay, az, bx, by, bz,
                body.BaseX, body.AxisBottomY, body.BaseZ, body.BaseX, body.AxisTopY, body.BaseZ);
        }

        /// <summary>2つの線分の最短距離（Ericson『Real-Time Collision Detection』5.1.9 の方法）。</summary>
        public static float SegmentSegmentDistance(
            float p1x, float p1y, float p1z, float q1x, float q1y, float q1z,
            float p2x, float p2y, float p2z, float q2x, float q2y, float q2z)
        {
            const double eps = 1e-12;
            double d1x = q1x - p1x, d1y = q1y - p1y, d1z = q1z - p1z;
            double d2x = q2x - p2x, d2y = q2y - p2y, d2z = q2z - p2z;
            double rx = p1x - p2x, ry = p1y - p2y, rz = p1z - p2z;
            var a = d1x * d1x + d1y * d1y + d1z * d1z;
            var e = d2x * d2x + d2y * d2y + d2z * d2z;
            var f = d2x * rx + d2y * ry + d2z * rz;

            double s, t;
            if (a <= eps && e <= eps)
            {
                s = 0;
                t = 0;
            }
            else if (a <= eps)
            {
                s = 0;
                t = Clamp01(f / e);
            }
            else
            {
                var c = d1x * rx + d1y * ry + d1z * rz;
                if (e <= eps)
                {
                    t = 0;
                    s = Clamp01(-c / a);
                }
                else
                {
                    var b = d1x * d2x + d1y * d2y + d1z * d2z;
                    var denom = a * e - b * b;
                    s = denom > eps ? Clamp01((b * f - c * e) / denom) : 0;
                    t = (b * s + f) / e;
                    if (t < 0)
                    {
                        t = 0;
                        s = Clamp01(-c / a);
                    }
                    else if (t > 1)
                    {
                        t = 1;
                        s = Clamp01((b - c) / a);
                    }
                }
            }

            var dx = (p1x + d1x * s) - (p2x + d2x * t);
            var dy = (p1y + d1y * s) - (p2y + d2y * t);
            var dz = (p1z + d1z * s) - (p2z + d2z * t);
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Distance(float ax, float ay, float az, float bx, float by, float bz)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var dz = bz - az;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
