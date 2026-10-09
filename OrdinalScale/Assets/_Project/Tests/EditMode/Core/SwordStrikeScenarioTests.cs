using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>
    /// 剣の命中判定の場面テスト（Claude/docs/sword-input-design.md 4章、条件 C1〜C8）。
    /// 刃を毎フレーム（90Hz）の観測値として流し、SwordStrikeTracker が何回命中を数えるかを見る。
    ///
    /// 配置：敵の体は足元 (0, 0, 2)・高さ1.6m・半径0.25m。刃は高さ1.2mで +Z（敵の方）を向いた0.9mの線分で、
    /// 手元 (x, 1.2, 0.9)・刃先 (x, 1.2, 1.8)。刃を X 方向へ横に払うと、|x| がおよそ0.18m以下の間だけ体に触れる。
    /// 速さのしきい値は SwordTuning の仮の値（開始1.5m/s・終了0.6m/s）。Quest 実機の値ではない。
    /// </summary>
    public class SwordStrikeScenarioTests
    {
        private const int EnemyId = 1;
        private static readonly BodyCapsule Body = new BodyCapsule(0f, 0f, 2f, 1.6f, 0.25f);

        /// <summary>刃を動かしながら tracker に流す小さな再生器。</summary>
        private sealed class Sword
        {
            public const double Dt = 1.0 / 90.0;

            private readonly SwordStrikeTracker _tracker;
            private double _time;

            public float X { get; private set; }
            public float Height { get; set; } = 1.2f;
            public List<HitVerdict> Verdicts { get; } = new List<HitVerdict>();
            public List<SwingState> Swings { get; } = new List<SwingState>();

            public Sword(float startX, SwordTuning tuning = null)
            {
                _tracker = new SwordStrikeTracker(tuning);
                X = startX;
            }

            public int Hits => _tracker.TotalHits;

            /// <summary>x を一定の速さ（m/s）で target まで動かす。</summary>
            public Sword MoveTo(float target, float speed)
            {
                var step = (float)(speed * Dt);
                while (System.Math.Abs(target - X) > 1e-5f)
                {
                    var remaining = target - X;
                    X = System.Math.Abs(remaining) <= step ? target : X + System.Math.Sign(remaining) * step;
                    Frame(true);
                }

                return this;
            }

            /// <summary>その場で止めておく。</summary>
            public Sword Hold(double seconds)
            {
                for (var t = 0.0; t < seconds; t += Dt) Frame(true);
                return this;
            }

            /// <summary>追跡を失った状態を続ける。</summary>
            public Sword Lose(double seconds)
            {
                for (var t = 0.0; t < seconds; t += Dt) Frame(false);
                return this;
            }

            /// <summary>1フレームで x へ瞬間移動したように観測される（追跡の飛び）。</summary>
            public Sword TeleportTo(float x)
            {
                X = x;
                Frame(true);
                return this;
            }

            /// <summary>指定した x の列を1フレームずつ、間隔 dt で観測する。</summary>
            public Sword Samples(double dt, params float[] xs)
            {
                foreach (var x in xs)
                {
                    X = x;
                    _time += dt - Dt; // Frame が Dt 進めるので、その差だけ足す
                    Frame(true);
                }

                return this;
            }

            private void Frame(bool tracked)
            {
                _time += Dt;
                var sample = tracked
                    ? new BladeSample(_time, true, X, Height, 0.9f, X, Height, 1.8f)
                    : BladeSample.Untracked(_time);
                Swings.Add(_tracker.Step(sample));
                Verdicts.Add(_tracker.Resolve(EnemyId, Body));
            }
        }

        [Test]
        public void FastSwingThroughBodyHitsOnce()
        {
            // C1：振っている最中に体に触れたら命中。体を通り抜ける数フレームの接触でも1回（C3）
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0.8f, 4f).Hold(0.3);
            Assert.That(s.Hits, Is.EqualTo(1));
            Assert.That(s.Verdicts.Contains(HitVerdict.ContactContinued), Is.True);
        }

        [Test]
        public void SlowPressDoesNotHit()
        {
            // C2：ゆっくり押し当てる接触は命中しない
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0f, 0.5f).Hold(0.5);
            Assert.That(s.Hits, Is.EqualTo(0));
            Assert.That(s.Verdicts.Contains(HitVerdict.NotSwinging), Is.True);
        }

        [Test]
        public void PressedThenMovedFastWithoutLeavingDoesNotHit()
        {
            // C2・C3：押し当てたまま速く動かしても、接触が一度離れるまでは「接触の開始」が来ないので命中しない
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0f, 0.5f).Hold(0.1);
            for (var i = 0; i < 5; i++) s.MoveTo(0.12f, 3f).MoveTo(-0.12f, 3f);
            Assert.That(s.Swings.Exists(w => w.IsSwinging), Is.True, "前提：体の中で速く動かしているので振りとしては検出される");
            Assert.That(s.Hits, Is.EqualTo(0));
        }

        [Test]
        public void StayingInContactDuringSwingCountsOnce()
        {
            // C3・C5：振りの途中で体に入り、そのまま体の中で動かし続けても1回だけ
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0f, 4f);
            for (var i = 0; i < 5; i++) s.MoveTo(0.1f, 3f).MoveTo(-0.1f, 3f);
            s.Hold(0.3);
            Assert.That(s.Hits, Is.EqualTo(1));
        }

        [Test]
        public void LeavingAndReenteringWithinOneSwingCountsOnce()
        {
            // C5：1振りの間に体から離れて再び触れても、その振りでは1回だけ（往復の切り返しで減速しない）
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0.8f, 4f).MoveTo(-0.8f, 4f).Hold(0.3);
            Assert.That(s.Swings.FindAll(w => w.Phase == SwingPhase.Started).Count, Is.EqualTo(1), "前提：往復は1振り");
            Assert.That(s.Hits, Is.EqualTo(1));
            Assert.That(s.Verdicts.Contains(HitVerdict.AlreadyHitThisSwing), Is.True);
        }

        [Test]
        public void NextSwingAfterTheFirstEndsHitsAgain()
        {
            // C5：振りが終わってから次の振りで触れれば2回目の命中
            var s = new Sword(-0.8f).Hold(0.1).MoveTo(0.8f, 4f).Hold(0.3).MoveTo(-0.8f, 4f).Hold(0.3);
            Assert.That(s.Swings.FindAll(w => w.Phase == SwingPhase.Started).Count, Is.EqualTo(2));
            Assert.That(s.Hits, Is.EqualTo(2));
        }

        [Test]
        public void SwingThatMissesTheBodyDoesNotHit()
        {
            // C4：体に触れない振り（頭の上を払う）は命中しない
            var s = new Sword(-0.8f) { Height = 2.0f };
            s.Hold(0.1).MoveTo(0.8f, 4f).Hold(0.3);
            Assert.That(s.Swings.Exists(w => w.IsSwinging), Is.True, "前提：振りとしては検出される");
            Assert.That(s.Hits, Is.EqualTo(0));
            Assert.That(s.Verdicts.TrueForAll(v => v == HitVerdict.NoContact), Is.True);
        }

        [Test]
        public void ReappearingInsideBodyAfterTrackingLossIsNotASwing()
        {
            // C1・C8：追跡が途切れ、復帰した位置が体の中でも「振り」とはみなさない
            var s = new Sword(-0.8f).Hold(0.1).Lose(0.3).TeleportTo(0f).Hold(0.3);
            Assert.That(s.Hits, Is.EqualTo(0));
            Assert.That(s.Verdicts.Contains(HitVerdict.NotSwinging), Is.True);
        }

        [Test]
        public void TrackingJumpIntoBodyIsNotASwing()
        {
            // C1・C8：追跡の飛びで1フレームに0.8m動いても振りとみなさず、途中を掃引もしない
            var s = new Sword(-0.8f).Hold(0.1).TeleportTo(0f).Hold(0.3);
            Assert.That(s.Swings.Exists(w => w.Break == SwingBreak.Jump), Is.True);
            Assert.That(s.Swings.Exists(w => w.IsSwinging), Is.False);
            Assert.That(s.Hits, Is.EqualTo(0));
        }

        [Test]
        public void VeryFastSwingDoesNotPassThroughBody()
        {
            // すり抜け対策：前後どちらのフレームでも体の外だが、その間に体を横切った場合も命中する
            var before = new BladeSample(0, true, -0.25f, 1.2f, 0.9f, -0.25f, 1.2f, 1.8f);
            var after = new BladeSample(0, true, 0.2f, 1.2f, 0.9f, 0.2f, 1.2f, 1.8f);
            Assert.That(SwordContact.Touches(default, false, before, Body, SwordTuning.Default), Is.False, "前提：前のフレームは体の外");
            Assert.That(SwordContact.Touches(default, false, after, Body, SwordTuning.Default), Is.False, "前提：今のフレームも体の外");

            // 72Hz・刃先 約32m/s（1フレーム0.45m）
            var s = new Sword(-1.15f).Hold(0.1).Samples(1.0 / 72.0, -0.7f, -0.25f, 0.2f, 0.65f).Hold(0.3);
            Assert.That(s.Hits, Is.EqualTo(1));
        }

        [Test]
        public void TenSeparateSwingsGiveTenHits()
        {
            // E3・Q-4 の前提：有効な振りを10回当てれば命中は10回（HP への接続は Q2）
            var s = new Sword(-0.8f).Hold(0.1);
            for (var i = 0; i < 10; i++)
            {
                s.MoveTo(i % 2 == 0 ? 0.8f : -0.8f, 4f).Hold(0.3);
            }

            Assert.That(s.Hits, Is.EqualTo(10));
        }

        [Test]
        public void HigherStartThresholdTurnsTheSameMotionIntoAPress()
        {
            // C8：しきい値は調整値。同じ 1.2m/s の動きが、開始 1.0m/s なら命中、1.5m/s（既定）なら押し当て
            var loose = new Sword(-0.8f, new SwordTuning(swingStartSpeed: 1.0f)).Hold(0.1).MoveTo(0.8f, 1.2f).Hold(0.3);
            var strict = new Sword(-0.8f).Hold(0.1).MoveTo(0.8f, 1.2f).Hold(0.3);
            Assert.That(loose.Hits, Is.EqualTo(1));
            Assert.That(strict.Hits, Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsSwingAndContactHistory()
        {
            var tracker = new SwordStrikeTracker();
            var t = 0.0;
            for (var x = -0.8f; x <= 0.8f; x += 0.05f)
            {
                t += 1.0 / 90.0;
                tracker.Step(new BladeSample(t, true, x, 1.2f, 0.9f, x, 1.2f, 1.8f));
                tracker.Resolve(EnemyId, Body);
            }

            Assert.That(tracker.TotalHits, Is.EqualTo(1));
            tracker.Reset();
            Assert.That(tracker.TotalHits, Is.EqualTo(0));
            Assert.That(tracker.Swing.SwingId, Is.EqualTo(0));
        }
    }
}
