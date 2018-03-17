// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Type of match result.
    /// </summary>
    public enum MatchResultType
    {
        /// <summary>
        /// No suitable match result found within data.
        /// </summary>
        DoesNotExist,

        /// <summary>
        /// Match result found.
        /// </summary>
        Created,

        /// <summary>
        /// No suitable match result found due to lack of data.
        /// </summary>
        InsufficientData
    }

    /// <summary>
    /// Result of matching.
    /// </summary>
    /// <typeparam name="T">Type of values being matched.</typeparam>
#pragma warning disable SA1649 // File name must match first type name
    public struct MatchResult<T>
#pragma warning restore SA1649 // File name must match first type name
    {
        /// <summary>
        /// Matched value (if any).
        /// </summary>
        public readonly T Value;

        /// <summary>
        /// Type of match result.
        /// </summary>
        public readonly MatchResultType Type;

        /// <summary>
        /// Time after which this match result is obsolete.
        /// </summary>
        public readonly DateTime ObsoleteTime;

        private MatchResult(T value, MatchResultType type, DateTime obsoleteTime)
        {
            this.Value = value;
            this.Type = type;
            this.ObsoleteTime = obsoleteTime;
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="first">First match result.</param>
        /// <param name="second">Second match result.</param>
        /// <returns>A value indicating whether the match results are equal.</returns>
        public static bool operator ==(MatchResult<T> first, MatchResult<T> second)
        {
            return EqualityComparer<T>.Default.Equals(first.Value, second.Value) && first.Type == second.Type && first.ObsoleteTime == second.ObsoleteTime;
        }

        /// <summary>
        /// Non-equality comparison.
        /// </summary>
        /// <param name="first">First match result.</param>
        /// <param name="second">Second match result.</param>
        /// <returns>A value indicating whether the match results are non-equal.</returns>
        public static bool operator !=(MatchResult<T> first, MatchResult<T> second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Construct match result indicating insufficient data.
        /// </summary>
        /// <param name="obsoleteTime">Time after which this match result is obsolete.</param>
        /// <returns>Match result indicating insufficient data.</returns>
        public static MatchResult<T> InsufficientData(DateTime obsoleteTime)
        {
            return new MatchResult<T>(default(T), MatchResultType.InsufficientData, obsoleteTime);
        }

        /// <summary>
        /// Construct match result indicating no match found within data.
        /// </summary>
        /// <param name="obsoleteTime">Time after which this match result is obsolete.</param>
        /// <returns>Match result indicating no match found within data.</returns>
        public static MatchResult<T> DoesNotExist(DateTime obsoleteTime)
        {
            return new MatchResult<T>(default(T), MatchResultType.DoesNotExist, obsoleteTime);
        }

        /// <summary>
        /// Construct match result indicating matching value found within data.
        /// </summary>
        /// <param name="value">Matched value.</param>
        /// <param name="obsoleteTime">Time after which this match result is obsolete.</param>
        /// <returns>Match result indicating matching value found within data.</returns>
        public static MatchResult<T> Create(T value, DateTime obsoleteTime)
        {
            return new MatchResult<T>(value, MatchResultType.Created, obsoleteTime);
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="obj">Match result to which to compare.</param>
        /// <returns>A value indicating equality.</returns>
        public override bool Equals(object obj)
        {
            return (obj is MatchResult<T>) && (this == (MatchResult<T>)obj);
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

    /// <summary>
    /// Collection of interpolators used for matching message values.
    /// </summary>
    public static class Match
    {
        /// <summary>
        /// Match that takes the value with an originating time nearest to the match time exactly.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Exact<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Zero, true, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match time exactly.
        /// If no message is available matching exactly, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> ExactOrDefault<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Zero, true, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within an infinite window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Best<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Infinite, true, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within a window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="window">
        /// The window within which to search for the message that is closest to the match time.
        /// May extend up to TimeSpan.MinValue and TimeSpan.MaxValue relative to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Best<T>(RelativeTimeInterval window)
        {
            return NearestValue<T>(window, true, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within a window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">
        /// The tolerance within which to search for the message that is closest to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Best<T>(TimeSpan tolerance)
        {
            return NearestValue<T>(new RelativeTimeInterval(-tolerance, tolerance), true, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within an infinite window which looks both forward and backward in time.
        /// If no message is available in that specified window, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> BestOrDefault<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Infinite, true, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within a window which looks both forward and backward in time.
        /// If no message is available in that specified window, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="window">
        /// The window within which to search for the message that is closest to the match time.
        /// May extend up to TimeSpan.MinValue and TimeSpan.MaxValue relative to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> BestOrDefault<T>(RelativeTimeInterval window)
        {
            return NearestValue<T>(window, true, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time, within a window which looks both forward and backward in time.
        /// If no message is available in that specified window, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">
        /// The tolerance within which to search for the message that is closest to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time is required for a matched value to be created.
        /// This ensures correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> BestOrDefault<T>(TimeSpan tolerance)
        {
            return NearestValue<T>(new RelativeTimeInterval(-tolerance, tolerance), true, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match time (see remarks),
        /// within an infinite window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Any<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Infinite, false, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time (see remarks), within a window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="window">
        /// The window within which to search for the message that is closest to the match time.
        /// May extend up to TimeSpan.MinValue and TimeSpan.MaxValue relative to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Any<T>(RelativeTimeInterval window)
        {
            return NearestValue<T>(window, false, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time (see remarks), within a window which looks both forward and backward in time.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">
        /// The tolerance within which to search for the message that is closest to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> Any<T>(TimeSpan tolerance)
        {
            return NearestValue<T>(new RelativeTimeInterval(-tolerance, tolerance), false, false);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time (see remarks), within an infinite window which looks both forward and backward in time.
        /// If no message is available, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> AnyOrDefault<T>()
        {
            return NearestValue<T>(RelativeTimeInterval.Infinite, false, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time (see remarks), within a window which looks both forward and backward in time.
        /// If no message is available in that specified window, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="window">
        /// The window within which to search for the message that is closest to the match time.
        /// May extend up to TimeSpan.MinValue and TimeSpan.MaxValue relative to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> AnyOrDefault<T>(RelativeTimeInterval window)
        {
            return NearestValue<T>(window, false, true);
        }

        /// <summary>
        /// Match that takes the value with an originating time nearest to the match
        /// time (see remarks), within a window which looks both forward and backward in time.
        /// If no message is available, it returns the default(T) value.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="tolerance">
        /// The tolerance within which to search for the message that is closest to the match time.
        /// May extend up to TimeSpan.MinValue and TimeSpan.MaxValue relative to the match time.
        /// </param>
        /// <remarks>
        /// The next value at or after the match time *is not* required for a matched value to be created.
        /// This *does not* ensure correctness regardless of execution timing.
        /// </remarks>
        /// <returns>The interpolator for the match.</returns>
        public static Interpolator<T> AnyOrDefault<T>(TimeSpan tolerance)
        {
            return NearestValue<T>(new RelativeTimeInterval(-tolerance, tolerance), false, true);
        }

        private static Interpolator<T> NearestValue<T>(RelativeTimeInterval window, bool requireNextValue, bool orDefault)
        {
            return new Interpolator<T>(NearestValueFn<T>(window, requireNextValue, orDefault), window, requireNextValue, orDefault);
        }

        private static Func<DateTime, IEnumerable<Message<T>>, MatchResult<T>> NearestValueFn<T>(RelativeTimeInterval window, bool requireNextValue, bool orDefault)
        {
            return (DateTime matchTime, IEnumerable<Message<T>> messages) =>
            {
                var count = messages.Count();

                // If no messages available, signal insufficient data
                if (count == 0)
                {
                    return MatchResult<T>.InsufficientData(DateTime.MinValue);
                }

                Message<T> bestMatch = default(Message<T>);
                TimeSpan bestDistance = TimeSpan.MaxValue;
                DateTime upperBound = (window.Right < TimeSpan.Zero) ? matchTime + window.Right : matchTime;

                int i = 0;
                foreach (var message in messages)
                {
                    TimeSpan delta = message.OriginatingTime - matchTime;
                    TimeSpan distance = delta.Duration();

                    // Only consider messages that occur within the lookback window.
                    if (delta >= window.Left)
                    {
                        // We stop searching either when we reach a message that is beyond the lookahead
                        // window or when the distance (absolute delta) exceeds the best distance.
                        if ((delta > window.Right) || (distance > bestDistance))
                        {
                            break;
                        }

                        // keep track of the best match so far and its delta
                        bestMatch = message;
                        bestDistance = distance;
                    }

                    i++;
                }

                // If bestDistance is anything other than MaxValue, we found a nearest matching message.
                if (bestDistance < TimeSpan.MaxValue)
                {
                    // Check if we need to satisfy additional conditions
                    // if the best match is the last available message
                    if (requireNextValue && (i == count))
                    {
                        // We need to guarantee that bestMatch is indeed the best match. If it has an
                        // originating time that occurs at or after the match time (or the
                        // upper boundary of the window, whichever occurs earlier in time), then this
                        // must be true as we will never see a closer match in any of the messages
                        // that may arrive in the future. However if it is before the match time,
                        // then we will need to see a message beyond the match/window time to
                        // be sure that there is no closer match (i.e. as long as we haven't seen a
                        // message at or past the match/window time, it is always possible that
                        // a future message will show up with a distance that is closer to the
                        // match time.
                        if (bestMatch.OriginatingTime < upperBound)
                        {
                            // Signal insufficient data to continue waiting for more messages.
                            return MatchResult<T>.InsufficientData(DateTime.MinValue);
                        }
                    }

                    // Return the matching message value as the matched result.
                    // All messages before the matching message are obsolete.
                    return MatchResult<T>.Create(bestMatch.Data, bestMatch.OriginatingTime);
                }

                var last = messages.Last();
                if (last.OriginatingTime >= upperBound)
                {
                    // If no nearest match was found and the match time occurs before or coincident with
                    // the last message, then no future message will alter the result and we can therefore conclude
                    // that no matched value exists at that time.

                    // In that case, either return DoesNotExist or the default value (according to the parameter)
                    return orDefault ?
                        MatchResult<T>.Create(default(T), upperBound) :
                        MatchResult<T>.DoesNotExist(upperBound);
                }
                else
                {
                    // Otherwise signal insufficient data.
                    return MatchResult<T>.InsufficientData(DateTime.MinValue);
                }
            };
        }

        /// <summary>
        /// Interpolator within message windows.
        /// </summary>
        /// <typeparam name="T">The type of messages.</typeparam>
        public class Interpolator<T>
        {
            private readonly Func<DateTime, IEnumerable<Message<T>>, MatchResult<T>> matchFn;

            /// <summary>
            /// Initializes a new instance of the <see cref="Interpolator{T}"/> class.
            /// </summary>
            /// <param name="match">Function producing match results over a message window.</param>
            /// <param name="window">Message window interval.</param>
            /// <param name="requireNextValue">Whether the next value is required as confirmation of proper match.</param>
            /// <param name="orDefault">Whether to return a default value upon failure to find a suitable match.</param>
            public Interpolator(Func<DateTime, IEnumerable<Message<T>>, MatchResult<T>> match, RelativeTimeInterval window, bool requireNextValue, bool orDefault)
            {
                this.Window = window;
                this.RequireNextValue = requireNextValue;
                this.OrDefault = orDefault;
                this.matchFn = match;
            }

            /// <summary>
            /// Gets a value indicating the message window interval.
            /// </summary>
            public RelativeTimeInterval Window { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the next value is required as confirmation of proper match.
            /// </summary>
            public bool RequireNextValue { get; private set; }

            /// <summary>
            /// Gets a value indicating whether to return a default value upon failure to find a suitable match.
            /// </summary>
            public bool OrDefault { get; private set; }

            /// <summary>
            /// Implicitly convert relative time intervals to the equivalent of a `Best` match.
            /// </summary>
            /// <param name="window">Window within which to match messages.</param>
            public static implicit operator Interpolator<T>(RelativeTimeInterval window)
            {
                return NearestValue<T>(window, true, false);
            }

            /// <summary>
            /// Implicitly convert timespan to the equivalent of a `Best` match.
            /// </summary>
            /// <param name="tolerance">Relative window tolerance within which to match messages.</param>
            public static implicit operator Interpolator<T>(TimeSpan tolerance)
            {
                return NearestValue<T>(new RelativeTimeInterval(-tolerance, tolerance), true, false);
            }

            /// <summary>
            /// Find suitable match result within a window of messages.
            /// </summary>
            /// <param name="matchTime">Time at which to match.</param>
            /// <param name="messages">Window of messages.</param>
            /// <returns>Resulting match.</returns>
            public MatchResult<T> Match(DateTime matchTime, IEnumerable<Message<T>> messages)
            {
                return this.matchFn(matchTime, messages);
            }
        }
    }
}
