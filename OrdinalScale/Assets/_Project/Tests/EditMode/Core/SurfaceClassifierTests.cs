using System;
using NUnit.Framework;
using OrdinalScale.Core.Spatial;

namespace OrdinalScale.Core.Tests
{
    public class SurfaceClassifierTests
    {
        private static float NormalYAtTiltDegrees(double tiltFromUp) => (float)Math.Cos(tiltFromUp * Math.PI / 180.0);

        [TestCase(1f, SurfaceKind.HorizontalUp)]
        [TestCase(-1f, SurfaceKind.HorizontalDown)]
        [TestCase(0f, SurfaceKind.Vertical)]
        public void ClassifiesAxisAlignedNormals(float normalY, SurfaceKind expected)
        {
            Assert.That(SurfaceClassifier.Classify(normalY), Is.EqualTo(expected));
        }

        [Test]
        public void SlightlyTiltedFloorIsStillHorizontal()
        {
            // 実際の床の検出面や Editor の床は数度傾くことがある。許容誤差内なら床として扱う
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(9)), Is.EqualTo(SurfaceKind.HorizontalUp));
        }

        [Test]
        public void SlopeBeyondToleranceIsNotHorizontal()
        {
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(11)), Is.EqualTo(SurfaceKind.Other));
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(45)), Is.EqualTo(SurfaceKind.Other));
        }

        [Test]
        public void SlightlyTiltedWallIsStillVertical()
        {
            // 境界（ちょうど10°）は浮動小数の誤差でどちらにも転ぶため、境界から離れた値で確かめる
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(95)), Is.EqualTo(SurfaceKind.Vertical));
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(85)), Is.EqualTo(SurfaceKind.Vertical));
            Assert.That(SurfaceClassifier.Classify(NormalYAtTiltDegrees(75)), Is.EqualTo(SurfaceKind.Other));
        }

        [Test]
        public void NaNNormalIsTreatedAsNoSurface()
        {
            Assert.That(SurfaceClassifier.Classify(float.NaN), Is.EqualTo(SurfaceKind.None));
        }
    }
}
