// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a time interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    public class TimeInterval : Interval<DateTime, TimeSpan, IntervalEndpoint<DateTime>, TimeInterval>
    {
        /// <summary>
        /// Canonical infinite interval (unbounded on both ends).
        /// </summary>
        public static readonly TimeInterval Infinite =
            new TimeInterval(DateTime.MinValue, false, false, DateTime.MaxValue, false, false);

        /// <summary>
        /// Canonical empty instance (bounded, non-inclusive, single point).
        /// </summary>
        public static readonly TimeInterval Empty =
            new TimeInterval(DateTime.MinValue, false, true, DateTime.MinValue, false, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="origin">Origin around which interval is to be created.</param>
        /// <param name="relative">Time span interval specifying relative endpoints, bounding and inclusivity.</param>
        public TimeInterval(DateTime origin, RelativeTimeInterval relative)
            : base(
                relative.LeftEndpoint.Bounded ? new IntervalEndpoint<DateTime>(origin + relative.Left, relative.LeftEndpoint.Inclusive) : new IntervalEndpoint<DateTime>(DateTime.MinValue),
                relative.RightEndpoint.Bounded ? new IntervalEndpoint<DateTime>(origin + relative.Right, relative.RightEndpoint.Inclusive) : new IntervalEndpoint<DateTime>(DateTime.MaxValue))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="rightPoint">Right bound point.</param>
        public TimeInterval(DateTime leftPoint, DateTime rightPoint)
            : base(new IntervalEndpoint<DateTime>(leftPoint, true), new IntervalEndpoint<DateTime>(rightPoint, true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="leftInclusive">Whether left point is inclusive.</param>
        /// <param name="rightPoint">Right bound point.</param>
        /// <param name="rightInclusive">Whether right point is inclusive.</param>
        public TimeInterval(DateTime leftPoint, bool leftInclusive, DateTime rightPoint, bool rightInclusive)
            : base(new IntervalEndpoint<DateTime>(leftPoint, leftInclusive), new IntervalEndpoint<DateTime>(rightPoint, rightInclusive))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point (or min value if unbound).</param>
        /// <param name="leftInclusive">Whether left point is inclusive (always false if unbound).</param>
        /// <param name="leftBounded">Whether left point is bounded.</param>
        /// <param name="rightPoint">Right bound point (or min value if unbound).</param>
        /// <param name="rightInclusive">Whether right point is inclusive (always false if unbound).</param>
        /// <param name="rightBounded">Whether right point is bounded.</param>
        public TimeInterval(DateTime leftPoint, bool leftInclusive, bool leftBounded, DateTime rightPoint, bool rightInclusive, bool rightBounded)
            : base(
                leftBounded ? new IntervalEndpoint<DateTime>(leftPoint, leftInclusive) : new IntervalEndpoint<DateTime>(leftPoint),
                rightBounded ? new IntervalEndpoint<DateTime>(rightPoint, rightInclusive) : new IntervalEndpoint<DateTime>(rightPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeInterval"/> class.
        /// </summary>
        /// <param name="leftEndpoint">Left endpoint.</param>
        /// <param name="rightEndpoint">Right endpoint.</param>
        public TimeInterval(IntervalEndpoint<DateTime> leftEndpoint, IntervalEndpoint<DateTime> rightEndpoint)
            : base(leftEndpoint, rightEndpoint)
        {
        }

        /// <summary>
        /// Gets the point minimum value.
        /// </summary>
        protected override DateTime PointMinValue
        {
            get { return DateTime.MinValue; }
        }

        /// <summary>
        /// Gets the point maximum value.
        /// </summary>
        protected override DateTime PointMaxValue
        {
            get { return DateTime.MaxValue; }
        }

        /// <summary>
        /// Gets the span zero value.
        /// </summary>
        protected override TimeSpan SpanZeroValue
        {
            get { return TimeSpan.Zero; }
        }

        /// <summary>
        /// Gets the span minimum value.
        /// </summary>
        protected override TimeSpan SpanMinValue
        {
            get { return TimeSpan.MinValue; }
        }

        /// <summary>
        /// Gets the span maximum value.
        /// </summary>
        protected override TimeSpan SpanMaxValue
        {
            get { return TimeSpan.MaxValue; }
        }

        /// <summary>
        /// Determine coverage from minimum left to maximum right.
        /// </summary>
        /// <param name="intervals">Sequence of intervals.</param>
        /// <remarks>Returns negative interval from max to min point when sequence is empty.</remarks>
        /// <returns>Interval from minimum left to maximum right value.</returns>
        public static TimeInterval Coverage(IEnumerable<TimeInterval> intervals)
        {
            return Coverage(intervals, (left, right) => new TimeInterval(left, right), TimeInterval.Empty);
        }

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <param name="left">Left bound point.</param>
        /// <param name="inclusive">Whether left point is inclusive.</param>
        /// <returns>A left-bound instance of the <see cref="TimeInterval"/> class.</returns>
        public static TimeInterval LeftBounded(DateTime left, bool inclusive)
        {
            return new TimeInterval(left, inclusive, true, DateTime.MaxValue, false, false);
        }

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="left">Left bound point.</param>
        /// <returns>A left-bound instance of the <see cref="TimeInterval"/> class.</returns>
        public static TimeInterval LeftBounded(DateTime left)
        {
            return LeftBounded(left, true);
        }

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <param name="right">Right bound point.</param>
        /// <param name="inclusive">Whether right point is inclusive.</param>
        /// <returns>A right-bound instance of the <see cref="TimeInterval"/> class.</returns>
        public static TimeInterval RightBounded(DateTime right, bool inclusive)
        {
            return new TimeInterval(DateTime.MinValue, false, false, right, inclusive, true);
        }

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="right">Right bound point.</param>
        /// <returns>A right-bound instance of the <see cref="TimeInterval"/> class.</returns>
        public static TimeInterval RightBounded(DateTime right)
        {
            return RightBounded(right, true);
        }

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public override TimeInterval Translate(TimeSpan span)
        {
            return this.Translate(span, (lp, li, lb, rp, ri, rb) => new TimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale endpoints by span distances.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override TimeInterval Scale(TimeSpan left, TimeSpan right)
        {
            return this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new TimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale endpoints by factors.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override TimeInterval Scale(float left, float right)
        {
            return this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new TimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value.</param>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled span.</returns>
        protected override TimeSpan ScaleSpan(TimeSpan span, double factor)
        {
            return TimeSpan.FromTicks((long)Math.Round(span.Ticks * factor));
        }

        /// <summary>
        /// Negate span.
        /// </summary>
        /// <param name="span">Span to be negated.</param>
        /// <returns>Negated span.</returns>
        protected override TimeSpan NegateSpan(TimeSpan span)
        {
            return span.Negate();
        }

        /// <summary>
        /// Translate point by given span.
        /// </summary>
        /// <param name="point">Point value.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated point.</returns>
        protected override DateTime TranslatePoint(DateTime point, TimeSpan span)
        {
            return point + span;
        }

        /// <summary>
        /// Determine span between two given points.
        /// </summary>
        /// <param name="x">First point.</param>
        /// <param name="y">Second point.</param>
        /// <returns>Span between points.</returns>
        protected override TimeSpan Difference(DateTime x, DateTime y)
        {
            return x - y;
        }

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected override int ComparePoints(DateTime a, DateTime b)
        {
            return a.CompareTo(b);
        }
    }
}