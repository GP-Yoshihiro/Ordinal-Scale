using NUnit.Framework;
using OrdinalScale.Core.Spatial;

namespace OrdinalScale.Core.Tests
{
    public class PlacementMathTests
    {
        [TestCase(0f, 1f, 0f)]    // プレイヤーが +Z 側 → 0°
        [TestCase(1f, 0f, 90f)]   // +X 側 → 90°
        [TestCase(-1f, 0f, -90f)] // -X 側 → -90°
        public void EnemyTurnsTowardPlayer(float playerX, float playerZ, float expectedYaw)
        {
            Assert.That(PlacementMath.TryYawDegreesToFace(0f, 0f, playerX, playerZ, out var yaw), Is.True);
            Assert.That(yaw, Is.EqualTo(expectedYaw).Within(1e-4f));
        }

        [Test]
        public void PlayerBehindGivesHalfTurn()
        {
            Assert.That(PlacementMath.TryYawDegreesToFace(0f, 0f, 0f, -1f, out var yaw), Is.True);
            Assert.That(System.Math.Abs(yaw), Is.EqualTo(180f).Within(1e-4f));
        }

        [Test]
        public void UsesRelativePositionNotAbsolute()
        {
            // 敵が原点以外にいても、敵から見たプレイヤーの方向で決まる
            Assert.That(PlacementMath.TryYawDegreesToFace(2f, 3f, 3f, 4f, out var yaw), Is.True);
            Assert.That(yaw, Is.EqualTo(45f).Within(1e-4f));
        }

        [Test]
        public void SamePositionHasNoDefinedFacing()
        {
            Assert.That(PlacementMath.TryYawDegreesToFace(1f, 1f, 1.001f, 1f, out var yaw), Is.False);
            Assert.That(yaw, Is.EqualTo(0f));
        }
    }
}
