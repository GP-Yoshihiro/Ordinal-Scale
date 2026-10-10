using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>
    /// Q3：プレイヤーの HP と敗北、敗北後の2択、再挑戦で双方を初期化（Q-3・Q-5）。
    /// 開始・決着・再挑戦の間で状態が混ざらないこと（計画 Q3 の完了条件）も確かめる。プレイヤーの HP は仮の既定値5。
    /// </summary>
    public class BattleDefeatTests
    {
        private static void EnemyHits(BattleSession s, int n)
        {
            for (var i = 0; i < n; i++) s.RegisterEnemyAttackHit();
        }

        private static void PlayerHits(BattleSession s, int n)
        {
            for (var i = 0; i < n; i++) s.RegisterValidHit();
        }

        [Test]
        public void PlayerStartsWithFiveHp()
        {
            var s = new BattleSession();
            Assert.That(s.PlayerHealth.Max, Is.EqualTo(5));
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(5));
        }

        [Test]
        public void FourEnemyHitsDoNotDefeatThePlayer()
        {
            var s = new BattleSession();
            EnemyHits(s, 4);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(1));
        }

        [Test]
        public void FifthEnemyHitDefeatsThePlayerOnce()
        {
            var s = new BattleSession();
            var defeats = 0;
            s.Defeat += () => defeats++;
            EnemyHits(s, 4);
            Assert.That(s.RegisterEnemyAttackHit(), Is.EqualTo(PlayerHitResult.PlayerDefeated));
            Assert.That(s.State, Is.EqualTo(BattleState.Defeat));
            Assert.That(defeats, Is.EqualTo(1));
        }

        [Test]
        public void BothSidesAttacksAreIgnoredAfterDefeat()
        {
            var s = new BattleSession();
            EnemyHits(s, 5);
            Assert.That(s.RegisterValidHit(), Is.EqualTo(BattleHitResult.IgnoredNotFighting), "敗北後に剣で敵を倒せない");
            Assert.That(s.RegisterEnemyAttackHit(), Is.EqualTo(PlayerHitResult.IgnoredNotFighting));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(10));
            Assert.That(s.State, Is.EqualTo(BattleState.Defeat));
        }

        [Test]
        public void EnemyAttacksAreIgnoredAfterVictory()
        {
            // 勝利と同じフレームに敵の攻撃が来ても敗北に変わらない
            var s = new BattleSession();
            EnemyHits(s, 4);
            PlayerHits(s, 10);
            Assert.That(s.RegisterEnemyAttackHit(), Is.EqualTo(PlayerHitResult.IgnoredNotFighting));
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(1));
        }

        [Test]
        public void DefeatOffersQuitAndRetry()
        {
            var s = new BattleSession();
            EnemyHits(s, 5);
            Assert.That(s.AvailableChoices, Is.EqualTo(new[] { BattleChoice.Quit, BattleChoice.Retry }));
            Assert.That(s.Choose(BattleChoice.Quit), Is.True);
            Assert.That(s.QuitRequested, Is.True);
        }

        [Test]
        public void RetryAfterDefeatRestoresBothSides()
        {
            var s = new BattleSession();
            PlayerHits(s, 6);
            EnemyHits(s, 5);
            Assert.That(s.Choose(BattleChoice.Retry), Is.True);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting));
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(10), "前のラウンドで与えた6ダメージを持ち越さない");
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(5));
            Assert.That(s.ValidHits, Is.EqualTo(0));
            Assert.That(s.EnemyAttackHits, Is.EqualTo(0));
            Assert.That(s.Round, Is.EqualTo(2));
        }

        [Test]
        public void RetryAfterVictoryRestoresPlayerHpToo()
        {
            var s = new BattleSession();
            EnemyHits(s, 3);
            PlayerHits(s, 10);
            s.Choose(BattleChoice.Retry);
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(5), "前のラウンドの被弾を持ち越さない");
        }

        [Test]
        public void ResultsDoNotLeakAcrossRounds()
        {
            // 勝利 → 再挑戦 → 敗北 → 再挑戦 → 勝利：各ラウンドの結末と回数が独立している
            var s = new BattleSession();
            PlayerHits(s, 10);
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
            s.Choose(BattleChoice.Retry);

            PlayerHits(s, 9);
            EnemyHits(s, 5);
            Assert.That(s.State, Is.EqualTo(BattleState.Defeat), "前のラウンドの勝利が残らない");
            Assert.That(s.EnemyHealth.Current, Is.EqualTo(1));
            s.Choose(BattleChoice.Retry);

            PlayerHits(s, 9);
            Assert.That(s.State, Is.EqualTo(BattleState.Fighting), "前のラウンドの9命中を持ち越さない");
            PlayerHits(s, 1);
            Assert.That(s.State, Is.EqualTo(BattleState.Victory));
            Assert.That(s.Round, Is.EqualTo(3));
        }

        [Test]
        public void EnemyBrainAndSessionReachDefeatAgainstAStillPlayer()
        {
            // 動かないプレイヤー：敵が近づいて攻撃を繰り返し、5回目の命中で敗北。決着後は Step を止め、再挑戦で初期化
            var s = new BattleSession();
            var brain = new EnemyBrain();
            brain.Reset(0f, 2f);
            var t = 0.0;
            while (s.State == BattleState.Fighting && t < 60.0)
            {
                t += 1.0 / 60.0;
                if (brain.Step(1.0 / 60.0, 0f, 0f).StrikeLanded) s.RegisterEnemyAttackHit();
            }

            Assert.That(s.State, Is.EqualTo(BattleState.Defeat));
            Assert.That(brain.Strikes, Is.EqualTo(5));
            Assert.That(t, Is.LessThan(20.0), "仮の値では開始から約15秒で敗北する");

            s.Choose(BattleChoice.Retry);
            brain.Reset(0f, 2f);
            Assert.That(s.PlayerHealth.Current, Is.EqualTo(5));
            Assert.That(brain.Phase, Is.EqualTo(EnemyPhase.Waiting));
        }

        [Test]
        public void PlayerMaxHpMustBePositive()
        {
            Assert.That(() => new BattleSession(10, 0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
