using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>Q4：ヘッドセット内の2択パネル。レイとパネルの当たり、選択の誤操作防止。</summary>
    public class ChoicePanelsTests
    {
        // 正面1m・目の高さ1.5m に、幅0.5m・高さ0.3m のパネル（右=+X、上=+Y）
        private static readonly ChoicePanel Front = new ChoicePanel(0f, 1.5f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 0.25f, 0.15f);

        [Test]
        public void RayThroughTheCenterHits()
        {
            Assert.That(Front.TryHit(0f, 1.5f, 0f, 0f, 0f, 1f, 5f, out var d), Is.True);
            Assert.That(d, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void RayNearTheEdgeHitsAndOutsideMisses()
        {
            Assert.That(Front.TryHit(0.24f, 1.5f, 0f, 0f, 0f, 1f, 5f, out _), Is.True);
            Assert.That(Front.TryHit(0.26f, 1.5f, 0f, 0f, 0f, 1f, 5f, out _), Is.False);
            Assert.That(Front.TryHit(0f, 1.66f, 0f, 0f, 0f, 1f, 5f, out _), Is.False);
        }

        [Test]
        public void AngledRayHitsWhereItCrossesThePlane()
        {
            // 手元 (0.3, 1.2, 0) から斜め前へ：1m 先で (0.1, 1.5) を通る → 当たる
            Assert.That(Front.TryHit(0.3f, 1.2f, 0f, -0.2f, 0.3f, 1f, 5f, out _), Is.True);
        }

        [Test]
        public void RayPointingAwayOrParallelMisses()
        {
            Assert.That(Front.TryHit(0f, 1.5f, 0f, 0f, 0f, -1f, 5f, out _), Is.False, "後ろ向き");
            Assert.That(Front.TryHit(0f, 1.5f, 0f, 1f, 0f, 0f, 5f, out _), Is.False, "パネルと平行");
            Assert.That(Front.TryHit(0f, 1.5f, -10f, 0f, 0f, 1f, 5f, out _), Is.False, "届く距離より遠い");
        }

        [Test]
        public void CannotSelectBeforeArmTime()
        {
            // 剣を振っている途中のトリガーで決まらない
            var s = new ChoiceSelector(0.6);
            s.Show(10.0);
            Assert.That(s.Update(10.3, true, 0), Is.EqualTo(-1));
            Assert.That(s.Update(10.7, true, 1), Is.EqualTo(1));
        }

        [Test]
        public void SelectNeedsAPanelUnderTheRay()
        {
            var s = new ChoiceSelector(0.0);
            s.Show(0.0);
            Assert.That(s.Update(1.0, true, -1), Is.EqualTo(-1), "何も指していない");
            Assert.That(s.Update(1.0, false, 0), Is.EqualTo(-1), "トリガーを押していない");
            Assert.That(s.Update(1.1, true, 0), Is.EqualTo(0));
        }

        [Test]
        public void OnlyOneChoicePerShow()
        {
            var s = new ChoiceSelector(0.0);
            s.Show(0.0);
            Assert.That(s.Update(1.0, true, 1), Is.EqualTo(1));
            Assert.That(s.Update(1.5, true, 0), Is.EqualTo(-1), "連打で2つ目を選ばない");
            s.Hide();
            s.Show(2.0);
            Assert.That(s.Update(3.0, true, 0), Is.EqualTo(0), "表示し直せば選べる");
        }

        [Test]
        public void HiddenPanelsCannotBeSelected()
        {
            var s = new ChoiceSelector(0.0);
            Assert.That(s.Update(1.0, true, 0), Is.EqualTo(-1));
            s.Show(0.0);
            s.Hide();
            Assert.That(s.Update(1.0, true, 0), Is.EqualTo(-1));
        }
    }
}
