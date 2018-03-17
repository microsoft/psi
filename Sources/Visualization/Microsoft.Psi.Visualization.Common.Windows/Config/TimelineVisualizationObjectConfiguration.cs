// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a timeline visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationObjectConfiguration : StreamVisualizationObjectConfiguration
    {
        private long samplingTicks;

        /// <summary>
        /// Gets or sets the sampling ticks.
        /// </summary>
        [DataMember]
        public long SamplingTicks
        {
            get => this.samplingTicks;
            set
            {
                if (value > 0)
                {
                    value = 1L << (int)Math.Log(value, 2);
                }

                this.Set(nameof(this.SamplingTicks), ref this.samplingTicks, value);
            }
        }
    }
}
