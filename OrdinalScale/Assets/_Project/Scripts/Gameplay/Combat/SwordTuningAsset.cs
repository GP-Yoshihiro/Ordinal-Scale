using System;
using OrdinalScale.Core.Combat;
using UnityEngine;

namespace OrdinalScale.Gameplay.Combat
{
    /// <summary>
    /// 剣の命中判定の調整値を Unity の Inspector で変えるための設定アセット（条件 C8）。
    /// 値は Core の SwordTuning にそのまま渡す。**既定値はすべて仮の値で、Quest 実機では未確認**。
    /// 再生中に Inspector で値を変えると、SwordHitDetector が次のフレームから新しい値で判定し直す（判定の途中状態は初期化）。
    /// 受入時はこのアセットの値を固定してコミットし、起動時のログ（[OrdinalScale][Q1] 剣の調整値）を記録に残す。
    /// </summary>
    [CreateAssetMenu(fileName = "SwordTuning", menuName = "OrdinalScale/Sword Tuning", order = 0)]
    public sealed class SwordTuningAsset : ScriptableObject
    {
        [Header("振りの判定（仮の値。Quest 実機で調整する）")]
        [Tooltip("刃先の速さ（m/s）がこれ以上になったら振りが始まる。これ未満で体に触れても「ゆっくり接触」で命中しない。")]
        [SerializeField, Min(0.01f)] private float swingStartSpeed = 1.5f;
        [Tooltip("振り中、刃先の速さ（m/s）がこれ未満の状態が下の時間続いたら振りが終わる。開始より小さくする。")]
        [SerializeField, Min(0f)] private float swingEndSpeed = 0.6f;
        [Tooltip("振りの終了とみなす低速の継続時間（ミリ秒）。")]
        [SerializeField, Min(0f)] private float swingEndHoldMs = 80f;
        [Tooltip("振りの最短時間（ミリ秒）。1振りが2つに割れないようにする。")]
        [SerializeField, Min(0f)] private float minSwingMs = 100f;

        [Header("追跡の乱れの除外（仮の値）")]
        [Tooltip("1フレームで刃先がこれ（m）より大きく動いたら、追跡の飛びとみなして振りに数えない。")]
        [SerializeField, Min(0.01f)] private float maxTipStepMeters = 0.5f;
        [Tooltip("フレームの間隔がこれ（ミリ秒）より空いたら、振りを打ち切る。")]
        [SerializeField, Min(1f)] private float maxSampleGapMs = 100f;

        [Header("接触（仮の値）")]
        [Tooltip("刃の太さ（半径 m）。")]
        [SerializeField, Min(0f)] private float bladeRadius = 0.02f;
        [Tooltip("前後フレームの刃を補間する間隔（m）。小さいほど速い振りの取りこぼしが減る。")]
        [SerializeField, Min(0.005f)] private float sweepStepMeters = 0.05f;
        [Tooltip("1フレームの補間の上限回数。")]
        [SerializeField, Min(1)] private int maxSweepSteps = 16;

        [NonSerialized] private int _revision;

        /// <summary>Inspector で値が変わるたびに増える番号（Editor のみ）。SwordHitDetector が変更を検出するのに使う。</summary>
        public int Revision => _revision;

        /// <summary>Core の調整値を作る。値の組み合わせが不正（終了＞開始など）なら false と理由を返す。</summary>
        public bool TryCreate(out SwordTuning tuning, out string error)
        {
            try
            {
                tuning = new SwordTuning(
                    swingStartSpeed,
                    swingEndSpeed,
                    swingEndHoldMs / 1000.0,
                    minSwingMs / 1000.0,
                    maxTipStepMeters,
                    maxSampleGapMs / 1000.0,
                    bladeRadius,
                    sweepStepMeters,
                    maxSweepSteps);
                error = null;
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                tuning = null;
                error = e.Message;
                return false;
            }
        }

        private void OnValidate()
        {
            _revision++;
        }
    }
}
