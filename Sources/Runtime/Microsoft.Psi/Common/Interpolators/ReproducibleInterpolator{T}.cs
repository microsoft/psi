// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Defines a reproducible stream interpolator with the same input and output type.
    /// </summary>
    /// <typeparam name="T">The type of the input messages and of the result.</typeparam>
    /// <remarks>Reproducible interpolators produce results that do not depend on the wall-clock time of
    /// message arrival on a stream, i.e., they are based on originating times of messages. As a result,
    /// these interpolators might introduce extra delays as they might have to wait for enough messages on the
    /// secondary stream to proove that the interpolation result is correct, irrespective of any other messages
    /// that might arrive later.</remarks>
    public abstract class ReproducibleInterpolator<T> : ReproducibleInterpolator<T, T>
    {
        /// <summary>
        /// Implicitly convert relative time intervals to the equivalent of a reproducible nearest match within that window.
        /// </summary>
        /// <param name="window">Window within which to match messages.</param>
        public static implicit operator ReproducibleInterpolator<T>(RelativeTimeInterval window)
        {
            return Reproducible.Nearest<T>(window);
        }

        /// <summary>
        /// Implicitly convert timespan to the equivalent of a reproducibla nearest match with that tolerance.
        /// </summary>
        /// <param name="tolerance">Relative window tolerance within which to match messages.</param>
        public static implicit operator ReproducibleInterpolator<T>(TimeSpan tolerance)
        {
            return Reproducible.Nearest<T>(tolerance);
        }
    }
}
