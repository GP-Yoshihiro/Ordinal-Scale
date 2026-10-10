using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>出来事の分類（空振り・ゆっくり接触・触れ続け・同じ振りでの再接触・別の振り）を、判定結果を直接与えて確かめる。</summary>
    public class StrikeEventRecorderTests
    {
        private readonly StrikeEventRecorder _rec = new StrikeEventRecorder();
        private readonly List<StrikeEvent> _events = new List<StrikeEvent>();

        private static SwingState Idle(int id) => new SwingState(SwingPhase.Idle, id, 0.1f, true, SwingBreak.None);
        private static SwingState Started(int id, float v = 2f) => new SwingState(SwingPhase.Started, id, v, true, SwingBreak.None);
        private static SwingState Swinging(int id, float v = 3f) => new SwingState(SwingPhase.Swinging, id, v, true, SwingBreak.None);
        private static SwingState Ended(int id, SwingBreak b = SwingBreak.SlowedDown) => new SwingState(SwingPhase.Ended, id, 0f, true, b);

        private void Feed(SwingState s, HitVerdict v) => _rec.Process(s, v, _events);

        private List<StrikeEventKind> Kinds()
        {
            var kinds = new List<StrikeEventKind>();
            foreach (var e in _events) kinds.Add(e.Kind);
            return kinds;
        }

        private StrikeEvent Last(StrikeEventKind kind)
        {
            for (var i = _events.Count - 1; i >= 0; i--)
                if (_events[i].Kind == kind) return _events[i];
            Assert.That(false, Is.True, $"{kind} が記録されていない");
            return default;
        }

        [Test]
        public void SwingWithoutContactIsWhiff()
        {
            Feed(Started(1), HitVerdict.NoContact);
            Feed(Swinging(1, 4f), HitVerdict.NoContact);
            Feed(Ended(1), HitVerdict.NoContact);

            Assert.That(Kinds(), Is.EqualTo(new[] { StrikeEventKind.SwingStarted, StrikeEventKind.SwingEnded }));
            Assert.That(Last(StrikeEventKind.SwingEnded).Outcome, Is.EqualTo(SwingOutcome.Whiff));
            Assert.That(Last(StrikeEventKind.SwingEnded).Speed, Is.EqualTo(4f), "振りの間の最大の速さを記録する");
            Assert.That(_rec.Whiffs, Is.EqualTo(1));
            Assert.That(_rec.Hits, Is.EqualTo(0));
        }

        [Test]
        public void ContactOutsideSwingIsSlowContact()
        {
            Feed(Idle(0), HitVerdict.NotSwinging);
            Feed(Idle(0), HitVerdict.ContactContinued);
            Feed(Idle(0), HitVerdict.NoContact);

            Assert.That(Kinds(), Is.EqualTo(new[] { StrikeEventKind.SlowContact, StrikeEventKind.ContactEnded }));
            Assert.That(_rec.SlowContacts, Is.EqualTo(1));
            Assert.That(_rec.Swings, Is.EqualTo(0), "ゆっくり接触は振りではない");
        }

        [Test]
        public void HoldingAfterHitIsOneHitAndOneHeldContact()
        {
            Feed(Started(1), HitVerdict.Hit);
            for (var i = 0; i < 10; i++) Feed(Swinging(1), HitVerdict.ContactContinued);
            Feed(Ended(1), HitVerdict.ContactContinued);
            Feed(Idle(1), HitVerdict.ContactContinued);
            Feed(Idle(1), HitVerdict.NoContact);

            Assert.That(_rec.Hits, Is.EqualTo(1));
            Assert.That(_rec.HeldContacts, Is.EqualTo(1));
            var end = Last(StrikeEventKind.ContactEnded);
            Assert.That(end.ContactFrames, Is.EqualTo(13));
            Assert.That(end.ContactHadHit, Is.True);
            Assert.That(Last(StrikeEventKind.SwingEnded).Outcome, Is.EqualTo(SwingOutcome.Hit));
        }

        [Test]
        public void SingleFrameContactIsNotHeld()
        {
            // 速い振りで1フレームだけ触れて抜けた場合は「触れ続け」に数えない
            Feed(Started(1), HitVerdict.Hit);
            Feed(Swinging(1), HitVerdict.NoContact);
            Assert.That(_rec.HeldContacts, Is.EqualTo(0));
            Assert.That(Last(StrikeEventKind.ContactEnded).ContactFrames, Is.EqualTo(1));
        }

        [Test]
        public void PressThenSwingWhileTouchingIsContactWithoutHit()
        {
            // 押し当て（振り外で接触）→ 触れたまま振る → 振りの結果は「接触したが命中なし」
            Feed(Idle(0), HitVerdict.NotSwinging);
            Feed(Started(1), HitVerdict.ContactContinued);
            Feed(Swinging(1), HitVerdict.ContactContinued);
            Feed(Ended(1), HitVerdict.ContactContinued);

            Assert.That(_rec.Hits, Is.EqualTo(0));
            Assert.That(_rec.Whiffs, Is.EqualTo(0));
            Assert.That(Last(StrikeEventKind.SwingEnded).Outcome, Is.EqualTo(SwingOutcome.ContactWithoutHit));
        }

        [Test]
        public void RecontactInSameSwingIsRepeatContact()
        {
            Feed(Started(1), HitVerdict.Hit);
            Feed(Swinging(1), HitVerdict.NoContact);
            Feed(Swinging(1), HitVerdict.AlreadyHitThisSwing);
            Feed(Swinging(1), HitVerdict.NoContact);
            Feed(Ended(1), HitVerdict.NoContact);

            Assert.That(Kinds(), Is.EqualTo(new[]
            {
                StrikeEventKind.SwingStarted, StrikeEventKind.Hit, StrikeEventKind.ContactEnded,
                StrikeEventKind.RepeatContact, StrikeEventKind.ContactEnded, StrikeEventKind.SwingEnded,
            }));
            Assert.That(_rec.Hits, Is.EqualTo(1));
            Assert.That(_rec.RepeatContacts, Is.EqualTo(1));
            Assert.That(Last(StrikeEventKind.SwingEnded).Outcome, Is.EqualTo(SwingOutcome.Hit));
        }

        [Test]
        public void SeparateSwingsAreCountedSeparately()
        {
            Feed(Started(1), HitVerdict.Hit);
            Feed(Swinging(1), HitVerdict.NoContact);
            Feed(Ended(1), HitVerdict.NoContact);
            Feed(Idle(1), HitVerdict.NoContact);
            Feed(Started(2), HitVerdict.NoContact);
            Feed(Swinging(2), HitVerdict.Hit);
            Feed(Ended(2), HitVerdict.NoContact);

            Assert.That(_rec.Swings, Is.EqualTo(2));
            Assert.That(_rec.Hits, Is.EqualTo(2));
            Assert.That(Last(StrikeEventKind.Hit).SwingId, Is.EqualTo(2));
        }

        [Test]
        public void SwingEndedByReleaseKeepsTheReason()
        {
            Feed(Started(1), HitVerdict.NoContact);
            Feed(Ended(1, SwingBreak.TrackingLost), HitVerdict.NoContact);
            Assert.That(Last(StrikeEventKind.SwingEnded).Break, Is.EqualTo(SwingBreak.TrackingLost));
        }

        [Test]
        public void ProcessAppendsWithoutClearingAndReturnsCount()
        {
            _events.Add(new StrikeEvent(StrikeEventKind.Hit, 99, 0f));
            var n = _rec.Process(Started(1), HitVerdict.Hit, _events);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(_events.Count, Is.EqualTo(3));
        }

        [Test]
        public void ResetClearsCounters()
        {
            Feed(Started(1), HitVerdict.Hit);
            Feed(Ended(1), HitVerdict.NoContact);
            _rec.Reset();
            Assert.That(_rec.Hits, Is.EqualTo(0));
            Assert.That(_rec.Swings, Is.EqualTo(0));
        }

        [Test]
        public void EveryKindHasJapaneseAndShortText()
        {
            foreach (StrikeEventKind kind in System.Enum.GetValues(typeof(StrikeEventKind)))
            {
                var e = new StrikeEvent(kind, 1, 2f, SwingOutcome.Whiff, SwingBreak.SlowedDown, 3, true);
                Assert.That(StrikeMessages.Describe(e), Does.Not.StartWith(kind.ToString()));
                Assert.That(StrikeMessages.ShortCode(e), Is.Not.Empty);
            }

            Assert.That(StrikeMessages.Describe(new StrikeEvent(StrikeEventKind.SwingEnded, 1, 2f, SwingOutcome.Whiff)), Does.Contain("空振り"));
            Assert.That(StrikeMessages.Describe(new StrikeEvent(StrikeEventKind.ContactEnded, 1, 0f, contactFrames: 5)), Does.Contain("触れ続け"));
        }
    }
}
