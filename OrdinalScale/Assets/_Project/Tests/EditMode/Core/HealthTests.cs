using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    public class HealthTests
    {
        [Test]
        public void StartsAtMax()
        {
            var hp = new Health(3);
            Assert.That(hp.Current, Is.EqualTo(3));
            Assert.That(hp.IsDead, Is.False);
        }

        [Test]
        public void RejectsNonPositiveMax()
        {
            Assert.That(() => new Health(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DamageReducesHpUntilKilled()
        {
            var hp = new Health(3);
            Assert.That(hp.ApplyDamage(1), Is.EqualTo(DamageResult.Damaged));
            Assert.That(hp.ApplyDamage(1), Is.EqualTo(DamageResult.Damaged));
            Assert.That(hp.ApplyDamage(1), Is.EqualTo(DamageResult.Killed));
            Assert.That(hp.Current, Is.EqualTo(0));
            Assert.That(hp.IsDead, Is.True);
        }

        [Test]
        public void OverkillClampsToZeroAndReportsDealtAmount()
        {
            var hp = new Health(3);
            var dealt = -1;
            hp.Damaged += (amount, _) => dealt = amount;

            Assert.That(hp.ApplyDamage(10), Is.EqualTo(DamageResult.Killed));
            Assert.That(hp.Current, Is.EqualTo(0));
            Assert.That(dealt, Is.EqualTo(3));
        }

        [Test]
        public void DiedFiresOnceAndFurtherDamageIsIgnored()
        {
            var hp = new Health(1);
            var diedCount = 0;
            hp.Died += () => diedCount++;

            hp.ApplyDamage(1);
            Assert.That(hp.ApplyDamage(1), Is.EqualTo(DamageResult.Ignored));
            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroOrNegativeDamageIsIgnored()
        {
            var hp = new Health(3);
            Assert.That(hp.ApplyDamage(0), Is.EqualTo(DamageResult.Ignored));
            Assert.That(hp.ApplyDamage(-5), Is.EqualTo(DamageResult.Ignored));
            Assert.That(hp.Current, Is.EqualTo(3));
        }

        [Test]
        public void ResetRestoresFullHpAndAllowsAnotherKill()
        {
            var hp = new Health(2);
            var diedCount = 0;
            var revived = false;
            hp.Died += () => diedCount++;
            hp.Revived += () => revived = true;

            hp.ApplyDamage(2);
            hp.Reset();

            Assert.That(revived, Is.True);
            Assert.That(hp.Current, Is.EqualTo(2));
            Assert.That(hp.ApplyDamage(2), Is.EqualTo(DamageResult.Killed));
            Assert.That(diedCount, Is.EqualTo(2));
        }
    }
}
