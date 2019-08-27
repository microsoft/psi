// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Type of interpolation result.
    /// </summary>
    public enum InterpolationResultType
    {
        /// <summary>
        /// No interpolation exists.
        /// </summary>
        DoesNotExist,

        /// <summary>
        /// An interpolation result was created based on the data.
        /// </summary>
        Created,

        /// <summary>
        /// No interpolation result can be created due to insufficient data.
        /// </summary>
        InsufficientData,
    }

    /// <summary>
    /// Result of interpolation.
    /// </summary>
    /// <typeparam name="T">Type of values being interpolated.</typeparam>
    public struct InterpolationResult<T>
    {
        /// <summary>
        /// Interpolated value (if any).
        /// </summary>
        public readonly T Value;

        /// <summary>
        /// Type of interpolation result.
        /// </summary>
        public readonly InterpolationResultType Type;

        /// <summary>
        /// Time prior to which messages on the interpolation stream are obsolete and can safely be discarded.
        /// </summary>
        public readonly DateTime ObsoleteTime;

        private InterpolationResult(T value, InterpolationResultType type, DateTime obsoleteTime)
        {
            this.Value = value;
            this.Type = type;
            this.ObsoleteTime = obsoleteTime;
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="first">First interpolation result.</param>
        /// <param name="second">Second interpolation result.</param>
        /// <returns>A value indicating whether the interpolation results are equal.</returns>
        public static bool operator ==(InterpolationResult<T> first, InterpolationResult<T> second)
        {
            return EqualityComparer<T>.Default.Equals(first.Value, second.Value) && first.Type == second.Type && first.ObsoleteTime == second.ObsoleteTime;
        }

        /// <summary>
        /// Non-equality comparison.
        /// </summary>
        /// <param name="first">First interpolation result.</param>
        /// <param name="second">Second interpolation result.</param>
        /// <returns>A value indicating whether the interpolation results are non-equal.</returns>
        public static bool operator !=(InterpolationResult<T> first, InterpolationResult<T> second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Construct interpolation result indicating insufficient data.
        /// </summary>
        /// <returns>Interpolation result indicating insufficient data.</returns>
        public static InterpolationResult<T> InsufficientData()
        {
            return new InterpolationResult<T>(default, InterpolationResultType.InsufficientData, DateTime.MinValue);
        }

        /// <summary>
        /// Construct interpolation result indicating no interpolation can be constructed based on the data.
        /// </summary>
        /// <param name="obsoleteTime">Time prior to which messages on the interpolation stream are obsolete and can safely be discarded.</param>
        /// <returns>Interpolation result indicating no interpolation can be constructed based on the data.</returns>
        public static InterpolationResult<T> DoesNotExist(DateTime obsoleteTime)
        {
            return new InterpolationResult<T>(default, InterpolationResultType.DoesNotExist, obsoleteTime);
        }

        /// <summary>
        /// Construct interpolation result indicating an interpolation was created based on the data.
        /// </summary>
        /// <param name="value">Resulting interpolation value.</param>
        /// <param name="obsoleteTime">Time prior to which messages on the interpolation stream are obsolete and can safely be discarded.</param>
        /// <returns>Interpolation result indicating an interpolation was created based on the data.</returns>
        public static InterpolationResult<T> Create(T value, DateTime obsoleteTime)
        {
            return new InterpolationResult<T>(value, InterpolationResultType.Created, obsoleteTime);
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="obj">interpolation result to which to compare.</param>
        /// <returns>A value indicating equality.</returns>
        public override bool Equals(object obj)
        {
            return (obj is InterpolationResult<T>) && (this == (InterpolationResult<T>)obj);
        }

        /// <summary>
        /// Generate a hashcode for the instance.
        /// </summary>
        /// <returns>The hashcode for the instance.</returns>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode() ^ this.Type.GetHashCode() ^ this.ObsoleteTime.GetHashCode();
        }
    }
}
