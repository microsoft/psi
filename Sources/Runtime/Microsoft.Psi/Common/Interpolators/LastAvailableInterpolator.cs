// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common.Interpolators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements a greedy interpolator that selects the last value from a specified window. The
    /// interpolator only considers messages available in the window on the secondary stream at
    /// the moment the primary stream message arrives. As such, it belongs to the class of greedy
    /// interpolators and does not guarantee reproducible results.
    /// </summary>
    /// <typeparam name="T">The type of messages.</typeparam>
    public sealed class LastAvailableInterpolator<T> : GreedyInterpolator<T>
    {
        private readonly RelativeTimeInterval relativeTimeInterval;
        private readonly bool orDefault;
        private readonly T defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastAvailableInterpolator{T}"/> class.
        /// </summary>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the first message.</param>
        /// <param name="orDefault">Indicates whether to output a default value when no result is found.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        public LastAvailableInterpolator(RelativeTimeInterval relativeTimeInterval, bool orDefault, T defaultValue = default)
        {
            this.relativeTimeInterval = relativeTimeInterval;
            this.orDefault = orDefault;
            this.defaultValue = defaultValue;
        }

        /// <inheritdoc/>
        public override InterpolationResult<T> Interpolate(DateTime interpolationTime, IEnumerable<Message<T>> messages, DateTime? closedOriginatingTime)
        {
            // If no messages available,
            if (messages.Count() == 0)
            {
                // Then depending on orDefault, either create a default value or return does not exist.
                return this.orDefault ?
                    InterpolationResult<T>.Create(this.defaultValue, DateTime.MinValue) :
                    InterpolationResult<T>.DoesNotExist(DateTime.MinValue);
            }

            Message<T> lastMessage = default;
            bool found = false;

            foreach (var message in messages)
            {
                var delta = message.OriginatingTime - interpolationTime;

                // Determine if the message is on the right side of the window start
                var messageIsAfterWindowStart = this.relativeTimeInterval.LeftEndpoint.Inclusive ? delta >= this.relativeTimeInterval.Left : delta > this.relativeTimeInterval.Left;

                // Only consider messages that occur within the lookback window.
                if (messageIsAfterWindowStart)
                {
                    // Determine if the message is outside the window end
                    var messageIsOutsideWindowEnd = this.relativeTimeInterval.RightEndpoint.Inclusive ? delta > this.relativeTimeInterval.Right : delta >= this.relativeTimeInterval.Right;

                    // We stop searching either when we reach a message that is beyond the lookahead window
                    if (messageIsOutsideWindowEnd)
                    {
                        break;
                    }

                    // keep track of message and keep going
                    lastMessage = message;
                    found = true;
                }
            }

            // If we found a last message in the window
            if (found)
            {
                // Return the matching message value as the matched result.
                // All messages before the matching message are obsolete.
                return InterpolationResult<T>.Create(lastMessage.Data, lastMessage.OriginatingTime);
            }
            else
            {
                // o/w, that means no match was found, which implies there was no message in the
                // window. In that case, either return DoesNotExist or the default value.
                var windowLeft = interpolationTime.BoundedAdd(this.relativeTimeInterval.Left);
                return this.orDefault ?
                    InterpolationResult<T>.Create(this.defaultValue, windowLeft) :
                    InterpolationResult<T>.DoesNotExist(windowLeft);
            }
        }
    }
}