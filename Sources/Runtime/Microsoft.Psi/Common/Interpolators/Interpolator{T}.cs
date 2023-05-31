// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Defines a stream interpolator with the same input and output type.
    /// </summary>
    /// <typeparam name="T">The type of the input messages and of the result.</typeparam>
    public abstract class Interpolator<T> : Interpolator<T, T>
    {
        /// <summary>
        /// Implicitly convert relative time intervals to the equivalent of a reproducible nearest match within that window.
        /// </summary>
        /// <param name="window">Window within which to match messages.</param>
        public static implicit operator Interpolator<T>(RelativeTimeInterval window) => Reproducible.Nearest<T>(window);

        /// <summary>
        /// Implicitly convert timespan to the equivalent of a reproducible nearest match with that tolerance.
        /// </summary>
        /// <param name="tolerance">Relative window tolerance within which to match messages.</param>
        public static implicit operator Interpolator<T>(TimeSpan tolerance) => Reproducible.Nearest<T>(tolerance);
    }
}
