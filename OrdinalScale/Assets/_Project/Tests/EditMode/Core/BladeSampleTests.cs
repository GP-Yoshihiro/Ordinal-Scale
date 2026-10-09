using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    public class BladeSampleTests
    {
        private static BladeSample Tip(double time, float x, float y, float z)
        {
            return new BladeSample(time, true, 0f, 1f, 0f, x, y, z);
        }

        [Test]
        public void TipSpeedIsDistanceOverTime()
        {
            // 刃先が 0.1 秒で 0.3m 動いた → 3 m/s
            var a = Tip(1.0, 0f, 1f, 1f);
            var b = Tip(1.1, 0.3f, 1f, 1f);
            Assert.That(BladeSample.TryTipSpeed(a, b, out var speed), Is.True);
            Assert.That(speed, Is.EqualTo(3f).Within(1e-3f));
        }

        [Test]
        public void TipSpeedUsesAllThreeAxes()
        {
            // (0.3, 0.4, 1.2) の移動量は長さ 1.3m。1 秒なら 1.3 m/s
            var a = Tip(0.0, 0f, 0f, 0f);
            var b = Tip(1.0, 0.3f, 0.4f, 1.2f);
            Assert.That(BladeSample.TryTipSpeed(a, b, out var speed), Is.True);
            Assert.That(speed, Is.EqualTo(1.3f).Within(1e-4f));
        }

        [Test]
        public void NoSpeedWhenEitherSampleIsUntracked()
        {
            // 追跡が途切れたサンプルを含む区間は速さを決めない（位置の飛びを振りと誤認しないための前提）
            var tracked = Tip(1.0, 0f, 1f, 1f);
            var lost = BladeSample.Untracked(1.1);
            Assert.That(BladeSample.TryTipSpeed(tracked, lost, out var s1), Is.False);
            Assert.That(BladeSample.TryTipSpeed(lost, Tip(1.2, 1f, 1f, 1f), out var s2), Is.False);
            Assert.That(s1, Is.EqualTo(0f));
            Assert.That(s2, Is.EqualTo(0f));
        }

        [Test]
        public void NoSpeedWhenTimeDoesNotAdvance()
        {
            var a = Tip(2.0, 0f, 1f, 1f);
            var b = Tip(2.0, 1f, 1f, 1f);
            Assert.That(BladeSample.TryTipSpeed(a, b, out _), Is.False);
            Assert.That(BladeSample.TryTipSpeed(b, a, out _), Is.False);
        }

        [Test]
        public void LengthIsHiltToTipDistance()
        {
            var s = new BladeSample(0.0, true, 0f, 1f, 0f, 0f, 1f, 0.9f);
            Assert.That(s.Length, Is.EqualTo(0.9f).Within(1e-5f));
        }
    }
}
