// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IntervalTests
    {
        // IntervalEndpoint tests
        [TestMethod]
        [Timeout(60000)]
        public void BoundedInclusiveIntervalEndpoint()
        {
            var boundedInclusive = new IntervalEndpoint<int>(42, true);
            Assert.IsTrue(boundedInclusive.Bounded);
            Assert.IsTrue(boundedInclusive.Inclusive);
            Assert.AreEqual(42, boundedInclusive.Point);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BoundedExclusiveIntervalEndpoint()
        {
            var boundedExclusive = new IntervalEndpoint<int>(7, false);
            Assert.IsTrue(boundedExclusive.Bounded);
            Assert.IsFalse(boundedExclusive.Inclusive);
            Assert.AreEqual(7, boundedExclusive.Point);
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnboundedIntervalEndpoint()
        {
            var unbounded = new IntervalEndpoint<int>(int.MinValue);
            Assert.IsFalse(unbounded.Bounded);
            Assert.IsFalse(unbounded.Inclusive); // implied by being unbounded
            Assert.AreEqual(int.MinValue, unbounded.Point);
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnboundedIntervalEndpointNonsensePoint()
        {
            var unbounded = new IntervalEndpoint<int>(int.MaxValue);
            Assert.IsFalse(unbounded.Bounded);
            Assert.IsFalse(unbounded.Inclusive); // implied by being unbounded
            Assert.AreEqual(int.MaxValue, unbounded.Point);
        }

        // Interval tests
        [TestMethod]
        [Timeout(60000)]
        public void BoundedIntInterval()
        {
            var interval = new IntInterval(7, true, 42, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42, interval.Right);
            Assert.AreEqual(42 - 7, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsTrue(interval.IsFinite);
            Assert.IsFalse(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void LeftBoundedIntInterval()
        {
            var interval = RealInterval.LeftBounded(7.0);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7.0, interval.Left);
            Assert.IsFalse(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(double.MaxValue, interval.Right);
            Assert.AreEqual(double.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RightBoundedIntInterval()
        {
            var interval = RealInterval.RightBounded(42);
            Assert.IsFalse(interval.LeftEndpoint.Bounded);
            Assert.IsFalse(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(double.MinValue, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsTrue(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42, interval.Right);
            Assert.AreEqual(double.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BoundedRealInterval()
        {
            var interval = new RealInterval(7.0, true, 42.0, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7.0, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42.0, interval.Right);
            Assert.AreEqual(42.0 - 7.0, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsTrue(interval.IsFinite);
            Assert.IsFalse(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void LeftBoundedRealInterval()
        {
            var interval = RealInterval.LeftBounded(7.0);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7.0, interval.Left);
            Assert.IsFalse(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(double.MaxValue, interval.Right);
            Assert.AreEqual(double.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RightBoundedRealInterval()
        {
            var interval = RealInterval.RightBounded(42.0);
            Assert.IsFalse(interval.LeftEndpoint.Bounded);
            Assert.IsFalse(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(double.MinValue, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsTrue(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42.0, interval.Right);
            Assert.AreEqual(double.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BoundedTimeInterval()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(10);
            var interval = new TimeInterval(start, true, end, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(start, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(end, interval.Right);
            Assert.AreEqual(end.Subtract(start), interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsTrue(interval.IsFinite);
            Assert.IsFalse(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void LeftBoundedTimeInterval()
        {
            var start = DateTime.UtcNow;
            var interval = TimeInterval.LeftBounded(start);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(start, interval.Left);
            Assert.IsFalse(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(DateTime.MaxValue, interval.Right);
            Assert.AreEqual(TimeSpan.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RightBoundedTimeInterval()
        {
            var end = DateTime.UtcNow;
            var interval = TimeInterval.RightBounded(end);
            Assert.IsFalse(interval.LeftEndpoint.Bounded);
            Assert.IsFalse(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(DateTime.MinValue, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsTrue(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(end, interval.Right);
            Assert.AreEqual(TimeSpan.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BoundedTimeSpanInterval()
        {
            var start = TimeSpan.FromMinutes(-5);
            var end = TimeSpan.FromMinutes(10);
            var interval = new RelativeTimeInterval(start, true, end, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(start, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(end, interval.Right);
            Assert.AreEqual(end.Subtract(start), interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsTrue(interval.IsFinite);
            Assert.IsFalse(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void LeftBoundedTimeSpanInterval()
        {
            var start = TimeSpan.FromMinutes(-5);
            var end = TimeSpan.FromMinutes(10);
            var interval = RelativeTimeInterval.LeftBounded(start);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(start, interval.Left);
            Assert.IsFalse(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(TimeSpan.MaxValue, interval.Right);
            Assert.AreEqual(TimeSpan.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RightBoundedTimeSpanInterval()
        {
            var end = TimeSpan.FromMinutes(10);
            var interval = RelativeTimeInterval.RightBounded(end);
            Assert.IsFalse(interval.LeftEndpoint.Bounded);
            Assert.IsFalse(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(TimeSpan.MinValue, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsTrue(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(end, interval.Right);
            Assert.AreEqual(TimeSpan.MaxValue, interval.Span);
            Assert.IsFalse(interval.IsClosed);
            Assert.IsFalse(interval.IsDegenerate);
            Assert.IsFalse(interval.IsFinite);
            Assert.IsTrue(interval.IsHalfBounded);
            Assert.IsFalse(interval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void TimeIntervalFromDateTimeAndTimeSpanInterval()
        {
            var start = TimeSpan.FromMinutes(-5);
            var end = TimeSpan.FromMinutes(10);
            var timeSpanInterval = new RelativeTimeInterval(start, end);
            var origin = DateTime.UtcNow;
            var timeInterval = new TimeInterval(origin, timeSpanInterval);
            Assert.IsTrue(timeInterval.LeftEndpoint.Bounded);
            Assert.IsTrue(timeInterval.LeftEndpoint.Inclusive);
            Assert.AreEqual(origin + start, timeInterval.Left);
            Assert.IsTrue(timeInterval.RightEndpoint.Bounded);
            Assert.IsTrue(timeInterval.RightEndpoint.Inclusive);
            Assert.AreEqual(origin + end, timeInterval.Right);
            Assert.AreEqual(TimeSpan.FromMinutes(15), timeInterval.Span);
            Assert.IsTrue(timeInterval.IsClosed);
            Assert.IsFalse(timeInterval.IsDegenerate);
            Assert.IsTrue(timeInterval.IsFinite);
            Assert.IsFalse(timeInterval.IsHalfBounded);
            Assert.IsFalse(timeInterval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void TimeIntervalFromDateTimeAndLeftBoundedTimeSpanInterval()
        {
            var start = TimeSpan.FromMinutes(-5);
            var timeSpanInterval = RelativeTimeInterval.LeftBounded(start);
            var leftBoundedTimeSpanInterval = RelativeTimeInterval.LeftBounded(start);
            var origin = DateTime.UtcNow;
            var timeInterval = new TimeInterval(origin, timeSpanInterval);
            Assert.IsTrue(timeInterval.LeftEndpoint.Bounded);
            Assert.IsTrue(timeInterval.LeftEndpoint.Inclusive);
            Assert.AreEqual(origin + start, timeInterval.Left);
            Assert.IsFalse(timeInterval.RightEndpoint.Bounded);
            Assert.IsFalse(timeInterval.RightEndpoint.Inclusive);
            Assert.AreEqual(DateTime.MaxValue, timeInterval.Right);
            Assert.AreEqual(TimeSpan.MaxValue, timeInterval.Span);
            Assert.IsFalse(timeInterval.IsClosed);
            Assert.IsFalse(timeInterval.IsDegenerate);
            Assert.IsFalse(timeInterval.IsFinite);
            Assert.IsTrue(timeInterval.IsHalfBounded);
            Assert.IsFalse(timeInterval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void TimeIntervalFromDateTimeAndTimeSpanIntervalUsingOperator()
        {
            var start = TimeSpan.FromMinutes(-5);
            var end = TimeSpan.FromMinutes(10);
            var timeSpanInterval = new RelativeTimeInterval(start, end);
            var origin = DateTime.UtcNow;
            var timeInterval = origin + timeSpanInterval;
            Assert.IsTrue(timeInterval.LeftEndpoint.Bounded);
            Assert.IsTrue(timeInterval.LeftEndpoint.Inclusive);
            Assert.AreEqual(origin + start, timeInterval.Left);
            Assert.IsTrue(timeInterval.RightEndpoint.Bounded);
            Assert.IsTrue(timeInterval.RightEndpoint.Inclusive);
            Assert.AreEqual(origin + end, timeInterval.Right);
            Assert.AreEqual(TimeSpan.FromMinutes(15), timeInterval.Span);
            Assert.IsTrue(timeInterval.IsClosed);
            Assert.IsFalse(timeInterval.IsDegenerate);
            Assert.IsTrue(timeInterval.IsFinite);
            Assert.IsFalse(timeInterval.IsHalfBounded);
            Assert.IsFalse(timeInterval.IsOpen);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NegativeIntervals()
        {
            Assert.AreEqual(7 - 42, new IntInterval(42, true, 7, true).Span);
            Assert.AreEqual(7.0 - 42.0, new RealInterval(42.0, true, 7.0, true).Span);
            Assert.AreEqual(TimeSpan.FromDays(365).Negate(), new TimeInterval(new DateTime(2016, 1, 1), true, new DateTime(2015, 1, 1), true).Span);
        }

        // interval operations
        [TestMethod]
        [Timeout(60000)]
        public void IntervalCenter()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(10);
            var span = end - start;
            var mid = end - new TimeSpan(span.Ticks / 2);
            var interval = new TimeInterval(start, true, end, false);
            Assert.AreEqual(mid, interval.Center);

            var leftBounded = TimeInterval.LeftBounded(start);
            Assert.AreEqual(DateTime.MaxValue, leftBounded.Center); // "center" when left-bounded is +"infinity"

            var rightBounded = TimeInterval.RightBounded(start);
            Assert.AreEqual(DateTime.MinValue, rightBounded.Center); // "center" when right-bounded is -"infinity"

            var infinite = TimeInterval.Infinite;
            Assert.AreEqual(DateTime.MinValue, infinite.Center); // "center" when unbounded is (arbitrarily) -"infinity"
        }

        [TestMethod]
        [Timeout(60000)]
        public void IntervalWithin()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(10);
            var span = end - start;
            var mid = end - new TimeSpan(span.Ticks / 2);
            var interval = new TimeInterval(start, true, end, false);
            Assert.IsTrue(interval.PointIsWithin(mid));
            Assert.IsTrue(interval.PointIsWithin(start));
            Assert.IsFalse(interval.PointIsWithin(end));
            Assert.IsFalse(interval.PointIsWithin(DateTime.MinValue));
            Assert.IsFalse(interval.PointIsWithin(DateTime.MaxValue));
        }

        [TestMethod]
        [Timeout(60000)]
        public void IntervalTranslation()
        {
            var interval = new IntInterval(7, true, 42, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42, interval.Right);
            Assert.AreEqual(42 - 7, interval.Span);

            var transRight = interval.Translate(100);
            Assert.IsTrue(transRight.LeftEndpoint.Bounded);
            Assert.IsTrue(transRight.LeftEndpoint.Inclusive);
            Assert.AreEqual(107, transRight.Left);
            Assert.IsTrue(transRight.RightEndpoint.Bounded);
            Assert.IsFalse(transRight.RightEndpoint.Inclusive);
            Assert.AreEqual(142, transRight.Right);
            Assert.AreEqual(42 - 7, transRight.Span);

            var plus = interval + 100; // same as interval.Translate(100)
            Assert.IsTrue(plus.LeftEndpoint.Bounded);
            Assert.IsTrue(plus.LeftEndpoint.Inclusive);
            Assert.AreEqual(107, plus.Left);
            Assert.IsTrue(plus.RightEndpoint.Bounded);
            Assert.IsFalse(plus.RightEndpoint.Inclusive);
            Assert.AreEqual(142, plus.Right);
            Assert.AreEqual(42 - 7, plus.Span);

            var transLeft = interval.Translate(100);
            Assert.IsTrue(transLeft.LeftEndpoint.Bounded);
            Assert.IsTrue(transLeft.LeftEndpoint.Inclusive);
            Assert.AreEqual(107, transLeft.Left);
            Assert.IsTrue(transLeft.RightEndpoint.Bounded);
            Assert.IsFalse(transLeft.RightEndpoint.Inclusive);
            Assert.AreEqual(142, transLeft.Right);
            Assert.AreEqual(42 - 7, transLeft.Span);

            var minus = interval - 5; // same as interval.Translate(5)
            Assert.IsTrue(minus.LeftEndpoint.Bounded);
            Assert.IsTrue(minus.LeftEndpoint.Inclusive);
            Assert.AreEqual(2, minus.Left);
            Assert.IsTrue(minus.RightEndpoint.Bounded);
            Assert.IsFalse(minus.RightEndpoint.Inclusive);
            Assert.AreEqual(37, minus.Right);
            Assert.AreEqual(42 - 7, minus.Span);
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnboundedIntervalTranslation()
        {
            var interval = IntInterval.LeftBounded(7);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, interval.Left);
            Assert.IsFalse(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(int.MaxValue, interval.Right);
            Assert.AreEqual(int.MaxValue, interval.Span);

            var translated = interval.Translate(100);
            Assert.IsTrue(translated.LeftEndpoint.Bounded);
            Assert.IsTrue(translated.LeftEndpoint.Inclusive);
            Assert.AreEqual(107, translated.Left);
            Assert.IsFalse(translated.RightEndpoint.Bounded);
            Assert.IsFalse(translated.RightEndpoint.Inclusive);
            Assert.AreEqual(int.MaxValue, translated.Right); // unbounded end does not move
            Assert.AreEqual(int.MaxValue, translated.Span);
        }

        [TestMethod]
        [Timeout(60000)]
        public void IntervalScaling()
        {
            var interval = new IntInterval(7, true, 42, false);
            Assert.IsTrue(interval.LeftEndpoint.Bounded);
            Assert.IsTrue(interval.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, interval.Left);
            Assert.IsTrue(interval.RightEndpoint.Bounded);
            Assert.IsFalse(interval.RightEndpoint.Inclusive);
            Assert.AreEqual(42, interval.Right);
            Assert.AreEqual(42 - 7, interval.Span);

            var byFactor = interval.Scale(2.0f, 3.0f);
            Assert.IsTrue(byFactor.LeftEndpoint.Bounded);
            Assert.IsTrue(byFactor.LeftEndpoint.Inclusive);
            Assert.AreEqual(7 - (42 - 7), byFactor.Left);
            Assert.IsTrue(byFactor.RightEndpoint.Bounded);
            Assert.IsFalse(byFactor.RightEndpoint.Inclusive);
            Assert.AreEqual(42 + (42 - 7) * 2, byFactor.Right);
            Assert.AreEqual((42 - 7) * 4, byFactor.Span);

            var leftByFactor = interval.ScaleRight(2.0f);
            Assert.IsTrue(leftByFactor.LeftEndpoint.Bounded);
            Assert.IsTrue(leftByFactor.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, leftByFactor.Left);
            Assert.IsTrue(leftByFactor.RightEndpoint.Bounded);
            Assert.IsFalse(leftByFactor.RightEndpoint.Inclusive);
            Assert.AreEqual(42 + (42 - 7), leftByFactor.Right);
            Assert.AreEqual((42 - 7) * 2, leftByFactor.Span);

            var centerByFactor = interval.ScaleCenter(2.0f);
            Assert.IsTrue(centerByFactor.LeftEndpoint.Bounded);
            Assert.IsTrue(centerByFactor.LeftEndpoint.Inclusive);
            Assert.AreEqual(7 - (42 - 7) / 2 - 1, centerByFactor.Left); // -1 for round down
            Assert.IsTrue(centerByFactor.RightEndpoint.Bounded);
            Assert.IsFalse(centerByFactor.RightEndpoint.Inclusive);
            Assert.AreEqual(42 + (42 - 7) / 2 + 1, centerByFactor.Right); // +1 for round up
            Assert.AreEqual((42 - 7) * 2 + 1, centerByFactor.Span); // +1 for round up

            var rightByFactor = interval.ScaleLeft(2.0f);
            Assert.IsTrue(rightByFactor.LeftEndpoint.Bounded);
            Assert.IsTrue(rightByFactor.LeftEndpoint.Inclusive);
            Assert.AreEqual(7 - (42 - 7), rightByFactor.Left);
            Assert.IsTrue(rightByFactor.RightEndpoint.Bounded);
            Assert.IsFalse(rightByFactor.RightEndpoint.Inclusive);
            Assert.AreEqual(42, rightByFactor.Right);
            Assert.AreEqual((42 - 7) * 2, rightByFactor.Span);

            var leftBySpan = interval.ScaleRight(100);
            Assert.IsTrue(leftBySpan.LeftEndpoint.Bounded);
            Assert.IsTrue(leftBySpan.LeftEndpoint.Inclusive);
            Assert.AreEqual(7, leftBySpan.Left);
            Assert.IsTrue(leftBySpan.RightEndpoint.Bounded);
            Assert.IsFalse(leftBySpan.RightEndpoint.Inclusive);
            Assert.AreEqual(42 + 100, leftBySpan.Right);
            Assert.AreEqual(42 - 7 + 100, leftBySpan.Span);

            var centerBySpan = interval.ScaleCenter(100);
            Assert.IsTrue(centerBySpan.LeftEndpoint.Bounded);
            Assert.IsTrue(centerBySpan.LeftEndpoint.Inclusive);
            Assert.AreEqual(7 - 100 / 2, centerBySpan.Left);
            Assert.IsTrue(centerBySpan.RightEndpoint.Bounded);
            Assert.IsFalse(centerBySpan.RightEndpoint.Inclusive);
            Assert.AreEqual(42 + 100 / 2, centerBySpan.Right);
            Assert.AreEqual(42 - 7 + 100, centerBySpan.Span);

            var rightBySpan = interval.ScaleLeft(100);
            Assert.IsTrue(rightBySpan.LeftEndpoint.Bounded);
            Assert.IsTrue(rightBySpan.LeftEndpoint.Inclusive);
            Assert.AreEqual(7 - 100, rightBySpan.Left);
            Assert.IsTrue(rightBySpan.RightEndpoint.Bounded);
            Assert.IsFalse(rightBySpan.RightEndpoint.Inclusive);
            Assert.AreEqual(42, rightBySpan.Right);
            Assert.AreEqual(42 - 7 + 100, rightBySpan.Span);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Negative()
        {
            var pos = new IntInterval(7, 42);
            Assert.IsFalse(pos.IsNegative);
            var neg = new IntInterval(42, 7);
            Assert.IsFalse(pos.IsNegative);
        }

        [TestMethod]
        [Timeout(60000)]
        public void IntersectsDisjoint()
        {
            // simple overlap
            var a = new IntInterval(7, 42);
            var b = new IntInterval(10, 100);
            Assert.IsTrue(a.IntersectsWith(b));
            Assert.IsTrue(b.IntersectsWith(a));
            Assert.IsTrue(a.IntersectsWith(a));
            Assert.IsFalse(a.IsDisjointFrom(b));
            Assert.IsFalse(b.IsDisjointFrom(a));
            Assert.IsFalse(a.IsDisjointFrom(a));

            // "touching", inclusive
            var c = new IntInterval(42, 100);
            Assert.IsTrue(a.IntersectsWith(c));
            Assert.IsTrue(c.IntersectsWith(a));
            Assert.IsFalse(a.IsDisjointFrom(c));
            Assert.IsFalse(c.IsDisjointFrom(a));

            // "touching", but non-inclusive
            var d = new IntInterval(42, false, 100, true);
            Assert.IsFalse(a.IntersectsWith(d));
            Assert.IsFalse(d.IntersectsWith(a));
            Assert.IsTrue(a.IsDisjointFrom(d));
            Assert.IsTrue(d.IsDisjointFrom(a));
            var e = new IntInterval(0, true, 7, false);
            Assert.IsFalse(a.IntersectsWith(e));
            Assert.IsFalse(e.IntersectsWith(a));
            Assert.IsTrue(a.IsDisjointFrom(e));
            Assert.IsTrue(e.IsDisjointFrom(a));

            // simple disjoint
            var f = new IntInterval(0, 3);
            Assert.IsFalse(a.IntersectsWith(f));
            Assert.IsFalse(f.IntersectsWith(a));
            Assert.IsTrue(a.IsDisjointFrom(f));
            Assert.IsTrue(f.IsDisjointFrom(a));

            // negative interval
            var g = new IntInterval(8, 0);
            Assert.IsTrue(a.IntersectsWith(g));
            Assert.IsTrue(g.IntersectsWith(a));
            Assert.IsFalse(a.IsDisjointFrom(g));
            Assert.IsFalse(g.IsDisjointFrom(a));

            // containment
            var h = new IntInterval(7, 42);
            var i = new IntInterval(10, 40);
            Assert.IsTrue(h.IntersectsWith(i));
            Assert.IsTrue(i.IntersectsWith(h));
            Assert.IsFalse(h.IsDisjointFrom(i));
            Assert.IsFalse(i.IsDisjointFrom(h));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Subset()
        {
            var a = new IntInterval(7, 42);
            var b = new IntInterval(10, 20);
            var c = new IntInterval(0, 20);
            var d = new IntInterval(10, 100);
            Assert.IsTrue(b.IsSubsetOf(a));
            Assert.IsTrue(b.IsProperSubsetOf(a));
            Assert.IsFalse(c.IsSubsetOf(a));
            Assert.IsFalse(c.IsProperSubsetOf(a));
            Assert.IsFalse(d.IsSubsetOf(a));
            Assert.IsFalse(d.IsProperSubsetOf(a));

            // proper
            Assert.IsTrue(a.IsSubsetOf(a));
            Assert.IsFalse(a.IsProperSubsetOf(a));
        }

        [TestMethod]
        [Timeout(60000)]
        public void IntIntervalCoverage()
        {
            var a = new IntInterval(7, 42);
            var b = new IntInterval(40, 50);
            var c = new IntInterval(200, 100); // negative
            var e = IntInterval.Empty;

            var empty = IntInterval.Coverage(new IntInterval[] { });
            var single = IntInterval.Coverage(new IntInterval[] { a });
            var ab = IntInterval.Coverage(new IntInterval[] { a, b });
            var ba = IntInterval.Coverage(new IntInterval[] { b, a });
            var ac = IntInterval.Coverage(new IntInterval[] { a, c });
            var ca = IntInterval.Coverage(new IntInterval[] { c, a });
            var ae = IntInterval.Coverage(new IntInterval[] { a, e });
            var ea = IntInterval.Coverage(new IntInterval[] { e, a });
            var bc = IntInterval.Coverage(new IntInterval[] { b, c });
            var cb = IntInterval.Coverage(new IntInterval[] { c, b });
            var be = IntInterval.Coverage(new IntInterval[] { b, e });
            var eb = IntInterval.Coverage(new IntInterval[] { e, b });
            var ce = IntInterval.Coverage(new IntInterval[] { c, e });
            var ec = IntInterval.Coverage(new IntInterval[] { e, c });

            Assert.IsTrue(c.IsNegative);
            Assert.AreEqual(0, empty.Right);
            Assert.AreEqual(0, empty.Left);
            Assert.AreEqual(a.Left, single.Left);
            Assert.AreEqual(a.Right, single.Right);
            Assert.AreEqual(ab.Left, a.Left);
            Assert.AreEqual(ab.Right, b.Right);
            Assert.AreEqual(ba.Left, a.Left);
            Assert.AreEqual(ba.Right, b.Right);
            Assert.AreEqual(ac.Left, a.Left);
            Assert.AreEqual(ac.Right, c.Left); // c is negative
            Assert.AreEqual(ae.Left, a.Left);
            Assert.AreEqual(ae.Right, a.Right);
            Assert.AreEqual(ea.Left, a.Left);
            Assert.AreEqual(ea.Right, a.Right);
            Assert.AreEqual(ca.Left, a.Left);
            Assert.AreEqual(ca.Right, c.Left); // c is negative
            Assert.AreEqual(bc.Left, b.Left);
            Assert.AreEqual(bc.Right, c.Left); // c is negative
            Assert.AreEqual(cb.Left, b.Left);
            Assert.AreEqual(cb.Right, c.Left); // c is negative
            Assert.AreEqual(be.Left, b.Left);
            Assert.AreEqual(be.Right, b.Right);
            Assert.AreEqual(eb.Left, b.Left);
            Assert.AreEqual(eb.Right, b.Right);
            Assert.AreEqual(ce.Left, c.Right); // c is negative
            Assert.AreEqual(ec.Right, c.Left); // c is negative
            Assert.IsTrue(IntInterval.Coverage(new IntInterval[] { }).IsEmpty); // empty sequence -> empty interval
            Assert.IsTrue(IntInterval.Coverage(new IntInterval[] { e }).IsEmpty); // sequence of only empty intervals -> empty interval
        }

        [TestMethod]
        [Timeout(60000)]
        public void RealIntervalCoverage()
        {
            var a = new RealInterval(7, 42);
            var b = new RealInterval(40, 50);
            var c = new RealInterval(200, 100); // negative
            var e = RealInterval.Empty;

            var empty = RealInterval.Coverage(new RealInterval[] { });
            var single = RealInterval.Coverage(new RealInterval[] { a });
            var ab = RealInterval.Coverage(new RealInterval[] { a, b });
            var ba = RealInterval.Coverage(new RealInterval[] { b, a });
            var ac = RealInterval.Coverage(new RealInterval[] { a, c });
            var ca = RealInterval.Coverage(new RealInterval[] { c, a });
            var ae = RealInterval.Coverage(new RealInterval[] { a, e });
            var ea = RealInterval.Coverage(new RealInterval[] { e, a });
            var bc = RealInterval.Coverage(new RealInterval[] { b, c });
            var cb = RealInterval.Coverage(new RealInterval[] { c, b });
            var be = RealInterval.Coverage(new RealInterval[] { b, e });
            var eb = RealInterval.Coverage(new RealInterval[] { e, b });
            var ce = RealInterval.Coverage(new RealInterval[] { c, e });
            var ec = RealInterval.Coverage(new RealInterval[] { e, c });

            Assert.IsTrue(c.IsNegative);
            Assert.AreEqual(0, empty.Right);
            Assert.AreEqual(0, empty.Left);
            Assert.AreEqual(a.Left, single.Left);
            Assert.AreEqual(a.Right, single.Right);
            Assert.AreEqual(ab.Left, a.Left);
            Assert.AreEqual(ab.Right, b.Right);
            Assert.AreEqual(ba.Left, a.Left);
            Assert.AreEqual(ba.Right, b.Right);
            Assert.AreEqual(ac.Left, a.Left);
            Assert.AreEqual(ac.Right, c.Left); // c is negative
            Assert.AreEqual(ae.Left, a.Left);
            Assert.AreEqual(ae.Right, a.Right);
            Assert.AreEqual(ea.Left, a.Left);
            Assert.AreEqual(ea.Right, a.Right);
            Assert.AreEqual(ca.Left, a.Left);
            Assert.AreEqual(ca.Right, c.Left); // c is negative
            Assert.AreEqual(bc.Left, b.Left);
            Assert.AreEqual(bc.Right, c.Left); // c is negative
            Assert.AreEqual(cb.Left, b.Left);
            Assert.AreEqual(cb.Right, c.Left); // c is negative
            Assert.AreEqual(be.Left, b.Left);
            Assert.AreEqual(be.Right, b.Right);
            Assert.AreEqual(eb.Left, b.Left);
            Assert.AreEqual(eb.Right, b.Right);
            Assert.AreEqual(ce.Left, c.Right); // c is negative
            Assert.AreEqual(ec.Right, c.Left); // c is negative
            Assert.IsTrue(RealInterval.Coverage(new RealInterval[] { }).IsEmpty); // empty sequence -> empty interval
            Assert.IsTrue(RealInterval.Coverage(new RealInterval[] { e }).IsEmpty); // sequence of only empty intervals -> empty interval
        }

        [TestMethod]
        [Timeout(60000)]
        public void RelativeTimeIntervalCoverage()
        {
            var a = new RelativeTimeInterval(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(42));
            var b = new RelativeTimeInterval(TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(50));
            var c = new RelativeTimeInterval(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(100)); // negative
            var e = RelativeTimeInterval.Empty;

            var empty = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { });
            var single = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { a });
            var ab = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { a, b });
            var ba = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { b, a });
            var ac = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { a, c });
            var ca = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { c, a });
            var ae = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { a, e });
            var ea = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { e, a });
            var bc = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { b, c });
            var cb = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { c, b });
            var be = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { b, e });
            var eb = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { e, b });
            var ce = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { c, e });
            var ec = RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { e, c });

            Assert.IsTrue(c.IsNegative);
            Assert.AreEqual(TimeSpan.Zero, empty.Right);
            Assert.AreEqual(TimeSpan.Zero, empty.Left);
            Assert.AreEqual(a.Left, single.Left);
            Assert.AreEqual(a.Right, single.Right);
            Assert.AreEqual(ab.Left, a.Left);
            Assert.AreEqual(ab.Right, b.Right);
            Assert.AreEqual(ba.Left, a.Left);
            Assert.AreEqual(ba.Right, b.Right);
            Assert.AreEqual(ac.Left, a.Left);
            Assert.AreEqual(ac.Right, c.Left); // c is negative
            Assert.AreEqual(ae.Left, a.Left);
            Assert.AreEqual(ae.Right, a.Right);
            Assert.AreEqual(ea.Left, a.Left);
            Assert.AreEqual(ea.Right, a.Right);
            Assert.AreEqual(ca.Left, a.Left);
            Assert.AreEqual(ca.Right, c.Left); // c is negative
            Assert.AreEqual(bc.Left, b.Left);
            Assert.AreEqual(bc.Right, c.Left); // c is negative
            Assert.AreEqual(cb.Left, b.Left);
            Assert.AreEqual(cb.Right, c.Left); // c is negative
            Assert.AreEqual(be.Left, b.Left);
            Assert.AreEqual(be.Right, b.Right);
            Assert.AreEqual(eb.Left, b.Left);
            Assert.AreEqual(eb.Right, b.Right);
            Assert.AreEqual(ce.Left, c.Right); // c is negative
            Assert.AreEqual(ec.Right, c.Left); // c is negative
            Assert.IsTrue(RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { }).IsEmpty); // empty sequence -> empty interval
            Assert.IsTrue(RelativeTimeInterval.Coverage(new RelativeTimeInterval[] { e }).IsEmpty); // sequence of only empty intervals -> empty interval
        }

        [TestMethod]
        [Timeout(60000)]
        public void TimeIntervalCoverage()
        {
            var a = new TimeInterval(new DateTime(7), new DateTime(42));
            var b = new TimeInterval(new DateTime(40), new DateTime(50));
            var c = new TimeInterval(new DateTime(200), new DateTime(100)); // negative
            var e = TimeInterval.Empty;

            var empty = TimeInterval.Coverage(new TimeInterval[] { });
            var single = TimeInterval.Coverage(new TimeInterval[] { a });
            var ab = TimeInterval.Coverage(new TimeInterval[] { a, b });
            var ba = TimeInterval.Coverage(new TimeInterval[] { b, a });
            var ac = TimeInterval.Coverage(new TimeInterval[] { a, c });
            var ca = TimeInterval.Coverage(new TimeInterval[] { c, a });
            var ae = TimeInterval.Coverage(new TimeInterval[] { a, e });
            var ea = TimeInterval.Coverage(new TimeInterval[] { e, a });
            var bc = TimeInterval.Coverage(new TimeInterval[] { b, c });
            var cb = TimeInterval.Coverage(new TimeInterval[] { c, b });
            var be = TimeInterval.Coverage(new TimeInterval[] { b, e });
            var eb = TimeInterval.Coverage(new TimeInterval[] { e, b });
            var ce = TimeInterval.Coverage(new TimeInterval[] { c, e });
            var ec = TimeInterval.Coverage(new TimeInterval[] { e, c });

            Assert.IsTrue(c.IsNegative);
            Assert.AreEqual(default(DateTime), empty.Right);
            Assert.AreEqual(default(DateTime), empty.Left);
            Assert.AreEqual(a.Left, single.Left);
            Assert.AreEqual(a.Right, single.Right);
            Assert.AreEqual(ab.Left, a.Left);
            Assert.AreEqual(ab.Right, b.Right);
            Assert.AreEqual(ba.Left, a.Left);
            Assert.AreEqual(ba.Right, b.Right);
            Assert.AreEqual(ac.Left, a.Left);
            Assert.AreEqual(ac.Right, c.Left); // c is negative
            Assert.AreEqual(ae.Left, a.Left);
            Assert.AreEqual(ae.Right, a.Right);
            Assert.AreEqual(ea.Left, a.Left);
            Assert.AreEqual(ea.Right, a.Right);
            Assert.AreEqual(ca.Left, a.Left);
            Assert.AreEqual(ca.Right, c.Left); // c is negative
            Assert.AreEqual(bc.Left, b.Left);
            Assert.AreEqual(bc.Right, c.Left); // c is negative
            Assert.AreEqual(cb.Left, b.Left);
            Assert.AreEqual(cb.Right, c.Left); // c is negative
            Assert.AreEqual(be.Left, b.Left);
            Assert.AreEqual(be.Right, b.Right);
            Assert.AreEqual(eb.Left, b.Left);
            Assert.AreEqual(eb.Right, b.Right);
            Assert.AreEqual(ce.Left, c.Right); // c is negative
            Assert.AreEqual(ec.Right, c.Left); // c is negative
            Assert.IsTrue(TimeInterval.Coverage(new TimeInterval[] { }).IsEmpty); // empty sequence -> empty interval
            Assert.IsTrue(TimeInterval.Coverage(new TimeInterval[] { e }).IsEmpty); // sequence of only empty intervals -> empty interval
        }
    }
}