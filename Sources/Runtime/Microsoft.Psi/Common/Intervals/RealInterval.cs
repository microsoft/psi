﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a real (double) interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    public class RealInterval : Interval<double, double, IntervalEndpoint<double>, RealInterval>
    {
        /// <summary>
        /// Canonical infinite interval (unbounded on both ends).
        /// </summary>
        public static readonly RealInterval Infinite = new (double.MinValue, false, false, double.MaxValue, false, false);

        /// <summary>
        /// Canonical empty instance (bounded, non-inclusive, single point).
        /// </summary>
        public static readonly RealInterval Empty = new (0, false, true, 0, false, true);

        /// <summary>
        /// Zero interval (unbounded but inclusive, zero value).
        /// </summary>
        public static readonly RealInterval Zero = new (0, true, true, 0, true, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="RealInterval"/> class.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="rightPoint">Right bound point.</param>
        public RealInterval(double leftPoint, double rightPoint)
            : base(new IntervalEndpoint<double>(leftPoint, true), new IntervalEndpoint<double>(rightPoint, true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="leftInclusive">Whether left point is inclusive.</param>
        /// <param name="rightPoint">Right bound point.</param>
        /// <param name="rightInclusive">Whether right point is inclusive.</param>
        public RealInterval(double leftPoint, bool leftInclusive, double rightPoint, bool rightInclusive)
            : base(new IntervalEndpoint<double>(leftPoint, leftInclusive), new IntervalEndpoint<double>(rightPoint, rightInclusive))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point (or min value if unbound).</param>
        /// <param name="leftInclusive">Whether left point is inclusive (always false if unbound).</param>
        /// <param name="leftBounded">Whether left point is bounded.</param>
        /// <param name="rightPoint">Right bound point (or min value if unbound).</param>
        /// <param name="rightInclusive">Whether right point is inclusive (always false if unbound).</param>
        /// <param name="rightBounded">Whether right point is bounded.</param>
        public RealInterval(double leftPoint, bool leftInclusive, bool leftBounded, double rightPoint, bool rightInclusive, bool rightBounded)
            : base(
                leftBounded ? new IntervalEndpoint<double>(leftPoint, leftInclusive) : new IntervalEndpoint<double>(leftPoint),
                rightBounded ? new IntervalEndpoint<double>(rightPoint, rightInclusive) : new IntervalEndpoint<double>(rightPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealInterval"/> class.
        /// </summary>
        /// <param name="leftEndpoint">Left endpoint.</param>
        /// <param name="rightEndpoint">Right endpoint.</param>
        public RealInterval(IntervalEndpoint<double> leftEndpoint, IntervalEndpoint<double> rightEndpoint)
            : base(leftEndpoint, rightEndpoint)
        {
        }

        /// <summary>
        /// Gets the point minimum value.
        /// </summary>
        protected override double PointMinValue => double.MinValue;

        /// <summary>
        /// Gets the point maximum value.
        /// </summary>
        protected override double PointMaxValue => double.MaxValue;

        /// <summary>
        /// Gets the span zero value.
        /// </summary>
        protected override double SpanZeroValue => 0.0;

        /// <summary>
        /// Gets the span minimum value.
        /// </summary>
        protected override double SpanMinValue => double.MinValue;

        /// <summary>
        /// Gets the span maximum value.
        /// </summary>
        protected override double SpanMaxValue => double.MaxValue;

        /// <summary>
        /// Merges a specified set of real intervals into a set of non-overlapping real intervals that cover the specified intervals.
        /// </summary>
        /// <param name="intervals">A set of intervals to cover.</param>
        /// <returns>Set of non-overlapping intervals that cover the given intervals.</returns>
        public static IEnumerable<RealInterval> Merge(IEnumerable<RealInterval> intervals)
            => Merge(intervals, (left, right) => new (left, right));

        /// <summary>
        /// Determine coverage from minimum left to maximum right for a set of given intervals.
        /// </summary>
        /// <param name="intervals">The set of intervals.</param>
        /// <remarks>Returns empty interval when sequence is empty or contains only empty intervals.</remarks>
        /// <returns>Interval from minimum left to maximum right value.</returns>
        public static RealInterval Coverage(IEnumerable<RealInterval> intervals)
            => Coverage(intervals, (left, right) => new RealInterval(left, right), RealInterval.Empty);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <param name="left">Left bound point.</param>
        /// <param name="inclusive">Whether left point is inclusive.</param>
        /// <returns>A left-bound instance of the <see cref="RealInterval"/> class.</returns>
        public static RealInterval LeftBounded(double left, bool inclusive)
            => new (left, inclusive, true, double.MaxValue, false, false);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="left">Left bound point.</param>
        /// <returns>A left-bound instance of the <see cref="RealInterval"/> class.</returns>
        public static RealInterval LeftBounded(double left) => LeftBounded(left, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <param name="right">Right bound point.</param>
        /// <param name="inclusive">Whether right point is inclusive.</param>
        /// <returns>A right-bound instance of the <see cref="RealInterval"/> class.</returns>
        public static RealInterval RightBounded(double right, bool inclusive)
            => new (double.MinValue, false, false, right, inclusive, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="right">Right bound point.</param>
        /// <returns>A right-bound instance of the <see cref="RealInterval"/> class.</returns>
        public static RealInterval RightBounded(double right) => RightBounded(right, true);

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public override RealInterval Translate(double span)
            => this.Translate(span, (lp, li, lb, rp, ri, rb) => new (lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by span distances.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override RealInterval Scale(double left, double right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new (lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by factors.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override RealInterval Scale(float left, float right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new (lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value.</param>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled span.</returns>
        protected override double ScaleSpan(double span, double factor) => Math.Round(span * factor);

        /// <summary>
        /// Negate span.
        /// </summary>
        /// <param name="span">Span to be negated.</param>
        /// <returns>Negated span.</returns>
        protected override double NegateSpan(double span) => -span;

        /// <summary>
        /// Translate point by given span.
        /// </summary>
        /// <param name="point">Point value.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated point.</returns>
        protected override double TranslatePoint(double point, double span) => point + span;

        /// <summary>
        /// Determine span between two given points.
        /// </summary>
        /// <param name="x">First point.</param>
        /// <param name="y">Second point.</param>
        /// <returns>Span between points.</returns>
        protected override double Difference(double x, double y) => x - y;

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected override int ComparePoints(double a, double b) => a.CompareTo(b);
    }
}