using OrdinalScale.Core.Combat;
using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 剣の刃の姿勢（ワールド座標）。手元（Hilt）から刃先（Tip）までの線分として表す。
    /// 命中判定はこの線分を前後フレームで掃引して行う（Claude/docs/sword-input-design.md 3.3）。
    /// </summary>
    public readonly struct BladePose
    {
        /// <summary>手元（コントローラのグリップ位置）。</summary>
        public Vector3 Hilt { get; }

        /// <summary>刃先。</summary>
        public Vector3 Tip { get; }

        /// <summary>刃の向き（+Z が手元→刃先）。表示用。</summary>
        public Quaternion Rotation { get; }

        /// <summary>観測した時刻（秒、Time.timeAsDouble 基準）。</summary>
        public double Time { get; }

        /// <summary>追跡中か。false のとき位置は前回値または既定値で、命中判定に使ってはならない。</summary>
        public bool IsTracked { get; }

        public BladePose(Vector3 hilt, Vector3 tip, Quaternion rotation, double time, bool isTracked)
        {
            Hilt = hilt;
            Tip = tip;
            Rotation = rotation;
            Time = time;
            IsTracked = isTracked;
        }

        public static BladePose Untracked(double time)
        {
            return new BladePose(Vector3.zero, Vector3.zero, Quaternion.identity, time, false);
        }

        /// <summary>Core の振り判定へ渡す形に変換する。</summary>
        public BladeSample ToSample()
        {
            return IsTracked
                ? new BladeSample(Time, true, Hilt.x, Hilt.y, Hilt.z, Tip.x, Tip.y, Tip.z)
                : BladeSample.Untracked(Time);
        }

        /// <summary>
        /// グリップ姿勢から刃を作る。刃はグリップの向きに bladeLocalRotation を掛けた +Z 方向へ伸びる。
        /// Quest とEditor で同じ計算を使い、刃の長さ・角度の調整値（C8）を1か所にまとめる。
        /// </summary>
        public static BladePose FromGrip(Vector3 gripPosition, Quaternion gripRotation, Quaternion bladeLocalRotation,
            float bladeLength, double time)
        {
            var rotation = gripRotation * bladeLocalRotation;
            var tip = gripPosition + rotation * Vector3.forward * bladeLength;
            return new BladePose(gripPosition, tip, rotation, time, true);
        }
    }
}
