using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>
    /// 剣の動き → 命中判定（Q1）→ 戦闘（Q2）を通す場面テスト。Gameplay の SwordHitDetector と BattleController と同じ順で、
    /// 出来事が「命中」のときだけ BattleSession.RegisterValidHit を呼ぶ。
    /// 配置は SwordStrikeScenarioTests と同じ（敵は足元 (0,0,2)・高さ1.6m・半径0.25m、刃は高さ1.2mで +Z 向き0.9m、90Hz）。
    /// 速さのしきい値は仮の値（開始1.5m/s）。
    /// </summary>
    public class BattleScenarioTests
    {
        private static readonly BodyCapsule Body = new BodyCapsule(0f, 0f, 2f, 1.6f, 0.25f);

        private sealed class Arena
        {
            private const double Dt = 1.0 / 90.0;

            private readonly SwordStrikeTracker _tracker = new SwordStrikeTracker();
            private readonly List<StrikeEvent> _events = new List<StrikeEvent>();
            private double _time;
            private float _x = -0.8f;

            public BattleSession Battle { get; }
            public StrikeEventRecorder Recorder { get; } = new StrikeEventRecorder();

            public Arena(int hitsToDefeat = BattleSession.DefaultHitsToDefeat)
            {
                Battle = new BattleSession(hitsToDefeat);
                Hold(0.1);
            }

            /// <summary>体を横切る速い振り（4m/s）をして止める。左右交互。</summary>
            public Arena Swing(int times = 1, float height = 1.2f)
            {
                for (var i = 0; i < times; i++)
                {
                    MoveTo(_x < 0f ? 0.8f : -0.8f, 4f, height);
                    Hold(0.3, height);
                }

                return this;
            }

            /// <summary>体の中央までゆっくり押し当て（0.5m/s）、元の位置へゆっくり戻す。</summary>
            public Arena SlowPress(int times = 1)
            {
                for (var i = 0; i < times; i++)
                {
                    var start = _x;
                    MoveTo(0f, 0.5f, 1.2f);
                    Hold(0.3);
                    MoveTo(start, 0.5f, 1.2f);
                    Hold(0.2);
                }

                return this;
            }

            /// <summary>Gameplay の再挑戦と同じ初期化（剣の判定と回数）。</summary>
            public Arena Retry()
            {
                Assert.That(Battle.Choose(BattleChoice.Retry), Is.True);
                _tracker.Reset();
                Recorder.Reset();
                return Hold(0.1);
            }

            public Arena Hold(double seconds, float height = 1.2f)
            {
                for (var t = 0.0; t < seconds - 1e-9; t += Dt) Frame(height);
                return this;
            }

            private void MoveTo(float target, float speed, float height)
            {
                var step = (float)(speed * Dt);
                while (System.Math.Abs(target - _x) > 1e-5f)
                {
                    var remaining = target - _x;
                    _x = System.Math.Abs(remaining) <= step ? target : _x + System.Math.Sign(remaining) * step;
                    Frame(height);
                }
            }

            private void Frame(float height)
            {
                _time += Dt;
                var swing = _tracker.Step(new BladeSample(_time, true, _x, height, 0.9f, _x, height, 1.8f));
                var verdict = _tracker.Resolve(1, Body);
                _events.Clear();
                Recorder.Process(swing, verdict, _events);
                foreach (var e in _events)
                    if (e.Kind == StrikeEventKind.Hit) Battle.RegisterValidHit();
            }
        }

        [Test]
        public void TenValidSwingsDefeatTheEnemy()
        {
            var a = new Arena().Swing(10);
            Assert.That(a.Recorder.Hits, Is.EqualTo(10));
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Victory));
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(0));
        }

        [Test]
        public void NineValidSwingsLeaveOneHp()
        {
            var a = new Arena().Swing(9);
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Fighting));
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(1));
        }

        [Test]
        public void WhiffsNeverReduceHp()
        {
            // 頭の上（2.0m）を20回払う：振りは数えるが HP は減らない
            var a = new Arena().Swing(20, height: 2.0f);
            Assert.That(a.Recorder.Swings, Is.EqualTo(20));
            Assert.That(a.Recorder.Whiffs, Is.EqualTo(20));
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(10));
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Fighting));
        }

        [Test]
        public void SlowPressesNeverReduceHp()
        {
            var a = new Arena().SlowPress(20);
            Assert.That(a.Recorder.SlowContacts, Is.EqualTo(20));
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(10));
        }

        [Test]
        public void MixedActionsOnlyValidHitsCount()
        {
            // 有効な振り9回＋空振り5回＋押し当て5回 → 撃破しない。最後の1振りで撃破
            var a = new Arena().Swing(4).SlowPress(5).Swing(5, height: 2.0f).Swing(5);
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(1));
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Fighting));
            a.Swing(1);
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Victory));
        }

        [Test]
        public void SwingsAfterVictoryDoNotCount()
        {
            var a = new Arena().Swing(10).Swing(5);
            Assert.That(a.Battle.ValidHits, Is.EqualTo(10));
            Assert.That(a.Battle.IgnoredHits, Is.EqualTo(5));
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Victory));
        }

        [Test]
        public void RetryResetsAndTheEnemyCanBeDefeatedAgain()
        {
            var a = new Arena().Swing(10).Retry();
            Assert.That(a.Battle.EnemyHealth.Current, Is.EqualTo(10));
            Assert.That(a.Recorder.Hits, Is.EqualTo(0));
            a.Swing(9);
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Fighting));
            a.Swing(1);
            Assert.That(a.Battle.State, Is.EqualTo(BattleState.Victory));
            Assert.That(a.Battle.Round, Is.EqualTo(2));
        }
    }
}
