// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;

    /// <summary>
    /// Class that represents diagnostics collector configuration information.
    /// </summary>
    public class DiagnosticsConfiguration
    {
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static readonly DiagnosticsConfiguration Default = new DiagnosticsConfiguration();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsConfiguration"/> class.
        /// </summary>
        public DiagnosticsConfiguration()
        {
            this.SamplingInterval = TimeSpan.FromMilliseconds(100);
            this.TrackMessageSize = false;
            this.AveragingTimeSpan = TimeSpan.FromSeconds(1);
            this.IncludeStoppedPipelines = false;
            this.IncludeStoppedPipelineElements = false;
        }

        /// <summary>
        /// Gets or sets sampling interval.
        /// </summary>
        public TimeSpan SamplingInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to track message sizes (notable performance penalty).
        /// </summary>
        public bool TrackMessageSize { get; set; }

        /// <summary>
        /// Gets or sets the time span over which to average latencies, processing time, message sizes, ...
        /// </summary>
        public TimeSpan AveragingTimeSpan { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include stopped pipelines.
        /// </summary>
        public bool IncludeStoppedPipelines { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include stopped pipeline elements.
        /// </summary>
        public bool IncludeStoppedPipelineElements { get; set; }
    }
}