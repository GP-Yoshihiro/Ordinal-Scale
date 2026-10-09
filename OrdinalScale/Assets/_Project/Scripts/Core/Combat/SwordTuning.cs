using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// 剣の命中判定の調整値（条件 C8）。**すべて仮の値で、Quest 実機では未確認**。
    /// 11月18日以降の実機で「斬った」「押し当てた」の感覚に合わせて調整し、受入時の値を固定して記録する。
    /// 初期値は Claude/docs/sword-input-design.md 3.4 のとおり。
    /// </summary>
    public sealed class SwordTuning
    {
        /// <summary>この刃先の速さ（m/s）以上になったら振りが始まる。</summary>
        public float SwingStartSpeed { get; }

        /// <summary>振り中、刃先の速さ（m/s）がこれ未満の状態が SwingEndHoldSeconds 続いたら振りが終わる。開始より低くしてヒステリシスにする。</summary>
        public float SwingEndSpeed { get; }

        /// <summary>振りの終了とみなす低速の継続時間（秒）。</summary>
        public double SwingEndHoldSeconds { get; }

        /// <summary>振りの最短時間（秒）。これより短い間は低速になっても振りを終えない（1振りが2つに割れないようにする）。</summary>
        public double MinSwingSeconds { get; }

        /// <summary>1サンプルで刃先がこれ（m）より大きく動いたら、追跡の飛びとみなして振りに数えない。</summary>
        public float MaxTipStepMeters { get; }

        /// <summary>サンプルの間隔がこれ（秒）より空いたら連続していないとみなす（処理落ち・追跡の途切れ）。</summary>
        public double MaxSampleGapSeconds { get; }

        /// <summary>刃の太さ（半径 m）。体のカプセル半径に足して接触を判定する。</summary>
        public float BladeRadius { get; }

        /// <summary>前後フレームの刃を補間する間隔（刃先の移動量 m）。速い振りのすり抜けを防ぐ。</summary>
        public float SweepStepMeters { get; }

        /// <summary>1フレームの補間の上限回数（計算量の上限）。</summary>
        public int MaxSweepSteps { get; }

        public SwordTuning(
            float swingStartSpeed = 1.5f,
            float swingEndSpeed = 0.6f,
            double swingEndHoldSeconds = 0.08,
            double minSwingSeconds = 0.1,
            float maxTipStepMeters = 0.5f,
            double maxSampleGapSeconds = 0.1,
            float bladeRadius = 0.02f,
            float sweepStepMeters = 0.05f,
            int maxSweepSteps = 16)
        {
            if (!(swingStartSpeed > 0f)) throw new ArgumentOutOfRangeException(nameof(swingStartSpeed), swingStartSpeed, "Must be positive.");
            if (!(swingEndSpeed >= 0f) || swingEndSpeed > swingStartSpeed)
                throw new ArgumentOutOfRangeException(nameof(swingEndSpeed), swingEndSpeed, "Must be between 0 and swingStartSpeed.");
            if (!(swingEndHoldSeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(swingEndHoldSeconds));
            if (!(minSwingSeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(minSwingSeconds));
            if (!(maxTipStepMeters > 0f)) throw new ArgumentOutOfRangeException(nameof(maxTipStepMeters));
            if (!(maxSampleGapSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(maxSampleGapSeconds));
            if (!(bladeRadius >= 0f)) throw new ArgumentOutOfRangeException(nameof(bladeRadius));
            if (!(sweepStepMeters > 0f)) throw new ArgumentOutOfRangeException(nameof(sweepStepMeters));
            if (maxSweepSteps < 1) throw new ArgumentOutOfRangeException(nameof(maxSweepSteps));

            SwingStartSpeed = swingStartSpeed;
            SwingEndSpeed = swingEndSpeed;
            SwingEndHoldSeconds = swingEndHoldSeconds;
            MinSwingSeconds = minSwingSeconds;
            MaxTipStepMeters = maxTipStepMeters;
            MaxSampleGapSeconds = maxSampleGapSeconds;
            BladeRadius = bladeRadius;
            SweepStepMeters = sweepStepMeters;
            MaxSweepSteps = maxSweepSteps;
        }

        /// <summary>設計書 3.4 の仮の初期値。</summary>
        public static SwordTuning Default { get; } = new SwordTuning();

        /// <summary>報告・ログに残すための1行表記（受入時の値の記録用）。</summary>
        public override string ToString()
        {
            return $"start={SwingStartSpeed:0.##}m/s end={SwingEndSpeed:0.##}m/s hold={SwingEndHoldSeconds * 1000:0}ms " +
                   $"minSwing={MinSwingSeconds * 1000:0}ms jump={MaxTipStepMeters:0.##}m gap={MaxSampleGapSeconds * 1000:0}ms " +
                   $"bladeR={BladeRadius:0.###}m sweep={SweepStepMeters:0.###}m x{MaxSweepSteps}";
        }
    }
}
