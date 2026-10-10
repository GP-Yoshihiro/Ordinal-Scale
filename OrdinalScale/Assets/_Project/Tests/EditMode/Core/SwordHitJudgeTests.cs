using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>命中の規則（接触の開始 × 振り中 × この振りで未命中）を、振りの状態を直接与えて確かめる。</summary>
    public class SwordHitJudgeTests
    {
        private static SwingState Idle(int id = 0) => new SwingState(SwingPhase.Idle, id, 0f, true, SwingBreak.None);
        private static SwingState Started(int id) => new SwingState(SwingPhase.Started, id, 3f, true, SwingBreak.None);
        private static SwingState Swinging(int id) => new SwingState(SwingPhase.Swinging, id, 3f, true, SwingBreak.None);
        private static SwingState Ended(int id) => new SwingState(SwingPhase.Ended, id, 0f, true, SwingBreak.SlowedDown);

        [Test]
        public void ContactStartDuringSwingIsHit()
        {
            var j = new SwordHitJudge();
            j.BeginFrame(Started(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.Hit));
            Assert.That(j.TotalHits, Is.EqualTo(1));
        }

        [Test]
        public void ContactStartOutsideSwingIsNotHit()
        {
            var j = new SwordHitJudge();
            j.BeginFrame(Idle());
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.NotSwinging));

            // 振りが終わったフレーム（Ended）もすでに振り中ではない
            j.BeginFrame(Ended(1));
            Assert.That(j.Evaluate(2, true), Is.EqualTo(HitVerdict.NotSwinging));
            Assert.That(j.TotalHits, Is.EqualTo(0));
        }

        [Test]
        public void ContinuedContactNeverHitsEvenWhenSwingStarts()
        {
            // 押し当て（振り外で接触開始）→ 触れたまま振りが始まっても命中しない
            var j = new SwordHitJudge();
            j.BeginFrame(Idle());
            j.Evaluate(1, true);
            j.BeginFrame(Started(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.ContactContinued));
            j.BeginFrame(Swinging(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.ContactContinued));
            Assert.That(j.TotalHits, Is.EqualTo(0));
        }

        [Test]
        public void SecondContactInSameSwingIsAlreadyHit()
        {
            var j = new SwordHitJudge();
            j.BeginFrame(Started(1));
            j.Evaluate(1, true);
            j.BeginFrame(Swinging(1));
            Assert.That(j.Evaluate(1, false), Is.EqualTo(HitVerdict.NoContact));
            j.BeginFrame(Swinging(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.AlreadyHitThisSwing));
            Assert.That(j.TotalHits, Is.EqualTo(1));
        }

        [Test]
        public void NewSwingAllowsAnotherHit()
        {
            var j = new SwordHitJudge();
            j.BeginFrame(Started(1));
            j.Evaluate(1, true);
            j.BeginFrame(Ended(1));
            j.Evaluate(1, false);
            j.BeginFrame(Started(2));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.Hit));
            Assert.That(j.TotalHits, Is.EqualTo(2));
        }

        [Test]
        public void OneSwingCanHitEachEnemyOnce()
        {
            // 1振り1命中は敵ごと。同じ振りで2体に触れれば2体とも1回ずつ
            var j = new SwordHitJudge();
            j.BeginFrame(Started(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.Hit));
            Assert.That(j.Evaluate(2, true), Is.EqualTo(HitVerdict.Hit));
            Assert.That(j.TotalHits, Is.EqualTo(2));
        }

        [Test]
        public void NoContactNeverHits()
        {
            var j = new SwordHitJudge();
            j.BeginFrame(Started(1));
            Assert.That(j.Evaluate(1, false), Is.EqualTo(HitVerdict.NoContact));
            Assert.That(j.TotalHits, Is.EqualTo(0));
        }

        [Test]
        public void ForgetClearsContactOfThatEnemy()
        {
            // 敵を置き直したら、前の位置での「触れ続け」を引き継がない
            var j = new SwordHitJudge();
            j.BeginFrame(Idle());
            j.Evaluate(1, true);
            j.Forget(1);
            j.BeginFrame(Started(1));
            Assert.That(j.Evaluate(1, true), Is.EqualTo(HitVerdict.Hit));
        }
    }
}
