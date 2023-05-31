// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements numeric range summarization.
    /// </summary>
    internal static class NumericRangeSummarizer
    {
        /// <summary>
        /// Summarizes an enumerable of double messages into summarized doubles.
        /// </summary>
        /// <typeparam name="T">The type of messages to summarize.</typeparam>
        /// <param name="messages">Enumerable of double messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>List of summarized doubles.</returns>
        internal static List<IntervalData<T>> Summarizer<T>(IEnumerable<Message<T>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => Summarizer<T, T>.GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();

                        // Use the last value as representative for summarization, with the first message
                        // originating time.
                        return IntervalData.Create(
                            value: lastMessage.Data,
                            minimum: group.Min(m => m.Data),
                            maximum: group.Max(m => m.Data),
                            originatingTime: firstMessage.OriginatingTime,
                            interval: lastMessage.OriginatingTime - firstMessage.OriginatingTime);
                    }).ToList();
        }
    }
}
