// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// The counters supported by all Emitters.
    /// </summary>
    public enum EmitterCounters
    {
        /// <summary>
        /// The rate of received messages.
        /// </summary>
        MessageCount,

        /// <summary>
        /// Total latency, from beginning of the pipeline.
        /// </summary>
        MessageLatency,
    }
}
