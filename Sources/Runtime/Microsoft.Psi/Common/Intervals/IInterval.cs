// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents an interval with bounded/unbounded and inclusive/exclusive end points.
    /// </summary>
    /// <typeparam name="TPoint">Type of point values.</typeparam>
    /// <typeparam name="TSpan">Type of spans between point values.</typeparam>
    /// <typeparam name="TEndpoint">Explicit endpoint type (instance of IIntervalEndpoint{TPoint}).</typeparam>
    /// <typeparam name="T">Concrete type implementing this interface.</typeparam>
    public interface IInterval<TPoint, TSpan, TEndpoint, T>
        where TPoint : IComparable
        where TEndpoint : IIntervalEndpoint<TPoint>
    {
        /// <summary>
        /// Gets left interval endpoint.
        /// </summary>
        TEndpoint LeftEndpoint { get; }

        /// <summary>
        /// Gets left endpoint value.
        /// </summary>
        /// <remarks>For convenience (same as LeftEnpoint.Point).</remarks>
        TPoint Left { get; }

        /// <summary>
        /// Gets right interval endpoint.
        /// </summary>
        TEndpoint RightEndpoint { get; }

        /// <summary>
        /// Gets right endpoint value.
        /// </summary>
        /// <remarks>For convenience (same as RightEnpoint.Point).</remarks>
        TPoint Right { get; }

        /// <summary>
        /// Gets the span (or "diameter") of the interval.
        /// </summary>
        TSpan Span { get; } // "diameter"?

        /// <summary>
        /// Gets a value indicating whether the interval is bounded at both ends.
        /// </summary>
        bool IsFinite { get; } // both endpoints

        /// <summary>
        /// Gets a value indicating whether neither Left nor Right are inclusive.
        /// </summary>
        bool IsOpen { get; } // neither left/right inclusive

        /// <summary>
        /// Gets a value indicating whether Left and Right are both inclusive.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets a value indicating whether the interval represents a single point.
        /// </summary>
        /// <remarks>Same as !IsProper.</remarks>
        bool IsDegenerate { get; } // single point

        /// <summary>
        /// Gets a value indicating whether the interval represents a single point with closed endpoints.
        /// </summary>
        /// <remarks>Same as !IsProper.</remarks>
        bool IsEmpty { get; } // single point and closed end points

        /// <summary>
        /// Gets a value indicating whether the interval is unbounded at one end.
        /// </summary>
        /// <remarks>Same as !Left.Bounded || !Right.Bounded.</remarks>
        bool IsHalfBounded { get; }

        /// <summary>
        /// Gets a value indicating whether the interval is negative.
        /// </summary>
        bool IsNegative { get; }

        /// <summary>
        /// Gets a value indicating the center of the interval.
        /// </summary>
        /// <remarks>Throws when interval is unbounded.</remarks>
        TPoint Center { get; }

        /// <summary>
        /// Determines whether a point is within the interval.
        /// </summary>
        /// <remarks>Taking into account the inclusive/exclusive endpoints.</remarks>
        /// <param name="point">Point value to be tested.</param>
        /// <returns>Whether the point is within the interval.</returns>
        bool PointIsWithin(TPoint point);

        /// <summary>
        /// Translate by a span distance.
        /// </summary>
        /// <remarks>Unbound points do not change.</remarks>
        /// <param name="span">Span by which to translate.</param>
        /// <returns>Translated interval.</returns>
        T Translate(TSpan span);

        /// <summary>
        /// Scale from left point by a span distance.
        /// </summary>
        /// <param name="left">Span by which to scale left.</param>
        /// <param name="right">Span by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        T Scale(TSpan left, TSpan right);

        /// <summary>
        /// Scale from left point by a factor.
        /// </summary>
        /// <param name="left">Factor by which to scale left.</param>
        /// <param name="right">Factor by which to scale right.</param>
        /// <returns>Scaled interval.</returns>
        T Scale(float left, float right);

        /// <summary>
        /// Scale from left point by a span distance.
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleLeft(TSpan span);

        /// <summary>
        /// Scale from left point by a factor.
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleLeft(float factor);

        /// <summary>
        /// Scale from center point by a span distance.
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleCenter(TSpan span);

        /// <summary>
        /// Scale from center point by a factor.
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleCenter(float factor);

        /// <summary>
        /// Scale from right point by a span distance.
        /// </summary>
        /// <param name="span">Span by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleRight(TSpan span);

        /// <summary>
        /// Scale from right point by a factor.
        /// </summary>
        /// <param name="factor">Factor by which to scale.</param>
        /// <returns>Scaled interval.</returns>
        T ScaleRight(float factor);

        /// <summary>
        /// Determine whether this interval intersects another.
        /// </summary>
        /// <remarks>Same as !Disjoint(...)</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether there is an intersection.</returns>
        bool IntersectsWith(IInterval<TPoint, TSpan, TEndpoint, T> other);

        /// <summary>
        /// Determine whether this interval is disjoint with another.
        /// </summary>
        /// <remarks>Same as !Intersects(...)</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether there is an intersection.</returns>
        bool IsDisjointFrom(IInterval<TPoint, TSpan, TEndpoint, T> other);

        /// <summary>
        /// Determine whether this interval is a subset of another.
        /// </summary>
        /// <remarks>Subset may be equal (see <see cref="IsProperSubsetOf(IInterval{TPoint, TSpan, TEndpoint, T})"/>).</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether this is a subset of the other.</returns>
        bool IsSubsetOf(IInterval<TPoint, TSpan, TEndpoint, T> other);

        /// <summary>
        /// Determine whether this interval is a proper subset of another.
        /// </summary>
        /// <remarks>Subset and not equal (see <see cref="IsSubsetOf(IInterval{TPoint, TSpan, TEndpoint, T})"/>).</remarks>
        /// <param name="other">Other interval.</param>
        /// <returns>Whether this is a subset of the other.</returns>
        bool IsProperSubsetOf(IInterval<TPoint, TSpan, TEndpoint, T> other);
    }
}
