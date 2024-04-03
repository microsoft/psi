﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an integer interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    public class IntInterval : Interval<int, int, IntervalEndpoint<int>, IntInterval>
    {
        /// <summary>
        /// Canonical infinite interval (unbounded on both ends).
        /// </summary>
        public static readonly IntInterval Infinite = new (int.MinValue, false, false, int.MaxValue, false, false);

        /// <summary>
        /// Canonical empty instance (bounded, non-inclusive, single point).
        /// </summary>
        public static readonly IntInterval Empty = new (0, false, true, 0, false, true);

        /// <summary>
        /// Zero interval (unbounded but inclusive, zero value).
        /// </summary>
        public static readonly IntInterval Zero = new (0, true, true, 0, true, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="IntInterval"/> class.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="rightPoint">Right bound point.</param>
        public IntInterval(int leftPoint, int rightPoint)
            : base(new IntervalEndpoint<int>(leftPoint, true), new IntervalEndpoint<int>(rightPoint, true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="leftInclusive">Whether left point is inclusive.</param>
        /// <param name="rightPoint">Right bound point.</param>
        /// <param name="rightInclusive">Whether right point is inclusive.</param>
        public IntInterval(int leftPoint, bool leftInclusive, int rightPoint, bool rightInclusive)
            : base(new IntervalEndpoint<int>(leftPoint, leftInclusive), new IntervalEndpoint<int>(rightPoint, rightInclusive))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point (or min value if unbound).</param>
        /// <param name="leftInclusive">Whether left point is inclusive (always false if unbound).</param>
        /// <param name="leftBounded">Whether left point is bounded.</param>
        /// <param name="rightPoint">Right bound point (or min value if unbound).</param>
        /// <param name="rightInclusive">Whether right point is inclusive (always false if unbound).</param>
        /// <param name="rightBounded">Whether right point is bounded.</param>
        public IntInterval(int leftPoint, bool leftInclusive, bool leftBounded, int rightPoint, bool rightInclusive, bool rightBounded)
            : base(
                leftBounded ? new IntervalEndpoint<int>(leftPoint, leftInclusive) : new IntervalEndpoint<int>(leftPoint),
                rightBounded ? new IntervalEndpoint<int>(rightPoint, rightInclusive) : new IntervalEndpoint<int>(rightPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntInterval"/> class.
        /// </summary>
        /// <param name="leftEndpoint">Left endpoint.</param>
        /// <param name="rightEndpoint">Right endpoint.</param>
        public IntInterval(IntervalEndpoint<int> leftEndpoint, IntervalEndpoint<int> rightEndpoint)
            : base(leftEndpoint, rightEndpoint)
        {
        }

        /// <summary>
        /// Gets the point minimum value.
        /// </summary>
        protected override int PointMinValue => int.MinValue;

        /// <summary>
        /// Gets the point maximum value.
        /// </summary>
        protected override int PointMaxValue => int.MaxValue;

        /// <summary>
        /// Gets the span zero value.
        /// </summary>
        protected override int SpanZeroValue => 0;

        /// <summary>
        /// Gets the span minimum value.
        /// </summary>
        protected override int SpanMinValue => int.MinValue;

        /// <summary>
        /// Gets the span maximum value.
        /// </summary>
        protected override int SpanMaxValue => int.MaxValue;

        /// <summary>
        /// Merges a specified set of int intervals into a set of non-overlapping int intervals that cover the specified intervals.
        /// </summary>
        /// <param name="intervals">A set of intervals to cover.</param>
        /// <returns>Set of non-overlapping intervals that cover the given intervals.</returns>
        public static IEnumerable<IntInterval> Merge(IEnumerable<IntInterval> intervals)
            => Merge(intervals, (left, right) => new IntInterval(left, right));

        /// <summary>
        /// Determine coverage from minimum left to maximum right for a set of given intervals.
        /// </summary>
        /// <param name="intervals">The set of intervals.</param>
        /// <remarks>Returns empty interval when sequence is empty or contains only empty intervals.</remarks>
        /// <returns>Interval from minimum left to maximum right value.</returns>
        public static IntInterval Coverage(IEnumerable<IntInterval> intervals)
            => Coverage(intervals, (left, right) => new IntInterval(left, right), IntInterval.Empty);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <param name="left">Left bound point.</param>
        /// <param name="inclusive">Whether left point is inclusive.</param>
        /// <returns>A left-bound instance of the <see cref="IntInterval"/> class.</returns>
        public static IntInterval LeftBounded(int left, bool inclusive)
            => new (left, inclusive, true, int.MaxValue, false, false);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="left">Left bound point.</param>
        /// <returns>A left-bound instance of the <see cref="IntInterval"/> class.</returns>
        public static IntInterval LeftBounded(int left) => LeftBounded(left, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <param name="right">Right bound point.</param>
        /// <param name="inclusive">Whether right point is inclusive.</param>
        /// <returns>A right-bound instance of the <see cref="IntInterval"/> class.</returns>
        public static IntInterval RightBounded(int right, bool inclusive)
            => new (int.MinValue, false, false, right, inclusive, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="right">Right bound point.</param>
        /// <returns>A right-bound instance of the <see cref="IntInterval"/> class.</returns>
        public static IntInterval RightBounded(int right) => RightBounded(right, true);

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public override IntInterval Translate(int span)
            => this.Translate(span, (lp, li, lb, rp, ri, rb) => new IntInterval(lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by span distances.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override IntInterval Scale(int left, int right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new IntInterval(lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by factors.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override IntInterval Scale(float left, float right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new IntInterval(lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value.</param>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled span.</returns>
        protected override int ScaleSpan(int span, double factor) => (int)Math.Round(span * factor);

        /// <summary>
        /// Negate span.
        /// </summary>
        /// <param name="span">Span to be negated.</param>
        /// <returns>Negated span..</returns>
        protected override int NegateSpan(int span) => -span;

        /// <summary>
        /// Translate point by given span.
        /// </summary>
        /// <param name="point">Point value.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated point.</returns>
        protected override int TranslatePoint(int point, int span) => point + span;

        /// <summary>
        /// Determine span between two given points.
        /// </summary>
        /// <param name="x">First point.</param>
        /// <param name="y">Second point.</param>
        /// <returns>Span between points.</returns>
        protected override int Difference(int x, int y) => x - y;

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected override int ComparePoints(int a, int b) => a.CompareTo(b);
    }
}