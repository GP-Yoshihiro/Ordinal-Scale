using NUnit.Framework;
using OrdinalScale.Core.Spatial;

namespace OrdinalScale.Core.Tests
{
    public class PlacementGateTests
    {
        private static SpatialStatus Status(CameraPermission permission, TrackingPhase phase, int planes)
            => new SpatialStatus(permission, phase, planes);

        [Test]
        public void AllowsPlacementWhenTrackingWithPlane()
        {
            var status = Status(CameraPermission.Granted, TrackingPhase.Tracking, 1);
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(PlacementBlockReason.None));
        }

        [Test]
        public void CameraDeniedTakesPriorityOverEverything()
        {
            var status = Status(CameraPermission.Denied, TrackingPhase.Unsupported, 0);
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(PlacementBlockReason.CameraPermissionDenied));
        }

        [TestCase(TrackingPhase.Unsupported, PlacementBlockReason.TrackingUnsupported)]
        [TestCase(TrackingPhase.Initializing, PlacementBlockReason.TrackingInitializing)]
        [TestCase(TrackingPhase.Limited, PlacementBlockReason.TrackingLimited)]
        public void BlocksUntilTrackingIsNormal(TrackingPhase phase, PlacementBlockReason expected)
        {
            // 平面が残っていても、追跡が正常でなければ配置させない（位置がずれた平面に置くのを防ぐ）
            var status = Status(CameraPermission.Granted, phase, 3);
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(expected));
        }

        [Test]
        public void BlocksWhenNoPlaneDetected()
        {
            var status = Status(CameraPermission.Granted, TrackingPhase.Tracking, 0);
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(PlacementBlockReason.NoPlaneDetected));
        }

        [Test]
        public void UnknownPermissionDoesNotBlockOnceTracking()
        {
            // 許可の問い合わせ結果が取れない端末でも、実際に追跡できていればカメラは使えている
            var status = Status(CameraPermission.Unknown, TrackingPhase.Tracking, 1);
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(PlacementBlockReason.None));
        }

        [Test]
        public void NegativePlaneCountIsTreatedAsZero()
        {
            var status = Status(CameraPermission.Granted, TrackingPhase.Tracking, -1);
            Assert.That(status.HorizontalPlaneCount, Is.EqualTo(0));
            Assert.That(PlacementGate.Evaluate(status), Is.EqualTo(PlacementBlockReason.NoPlaneDetected));
        }

        [Test]
        public void TargetOffPlaneIsRejectedEvenWhenReady()
        {
            var status = Status(CameraPermission.Granted, TrackingPhase.Tracking, 1);
            Assert.That(PlacementGate.EvaluateTarget(status, targetIsOnPlane: false), Is.EqualTo(PlacementBlockReason.TargetNotOnPlane));
            Assert.That(PlacementGate.EvaluateTarget(status, targetIsOnPlane: true), Is.EqualTo(PlacementBlockReason.None));
        }

        [Test]
        public void TargetEvaluationReportsStateProblemFirst()
        {
            var status = Status(CameraPermission.Granted, TrackingPhase.Limited, 1);
            Assert.That(PlacementGate.EvaluateTarget(status, targetIsOnPlane: true), Is.EqualTo(PlacementBlockReason.TrackingLimited));
        }
    }
}
