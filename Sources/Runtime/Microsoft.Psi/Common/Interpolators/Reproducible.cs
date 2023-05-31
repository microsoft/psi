// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Common.Interpolators;

    /// <summary>
    /// Collection of reproducible interpolators.
    /// </summary>
    public static class Reproducible
    {
        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time exactly matching the interpolation time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Exact<T>()
        {
            return new NearestReproducibleInterpolator<T>(RelativeTimeInterval.Zero, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time exactly matching the interpolation time,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> ExactOrDefault<T>(T defaultValue = default)
        {
            return new NearestReproducibleInterpolator<T>(RelativeTimeInterval.Zero, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Nearest<T>()
        {
            return new NearestReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time,
        /// within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Nearest<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new NearestReproducibleInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time,
        /// within a given tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Nearest<T>(TimeSpan tolerance)
        {
            return new NearestReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> NearestOrDefault<T>(T defaultValue = default)
        {
            return new NearestReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time,
        /// within a specified <see cref="RelativeTimeInterval"/>, or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> NearestOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new NearestReproducibleInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the value with an originating time nearest to the interpolation time,
        /// within a given tolerance, or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> NearestOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new NearestReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value in the stream.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> First<T>()
        {
            return new FirstReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the first message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> First<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new FirstReproducibleInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified time tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the first message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> First<T>(TimeSpan tolerance)
        {
            return new FirstReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value in the stream, or default if no such
        /// value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> FirstOrDefault<T>(T defaultValue = default)
        {
            return new FirstReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified <see cref="RelativeTimeInterval"/>,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the first message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> FirstOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new FirstReproducibleInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified time tolerance, or default if
        /// no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the first message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> FirstOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new FirstReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the last value in the stream.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Last<T>()
        {
            return new LastReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the last value within a specified <see cref="RelativeTimeInterval"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the last message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Last<T>(RelativeTimeInterval relativeTimeInterval)
        {
            return new LastReproducibleInterpolator<T>(relativeTimeInterval, false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified time tolerance.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the last message.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> Last<T>(TimeSpan tolerance)
        {
            return new LastReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), false);
        }

        /// <summary>
        /// Reproducible interpolator that selects the last value in the stream, or default if no such
        /// value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> LastOrDefault<T>(T defaultValue = default)
        {
            return new LastReproducibleInterpolator<T>(RelativeTimeInterval.Infinite, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the last value within a specified <see cref="RelativeTimeInterval"/>,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the last message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> LastOrDefault<T>(RelativeTimeInterval relativeTimeInterval, T defaultValue = default)
        {
            return new LastReproducibleInterpolator<T>(relativeTimeInterval, true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that selects the first value within a specified time tolerance,
        /// or default if no such value is found.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">The tolerance within which to search for the last message.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        /// <returns>The reproducible interpolator.</returns>
        public static ReproducibleInterpolator<T> LastOrDefault<T>(TimeSpan tolerance, T defaultValue = default)
        {
            return new LastReproducibleInterpolator<T>(new RelativeTimeInterval(-tolerance, tolerance), true, defaultValue);
        }

        /// <summary>
        /// Reproducible interpolator that performs a linear interpolation, between
        /// the nearest messages to the originating time.
        /// </summary>
        /// <returns>The linear interpolator.</returns>
        public static AdjacentValuesInterpolator<double, double> Linear()
        {
            return new AdjacentValuesInterpolator<double, double>((i1, i2, r) => (1 - r) * i1 + r * i2, false);
        }

        internal static DateTime BoundedAdd(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan.Ticks > (DateTime.MaxValue.Ticks - dateTime.Ticks))
            {
                return DateTime.MaxValue;
            }
            else if (timeSpan.Ticks < -dateTime.Ticks)
            {
                return DateTime.MinValue;
            }
            else
            {
                return dateTime + timeSpan;
            }
        }
    }
}
