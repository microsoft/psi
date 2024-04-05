// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// Represents the configuration for the <see cref="SigmaLiveComputeServer"/>.
    /// </summary>
    public class SigmaLiveComputeServerConfiguration
    {
        /// <summary>
        /// Gets or sets the server log filename.
        /// </summary>
        public string ServerLogFilename { get; set; }

        /// <summary>
        /// Gets or sets the available compute server pipeline configurations.
        /// </summary>
        public SigmaComputeServerPipelineConfiguration[] PipelineConfigurations { get; set; }
    }
}
