// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a TimeSpan interval with bounded/unbounded and inclusive/exclusive end points
    /// </summary>
    public class RelativeTimeInterval : Interval<TimeSpan, TimeSpan, IntervalEndpoint<TimeSpan>, RelativeTimeInterval>
    {
        /// <summary>
        /// Canonical infinite interval (unbounded on both ends)
        /// </summary>
        public static readonly RelativeTimeInterval Infinite =
            new RelativeTimeInterval(TimeSpan.MinValue, false, false, TimeSpan.MaxValue, false, false);

        /// <summary>
        /// Canonical empty instance (bounded, non-inclusive, single point).
        /// </summary>
        public static readonly RelativeTimeInterval Empty =
            new RelativeTimeInterval(TimeSpan.Zero, false, true, TimeSpan.Zero, false, true);

        /// <summary>
        /// Zero interval (unbounded but inclusive, zero value)
        /// </summary>
        public static readonly RelativeTimeInterval Zero =
            new RelativeTimeInterval(TimeSpan.Zero, true, true, TimeSpan.Zero, true, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <remarks>Defaults to inclusive</remarks>
        /// <param name="leftPoint">Left bound point</param>
        /// <param name="rightPoint">Right bound point</param>
        public RelativeTimeInterval(TimeSpan leftPoint, TimeSpan rightPoint)
            : base(new IntervalEndpoint<TimeSpan>(leftPoint, true), new IntervalEndpoint<TimeSpan>(rightPoint, true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point</param>
        /// <param name="leftInclusive">Whether left point is inclusive</param>
        /// <param name="rightPoint">Right bound point</param>
        /// <param name="rightInclusive">Whether right point is inclusive</param>
        public RelativeTimeInterval(TimeSpan leftPoint, bool leftInclusive, TimeSpan rightPoint, bool rightInclusive)
            : base(new IntervalEndpoint<TimeSpan>(leftPoint, leftInclusive), new IntervalEndpoint<TimeSpan>(rightPoint, rightInclusive))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point (or min value if unbound)</param>
        /// <param name="leftInclusive">Whether left point is inclusive (always false if unbound)</param>
        /// <param name="leftBounded">Whether left point is bounded</param>
        /// <param name="rightPoint">Right bound point (or min value if unbound)</param>
        /// <param name="rightInclusive">Whether right point is inclusive (always false if unbound)</param>
        /// <param name="rightBounded">Whether right point is bounded</param>
        public RelativeTimeInterval(TimeSpan leftPoint, bool leftInclusive, bool leftBounded, TimeSpan rightPoint, bool rightInclusive, bool rightBounded)
            : base(
                leftBounded ? new IntervalEndpoint<TimeSpan>(leftPoint, leftInclusive) : new IntervalEndpoint<TimeSpan>(leftPoint),
                rightBounded ? new IntervalEndpoint<TimeSpan>(rightPoint, rightInclusive) : new IntervalEndpoint<TimeSpan>(rightPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftEndpoint">Left endpoint</param>
        /// <param name="rightEndpoint">Right endpoint</param>
        public RelativeTimeInterval(IntervalEndpoint<TimeSpan> leftEndpoint, IntervalEndpoint<TimeSpan> rightEndpoint)
            : base(leftEndpoint, rightEndpoint)
        {
        }

        /// <summary>
        /// Gets the point minimum value
        /// </summary>
        protected override TimeSpan PointMinValue
        {
            get { return TimeSpan.MinValue; }
        }

        /// <summary>
        /// Gets the point maximum value
        /// </summary>
        protected override TimeSpan PointMaxValue
        {
            get { return TimeSpan.MaxValue; }
        }

        /// <summary>
        /// Gets the span zero value
        /// </summary>
        protected override TimeSpan SpanZeroValue
        {
            get { return TimeSpan.Zero; }
        }

        /// <summary>
        /// Gets the span minimum value
        /// </summary>
        protected override TimeSpan SpanMinValue
        {
            get { return TimeSpan.MinValue; }
        }

        /// <summary>
        /// Gets the span maximum value
        /// </summary>
        protected override TimeSpan SpanMaxValue
        {
            get { return TimeSpan.MaxValue; }
        }

        /// <summary>
        /// Construct TimeInterval relative to an origin (DateTime)
        /// </summary>
        /// <param name="origin">Origin time point</param>
        /// <param name="relative">Relative endpoints</param>
        /// <returns>Translated interval</returns>
        public static TimeInterval operator +(DateTime origin, RelativeTimeInterval relative)
        {
            return new TimeInterval(origin, relative);
        }

        /// <summary>
        /// Determine coverage from minimum left to maximum right.
        /// </summary>
        /// <param name="intervals">Sequence of intervals.</param>
        /// <remarks>Returns negative interval from max to min point when sequence is empty.</remarks>
        /// <returns>Interval from minimum left to maximum right value.</returns>
        public static RelativeTimeInterval Coverage(IEnumerable<RelativeTimeInterval> intervals)
        {
            return Coverage(intervals, (left, right) => new RelativeTimeInterval(left, right), RelativeTimeInterval.Empty);
        }

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <param name="left">Left bound point</param>
        /// <param name="inclusive">Whether left point is inclusive</param>
        /// <returns>A left-bound instance of the <see cref="RelativeTimeInterval"/> class</returns>
        public static RelativeTimeInterval LeftBounded(TimeSpan left, bool inclusive)
        {
            return new RelativeTimeInterval(left, inclusive, true, TimeSpan.MaxValue, false, false);
        }

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive</remarks>
        /// <param name="left">Left bound point</param>
        /// <returns>A left-bound instance of the <see cref="RelativeTimeInterval"/> class</returns>
        public static RelativeTimeInterval LeftBounded(TimeSpan left)
        {
            return LeftBounded(left, true);
        }

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <param name="right">Right bound point</param>
        /// <param name="inclusive">Whether right point is inclusive</param>
        /// <returns>A right-bound instance of the <see cref="RelativeTimeInterval"/> class</returns>
        public static RelativeTimeInterval RightBounded(TimeSpan right, bool inclusive)
        {
            return new RelativeTimeInterval(TimeSpan.MinValue, false, false, right, inclusive, true);
        }

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive</remarks>
        /// <param name="right">Right bound point</param>
        /// <returns>A right-bound instance of the <see cref="RelativeTimeInterval"/> class</returns>
        public static RelativeTimeInterval RightBounded(TimeSpan right)
        {
            return RightBounded(right, true);
        }

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change</remarks>
        /// <param name="span">Span by which to translate</param>
        /// <returns>Translated interval</returns>
        public override RelativeTimeInterval Translate(TimeSpan span)
        {
            return this.Translate(span, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale endpoints by span distances
        /// </summary>
        /// <param name="left">Span by which to scale left</param>
        /// <param name="right">Span by which to scale right</param>
        /// <returns>Scaled interval</returns>
        public override RelativeTimeInterval Scale(TimeSpan left, TimeSpan right)
        {
            return this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale endpoints by factors
        /// </summary>
        /// <param name="left">Factor by which to scale left</param>
        /// <param name="right">Factor by which to scale right</param>
        /// <returns>Scaled interval</returns>
        public override RelativeTimeInterval Scale(float left, float right)
        {
            return this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));
        }

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value</param>
        /// <param name="factor">Factor by which to scale</param>
        /// <returns>Scaled span</returns>
        protected override TimeSpan ScaleSpan(TimeSpan span, double factor)
        {
            return new TimeSpan((long)Math.Round(span.Ticks * factor));
        }

        /// <summary>
        /// Negate span
        /// </summary>
        /// <param name="span">Span to be negated</param>
        /// <returns>Negated span</returns>
        protected override TimeSpan NegateSpan(TimeSpan span)
        {
            return -span;
        }

        /// <summary>
        /// Translate point by given span
        /// </summary>
        /// <param name="point">Point value</param>
        /// <param name="span">Span by which to translate</param>
        /// <returns>Translated point</returns>
        protected override TimeSpan TranslatePoint(TimeSpan point, TimeSpan span)
        {
            return point + span;
        }

        /// <summary>
        /// Determine span between two given points
        /// </summary>
        /// <param name="x">First point</param>
        /// <param name="y">Second point</param>
        /// <returns>Span between points</returns>
        protected override TimeSpan Difference(TimeSpan x, TimeSpan y)
        {
            return x - y;
        }

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected override int ComparePoints(TimeSpan a, TimeSpan b)
        {
            return a.CompareTo(b);
        }
    }
}