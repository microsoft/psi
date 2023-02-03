// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common.Interpolators
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implements an interpolator based on the values adjacent to the interpolation time, i.e. the
    /// nearest values before and after the interpolation time.
    /// </summary>
    /// <typeparam name="T">The type of the messages and of the result.</typeparam>
    /// <remarks>The interpolator results do not depend on the wall-clock time of the messages arriving
    /// on the secondary stream, i.e., they are based on originating times of messages. As a result,
    /// the interpolator might introduce an extra delay as it might have to wait for enough messages on the
    /// secondary stream to prove that the interpolation result is correct, irrespective of any other messages
    /// that might arrive later.</remarks>
    public class AdjacentValuesInterpolator<T> : ReproducibleInterpolator<T>
    {
        private readonly AdjacentValuesInterpolator<T, T> interpolator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjacentValuesInterpolator{T}"/> class.
        /// </summary>
        /// <param name="interpolatorFunc">A function which produces an interpolation result, given the two nearest values
        /// and the ratio between them.</param>
        /// <param name="orDefault">Indicates whether to output a default value when no result is found.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <param name="name">An optional name for the interpolator (defaults to AdjacentValues).</param>
        public AdjacentValuesInterpolator(Func<T, T, double, T> interpolatorFunc, bool orDefault, T defaultValue = default, string name = null)
        {
            T InterpolateWithEndpointHandling(T t1, T t2, double amount)
            {
                if (amount < 0 || amount > 1)
                {
                    throw new ArgumentException("Cannot pass interpolation values less than 0 or greater than 1." +
                                                $"{nameof(AdjacentValuesInterpolator<T>)} does not support extrapolation.");
                }

                return amount == 0 ? t1 :
                       amount == 1 ? t2 :
                       interpolatorFunc(t1, t2, amount);
            }

            this.interpolator = new (InterpolateWithEndpointHandling, orDefault, defaultValue, name);
        }

        /// <inheritdoc/>
        public override InterpolationResult<T> Interpolate(DateTime interpolationTime, IEnumerable<Message<T>> messages, DateTime? closedOriginatingTime)
            => this.interpolator.Interpolate(interpolationTime, messages, closedOriginatingTime);

        /// <inheritdoc/>
        public override string ToString() => this.interpolator.ToString();
    }
}