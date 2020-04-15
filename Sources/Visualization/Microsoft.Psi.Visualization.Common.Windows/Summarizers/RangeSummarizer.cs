// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of double values.
    /// </summary>
    [Summarizer]
    public class RangeSummarizer : Summarizer<double, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RangeSummarizer"/> class.
        /// </summary>
        public RangeSummarizer()
            : base(Summarizer)
        {
        }

        /// <summary>
        /// Summarizes an enumerable of double messages into summarized doubles.
        /// </summary>
        /// <param name="messages">Enumerable of double messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>List of summarized doubles.</returns>
        public static List<IntervalData<double>> Summarizer(IEnumerable<Message<double>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();
                        return IntervalData.Create(
                            lastMessage.Data, // Take last value as representative value for plotting
                            group.Min(m => m.Data), // Minimum value
                            group.Max(m => m.Data), // Maximum value
                            firstMessage.OriginatingTime, // First message's OT
                            lastMessage.OriginatingTime - firstMessage.OriginatingTime); // Interval between first and last messages
                    }).ToList();
        }
    }
}
