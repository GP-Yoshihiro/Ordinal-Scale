using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>敵の移動と反撃（Q3・Q-3）。値は仮（接近0.5m/s・攻撃距離1.2m・届く距離1.5m・安全範囲1.0m・予備動作0.8秒・硬直2秒・開始待ち1.5秒）。</summary>
    public class EnemyBrainTests
    {
        private const double Dt = 1.0 / 60.0;

        private static EnemyBrain Spawned(float x = 0f, float z = 2f)
        {
            var b = new EnemyBrain();
            b.Reset(x, z);
            return b;
        }

        /// <summary>seconds 秒進め、その間の命中・回避・予備動作の開始を数える。</summary>
        private static (int landed, int missed, int windUps) Run(EnemyBrain b, double seconds, float px = 0f, float pz = 0f)
        {
            int landed = 0, missed = 0, windUps = 0;
            for (var t = 0.0; t < seconds - 1e-9; t += Dt)
            {
                var s = b.Step(Dt, px, pz);
                if (s.StrikeLanded) landed++;
                if (s.StrikeMissed) missed++;
                if (s.WindUpStarted) windUps++;
            }

            return (landed, missed, windUps);
        }

        [Test]
        public void WaitsBeforeMoving()
        {
            var b = Spawned();
            Run(b, 1.4);
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.Waiting));
            Assert.That(b.Z, Is.EqualTo(2f));
        }

        [Test]
        public void ApproachesAndStopsAtAttackRange()
        {
            // 2m 先から 0.5m/s で 0.8m 進み、1.2m で止まって予備動作に入る
            var b = Spawned();
            var r = Run(b, 1.5 + 1.7);
            Assert.That(r.windUps, Is.EqualTo(1));
            Assert.That(b.Z, Is.EqualTo(1.2f).Within(0.01f));
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.WindUp));
        }

        [Test]
        public void NeverComesCloserThanAttackRange()
        {
            var b = Spawned();
            for (var t = 0.0; t < 20.0; t += Dt)
            {
                b.Step(Dt, 0f, 0f);
                Assert.That(b.Z, Is.GreaterThanOrEqualTo(1.2f - 1e-3f));
            }
        }

        [Test]
        public void FastEnemyDoesNotOvershootIntoTheAttackRange()
        {
            // 1フレームの移動量が大きい（5m/s）ときも、攻撃距離より内側へ踏み込まない
            var b = new EnemyBrain(new EnemyBrainSettings(moveSpeed: 5f, leashRadius: 2f, startDelaySeconds: 0));
            b.Reset(0f, 2f);
            for (var t = 0.0; t < 3.0; t += Dt)
            {
                b.Step(Dt, 0f, 0f);
                Assert.That(b.Z, Is.GreaterThanOrEqualTo(1.2f - 1e-4f));
            }
        }

        [Test]
        public void StaysInsideTheSafeRadiusWhenThePlayerWalksAway()
        {
            // プレイヤーが5m 離れても、配置位置から1m を超えて追わず、攻撃もしない
            var b = Spawned(0f, 2f);
            var r = Run(b, 15.0, px: 0f, pz: -3f);
            var dz = b.Z - 2f;
            Assert.That(System.Math.Abs(dz), Is.EqualTo(1.0).Within(1e-3));
            Assert.That(r.windUps, Is.EqualTo(0));
        }

        [Test]
        public void StrikeLandsAfterWindUpWhenThePlayerStays()
        {
            var b = Spawned();
            Run(b, 3.2);
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.WindUp));
            var r = Run(b, 0.85);
            Assert.That(r.landed, Is.EqualTo(1));
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.Recovering));
        }

        [Test]
        public void SteppingBackDuringWindUpDodges()
        {
            // 予備動作の間にプレイヤーが0.5m 下がる（距離1.7m > 届く距離1.5m）→ 外れる
            var b = Spawned();
            Run(b, 3.2);
            var r = Run(b, 0.85, pz: -0.5f);
            Assert.That(r.missed, Is.EqualTo(1));
            Assert.That(r.landed, Is.EqualTo(0));
        }

        [Test]
        public void AttacksRepeatAfterRecovery()
        {
            // 動かないプレイヤーには、予備動作0.8秒＋硬直2秒ごとに攻撃する
            var b = Spawned();
            var r = Run(b, 13.0); // 1回目は約3.9秒、以後約2.8秒ごと → 13秒で4回
            Assert.That(r.landed, Is.EqualTo(4));
            Assert.That(b.Strikes, Is.EqualTo(4));
        }

        [Test]
        public void HugeFrameTimeIsClamped()
        {
            var b = Spawned();
            b.Step(10.0, 0f, 0f);
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.Waiting), "10秒の処理落ちでも一気に進めない");
        }

        [Test]
        public void ResetReturnsToSpawnAndWaits()
        {
            var b = Spawned();
            Run(b, 6.0);
            b.Reset(1f, 3f);
            Assert.That(b.Phase, Is.EqualTo(EnemyPhase.Waiting));
            Assert.That(b.X, Is.EqualTo(1f));
            Assert.That(b.Z, Is.EqualTo(3f));
            Assert.That(b.Strikes, Is.EqualTo(0));
        }

        [Test]
        public void InvalidSettingsAreRejected()
        {
            Assert.That(() => new EnemyBrainSettings(attackRange: 1.5f, strikeReach: 1.0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new EnemyBrainSettings(attackRange: 0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
