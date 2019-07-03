// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    /// <summary>
    /// The counters we support.
    /// </summary>
    public enum PriorityQueueCounters
    {
        /// <summary>
        /// The number of workitems in the global queue.
        /// </summary>
        WorkitemCount,

        /// <summary>
        /// The time it took to enqueue a workitem.
        /// </summary>
        EnqueuingTime,

        /// <summary>
        /// The delta between the time the message was posted and the time the message was received.
        /// </summary>
        DequeueingTime,

        /// <summary>
        /// The ratio of retries per enqueue operation.
        /// </summary>
        EnqueueingRetries,

        /// <summary>
        /// The base counter for computing the enqueuing retry count.
        /// </summary>
        EnqueueingCount,

        /// <summary>
        /// The ratio of retries per dequeue operation.
        /// </summary>
        DequeuingRetries,

        /// <summary>
        /// The base counter for computing the dequeuing retry count.
        /// </summary>
        DequeueingCount,
    }
}
