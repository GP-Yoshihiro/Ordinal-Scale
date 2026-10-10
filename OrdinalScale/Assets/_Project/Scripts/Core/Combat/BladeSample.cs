using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// 剣の刃の1フレーム分の観測値（UnityEngine 非依存）。
    /// Platform 層の ISwordPoseSource（Quest のコントローラ／Editor のマウス）が作る BladePose をこの形に直し、
    /// Q1 で追加する振り判定（SwingDetector）と命中判定（SwordHitJudge）へ渡す。
    /// 座標は Unity のワールド座標（m）、時刻は秒。
    /// </summary>
    public readonly struct BladeSample
    {
        /// <summary>これより短い時間差では速さを計算しない（同一フレームの重複サンプル対策）。</summary>
        public const double MinDeltaSeconds = 1e-4;

        public double Time { get; }
        public bool IsTracked { get; }

        public float HiltX { get; }
        public float HiltY { get; }
        public float HiltZ { get; }
        public float TipX { get; }
        public float TipY { get; }
        public float TipZ { get; }

        public BladeSample(double time, bool isTracked,
            float hiltX, float hiltY, float hiltZ,
            float tipX, float tipY, float tipZ)
        {
            Time = time;
            IsTracked = isTracked;
            HiltX = hiltX;
            HiltY = hiltY;
            HiltZ = hiltZ;
            TipX = tipX;
            TipY = tipY;
            TipZ = tipZ;
        }

        /// <summary>追跡していない時刻のサンプル（位置は意味を持たない）。</summary>
        public static BladeSample Untracked(double time)
        {
            return new BladeSample(time, false, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        /// <summary>手元から刃先までの長さ（m）。</summary>
        public float Length
        {
            get
            {
                var dx = TipX - HiltX;
                var dy = TipY - HiltY;
                var dz = TipZ - HiltZ;
                return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }

        /// <summary>
        /// 2つのサンプル間の刃先の平均の速さ（m/s）。振り判定（C1・C2）の入力になる。
        /// どちらかが追跡外、または時刻が進んでいない場合は速さを決められないので false。
        /// 追跡の途切れ・復帰による位置の飛びを除外する処理は Q1 の SwingDetector が持つ（ここでは計算だけ）。
        /// </summary>
        public static bool TryTipSpeed(in BladeSample previous, in BladeSample current, out float metersPerSecond)
        {
            var dt = current.Time - previous.Time;
            if (!previous.IsTracked || !current.IsTracked || dt < MinDeltaSeconds)
            {
                metersPerSecond = 0f;
                return false;
            }

            var dx = current.TipX - previous.TipX;
            var dy = current.TipY - previous.TipY;
            var dz = current.TipZ - previous.TipZ;
            metersPerSecond = (float)(Math.Sqrt(dx * dx + dy * dy + dz * dz) / dt);
            return true;
        }
    }
}
