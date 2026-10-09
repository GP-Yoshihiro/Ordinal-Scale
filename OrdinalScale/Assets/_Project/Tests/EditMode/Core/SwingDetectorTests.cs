using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>振りの開始・継続・終了（C1・C2・C5・C8）。速さのしきい値は仮の値（開始1.5・終了0.6 m/s、終了の継続80ms、最短100ms）。</summary>
    public class SwingDetectorTests
    {
        private const double Dt = 0.01; // 100Hz で数えやすくする

        /// <summary>刃先を X 方向に動かして観測値を作る。speed は直前のサンプルからの刃先の速さ（m/s）。</summary>
        private sealed class Feed
        {
            private readonly SwingDetector _detector;
            private double _time;
            private float _x;

            public Feed(SwordTuning tuning = null)
            {
                _detector = new SwingDetector(tuning ?? SwordTuning.Default);
                Next(0f); // 最初の1サンプルは速さが出ない
            }

            public SwingState Next(float speed, double dt = Dt)
            {
                _time += dt;
                _x += (float)(speed * dt);
                return _detector.Update(new BladeSample(_time, true, _x, 1f, 0f, _x, 1f, 0.9f));
            }

            public SwingState Raw(BladeSample s) => _detector.Update(s);

            public double Time => _time;

            public float X => _x;
        }

        [Test]
        public void FirstSampleHasNoSpeed()
        {
            var d = new SwingDetector(SwordTuning.Default);
            var s = d.Update(new BladeSample(0, true, 0f, 1f, 0f, 0f, 1f, 0.9f));
            Assert.That(s.HasSpeed, Is.False);
            Assert.That(s.Phase, Is.EqualTo(SwingPhase.Idle));
        }

        [Test]
        public void BelowStartSpeedStaysIdle()
        {
            var f = new Feed();
            for (var i = 0; i < 50; i++) Assert.That(f.Next(1.4f).Phase, Is.EqualTo(SwingPhase.Idle));
        }

        [Test]
        public void StartSpeedStartsASwingWithNewId()
        {
            var f = new Feed();
            var s = f.Next(1.6f);
            Assert.That(s.Phase, Is.EqualTo(SwingPhase.Started));
            Assert.That(s.IsSwinging, Is.True);
            Assert.That(s.SwingId, Is.EqualTo(1));
            Assert.That(s.TipSpeed, Is.EqualTo(1.6f).Within(1e-3f));
        }

        [Test]
        public void SpeedBetweenEndAndStartKeepsSwinging()
        {
            // ヒステリシス：開始後は 0.6〜1.5 m/s でも振りが続く
            var f = new Feed();
            f.Next(2f);
            for (var i = 0; i < 30; i++) Assert.That(f.Next(0.8f).Phase, Is.EqualTo(SwingPhase.Swinging));
        }

        [Test]
        public void EndsOnlyAfterSlowForHoldTime()
        {
            // 振りを 200ms 続けた後に止める。低速 80ms 未満の間は振り中、80ms に達したフレームで終了
            var f = new Feed();
            for (var i = 0; i < 20; i++) f.Next(3f);
            for (var i = 1; i <= 7; i++) Assert.That(f.Next(0f).Phase, Is.EqualTo(SwingPhase.Swinging), $"低速 {i * 10}ms");
            var end = f.Next(0f);
            Assert.That(end.Phase, Is.EqualTo(SwingPhase.Ended));
            Assert.That(end.Break, Is.EqualTo(SwingBreak.SlowedDown));
            Assert.That(end.IsSwinging, Is.False);
            Assert.That(f.Next(0f).Phase, Is.EqualTo(SwingPhase.Idle));
        }

        [Test]
        public void ShortDipDoesNotSplitTheSwing()
        {
            // 切り返しなどで 50ms だけ遅くなっても1振りのまま（C5 の「1振り」を割らない）
            var f = new Feed();
            f.Next(3f);
            for (var i = 0; i < 5; i++) f.Next(0.2f);
            var s = f.Next(3f);
            Assert.That(s.Phase, Is.EqualTo(SwingPhase.Swinging));
            Assert.That(s.SwingId, Is.EqualTo(1));
        }

        [Test]
        public void MinimumSwingDurationDelaysTheEnd()
        {
            // 終了の継続時間を0にしても、開始から100ms経つまでは終わらない
            var f = new Feed(new SwordTuning(swingEndHoldSeconds: 0));
            f.Next(3f); // 開始（区間の始まり t=0.01 を開始時刻とする）
            for (var i = 0; i < 8; i++) Assert.That(f.Next(0f).IsSwinging, Is.True, $"開始から {(i + 2) * 10}ms");
            Assert.That(f.Next(0f).Phase, Is.EqualTo(SwingPhase.Ended));
        }

        [Test]
        public void NextSwingGetsNextId()
        {
            var f = new Feed();
            f.Next(3f);
            for (var i = 0; i < 10; i++) f.Next(0f);
            var s = f.Next(3f);
            Assert.That(s.Phase, Is.EqualTo(SwingPhase.Started));
            Assert.That(s.SwingId, Is.EqualTo(2));
        }

        [Test]
        public void TrackingLossEndsTheSwingAndRecoveryIsNotASwing()
        {
            var f = new Feed();
            f.Next(3f);
            var lost = f.Raw(BladeSample.Untracked(f.Time + Dt));
            Assert.That(lost.Phase, Is.EqualTo(SwingPhase.Ended));
            Assert.That(lost.Break, Is.EqualTo(SwingBreak.TrackingLost));

            // 復帰直後のサンプルは、どれだけ離れた位置でも速さを出さない
            var back = f.Raw(new BladeSample(f.Time + 2 * Dt, true, 5f, 1f, 0f, 5f, 1f, 0.9f));
            Assert.That(back.HasSpeed, Is.False);
            Assert.That(back.Phase, Is.EqualTo(SwingPhase.Idle));
        }

        [Test]
        public void JumpIsNotCountedAsSwing()
        {
            // 1サンプルで 0.6m（上限0.5m）動いたら、速さが大きくても振りにしない
            var f = new Feed();
            var s = f.Next(60f);
            Assert.That(s.Break, Is.EqualTo(SwingBreak.Jump));
            Assert.That(s.IsSwinging, Is.False);
            Assert.That(s.SwingId, Is.EqualTo(0));

            // 飛んだ先の位置から測り直す
            Assert.That(f.Next(3f).Phase, Is.EqualTo(SwingPhase.Started));
        }

        [Test]
        public void LongGapBetweenSamplesInterruptsTheSwing()
        {
            var f = new Feed();
            f.Next(3f);
            var s = f.Next(3f, dt: 0.2);
            Assert.That(s.Phase, Is.EqualTo(SwingPhase.Ended));
            Assert.That(s.Break, Is.EqualTo(SwingBreak.Gap));
        }

        [Test]
        public void DuplicateTimestampDoesNotChangeState()
        {
            var f = new Feed();
            f.Next(3f);
            var same = f.Raw(new BladeSample(f.Time, true, f.X + 1f, 1f, 0f, f.X + 1f, 1f, 0.9f));
            Assert.That(same.Phase, Is.EqualTo(SwingPhase.Swinging));
            Assert.That(same.SwingId, Is.EqualTo(1));
        }

        [Test]
        public void ResetStartsCountingFromZero()
        {
            var d = new SwingDetector(SwordTuning.Default);
            d.Update(new BladeSample(0.00, true, 0f, 1f, 0f, 0f, 1f, 0.9f));
            d.Update(new BladeSample(0.01, true, 0.03f, 1f, 0f, 0.03f, 1f, 0.9f));
            Assert.That(d.SwingCount, Is.EqualTo(1));
            d.Reset();
            Assert.That(d.SwingCount, Is.EqualTo(0));
            Assert.That(d.IsSwinging, Is.False);
        }
    }
}
