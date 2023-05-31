// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an time interval summarizer that performs interval-based data summarization over a series of data values.
    /// </summary>
    [Summarizer]
    public class TimeIntervalSummarizer : Summarizer<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalSummarizer"/> class.
        /// </summary>
        public TimeIntervalSummarizer()
            : base(Summarizer)
        {
        }

        /// <summary>
        /// Summarizes an enumerable of time interval messages into summarized time intervals.
        /// </summary>
        /// <param name="messages">Enumerable of time interval messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>List of summarized time intervals.</returns>
        public static List<IntervalData<Tuple<DateTime, DateTime>>> Summarizer(IEnumerable<Message<Tuple<DateTime, DateTime>>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();
                        var ascendingLatencies = group.OrderBy(m => m.Data.Item2 - m.Data.Item1).Select(m => m.Data);
                        var minLatency = ascendingLatencies.First();
                        var maxLatency = ascendingLatencies.Last();

                        // Use max latency as representative value
                        return IntervalData.Create(
                            maxLatency,
                            minLatency,
                            maxLatency,
                            firstMessage.OriginatingTime,
                            lastMessage.OriginatingTime - firstMessage.OriginatingTime);
                    }).ToList();
        }
    }
}
