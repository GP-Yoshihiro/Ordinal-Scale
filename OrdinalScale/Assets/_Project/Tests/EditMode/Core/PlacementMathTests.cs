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

        [Test]
        public void PointInFrontFollowsHorizontalGaze()
        {
            // (1, 2) に立って +X を見ている → 2m 先は (3, 2)
            Assert.That(PlacementMath.TryPointInFront(1f, 2f, 1f, 0f, 2f, out var x, out var z), Is.True);
            Assert.That(x, Is.EqualTo(3f).Within(1e-5f));
            Assert.That(z, Is.EqualTo(2f).Within(1e-5f));
        }

        [Test]
        public void PointInFrontIgnoresLookingDown()
        {
            // 少しうつむいていても（水平成分が短くても）距離は水平に 2m のまま
            Assert.That(PlacementMath.TryPointInFront(0f, 0f, 0f, 0.5f, 2f, out var x, out var z), Is.True);
            Assert.That(x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(z, Is.EqualTo(2f).Within(1e-5f));
        }

        [Test]
        public void PointInFrontNormalizesDiagonalGaze()
        {
            Assert.That(PlacementMath.TryPointInFront(0f, 0f, 1f, 1f, 2f, out var x, out var z), Is.True);
            Assert.That(x, Is.EqualTo(1.41421f).Within(1e-4f));
            Assert.That(z, Is.EqualTo(1.41421f).Within(1e-4f));
        }

        [Test]
        public void PointInFrontUndefinedWhenLookingStraightDown()
        {
            // 真下を見ていると正面が決まらないので配置しない（次のフレームで再試行させる）
            Assert.That(PlacementMath.TryPointInFront(5f, 6f, 0.05f, 0f, 2f, out var x, out var z), Is.False);
            Assert.That(x, Is.EqualTo(5f));
            Assert.That(z, Is.EqualTo(6f));
        }

        [Test]
        public void PointInFrontRejectsNonPositiveDistance()
        {
            Assert.That(PlacementMath.TryPointInFront(0f, 0f, 0f, 1f, 0f, out _, out _), Is.False);
        }
    }
}
