using NUnit.Framework;
using OrdinalScale.Core.Spatial;

namespace OrdinalScale.Core.Tests
{
    public class EnemyPlacementSessionTests
    {
        private static readonly SpatialStatus Ready = new SpatialStatus(CameraPermission.Granted, TrackingPhase.Tracking, 1);
        private static readonly SpatialStatus NoPlane = new SpatialStatus(CameraPermission.Granted, TrackingPhase.Tracking, 0);
        private static readonly SpatialStatus Limited = new SpatialStatus(CameraPermission.Granted, TrackingPhase.Limited, 2);

        [Test]
        public void FirstValidTapPlacesEnemy()
        {
            var session = new EnemyPlacementSession();
            var result = session.Attempt(Ready, SurfaceKind.HorizontalUp);

            Assert.That(result.Outcome, Is.EqualTo(PlacementOutcome.Placed));
            Assert.That(result.Reason, Is.EqualTo(PlacementBlockReason.None));
            Assert.That(result.AttemptNumber, Is.EqualTo(1));
            Assert.That(session.HasEnemy, Is.True);
        }

        [Test]
        public void SecondValidTapMovesTheSameSingleEnemy()
        {
            var session = new EnemyPlacementSession();
            session.Attempt(Ready, SurfaceKind.HorizontalUp);
            var result = session.Attempt(Ready, SurfaceKind.HorizontalUp);

            Assert.That(result.Outcome, Is.EqualTo(PlacementOutcome.Moved));
            Assert.That(session.PlacedCount, Is.EqualTo(2));
        }

        [TestCase(SurfaceKind.None, PlacementBlockReason.TargetNotOnPlane)]
        [TestCase(SurfaceKind.Vertical, PlacementBlockReason.TargetNotHorizontal)]
        public void InvalidTargetDoesNotPlaceEnemy(SurfaceKind surface, PlacementBlockReason expected)
        {
            var session = new EnemyPlacementSession();
            var result = session.Attempt(Ready, surface);

            Assert.That(result.IsBlocked, Is.True);
            Assert.That(result.Reason, Is.EqualTo(expected));
            Assert.That(session.HasEnemy, Is.False);
        }

        [Test]
        public void NoPlaneDetectedBlocksEvenIfRayHitsSomething()
        {
            // 平面数0なのに当たり判定が上向き面を返すような食い違いでも、未検出として抑止する
            var session = new EnemyPlacementSession();
            var result = session.Attempt(NoPlane, SurfaceKind.HorizontalUp);

            Assert.That(result.Reason, Is.EqualTo(PlacementBlockReason.NoPlaneDetected));
            Assert.That(session.HasEnemy, Is.False);
        }

        [Test]
        public void BlockedTapKeepsExistingEnemyWhereItWas()
        {
            var session = new EnemyPlacementSession();
            session.Attempt(Ready, SurfaceKind.HorizontalUp);
            var result = session.Attempt(Limited, SurfaceKind.HorizontalUp);

            Assert.That(result.Outcome, Is.EqualTo(PlacementOutcome.Blocked));
            Assert.That(result.Reason, Is.EqualTo(PlacementBlockReason.TrackingLimited));
            Assert.That(session.HasEnemy, Is.True, "抑止した操作で既存の敵を消してはいけない");
        }

        [Test]
        public void CountsAndLastResultTrackEveryAttempt()
        {
            var session = new EnemyPlacementSession();
            session.Attempt(NoPlane, SurfaceKind.None);
            session.Attempt(Ready, SurfaceKind.HorizontalUp);
            session.Attempt(Ready, SurfaceKind.None);

            Assert.That(session.AttemptCount, Is.EqualTo(3));
            Assert.That(session.PlacedCount, Is.EqualTo(1));
            Assert.That(session.BlockedCount, Is.EqualTo(2));
            Assert.That(session.LastResult.AttemptNumber, Is.EqualTo(3));
            Assert.That(session.LastResult.Reason, Is.EqualTo(PlacementBlockReason.TargetNotOnPlane));
        }

        [Test]
        public void AfterRemovalNextValidTapPlacesAgain()
        {
            var session = new EnemyPlacementSession();
            session.Attempt(Ready, SurfaceKind.HorizontalUp);
            session.RemoveEnemy();

            Assert.That(session.HasEnemy, Is.False);
            Assert.That(session.Attempt(Ready, SurfaceKind.HorizontalUp).Outcome, Is.EqualTo(PlacementOutcome.Placed));
        }
    }
}
