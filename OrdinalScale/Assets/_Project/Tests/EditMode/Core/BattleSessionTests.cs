using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>戦闘の進行（E3・E4）：有効命中で HP が1ずつ減り、既定10回で撃破、撃破後は攻撃無効、2択、再挑戦、終了要求。</summary>
    public class BattleSessionTests
    {
        private static void Hit(BattleSession s, int times)
        {
            for (var i = 0; i < times; i++) s.RegisterValidHit();
        }

        [Test]
        public void DefaultNeedsTenValidHits()
        {
            var s = new BattleSession();
            Assert.That(s.HitsToDefeat, Is.EqualTo(10));
            Assert.That(s.EnemyHealth.Max, Is.EqualTo(10));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(10));
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
        }

        [Test]
        public void EachValidHitRemovesOneHp()
        {
            var s = new BattleSession();
            var log = new List<int>();
            s.EnemyDamaged += (current, max) => log.Add(current);
            Hit(s, 3);
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(7));
            Assert.That(log, Is.EqualTo(new[] { 9, 8, 7 }));
        }

        [Test]
        public void NineHitsDoNotDefeat()
        {
            var s = new BattleSession();
            Hit(s, 9);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(1));
            Assert.That(s.EnemyHealth.IsDead, Is.False);
            Assert.That(s.AvailableChoices.Count, Is.EqualTo(0));
        }

        [Test]
        public void TenthHitDefeatsAndRaisesVictoryOnce()
        {
            var s = new BattleSession();
            var victories = 0;
            s.Victory += () => victories++;
            Hit(s, 9);
            Assert.That(s.RegisterValidHit(), Is.EqualTo(BattleHitResult.Defeated));
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
            Assert.That(s.EnemyHealth.IsDead, Is.True);
            Assert.That(s.ValidHits, Is.EqualTo(10));
            Assert.That(victories, Is.EqualTo(1));
        }

        [Test]
        public void HitsAfterVictoryAreIgnored()
        {
            // 撃破後は攻撃を無効化：HP・有効命中数・撃破の通知は変わらない
            var s = new BattleSession();
            var victories = 0;
            s.Victory += () => victories++;
            Hit(s, 10);
            Assert.That(s.AcceptsAttacks, Is.False);
            Assert.That(s.RegisterValidHit(), Is.EqualTo(BattleHitResult.IgnoredNotFighting));
            Hit(s, 4);
            Assert.That(s.ValidHits, Is.EqualTo(10));
            Assert.That(s.IgnoredHits, Is.EqualTo(5));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(0));
            Assert.That(victories, Is.EqualTo(1));
        }

        [Test]
        public void ChoicesAppearOnlyAfterVictory()
        {
            var s = new BattleSession();
            Assert.That(s.Choose(BattleChoice.Retry), Is.False, "戦闘中は再挑戦できない");
            Assert.That(s.Choose(BattleChoice.Quit), Is.False, "戦闘中は終了を選べない");
            Assert.That(s.QuitRequested, Is.False);
            Hit(s, 10);
            Assert.That(s.AvailableChoices, Is.EqualTo(new[] { BattleChoice.Quit, BattleChoice.Retry }));
        }

        [Test]
        public void RetryRestoresEnemyAndStartsNextRound()
        {
            var s = new BattleSession();
            var retriedRound = 0;
            s.Retried += round => retriedRound = round;
            Hit(s, 12);
            Assert.That(s.Choose(BattleChoice.Retry), Is.True);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(10));
            Assert.That(s.ValidHits, Is.EqualTo(0));
            Assert.That(s.IgnoredHits, Is.EqualTo(0));
            Assert.That(s.Round, Is.EqualTo(2));
            Assert.That(retriedRound, Is.EqualTo(2));
            Assert.That(s.AvailableChoices.Count, Is.EqualTo(0));

            // 再挑戦後も同じ回数で撃破できる
            Hit(s, 9);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Hit(s, 1);
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
        }

        [Test]
        public void QuitIsRequestedOnceAfterVictory()
        {
            var s = new BattleSession();
            var requests = 0;
            s.QuitRequestedEvent += () => requests++;
            Hit(s, 10);
            Assert.That(s.Choose(BattleChoice.Quit), Is.True);
            Assert.That(s.Choose(BattleChoice.Quit), Is.True);
            Assert.That(s.QuitRequested, Is.True);
            Assert.That(requests, Is.EqualTo(1), "二重に閉じようとしない");
        }

        [Test]
        public void HitsToDefeatIsConfigurable()
        {
            // 試遊で調整する値（Q-4）。3回にすれば3回で撃破
            var s = new BattleSession(3);
            Hit(s, 2);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Hit(s, 1);
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
        }

        [Test]
        public void HitsToDefeatMustBePositive()
        {
            Assert.That(() => new BattleSession(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
