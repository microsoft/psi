// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    /// <typeparam name="TPoint">Type of point values.</typeparam>
    /// <typeparam name="TSpan">Type of spans between point values.</typeparam>
    /// <typeparam name="TEndpoint">Explicit endpoint type (instance of IIntervalEndpoint{TPoint}).</typeparam>
    /// <typeparam name="T">Concrete type implementing this interface.</typeparam>
    public abstract class Interval<TPoint, TSpan, TEndpoint, T> : IInterval<TPoint, TSpan, TEndpoint, T>
        where TPoint : IComparable
        where TEndpoint : IIntervalEndpoint<TPoint>
        where T : Interval<TPoint, TSpan, TEndpoint, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Interval{TPoint, TSpan, TEndpoint, T}"/> class.
        /// </summary>
        /// <param name="left">Left interval endpoint.</param>
        /// <param name="right">Right interval endpoint.</param>
        protected Interval(TEndpoint left, TEndpoint right)
        {
            if ((left.Inclusive && !left.Bounded) || (right.Inclusive && !right.Bounded))
            {
                throw new Exception("Invalid unbounded, yet inclusive endpoint");
            }

            this.LeftEndpoint = left;
            this.RightEndpoint = right;
        }

        /// <summary>
        /// Gets left interval endpoint.
        /// </summary>
        public TEndpoint LeftEndpoint { get; private set; }

        /// <summary>
        /// Gets left endpoint value.
        /// </summary>
        /// <remarks>For convenience (same as LeftEnpoint.Point).</remarks>
        public TPoint Left
        {
            get { return this.LeftEndpoint.Point; }
        }

        /// <summary>
        /// Gets right interval endpoint.
        /// </summary>
        public TEndpoint RightEndpoint { get; private set; }

        /// <summary>
        /// Gets right endpoint value.
        /// </summary>
        /// <remarks>For convenience (same as LeftEnpoint.Point).</remarks>
        public TPoint Right
        {
            get { return this.RightEndpoint.Point; }
        }

        /// <summary>
        /// Gets the span (or "diameter") of the interval.
        /// </summary>
        public TSpan Span
        {
            get
            {
                return this.IsFinite ? this.Difference(this.Right, this.Left) : this.SpanMaxValue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the interval is bounded at both ends.
        /// </summary>
        public bool IsFinite
        {
            get
            {
                return this.LeftEndpoint.Bounded && this.RightEndpoint.Bounded;
            }
        }

        /// <summary>
        /// Gets a value indicating whether neither Left nor Right are inclusive.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return !this.LeftEndpoint.Inclusive && !this.RightEndpoint.Inclusive;
            }
        }

        /// <summary>
        /// Gets a value indicating whether Left and Right are both inclusive.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return this.LeftEndpoint.Inclusive && this.RightEndpoint.Inclusive;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the interval represents a single point.
        /// </summary>
        /// <remarks>Same as !IsProper.</remarks>
        public bool IsDegenerate
        {
            get
            {
                // can already assume Left or Right is inclusive (constructor check)
                return this.Left.CompareTo(this.Right) == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the interval represents a single point with closed endpoints.
        /// </summary>
        /// <remarks>Same as !IsProperty.</remarks>
        public bool IsEmpty
        {
            get
            {
                // single point and closed end points
                return this.IsDegenerate && this.IsOpen;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the interval is unbounded at one end.
        /// </summary>
        /// <remarks>Same as !Left.Bounded || !Right.Bounded.</remarks>
        public bool IsHalfBounded
        {
            get
            {
                return !this.LeftEndpoint.Bounded || !this.RightEndpoint.Bounded;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the interval is negative.
        /// </summary>
        public bool IsNegative
        {
            get
            {
                return this.Left.CompareTo(this.Right) > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating the center of the interval.
        /// </summary>
        /// <remarks>Throws when interval is unbounded.</remarks>
        public virtual TPoint Center
        {
            get
            {
                if (!this.LeftEndpoint.Bounded)
                {
                    return this.Left;
                }

                if (!this.RightEndpoint.Bounded)
                {
                    return this.Right;
                }

                return this.TranslatePoint(this.Left, this.ScaleSpan(this.Span, 0.5));
            }
        }

        /// <summary>
        /// Gets the point minimum value.
        /// </summary>
        protected abstract TPoint PointMinValue { get; }

        /// <summary>
        /// Gets the point maximum value.
        /// </summary>
        protected abstract TPoint PointMaxValue { get; }

        /// <summary>
        /// Gets the span zero value.
        /// </summary>
        protected abstract TSpan SpanZeroValue { get; }

        /// <summary>
        /// Gets the span minimum value.
        /// </summary>
        protected abstract TSpan SpanMinValue { get; }

        /// <summary>
        /// Gets the span maximum value.
        /// </summary>
        protected abstract TSpan SpanMaxValue { get; }

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="interval">Interval to translate.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public static T operator +(Interval<TPoint, TSpan, TEndpoint, T> interval, TSpan span)
        {
            return interval.Translate(span);
        }

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="interval">Interval to translate.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public static T operator -(Interval<TPoint, TSpan, TEndpoint, T> interval, TSpan span)
        {
            return interval + interval.NegateSpan(span);
        }

        /// <summary>
        /// Determines whether a point is within the interval.
        /// </summary>
        /// <remarks>Taking into account the inclusive/exclusive endpoints.</remarks>
        /// <param name="point">Point value to be tested.</param>
        /// <returns>Whether the point is within the interval.</returns>
        public bool PointIsWithin(TPoint point)
        {
            var left = this.IsNegative ? this.Right : this.Left;
            var right = this.IsNegative ? this.Left : this.Right;
            var leftComp = left.CompareTo(point);
            var rightComp = right.CompareTo(point);
            return
                (leftComp < 0 || (leftComp == 0 && this.LeftEndpoint.Inclusive)) &&
                (rightComp > 0 || (rightComp == 0 && this.RightEndpoint.Inclusive));
        }

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        public abstract T Translate(TSpan span);

        /// <summary>
        /// Scale endpoints by span distances.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public abstract T Scale(TSpan left, TSpan right);

        /// <summary>
        /// Scale endpoints by factors.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        public abstract T Scale(float left, float right);

        /// <summary>
        /// Scale left point by a span distance.
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleLeft(TSpan span)
        {
            return this.Scale(span, this.SpanZeroValue);
        }

        /// <summary>
        /// Scale left point by a factor (helper for concrete instances).
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleLeft(float factor)
        {
            return this.Scale(factor, 1.0f);
        }

        /// <summary>
        /// Scale center point by a span distance (helper for concrete instances).
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleCenter(TSpan span)
        {
            var half = this.ScaleSpan(span, 0.5);
            return this.Scale(half, half);
        }

        /// <summary>
        /// Scale center point by a factor (helper for concrete instances).
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleCenter(float factor)
        {
            var half = ((factor - 1.0f) / 2.0f) + 1.0f;
            return this.Scale(half, half);
        }

        /// <summary>
        /// Scale right point by a span distance (helper for concrete instances).
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleRight(TSpan span)
        {
            return this.Scale(this.SpanZeroValue, span);
        }

        /// <summary>
        /// Scale right point by a factor (helper for concrete instances).
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Constructed interval.</returns>
        public T ScaleRight(float factor)
        {
            return this.Scale(1.0f, factor);
        }

        /// <summary>
        /// Determine whether this interval intersects another.
        /// </summary>
        /// <remarks>Same as !Disjoint(...)</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether there is an intersection.</returns>
        public bool IntersectsWith(IInterval<TPoint, TSpan, TEndpoint, T> other)
        {
            return
                (this.PointIsWithin(other.Left) && ((this.RightEndpoint.Inclusive && other.LeftEndpoint.Inclusive) || this.Right.CompareTo(other.Left) != 0)) ||
                (this.PointIsWithin(other.Right) && ((this.LeftEndpoint.Inclusive && other.RightEndpoint.Inclusive) || this.Left.CompareTo(other.Right) != 0)) ||
                (other.PointIsWithin(this.Left) && other.PointIsWithin(this.Right));
        }

        /// <summary>
        /// Determine whether this interval is disjoint with another.
        /// </summary>
        /// <remarks>Same as !Intersects(...)</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether there is an intersection.</returns>
        public bool IsDisjointFrom(IInterval<TPoint, TSpan, TEndpoint, T> other)
        {
            return !this.IntersectsWith(other);
        }

        /// <summary>
        /// Determine whether this interval is a subset of another.
        /// </summary>
        /// <remarks>Subset may be equal (see <see cref="IsProperSubsetOf(IInterval{TPoint, TSpan, TEndpoint, T})"/>).</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether this is a subset of the other.</returns>
        public bool IsSubsetOf(IInterval<TPoint, TSpan, TEndpoint, T> other)
        {
            return other.PointIsWithin(this.Left) && other.PointIsWithin(this.Right);
        }

        /// <summary>
        /// Determine whether this interval is a proper subset of another.
        /// </summary>
        /// <remarks>Subset and not equal (see <see cref="IsSubsetOf(IInterval{TPoint, TSpan, TEndpoint, T})"/>).</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether this is a subset of the other.</returns>
        public bool IsProperSubsetOf(IInterval<TPoint, TSpan, TEndpoint, T> other)
        {
            var matchLL = this.LeftEndpoint.Inclusive == other.LeftEndpoint.Inclusive && this.Left.CompareTo(other.Left) == 0;
            var matchRR = this.RightEndpoint.Inclusive == other.RightEndpoint.Inclusive && this.Right.CompareTo(other.Right) == 0;
            var matchLR = this.LeftEndpoint.Inclusive == other.RightEndpoint.Inclusive && this.Left.CompareTo(other.Right) == 0;
            var matchRL = this.RightEndpoint.Inclusive == other.LeftEndpoint.Inclusive && this.Right.CompareTo(other.Left) == 0;
            return this.IsSubsetOf(other) && !((matchLL && matchRR) || (matchLR && matchRL));
        }

        /// <summary>
        /// Determine coverage from minimum left to maximum right.
        /// </summary>
        /// <param name="intervals">Sequence of intervals.</param>
        /// <param name="ctor">Constructor function for interval type.</param>
        /// <param name="empty">Empty instance.</param>
        /// <remarks>Returns empty interval when sequence is empty or contains only empty intervals.</remarks>
        /// <returns>Interval from minimum left to maximum right value (or empty).</returns>
        protected static T Coverage(IEnumerable<Interval<TPoint, TSpan, TEndpoint, T>> intervals, Func<TEndpoint, TEndpoint, T> ctor, Interval<TPoint, TSpan, TEndpoint, T> empty)
        {
            var left = empty.LeftEndpoint;
            var right = empty.RightEndpoint;
            var first = true;
            foreach (var interval in intervals)
            {
                if (!interval.IsEmpty)
                {
                    var neg = interval.IsNegative;
                    var min = neg ? interval.RightEndpoint : interval.LeftEndpoint;
                    var max = neg ? interval.LeftEndpoint : interval.RightEndpoint;

                    var compLeft = interval.ComparePoints(min.Point, left.Point);
                    if (first || compLeft < 0)
                    {
                        left = min;
                    }
                    else if (compLeft == 0)
                    {
                        if ((left.Bounded && !min.Bounded) ||
                            (!left.Inclusive && min.Inclusive))
                        {
                            left = min;
                        }
                    }

                    var compRight = interval.ComparePoints(max.Point, right.Point);
                    if (first || compRight > 0)
                    {
                        right = max;
                    }
                    else if (compRight == 0)
                    {
                        if ((right.Bounded && !max.Bounded) ||
                            (!right.Inclusive && max.Inclusive))
                        {
                            right = max;
                        }
                    }

                    first = false;
                }
            }

            return ctor(left, right);
        }

        /// <summary>
        /// Compare points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>Less (-1), greater (+1) or equal (0).</returns>
        protected abstract int ComparePoints(TPoint a, TPoint b);

        /// <summary>
        /// Scale a span by a given factor.
        /// </summary>
        /// <param name="span">Span value.</param>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled span.</returns>
        protected abstract TSpan ScaleSpan(TSpan span, double factor);

        /// <summary>
        /// Negate span.
        /// </summary>
        /// <param name="span">Span to be negated.</param>
        /// <returns>Negated span.</returns>
        protected abstract TSpan NegateSpan(TSpan span);

        /// <summary>
        /// Translate point by given span.
        /// </summary>
        /// <param name="point">Point value.</param>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated point.</returns>
        protected abstract TPoint TranslatePoint(TPoint point, TSpan span);

        /// <summary>
        /// Determine span between two given points.
        /// </summary>
        /// <param name="x">First point.</param>
        /// <param name="y">Second point.</param>
        /// <returns>Span between points.</returns>
        protected abstract TSpan Difference(TPoint x, TPoint y);

        /// <summary>
        /// Translate by a span distance (helper for concrete instances).
        /// </summary>
        /// <remarks>Calls `ctor` function with constructor args.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <param name="ctor">Constructor function for concrete instance (T).</param>
        /// <returns>Constructed T.</returns>
        protected T Translate(TSpan span, Func<TPoint, bool, bool, TPoint, bool, bool, T> ctor)
        {
            return
                ctor(
                    this.LeftEndpoint.Bounded ? this.TranslatePoint(this.Left, span) : this.Left,
                    this.LeftEndpoint.Inclusive,
                    this.LeftEndpoint.Bounded,
                    this.RightEndpoint.Bounded ? this.TranslatePoint(this.Right, span) : this.Right,
                    this.RightEndpoint.Inclusive,
                    this.RightEndpoint.Bounded);
        }

        /// <summary>
        /// Scale by a span distance (helper for concrete instances).
        /// </summary>
        /// <remarks>Calls `ctor` function with constructor args.</remarks>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <param name="ctor">Constructor function for concrete instance (T).</param>
        /// <returns>Constructed T.</returns>
        protected T Scale(TSpan left, TSpan right, Func<TPoint, bool, bool, TPoint, bool, bool, T> ctor)
        {
            return
                ctor(
                    this.LeftEndpoint.Bounded ? this.TranslatePoint(this.Left, this.NegateSpan(left)) : this.Left,
                    this.LeftEndpoint.Inclusive,
                    this.LeftEndpoint.Bounded,
                    this.RightEndpoint.Bounded ? this.TranslatePoint(this.Right, right) : this.Right,
                    this.RightEndpoint.Inclusive,
                    this.RightEndpoint.Bounded);
        }

        /// <summary>
        /// Scale by a factor (helper for concrete instances).
        /// </summary>
        /// <remarks>Calls `ctor` function with constructor args.</remarks>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <param name="ctor">Constructor function for concrete instance (T).</param>
        /// <returns>Constructed T.</returns>
        protected T Scale(double left, double right, Func<TPoint, bool, bool, TPoint, bool, bool, T> ctor)
        {
            var span = this.Span;
            return
                ctor(
                    this.LeftEndpoint.Bounded ? this.TranslatePoint(this.Left, this.ScaleSpan(span, -(left - 1.0))) : this.Left,
                    this.LeftEndpoint.Inclusive,
                    this.LeftEndpoint.Bounded,
                    this.RightEndpoint.Bounded ? this.TranslatePoint(this.Right, this.ScaleSpan(span, right - 1.0)) : this.Right,
                    this.RightEndpoint.Inclusive,
                    this.RightEndpoint.Bounded);
        }
    }
}