using System;
using System.Collections.Generic;
using NUnit.Framework;
using OrdinalScale.Core.Spatial;

namespace OrdinalScale.Core.Tests
{
    public class PlacementMessagesTests
    {
        [Test]
        public void EveryBlockReasonHasItsOwnExplicitHint()
        {
            // 新しい理由を足したときに文言の追加漏れ（既定文への落ち込み・重複）を検出する
            var seen = new HashSet<string>();
            foreach (PlacementBlockReason reason in Enum.GetValues(typeof(PlacementBlockReason)))
            {
                if (reason == PlacementBlockReason.None) continue;

                var hint = PlacementMessages.Hint(reason);
                Assert.That(hint, Is.Not.Empty, reason.ToString());
                Assert.That(hint, Does.Not.StartWith("配置できません（"), $"{reason} に専用の文言がない");
                Assert.That(seen.Add(hint), Is.True, $"{reason} の文言が他の理由と重複している");
            }
        }

        [Test]
        public void NoneHasNoHint()
        {
            Assert.That(PlacementMessages.Hint(PlacementBlockReason.None), Is.Empty);
        }

        [Test]
        public void DescribeIncludesAttemptNumberCodeAndHint()
        {
            var blocked = new PlacementAttemptResult(PlacementOutcome.Blocked, PlacementBlockReason.TargetNotOnPlane, 4);
            var text = PlacementMessages.Describe(blocked);

            Assert.That(text, Does.StartWith("#4 "));
            Assert.That(text, Does.Contain("TargetNotOnPlane"));
            Assert.That(text, Does.Contain(PlacementMessages.Hint(PlacementBlockReason.TargetNotOnPlane)));
        }

        [Test]
        public void DescribeDistinguishesPlacedAndMoved()
        {
            var placed = PlacementMessages.Describe(new PlacementAttemptResult(PlacementOutcome.Placed, PlacementBlockReason.None, 1));
            var moved = PlacementMessages.Describe(new PlacementAttemptResult(PlacementOutcome.Moved, PlacementBlockReason.None, 2));

            Assert.That(placed, Does.Contain("Placed"));
            Assert.That(moved, Does.Contain("Moved"));
        }
    }
}
