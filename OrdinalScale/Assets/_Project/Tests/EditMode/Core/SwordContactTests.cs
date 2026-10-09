using NUnit.Framework;
using OrdinalScale.Core.Combat;

namespace OrdinalScale.Core.Tests
{
    /// <summary>刃と体のカプセルの接触（C6：体全体が同じ当たり判定）と、前後フレームの掃引。</summary>
    public class SwordContactTests
    {
        // 足元 (0,0,0)、高さ1.6m、半径0.25m → 中心線は y=0.25〜1.35
        private static readonly BodyCapsule Body = new BodyCapsule(0f, 0f, 0f, 1.6f, 0.25f);

        private static BladeSample Blade(float hx, float hy, float hz, float tx, float ty, float tz)
        {
            return new BladeSample(0, true, hx, hy, hz, tx, ty, tz);
        }

        [Test]
        public void ParallelSegmentsDistance()
        {
            var d = SwordContact.SegmentSegmentDistance(0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 0);
            Assert.That(d, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void CrossingSegmentsHaveZeroDistance()
        {
            var d = SwordContact.SegmentSegmentDistance(-1, 0, 0, 1, 0, 0, 0, -1, 0, 0, 1, 0);
            Assert.That(d, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void SkewSegmentsUseClosestInteriorPoints()
        {
            // X 軸上の線分と、z=2 で Y 方向の線分 → 距離2
            var d = SwordContact.SegmentSegmentDistance(-1, 0, 0, 1, 0, 0, 0, -1, 2, 0, 1, 2);
            Assert.That(d, Is.EqualTo(2f).Within(1e-5f));
        }

        [Test]
        public void ClosestPointCanBeAnEndpoint()
        {
            // 線分の延長上なら交わるが、線分としては端点どうしが最短（距離1）
            var d = SwordContact.SegmentSegmentDistance(0, 0, 0, 1, 0, 0, 2, 0, 0, 2, 1, 0);
            Assert.That(d, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void DegeneratePointSegmentsWork()
        {
            var d = SwordContact.SegmentSegmentDistance(0, 0, 0, 0, 0, 0, 3, 4, 0, 3, 4, 0);
            Assert.That(d, Is.EqualTo(5f).Within(1e-5f));
        }

        [TestCase(1.2f, true)]   // 胸の高さ
        [TestCase(0.1f, true)]   // 足元の半球
        [TestCase(1.55f, true)]  // 頭頂のすぐ下（中心線上端1.35＋半径0.25）
        [TestCase(1.7f, false)]  // 頭の上
        public void WholeBodyIsOneHitVolume(float height, bool expected)
        {
            // C6：部位差なし。刃先が体の中心線の真上・真下・横のどこでも同じ規則で判定する
            var blade = Blade(-1f, height, 0f, 0f, height, 0f);
            Assert.That(SwordContact.Touches(default, false, blade, Body, SwordTuning.Default), Is.EqualTo(expected));
        }

        [Test]
        public void BladeRadiusExtendsReach()
        {
            // 刃先が体の表面から 1.5cm 手前：刃の半径2cmなら接触、0なら非接触
            var blade = Blade(0f, 1f, -1.5f, 0f, 1f, -0.265f);
            Assert.That(SwordContact.Touches(default, false, blade, Body, new SwordTuning(bladeRadius: 0.02f)), Is.True);
            Assert.That(SwordContact.Touches(default, false, blade, Body, new SwordTuning(bladeRadius: 0f)), Is.False);
        }

        [Test]
        public void UntrackedBladeNeverTouches()
        {
            Assert.That(SwordContact.Touches(default, false, BladeSample.Untracked(1), Body, SwordTuning.Default), Is.False);
        }

        [Test]
        public void SweepCatchesABladeThatCrossesBetweenFrames()
        {
            // 前後のフレームは体の左右の外。単独では非接触だが、間の掃引で接触になる
            var left = Blade(-0.5f, 1f, -1f, -0.5f, 1f, 0f);
            var right = Blade(0.5f, 1f, -1f, 0.5f, 1f, 0f);
            Assert.That(SwordContact.Touches(default, false, left, Body, SwordTuning.Default), Is.False);
            Assert.That(SwordContact.Touches(default, false, right, Body, SwordTuning.Default), Is.False);
            Assert.That(SwordContact.Touches(left, true, right, Body, SwordTuning.Default), Is.True);
        }

        [Test]
        public void SweepIgnoresUntrackedPreviousSample()
        {
            // 前のサンプルが追跡外なら掃引しない（復帰時に途中の空間を切ったことにしない）
            var right = Blade(0.5f, 1f, -1f, 0.5f, 1f, 0f);
            Assert.That(SwordContact.Touches(BladeSample.Untracked(0), true, right, Body, SwordTuning.Default), Is.False);
        }

        [Test]
        public void SweepThatStaysOutsideDoesNotTouch()
        {
            var a = Blade(-0.5f, 2f, -1f, -0.5f, 2f, 0f);
            var b = Blade(0.5f, 2f, -1f, 0.5f, 2f, 0f);
            Assert.That(SwordContact.Touches(a, true, b, Body, SwordTuning.Default), Is.False);
        }

        [Test]
        public void InvalidCapsuleIsRejected()
        {
            Assert.That(() => new BodyCapsule(0f, 0f, 0f, 0.4f, 0.25f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new BodyCapsule(0f, 0f, 0f, 1.6f, 0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
