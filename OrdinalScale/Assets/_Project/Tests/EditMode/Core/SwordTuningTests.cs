using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>調整値（C8）。既定値は設計書 3.4 の仮の値で、Quest 実機では未確認。</summary>
    public class SwordTuningTests
    {
        [Test]
        public void DefaultsMatchDesignDocument()
        {
            var t = SwordTuning.Default;
            Assert.That(t.SwingStartSpeed, Is.EqualTo(1.5f));
            Assert.That(t.SwingEndSpeed, Is.EqualTo(0.6f));
            Assert.That(t.SwingEndHoldSeconds, Is.EqualTo(0.08).Within(1e-9));
            Assert.That(t.MinSwingSeconds, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(t.MaxTipStepMeters, Is.EqualTo(0.5f));
        }

        [Test]
        public void EndSpeedAboveStartSpeedIsRejected()
        {
            // ヒステリシスが逆転すると振りが毎フレーム開始・終了を繰り返すため受け付けない
            Assert.That(() => new SwordTuning(swingStartSpeed: 1f, swingEndSpeed: 2f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void NonPositiveStartSpeedIsRejected()
        {
            Assert.That(() => new SwordTuning(swingStartSpeed: 0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DescriptionContainsValuesForTheRecord()
        {
            Assert.That(SwordTuning.Default.ToString(), Does.Contain("start="));
            Assert.That(SwordTuning.Default.ToString(), Does.Contain("end="));
        }
    }
}
