namespace OrdinalScale.Core.Combat
{
    /// <summary>このフレームで振りの状態がどう変わったか。</summary>
    public enum SwingPhase
    {
        /// <summary>振っていない。</summary>
        Idle,
        /// <summary>このフレームで振りが始まった（このフレームから振り中）。</summary>
        Started,
        /// <summary>振りが続いている。</summary>
        Swinging,
        /// <summary>このフレームで振りが終わった（このフレームはもう振り中ではない）。</summary>
        Ended,
    }

    /// <summary>振りが終わった、または始まらなかった理由（ログ・テスト用）。</summary>
    public enum SwingBreak
    {
        None,
        /// <summary>低速が続いて自然に終わった。</summary>
        SlowedDown,
        /// <summary>追跡が途切れた。</summary>
        TrackingLost,
        /// <summary>刃先が1サンプルで不自然に大きく動いた（追跡の飛び）。</summary>
        Jump,
        /// <summary>サンプルの間隔が空きすぎた（処理落ちなど）。</summary>
        Gap,
    }

    /// <summary>1フレーム分の振り判定の結果。</summary>
    public readonly struct SwingState
    {
        public SwingPhase Phase { get; }

        /// <summary>今の振り（または直前に終わった振り）の通し番号。1から始まり、振りが始まるたびに増える。まだ振っていなければ0。</summary>
        public int SwingId { get; }

        /// <summary>このフレームの刃先の速さ（m/s）。速さを決められないフレームは0。</summary>
        public float TipSpeed { get; }

        /// <summary>速さを計算できたか（追跡外・飛び・間隔の空きでは false）。</summary>
        public bool HasSpeed { get; }

        public SwingBreak Break { get; }

        /// <summary>このフレームが振り中か（命中の条件 C1）。Started と Swinging のとき true。</summary>
        public bool IsSwinging => Phase == SwingPhase.Started || Phase == SwingPhase.Swinging;

        public SwingState(SwingPhase phase, int swingId, float tipSpeed, bool hasSpeed, SwingBreak swingBreak)
        {
            Phase = phase;
            SwingId = swingId;
            TipSpeed = tipSpeed;
            HasSpeed = hasSpeed;
            Break = swingBreak;
        }
    }

    /// <summary>
    /// 刃先の速さの時系列から「振り」の始まりと終わりを決める（条件 C1・C2・C5・C8）。
    /// - 速さが SwingStartSpeed 以上で開始。
    /// - SwingEndSpeed 未満が SwingEndHoldSeconds 続き、かつ MinSwingSeconds を過ぎたら終了（ヒステリシス）。
    /// - 追跡外・刃先の飛び・サンプル間隔の空きでは速さを使わない。振り中なら振りを打ち切り、振り始めとも数えない。
    /// 1フレームに1回 Update を呼ぶ。数値はすべて SwordTuning の仮の値。
    /// </summary>
    public sealed class SwingDetector
    {
        private readonly SwordTuning _tuning;

        private BladeSample _previous;
        private bool _hasPrevious;
        private bool _swinging;
        private int _swingId;
        private double _swingStartTime;
        private double _slowSince = -1;

        public SwingDetector(SwordTuning tuning)
        {
            _tuning = tuning ?? SwordTuning.Default;
        }

        public SwordTuning Tuning => _tuning;

        /// <summary>今まで始まった振りの数。</summary>
        public int SwingCount => _swingId;

        public bool IsSwinging => _swinging;

        public SwingState Update(in BladeSample sample)
        {
            if (!sample.IsTracked)
            {
                _hasPrevious = false;
                return Interrupt(SwingBreak.TrackingLost);
            }

            if (!_hasPrevious)
            {
                // 追跡の開始・復帰直後の1サンプルは速さを決められない。この位置から次のサンプルとの差を見る
                _previous = sample;
                _hasPrevious = true;
                return Interrupt(SwingBreak.None);
            }

            var previous = _previous;
            _previous = sample;

            var dt = sample.Time - previous.Time;
            if (dt < BladeSample.MinDeltaSeconds)
            {
                // 同じ時刻のサンプル（重複）は状態を変えない
                return Current(SwingPhase.Idle, 0f, false, SwingBreak.None);
            }

            if (dt > _tuning.MaxSampleGapSeconds) return Interrupt(SwingBreak.Gap);

            if (!BladeSample.TryTipSpeed(previous, sample, out var speed)) return Interrupt(SwingBreak.TrackingLost);
            if (speed * dt > _tuning.MaxTipStepMeters) return Interrupt(SwingBreak.Jump);

            if (!_swinging)
            {
                if (speed < _tuning.SwingStartSpeed) return new SwingState(SwingPhase.Idle, _swingId, speed, true, SwingBreak.None);

                _swinging = true;
                _swingId++;
                // 開始は「前のサンプルから今のサンプルまでの区間」で速さが出たので、その区間の始まりを振りの開始時刻とする
                _swingStartTime = previous.Time;
                _slowSince = -1;
                return new SwingState(SwingPhase.Started, _swingId, speed, true, SwingBreak.None);
            }

            if (speed >= _tuning.SwingEndSpeed)
            {
                _slowSince = -1;
                return new SwingState(SwingPhase.Swinging, _swingId, speed, true, SwingBreak.None);
            }

            // 低速になった区間の始まり（前のサンプルの時刻）から継続時間を数える
            if (_slowSince < 0) _slowSince = previous.Time;
            var slowFor = sample.Time - _slowSince;
            var swingFor = sample.Time - _swingStartTime;
            if (slowFor >= _tuning.SwingEndHoldSeconds - 1e-9 && swingFor >= _tuning.MinSwingSeconds - 1e-9)
            {
                _swinging = false;
                _slowSince = -1;
                return new SwingState(SwingPhase.Ended, _swingId, speed, true, SwingBreak.SlowedDown);
            }

            return new SwingState(SwingPhase.Swinging, _swingId, speed, true, SwingBreak.None);
        }

        /// <summary>状態を初期化する（再挑戦など）。振りの通し番号も0に戻す。</summary>
        public void Reset()
        {
            _hasPrevious = false;
            _swinging = false;
            _swingId = 0;
            _slowSince = -1;
        }

        private SwingState Interrupt(SwingBreak reason)
        {
            if (_swinging)
            {
                _swinging = false;
                _slowSince = -1;
                return new SwingState(SwingPhase.Ended, _swingId, 0f, false, reason);
            }

            return new SwingState(SwingPhase.Idle, _swingId, 0f, false, reason);
        }

        private SwingState Current(SwingPhase idlePhase, float speed, bool hasSpeed, SwingBreak reason)
        {
            return _swinging
                ? new SwingState(SwingPhase.Swinging, _swingId, speed, hasSpeed, reason)
                : new SwingState(idlePhase, _swingId, speed, hasSpeed, reason);
        }
    }
}
