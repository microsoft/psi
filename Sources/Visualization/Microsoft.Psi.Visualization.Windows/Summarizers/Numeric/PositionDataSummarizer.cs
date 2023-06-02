// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over bool values.
    /// </summary>
    [Summarizer]
    public class PositionDataSummarizer : Summarizer<object, PositionData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionDataSummarizer"/> class.
        /// </summary>
        public PositionDataSummarizer()
            : base(Summarizer)
        {
        }

        private static List<IntervalData<PositionData>> Summarizer(IEnumerable<Message<object>> messages, TimeSpan interval)
        {
            return messages
                .Select(
                    group =>
                    {
                        var firstMessage = messages.First();
                        var lastMessage = messages.Last();
                        var representative = (PositionData)firstMessage.Data;
                        return IntervalData.Create(
                            representative,
                            default(PositionData),
                            default(PositionData),
                            firstMessage.OriginatingTime, // First message's OT
                            lastMessage.OriginatingTime - firstMessage.OriginatingTime); // Interval between first and last messages
                    }).ToList();
        }
    }
}
