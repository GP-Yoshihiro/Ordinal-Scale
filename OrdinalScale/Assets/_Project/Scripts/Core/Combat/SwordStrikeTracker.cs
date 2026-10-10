namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// 剣の命中判定の入口（Q1）。ISwordPoseSource から毎フレーム得た BladeSample を Step に渡し、
    /// 続けて各敵の体のカプセルについて Resolve を呼ぶと、命中かどうか（と理由）が返る。
    /// 振り判定（SwingDetector）・接触の掃引（SwordContact）・1振り1命中の規則（SwordHitJudge）をまとめる。
    /// HP の減少や勝敗は呼び出し側（Q2）が Hit を受けて行う。
    /// </summary>
    public sealed class SwordStrikeTracker
    {
        private readonly SwingDetector _swing;
        private readonly SwordHitJudge _judge = new SwordHitJudge();

        private BladeSample _previous;
        private bool _hasPrevious;
        private BladeSample _current;
        private bool _hasCurrent;

        public SwordStrikeTracker(SwordTuning tuning = null)
        {
            Tuning = tuning ?? SwordTuning.Default;
            _swing = new SwingDetector(Tuning);
        }

        public SwordTuning Tuning { get; }

        /// <summary>直近の Step の振りの状態。</summary>
        public SwingState Swing { get; private set; }

        public int TotalHits => _judge.TotalHits;

        /// <summary>このフレームの刃の観測値を渡す。1フレームに1回、Resolve より先に呼ぶ。</summary>
        public SwingState Step(in BladeSample sample)
        {
            if (_hasCurrent)
            {
                _previous = _current;
                _hasPrevious = true;
            }

            _current = sample;
            _hasCurrent = true;

            // 追跡の飛び・間隔の空きでは、前のフレームの刃から掃引すると途中の空間を「切った」ことになってしまうので掃引しない
            Swing = _swing.Update(sample);
            if (Swing.Break == SwingBreak.Jump || Swing.Break == SwingBreak.Gap) _hasPrevious = false;

            _judge.BeginFrame(Swing);
            return Swing;
        }

        /// <summary>Step で渡したフレームについて、敵 targetId（体 body）への命中を判定する。</summary>
        public HitVerdict Resolve(int targetId, in BodyCapsule body)
        {
            var touching = _hasCurrent && SwordContact.Touches(_previous, _hasPrevious, _current, body, Tuning);
            return _judge.Evaluate(targetId, touching);
        }

        /// <summary>敵が消えた・置き直されたとき。</summary>
        public void Forget(int targetId) => _judge.Forget(targetId);

        /// <summary>再挑戦などで全状態を初期化する。</summary>
        public void Reset()
        {
            _swing.Reset();
            _judge.Reset();
            _hasPrevious = false;
            _hasCurrent = false;
            Swing = default;
        }
    }
}
