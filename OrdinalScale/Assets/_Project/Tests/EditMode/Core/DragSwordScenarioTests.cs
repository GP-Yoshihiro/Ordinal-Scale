using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>
    /// Editor の検証手順（Claude/docs/quest-xr-setup.md 2-3）と同じ配置で、マウスのドラッグ → 刃 → 判定 → 出来事 を通して確かめる。
    /// 配置：カメラは高さ1.6mで +Z を向く。敵（仮モデル）は正面2m・高さ1.6m・半径0.25m。刃先は正面2mの平面上を動く。
    /// 画面の高さ1000px、換算係数 1.0 m/画面高（仮）。60fps。マウスを画面の高さ分（1000px）動かすと刃先が1m動く。
    /// </summary>
    public class DragSwordScenarioTests
    {
        private const float EyeHeight = 1.6f;
        private const float TipDistance = 2f;
        private const float ScreenHeight = 1000f;
        private static readonly BodyCapsule Body = new BodyCapsule(0f, 0f, 2f, 1.6f, 0.25f);

        private sealed class DragRig
        {
            private const double Dt = 1.0 / 60.0;

            private readonly DragBladeModel _model;
            private readonly SwordStrikeTracker _tracker = new SwordStrikeTracker();
            private double _time;
            private float _mouseX = 500f;
            private float _mouseY = 500f;

            public StrikeEventRecorder Recorder { get; } = new StrikeEventRecorder();
            public List<StrikeEvent> Events { get; } = new List<StrikeEvent>();

            public DragRig(float metersPerScreenHeight = 1f)
            {
                _model = new DragBladeModel(metersPerScreenHeight);
            }

            /// <summary>刃先がカメラから見て (tipX, tipY) の位置（正面2mの平面上）になる場所でボタンを押す。</summary>
            public DragRig Press(float tipX, float tipY)
            {
                _model.Begin(tipX, tipY, TipDistance, _mouseX, _mouseY);
                return Frame();
            }

            /// <summary>ボタンを押したまま、マウスを (dx, dy) ピクセル、seconds 秒かけて一定の速さで動かす。</summary>
            public DragRig Drag(float dxPixels, float dyPixels, double seconds)
            {
                var frames = (int)System.Math.Ceiling(seconds / Dt - 1e-9);
                for (var i = 0; i < frames; i++)
                {
                    _mouseX += dxPixels / frames;
                    _mouseY += dyPixels / frames;
                    _model.Move(_mouseX, _mouseY, ScreenHeight);
                    Frame();
                }

                return this;
            }

            public DragRig Hold(double seconds)
            {
                for (var t = 0.0; t < seconds - 1e-9; t += Dt) Frame();
                return this;
            }

            public DragRig Release()
            {
                _model.End();
                Frame();
                return Hold(0.1);
            }

            public float MaxSwingPeakSpeed()
            {
                var max = 0f;
                foreach (var e in Events)
                    if (e.Kind == StrikeEventKind.SwingEnded && e.Speed > max) max = e.Speed;
                return max;
            }

            public bool Has(StrikeEventKind kind, SwingOutcome outcome = SwingOutcome.None)
            {
                foreach (var e in Events)
                    if (e.Kind == kind && (outcome == SwingOutcome.None || e.Outcome == outcome)) return true;
                return false;
            }

            private DragRig Frame()
            {
                _time += Dt;
                var sample = _model.TryGetLocalBlade(out var hx, out var hy, out var hz, out var tx, out var ty, out var tz)
                    ? new BladeSample(_time, true, hx, hy + EyeHeight, hz, tx, ty + EyeHeight, tz)
                    : BladeSample.Untracked(_time);
                var swing = _tracker.Step(sample);
                var verdict = _tracker.Resolve(1, Body);
                Recorder.Process(swing, verdict, Events);
                return this;
            }
        }

        [Test]
        public void FastDragAboveTheHeadIsWhiff()
        {
            // 空振り：頭の上（刃先の高さ2.1m）を速く払う（1200px を0.25秒 → 4.8m/s）
            var r = new DragRig().Press(-0.6f, 0.5f).Drag(1200f, 0f, 0.25).Hold(0.3).Release();
            Assert.That(r.Recorder.Swings, Is.EqualTo(1));
            Assert.That(r.Recorder.Whiffs, Is.EqualTo(1));
            Assert.That(r.Recorder.Hits, Is.EqualTo(0));
            Assert.That(r.Has(StrikeEventKind.SwingEnded, SwingOutcome.Whiff), Is.True);
        }

        [Test]
        public void SlowDragIntoTheBodyIsSlowContact()
        {
            // ゆっくり接触：胸の高さで中央まで 600px を2秒（0.3m/s）
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(600f, 0f, 2.0).Hold(0.3).Release();
            Assert.That(r.Recorder.Swings, Is.EqualTo(0));
            Assert.That(r.Recorder.SlowContacts, Is.EqualTo(1));
            Assert.That(r.Recorder.Hits, Is.EqualTo(0));
        }

        [Test]
        public void FastDragThroughTheBodyHitsOnce()
        {
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(1200f, 0f, 0.25).Hold(0.3).Release();
            Assert.That(r.Recorder.Hits, Is.EqualTo(1));
            Assert.That(r.Has(StrikeEventKind.SwingEnded, SwingOutcome.Hit), Is.True);
        }

        [Test]
        public void StoppingInsideTheBodyAfterAHitIsHeldContact()
        {
            // 触れ続け：速く振って中央で止め、0.5秒そのまま、ゆっくり抜く → 命中1回・触れ続け1回
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(600f, 0f, 0.15).Hold(0.5).Drag(600f, 0f, 2.0).Release();
            Assert.That(r.Recorder.Hits, Is.EqualTo(1));
            Assert.That(r.Recorder.HeldContacts, Is.EqualTo(1));
            Assert.That(r.Recorder.SlowContacts, Is.EqualTo(0));
        }

        [Test]
        public void ShakingWhilePressedAgainstTheBodyNeverHits()
        {
            // 押し当てたまま速く揺らす：体の中（|x|≤0.15m）で 300px を0.1秒（3m/s）で往復 → 振りは出るが命中なし
            var r = new DragRig().Press(-0.15f, -0.4f);
            for (var i = 0; i < 4; i++) r.Drag(300f, 0f, 0.1).Drag(-300f, 0f, 0.1);
            r.Hold(0.3).Release();
            Assert.That(r.Recorder.Swings, Is.GreaterThanOrEqualTo(1));
            Assert.That(r.Recorder.Hits, Is.EqualTo(0));
            Assert.That(r.Recorder.SlowContacts, Is.EqualTo(1));
            Assert.That(r.Has(StrikeEventKind.SwingEnded, SwingOutcome.ContactWithoutHit), Is.True);
        }

        [Test]
        public void BackAndForthWithoutPauseIsOneSwingWithRepeatContact()
        {
            // 1振りでの再接触：右へ払ってすぐ左へ戻す（止めない）
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(1200f, 0f, 0.25).Drag(-1200f, 0f, 0.25).Hold(0.3).Release();
            Assert.That(r.Recorder.Swings, Is.EqualTo(1));
            Assert.That(r.Recorder.Hits, Is.EqualTo(1));
            Assert.That(r.Recorder.RepeatContacts, Is.EqualTo(1));
        }

        [Test]
        public void PausingBetweenSwipesMakesTwoSwingsAndTwoHits()
        {
            // 別の振り：右へ払って0.3秒止め（ボタンは押したまま）、左へ払う
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(1200f, 0f, 0.25).Hold(0.3).Drag(-1200f, 0f, 0.25).Hold(0.3).Release();
            Assert.That(r.Recorder.Swings, Is.EqualTo(2));
            Assert.That(r.Recorder.Hits, Is.EqualTo(2));
            Assert.That(r.Recorder.RepeatContacts, Is.EqualTo(0));
        }

        [Test]
        public void ReleasingAndPressingAgainAlsoMakesASeparateSwing()
        {
            var r = new DragRig().Press(-0.6f, -0.4f).Drag(1200f, 0f, 0.25).Release()
                .Press(0.6f, -0.4f).Drag(-1200f, 0f, 0.25).Release();
            Assert.That(r.Recorder.Swings, Is.EqualTo(2));
            Assert.That(r.Recorder.Hits, Is.EqualTo(2));
        }

        [Test]
        public void TipSpeedIsMouseSpeedTimesCoefficient()
        {
            // 1200px を0.5秒（2.4画面高/秒）→ 係数1.0なら 2.4m/s。係数0.5なら 1.2m/s で振り開始（1.5m/s・仮）に届かない
            Assert.That(DragBladeModel.TipSpeedFromMouse(2400f, ScreenHeight, 1f), Is.EqualTo(2.4f).Within(1e-4f));

            var normal = new DragRig(1f).Press(-0.6f, -0.4f).Drag(1200f, 0f, 0.5).Hold(0.3).Release();
            Assert.That(normal.MaxSwingPeakSpeed(), Is.EqualTo(2.4f).Within(0.05f));
            Assert.That(normal.Recorder.Hits, Is.EqualTo(1));

            var halved = new DragRig(0.5f).Press(-0.3f, -0.4f).Drag(1200f, 0f, 0.5).Hold(0.3).Release();
            Assert.That(halved.Recorder.Swings, Is.EqualTo(0));
            Assert.That(halved.Recorder.SlowContacts, Is.EqualTo(1));
        }

        [Test]
        public void NoBladeUnlessDragging()
        {
            var m = new DragBladeModel();
            Assert.That(m.TryGetLocalBlade(out _, out _, out _, out _, out _, out _), Is.False);
            m.Begin(0f, 0f, 2f, 10f, 10f);
            Assert.That(m.TryGetLocalBlade(out _, out _, out _, out _, out _, out _), Is.True);
            m.End();
            Assert.That(m.TryGetLocalBlade(out _, out _, out _, out _, out _, out _), Is.False);
        }

        [Test]
        public void BladeKeepsItsLengthAndPointsFromShoulderToTip()
        {
            var m = new DragBladeModel(bladeLength: 0.9f, shoulderX: 0f, shoulderY: 0f, shoulderZ: 0f);
            m.Begin(0f, 0f, 2f, 0f, 0f);
            m.Move(300f, 0f, ScreenHeight); // 刃先が右へ0.3m
            Assert.That(m.TryGetLocalBlade(out var hx, out var hy, out var hz, out var tx, out var ty, out var tz), Is.True);
            Assert.That(tx, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(tz, Is.EqualTo(2f).Within(1e-5f));
            var len = System.Math.Sqrt((tx - hx) * (tx - hx) + (ty - hy) * (ty - hy) + (tz - hz) * (tz - hz));
            Assert.That(len, Is.EqualTo(0.9).Within(1e-4));
            Assert.That(hz, Is.LessThan(tz));
        }

        [Test]
        public void HiltLiesOnTheLineFromShoulderToTip()
        {
            // 刃は肩から刃先へ向かう：手元・刃先・肩が一直線に並び、手元は肩と刃先の間にある
            var m = new DragBladeModel(bladeLength: 0.9f, shoulderX: 0.2f, shoulderY: -0.35f, shoulderZ: 0.1f);
            m.Begin(-0.3f, 0.1f, 2f, 0f, 0f);
            m.TryGetLocalBlade(out var hx, out var hy, out var hz, out var tx, out var ty, out var tz);
            double ax = hx - 0.2, ay = hy + 0.35, az = hz - 0.1;
            double bx = tx - 0.2, by = ty + 0.35, bz = tz - 0.1;
            var cross = System.Math.Sqrt(System.Math.Pow(ay * bz - az * by, 2) + System.Math.Pow(az * bx - ax * bz, 2) + System.Math.Pow(ax * by - ay * bx, 2));
            Assert.That(cross, Is.EqualTo(0.0).Within(1e-4));
            Assert.That(ax * bx + ay * by + az * bz, Is.GreaterThanOrEqualTo(0.0));
        }

        [Test]
        public void MoveIgnoresInvalidScreenHeightAndMovesBeforeBegin()
        {
            var m = new DragBladeModel();
            m.Move(100f, 100f, ScreenHeight);
            m.Begin(0f, 0f, 2f, 0f, 0f);
            m.Move(500f, 0f, 0f);
            m.TryGetLocalBlade(out _, out _, out _, out var tx, out _, out _);
            Assert.That(tx, Is.EqualTo(0f));
        }
    }
}
