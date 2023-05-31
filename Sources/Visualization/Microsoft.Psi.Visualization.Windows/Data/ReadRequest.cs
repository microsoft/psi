// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents a read request on a stream.
    /// </summary>
    public class ReadRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadRequest"/> class.
        /// </summary>
        /// <param name="startTime">Start time of the read request.</param>
        /// <param name="endTime">End time of the read request.</param>
        /// <param name="tailCount">
        /// Number of items to read in a tail read request. Set this to zero for a fixed-range read request.
        /// </param>
        /// <param name="tailRange">
        /// Tail range function for a dynamic read request. This function computes the start time of the next
        /// read range given the time of the last read item. It is typically used to compute a rolling fixed-
        /// duration window over live data. Set this to null for a fixed-range read request.
        /// </param>
        /// <param name="readIndicesOnly">Indicates whether to read the indices rather than the actual data.</param>
        public ReadRequest(DateTime startTime, DateTime endTime, uint tailCount, Func<DateTime, DateTime> tailRange, bool readIndicesOnly)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.TailCount = tailCount;
            this.TailRange = tailRange;
            this.ReadIndicesOnly = readIndicesOnly;
        }

        /// <summary>
        /// Gets the start time of the read request.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets the end time of the read request.
        /// </summary>
        public DateTime EndTime { get; }

        /// <summary>
        /// Gets the number of items to read in a tail read request.
        /// </summary>
        public uint TailCount { get; }

        /// <summary>
        /// Gets the tail range function for a dynamic read request.
        /// </summary>
        public Func<DateTime, DateTime> TailRange { get; }

        /// <summary>
        /// Gets a value indicating whether to read the indices rather than the actual data.
        /// </summary>
        public bool ReadIndicesOnly { get; }
    }
}
