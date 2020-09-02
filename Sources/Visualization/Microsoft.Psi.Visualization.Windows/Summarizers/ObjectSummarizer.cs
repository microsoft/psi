// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an object summarizer that performs interval-based data summarization over a series of objects.
    /// </summary>
    [Summarizer]
    public class ObjectSummarizer : Summarizer<object, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSummarizer"/> class.
        /// </summary>
        public ObjectSummarizer()
            : base(Summarizer)
        {
        }

        private static List<IntervalData<object>> Summarizer(IEnumerable<Message<object>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();
                        var representative = (object)firstMessage.Data;
                        return IntervalData.Create(
                            representative,
                            default(object),
                            default(object),
                            firstMessage.OriginatingTime, // First message's OT
                            lastMessage.OriginatingTime - firstMessage.OriginatingTime); // Interval between first and last messages
                    }).ToList();
        }
    }
}
