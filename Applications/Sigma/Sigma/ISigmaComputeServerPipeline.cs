// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Microsoft.Psi.Data;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Defines a compute server pipeline.
    /// </summary>
    public interface ISigmaComputeServerPipeline
    {
        /// <summary>
        /// Gets the set of output streams.
        /// </summary>
        public IClientServerCommunicationStreams OutputStreams { get; }

        /// <summary>
        /// Initializes the compute server pipeline.
        /// </summary>
        public void Initialize();

        /// <summary>
        /// Writes the streams from the compute server pipeline to an exporter.
        /// </summary>
        /// <param name="prefix">The prefix to write the streams under.</param>
        /// <param name="exporter">The exporter.</param>
        public void Write(string prefix, Exporter exporter);
    }
}
