// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a generic sampling summarizer.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    public abstract class SamplingSummarizer<T> : Summarizer<T, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingSummarizer{T}"/> class.
        /// </summary>
        public SamplingSummarizer()
            : base(Summarizer, Combiner)
        {
        }

        /// <summary>
        /// Summarizes an enumerable of time interval messages into summarized time intervals.
        /// </summary>
        /// <param name="messages">Enumerable of time interval messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>List of summarized time intervals.</returns>
        public static List<IntervalData<T>> Summarizer(IEnumerable<Message<T>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();

                        // Use max latency as representative value
                        return IntervalData.Create(
                            lastMessage.Data,
                            lastMessage.Data,
                            lastMessage.Data,
                            firstMessage.OriginatingTime,
                            lastMessage.OriginatingTime - firstMessage.OriginatingTime);
                    }).ToList();
        }

        /// <summary>
        /// Default method for combining two <see cref="IntervalData{TDest}"/> values.
        /// </summary>
        /// <param name="left">The first value to combine.</param>
        /// <param name="right">The second value to combine.</param>
        /// <returns>The combined value.</returns>
        public static IntervalData<T> Combiner(IntervalData<T> left, IntervalData<T> right)
        {
            T value;
            DateTime originatingTime;

            // Take the value which occurs last, and the time which occurs first
            if (left.OriginatingTime <= right.OriginatingTime)
            {
                value = right.Value;
                originatingTime = left.OriginatingTime;
            }
            else
            {
                value = left.Value;
                originatingTime = right.OriginatingTime;
            }

            // Take the whichever end time occurs last and use it to find the interval
            TimeSpan interval = (right.EndTime > left.EndTime ? right.EndTime : left.EndTime) - originatingTime;
            return IntervalData.Create(value, value, value, originatingTime, interval);
        }
    }
}
