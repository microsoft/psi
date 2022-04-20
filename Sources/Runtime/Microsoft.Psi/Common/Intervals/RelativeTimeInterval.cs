// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a TimeSpan interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    public class RelativeTimeInterval : Interval<TimeSpan, TimeSpan, IntervalEndpoint<TimeSpan>, RelativeTimeInterval>, IEquatable<RelativeTimeInterval>
    {
        /// <summary>
        /// Canonical infinite interval (unbounded on both ends).
        /// </summary>
        public static readonly RelativeTimeInterval Infinite =
            new (TimeSpan.MinValue, false, false, TimeSpan.MaxValue, false, false);

        /// <summary>
        /// Canonical empty instance (bounded, non-inclusive, single point).
        /// </summary>
        public static readonly RelativeTimeInterval Empty =
            new (TimeSpan.Zero, false, true, TimeSpan.Zero, false, true);

        /// <summary>
        /// Zero interval (unbounded but inclusive, zero value).
        /// </summary>
        public static readonly RelativeTimeInterval Zero =
            new (TimeSpan.Zero, true, true, TimeSpan.Zero, true, true);

        private static readonly RelativeTimeInterval PastInterval =
            new (TimeSpan.MinValue, false, false, TimeSpan.Zero, true, true);

        private static readonly RelativeTimeInterval FutureInterval =
            new (TimeSpan.Zero, true, true, TimeSpan.MaxValue, false, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="rightPoint">Right bound point.</param>
        public RelativeTimeInterval(TimeSpan leftPoint, TimeSpan rightPoint)
            : base(new IntervalEndpoint<TimeSpan>(leftPoint, true), new IntervalEndpoint<TimeSpan>(rightPoint, true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point.</param>
        /// <param name="leftInclusive">Whether left point is inclusive.</param>
        /// <param name="rightPoint">Right bound point.</param>
        /// <param name="rightInclusive">Whether right point is inclusive.</param>
        public RelativeTimeInterval(TimeSpan leftPoint, bool leftInclusive, TimeSpan rightPoint, bool rightInclusive)
            : base(new IntervalEndpoint<TimeSpan>(leftPoint, leftInclusive), new IntervalEndpoint<TimeSpan>(rightPoint, rightInclusive))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftPoint">Left bound point (or min value if unbound).</param>
        /// <param name="leftInclusive">Whether left point is inclusive (always false if unbound).</param>
        /// <param name="leftBounded">Whether left point is bounded.</param>
        /// <param name="rightPoint">Right bound point (or min value if unbound).</param>
        /// <param name="rightInclusive">Whether right point is inclusive (always false if unbound).</param>
        /// <param name="rightBounded">Whether right point is bounded.</param>
        public RelativeTimeInterval(TimeSpan leftPoint, bool leftInclusive, bool leftBounded, TimeSpan rightPoint, bool rightInclusive, bool rightBounded)
            : base(
                leftBounded ? new IntervalEndpoint<TimeSpan>(leftPoint, leftInclusive) : new IntervalEndpoint<TimeSpan>(leftPoint),
                rightBounded ? new IntervalEndpoint<TimeSpan>(rightPoint, rightInclusive) : new IntervalEndpoint<TimeSpan>(rightPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeTimeInterval"/> class.
        /// </summary>
        /// <param name="leftEndpoint">Left endpoint.</param>
        /// <param name="rightEndpoint">Right endpoint.</param>
        public RelativeTimeInterval(IntervalEndpoint<TimeSpan> leftEndpoint, IntervalEndpoint<TimeSpan> rightEndpoint)
            : base(leftEndpoint, rightEndpoint)
        {
        }

        /// <summary>
        /// Gets the point minimum value.
        /// </summary>
        protected override TimeSpan PointMinValue
        {
            get { return TimeSpan.MinValue; }
        }

        /// <summary>
        /// Gets the point maximum value.
        /// </summary>
        protected override TimeSpan PointMaxValue
        {
            get { return TimeSpan.MaxValue; }
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
        /// Equality operator that returns true if the operands are equal, false otherwise.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A value indicating whether the operands are equal.</returns>
        public static bool operator ==(RelativeTimeInterval x, RelativeTimeInterval y) => Equals(x, y);

        /// <summary>
        /// Inequality operator that returns true if the operands are not equal, false otherwise.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A value indicating whether the operands are not equal.</returns>
        public static bool operator !=(RelativeTimeInterval x, RelativeTimeInterval y) => !(x == y);

        /// <summary>
        /// Construct TimeInterval relative to an origin (DateTime).
        /// </summary>
        /// <param name="origin">Origin time point.</param>
        /// <param name="relative">Relative endpoints.</param>
        /// <returns>Translated interval.</returns>
        public static TimeInterval operator +(DateTime origin, RelativeTimeInterval relative)
            => new (origin, relative);

        /// <summary>
        /// Returns a relative time interval describing the past. The returned interval includes the present moment.
        /// </summary>
        /// <returns>A relative time interval describing the past.</returns>
        public static RelativeTimeInterval Past() => PastInterval;

        /// <summary>
        /// Returns a relative time interval of a specified duration in the past. The returned interval includes the present moment.
        /// </summary>
        /// <param name="duration">The duration of the time interval.</param>
        /// <param name="inclusive">Indicates if the interval should be inclusive of the left endpoint.</param>
        /// <returns>A relative time interval of a specified duration in the past.</returns>
        public static RelativeTimeInterval Past(TimeSpan duration, bool inclusive = true)
            => new (-duration, inclusive, true, TimeSpan.Zero, true, true);

        /// <summary>
        /// Returns a relative time interval describing the future. The returned interval includes the present moment.
        /// </summary>
        /// <returns>A relative time interval describing the future.</returns>
        public static RelativeTimeInterval Future() => FutureInterval;

        /// <summary>
        /// Returns a relative time interval of a specified duration in the future. The returned interval includes the present moment.
        /// </summary>
        /// <param name="duration">The duration of the time interval.</param>
        /// <param name="inclusive">Indicates if the interval should be inclusive of the right endpoint.</param>
        /// <returns>A relative time interval of a specified duration in the future.</returns>
        public static RelativeTimeInterval Future(TimeSpan duration, bool inclusive = true)
            => new (TimeSpan.Zero, true, true, duration, inclusive, true);

        /// <summary>
        /// Determine coverage from minimum left to maximum right.
        /// </summary>
        /// <param name="intervals">Sequence of intervals.</param>
        /// <remarks>Returns negative interval from max to min point when sequence is empty.</remarks>
        /// <returns>Interval from minimum left to maximum right value.</returns>
        public static RelativeTimeInterval Coverage(IEnumerable<RelativeTimeInterval> intervals)
            => Coverage(intervals, (left, right) => new RelativeTimeInterval(left, right), RelativeTimeInterval.Empty);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <param name="left">Left bound point.</param>
        /// <param name="inclusive">Whether left point is inclusive.</param>
        /// <returns>A left-bound instance of the <see cref="RelativeTimeInterval"/> class.</returns>
        public static RelativeTimeInterval LeftBounded(TimeSpan left, bool inclusive)
            => new (left, inclusive, true, TimeSpan.MaxValue, false, false);

        /// <summary>
        /// Constructor helper for left-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="left">Left bound point.</param>
        /// <returns>A left-bound instance of the <see cref="RelativeTimeInterval"/> class.</returns>
        public static RelativeTimeInterval LeftBounded(TimeSpan left) => LeftBounded(left, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <param name="right">Right bound point.</param>
        /// <param name="inclusive">Whether right point is inclusive.</param>
        /// <returns>A right-bound instance of the <see cref="RelativeTimeInterval"/> class.</returns>
        public static RelativeTimeInterval RightBounded(TimeSpan right, bool inclusive)
            => new (TimeSpan.MinValue, false, false, right, inclusive, true);

        /// <summary>
        /// Constructor helper for right-bound instances.
        /// </summary>
        /// <remarks>Defaults to inclusive.</remarks>
        /// <param name="right">Right bound point.</param>
        /// <returns>A right-bound instance of the <see cref="RelativeTimeInterval"/> class.</returns>
        public static RelativeTimeInterval RightBounded(TimeSpan right) => RightBounded(right, true);

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public override RelativeTimeInterval Translate(TimeSpan span)
            => this.Translate(span, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by span distances.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override RelativeTimeInterval Scale(TimeSpan left, TimeSpan right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));

        /// <summary>
        /// Scale endpoints by factors.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public override RelativeTimeInterval Scale(float left, float right)
            => this.Scale(left, right, (lp, li, lb, rp, ri, rb) => new RelativeTimeInterval(lp, li, lb, rp, ri, rb));

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => (obj is RelativeTimeInterval other) && this.Equals(other);

        /// <inheritdoc/>
        public bool Equals(RelativeTimeInterval other)
            => (this.LeftEndpoint.Point, this.LeftEndpoint.Inclusive, this.LeftEndpoint.Bounded, this.RightEndpoint.Point, this.RightEndpoint.Inclusive, this.RightEndpoint.Bounded) ==
                (other.LeftEndpoint.Point, other.LeftEndpoint.Inclusive, other.LeftEndpoint.Bounded, other.RightEndpoint.Point, other.RightEndpoint.Inclusive, other.RightEndpoint.Bounded);

        /// <inheritdoc/>
        public override int GetHashCode()
            => (this.LeftEndpoint.Point,
                this.LeftEndpoint.Inclusive,
                this.LeftEndpoint.Bounded,
                this.RightEndpoint.Point,
                this.RightEndpoint.Inclusive,
                this.RightEndpoint.Bounded).GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            var openParens = this.LeftEndpoint.Inclusive ? "[" : "(";
            var min = this.TimeSpanToString(this.LeftEndpoint.Point);
            var max = this.TimeSpanToString(this.RightEndpoint.Point);
            var closeParens = this.RightEndpoint.Inclusive ? "]" : ")";
            return $"{openParens}{min},{max}{closeParens}";
        }

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value.</param>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled span.</returns>
        protected override TimeSpan ScaleSpan(TimeSpan span, double factor)
            => new ((long)Math.Round(span.Ticks * factor));

        /// <summary>
        /// Negate span.
        /// </summary>
        /// <param name="span">Span to be negated.</param>
        /// <returns>Negated span.</returns>
        protected override TimeSpan NegateSpan(TimeSpan span) => -span;

        /// <summary>
        /// Translate point by given span.
        /// </summary>
        /// <param name="point">Point value.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated point.</returns>
        protected override TimeSpan TranslatePoint(TimeSpan point, TimeSpan span) => point + span;

        /// <summary>
        /// Determine span between two given points.
        /// </summary>
        /// <param name="x">First point.</param>
        /// <param name="y">Second point.</param>
        /// <returns>Span between points.</returns>
        protected override TimeSpan Difference(TimeSpan x, TimeSpan y) => x - y;

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected override int ComparePoints(TimeSpan a, TimeSpan b) => a.CompareTo(b);

        private string TimeSpanToString(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                return "0";
            }
            else if (timeSpan == TimeSpan.MinValue)
            {
                return double.NegativeInfinity.ToString();
            }
            else if (timeSpan == TimeSpan.MaxValue)
            {
                return double.PositiveInfinity.ToString();
            }
            else if (timeSpan.TotalMilliseconds < 1000 && timeSpan.TotalMilliseconds == Math.Floor(timeSpan.TotalMilliseconds))
            {
                return $"{(int)timeSpan.TotalMilliseconds}ms";
            }
            else
            {
                return timeSpan.ToString();
            }
        }
    }
}