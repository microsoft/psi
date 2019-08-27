// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common.Interpolators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements a interpolator based on the values adjacent to the interpolation time, i.e. the
    /// nearest values before and after the interpolation time.
    /// </summary>
    /// <typeparam name="TIn">The type of the messages to interpolate.</typeparam>
    /// <typeparam name="TOut">The type of the output interpolation result.</typeparam>
    /// <remarks>The interpolator results do not depend on the wall-clock time of the messages arriving
    /// on the secondary stream, i.e., they are based on originating times of messages. As a result,
    /// the interpolator might introduce an extra delay as it might have to wait for enough messages on the
    /// secondary stream to proove that the interpolation result is correct, irrespective of any other messages
    /// that might arrive later.</remarks>
    public class AdjacentValuesInterpolator<TIn, TOut> : ReproducibleInterpolator<TIn, TOut>
    {
        private readonly Func<TIn, TIn, double, TOut> interpolator;
        private readonly bool orDefault;
        private readonly TOut defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjacentValuesInterpolator{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="interpolator">An interpolator function which given the two nearest values and the ratio
        /// between them where the interpolation result should be produces the interpolation result.</param>
        /// <param name="orDefault">Indicates whether to output a default value when no result is found.</param>
        /// <param name="defaultValue">An optional default value to use.</param>
        public AdjacentValuesInterpolator(Func<TIn, TIn, double, TOut> interpolator, bool orDefault, TOut defaultValue = default)
        {
            this.interpolator = interpolator;
            this.orDefault = orDefault;
            this.defaultValue = defaultValue;
        }

        /// <inheritdoc/>
        public override InterpolationResult<TOut> Interpolate(DateTime interpolationTime, IEnumerable<Message<TIn>> messages, DateTime? closedOriginatingTime)
        {
            // If no messages available,
            if (messages.Count() == 0)
            {
                // If stream is closed,
                if (closedOriginatingTime.HasValue)
                {
                    // Then no other value or better match will appear, so depending on orDefault,
                    // either create a default value or return does not exist.
                    return this.orDefault ?
                        InterpolationResult<TOut>.Create(this.defaultValue, DateTime.MinValue) :
                        InterpolationResult<TOut>.DoesNotExist(DateTime.MinValue);
                }
                else
                {
                    // otherwise if the stream is not closed yet, insufficient data
                    return InterpolationResult<TOut>.InsufficientData();
                }
            }

            Message<TIn> lastMessage = default;

            foreach (var message in messages)
            {
                // If we have a message past or at the interpolation time
                if (message.OriginatingTime >= interpolationTime)
                {
                    // Then if we have a previous message
                    if (lastMessage != default)
                    {
                        // Then interpolate and return the result
                        var ratio = (interpolationTime - lastMessage.OriginatingTime).Ticks / (double)(message.OriginatingTime - lastMessage.OriginatingTime).Ticks;
                        return InterpolationResult<TOut>.Create(
                            this.interpolator(lastMessage.Data, message.Data, ratio),
                            message.OriginatingTime == interpolationTime ? message.OriginatingTime : lastMessage.OriginatingTime);
                    }
                    else if (message.OriginatingTime == interpolationTime)
                    {
                        // O/w if the message is right at the interpolation time, we don't need the previous
                        // message
                        return InterpolationResult<TOut>.Create(this.interpolator(default, message.Data, 1), message.OriginatingTime);
                    }
                    else
                    {
                        // o/w there since we have no previous message, depending on orDefault,
                        // either create a default value or return does not exist.
                        return this.orDefault ?
                            InterpolationResult<TOut>.Create(this.defaultValue, DateTime.MinValue) :
                            InterpolationResult<TOut>.DoesNotExist(DateTime.MinValue);
                    }
                }

                lastMessage = message;
            }

            // If we are here, that means we have not seen enough data to create an interpolation result.
            // If the stream has closed
            if (closedOriginatingTime.HasValue)
            {
                // Then we will never get enough data to interpolate, so depending on orDefault
                // either create a default value or return does not exist.
                return this.orDefault ?
                    InterpolationResult<TOut>.Create(this.defaultValue, DateTime.MinValue) :
                    InterpolationResult<TOut>.DoesNotExist(lastMessage != default ? lastMessage.OriginatingTime : DateTime.MinValue);
            }
            else
            {
                // O/w we might get more data so simply wait
                return InterpolationResult<TOut>.InsufficientData();
            }
        }
    }
}
