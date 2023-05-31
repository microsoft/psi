// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Common.Interpolators;

    /// <summary>
    /// Collection of greedy interpolators that act on immediately available data.
    /// </summary>
    /// <remarks>The interpolators defined by the <see cref="Available"/> class produce results
    /// based on what is available on the secondary stream at the moment the primary message
    /// arrives. As such, they depend on the wall-clock time of message arrival, and hence are
    /// not guaranteed to produce reproducible results. For reproducible interpolators, see
    /// the interpolators defined by the <see cref="Reproducible"/> static class.</remarks>
    public static class Available
    {
        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time exactly matching the interpolation time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Exact<T>()
        {
            return new NearestAvailableInterpolator<T>(RelativeTimeInterval.Zero, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time exactly matching the interpolation time,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> ExactOrDefault<T>(T defaultValue = default)
        {
            return new NearestAvailableInterpolator<T>(RelativeTimeInterval.Zero, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Nearest<T>()
        {
            return new NearestAvailableInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time,
        /// within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Nearest<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new NearestAvailableInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time,
        /// within a given tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Nearest<T>(TimeSpan tolerance)
        {
            return new NearestAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> NearestOrDefault<T>(T defaultValue = default)
        {
            return new NearestAvailableInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time,
        /// within a specified <see cref="RelativeTimeInterval"/>, or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> NearestOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new NearestAvailableInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the available value with an originating time nearest to the interpolation time,
        /// within a given tolerance, or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> NearestOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new NearestAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value in the stream.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> First<T>()
        {
            return new FirstAvailableInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the first message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> First<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new FirstAvailableInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified time tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the first message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> First<T>(TimeSpan tolerance)
        {
            return new FirstAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value in the stream, or default if no such
        /// value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> FirstOrDefault<T>(T defaultValue = default)
        {
            return new FirstAvailableInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified <see cref="RelativeTimeInterval"/>,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the first message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> FirstOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new FirstAvailableInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified time tolerance, or default if
        /// no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the first message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> FirstOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new FirstAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the last available value in the stream.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Last<T>()
        {
            return new LastAvailableInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the last available value within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the last message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Last<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new LastAvailableInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified time tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the last message.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> Last<T>(TimeSpan tolerance)
        {
            return new LastAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Greedy interpolator that selects the last available value in the stream, or default if no such
        /// value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> LastOrDefault<T>(T defaultValue = default)
        {
            return new LastAvailableInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the last available value within a specified <see cref="RelativeTimeInterval"/>,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the last message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> LastOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new LastAvailableInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Greedy interpolator that selects the first available value within a specified time tolerance,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the last message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The greedy interpolator.</returns>
        public static GreedyInterpolator<T> LastOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new LastAvailableInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }
    }
}
